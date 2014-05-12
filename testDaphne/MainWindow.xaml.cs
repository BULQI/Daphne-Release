using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Daphne;
//using libsbmlcs;

namespace testDaphne
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int nSteps;
        private double dt;
        private ReactionsConfigurator config;

        private Dictionary<string, Molecule> MolDict;
        private Simulation sim;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void initialize()
        {
            
            double[] foo = new double[3] {1.2, 1.5, 1.7 };
            int[] ii = new int[3] {(int)foo[0], (int)foo[1], (int)foo[2]};


            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t1.0\nCXCR5:CXCL13\t\t\t1.0\ngCXCR5\t\t\t\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            // TODO: 
            // Add reference to libsbmlcs.dll, use SBML XML reader, and reformat our XML file to look more like SBML
            // Add molSpec info in XML file as SBML-lke Species references

            // Build array of ReactionTemplates from the specified XML file 
            //List<ReactionTemplate> ReacTempl =  ReactionTemplateBuilder.Go("ReacSpecFile1.xml"/*, Mol*/);
            config = new ReactionsConfigurator();

            config.deserialize("ReacSpecFile1.xml");

            config.TemplReacType(config.content.listOfReactions);

            // NOTE: created a class called ExtracelluarSpaceTypeI in Compartment.cs
            // which has as it's interior manifold BoundedRectangularPrism, but not using it at this point.

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            int[] numGridPts = {5, 5, 5};
            // min and max spatial extent in each dimension
            double[] XCellSpatialExtent = { 0.0, 10.0, 0.0, 10.0, 0.0, 10.0 };

            // NOTE: Create a BoundedRectangularPrism locally in order to be able to use GaussianDensity
            // to define a Gaussian density distribution for CXCL13 
            // If not, we could use the commented statement below
            //Compartment XCellSpace = new Compartment(new BoundedRectangularPrism(numGridPts, XCellSpatialExtent));
            BoundedRectangularPrism b = new BoundedRectangularPrism(numGridPts, XCellSpatialExtent);
            Compartment XCellSpace = new Compartment(b);

            // Uniformly populate the extracellular fluid with cells 
            int numCells = 1;
            double[] cellPos = new double[XCellSpace.Interior.Dim];

            // create the simulation
            sim = new Simulation();

            for (int i = 0; i < numCells; i++)
            {
                cellPos[0] = XCellSpatialExtent[0] + (i + 1) * (XCellSpatialExtent[1] - XCellSpatialExtent[0]) / (numCells + 1);
                cellPos[1] = XCellSpatialExtent[2] + (i + 1) * (XCellSpatialExtent[3] - XCellSpatialExtent[2]) / (numCells + 1);
                cellPos[2] = XCellSpatialExtent[4] + (i + 1) * (XCellSpatialExtent[5] - XCellSpatialExtent[4]) / (numCells + 1);

                // create the cell with its Cytoplasm and PlasmaMembrane compartments
                sim.AddCell(cellPos, new double[] { 0, 0, 0 });
            }

            //
            // Add all embedded manifolds that aren't already assigned
            // Add entries into Dictionary<Manifold,Compartment> that is required for adding Boundary reactions
            // 

            // TODO: TranslEmbedding has a position field that should get updated as the cell moves.
            // The Locator structure may not be needed.
            // We need to determine how we will implement cell motion. 
            foreach(KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                // Add the cell plasma membrane as an embedded boundary manifold in the extracellular compartment
                //MotileTSEmbedding cellMembraneEmbed = new MotileTSEmbedding(cells[i].PlasmaMembrane.Interior, (BoundedRectangularPrism)XCellSpace.Interior, cells[i].Loc);
                TranslEmbedding cellMembraneEmbed = new TranslEmbedding(kvp.Value.PlasmaMembrane.Interior, (BoundedRectangularPrism)XCellSpace.Interior, new int[1]{0}, sim.CMGR.States[kvp.Value].X);
                XCellSpace.Interior.Boundaries.Add(kvp.Value.PlasmaMembrane.Interior, cellMembraneEmbed);
            }

            //
            // Add all molecular populations
            //

            //// Adds new MolecularPopulation with intial concentration 1.0
            //XCellSpace.AddMolecularPopulation("Extracellular_CXCL13", MolDict["CXCL13"], 1.0);

            // Add a new MolecularPopulation whose concentration is a Gaussian density function
            double maxConc = 9.0;
            ScalarField s = new ScalarField(XCellSpace.Interior);
            double[] sigma = { 1.0, 1.0, 1.0 };
            double[] center = new double[XCellSpace.Interior.Dim];
            center[0] = (XCellSpatialExtent[1] + XCellSpatialExtent[0]) / 2.0;
            center[1] = (XCellSpatialExtent[3] + XCellSpatialExtent[2]) / 2.0;
            center[2] = (XCellSpatialExtent[5] + XCellSpatialExtent[4]) / 2.0;
            s = maxConc * b.GaussianField(center, sigma);
            XCellSpace.AddMolecularPopulation(MolDict["CXCL13"], s);

            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["CXCR5"], 1.0);
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["gCXCR5"], 1.0);

                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], 1.0);
            }

            // Iterate through compartments and add reactions until there aren't any more changes
            // tf = true if a reaction or product molecule was added

            bool tf;

            do
            {
                tf = false;

                // Add reactions for Compartment molecular populations on the Interior manifolds
                // ecs
                tf |= ReactionBuilder.CompartReactions(XCellSpace, config.content.listOfReactions, MolDict);
                foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                {
                    // cytosol
                    tf |= ReactionBuilder.CompartReactions(kvp.Value.Cytosol, config.content.listOfReactions, MolDict);
                    // membrane
                    tf |= ReactionBuilder.CompartReactions(kvp.Value.PlasmaMembrane, config.content.listOfReactions, MolDict);

                    // each cell has a boundary with the ecs and an internal cytosol-membrane boundary
                    // add appropriate boundary reactions to the embedded manifold
                    // NOTE: Automatically inferring the correct boundary reactions based on molecular population only
                    // is hard to do in a general way and this implementation may not be very robust.
                    // TODO: Implement transport reactions (e.g., cytosol CXCR5 -> plasma membrane CXCR5)
                    // TODO: Tom would prefer that the boundary reactions get added to the embedding manifold, 
                    // rather than the embedded manifold, but this is problematic without some changes to the algorithm
                    // in CompartBoundaryReactions()

                    // ecs - membrane
                    tf |= ReactionBuilder.CompartBoundaryReactions(XCellSpace, kvp.Value.PlasmaMembrane, config.content.listOfReactions, MolDict);
                    // cell cytosol - membrane
                    tf |= ReactionBuilder.CompartBoundaryReactions(kvp.Value.Cytosol, kvp.Value.PlasmaMembrane, config.content.listOfReactions, MolDict);
                }

            } while (tf);
            
            //// This code is for debugging purposes
            //int cnt = 0;
            //foreach (Compartment c in CompList)
            //{
            //    Console.WriteLine("\n");
            //    Console.WriteLine("Compartment {0}\n", cnt);
            //    foreach (ReactionTemplate rt in c.rtList)
            //    {
            //        foreach (SpeciesReference sp in rt.listOfReactants)
            //        {
            //            Console.WriteLine("\t{0} + ", sp.species);
            //        }
            //        foreach (SpeciesReference sp in rt.listOfModifiers)
            //        {
            //            Console.WriteLine("\t{0} + ", sp.species);
            //        }
            //        Console.WriteLine("\t -> ");
            //        foreach (SpeciesReference sp in rt.listOfProducts)
            //        {
            //            Console.WriteLine("\t{0} + ", sp.species);
            //        }
            //        Console.WriteLine("\n");

            //    }
            //    cnt++;
            //}

            // this needs to go into the simulation's variable for that
            sim.ECS = XCellSpace;

            nSteps = 3;
            dt = 1.0e-3;
            for (int i = 0; i < nSteps; i++)
            {
                XCellSpace.Step(dt);
                foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                {
                    kvp.Value.Step(dt);

                    // XCellSpace.Interior.Boundaries[cells[j].PlasmaMembrane.Interior].position;
                }
            }

            
        }

        private void go()
        {
            //if (cell == null)
            initialize();

            //// set parameters for run
            //nSteps = 1000;
            //dt = 0.1;

            //for (int i = 0; i < nSteps; i++)
            //{
            //    cell.Step(dt);
            //}
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            go();
        }
    }
}
