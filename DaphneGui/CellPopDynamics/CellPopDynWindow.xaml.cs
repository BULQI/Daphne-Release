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
using System.Collections.ObjectModel;
using Abt.Controls.SciChart.Themes;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Abt.Controls.SciChart.Visuals.PointMarkers;
using Newtonsoft.Json;
using System.IO;
using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Visuals.Axes;
//using ActiproSoftware.Windows.Controls.Docking;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynWindow.xaml
    /// </summary>
    public partial class CellPopDynWindow : Window
    {
        private Dictionary<int, Color> lineColors;
        private int NextColorIndex = 0;

        public CellPopDynWindow()
        {
            InitializeComponent();
            DataContext = this;

            lineColors = new Dictionary<int, Color>();
            lineColors.Add(0, Colors.Blue);
            lineColors.Add(1, Colors.Red);
            lineColors.Add(2, new Color{A=255, R=8, G=251, B=3});   //bright green
            lineColors.Add(3, Colors.Magenta);
            lineColors.Add(4, Colors.Cyan);
            lineColors.Add(5, Colors.Black);
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
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
            //if (pop.Cell.NoPlotStatesSelected() == true)
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

            mySciChart.RenderableSeries.Clear();

            DrawStates(pop.Cell, pop.Cell.death_driver, data, CellPopulationDynamicsData.State.DEATH);
            DrawStates(pop.Cell, pop.Cell.diff_scheme.Driver, data, CellPopulationDynamicsData.State.DIFF);
            DrawStates(pop.Cell, pop.Cell.div_scheme.Driver, data, CellPopulationDynamicsData.State.DIV);

            //This draws the graph
            mySciChart.ZoomExtents();
            return;

#if false
            //Tried to serialize the data so would not have to "run" repeatedly, but this did not work yet - will try again

            ////var Settings = new JsonSerializerSettings();
            ////Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            ////Settings.TypeNameHandling = TypeNameHandling.Auto;

            ////string jsonSpec;
            ////string jsonFile = "C:\\TEMP\\GraphData.json";
            ////try 
            ////{
            ////    //serialize Protocol
            ////    jsonSpec = JsonConvert.SerializeObject(dynGraph.mySciChart.RenderableSeries, Newtonsoft.Json.Formatting.Indented, Settings);
            ////    File.WriteAllText(jsonFile, jsonSpec);
            ////}
            ////catch(Exception ex)
            ////{
            ////    MessageBox.Show("CellPopDynWindow Serialize failed: " + jsonFile + "  " + ex.Message);
            ////}
#endif

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

                    //convert to trimmed list - plot every interval'th item and make it a double - SHOULD BE INT!
                    List<double> dSeries = new List<double>();
                    List<double> dTimes = new List<double>();

                    for (int j = 0; j < series.Count; j++)
                    {
                            dSeries.Add(series[j]);
                            dTimes.Add(data.Times[j]);
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

                    mySciChart.RenderableSeries.Add(flrs);
                }
            }
        }

        private void plotTester_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not yet implemented.", "Plotting Tester", MessageBoxButton.OK, MessageBoxImage.Information);

#if false
            string jsonFile = "C:\\TEMP\\GraphData.json";

            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize
            string readText = File.ReadAllText(jsonFile);

            try
            {
                ObservableCollection<IRenderableSeries> renderableSeries = JsonConvert.DeserializeObject<ObservableCollection<IRenderableSeries>>(readText, settings);
                dynGraph.mySciChart.RenderableSeries = renderableSeries;
                
            }
            catch
            {
                MessageBox.Show("CellPopDynWindow DeserializeObject failed: " + jsonFile);
                return;
            }

            //CellPopDynamicsGraph graph = JsonConvert.DeserializeObject<CellPopDynamicsGraph>(readText, settings);
            //dynGraph = graph;

            dynGraph.mySciChart.ZoomExtents();
#endif
            
        }

        private void plotExportButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not yet implemented.", "Export Plot", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
