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
using System.Windows.Shapes;
using Daphne;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynWindow.xaml
    /// </summary>
    public partial class CellPopDynWindow : Window
    {
        public CellPopDynWindow()
        {
            InitializeComponent();
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation pop = plotOptions.lbPlotCellPops.SelectedItem as CellPopulation;
            CellPopulationDynamicsData data = MainWindow.Sim.Reporter.ProvideCellPopulationDynamicsData(pop);
            int x = 0;
            x++;
        }
    }
}
