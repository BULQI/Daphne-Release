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

        /// <summary>
        /// ConfigDiffScheme Attached Dependency Property
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
            //dataGrid.TargetUpdated -= new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
            //dataGrid.TargetUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
        }

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

            //renderCollection.Add(rcell.diff_state_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.diff_state_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Differentiation States");



            //renderCollection.Add(rcell.death_state_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.death_state_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Death States");

            //renderCollection.Add(rcell.gen_colors);
            tmp_collection = new ObservableCollection<RenderColor>(rcell.gen_colors);
            renderCollection.Add(tmp_collection);
            RowHeaders.Add("Generation");

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
}
