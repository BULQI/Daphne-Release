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
using System.Windows.Controls.Primitives;
using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for DiffSchemeDataGrid.xaml
    /// </summary>
    public partial class DiffSchemeDataGrid : UserControl
    {
        public DiffSchemeDataGrid()
        {
            InitializeComponent();
        }


        #region context_menus
        private void ContextMenuDeleteGenes_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;

            var dx = dataGrid.DataContext;

            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            foreach (DataGridTextColumn col in dataGrid.Columns.ToList())
            {
                bool isSelected = DataGridBehavior.GetHighlightColumn(col);
                string gene_name = col.Header as string;
                string guid = MainWindow.SOP.Protocol.findGeneGuid(gene_name, MainWindow.SOP.Protocol);
                if (isSelected && guid != null && guid.Length > 0)
                {
                    diff_scheme.genes.Remove(guid);
                    dataGrid.Columns.Remove(col);
                }
            }

        }

        private void ContextMenuDeleteStates_Click(object sender, RoutedEventArgs e)
        {

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            foreach (ConfigActivationRow diffrow in diff_scheme.activationRows.ToList())
            {
                if (dataGrid.SelectedItems.Contains(diffrow))
                {
                    int index = diff_scheme.activationRows.IndexOf(diffrow);
                    string stateToDelete = diff_scheme.Driver.states[index];

                    //this deletes the column from the differentiation regulators grid
                    //to do below.....
                    //DeleteDiffRegGridColumn(stateToDelete);

                    //this removes the activation row from the differentiation scheme
                    diff_scheme.RemoveActivationRow(diffrow);
                }
            }

            DiffSchemeDataGrid.update_datagrid_rowheaders(dataGrid);
            //update the reg grid
            DiffSchemeDataGrid.SetDiffSchemeSource(this.DivRegGrid, null);
            DiffSchemeDataGrid.SetDiffSchemeSource(this.DivRegGrid, diff_scheme);

        }

        private void ContextMenuAddState_Click(object sender, RoutedEventArgs e)
        {

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            //Show a dialog that gets the new state's name
            AddDiffState ads = new AddDiffState();
            if (ads.ShowDialog() != true) return;

            //DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            diff_scheme.AddState(ads.StateName);

            DiffSchemeDataGrid.SetDiffSchemeSource(dataGrid, null);
            DiffSchemeDataGrid.SetDiffSchemeSource(dataGrid, diff_scheme);
            DiffSchemeDataGrid.SetDiffSchemeSource(this.DivRegGrid, null);
            DiffSchemeDataGrid.SetDiffSchemeSource(this.DivRegGrid, diff_scheme);         
        }

        private void EpigeneticMapGrid_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: Add event handler implementation here.
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader) && !(dep is DataGridRowHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            else if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
                DataGridBehavior.SetHighlightColumn(columnHeader.Column, true);
            }

            else if (dep is DataGridRowHeader)
            {
            }

            else if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                // do something                
            }
        }

        private void EpigeneticMapGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion


        #region dynamic_datagrid generation

        /// <summary>
        /// ConfigTransitionScheme Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty DiffSchemeSourceProperty =
            DependencyProperty.RegisterAttached("DiffSchemeSource",
            typeof(ConfigTransitionScheme), typeof(DiffSchemeDataGrid),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnDiffSchemeChanged)));

        public static readonly DependencyProperty DiffSchemeTargetProperty =
            DependencyProperty.RegisterAttached("DiffSchemeTarget",
            typeof(string), typeof(DiffSchemeDataGrid),
            new FrameworkPropertyMetadata(null,
            null));

        /// <summary>
        /// Gets the DiffScheme property.  
        /// </summary>
        public static ConfigTransitionScheme GetDiffSchemeSource(DependencyObject d)
        {
            return (ConfigTransitionScheme)d.GetValue(DiffSchemeSourceProperty);
        }

        public static string GetDiffSchemeTarget(DependencyObject d)
        {
            return (string)d.GetValue(DiffSchemeTargetProperty);
        }

        /// <summary>
        /// Sets the MatrixSource property.  
        /// </summary>
        public static void SetDiffSchemeSource(DependencyObject d, ConfigTransitionScheme value)
        {
            d.SetValue(DiffSchemeSourceProperty, value);
        }

        public static void SetDiffSchemeTarget(DependencyObject d, string value)
        {
            d.SetValue(DiffSchemeTargetProperty, value);
        }

        /// <summary>
        /// Handles changes to the MatrixSource property.
        /// </summary>
        private static void OnDiffSchemeChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            DataGrid dataGrid = d as DataGrid;
            ConfigTransitionScheme diffScheme = e.NewValue as ConfigTransitionScheme;
            if (diffScheme == null) return;

            string DiffSchemeTarget = GetDiffSchemeTarget(dataGrid);

            //var tmp = FindLogicalParent<CellDetailsControl>(dataGrid);

            CellDetailsControl cdc = FindLogicalParent<CellDetailsControl>(dataGrid);
            if (DiffSchemeTarget == "EpigeneticMap")
            {
                Binding b1 = new Binding("activationRows") { Source = diffScheme };
                b1.Mode = BindingMode.TwoWay;
                dataGrid.SetBinding(DataGrid.ItemsSourceProperty, b1);

                //dataGrid.ItemsSource = diffScheme.activationRows;

                int count = 0;
                dataGrid.Columns.Clear();
                foreach (var gene_guid in diffScheme.genes)
                {
                    if (!er.genes_dict.ContainsKey(gene_guid))
                        continue;

                    ConfigGene gene = er.genes_dict[gene_guid];

                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = gene.Name;
                    col.CanUserSort = false;
                    Binding b = new Binding(string.Format("activations[{0}]", count));
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    col.Binding = b;
                    dataGrid.Columns.Add(col);
                    count++;
                }

                DataGridTextColumn combobox_col = cdc.CreateUnusedGenesColumn();
                dataGrid.Columns.Add(combobox_col);
            }
            else
            {
                dataGrid.ItemsSource = diffScheme.Driver.DriverElements;
                int count = 0;
                dataGrid.Columns.Clear();

                foreach (string s in diffScheme.Driver.states)
                {
                    DataGridTemplateColumn col = new DataGridTemplateColumn();

                    //column header binding
                    Binding hb = new Binding(string.Format("states[{0}]", count));
                    hb.Mode = BindingMode.OneWay;
                    hb.Source = diffScheme.Driver;
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
                    txtStateName.SetValue(TextBlock.StyleProperty, null);
                    //txtStateName.SetValue(TextBlock.DataContextProperty, cell.diff_scheme.Driver);
                    txtStateName.SetBinding(TextBlock.TextProperty, hb);
                    col.HeaderTemplate = new DataTemplate() { VisualTree = txtStateName };

                    col.CanUserSort = false;
                    Binding b = new Binding(string.Format("elements[{0}]", count));

                    var cellTemplate = cdc.FindResource("DataGridCell_TDE_NonEditing_Template");
                    FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ContentPresenter));
                    factory.SetValue(ContentPresenter.ContentTemplateProperty, cellTemplate);
                    factory.SetBinding(ContentPresenter.ContentProperty, b);
                    col.CellTemplate = new DataTemplate { VisualTree = factory };

                    //editing template
                    Binding b2 = new Binding(string.Format("elements[{0}]", count));
                    b2.Mode = BindingMode.TwoWay;
                    b2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    var cellEditingTemplate = cdc.FindResource("DataGridCell_TDE_Editing_Template");
                    FrameworkElementFactory factory2 = new FrameworkElementFactory(typeof(ContentPresenter));
                    factory2.SetValue(ContentPresenter.ContentTemplateProperty, cellEditingTemplate);
                    factory2.SetBinding(ContentPresenter.ContentProperty, b2);
                    col.CellEditingTemplate = new DataTemplate { VisualTree = factory2 };

                    dataGrid.Columns.Add(col);
                    count++;
                }

                dataGrid.CellEditEnding -= new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
                dataGrid.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
            }

            dataGrid.LoadingRow -= new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            dataGrid.LoadingRow += new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            dataGrid.TargetUpdated -= new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
            dataGrid.TargetUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);

        }

        static void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            foreach (DataGridColumn col in dg.Columns)
            {
                col.Width = DataGridLength.SizeToCells;
                col.Width = DataGridLength.Auto;
            }
        }

        static void dataGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            var diffScheme = GetDiffSchemeSource(dg);
            dg.RowHeaderWidth = 0;
            dg.RowHeaderWidth = Double.NaN;
        }

        private static void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            DataGrid dataGrid = sender as DataGrid;
            var diffScheme = GetDiffSchemeSource(dataGrid);
            if (diffScheme == null) return;
            int index = e.Row.GetIndex();
            if (index < diffScheme.Driver.states.Count)
            {
                //e.Row.Header = context.RowHeaders[index];
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = diffScheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }
        }

        public static void update_datagrid_rowheaders(DataGrid datagrid)
        {
            var diffScheme = GetDiffSchemeSource(datagrid);
            for (int i = 0; i < diffScheme.Driver.states.Count; i++)
            {
                DataGridRow row = (DataGridRow)datagrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = diffScheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", i));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                row.Header = dgr;
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }

        public static T FindLogicalParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = LogicalTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindLogicalParent<T>(parentObject);
            }
        }

        #endregion
    }


    #region value converters
    public class DataGridRowColumnIndexEqualValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 2) return true;

            DataGridRow row = values[0] as DataGridRow;
            int row_index = row.GetIndex();
            DataGridTemplateColumn col = values[1] as DataGridTemplateColumn;
            int col_index = col.DisplayIndex;
            return row_index == col_index;
        }


        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    #endregion
}
