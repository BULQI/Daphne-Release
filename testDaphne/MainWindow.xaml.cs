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
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t\nCXCR5:CXCL13\t\t\t\ngCXCR5\t\t\t\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            // TODO: 
            // Add reference to libsbmlcs.dll, use SBML XML reader, and reformat our XML file to look more like SBML
            // Add molSpec info in XML file as SBML-lke Species references

            // Build array of ReactionTemplates from the specified XML file 
            //List<ReactionTemplate> ReacTempl =  ReactionTemplateBuilder.Go("ReacSpecFile1.xml"/*, Mol*/);
            config = new ReactionsConfigurator();

            config.deserialize("ReacSpecFile1.xml");

            config.TemplReacType(config.content.listOfReactions);

            // Later, we should define a compartment class -   ExtracelluarSpaceTypeI,
            // which has as it's interior manifold BoundedRectangularPrism.

            int[] numGridPts = {5, 5, 5};
            // min and max spatial extent in each dimension
            double[] XCellSpatialExtent = { 0.0, 10.0, 0.0, 10.0, 0.0, 10.0 };
            Compartment XCellSpace = new Compartment(new BoundedRectangularPrism(numGridPts, XCellSpatialExtent));

            // Creates Cytoplasm and PlasmaMembrane compartments
            Cell cell_1 = new Cell();

            // Add the cell plasma membrane as a boundary in the extracellular compartment
            Embedding cellMembraneEmbed = new Embedding(cell_1.PlasmaMembrane.Interior, XCellSpace.Interior);
            XCellSpace.Interior.Boundaries.Add(cell_1.PlasmaMembrane.Interior, cellMembraneEmbed);

            // Adds new MolecularPopulation with intial concentration 1.0
            //XCellSpace.Populations = new List<MolecularPopulation>();
            XCellSpace.AddMolecularPopulation("Extracellular_CXCL13", MolDict["CXCL13"], 1.0);

            //cell_1.Cytosol.Populations = new List<MolecularPopulation>();
            cell_1.Cytosol.AddMolecularPopulation("Cytosolic_CXCR5", MolDict["CXCR5"], 1.0);
            cell_1.Cytosol.AddMolecularPopulation("Cytosolic_gCXCR5", MolDict["gCXCR5"], 1.0);
            //cell_1.PlasmaMembrane.Populations = new List<MolecularPopulation>();
            cell_1.PlasmaMembrane.AddMolecularPopulation("Membrane_CXCR5", MolDict["CXCR5"], 1.0);


            // Iterate through compartments and add reactions until there aren't any more changes
            // TO DO: change from simple 3 iterations, to sensing when there are no more changes
            // TO DO: add Boundary reactions 

            bool tf;
            for (int i = 0; i < 2; i++)
            {

                tf = ReactionBuilder.CompartReactions(XCellSpace, config.content.listOfReactions, MolDict);
                tf = ReactionBuilder.CompartReactions(cell_1.Cytosol, config.content.listOfReactions, MolDict);
                tf = ReactionBuilder.CompartReactions(cell_1.PlasmaMembrane, config.content.listOfReactions, MolDict);

                tf = ReactionBuilder.CompartBoundaryReactions(XCellSpace, config.content.listOfReactions, MolDict);

            }
            
            // create the simulation
            sim = new Simulation();
            // this needs to go into the simulation's variable for that
            sim.ExtracellularSpace = XCellSpace;

            sim.cells = new Dictionary<int, Cell>();
            sim.cells.Add(1, cell_1);

            nSteps = 3;
            dt = 1.0e-3;
            for (int i = 0; i < nSteps; i++)
            {
                XCellSpace.Step(dt);
                cell_1.Step(dt);
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
