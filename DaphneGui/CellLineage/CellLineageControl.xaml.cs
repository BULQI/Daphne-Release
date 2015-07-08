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

namespace DaphneGui.CellLineage
{
    /// <summary>
    /// Interaction logic for CellLineageControl.xaml
    /// </summary>
    public partial class CellLineageControl : ToolWindow
    {
        private readonly Random _random = new Random();
        public ObservableCollection<FounderInfo> FounderCells { get; set; }
        public ObservableCollection<GuiFounderInfo> FounderCellPops { get; set; }
        public ObservableCollection<GuiFounderInfo> FounderCellsByCellPop { get; set; }


        public CellLineageControl()
        {
            FounderCells = new ObservableCollection<FounderInfo>();
            FounderCellPops = new ObservableCollection<GuiFounderInfo>();
            FounderCellsByCellPop = new ObservableCollection<GuiFounderInfo>();

            InitializeComponent();

            DataContext = this;
        }
        
        private void LoadLineageData()
        {
            DataContext = this;

            Dictionary<int, FounderInfo> result = MainWindow.Sim.Reporter.ProvideFounderCells();

            if (result == null)
            {
                MessageBox.Show("Lineage reporting options not turned on.");
                return;
            }

            FounderCells.Clear();
            FounderCellPops.Clear();
            foreach (KeyValuePair<int, FounderInfo> kvp in result)
            {
                GuiFounderInfo gfi = new GuiFounderInfo();
                gfi.Lineage_Id = kvp.Value.Lineage_Id;
                gfi.Population_Id = kvp.Value.Population_Id;
                
                TissueScenario scenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                if (scenarioHandle.cellpopulation_dict.ContainsKey(kvp.Value.Population_Id) == true)
                {
                    gfi.Cell_Pop = scenarioHandle.cellpopulation_dict[kvp.Value.Population_Id];
                }

                FounderCells.Add(kvp.Value);
                FounderCellPops.Add(gfi);                
            }
            
        }

        private void cellPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1)
                return;

            CellPopulation pop = ((GuiFounderInfo)listbox.SelectedItem).Cell_Pop;
            if (listbox.SelectedIndex == -1)
                return;

