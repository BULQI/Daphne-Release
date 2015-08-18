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
using Abt.Controls.SciChart.Model.DataSeries;
using Abt.Controls.SciChart;
using ActiproSoftware.Windows.Controls.Docking;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Abt.Controls.SciChart.Visuals.PointMarkers;
using Abt.Controls.SciChart.Visuals.Axes;
using Abt.Controls.SciChart.Visuals.Annotations;

using Daphne;

using System.Numerics;
using System.Collections.ObjectModel;
using System.Windows.Forms.DataVisualization.Charting;
using Abt.Controls.SciChart.Visuals;
using System.Drawing;
using System.Drawing.Imaging;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace DaphneGui.CellLineage
{
    /// <summary>
    /// Interaction logic for CellLineageControl.xaml
    /// </summary>
    public partial class CellLineageControl : ToolWinBase
    {
        public ObservableCollection<FounderInfo> FounderCells { get; set; }
        public ObservableCollection<FounderInfo> FounderCellsByCellPop { get; set; }
        public ObservableCollection<CellPopulation> FounderCellPops { get; set; }
        public TissueScenario ScenarioHandle {get;set;}

        private CellPopChartSurface MemSurface;
        private List<Series> MemSeries;

        private double pdfScaleFactor = 8;
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

        public CellLineageControl()
        {
            FounderCells = new ObservableCollection<FounderInfo>();
            FounderCellsByCellPop = new ObservableCollection<FounderInfo>();
            FounderCellPops = new ObservableCollection<CellPopulation>();
            ScenarioHandle = null;

            InitializeComponent();

            DataContext = this;
            AxisFontSize = defaultFontSize;

            MemSeries = new List<Series>();
            MemSurface = new CellPopChartSurface();
            MemSurface.ChartTitle = "Rendered In Memory";
            //MemSurface.XAxis = new NumericAxis();            
        }
        
        /// <summary>
        /// This method calls the Reporter to return a list of FounderInfo, all founder cells.
        /// </summary>
        private void LoadLineageData()
        {
            DataContext = this;

            //First, get all the cell populations with division schemes, to display in the ListBox.
            ScenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            FounderCellPops.Clear();
            foreach (KeyValuePair<int,CellPopulation> kvp in ScenarioHandle.cellpopulation_dict)
            {
                if (kvp.Value.Cell.div_scheme != null)
                {
                    FounderCellPops.Add(kvp.Value);
                }
            }
            

            //Second, get the dictionary of all founder cells and store them in FounderCells
            Dictionary<int, FounderInfo> result = MainWindow.Sim.Reporter.ProvideFounderCells();

            if (result == null)
            {
                MessageBox.Show("Lineage reporting options not turned on.");
                return;
            }

            FounderCells.Clear();
            foreach (KeyValuePair<int, FounderInfo> kvp in result)
            {
                FounderCells.Add(kvp.Value);
            }

            if (FounderCellPops.Count > 0)
            {
                OnPropertyChanged("FounderCellPops");
            }

            if (cellPopsListBox.Items.Count > 0)
            {
                cellPopsListBox.SelectedIndex = -1;
                cellPopsListBox.SelectedIndex = 0;
            }
        }

        private void cellPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to meaningful user interaction, not just population and depopulation of cell pops list
            //if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            //    return;

            ListBox cellPopLBox = sender as ListBox;
            if (cellPopLBox.Items.Count == 0 || cellPopLBox.SelectedIndex == -1)
                return;

            //Get the cell pop id of currently selected cell populaton
            int cellPopId = ((CellPopulation)cellPopLBox.SelectedItem).cellpopulation_id;

            //If ScenarioHandle is null, assign it here
            if (ScenarioHandle == null)
            {
                ScenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            }

            ////Get the cell type and display it
            //string cell_type = ((CellPopulation)cellPopLBox.SelectedItem).Cell.CellName;
            //tbCellType.Text = cell_type;

            ////Get all the founder cells in the selected cell population and Add them to FounderCellsByCellPop which is bound to 2nd listbox
            //if (FounderCells.Count == 0)
            //    return;

            FounderCellsByCellPop.Clear();
            List<FounderInfo> listFI = FounderCells.Where(x => x.Population_Id == cellPopId).ToList();
            foreach (FounderInfo fi in listFI)
            {
                FounderCellsByCellPop.Add(fi);
            }

            lineageExportButton.IsEnabled = false;

        }

        /// <summary>
        /// This gets called when chart is loaded. Tooltip is initialized here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LineageSciChart_Loaded(object sender, RoutedEventArgs e)
        {            
            //Set up surface tooltip
            tbLineageSurfaceTooltip.Text = "";
            tbLineageSurfaceTooltip.AppendText("To zoom in:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Use the mouse wheel OR");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Select a rectangular area.");

            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("To zoom out:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Double click OR");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Use mouse wheel OR");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Right-click.");

            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("To pan:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Press mouse wheel and drag.");

            ClearChart();
            LoadLineageData();
            //CreateFakeDataSeries();
            System.Windows.Size size = new System.Windows.Size(600.0, 400.0);
            this.RenderSize = size;
            LineageSciChart.YAxis.Visibility = Visibility.Hidden;
            ourYAxis.StrokeThickness = 0;
            ourYAxis.BorderBrush = System.Windows.Media.Brushes.Transparent;

            //Hiding axes works well now - last thing was the surface's brush that needed to be hidden.
            LineageSciChart.BorderBrush = System.Windows.Media.Brushes.Transparent;
            LineageSciChart.ZoomExtents();
        }
         
        /// <summary>
        /// Export the chart to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lineageExportButton_Click(object sender, RoutedEventArgs e)
        {           
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Png Image|*.png|Pdf Image|*.pdf|Tiff Image|*.tif|Xps Image|*.xps";
            saveFileDialog.Title = "Export to File";
            saveFileDialog.RestoreDirectory = true;

            axisFontSize = defaultFontSize;

            // Show save file dialog box
            Nullable<bool> result = saveFileDialog.ShowDialog();

            //Need to move the SavePdf code to CellLineageChartSurface class
            if (result == true)
            {
                // Save file
                //public Style TitleStyle {get; set;}
                if (saveFileDialog.FileName.EndsWith("pdf"))
                {
                    
                    AxisFontSize *= pdfScaleFactor;
                    this.SavePdf(saveFileDialog.FileName);
                    AxisFontSize = defaultFontSize;
                    //LineageSciChart.ExportToPDF(saveFileDialog.FileName);
                }
                else
                {
                    LineageSciChart.SaveToFile(saveFileDialog.FileName);
                }
            }
        }

        private void SavePdf(string filename)
        {
            ThemeManager.SetTheme(MemSurface, "BrightSpark");

            Document doc = new Document(PageSize.LETTER);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();

            GetMemDataSeries();

            MemSurface.Width = doc.PageSize.Width * pdfScaleFactor;
            MemSurface.Height = doc.PageSize.Height * pdfScaleFactor;

            //linXAxis.TitleFontSize *= pdfScaleFactor;
            //Export this surface to bitmap source
            var source = MemSurface.ExportToBitmapSource();

            //Then retrieve from BitmapSource into a Bitmap object
            Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
            ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            
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

            ////A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
            ////You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)
            //// Add meta information to the document
            //doc.AddAuthor("Sanjeev Gupta");
            //doc.AddCreator("Daphne PDF output");
            //doc.AddKeywords("PDF export daphne");
            //doc.AddSubject("Document subject - Save the SciChart graph to a PDF document");
            //doc.AddTitle("The document title - Daphne graph in PDF format");
            doc.Close();
        }

        /// <summary>
        /// Gets series for the memory chart surface. This method assumes that each series is only two points.
        /// Do not use this method if your series is goint to be more than two points.
        /// </summary>
        private void GetMemDataSeries()
        {            
            foreach (Series s in MemSeries)
            {
                var dataSeries = new XyDataSeries<double, double>();
                dataSeries.SeriesName = "";
                EllipsePointMarker marker = new EllipsePointMarker();
                marker.Height = 12 * pdfScaleFactor;   marker.Width = 12 * pdfScaleFactor;
                marker.Fill = new System.Windows.Media.Color { A = 255, R = 148, G = 249, B = 146 };
                marker.Stroke = Colors.Green;
                marker.StrokeThickness = 1 * pdfScaleFactor;

                double[] x;
                double[] y;

                List<double> tempX = new List<double>();
                List<double> tempY = new List<double>();
                tempX.Add(s.Points[0].XValue);
                tempX.Add(s.Points[1].XValue );
                tempY.Add(s.Points[0].YValues[0]);
                tempY.Add(s.Points[1].YValues[0] );

                x = tempX.ToArray();
                y = tempY.ToArray();

                dataSeries.Append(x, y);

                //Annotations
                //For founder cell, the annotation should be the y-axis label
                if (x[0] == 0)
                {
                    var textAnnot0 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                    {
                        Name = s.Name,
                        Text = MemSurface.YAxis.AxisTitle,
                        X1 = x[0],
                        Y1 = y[0],
                    };
                    textAnnot0.FontSize = 9.0 * pdfScaleFactor;
                    MemSurface.Annotations.Add(textAnnot0);
                }

                //For the 1st point
                var textAnnot1 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                {
                    Name = s.Name,
                    Text = s.Points[0].Label,
                    X1 = x[0],
                    Y1 = y[0],
                };
                textAnnot1.FontSize = 9.0 * pdfScaleFactor;
                MemSurface.Annotations.Add(textAnnot1);

                //For the 2nd point
                var textAnnot2 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                {
                    Name = s.Name,
                    Text = s.Points[1].Label,
                    X1 = x[1],
                    Y1 = y[1],
                };
                textAnnot2.FontSize = 9.0 * pdfScaleFactor;
                MemSurface.Annotations.Add(textAnnot2);

                FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                System.Windows.Media.Color col = new System.Windows.Media.Color { A = s.Color.A, R = s.Color.R, G = s.Color.G, B = s.Color.B };
                flrs.DataSeries = dataSeries;
                flrs.SeriesColor = col;
                flrs.PointMarker = marker;
                flrs.StrokeThickness *= (int)pdfScaleFactor;

                MemSurface.RenderableSeries.Add(flrs);

            }

            //Axis related settings for memory surface
            
            //DefaultTickLabel dtl = new DefaultTickLabel();
            //dtl.Style = (Style)FindResource("XAxisLabelStyle");
            //dtl.FontSize = dtl.FontSize * pdfScaleFactor;

            NumericAxis na = new NumericAxis();
            na.AxisTitle = "Time in minutes";
            na.TitleFontSize = linXAxis.TitleFontSize * pdfScaleFactor;
            na.DrawMinorGridLines = true;
            na.DrawMajorGridLines = true;
            na.DrawLabels = true;
            na.TickLabelStyle = (Style)FindResource("XAxisLabelStyle");
            
            MemSurface.XAxis = na;
            MemSurface.XAxis.GrowBy = new DoubleRange(0.1, 0.2);    //Makes sure not to clip away data at the boundaries
            MemSurface.YAxis.GrowBy = new DoubleRange(0.2, 0.2);    //Makes sure Y-Axis is not clipped away for data at boundaries
            MemSurface.YAxis.Visibility = Visibility.Hidden;

            MemSurface.ZoomExtents();
            MemSurface.FontSize *= pdfScaleFactor;
            
        }

        private double GetSeriesMin(IList<double> values) 
        {            
            return (from v in values select v).Min();
        }

        private double GetSeriesMax(IList<double> values)
        {
            return (from v in values select v).Max();
        }



        /// <summary>
        /// Right-click Menu Zoom Out handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            LineageSciChart.ZoomExtents();
        }

        /// <summary>
        /// Draw button handler. Calls the Draw method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drawButton_Click(object sender, RoutedEventArgs e)
        {            
            lineageExportButton.IsEnabled = true;
            Draw();
        }

        /// <summary>
        /// This method draws the lineage.  
        /// During the draw operation, the data is rendered to a memory surface to for exporting a high res image later
        /// </summary>
        private void Draw()
        {
            int index = founderCellsListBox.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("Please select a founder cell first.", "Cell lineage error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PedigreeAnalysis pda = new PedigreeAnalysis();
            pda.SetReport(MainWindow.Sim.Reporter);
            List<Series> s = pda.GetPedigreeTreeSeries(FounderCellsByCellPop[index]);

            if (s == null || s.Count == 0)
            {
                MessageBox.Show("Pedigree was not found.", "Cell lineage error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //MEMORY RENDERING FOR HIGHER RESOLUTION
            List<Series> tempSer = pda.GetPedigreeTreeSeries(FounderCellsByCellPop[index]);
            foreach (Series ser in tempSer)
            {
                MemSeries.Add(ser);
            }
            MemSurface.XAxis = new NumericAxis();
            MemSurface.YAxis = new NumericAxis();
            MemSurface.ChartTitle = pda.GetChartTitle();
            MemSurface.XAxis.AxisTitle = pda.GetChartXTitle(); 
            MemSurface.YAxis.AxisTitle = pda.GetChartYTitle(); 
            MemSurface.BorderBrush = System.Windows.Media.Brushes.Transparent;
            //END MEMORY
            
            ClearChart();
            
            //last 3 things are the chart tile, x-axis and y-axis titles.
            string chartTitle = pda.GetChartTitle();
            string XTitle = pda.GetChartXTitle();
            string YTitle = pda.GetChartYTitle();

            ConvertAndDraw(chartTitle, XTitle, YTitle, s);
        }

        private void ClearChart()
        {
            LineageSciChart.RenderableSeries.Clear();
            LineageSciChart.Annotations.Clear();
            LineageSciChart.XAxis.Clear();
            LineageSciChart.YAxis.Clear();
        }

        /// <summary>
        /// This method takes all the series to be plotted (each series is only 2 points and is assumed to be 2 points), 
        /// and converts the data into a format that SciChart recognizes.  
        /// 
        /// NOTE: DO NOT USE THIS METHOD TO DRAW ANY SERIES WITH MORE THAN 2 POINTS
        /// 
        /// </summary>
        /// <param name="chartTitle"></param>
        /// <param name="xTitle"></param>
        /// <param name="yTitle"></param>
        /// <param name="series"></param>
        private void ConvertAndDraw(string chartTitle, string xTitle, string yTitle, List<Series> series)
        {
            if (series == null)
            {
                MessageBox.Show("No pedigree data was found.", "Cell lineage error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach(Series s in series) 
            {
                var dataSeries = new XyDataSeries<double, double>();
                dataSeries.SeriesName = "";
                EllipsePointMarker marker = new EllipsePointMarker();
                marker.Height = 12; marker.Width = 12;
                marker.Fill = new System.Windows.Media.Color { A=255, R=148, G=249, B=146 };
                marker.Stroke = Colors.Green;
                marker.StrokeThickness = 1;

                double[] x;
                double[] y;

                List<double> tempX = new List<double>();
                List<double> tempY = new List<double>();
                tempX.Add(s.Points[0].XValue * LineageSciChart.XScale);
                tempX.Add(s.Points[1].XValue * LineageSciChart.XScale);
                tempY.Add(s.Points[0].YValues[0]);
                tempY.Add(s.Points[1].YValues[0]);
                x = tempX.ToArray();
                y = tempY.ToArray();

                dataSeries.Append(x, y);

                FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                System.Windows.Media.Color col = new System.Windows.Media.Color { A=s.Color.A, R=s.Color.R, G=s.Color.G, B=s.Color.B };
                flrs.DataSeries = dataSeries;
                flrs.SeriesColor = col;
                flrs.PointMarker = marker;

                LineageSciChart.RenderableSeries.Add(flrs);

                //This is to add Annotations
                //For founder cell, the annotation should be the y-axis label
                if (x[0] == 0)
                {
                    var textAnnot0 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                    {
                        Name = s.Name,
                        Text = LineageSciChart.YAxis.AxisTitle,
                        FontSize = 6.0,
                        X1 = x[0],
                        Y1 = y[0],
                    };
                    textAnnot0.FontSize = 9.0;
                    LineageSciChart.Annotations.Add(textAnnot0);
                }

                //For the 1st point
                var textAnnot1 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                {
                    Name = s.Name,
                    Text = s.Points[0].Label,
                    FontSize = 6.0,
                    X1 = x[0],
                    Y1 = y[0],
                };
                textAnnot1.FontSize = 9.0;
                LineageSciChart.Annotations.Add(textAnnot1);

                //For the 2nd point
                var textAnnot2 = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                {
                    Name = s.Name,
                    Text = s.Points[1].Label,
                    X1 = x[1],
                    Y1 = y[1],
                };
                textAnnot2.FontSize = 9.0;
                LineageSciChart.Annotations.Add(textAnnot2);
            }

            //Other chart attributes
            LineageSciChart.ChartTitle = chartTitle;
            //LineageSciChart.XAxis.AxisTitle = xTitle;
            LineageSciChart.YAxis.AxisTitle = yTitle;
            LineageSciChart.BorderBrush = System.Windows.Media.Brushes.Transparent;
            LineageSciChart.ZoomExtents();
        }

        /// <summary>
        /// Event handler called when user changes time units (minutes, hours, days, weeks).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lineageTimeUnitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null || combo.SelectedIndex == -1)
                return;
            // Only want to respond to purposeful user interaction - so if initializing combo box, ignore                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count == 0)
                return;

            LineageSciChart.SetTimeUnits(combo.SelectedIndex);
            
            if (MemSeries.Count != 0)
                Draw();

        }

        

#if false
        private IXyDataSeries<double, double> CreateDataSeries()
        {
            var dataSeries = new XyDataSeries<double, double>();
            dataSeries.SeriesName = "Random Series";

            int i = 0;
            // Samples 0,1,2 append double.NaN
            for (; i < 3; i++)
            {
                dataSeries.Append(i, double.NaN);
            }

            // Samples 3,4,5,6 append values
            for (; i < 7; i++)
            {
                dataSeries.Append(i, _random.NextDouble());
            }

            // Samples 7,8,9 append double.NaN
            for (; i < 10; i++)
            {
                dataSeries.Append(i, double.NaN);
            }

            // Samples 10,11,12,13 append values
            for (; i < 14; i++)
            {
                dataSeries.Append(i, -_random.NextDouble());
            }

            // Samples 14,15,16 append double.NaN
            for (; i < 16; i++)
            {
                dataSeries.Append(i, double.NaN);
            }

            return dataSeries;
        }

        private void CreateFakeDataSeries()
        {
            var dataSeries = new XyDataSeries<double, double>();
            dataSeries.SeriesName = "Line 1";
            dataSeries.AcceptsUnsortedData = true;
            EllipsePointMarker marker = new EllipsePointMarker();
            marker.Height = 12; marker.Width = 12;
            marker.Fill = Colors.Red;
            marker.Stroke = Colors.Lavender;
            marker.StrokeThickness = 1;

            // Samples - append arbitrary y values
            dataSeries.Append(0, 20);
            dataSeries.Append(10, 35);
            dataSeries.Append(20, 40);
            renderableLineSeries.DataSeries = dataSeries;

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(0, 20);
            dataSeries.Append(10, 15);
            dataSeries.Append(20, 10);
            FastLineRenderableSeries flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Black;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 35);
            dataSeries.Append(20, 30);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Black;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 15);
            dataSeries.Append(20, 20);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Black;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 15);
            dataSeries.Append(20, 10);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Black;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

        }

#endif
    }
}
