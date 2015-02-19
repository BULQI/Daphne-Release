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
            if (c == null)
                return;

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
