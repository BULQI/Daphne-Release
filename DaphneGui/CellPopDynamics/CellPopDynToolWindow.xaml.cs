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
using Abt.Controls.SciChart.ChartModifiers;
using Abt.Controls.SciChart.Model.DataSeries;
using Daphne.Charting.Data;
using Abt.Controls.SciChart.Visuals;
using Abt.Controls.SciChart.Themes;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Abt.Controls.SciChart.Visuals.PointMarkers;
using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Visuals.Axes;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynToolWindow.xaml
    /// </summary>
    public partial class CellPopDynToolWindow : ToolWinBase
    {       
        public CellPopDynToolWindow()
        {
            InitializeComponent();
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.Plot(this, plotOptions);
        }

        private void plotExportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Image"; // Default file name
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "Bitmap (*.bmp)|*.bmp|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|TIFF (*.tif)|*.tif|PDF (*.pdf)|*.pdf";

            dlg.FilterIndex = 2;
            dlg.RestoreDirectory = true;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save file
                SaveToFile(dlg.FileName);
            }
           
        }

        public void SaveToFile(string filename)
        {
            if (filename.EndsWith("png"))
            {
                mySciChart.ExportToFile(filename, ExportType.Png);
            }
            else if (filename.EndsWith("bmp"))
            {
                mySciChart.ExportToFile(filename, ExportType.Bmp);
            }
            else if (filename.EndsWith("jpg"))
            {
                mySciChart.ExportToFile(filename, ExportType.Jpeg);
            }
            else if (filename.EndsWith("pdf"))
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true)
                {
                    var size = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaWidth * 3 / 4);
                    var scs = CreateSciChartSurfaceWithoutShowingIt();

                    // And print. This works particularly well to XPS!
                    Action printAction = () => dialog.PrintVisual(scs, "Exported");
                    Dispatcher.BeginInvoke(printAction);
                }
            }
        }

        private Visual CreateSciChartSurfaceWithoutShowingIt()
        {
            SciChartSurface surf = new SciChartSurface();
            // We must set a width and height. If you are rendering off screen without showing
            // we have to tell the control what size to render
            surf.Width = mySciChart.Width;
            surf.Height = mySciChart.Height;

            // Doing an export sets up the chart for rendering off screen, including handling all the horrible layout issues
            // that occur when a WPF element is created and rendered without showing it.
            //
            // You must call this before printing, even if you don't intend to use the bitmap.
            surf.ExportToBitmapSource();

            return surf;
        }

        private void TimeUnitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null || combo.SelectedIndex == -1)
                return;
            // Only want to respond to purposeful user interaction - so if initializing combo box, ignore                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count == 0)
                return;

            mySciChart.SetTimeUnits(combo.SelectedIndex);
            mySciChart.Plot(this, plotOptions);

        }

        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.ZoomExtents();            
        }

        private void CellPopDynWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //To zoom in and out: \n  Use the mouse wheel\n or select a rectangular area.\n\nTo pan, right-click and drag.
            tbSurfaceTooltip.Text = "";
            tbSurfaceTooltip.AppendText("To zoom in and out:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use the mouse wheel OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Select a rectangular area.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To zoom back out:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Double click OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use mouse wheel.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To pan:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Right-click and drag.");
        }

    }
   
}
