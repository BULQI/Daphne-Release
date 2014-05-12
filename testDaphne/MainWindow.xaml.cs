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
        Cell cell;
        int nSteps;
        double dt;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void initialize()
        {
            // create array of intracellular molecular populations 
            // this list would be built from the GUI would use SBML
            string mpSpecs = "L";

            MolecularPopulation[] iMPs = MolecularPopulationBuilder.Go(mpSpecs);
 
            // this line calls a canned reaction builder, but we would build a parser and parse SBML 
            Reaction[] reactions = ReactionBuilder.CannedBCellScheme(iMPs);

            // set initial values for iMPs
            // iMPs[1].Conc = 1;

            // create cell
            cell = CellBuilder.Build(string.Empty);

            // set up differentiator:
            //
            // 1. create array of genes
            MolecularPopulation[] genes = new MolecularPopulation[2];
            
            // 2. assign the iMPs that are genes to the genes array
            genes[0] = iMPs[1];
            genes[1] = iMPs[3];

            // 3. define the differentiation matrix, indicating which genes (columns) are on in which states (rows)
            // this, to will be returned by a parser
            int[,] diffmat = new int[2, 2] { { 1, 0 }, { 0, 1 } };

            // 4. create the array of signals
            MolecularPopulation[,] signals = new MolecularPopulation[2,2];
            
            // 5. match up signals with the corresponding iMPs
            signals[0,1] = iMPs[5];
            signals[1,0] = iMPs[7];

            // 6. create differentiator
            Differentiator differ = new Differentiator(signals, genes, cell) { DifferentationMatrix = diffmat };

            // 7. set cell's differentiator to this one
            cell.Differentiator = differ;

            // set cell differentiation state
            cell.DifferentiationState = 0;

        }

        private void go()
        {
            if (cell == null)
                initialize();

            // set parameters for run
            nSteps = 1000;
            dt = 0.1;

            for (int i = 0; i < nSteps; i++)
            {
                cell.Step(dt);
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            go();
        }
    }
}
