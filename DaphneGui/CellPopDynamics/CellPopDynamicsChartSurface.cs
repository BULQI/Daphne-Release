using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Daphne;
using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Visuals;
using Abt.Controls.SciChart.Visuals.Axes;
using Abt.Controls.SciChart.Model.DataSeries;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms.DataVisualization.Charting;


namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Chart surface class derived from SciChartSurface
    /// </summary>
    class CellPopDynamicsChartSurface : CellPopChartSurface
    {
        private Dictionary<int, System.Windows.Media.Color> lineColors;
        private int NextColorIndex = 0;

        private CellPopChartSurface MemSurface;
        private List<Series> MemSeries;
        private double defaultFontSize = 20;

        private double axisFontSize;
        public double AxisFontSize
        {
            get { return axisFontSize; }
            set
            {
                if (axisFontSize != value)
                {
                    axisFontSize = value;
                    OnPropertyChanged("AxisFontSize");
                }
            }
        }

        /// <summary>
        /// Constructor - initializes variables
        /// </summary>
        public CellPopDynamicsChartSurface()
        {
            lineColors = new Dictionary<int, System.Windows.Media.Color>();
            lineColors.Add(0, Colors.Blue);
            lineColors.Add(1, Colors.Red);
            lineColors.Add(2, new System.Windows.Media.Color { A = 255, R = 8, G = 251, B = 3 });   //bright green
            lineColors.Add(3, Colors.Magenta);
            lineColors.Add(4, Colors.Cyan);
            lineColors.Add(5, Colors.Black);

            AxisFontSize = defaultFontSize;
            MemSeries = new List<Series>();
            MemSurface = new CellPopChartSurface();
            this.ChartTitle = "Cell Population Dynamics";
            MemSurface.ChartTitle = "Cell Population Dynamics";
            ThemeManager.SetTheme(MemSurface, "BrightSpark");
        }

        /// <summary>
        /// This sets the scale factor given the time units the user selected.
        /// Also sets x axis label to be selected time units.
        /// </summary>
        /// <param name="selIndex"></param>
        public void SetTimeUnits(int selIndex)
        {
            //if out of bounds, do not change
            if (selIndex < 0 || selIndex >= xScaleValues.Length)
                return;

            XScale = xScaleValues[selIndex];
            XAxisLabel = xAxisLabels[selIndex];
        }

        /// <summary>
        /// Plot the graph. Check for certain error conditions first.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="plotOptions"></param>
        public void Plot(CellPopDynToolWindow window, CellPopDynOptions plotOptions)
        {
            NextColorIndex = 0;

            //Get the selected cell population - if none, inform user
            CellPopulation pop = plotOptions.lbPlotCellPops.SelectedItem as CellPopulation;
            if (pop == null)
            {
                MessageBox.Show("Please select a cell population first.", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (pop.Cell.HasDriver() == false)
            {
                MessageBox.Show("This cell population does not have any drivers so there is nothing to plot.", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (pop.Cell.IsAnyPlotStateSelected() == false)
            {
                MessageBox.Show("No states have been selected for plotting. Please select some states using the check boxes.", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Get the dynamics data for this cell pop - if null, that means the information for the desired population is not in the file - inform user
            CellPopulationDynamicsData data = MainWindow.Sim.Reporter.ProvideCellPopulationDynamicsData(pop);
            if (data == null)
            {
                MessageBox.Show("Missing data. Please either run this protocol first or load a past experiment", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //*********************************************************
            //If we get here, then we have the data and we can plot it.
            //*********************************************************

            RenderableSeries.Clear();

            //MEMORY RENDERING FOR HIGHER RESOLUTION
            MemSurface.XAxis = new NumericAxis();
            MemSurface.YAxis = new NumericAxis();
            MemSurface.XAxis.AxisTitle = "Time in minutes";
            MemSurface.YAxis.AxisTitle = "Number of cells";
            MemSurface.BorderBrush = System.Windows.Media.Brushes.Transparent;

            MemSeries.Clear();

            //END MEMORY RENDERING

            XAxes[0].AxisTitle = XAxisLabel;

            DrawStates(pop.Cell, pop.Cell.death_driver, data, CellPopulationDynamicsData.State.DEATH);
            DrawStates(pop.Cell, pop.Cell.diff_scheme.Driver, data, CellPopulationDynamicsData.State.DIFF);
            DrawStates(pop.Cell, pop.Cell.div_scheme.Driver, data, CellPopulationDynamicsData.State.DIV);

            //This draws the graph
            ZoomExtents();
            return;
        }

        private void DrawStates(ConfigCell cell, ConfigTransitionDriver driver, CellPopulationDynamicsData data, CellPopulationDynamicsData.State state)
        {
            ////NEED TO CHANGE Y VALUES FROM DOUBLE TO INT!
            for (int i = 0; i < driver.states.Count; i++)
            {
                if (driver.plotStates[i] == true) // states and plotStates are parallel lists
                {
                    List<int> series = data.GetState(state, i); // the first parameter is an enum
                    // can now plot series against data.Times; they are also parallel lists, i.e. times[j] belongs to series[j] – they form a point

                    List<double> dSeries = new List<double>();
                    List<double> dTimes = new List<double>();

                    Series memSeries = new Series();    //FOR RENDERING TO MEMORY

                    for (int j = 0; j < series.Count; j++)
                    {
                        dSeries.Add(series[j]);
                        dTimes.Add(data.Times[j] * XScale);
                        memSeries.Points.AddXY(data.Times[j] * XScale, series[j]);      //FOR RENDERING TO MEMORY
                    }

                    var newSeries = new XyDataSeries<double, double> { SeriesName = driver.states[i] };
                    newSeries.Append(dTimes, dSeries);

                    MemSeries.Add(memSeries);   //POPULATE MemSeries FOR RENDERING TO MEMORY

                    FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                    if (NextColorIndex >= lineColors.Count)
                    {
                        NextColorIndex = 0;
                    }

                    flrs.SeriesColor = lineColors[NextColorIndex];

                    NextColorIndex++;
                    if (NextColorIndex >= lineColors.Count)
                    {
                        NextColorIndex = 0;
                    }

                    flrs.DataSeries = newSeries;

                    RenderableSeries.Add(flrs);
                }
            }
        }

        /// <summary>
        /// Call this method to save plot to pdf file. 
        /// This assumes that the series have already been plotted to the screen. 
        /// Plotting to screen populates MemSeries which is needed by PopulateMemSurfaceSeries().
        /// </summary>
        /// <param name="filename"></param>
        public void SaveCellPopDynToPdf(string filename)
        {
            Document doc = new Document(PageSize.LETTER);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();

            PopulateMemSurfaceSeries();

            MemSurface.Width = doc.PageSize.Width * pdfScaleFactor;
            MemSurface.Height = doc.PageSize.Height * pdfScaleFactor;

            //linXAxis.TitleFontSize *= pdfScaleFactor;
            //Export this surface to bitmap source
            var source = MemSurface.ExportToBitmapSource();

            //Then retrieve from BitmapSource into a Bitmap object
            Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            bmp.Save("c:\\temp\\bitmaptest.bmp", ImageFormat.Bmp);
            bmp.Save("c:\\temp\\bitmaptest.jpg", ImageFormat.Jpeg);
            bmp.Save("c:\\temp\\bitmaptest.png", ImageFormat.Png);

            //Now create an image from the bitmap and convert it to iTextSharp.text.Image
            System.Drawing.Image image = bmp;
            iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Bmp);

            //Scale to page size, allow for margins         
            float w, h;
            w = doc.PageSize.Width - doc.LeftMargin * 2 - 10;
            h = doc.PageSize.Height - doc.TopMargin * 2 - 10;
            pic.ScaleAbsolute(w, h);

            //Add the image to the doc
            pic.Border = iTextSharp.text.Rectangle.BOX;
            pic.BorderColor = iTextSharp.text.BaseColor.BLACK;
            pic.BorderWidth = 0;  //3f;

            doc.Add(pic);
            doc.Close();
        }

        /// <summary>
        /// Once MemSeries is populated, this method can be called to add series data, in the correct format, to the MemSurface.
        /// </summary>
        private void PopulateMemSurfaceSeries()
        {
            NextColorIndex = 0;
            MemSurface.FontSize = defaultFontSize;

            foreach (Series s in MemSeries)
            {
                var dataSeries = new XyDataSeries<double, double>();
                dataSeries.SeriesName = "";
                
                double[] x;
                double[] y;

                List<double> tempX = new List<double>();
                List<double> tempY = new List<double>();

                for (int i = 0; i < s.Points.Count; i++)
                {
                    tempX.Add(s.Points[i].XValue);
                    tempY.Add(s.Points[i].YValues[0]);
                }

                x = tempX.ToArray();
                y = tempY.ToArray();

                dataSeries.Append(x, y);

                FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                
                flrs.StrokeThickness *= (int)pdfScaleFactor;
                flrs.DataSeries = dataSeries;

                //Color
                if (NextColorIndex >= lineColors.Count)
                {
                    NextColorIndex = 0;
                }
                flrs.SeriesColor = lineColors[NextColorIndex];
                NextColorIndex++;
                if (NextColorIndex >= lineColors.Count)
                {
                    NextColorIndex = 0;
                }

                MemSurface.RenderableSeries.Add(flrs);
            }

            //Axis related settings for memory surface

            //DefaultTickLabel dtl = new DefaultTickLabel();
            //dtl.Style = (Style)FindResource("XAxisLabelStyle");
            //dtl.FontSize = dtl.FontSize * pdfScaleFactor;

            NumericAxis xax = new NumericAxis();
            xax.AxisTitle = "Time in minutes";
            xax.TitleFontSize = xax.TitleFontSize * pdfScaleFactor;   // linXAxis.TitleFontSize * pdfScaleFactor;
            xax.DrawMinorGridLines = true;
            xax.DrawMajorGridLines = true;
            xax.DrawLabels = true;
            xax.TickLabelStyle = (Style)FindResource("XAxisLabelStyle");

            NumericAxis yax = new NumericAxis();
            yax.AxisTitle = "Number of cells";
            yax.AxisAlignment = AxisAlignment.Left;
            yax.TitleFontSize = yax.TitleFontSize * pdfScaleFactor;   // linXAxis.TitleFontSize * pdfScaleFactor;
            yax.DrawMinorGridLines = true;
            yax.DrawMajorGridLines = true;
            yax.DrawLabels = true;

            MemSurface.XAxis = xax;
            MemSurface.YAxis = yax;

            MemSurface.XAxis.GrowBy = new DoubleRange(0.1, 0.2);    //Makes sure not to clip away data at the boundaries
            MemSurface.YAxis.GrowBy = new DoubleRange(0.2, 0.2);    //Makes sure Y-Axis is not clipped away for data at boundaries

            MemSurface.FontSize *= pdfScaleFactor;
            MemSurface.ZoomExtents();

        }

        
    }
}
