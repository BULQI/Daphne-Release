using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Daphne;

using Abt.Controls.SciChart.Visuals;
using Abt.Controls.SciChart.Visuals.Axes;
using Abt.Controls.SciChart.Model.DataSeries;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Drawing;
using System.Drawing.Imaging;


namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Chart surface class derived from SciChartSurface
    /// </summary>
    class CellPopDynamicsChartSurface : CellPopChartSurface
    {
        //x axis default units are minutes 
        // For minutes, xScale = 1.0;
        // For hours,   xScale = 1.0/60 
        // For days,    xScale = 1.0/(60*24)
        // For weeks,   xScale = 1.0/(60*24*7)

        private double[] xScaleValues = { 1.0, 1.0 / 60, 1.0 / (60 * 24), 1.0 / (60 * 24 * 7) };
        private string[] xAxisLabels = { "Time in minutes", "Time in hours", "Time in days", "Time in weeks" };

        public string XAxisLabel { get; set; }
        public double XScale { get; set; }

        private Dictionary<int, System.Windows.Media.Color> lineColors;
        private int NextColorIndex = 0;

        public CellPopDynamicsChartSurface()
        {
            XAxisLabel = "Time in minutes";
            XScale = 1.0;

            lineColors = new Dictionary<int, System.Windows.Media.Color>();
            lineColors.Add(0, Colors.Blue);
            lineColors.Add(1, Colors.Red);
            lineColors.Add(2, new System.Windows.Media.Color { A = 255, R = 8, G = 251, B = 3 });   //bright green
            lineColors.Add(3, Colors.Magenta);
            lineColors.Add(4, Colors.Cyan);
            lineColors.Add(5, Colors.Black);
        }

        public void SetTimeUnits(int selIndex)
        {
            //if out of bounds, do not change
            if (selIndex < 0 || selIndex >= xScaleValues.Length)
                return;

            XScale = xScaleValues[selIndex];
            XAxisLabel = xAxisLabels[selIndex];
        }

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

            XAxes[0].AxisTitle = XAxisLabel;

            DrawStates(pop.Cell, pop.Cell.death_driver, data, CellPopulationDynamicsData.State.DEATH);
            DrawStates(pop.Cell, pop.Cell.diff_scheme.Driver, data, CellPopulationDynamicsData.State.DIFF);
            DrawStates(pop.Cell, pop.Cell.div_scheme.Driver, data, CellPopulationDynamicsData.State.DIV);

            // Set initial zoom??
            //mySciChart.XAxis.VisibleRange = new DoubleRange(3, 6);

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

                    for (int j = 0; j < series.Count; j++)
                    {
                        dSeries.Add(series[j]);
                        dTimes.Add(data.Times[j] * XScale);
                    }

                    var newSeries = new XyDataSeries<double, double> { SeriesName = driver.states[i] };
                    newSeries.Append(dTimes, dSeries);

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
        /// This method outputs a PDF file without first outputting a .bmp file.
        /// </summary>
        /// <param name="filename"></param>
        //public void OutputToPDF2(string filename)
        //{
        //    //Export this graph to BitmapSource
        //    var source = this.ExportToBitmapSource();

        //    //Then retrieve from BitmapSource into a Bitmap object
        //    Bitmap bmp1 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        //    BitmapData data = bmp1.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp1.Size),
        //    ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        //    source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
        //    bmp1.UnlockBits(data);

        //    //Now we have a bitmap. Convert the bitmap into a byte array by writing to memory instead of disk.
        //    //byte[] bmpArray = BitmapToByte(bmp1);

        //    //Now we have a bitmap to save to .PDF file instead of saving the graph as a .bmp file first

        //    //Create an image from bitmap array
        //    //iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(bmpArray);

        //    //-------------------------------------------------
        //    System.Drawing.Image image = bmp1;  
        //    Document doc = new Document(PageSize.A4);
        //    PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
        //    doc.Open();
        //    iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Jpeg);
        //    doc.Add(pdfImage);
        //    //A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
        //    //You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)
        //    // Add meta information to the document
        //    doc.AddAuthor("Sanjeev Gupta");
        //    doc.AddCreator("Daphne PDF output");
        //    doc.AddKeywords("PDF export daphne");
        //    doc.AddSubject("Document subject - Save the SciChart graph to a PDF document");
        //    doc.AddTitle("The document title - Daphne graph in PDF format");
        //    doc.Close();
        //    //------------------------------------------------------------------------------------------

        //}

        /// <summary>
        /// This method converts a bitmap to a byte array
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static byte[] BitmapToByte(Bitmap bmp)
        {
            byte[] byteArray = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Close();

                byteArray = stream.ToArray();
            }
            return byteArray;
        }

        //public void ExportToTiff(string outFile)
        //{
        //    var source = this.ExportToBitmapSource();
        //    Bitmap bmp3 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        //    BitmapData data = bmp3.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp3.Size),
        //        ImageLockMode.WriteOnly,
        //        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        //    source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
        //    bmp3.UnlockBits(data);
        //    bmp3.Save(outFile, ImageFormat.Tiff);
        //}
    }
}
