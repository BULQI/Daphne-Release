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
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t\nCXCR5:CXCL13\t\t\t1.0\ngCXCR5\t\t\t\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            // TODO: 
            // Add reference to libsbmlcs.dll, use SBML XML reader, and reformat our XML file to look more like SBML
            // Add molSpec info in XML file as SBML-lke Species references

            // Build array of ReactionTemplates from the specified XML file 
            //List<ReactionTemplate> ReacTempl =  ReactionTemplateBuilder.Go("ReacSpecFile1.xml"/*, Mol*/);
            config = new ReactionsConfigurator();

            config.deserialize("ReacSpecFile1.xml");

            config.TemplReacType(config.content.listOfReactions);

            // TODO: define a compartment class -   ExtracelluarSpaceTypeI,
            // which has as it's interior manifold BoundedRectangularPrism.

            // NOTE:The order in which compartments and molecular populations are implemented matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create all compartments
            //

            int[] numGridPts = {50, 50, 50};
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
            Cell[] cells = new Cell[numCells];
            for (int i = 0; i < numCells; i++)
            {
                cellPos[0] = XCellSpatialExtent[0] + (i+1)*(XCellSpatialExtent[1] - XCellSpatialExtent[0]) / (numCells+1);
                cellPos[1] = XCellSpatialExtent[2] + (i+1)*(XCellSpatialExtent[3] - XCellSpatialExtent[2]) / (numCells + 1);
                cellPos[2] = XCellSpatialExtent[4] + (i+1)*(XCellSpatialExtent[5] - XCellSpatialExtent[4]) / (numCells + 1);
                // Creates Cytoplasm and PlasmaMembrane compartments
                cells[i] = new Cell(cellPos);

            }

            //
            // Add all embedded manifolds that aren't already assigned
            // 

            for (int i = 0; i < numCells; i++)
            {
                // Add the cell plasma membrane as a boundary in the extracellular compartment
                MotileTSEmbedding cellMembraneEmbed = new MotileTSEmbedding(cells[i].PlasmaMembrane.Interior, (BoundedRectangularPrism) XCellSpace.Interior, cells[i].Loc);
                XCellSpace.Interior.Boundaries.Add(cells[i].PlasmaMembrane.Interior, cellMembraneEmbed);
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
            s = maxConc * b.GaussianDensity(center, sigma);
            XCellSpace.AddMolecularPopulation("Extracellular_CXCL13", MolDict["CXCL13"], s);

            for (int i = 0; i < numCells; i++)
            {
                //cell[i].Cytosol.Populations = new List<MolecularPopulation>();
                cells[i].Cytosol.AddMolecularPopulation("Cytosolic_CXCR5", MolDict["CXCR5"], 1.0);
                cells[i].Cytosol.AddMolecularPopulation("Cytosolic_gCXCR5", MolDict["gCXCR5"], 1.0);
                //cells[i]PlasmaMembrane.Populations = new List<MolecularPopulation>();
                cells[i].PlasmaMembrane.AddMolecularPopulation("Membrane_CXCR5", MolDict["CXCR5"], 1.0);
            }


            // Iterate through compartments and add reactions until there aren't any more changes
            // TO DO: change from simple 3 iterations, to sensing when there are no more changes
            // TO DO: add Boundary reactions 

            // tf = true if a reaction or product molecule was added
            bool tf;

            do
            {
                tf = false;
                tf = tf | ReactionBuilder.CompartReactions(XCellSpace, config.content.listOfReactions, MolDict);
                for (int i = 0; i < numCells; i++)
                {
                    tf = tf | ReactionBuilder.CompartReactions(cells[i].Cytosol, config.content.listOfReactions, MolDict);
                    tf = tf | ReactionBuilder.CompartReactions(cells[i].PlasmaMembrane, config.content.listOfReactions, MolDict);

                    tf = tf | ReactionBuilder.CompartBoundaryReactions(XCellSpace, config.content.listOfReactions, MolDict);
                }

            } while (tf);
            
            // create the simulation
            sim = new Simulation();
            // this needs to go into the simulation's variable for that
            sim.ExtracellularSpace = XCellSpace;

            sim.cells = new Dictionary<int, Cell>();
            for (int i = 0; i < numCells; i++)
            {
                sim.cells.Add(1, cells[i]);
            }

            nSteps = 3;
            dt = 1.0e-3;
            for (int i = 0; i < nSteps; i++)
            {
                XCellSpace.Step(dt);
                for (int j = 0; j < numCells; j++)
                {
                    cells[j].Step(dt);
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
