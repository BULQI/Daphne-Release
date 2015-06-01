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
using System.Windows.Controls.Primitives;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynOptions.xaml
    /// </summary>
    public partial class CellPopDynOptions : UserControl
    {
        public CellPopDynOptions()
        {
            InitializeComponent();
        }

        ////private void deathStatesGrid_Loaded(object sender, RoutedEventArgs e)
        ////{
        ////    var selItem = lbPlotCellPops.SelectedItem;
        ////    var selValue = lbPlotCellPops.SelectedValue;
        ////}

        ////private void lbPlotCellPops_SelectionChanged(object sender, SelectionChangedEventArgs e)
        ////{
        ////    var selItem = lbPlotCellPops.SelectedItem;
        ////    var selValue = lbPlotCellPops.SelectedValue;

            
        ////}

        

        ////private void deathStatesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        ////{
        ////    //DataGrid dg = (DataGrid)sender;
        ////    //CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
        ////    //int index = dg.SelectedIndex;
        ////    //bool plotOn = pop.Cell.death_driver.plotStates[index];

        ////    //foreach (DataGridColumn col in dg.Columns)
        ////    //{
        ////    //    if (col is DataGridCheckBoxColumn)
        ////    //    {
        ////    //        col.
        ////    //        col.SetValue = pop.Cell.death_driver.plotStates[index];
        ////    //    }
        ////    //    col.Width = DataGridLength.Auto;
        ////    //}
        ////}

        ////private void deathStatesGrid_Loaded_1(object sender, RoutedEventArgs e)
        ////{
        ////    int n = 1;
        ////    n++;
        ////}

        private void deathStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
            int index = e.Row.GetIndex();

            if (index < pop.Cell.death_driver.states.Count)
            {
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = pop.Cell.death_driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }

        }
        private void diffStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

            int index = e.Row.GetIndex();
            if (index < pop.Cell.diff_scheme.Driver.states.Count)
            {
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = pop.Cell.diff_scheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }

        }

        private void divStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

            int index = e.Row.GetIndex();
            if (index < pop.Cell.div_scheme.Driver.states.Count)
            {
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = pop.Cell.div_scheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }
        }

        private void divStatesGrid2_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

            int index = e.Row.GetIndex();
            if (index < pop.Cell.div_scheme.Driver.states.Count)
            {
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = pop.Cell.div_scheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }
        }
    }

    public class plotStateToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            bool plot = (bool)value;

            return plot;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "false";
        }
    }
}