            int cellPopId = pop.cellpopulation_id;
            FounderCellsByCellPop.Clear();
            List<GuiFounderInfo> listGFI = FounderCellPops.Where(x => x.Population_Id == cellPopId).ToList();
            foreach (GuiFounderInfo gfi in listGFI)
            {
                FounderCellsByCellPop.Add(gfi);
            }

        }

        private void founderCellListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (founderCellListBox.Items.Count == 0)
                return;
            if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;
            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            if (founderCellListBox.SelectedIndex >= 0) 
            {
                TissueScenario scenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                GuiFounderInfo gfi = founderCellListBox.SelectedItem as GuiFounderInfo;
                if (scenarioHandle.cellpopulation_dict.ContainsKey(gfi.Population_Id) == true)
                {
                    CellPopulation cp = scenarioHandle.cellpopulation_dict[gfi.Population_Id];
                    string name = cp.cellpopulation_name;
                    string cell_type = cp.Cell.CellName;
                    tbCellType.Text = cell_type;
                }
            }
        }

        
        private void LineageSciChart_Loaded(object sender, RoutedEventArgs e)
        {
            //Set up surface tooltip
            tbLineageSurfaceTooltip.Text = "";

            tbLineageSurfaceTooltip.AppendText("To zoom in and out:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Use the mouse wheel OR");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Select a rectangular area.");

            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("To zoom back out:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Double click OR");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Use mouse wheel.");

            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("To pan:");
            tbLineageSurfaceTooltip.AppendText(Environment.NewLine);
            tbLineageSurfaceTooltip.AppendText("    Press mouse wheel and drag.");

            LoadLineageData();
            //CreateFakeDataSeries();
            Size size = new Size(600.0, 400.0);
            this.RenderSize = size;
            LineageSciChart.YAxis.Visibility = Visibility.Hidden;
            ourYAxis.StrokeThickness = 0;
            ourYAxis.BorderBrush = Brushes.Transparent;

            //Hiding axes works well now - last thing was the surface's brush that needed to be hidden.
            LineageSciChart.BorderBrush = Brushes.Transparent;
            LineageSciChart.ZoomExtents();
        }

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

        //private void PlotLineageButton_Click(object sender, RoutedEventArgs e)
        //{
        //    double w = this.Width;
        //    double h = this.Height;

        //    GuiFounderInfo gfi = founderCellListBox.SelectedItem as GuiFounderInfo;

        //    int index = founderCellListBox.SelectedIndex;

        //    PedigreeAnalysis pda = new PedigreeAnalysis();
        //    List<Series> s = pda.GetPedigreeTreeSeries(FounderCells[index]);
 
        //    //last 3 things are the chart tile, x-axis and y-axis titles.
        //    string chartTitle=pda.GetChartTitle();
        //    string XTitle=pda.GetChartXTitle();
        //    string YTitle=pda.GetChartYTitle();

        //}

        private void drawButton_Click(object sender, RoutedEventArgs e)
        {
            Draw();
        }

        private void lineageExportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Draw()
        {
            int index = founderCellListBox.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("Please select a founder cell first.", "Cell lineage error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PedigreeAnalysis pda = new PedigreeAnalysis();
            pda.SetReport(MainWindow.Sim.Reporter);
            List<Series> s = pda.GetPedigreeTreeSeries(FounderCells[index]);

            //last 3 things are the chart tile, x-axis and y-axis titles.
            string chartTitle = pda.GetChartTitle();
            string XTitle = pda.GetChartXTitle();
            string YTitle = pda.GetChartYTitle();

            ConvertAndDraw(chartTitle, XTitle, YTitle, s);
        }

        private void ConvertAndDraw(string chartTitle, string xTitle, string yTitle, List<Series> series)
        {
            foreach(Series s in series) 
            {
                var dataSeries = new XyDataSeries<double, double>();
                dataSeries.SeriesName = "";
                EllipsePointMarker marker = new EllipsePointMarker();
                marker.Height = 12; marker.Width = 12;
                marker.Fill = Colors.Red;
                marker.Stroke = Colors.Lavender;
                marker.StrokeThickness = 1;

                double[] x;
                double[] y;

                List<double> tempX = new List<double>();
                List<double> tempY = new List<double>();
                tempX.Add(s.Points[0].XValue);
                tempX.Add(s.Points[1].XValue);
                tempY.Add(s.Points[0].YValues[0]);
                tempY.Add(s.Points[1].YValues[0]);
                x = tempX.ToArray();
                y = tempY.ToArray();

                dataSeries.Append(x, y);

                FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                flrs.DataSeries = dataSeries;
                flrs.SeriesColor = Colors.Black;
                flrs.PointMarker = marker;

                LineageSciChart.RenderableSeries.Add(flrs);

                //This is how to add Annotations - Do not delete this
                ////var textAnnot = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation()
                ////{
                ////    Name = s.Name,
                ////    Text = string.Format("({0:G4}, {1:G2})", x[0], y[0]),
                ////    X1 = x[0],
                ////    Y1 = y[0],
                ////};

                ////LineageSciChart.Annotations.Add(textAnnot);

                //Sample code
                //Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation ta = new Abt.Controls.SciChart.Visuals.Annotations.TextAnnotation();
                //LineageSciChart.Annotations.Add(ta);
            }

            LineageSciChart.ChartTitle = chartTitle;
            LineageSciChart.XAxis.AxisTitle = xTitle;
            LineageSciChart.YAxis.AxisTitle = yTitle;
            LineageSciChart.BorderBrush = Brushes.Transparent;
            LineageSciChart.ZoomExtents();
        }

        
    }

    public class GuiFounderInfo
    {
        public BigInteger Lineage_Id { get; set; }
        public int Population_Id { get; set; }
        public CellPopulation Cell_Pop { get; set; }
    }
}
