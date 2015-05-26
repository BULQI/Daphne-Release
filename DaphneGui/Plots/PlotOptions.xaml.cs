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

namespace DaphneGui.Plots
{
    /// <summary>
    /// Interaction logic for PlotOptions.xaml
    /// </summary>
    public partial class PlotOptions : UserControl
    {
        public PlotOptions()
        {
            InitializeComponent();
        }

        private void deathStatesGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var selItem = lbPlotCellPops.SelectedItem;
            var selValue = lbPlotCellPops.SelectedValue;
        }

        private void lbPlotCellPops_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selItem = lbPlotCellPops.SelectedItem;
            var selValue = lbPlotCellPops.SelectedValue;

            
        }

        private void deathStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ////DataGrid dg = (DataGrid)sender;
            ////CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
            ////int index = e.Row.GetIndex();
            ////bool plotOn = pop.Cell.death_driver.plotStates[index];

            //////DataGridColumn col = dg.Columns[0];

            ////foreach (DataGridColumn col in dg.Columns)
            ////{
            ////    if (col is DataGridCheckBoxColumn)
            ////    {
            ////        col.
            ////        col.SetValue = pop.Cell.death_driver.plotStates[index];
            ////    }
            ////}
        }

        private void deathStatesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //DataGrid dg = (DataGrid)sender;
            //CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
            //int index = dg.SelectedIndex;
            //bool plotOn = pop.Cell.death_driver.plotStates[index];

            //foreach (DataGridColumn col in dg.Columns)
            //{
            //    if (col is DataGridCheckBoxColumn)
            //    {
            //        col.
            //        col.SetValue = pop.Cell.death_driver.plotStates[index];
            //    }
            //    col.Width = DataGridLength.Auto;
            //}
        }
    }
}
