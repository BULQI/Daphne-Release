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
using System.Collections.ObjectModel;

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
            if (selected_distr_type == distr_parameter.DistributionType)
            {
                return;
            }

            ParameterDistribution old_pd = distr_parameter.ParamDistr;

            switch (selected_distr_type)
            {
                case ParameterDistributionType.CONSTANT:
                    // assign the mean of the previous distribution
                    switch (distr_parameter.DistributionType)
                    {
                        case ParameterDistributionType.GAMMA:
                            GammaParameterDistribution gpd = old_pd as GammaParameterDistribution;
                            distr_parameter.ConstValue = gpd.Shape / gpd.Rate;
                            break;

                        case ParameterDistributionType.POISSON:
                            distr_parameter.ConstValue = ((PoissonParameterDistribution)old_pd).Mean;
                            break;

                        case ParameterDistributionType.UNIFORM:
                            UniformParameterDistribution upd = old_pd as UniformParameterDistribution;
                            distr_parameter.ConstValue = (upd.MaxValue - upd.MinValue) / 2.0;
                            break;

                        case ParameterDistributionType.CATEGORICAL:
                            distr_parameter.ConstValue = ((CategoricalParameterDistribution)old_pd).MeanCategoryValue();
                            break;

                        case ParameterDistributionType.WEIBULL:
                            WeibullParameterDistribution wpd = old_pd as WeibullParameterDistribution;
                            distr_parameter.ConstValue = wpd.Scale * MathNet.Numerics.SpecialFunctions.Gamma(1.0 + 1.0 / wpd.Shape);
                            break;

                        case ParameterDistributionType.NEG_EXP:
                            distr_parameter.ConstValue = 1.0 / ((NegExpParameterDistribution)old_pd).Rate;
                            break;

                        default:
                            distr_parameter.ConstValue = 0.0;
                            break;
                    }
                    distr_parameter.ParamDistr = null;
                    distr_parameter.DistributionType = ParameterDistributionType.CONSTANT;

                    break;

                case ParameterDistributionType.CATEGORICAL:
                    CategoricalParameterDistribution new_categorical_distr = new CategoricalParameterDistribution();
                    new_categorical_distr.ProbMass.Add(new CategoricalDistrItem(distr_parameter.ConstValue, 0.5));
                    new_categorical_distr.ProbMass.Add(new CategoricalDistrItem(distr_parameter.ConstValue + 1.0, 0.5));  
                    distr_parameter.ParamDistr = new_categorical_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.CATEGORICAL;
                    break;

                case ParameterDistributionType.GAMMA:
                    GammaParameterDistribution new_gamma_distr = new GammaParameterDistribution();
                    if (distr_parameter.ConstValue != 0.0)
                    {
                        new_gamma_distr.Shape = distr_parameter.ConstValue;
                        new_gamma_distr.Rate = 1.0;
                    }
                    else
                    {
                        new_gamma_distr.Shape = 1.0;
                        new_gamma_distr.Rate = 1.0;
                    }
                    distr_parameter.ParamDistr = new_gamma_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.GAMMA;
                    break;

                case ParameterDistributionType.NEG_EXP:
                    NegExpParameterDistribution new_neg_exp_distr = new NegExpParameterDistribution();
                    if (distr_parameter.ConstValue != 0.0)
                    {
                        new_neg_exp_distr.Rate = 1.0 / distr_parameter.ConstValue;
                    }
                    else
                    {
                        new_neg_exp_distr.Rate= 1.0;
                    }
                    distr_parameter.ParamDistr = new_neg_exp_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.NEG_EXP;
                    break;

                case ParameterDistributionType.POISSON:
                    PoissonParameterDistribution new_poisson_distr = new PoissonParameterDistribution();
                    if (distr_parameter.ConstValue != 0.0)
                    {
                        new_poisson_distr.Mean = distr_parameter.ConstValue;
                    }
                    else
                    {
                        new_poisson_distr.Mean = 1.0;
                    }
                    distr_parameter.ParamDistr = new_poisson_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.POISSON;
                    break;

                case ParameterDistributionType.UNIFORM:
                    UniformParameterDistribution new_uniform_distr = new UniformParameterDistribution();
                    if (distr_parameter.ConstValue != 0.0)
                    {
                        new_uniform_distr.MinValue = distr_parameter.ConstValue;
                        new_uniform_distr.MaxValue = distr_parameter.ConstValue + 1.0;
                    }
                    else
                    {
                        new_uniform_distr.MinValue = 0.0;
                        new_uniform_distr.MaxValue = 1.0;
                    }
                    distr_parameter.ParamDistr = new_uniform_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.UNIFORM;
                    break;

                case ParameterDistributionType.WEIBULL:
                    WeibullParameterDistribution new_weibull_distr = new WeibullParameterDistribution();
                    if (distr_parameter.ConstValue != 0.0)
                    {
                        new_weibull_distr.Shape = 1.0;
                        new_weibull_distr.Scale = distr_parameter.ConstValue / MathNet.Numerics.SpecialFunctions.Gamma(2.0);
                    }
                    else
                    {
                        new_weibull_distr.Shape = 1.0;
                        new_weibull_distr.Scale = 1.0;
                    }
                    distr_parameter.ParamDistr = new_weibull_distr;
                    distr_parameter.DistributionType = ParameterDistributionType.WEIBULL;
                    break;


                default:
                    break;

            }

            detailsStackPanel.DataContext = null;
            detailsStackPanel.DataContext = distr_parameter;
        }

        private void dgProbMass_KeyDown(object sender, KeyEventArgs e)
        {
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;

            if (distr == null)
                return;

            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                string s = (string)Clipboard.GetData(DataFormats.Text);

                char[] delim = { '\t', '\r', '\n' };
                string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                distr.ProbMass.Clear();

                int n = 2 * (int)Math.Floor(paste.Length / 2.0);
                for (int i = 0; i < n; i += 2)
                {
                    distr.ProbMass.Add(new CategoricalDistrItem(double.Parse(paste[i]), double.Parse(paste[i + 1])));
                }

                distr.isInitialized = false;
            }
        }

        private void dgProbMass_Check(object sender, RoutedEventArgs e)
        {           
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            if (distrParam == null)
            {
                return;
            }

            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;
            if (distr == null)
            {
                return;
            }

            CategoricalDistrItem[] cdi = distr.ProbMass.ToArray();
            for (int i = 0; i < cdi.Count(); i++)
            {
                bool changed = false;

                if (cdi[i].CategoryValue < 0)
                {
                    cdi[i].CategoryValue = 0.0;
                    changed = true;
                }

                if (cdi[i].Prob < 0)
                {
                    cdi[i].Prob = 0.0;
                    changed = true;
                }

                if (changed == true)
                {
                    distr.ProbMass.Remove(cdi[i]);
                    distr.ProbMass.Add(cdi[i]);
                    distr.isInitialized = false;
                }
            }

            //distr.Normalize();
        }

        private void menuProbMassDelete_Click(object sender, RoutedEventArgs e)
        {
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;

            if (distr == null)
                return;

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;

            foreach (CategoricalDistrItem cdi in distr.ProbMass.ToList())
            {
                if (dataGrid.SelectedItems.Contains(cdi))
                {
                    distr.ProbMass.Remove(cdi);
                    distr.isInitialized = false;
                }
            }
        }

        private void menuProbMassNormalize_Click(object sender, RoutedEventArgs e)
        {
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;

            if (distr == null)
                return;

            distr.Normalize();
        }

        private void menuProbMassAdd_Click(object sender, RoutedEventArgs e)
        {
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;

            if (distr == null)
                return;

            distr.ProbMass.Add(new CategoricalDistrItem(0.0, 0.0));
            distr.isInitialized = false;
        }

        private void menuProbMassRefresh_Click(object sender, RoutedEventArgs e)
        {
            DistributedParameter distrParam = (DistributedParameter)paramDistrControl.DataContext;
            CategoricalParameterDistribution distr = (CategoricalParameterDistribution)distrParam.ParamDistr;

            if (distr == null)
                return;

            if (distr.ProbMass != null)
            {
                CategoricalDistrItem[] probMassArray = distr.ProbMass.ToArray();
                distr.ProbMass.Clear();
                foreach (CategoricalDistrItem cdi in probMassArray)
                {
                    distr.ProbMass.Add(cdi);
                }

                DataGrid dg = sender as DataGrid;
            }
        }

        //Needed this to update the selected distribution's details
        private void cbParamDistr_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string sTag = Tag as string;
            var comboBox = sender as ComboBox;
            ObservableCollection<ParameterDistributionType> coll = new ObservableCollection<ParameterDistributionType>();

            DistributedParameter dp = DataContext as DistributedParameter;
            ParameterDistributionType dtype = ParameterDistributionType.CONSTANT;

            if (dp != null)
            {
                dtype = dp.DistributionType;
            }

            switch (sTag)
            {
                case "DISCRETE":
                    coll.Add(ParameterDistributionType.CONSTANT);
                    coll.Add(ParameterDistributionType.POISSON);
                    coll.Add(ParameterDistributionType.CATEGORICAL);
                    break;
                case "CONTINUOUS":
                    coll.Add(ParameterDistributionType.CONSTANT);
                    coll.Add(ParameterDistributionType.GAMMA);
                    coll.Add(ParameterDistributionType.NEG_EXP);
                    coll.Add(ParameterDistributionType.UNIFORM);
                    coll.Add(ParameterDistributionType.WEIBULL);
                    break;
                default:
                    break;
            }

            if (coll.Count > 0)
            {
                comboBox.ItemsSource = coll;
                comboBox.SelectedItem = dtype;

                ParamDistrDetails.DataContext = null;
                ParamDistrDetails.DataContext = e.NewValue;
            }
        }

        private void cbParamDistr_Loaded(object sender, RoutedEventArgs e)
        {
            string sTag = Tag as string;
            var comboBox = sender as ComboBox;
            ObservableCollection<ParameterDistributionType> coll = new ObservableCollection<ParameterDistributionType>();

            DistributedParameter dp = DataContext as DistributedParameter;
            ParameterDistributionType dtype = ParameterDistributionType.CONSTANT;

            if (dp != null)
            {
                dtype = dp.DistributionType;
            }

            switch (sTag)
            {
                case "DISCRETE":
                    coll.Add(ParameterDistributionType.CONSTANT);
                    coll.Add(ParameterDistributionType.POISSON);
                    coll.Add(ParameterDistributionType.CATEGORICAL);
                    break;
                case "CONTINUOUS":
                    coll.Add(ParameterDistributionType.CONSTANT);
                    coll.Add(ParameterDistributionType.GAMMA);
                    coll.Add(ParameterDistributionType.NEG_EXP);
                    coll.Add(ParameterDistributionType.UNIFORM);
                    coll.Add(ParameterDistributionType.WEIBULL);
                    break;
                default:
                    break;
            }

            if (coll.Count > 0 && dp != null)
            {
                comboBox.ItemsSource = coll;
                comboBox.SelectedItem = dtype;
            }

        }

    }



