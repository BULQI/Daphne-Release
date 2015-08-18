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
using System.Collections.ObjectModel;

using System.Windows.Forms.DataVisualization.Charting;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynToolWindow.xaml
    /// </summary>
    public partial class CellPopDynToolWindow : ToolWinBase
    {
        ////private CellPopChartSurface MemSurface;
        ////private List<Series> MemSeries;

        ////private double pdfScaleFactor = 8;
        ////private double defaultFontSize = 20;

        ////private double axisFontSize;
        ////public double AxisFontSize
        ////{
        ////    get { return axisFontSize; }
        ////    set
        ////    {
        ////        if (axisFontSize != value)
        ////        {
        ////            axisFontSize = value;
        ////            OnPropertyChanged("AxisFontSize");
        ////        }
        ////    }
        ////}

        public CellPopDynToolWindow()
        {
            InitializeComponent();

            ////AxisFontSize = defaultFontSize;

            ////MemSeries = new List<Series>();
            ////MemSurface = new CellPopChartSurface();
            ////MemSurface.ChartTitle = "Rendered In Memory";
            ////ThemeManager.SetTheme(MemSurface, "BrightSpark");
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.Plot(this, plotOptions);
        }

        /// <summary>
        /// Export the chart to a file. 
        /// Must have first plotted the chart on screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void plotExportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Png Image|*.png|Pdf Image|*.pdf|Tiff Image|*.tif|Xps Image|*.xps";
            dlg.Title = "Export to File";
            dlg.RestoreDirectory = true;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                //Set legend to show only visible series for export
                legendModifier.GetLegendDataFor = SourceMode.AllVisibleSeries;
                legendModifier.UpdateLegend();

                //Export to file
                if (dlg.FileName.EndsWith("pdf"))
                {
                    ////AxisFontSize *= pdfScaleFactor;
                    mySciChart.SaveCellPopDynToPdf(dlg.FileName);
                }
                else
                {
                    mySciChart.SaveToFile(dlg.FileName);
                }

                //Set legend back to show all series
                legendModifier.GetLegendDataFor = SourceMode.AllSeries;
                legendModifier.UpdateLegend();                
            }

        }

        ////private void SaveCellPopDynToPdf(string filename)
        ////{
        ////    ThemeManager.SetTheme(MemSurface, "BrightSpark");

        ////    Document doc = new Document(PageSize.LETTER);
        ////    PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
        ////    doc.Open();

        ////    GetMemDataSeries();

        ////    MemSurface.Width = doc.PageSize.Width * pdfScaleFactor;
        ////    MemSurface.Height = doc.PageSize.Height * pdfScaleFactor;

        ////    //linXAxis.TitleFontSize *= pdfScaleFactor;
        ////    //Export this surface to bitmap source
        ////    var source = MemSurface.ExportToBitmapSource();

        ////    //Then retrieve from BitmapSource into a Bitmap object
        ////    Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        ////    BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
        ////    ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        ////    source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
        ////    bmp.UnlockBits(data);

        ////    //Now create an image from the bitmap and convert it to iTextSharp.text.Image
        ////    System.Drawing.Image image = bmp;
        ////    iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Bmp);

        ////    //Scale to page size, allow for margins         
        ////    float w, h;
        ////    w = doc.PageSize.Width - doc.LeftMargin * 2 - 10;
        ////    h = doc.PageSize.Height - doc.TopMargin * 2 - 10;
        ////    pic.ScaleAbsolute(w, h);

        ////    //Add the image to the doc
        ////    pic.Border = iTextSharp.text.Rectangle.BOX;
        ////    pic.BorderColor = iTextSharp.text.BaseColor.BLACK;
        ////    pic.BorderWidth = 0;  //3f;

        ////    doc.Add(pic);
        ////    doc.Close();
        ////}

        ////public void GetMemDataSeries()
        ////{
        ////}

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

        private void testButton_Click(object sender, RoutedEventArgs e)
        {            
        }

        private bool allStatesChecked = false;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                DataGrid dg = plotOptions.deathStatesGrid;
                
                CellPopulation pop = (CellPopulation)(plotOptions.lbPlotCellPops.SelectedItem);
                ObservableCollection<bool> states = pop.Cell.death_driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {
                    states[i] = !allStatesChecked;
                }

                states = pop.Cell.div_scheme.Driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {                    
                    states[i] = !allStatesChecked;
                }

                states = pop.Cell.diff_scheme.Driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {
                    states[i] = !allStatesChecked;
                }

                allStatesChecked = !allStatesChecked;
            }
        }
    }
}
