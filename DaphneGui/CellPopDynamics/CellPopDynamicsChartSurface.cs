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
    class CellPopDynamicsChartSurface : SciChartSurface
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

        public void OutputToPDF(string filename)
        {
            //First we create a file stream object representing the actual file and name it to whatever you want.
            //(By using the method MapPath we target the folder we created earlier as this is a Web application)

            System.IO.FileStream fs = new FileStream(filename, FileMode.Create);

            //To create a PDF document, create an instance of the class Document and pass the page size and the page margins to the constructor. 
            //Then use that object and the file stream to create the PdfWriter instance enabling us to output text and other elements to the PDF file.

            //First save image as bmp file    
            string tempFile = @"c:\temp\cellpopdyn.bmp";
            ExportToFile(tempFile, ExportType.Bmp);

            //Get image
            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(tempFile);

            // Create an instance of the document class which represents the PDF document itself.
            iTextSharp.text.Rectangle rect = new iTextSharp.text.Rectangle(img.Width, img.Height);
            Document document = new Document(rect, 25, 25, 20, 20);     //new Document(PageSize.A4, 25, 25, 30, 30);

            // Create an instance to the PDF file by creating an instance of the PDF
            // Writer class using the document and the filestrem in the constructor.
            PdfWriter writer = PdfWriter.GetInstance(document, fs);

            //A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
            //You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)

            // Add meta information to the document
            document.AddAuthor("Sanjeev Gupta");
            document.AddCreator("Daphne PDF output");
            document.AddKeywords("PDF export daphne");
            document.AddSubject("Document subject - Save the SciChart graph to a PDF document");
            document.AddTitle("The document title - Daphne graph in PDF format");

            //Before we can write to the document, we need to open it.
            document.Open();

            // Add a simple and wellknown phrase to the document in a flow layout manner
            //document.Add(new iTextSharp.text.Paragraph("Hello World!"));

            document.Add(img);

            // Close the document
            document.Close();

            // Close the writer instance
            writer.Close();

            // Always close open filehandles explicity
            fs.Close();

            //Other way
            //To place the image you first set the position and then add the image to the content byte:
            //PdfContentByte cb = writer.DirectContent;
            //iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(Server.MapPath("img.png"));
            //img.SetAbsolutePosition(50, 647);
            //cb.AddImage(img);

            ////You can scale the size using the ScaleAbsolute or ScalePercent methods like this:

            //img.ScaleAbsolute(216, 70);
            //img.ScalePercent(50);
        }

        public void ExportToTiff(string outFile)
        {
            //First save image as bmp file    
            string tempFile = @"c:\temp\cellpopdyn.bmp";
            ExportToFile(tempFile, ExportType.Bmp);

            //Start with the first bitmap by putting it into an Image object
            Bitmap bitmap = (Bitmap)System.Drawing.Image.FromFile(tempFile);
            bitmap.Save(outFile, ImageFormat.Tiff);
        }

    }
}
