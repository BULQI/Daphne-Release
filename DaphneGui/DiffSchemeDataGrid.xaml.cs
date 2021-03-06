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
using System.Windows.Controls.Primitives;
using Daphne;
using System.Collections.Specialized;
using System.Reflection;
using System.Collections.ObjectModel;

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
            Level level = MainWindow.GetLevelContext(this);

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            foreach (DataGridTextColumn col in dataGrid.Columns.ToList())
            {
                bool isSelected = DataGridBehavior.GetHighlightColumn(col);
                string gene_name = col.Header as string;
                string guid = level.findGeneGuid(gene_name, level);
                if (isSelected && guid != null && guid.Length > 0)
                {
                    //diff_scheme.genes.Remove(guid);
                    diff_scheme.DeleteGene(guid);
                }
            }
                    
        }

        private void ContextMenuDeleteStates_Click(object sender, RoutedEventArgs e)
        {

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            List<int> rowsToDelete = new List<int>();   //will contain a list of row indices to delete (ascending order)
            foreach (var n in dataGrid.SelectedItems)
            {
                var currentRowIndex = dataGrid.Items.IndexOf(n);
                rowsToDelete.Add(currentRowIndex);
            }

            //Reverse the order to make it easy to delete states from diff_scheme
            rowsToDelete.Sort();
            rowsToDelete.Reverse();
            

            foreach (int i in rowsToDelete)
            {
                //Delete state from diff_scheme - the order of rows matches the order in diff_scheme
                diff_scheme.DeleteState(i);
            }

            //Update row headers in both grids
            DiffSchemeDataGrid.update_datagrid_rowheaders(dataGrid);
            DiffSchemeDataGrid.update_datagrid_rowheaders(this.DivRegGrid);
        }

        private void ContextMenuInsertStateAbove_Click(object sender, RoutedEventArgs e)
        {
            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);

            if (diff_scheme == null)
                return;

            if (dataGrid.SelectedIndex < -1)
                return;

            DiffSchemeDataGrid diffSchemeDG = FindLogicalParent<DiffSchemeDataGrid>(dataGrid);
            if (diffSchemeDG == null)
                return;

            if (dataGrid.SelectedIndex < 0)
                return;

            // These next two statements are needed to prevent a crash during the Refresh operations, below.
            // The crash occurs when the user is still in editing mode in a cell and the Refresh method is called.
            // This is a known bug and fix.
            dataGrid.CommitEdit();
            dataGrid.CommitEdit();
            diffSchemeDG.DivRegGrid.CommitEdit();
            diffSchemeDG.DivRegGrid.CommitEdit();

            string stateName = diff_scheme.GenerateStateName();
            diff_scheme.InsertState(stateName, dataGrid.SelectedIndex);

            CollectionViewSource.GetDefaultView(diffSchemeDG.EpigeneticMapGridDiv.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(diffSchemeDG.DivRegGrid.ItemsSource).Refresh();
        }

        private void ContextMenuInsertStateBelow_Click(object sender, RoutedEventArgs e)
        {
            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);

            if (diff_scheme == null)
                return;

            if (dataGrid.SelectedIndex < -1)
                return;

            DiffSchemeDataGrid diffSchemeDG = FindLogicalParent<DiffSchemeDataGrid>(dataGrid);
            if (diffSchemeDG == null)
                return;

            // Only allow insert before the last, cytokinetic, state of a division scheme
            if (diffSchemeDG.Name == "DivSchemeGrid" && dataGrid.SelectedIndex == diff_scheme.Driver.states.Count - 1)
            {
                System.Windows.MessageBox.Show("Cannot add a state after the cytokinetic state in a cell division scheme.");
                return;
            }

            // These next two statements are needed to prevent a crash during the Refresh operations, below.
            // The crash occurs when the user is still in editing mode in a cell and the Refresh method is called.
            // This is a known bug and fix and the duplicates are necessary.
            dataGrid.CommitEdit();
            dataGrid.CommitEdit();
            diffSchemeDG.DivRegGrid.CommitEdit();
            diffSchemeDG.DivRegGrid.CommitEdit();

            string stateName = diff_scheme.GenerateStateName();
            diff_scheme.InsertState(stateName, dataGrid.SelectedIndex + 1);

            CollectionViewSource.GetDefaultView(diffSchemeDG.EpigeneticMapGridDiv.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(diffSchemeDG.DivRegGrid.ItemsSource).Refresh();
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

        /// <summary>
        /// This method is for drag-and-drop functionality. It allows the user to move a state up or down
        /// in the epigenetic map grid. The corresponding changes happen in the regulators grid.
        /// Remember that any time the rows are added/deleted/moved in the epigenetic grid, you must
        /// call the update_datagrid_rowheaders method for both grids.
        /// </summary>
        private ConfigActivationRow DraggedItem;
        private ConfigActivationRow TargetItem;

        private void EpigeneticGrid_MouseMove(object sender, MouseEventArgs e)
        {
            DataGrid dgSource = sender as DataGrid;
            if (dgSource.SelectedIndex < 0)
                return;

            ConfigActivationRow row = dgSource.SelectedItem as ConfigActivationRow;
            DraggedItem = row;

            if (row != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(dgSource,
                             row,
                             DragDropEffects.Copy);

                if (TargetItem == null)
                    return;

                ConfigTransitionScheme scheme = DataContext as ConfigTransitionScheme;

                //Get indices of the dragged and target rows
                var draggedIndex = scheme.activationRows.IndexOf(DraggedItem);
                var targetIndex = scheme.activationRows.IndexOf(TargetItem);

                

                //Call Move method
                scheme.MoveState(draggedIndex, targetIndex);
                dgSource.SelectedIndex = targetIndex;

                //Update row headers in both grids
                DiffSchemeDataGrid.update_datagrid_rowheaders(dgSource);
                DiffSchemeDataGrid.update_datagrid_rowheaders(this.DivRegGrid);
            }
        }

        /// <summary>
        /// This method gets the item at the destination row of the dragged item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EpigeneticGrid_Drop(object sender, DragEventArgs e)
        {
            TargetItem = null;

            DataGrid dgSource = sender as DataGrid;
            if (dgSource.SelectedIndex < 0)
                return;

            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            
            if (dep is DataGridRow)
            {
                TargetItem = (ConfigActivationRow)((dep as DataGridRow).Item);
            }
        }


        ///************************************

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

        public static readonly DependencyProperty GeneListProperty =
            DependencyProperty.RegisterAttached("GeneList",
            typeof(ObservableCollection<string>), typeof(DiffSchemeDataGrid),
                new FrameworkPropertyMetadata(null,
                new PropertyChangedCallback(OnGeneListChanged)));

        public static readonly DependencyProperty StateListProperty =
            DependencyProperty.RegisterAttached("StateList",
            typeof(ObservableCollection<string>), typeof(DiffSchemeDataGrid),
            new FrameworkPropertyMetadata(null,
            new PropertyChangedCallback(OnStateListChanged)));

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


        public static ObservableCollection<string> GetGeneList(DependencyObject d)
        {
            return (ObservableCollection<string>)d.GetValue(GeneListProperty);
        }

        public static void SetGeneList(DependencyObject d, ObservableCollection<string> value)
        {
            d.SetValue(GeneListProperty, value);
        }

        public static ObservableCollection<string> GetStateList(DependencyObject d)
        {
            return (ObservableCollection<string>)d.GetValue(StateListProperty);
        }

        public static void SetStateList(DependencyObject d, ObservableCollection<string> value)
        {
            d.SetValue(StateListProperty, value);
        }

        private static void CreateGeneColumns(DataGrid dataGrid, ObservableCollection<string> genes)
        {
            dataGrid.Columns.Clear();
            if (genes == null)return;
            
            CellDetailsControl cdc = FindLogicalParent<CellDetailsControl>(dataGrid);
            Level level = MainWindow.GetLevelContext(cdc);
            if (level == null)
            {
                return;
            } 
            EntityRepository er = level.entity_repository;

            //create columns
            int count = 0;
            foreach (var gene_guid in genes)
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
            ConfigTransitionScheme currentScheme = GetDiffSchemeSource(dataGrid);
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

            Level level = MainWindow.GetLevelContext(dataGrid);
            EntityRepository er = level.entity_repository;

            CellDetailsControl cdc = FindLogicalParent<CellDetailsControl>(dataGrid);

            int count = 0;
            ConfigTransitionScheme diffScheme = GetDiffSchemeSource(dataGrid);
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

        private static void OnGeneListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

        private static void OnStateListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        private static void OnDiffSchemeChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {

            DataGrid dataGrid = d as DataGrid;
            string DiffSchemeTarget = GetDiffSchemeTarget(dataGrid);

            ConfigTransitionScheme diffScheme = e.NewValue as ConfigTransitionScheme;
            if (diffScheme == null) return;

            CellDetailsControl cdc = FindLogicalParent<CellDetailsControl>(dataGrid);
            if (DiffSchemeTarget == "EpigeneticMap")
            {               
            }
            else
            {
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
            string DiffSchemeTarget = GetDiffSchemeTarget(dataGrid);
            if (diffScheme == null) return;
            int index = e.Row.GetIndex();

            //Enable all items (rows) - later disable cytokinetic row.
            for (int i = 0; i < diffScheme.Driver.states.Count; i++)
            {
                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                row.IsEnabled = true;
            }

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

                //This section is very important for disabling cytokinetic rows in both grids
                string sname = diffScheme.Driver.states[index];
                if (dataGrid.Name == "EpigeneticMapGridDiv" && sname == "cytokinetic")
                {
                    e.Row.IsEnabled = false;
                }
                if (dataGrid.Name == "DivRegGrid" && sname == "cytokinetic")
                {
                    e.Row.IsEnabled = false;
                }
            }

        }

        public static void update_datagrid_rowheaders(DataGrid datagrid)
        {
            var diffScheme = GetDiffSchemeSource(datagrid);
            string DiffSchemeTarget = GetDiffSchemeTarget(datagrid);

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
