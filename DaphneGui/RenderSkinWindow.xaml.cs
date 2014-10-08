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
using ActiproSoftware.Windows.Controls.Docking;
using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for RenderSkinEditor.xaml
    /// </summary>
    public partial class RenderSkinWindow : ToolWindow
    {

        public RenderSkinWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(RenderSkinWindow_Loaded);
            //this.DockSite.WindowClosing += new EventHandler<DockingWindowEventArgs>(RenderWindowClosing);
        }

        void RenderSkinWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DockSite.WindowClosing -= new EventHandler<DockingWindowEventArgs>(RenderWindowClosing);
            this.DockSite.WindowClosing += new EventHandler<DockingWindowEventArgs>(RenderWindowClosing);
        }

        void RenderWindowClosing(object sender, DockingWindowEventArgs e)
        {
            //check if this render skin change, if yes, ask for if change....
            //todo: do this check...
            RenderSkin rs = this.DataContext as RenderSkin;
            if (rs == null) return;
            rs.SerializeToFile(rs.FileName);
        }

        /// <summary>
        /// to persist selected index when switch skin, select index 0 by default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellsListBox1_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var item = CellsListBox1.SelectedItem;
            string item_lable = item == null ? null : (item as RenderCell).renderLabel;
            RenderSkin rs = e.NewValue as RenderSkin;
            CellsListBox1.ItemsSource = rs == null ? null : rs.renderCells;
            int selectedIndex = 0;
            if (rs != null && rs.renderCells != null && item_lable != null)
            {
                selectedIndex = rs.renderCells.IndexOf(rs.renderCells.Where(p => p.renderLabel == item_lable).FirstOrDefault());
                if (selectedIndex == -1) selectedIndex = 0;
            }
            CellsListBox1.SelectedIndex = selectedIndex;
        }

        private void cbCellColor2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;
            int index = combo.SelectedIndex;
            if (index != 7 && e.AddedItems.Count > 0 && e.RemovedItems.Count > 0)
            {
                var datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(combo);
                if (datagrid != null) datagrid.CancelEdit();
            }
        }

        private void cellColorEditBox_DropDownClosed(object sender, RoutedEventArgs e)
        {
            var editbox = sender as ActiproSoftware.Windows.Controls.Editors.ColorEditBox;
            var datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(editbox);
            if (datagrid != null) datagrid.CancelEdit();
        }

    }


    public class ColorToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Color))
                throw new InvalidOperationException("Value must be a Color");
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }



}
