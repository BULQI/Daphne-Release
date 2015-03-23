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
using System.Collections.Specialized;
using System.Reflection;
using System.Collections.ObjectModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for DiffSchemeReadOnlyDataGrid.xaml
    /// </summary>
    public partial class DiffSchemeReadOnlyDataGrid : UserControl
    {
        public DiffSchemeReadOnlyDataGrid()
        {
            InitializeComponent();
        }

        #region mouse_events
        
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
        public static readonly DependencyProperty RDiffSchemeSourceProperty =
            DependencyProperty.RegisterAttached("RDiffSchemeSource",
            typeof(ConfigTransitionScheme), typeof(DiffSchemeReadOnlyDataGrid),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnRDiffSchemeChanged)));

        public static readonly DependencyProperty RDiffSchemeTargetProperty =
            DependencyProperty.RegisterAttached("RDiffSchemeTarget",
            typeof(string), typeof(DiffSchemeReadOnlyDataGrid),
            new FrameworkPropertyMetadata(null,
            null));

        public static readonly DependencyProperty RGeneListProperty =
            DependencyProperty.RegisterAttached("RGeneList",
            typeof(ObservableCollection<string>), typeof(DiffSchemeReadOnlyDataGrid),
                new FrameworkPropertyMetadata(null,
                new PropertyChangedCallback(OnRGeneListChanged)));

        public static readonly DependencyProperty RStateListProperty =
            DependencyProperty.RegisterAttached("RStateList",
            typeof(ObservableCollection<string>), typeof(DiffSchemeReadOnlyDataGrid),
            new FrameworkPropertyMetadata(null,
            new PropertyChangedCallback(OnRStateListChanged)));

        /// <summary>
        /// Gets the DiffScheme property.  
        /// </summary>
        public static ConfigTransitionScheme GetRDiffSchemeSource(DependencyObject d)
        {
            return (ConfigTransitionScheme)d.GetValue(RDiffSchemeSourceProperty);
        }

        public static string GetRDiffSchemeTarget(DependencyObject d)
        {
            return (string)d.GetValue(RDiffSchemeTargetProperty);
        }

        /// <summary>
        /// Sets the MatrixSource property.  
        /// </summary>
        public static void SetRDiffSchemeSource(DependencyObject d, ConfigTransitionScheme value)
        {
            d.SetValue(RDiffSchemeSourceProperty, value);
        }

        public static void SetRDiffSchemeTarget(DependencyObject d, string value)
        {
            d.SetValue(RDiffSchemeTargetProperty, value);
        }


        public static ObservableCollection<string> GetGeneList(DependencyObject d)
        {
            return (ObservableCollection<string>)d.GetValue(RGeneListProperty);
        }

        public static void SetRGeneList(DependencyObject d, ObservableCollection<string> value)
        {
            d.SetValue(RGeneListProperty, value);
        }

        public static ObservableCollection<string> GetRStateList(DependencyObject d)
        {
            return (ObservableCollection<string>)d.GetValue(RStateListProperty);
        }

        public static void SetRStateList(DependencyObject d, ObservableCollection<string> value)
        {
            d.SetValue(RStateListProperty, value);
        }

        private static void CreateGeneColumns(DataGrid dataGrid, ObservableCollection<string> genes)
        {
            
            dataGrid.Columns.Clear();
            if (genes == null)return;

            CellDetailsReadOnlyControl cdc = FindLogicalParent<CellDetailsReadOnlyControl>(dataGrid);
            Level level = MainWindow.GetLevelContext(cdc);

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            
            //Level level = MainWindow.GetLevelContext(cdc);            
            //EntityRepository er = level.entity_repository;

            //create columns
            int count = 0;
            foreach (var gene_guid in genes)
            {
                if (!er.genes_dict.ContainsKey(gene_guid))
                    continue;
                ConfigGene gene = er.genes_dict[gene_guid];

                DataGridTextColumn col = new DataGridTextColumn();
                col.IsReadOnly = true;
                col.Header = gene.Name;
                col.CanUserSort = false;
                Binding b = new Binding(string.Format("activations[{0}]", count));
                b.Mode = BindingMode.TwoWay;
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                col.Binding = b;
                dataGrid.Columns.Add(col);
                count++;
            }
            ConfigTransitionScheme currentScheme = GetRDiffSchemeSource(dataGrid);

            //////FOR NOW DON'T CREATE THE LAST COLUMN
            return;

            DataGridTextColumn combobox_col = cdc.CreateUnusedGenesColumn(currentScheme);
            dataGrid.Columns.Add(combobox_col);
        }

        private static void CreateStateColumns(DataGrid dataGrid, ObservableCollection<string> states)
        {
            foreach (var item in dataGrid.Columns)
            {
                var col = item as DataGridTemplateColumn;
                if (col == null)continue;
                var dt = col.HeaderTemplate as DataTemplate;
                if (dt != null)
                {
                    var textBlock = dt.VisualTree;
                    
                }

            }

            dataGrid.Columns.Clear();
            if (states == null || states.Count == 0) return;

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            ////Level level = MainWindow.GetLevelContext(dataGrid);
            ////EntityRepository er = level.entity_repository;

            CellDetailsReadOnlyControl cdc = FindLogicalParent<CellDetailsReadOnlyControl>(dataGrid);

            int count = 0;
            ConfigTransitionScheme diffScheme = GetRDiffSchemeSource(dataGrid);
            if (diffScheme == null) return;
            foreach (string s in states)
            {
                DataGridTemplateColumn col = new DataGridTemplateColumn();
                //column header binding
                //Binding hb = new Binding(string.Format("states[{0}]", count));
                //hb.Mode = BindingMode.OneWay;
                //hb.Source = diffScheme.Driver;
                //FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
                //txtStateName.SetValue(TextBlock.StyleProperty, null);
                ////txtStateName.SetValue(TextBlock.DataContextProperty, cell.diff_scheme.Driver);
                //txtStateName.SetBinding(TextBlock.TextProperty, hb);
                //col.HeaderTemplate = new DataTemplate() { VisualTree = txtStateName };

                col.Header = states[count];


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
        }

        private static void OnRGeneListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = d as DataGrid;
            ObservableCollection<string> genes = e.NewValue as ObservableCollection<string>;

            dataGrid.Columns.Clear();
            if (genes == null) return;

            CreateGeneColumns(dataGrid, genes);
            genes.CollectionChanged += (sender, e2) =>
            {
                genes = sender as ObservableCollection<string>;
                CreateGeneColumns(dataGrid, genes);
            };
        }

        private static void OnRStateListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = d as DataGrid;
            ObservableCollection<string> states = e.NewValue as ObservableCollection<string>;

            dataGrid.Columns.Clear();
            if (states == null) return;

            CreateStateColumns(dataGrid, states);
            states.CollectionChanged += (sender, e2) =>
            {
                states = sender as ObservableCollection<string>;
                CreateStateColumns(dataGrid, states);
            };
        }

        /// <summary>
        /// Handles changes to the MatrixSource property.
        /// </summary>
        private static void OnRDiffSchemeChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {

            DataGrid dataGrid = d as DataGrid;
            string DiffSchemeTarget = GetRDiffSchemeTarget(dataGrid);

            ConfigTransitionScheme diffScheme = e.NewValue as ConfigTransitionScheme;
            if (diffScheme == null) return;

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            CellDetailsReadOnlyControl cdc = FindLogicalParent<CellDetailsReadOnlyControl>(dataGrid);
            if (DiffSchemeTarget == "EpigeneticMap")
            {
                //CreateGeneColumns(dataGrid, diffScheme.genes);
            }
            else
            {
                //CreateStateColumns(dataGrid, diffScheme.Driver.states);
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
            var diffScheme = GetRDiffSchemeSource(dg);
            dg.RowHeaderWidth = 0;
            dg.RowHeaderWidth = Double.NaN;
        }

        private static void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            DataGrid dataGrid = sender as DataGrid;
            var diffScheme = GetRDiffSchemeSource(dataGrid);
            string DiffSchemeTarget = GetRDiffSchemeTarget(dataGrid);
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
            var diffScheme = GetRDiffSchemeSource(datagrid);
            string DiffSchemeTarget = GetRDiffSchemeTarget(datagrid);

            for (int i = 0; i < diffScheme.Driver.states.Count; i++)
            {
                DataGridRow row = (DataGridRow)datagrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                DataGridRowHeader dgr = new DataGridRowHeader();
                //dgr.Content = diffScheme.Driver.states[i];

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

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var diff_scheme = DiffSchemeReadOnlyDataGrid.GetRDiffSchemeSource(EpigeneticMapGridDiv);
            int x = 0;
            x++;            
        }
    }


    #region value converters
    ////public class DataGridRowColumnIndexEqualValueConverter : IMultiValueConverter
    ////{
    ////    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    ////    {
    ////        if (values == null || values.Length < 2) return true;

    ////        DataGridRow row = values[0] as DataGridRow;
    ////        int row_index = row.GetIndex();
    ////        DataGridTemplateColumn col = values[1] as DataGridTemplateColumn;
    ////        int col_index = col.DisplayIndex;
    ////        return row_index == col_index;
    ////    }


    ////    public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
    ////    {
    ////        return null;
    ////    }

    ////}

    #endregion
}
