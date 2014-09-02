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

        public class diffSchemeValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType,
                object parameter, CultureInfo culture)
            {
                return value;
            }

            public object ConvertBack(object value, Type targetType,
                object parameter, CultureInfo culture)
            {
                ConfigDiffScheme val = value as ConfigDiffScheme;
                if (val != null && val.Name == "") return null;
                return value;
            }
        }

        private void CellTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            cell.ValidateName(MainWindow.SOP.Protocol);
        }

        private void cbCellDiffSchemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Don't want to do anything when first display this combo box
            //Only do something if user really clicked and selected a different scheme

            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            //ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            ComboBox combo = sender as ComboBox;

            if (combo.SelectedIndex == -1)
                return;

            if (combo.SelectedIndex == 0)
            {
                cell.diff_scheme = null;
                combo.Text = "None";
            }
            else
            {
                ConfigDiffScheme diffNew = (ConfigDiffScheme)combo.SelectedItem;

                if (cell.diff_scheme != null && diffNew.entity_guid == cell.diff_scheme.entity_guid)
                {
                    return;
                }

                EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

                if (er.diff_schemes_dict.ContainsKey(diffNew.entity_guid) == true)
                {
                    cell.diff_scheme = er.diff_schemes_dict[diffNew.entity_guid].Clone(true);
                }
            }
            ////int nIndex = CellsListBox.SelectedIndex;
            ////CellsListBox.SelectedIndex = -1;
            ////CellsListBox.SelectedIndex = nIndex;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("diffSchemesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.diff_schemes;

            cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
        }
    }
}