#if ODP_METHOD_WORKS
    /// <summary>
    /// This class implements a custom method for ObjectDataProvider so that we can 
    /// retrieve different subsets of the Enum depending on who called this control.
    /// But it is not working. Tag is not set yet.
    /// </summary>
    public class CDataAccess
    {
        ObservableCollection<ParameterDistributionType> _DistCollection;

        public ObservableCollection<ParameterDistributionType> DistCollection
        {
            get { return _DistCollection; }
            set { _DistCollection = value; }
        }

        public CDataAccess()
        {
            _DistCollection = new ObservableCollection<ParameterDistributionType>();
        }

        public ObservableCollection<ParameterDistributionType> GetDistributions(string Tag)
        {
            switch (Tag)
            {
                case "DISCRETE":
                    DistCollection.Add(ParameterDistributionType.CONSTANT);
                    DistCollection.Add(ParameterDistributionType.POISSON);
                    DistCollection.Add(ParameterDistributionType.CATEGORICAL);
                    break;
                case "CONTINUOUS":
                    DistCollection.Add(ParameterDistributionType.CONSTANT);
                    DistCollection.Add(ParameterDistributionType.GAMMA);
                    DistCollection.Add(ParameterDistributionType.NEG_EXP);
                    DistCollection.Add(ParameterDistributionType.UNIFORM);
                    DistCollection.Add(ParameterDistributionType.WEIBULL); 
                    break;
                default:
                    break;
            }

            return DistCollection;
        }
    }
#endif

}
