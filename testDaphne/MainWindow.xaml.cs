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
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t\nCXCR5:CXCL13\t\t\t\ngCXCR5\t\t\t\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            // Build array of ReactionTemplates from the specified XML file 
            //List<ReactionTemplate> ReacTempl =  ReactionTemplateBuilder.Go("ReacSpecFile1.xml"/*, Mol*/);
            config = new ReactionsConfigurator();

            config.deserialize("ReacSpecFile1.xml");

            // Later, we should define a compartment class -   ExtracelluarSpaceTypeI,
            // which has as it's interior manifold BoundedRectangularPrism.
            // The class BoundedRectangularPrism : DiscreteManifold also needs to be defined.
            int[] numDivisions = {5, 5, 5};
            Compartment XCellSpace = new Compartment(new RectangularPrism(numDivisions));

            // Creates Cytoplasm and PlasmaMembrane compartments
            Cell cell_1 = new Cell();

            // Add the cell plasma membrane as a boundary in the extracellular compartment
            Embedding cellMembraneEmbed = new Embedding(cell_1.PlasmaMembrane.Interior, XCellSpace.Interior);
            XCellSpace.Interior.Boundaries = new Dictionary<Manifold, Embedding>();
            XCellSpace.Interior.Boundaries.Add(cell_1.PlasmaMembrane.Interior, cellMembraneEmbed);

            // Adds new MolecularPopulation with intial concentration 1.0
            XCellSpace.Populations = new List<MolecularPopulation>();
            XCellSpace.AddMolecularPopulation("Extracellular_CXCL13", MolDict["CXCL13"], 1.0);

            cell_1.Cytosol.Populations = new List<MolecularPopulation>();
            cell_1.Cytosol.AddMolecularPopulation("Cytosolic_CXCR5", MolDict["CXCR5"], 1.0);
            cell_1.PlasmaMembrane.Populations = new List<MolecularPopulation>();
            cell_1.PlasmaMembrane.AddMolecularPopulation("Membrane_CXCR5", MolDict["CXCR5"], 1.0);

            MolecularPopulation molpop;

            cell_1.Cytosol.reactions = new List<Reaction>();
            // Find CXCR5 in the cytosol population list and add reactions
            molpop = cell_1.Cytosol.Populations.Find(delegate(MolecularPopulation mp)
            {
                return mp.Molecule.Name == "CXCR5";
            });
            if (molpop != null)
            {
                cell_1.Cytosol.reactions.Add(new Annihilation(molpop));
                // Check to see if CXCR5 is in the boundary (plasma membrane) and
                // add CXCR5 transport to and from the cell membrane, if so.
                // Ultimately, we need to add CXCR5:CXCL13 transport from the membrane into the cytoplasm,
                // but if we only specify CXCR5 in the membrane, we don't know yet that it can
                // bind to CXCL13.
            }

            XCellSpace.reactions = new List<Reaction>();
            // Find CXCR5 in the extracelluar population list and add reactions
            molpop = XCellSpace.Populations.Find(delegate(MolecularPopulation mp)
            {
                return mp.Molecule.Name == "CXCR5";
            });
            if (molpop != null)
            {
                // Add CXCL13 association and dissociation with the cell memebrane CXCR5
            }
            
            // Add reaction(s) to the extracelluar compartment
            //foreach (MolecularPopulation molpop in XCellSpace.Populations)
            //{
                // look at all the molecular populations in this compartment
                // look at the molecule name associated with a molecular population
                // find all the reactions in which this molecule name is a reactant
                // look to see whether the other reactants in that reaction are also contained in this compartment
                // if so add this reaction

                // look to see whether one or more of the reactants are in an embedded manifold
                // if so add this reaction

                // look at the products of a reaction
                // if the product molecules are not specified in a molecular population, then add this molecular population to the appropriate compartment
                // specify the initial concentration as zero
                // How do we know to which compartment to add the reaction? 
                // For example, cxcr5 (membrane) + cxcL13 (extracelluar) -> cxcr5:cxcl13 (membrane)
                // For example, cxcr5:cxc113 (membrane) -> cxcr5:cxcl13 (cytoplaxm)
            //}

           // Add reaction(s) to the Cytosol compartment
           // Add reaction(s) to the PlasmaMembrane compartment

            // this needs to go into the simulation's variable for that
            sim.ExtracellularSpace = XCellSpace;
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
