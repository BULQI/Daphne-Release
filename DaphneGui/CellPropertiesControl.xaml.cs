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
using System.Globalization;

using System.Collections.ObjectModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellPropertiesControl.xaml
    /// </summary>
    public partial class CellPropertiesControl : UserControl
    {
        public CellPropertiesControl()
        {
            InitializeComponent();
        }

        private void CellTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            Level level = MainWindow.GetLevelContext(this);
            cell.ValidateName(level);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            if (cvs.Source == null)
            {
                cvs.Source = new ObservableCollection<ConfigMolecule>();
            }

            ((ObservableCollection<ConfigMolecule>)cvs.Source).Clear();

            ConfigCell cell = DataContext as ConfigCell;    
            if (cell == null)
            {
                return;
            }

            foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            {
                ((ObservableCollection<ConfigMolecule>)cvs.Source).Add(configMolpop.molecule);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CellPropertiesGrid.IsEnabled = true;

            // Tag is set in PushBetweenLevels.xaml when pushing cells
            if (Tag == null)
                return;

            // Turn off edit capability when pushing cells between stores
            string temp = Tag.ToString().ToLower();
            if (temp == "false")
                CellPropertiesGrid.IsEnabled = false;
        }

        protected virtual void PushCellButton_Click2(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            ConfigCell cell = button.DataContext as ConfigCell;

            if (cell == null)
                return;

            //Push cell
            ConfigCell newcell = cell.Clone(true);
            MainWindow.GenericPush(newcell);
        }

    }

    public class RadiusValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, "Radius value cannot be empty.");
            else
            {                
                string strValue = value.ToString();
                strValue = strValue.Trim();
                if (strValue.Length <= 0)
                    return new ValidationResult(false, "Radius value cannot be blank.");

                double dValue;
                bool result = double.TryParse(strValue, out dValue);
                if (result == false)
                    return new ValidationResult(false, "Invalid Radius value entered.");

                //dValue = (double)value;
                if (dValue <= 0)
                    return new ValidationResult(false, "Radius must be greater than 0.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
