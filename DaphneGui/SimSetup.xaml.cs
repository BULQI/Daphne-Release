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
using System.Windows.Shapes;
using System.Globalization;

using Daphne;
using DaphneUserControlLib;
using MathNet.Numerics.Random;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for SimSetupControl.xaml
    /// </summary>
    public partial class SimSetupControl : UserControl
    {

        public SimSetupControl()
        {
            InitializeComponent();
        }

        private void comboToroidal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = e.Source as ComboBox;

            if (!cb.IsDropDownOpen)
                return;

            if (cb.SelectedIndex == (int)(BoundaryType.Toroidal))
            {
                MessageBoxResult res;
                res = MessageBox.Show("If you change the boundary condition to toroidal, all molecular populations using Linear initial distribution will be changed to Homogeneous.  Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedIndex = (int)(BoundaryType.Zero_Flux);
                    return;
                }

                foreach (ConfigMolecularPopulation cmp in MainWindow.SOP.Protocol.scenario.environment.comp.molpops)
                {
                    if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        cmp.mp_distribution = shl;
                    }
                }
            }
        }

        private void sampling_interval_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;

            if (MainWindow.SOP.Protocol == null)
                return;

            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            if (MainWindow.SOP.Protocol.scenario.time_config.sampling_interval > MainWindow.SOP.Protocol.scenario.time_config.duration)
                MainWindow.SOP.Protocol.scenario.time_config.sampling_interval = MainWindow.SOP.Protocol.scenario.time_config.duration;

        }

        private void rendering_interval_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;

            if (MainWindow.SOP.Protocol == null)
                return;

            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            if (MainWindow.SOP.Protocol.scenario.time_config.rendering_interval > MainWindow.SOP.Protocol.scenario.time_config.duration)
                MainWindow.SOP.Protocol.scenario.time_config.rendering_interval = MainWindow.SOP.Protocol.scenario.time_config.duration;
        }

        public void SelectSimSetupInGUISetExpName(string exp_name)
        {
            // SelectSimSetupInGUI();
            experiment_name_box.Text = exp_name;
            experiment_name_box.SelectAll();
            experiment_name_box.Focus();

            
        }


        private void time_duration_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;
            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            //Need to do this to make sure that the sampling and rendering sliders get the correct "Value"s
            double temp_samp = MainWindow.SOP.Protocol.scenario.time_config.sampling_interval;
            double temp_rand = MainWindow.SOP.Protocol.scenario.time_config.rendering_interval;

            if ((sampling_interval_slider == null) || (render_interval_slider == null))
            {
                return;
            }

            sampling_interval_slider.Maximum = time_duration_slider.Value;
            if (temp_samp > sampling_interval_slider.Maximum)
                temp_samp = sampling_interval_slider.Maximum;
            sampling_interval_slider.Value = temp_samp;
            MainWindow.SOP.Protocol.scenario.time_config.sampling_interval = sampling_interval_slider.Value;

            render_interval_slider.Maximum = time_duration_slider.Value;
            if (temp_rand > render_interval_slider.Maximum)
                temp_rand = render_interval_slider.Maximum;
            render_interval_slider.Value = temp_rand;
            MainWindow.SOP.Protocol.scenario.time_config.rendering_interval = render_interval_slider.Value;

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void integrator_step_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        /// <summary>
        /// Replace the global random seed with a new value specified by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void global_random_seed_LostFocus(object sender, RoutedEventArgs e)
        {
            if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;

            int oldSeed = MainWindow.SOP.Protocol.sim_params.globalRandomSeed;

            try
            {
                int newSeed = Convert.ToInt32(global_random_seed.Text);
                MainWindow.SOP.Protocol.sim_params.globalRandomSeed = newSeed;
            }
            catch
            {
                MainWindow.SOP.Protocol.sim_params.globalRandomSeed = oldSeed;
            }
        }

        /// <summary>
        /// Reset the global random seed with a new random value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNewRandomSeed_Click(object sender, RoutedEventArgs e)
        {
             if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;
            
            MainWindow.SOP.Protocol.sim_params.globalRandomSeed = RandomSeed.Robust();
        }
    }
}
