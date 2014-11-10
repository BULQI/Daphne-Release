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
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using ActiproSoftware.Windows;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for RenderSkinEditor.xaml
    /// </summary>
    public partial class RenderSkinWindow : ToolWinBase
    {

        public RenderSkinWindow()
        {
            InitializeComponent();
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

            DataGridRow datagrid_row = DiffSchemeDataGrid.FindVisualParent<DataGridRow>(combo);
            bool isShading = false;
            if (datagrid_row != null)
            {
                var rowheader = datagrid_row.Header as DataGridRowHeader;
                string rowName = rowheader.Content as string;
                if (rowName.EndsWith("Shade"))
                {
                    DataGrid datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(datagrid_row);
                    var color_collection = datagrid.CurrentItem as ObservableCollection<RenderColor>;
                    int col_index = datagrid.CurrentColumn.DisplayIndex;
                    int nShade = 0;
                    foreach (var item in color_collection)
                    {
                        if (item != null) nShade++;
                    }
                    bool reverse_flag = false; // col_index >= nShade / 2;
                    ColorList color_option = (ColorList)combo.SelectedItem;
                    col_index = 0;
                    if (color_option != ColorList.Custom)
                    {
                        Color base_color = (Color)ColorConverter.ConvertFromString(color_option.ToString());
                        color_collection[0].EntityColor = base_color;
                    }
                    setRenderColorShading(color_collection, col_index, reverse_flag);
                    isShading = true;
                }
            }

            if (index < 7 && !isShading)
            {
                MultiBindingExpression be = BindingOperations.GetMultiBindingExpression(combo, ComboBox.SelectedIndexProperty);
                //BindingExpression be = combo.GetBindingExpression(ComboBox.SelectedIndexProperty);
                be.UpdateSource();
            }

            var selectedItem = combo.SelectedItem;


            if (index < 7 && e.AddedItems.Count > 0 && e.RemovedItems.Count > 0)
            {
                var datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(combo);
                if (datagrid == null)
                {
                    datagrid = DiffSchemeDataGrid.FindLogicalParent<DataGrid>(combo);
                }
                if (datagrid != null) datagrid.CancelEdit();
            }

            if (index == 8)
            {
                if (datagrid_row != null)
                {
                    datagrid_row.DetailsVisibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                if (datagrid_row != null)
                {
                    datagrid_row.DetailsVisibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void cellColorEditBox_DropDownClosed(object sender, RoutedEventArgs e)
        {
            var editbox = sender as ActiproSoftware.Windows.Controls.Editors.ColorEditBox;
            var datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(editbox);
            if (datagrid != null) datagrid.CancelEdit();
        }

        //private void ColorEditBoxValueChanged(object sender, PropertyChangedRoutedEventArgs<Color?> e)
        //{
        //    var editbox = sender as ActiproSoftware.Windows.Controls.Editors.ColorEditBox;
        //    var datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(editbox);

        //    Console.WriteLine(string.Format("OLD VALUE = {0} NEW VALUE = {1}", e.OldValue, e.NewValue));
        //}

        private void cellColorEditBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var editbox = sender as ActiproSoftware.Windows.Controls.Editors.ColorEditBox;

            var datagrid_row = (DataGridRow)DiffSchemeDataGrid.FindVisualParent<DataGridRow>(editbox);
            if (datagrid_row == null || datagrid_row.Header == null) return;

            string rowName = (datagrid_row.Header as DataGridRowHeader).Content as string;
            if (rowName == null || rowName.EndsWith("Shade") == false) return;

            DataGrid datagrid = DiffSchemeDataGrid.FindVisualParent<DataGrid>(datagrid_row);
            int col_index = datagrid.CurrentColumn.DisplayIndex;

            var color_collection = datagrid.CurrentItem as ObservableCollection<RenderColor>;

            setRenderColorShading(color_collection, col_index);

        }

        /// <summary>
        /// given datagrid row, and color for the col index, set shade/gradient for the row
        /// </summary>
        /// <param name="col_index"></param>
        /// <param name="color"></param>
        /// <param name="grid_row"></param>
        private void setRenderColorShading(ObservableCollection<RenderColor> color_collection, int col_index, bool reverse_flag = false)
        {
            Color base_color = color_collection[col_index].EntityColor;
            int nitem = 0;
            foreach (var item in color_collection)
            {
                if (item != null) nitem++;
            }
            List<Color> shades = ColorHelper.pickColorShades(base_color, nitem);
            if (reverse_flag == false)
            {
                for (int i = 0; i < shades.Count; i++)
                {
                    color_collection[i].EntityColor = shades[i];
                }
            }
            else
            {
                for (int i = 0; i < shades.Count; i++)
                {
                    color_collection[nitem - i-1].EntityColor = shades[i];
                }
            }
        }


        /// <summary>
        /// this filter only applies to the skinEditor usercontrol for cell rendering
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorListCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {

            var item = (ColorList)e.Item;
            if (item == ColorList.ColorBrewer)
            {
                DataGrid dataGrid = SkinEditor.RenderColorHost;
                DataGridRow row = (DataGridRow)(dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedItem));
                if (row == null)
                {
                    e.Accepted = false;
                    return;
                }
                string header = (row.Header as DataGridRowHeader).Content as string;

                e.Accepted = (header != "Base Color" && header.EndsWith("Shade") != true);
                return;
            }
            e.Accepted = true;
        }

        private void cbCellColor2_Loaded(object sender, RoutedEventArgs e)
        {
            (FindResource("ColorListCollectionViewSourceWithFilter") as CollectionViewSource).View.Refresh();
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

    //convert hex representation of color to brush....
    public class ColorStringToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string colstr = value as string;
            if (colstr == null) return Colors.White;

            Color color = (Color)ColorConverter.ConvertFromString(colstr);
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class RenderMethodEnumDescriptionConverter : IValueConverter
    {

        private string GetEnumDescription(Enum enumObj)
        {

            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            object[] attribArray = fieldInfo.GetCustomAttributes(false);
            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute attrib = attribArray[0] as DescriptionAttribute;
                return attrib.Description;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return value;
            }
            Enum enum_val = (Enum)value;
            string description = GetEnumDescription(enum_val);
            return description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.Empty;
        }

    }

}
