/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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

        private void deathStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

            if (pop == null)
                return;

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

        //private void divStatesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        //{
        //    DataGrid dataGrid = sender as DataGrid;
        //    CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

        //    int index = e.Row.GetIndex();
        //    if (index < pop.Cell.div_scheme.Driver.states.Count)
        //    {
        //        DataGridRowHeader dgr = new DataGridRowHeader();
        //        dgr.DataContext = pop.Cell.div_scheme.Driver;
        //        Binding binding = new Binding(string.Format("states[{0}]", index));
        //        binding.NotifyOnTargetUpdated = true;
        //        binding.Mode = BindingMode.OneWay;
        //        binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        //        dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
        //        e.Row.Header = dgr;
        //    }
        //}

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

        private void DiffCheckBox_Click(object sender, RoutedEventArgs e)
        {
            int index = diffStatesGrid.SelectedIndex;
            if (index >= 0 && index < diffStatesGrid.Items.Count)
            {
                CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
                CheckBox check = sender as CheckBox;
                if (pop != null)
                {
                    pop.Cell.diff_scheme.Driver.plotStates[index] = (bool)check.IsChecked;
                    if ((string)Tag == "Reports" && check.IsChecked == true)
                    {
                        pop.reportStates.Differentiation = true;
                    }
                }
            }
        }

        private void DivCheckBox_Click(object sender, RoutedEventArgs e)
        {
            int index = divStatesGrid.SelectedIndex;
            if (index >= 0 && index < divStatesGrid.Items.Count)
            {
                CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
                CheckBox check = sender as CheckBox;
                if (pop != null)
                {
                    pop.Cell.div_scheme.Driver.plotStates[index] = (bool)check.IsChecked;
                    if ((string)Tag == "Reports" && check.IsChecked == true)
                    {
                        pop.reportStates.Division = true;
                    }
                }
            }
        }

        private void lbPlotCellPops_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;

            if (pop == null)
                return;

            if (pop.Cell == null)
                return;

            if (pop.Cell.diff_scheme == null)
                return;

            //Differentiation
            if (pop.Cell.diff_scheme != null && pop.Cell.diff_scheme.Driver != null)
            {
                int diff = pop.Cell.diff_scheme.Driver.states.Count - pop.Cell.diff_scheme.Driver.plotStates.Count;
                //if the number of plotStates items is less than number of states, add plotStates to make them same size
                if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        pop.Cell.diff_scheme.Driver.plotStates.Add(false);
                    }
                }
            }

            //Division
            if (pop.Cell.div_scheme != null && pop.Cell.div_scheme.Driver != null)
            {
                int div = pop.Cell.div_scheme.Driver.states.Count - pop.Cell.div_scheme.Driver.plotStates.Count;
                //if the number of plotStates items is less than number of states, add plotStates to make them same size
                if (div > 0)
                {
                    for (int i = 0; i < div; i++)
                    {
                        pop.Cell.div_scheme.Driver.plotStates.Add(false);
                    }
                }
            }

            //Death
            if (pop.Cell.death_driver != null)
            {
                int death = pop.Cell.death_driver.states.Count - pop.Cell.death_driver.plotStates.Count;
                //if the number of plotStates items is less than number of states, add plotStates to make them same size
                if (death > 0)
                {
                    for (int i = 0; i < death; i++)
                    {
                        pop.Cell.death_driver.plotStates.Add(false);
                    }
                }
                else if (death < 0)
                {
                    death = -death;
                    for (int i = 0; i < death; i++)
                    {
                        int last = pop.Cell.death_driver.plotStates.Count;
                        pop.Cell.death_driver.plotStates.RemoveAt(last - 1);
                    }
                }
            }
        }

        private void DeathCheckBox_Click(object sender, RoutedEventArgs e)
        {
            int index = deathStatesGrid.SelectedIndex;
            if (index >= 0 && index < deathStatesGrid.Items.Count)
            {
                CellPopulation pop = lbPlotCellPops.SelectedItem as CellPopulation;
                CheckBox check = sender as CheckBox;
                if (pop != null)
                {
                    pop.Cell.death_driver.plotStates[index] = (bool)check.IsChecked;
                    if ((string)Tag == "Reports" && check.IsChecked == true)
                    {
                        pop.reportStates.Death = true;
                    }
                }
            }
        }

        private void lbPlotCellPops_Loaded(object sender, RoutedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox.Items.Count > 0)
                if (listBox.SelectedIndex == -1)
                    listBox.SelectedIndex = 0;
        }
       
    }


    ////////////////////////////////////////////////////////////////////
    //CONVERTERS
    //

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
