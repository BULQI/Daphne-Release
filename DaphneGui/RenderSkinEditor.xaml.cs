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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;


namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for RenderSkinEditor.xaml
    /// </summary>
    public partial class RenderSkinEditor : UserControl
    {

        public RenderSkinEditor()
        {
            InitializeComponent();
        }


        public RenderCellEx renderCellEx
        {
            get 
            {
                return GetRenderCellExSource(this);
            }
            set
            {
                SetRenderCellExSource(this, value);
            }
        }

        public DataGrid RenderColorHost
        {
            get
            {
                return this.rendercelldg;
            }
        }

        /// <summary>
        /// ConfigTransitionScheme Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty RenderCellSourceProperty =
            DependencyProperty.RegisterAttached("RenderCellSource",
            typeof(RenderCell), typeof(RenderSkinEditor),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnRenderCellSourceChanged)
                    ));

        public static RenderCell GetRenderCellSource(DependencyObject d)
        {
            return (RenderCell)d.GetValue(RenderCellSourceProperty);
        }

        /// <summary>
        /// Sets the MatrixSource property.  
        /// </summary>
        public static void SetRenderCellSource(DependencyObject d, RenderCell value)
        {
            d.SetValue(RenderCellSourceProperty, value);
        }


        public static readonly DependencyProperty RenderCellSourceExProperty =
                DependencyProperty.RegisterAttached("RenderCellSourceEx",
                typeof(RenderCellEx), typeof(RenderSkinEditor),
                new FrameworkPropertyMetadata(null)
            );

        public static RenderCellEx GetRenderCellExSource(DependencyObject d)
        {
            return (RenderCellEx)d.GetValue(RenderCellSourceExProperty);
        }

        /// <summary>
        /// Sets the MatrixSource property.  
        /// </summary>
        public static void SetRenderCellExSource(DependencyObject d, RenderCellEx value)
        {
            d.SetValue(RenderCellSourceExProperty, value);
        }


        /// <summary>
        /// Handles changes to the MatrixSource property.
        /// </summary>
        private static void OnRenderCellSourceChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {

            DataGrid dataGrid = d as DataGrid;
            RenderCell renderCell = e.NewValue as RenderCell;
            var render_datagrid = DiffSchemeDataGrid.FindLogicalParent<RenderSkinEditor>(dataGrid);
            if (renderCell == null)
            {
                render_datagrid.renderCellEx = null;
                dataGrid.Columns.Clear();
                dataGrid.ItemsSource = null;
                return;
            }

            RenderCellEx rcx = new RenderCellEx(renderCell);
            render_datagrid.renderCellEx = rcx;

            dataGrid.ItemsSource = rcx.renderCollection;
            dataGrid.Columns.Clear();

            RenderSkinWindow rsw = DiffSchemeDataGrid.FindLogicalParent<RenderSkinWindow>(dataGrid);

            for (int i=0; i< rcx.ColumnHeader.Count; i++)
            {
                DataGridTemplateColumn col = new DataGridTemplateColumn();
                col.Header = rcx.ColumnHeader[i];

                col.CanUserSort = false;

                Binding b = new Binding(string.Format("[{0}]", i));
                var cellTemplate = rsw.FindResource("RenderColorCellTemplate");
                FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ContentPresenter));
                factory.SetValue(ContentPresenter.ContentTemplateProperty, cellTemplate);
                factory.SetBinding(ContentPresenter.ContentProperty, b);
                col.CellTemplate = new DataTemplate { VisualTree = factory };

                //editing template
                Binding b2 = new Binding(string.Format("[{0}]", i));
                b2.Mode = BindingMode.TwoWay;
                b2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                var cellEditingTemplate = rsw.FindResource("RenderColorEditingTemplate");
                FrameworkElementFactory factory2 = new FrameworkElementFactory(typeof(ContentPresenter));
                factory2.SetValue(ContentPresenter.ContentTemplateProperty, cellEditingTemplate);
                factory2.SetBinding(ContentPresenter.ContentProperty, b2);
                col.CellEditingTemplate = new DataTemplate { VisualTree = factory2 };

                dataGrid.Columns.Add(col);
            }

            dataGrid.CellEditEnding -= new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
            dataGrid.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
            dataGrid.LoadingRow -= new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            dataGrid.LoadingRow += new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            //dataGrid.SourceUpdated -= new EventHandler<DataTransferEventArgs>(dataGrid_SourceUpdated);
            //dataGrid.SourceUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_SourceUpdated);
            //dataGrid.TargetUpdated -= new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
            //dataGrid.TargetUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
        }


        //static void dataGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        //{
        //    DataGrid dg = (DataGrid)sender;
        //}
        /// <summary>
        /// called when the underline color is updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //static void dataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        private static void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            DataGrid dataGrid = sender as DataGrid;
            var render_datagrid = DiffSchemeDataGrid.FindLogicalParent<RenderSkinEditor>(dataGrid);
            RenderCellEx rce = GetRenderCellExSource(render_datagrid);
            if (rce == null) return;
            int index = e.Row.GetIndex();
            if (index < rce.RowHeaders.Count)
            {
                //e.Row.Header = context.RowHeaders[index];
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext =rce.RowHeaders;
                Binding binding = new Binding(string.Format("[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }
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

        private void rendercelldg_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            DataGridCellInfo dc = dg.CurrentCell;
            if (dc != null)
            {
                var item = dc.Item as ObservableCollection<RenderColor>;
                int col_index = dc.Column.DisplayIndex;
                if (col_index >= item.Count || item[col_index] == null)
                {
                    e.Cancel = true;
                    //dg.CancelEdit();
                }
            }
        }

        private void SolidColorRowDetail_Checked(object sender, RoutedEventArgs e)
        {

            var listbox = sender as ListBox;
            if (listbox == null || listbox.SelectedItem == null) return;

            List<string> src_colors = listbox.SelectedItem as List<string>;
            ObservableCollection<RenderColor> rc_list = listbox.DataContext as ObservableCollection<RenderColor>;

            if (rc_list == null) return;
            for (int i = 1, j = 0; i < src_colors.Count && j < rc_list.Count; i++, j++)
            {
                if (rc_list[j] == null) break;
                Color c = (Color)ColorConverter.ConvertFromString(src_colors[i]);
                rc_list[j].EntityColor = c;
            }
        }

        private void Dismiss_Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            var row = DiffSchemeDataGrid.FindVisualParent<DataGridRow>(btn);
            if (row == null) return;
            row.DetailsVisibility = System.Windows.Visibility.Collapsed;
        }
    }

    public class RenderCellEx
    {

        private RenderCell _renderCell;

        public ObservableCollection<ObservableCollection<RenderColor>> renderCollection;

        public ObservableCollection<string> RowHeaders { get; set; }

        public ObservableCollection<string> ColumnHeader { get; set; }

        public RenderCellEx(RenderCell rcell)
        {
            _renderCell = rcell;
            renderCollection = new ObservableCollection<ObservableCollection<RenderColor>>();
            RowHeaders = new ObservableCollection<string>();
            ColumnHeader = new ObservableCollection<string>();

            ObservableCollection<RenderColor> basecolor = new ObservableCollection<RenderColor>();
            basecolor.Add(rcell.base_color);
            renderCollection.Add(basecolor);
            RowHeaders.Add("Base Color");

            //renderCollection.Add(rcell.cell_pop_colors);
            ObservableCollection<RenderColor> tmp_collection = new ObservableCollection<RenderColor>(rcell.cell_pop_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Population");


            //renderCollection.Add(rcell.div_state_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.div_state_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Division states");

            tmp_collection = new ObservableCollection<RenderColor>(rcell.div_shade_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Division state Shade");


            //renderCollection.Add(rcell.diff_state_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.diff_state_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Differentiation States");

            tmp_collection = new ObservableCollection<RenderColor>(rcell.diff_shade_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Differentiation State Shade");

            //renderCollection.Add(rcell.death_state_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.death_state_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Death States");

            tmp_collection = new ObservableCollection<RenderColor>(rcell.death_shade_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Death State Shade");

            //renderCollection.Add(rcell.gen_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.gen_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Generation");

            //////shading
            //tmp_collection = new ObservableCollection<RenderColor>(rcell.div_shade_colors);
            //renderCollection.Add(tmp_collection);
            //RowHeaders.Add("Division state Shade");

            //tmp_collection = new ObservableCollection<RenderColor>(rcell.diff_shade_colors);
            //renderCollection.Add(tmp_collection);
            //RowHeaders.Add("Differentiation State Shade");

            //tmp_collection = new ObservableCollection<RenderColor>(rcell.death_shade_colors);
            //renderCollection.Add(tmp_collection);
            //RowHeaders.Add("Death State Shade");

            //renderCollection.Add(rcell.gen_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.gen_shade_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Generation Shade");


            int nColumn = renderCollection.Max(x => x.Count);
            for (int i = 0; i < nColumn; i++)
            {
                ColumnHeader.Add(string.Format("Color {0}", i+1));
            }

            for (int i = 0; i < renderCollection.Count; i++)
            {
                var collection = renderCollection[i];
                for (int j = collection.Count; j < nColumn; j++)
                {
                    //RenderColor rc = new RenderColor(Colors.Gray);
                    //collection.Add(rc);
                    collection.Add(null);
                }
            }
        }
    }


    public class ColorItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate ColorBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string itemstr = item as string;
            if (itemstr.StartsWith("#")) return ColorBoxTemplate;
            return CheckBoxTemplate;
        }
    }

    public class ColorGridRowDetailTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SolidColorListTemplate { get; set; }
        public DataTemplate GenerationColorListTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var collection = item as ObservableCollection<RenderColor>;
            if (item != null)
            {
                int color_count = 0;
                for (int i = 0; i < collection.Count; i++)
                {
                    if (collection[i] != null) color_count++;
                }

                return color_count == 12 ? GenerationColorListTemplate : SolidColorListTemplate;
            }
            return SolidColorListTemplate;

        }
    }

    public class RenderColorRowDetailMultiConverter : IMultiValueConverter
    { 
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            double len1 = (double)values[0];
            double len2 = (double)values[1];

            DataGridRow row = values[2] as DataGridRow;
            var t = row.ActualHeight;

            return len1 - len2;
        }


        /// <summary>
        /// given index, return color
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
