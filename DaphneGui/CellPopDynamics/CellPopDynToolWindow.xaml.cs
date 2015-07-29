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

        /// <summary>
        /// Export the chart to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void plotExportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "CellPopDynamics"; // Default file name
            dlg.DefaultExt = ".bmp"; // Default file extension
            dlg.Filter = "Bitmap Image (.bmp)|*.bmp|JPEG Image (.jpg)|*.jpg |PNG Image (.png)|*.png |TIFF Image (.tif)|*.tif |PDF Image (.pdf)|*.pdf |XPS Image (.xps)|*.xps";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results

            if (result == true)
                mySciChart.SaveToFile(dlg.FileName);

            ////The SavePdf is not working.  It is not outputting in high res so commenting it out for now.
            //if (result == true)
            //{
            //    // Save file
            //    if (dlg.FileName.EndsWith("pdf"))
            //    {
            //        this.SavePdf(dlg.FileName);
            //    }
            //    else
            //    {
            //        LineageSciChart.SaveToFile(dlg.FileName);
            //    }
            //}
        }

        /// <summary>
        /// Event handler called when user changes time units (minutes, hours, days, weeks).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// User selected "zoom out" from right-click context menu on chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.ZoomExtents();            
        }

        private void CellPopDynWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //actionsToolWindow.Dock(this.actionsContainer, ActiproSoftware.Windows.Controls.Docking.Direction.Bottom);
            //To zoom in and out: \n  Use the mouse wheel\n or select a rectangular area.\n\nTo pan, right-click and drag.
            tbSurfaceTooltip.Text = "";
            tbSurfaceTooltip.AppendText("To zoom in:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use the mouse wheel OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Select a rectangular area.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To zoom out:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Double click OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use mouse wheel OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Right-click + Zoom out.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To pan:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Press mouse wheel and drag.");
        }

    }
   
}
