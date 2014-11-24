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
using DaphneUserControlLib;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ParamDistrControl : UserControl
    {
        public ParamDistrControl()
        {
            InitializeComponent();
        }

        private void cbParamDistr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of parameter distribution type
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            {
                return;
            }

            DistributedParameter distr_parameter = DataContext as DistributedParameter;
            if (distr_parameter == null)
            {
                return;
            }

            ParameterDistributionType selected_distr_type = (ParameterDistributionType)cbParamDistr.SelectedItem;
            if (selected_distr_type == distr_parameter.ParamDistr.DistributionType)
            {
                return;
            }

            switch (selected_distr_type)
            {
                case ParameterDistributionType.CONSTANT:
                    ConstantParameterDistribution new_constant_distr = new ConstantParameterDistribution();
                    distr_parameter.ParamDistr = new_constant_distr;
                    break;

                case ParameterDistributionType.CATEGORICAL:
                    CategoricalParameterDistribution new_categorical_distr = new CategoricalParameterDistribution();
                    distr_parameter.ParamDistr = new_categorical_distr;
                    break;

                case ParameterDistributionType.GAMMA:
                    GammaParameterDistribution new_gamma_distr = new GammaParameterDistribution();
                    distr_parameter.ParamDistr = new_gamma_distr;
                    break;

                case ParameterDistributionType.POISSON:
                    PoissonParameterDistribution new_poisson_distr = new PoissonParameterDistribution();
                    distr_parameter.ParamDistr = new_poisson_distr;
                    break;

                case ParameterDistributionType.UNIFORM:
                    UniformParameterDistribution new_uniform_distr = new UniformParameterDistribution();
                    distr_parameter.ParamDistr = new_uniform_distr;
                    break;

                default:
                    break;

            }
        }

        private void dgProbMass_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void dgProbMass_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }

        private void dgProbMass_Check(object sender, DataTransferEventArgs e)
        {
        }

        private void menuProbMassPaste_Click(object sender, DataTransferEventArgs e)
        {
        }

        private void dgProbMass_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void menuProbMassPaste_Click(object sender, RoutedEventArgs e)
        {

        }

    }

}
