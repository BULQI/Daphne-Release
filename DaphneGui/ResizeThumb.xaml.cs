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
using System.Windows.Controls.Primitives;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ResizableDataGrid.xaml
    /// </summary>
    public partial class ResizeThumb : UserControl
    {
        public ResizeThumb()
        {
            InitializeComponent();
        }

        //private void MolTextBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule cm = MoleculesGrid.SelectedItem as ConfigMolecule;

        //    if (cm == null)
        //        return;

        //    Level level = this.DataContext as Level;
        //    if (level is Protocol)
        //    {
        //        Protocol p = level as Protocol;
        //        cm.ValidateName(p);
        //    }

        //    MainWindow.SOP.SelectedRenderSkin.SetRenderMolName(cm.renderLabel, cm.Name);

        //    int index = MoleculesGrid.SelectedIndex;
        //    MoleculesGrid.InvalidateVisual();
        //    MoleculesGrid.Items.Refresh();
        //    MoleculesGrid.SelectedIndex = index;
        //    cm = (ConfigMolecule)MoleculesGrid.SelectedItem;
        //    MoleculesGrid.ScrollIntoView(cm);
        //}

        private Cursor _cursor;

        private void OnResizeThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            _cursor = Cursor;
            Cursor = Cursors.SizeNS;
        }

        private void OnResizeThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Cursor = _cursor;
        }

        private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            Control c = DataContext as Control;
            //DataGrid grid = DataContext as DataGrid;
            double yChange = e.VerticalChange;
            double yNew = c.ActualHeight + yChange;

            //make sure not to resize to negative width or heigth  
            if (yNew < c.MinHeight)
                yNew = c.MinHeight;

            if (yNew > c.MaxHeight)
                yNew = c.MaxHeight;

            c.Height = yNew;
        }
    }
}
