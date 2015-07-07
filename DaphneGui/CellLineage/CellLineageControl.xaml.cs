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
using Daphne;

using System.Numerics;
using System.Collections.ObjectModel;

namespace DaphneGui.CellLineage
{
    /// <summary>
    /// Interaction logic for CellLineageControl.xaml
    /// </summary>
    public partial class CellLineageControl : ToolWindow
    {
        private readonly Random _random = new Random();
        public ObservableCollection<FounderInfo> FounderCells { get; set; }

        public CellLineageControl()
        {
            FounderCells = new ObservableCollection<FounderInfo>();
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
            foreach (KeyValuePair<int, FounderInfo> kvp in result)
            {
                ////BigInteger bigCellId = kvp.Value.Lineage_Id;   // ((FounderInfo)kvp.Value).
                //////founderCellListBox.Items.Add(bigCellId.ToString());
                ////founderCellListBox.Items.Add(kvp.Value);
                ////founderCellListBox.DisplayMemberPath = "Lineage_Id";

                FounderCells.Add(kvp.Value);
                TissueScenario scenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                if (scenarioHandle.cellpopulation_dict.ContainsKey(kvp.Value.Population_Id) == true)
                {
                    CellPopulation cp = scenarioHandle.cellpopulation_dict[kvp.Value.Population_Id];
                    string name = cp.cellpopulation_name;
                }

                //TissueScenario scenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                //if (scenarioHandle.cellpopulation_dict.ContainsKey(kvp.Value.Population_Id) == true) {
                //    CellPopulation cp = scenarioHandle.cellpopulation_dict[kvp.Value.Population_Id];
                //    string name = cp.cellpopulation_name;
                //    string cell_type = cp.Cell.CellName;
                //    tbCellPopName.Text = name;
                //    tbCellType.Text = cell_type;
                //}
            }
            
        }

        private void founderCellListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;
            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            TissueScenario scenarioHandle = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            FounderInfo fi = founderCellListBox.SelectedItem as FounderInfo;
            if (scenarioHandle.cellpopulation_dict.ContainsKey(fi.Population_Id) == true)
            {
                CellPopulation cp = scenarioHandle.cellpopulation_dict[fi.Population_Id];
                string name = cp.cellpopulation_name;
                string cell_type = cp.Cell.CellName;
                tbCellPopName.Text = name;
                tbCellType.Text = cell_type;
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

        private void PlotLineageButton_Click(object sender, RoutedEventArgs e)
        {
            double w = this.Width;
            double h = this.Height;
        }

        private void drawButton_Click(object sender, RoutedEventArgs e)
        {
            Draw();
        }

        private void lineageExportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Draw()
        {

        }

        
    }
}
