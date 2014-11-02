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
using System.Windows.Shapes;
using Daphne;

namespace DaphneGui.Rendering
{
    /// <summary>
    /// Interaction logic for RenderCell.xaml
    /// </summary>
    public partial class RenderCell : Window
    {
        public Dictionary<string, System.Windows.Media.Color> colors_dict;
        public RenderCell()
        {
            InitializeComponent();
            colors_dict = new Dictionary<string, System.Windows.Media.Color>();

            //System.Windows.Media.Color color = new System.Windows.Media.Color();
            //color = System.Windows.Media.Color.FromArgb(255, 255, 0, 0);
            colors_dict.Add("0", System.Windows.Media.Color.FromArgb(255, 255, 0, 0));
            colors_dict.Add("1", System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
            colors_dict.Add("2", System.Windows.Media.Color.FromArgb(255, 0, 0, 255));
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    ////public class RenderTemplateSelector : DataTemplateSelector
    ////{
    ////    public DataTemplate ByPopTemplate { get; set; }
    ////    public DataTemplate ByStateTemplate { get; set; }
    ////    public DataTemplate ByGenTemplate { get; set; }

    ////    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    ////    {
    ////        if (item == null)
    ////            return null;

    ////        //CellRenderingMethod method = (CellRenderingMethod)item;

    ////        //if (method == CellRenderingMethod.CELL_TYPE)
    ////        //    return ByPopTemplate;
    ////        //else if (method == CellRenderingMethod.CELL_STATE)
    ////        //    return ByStateTemplate;
    ////        //else if (method == CellRenderingMethod.CELL_GEN)
    ////        //    return ByGenTemplate;

    ////        return ByPopTemplate;
    ////    }
    ////}
}
