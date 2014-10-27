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

using System.Globalization;

using Daphne;
using DaphneUserControlLib;

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

            if (MainWindow.SOP.Protocol.scenario.time_config.sampling_interval > sampling_interval_slider.Maximum)
                MainWindow.SOP.Protocol.scenario.time_config.sampling_interval = sampling_interval_slider.Value;
        }

        private void rendering_interval_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;

            if (MainWindow.SOP.Protocol == null)
                return;

            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            if (MainWindow.SOP.Protocol.scenario.time_config.rendering_interval > render_interval_slider.Maximum)
                MainWindow.SOP.Protocol.scenario.time_config.rendering_interval = render_interval_slider.Value;
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
    }
}
