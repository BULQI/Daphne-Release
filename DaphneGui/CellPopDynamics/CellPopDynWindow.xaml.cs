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

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynWindow.xaml
    /// </summary>
    public partial class CellPopDynWindow : Window
    {
        public CellPopDynWindow()
        {
            InitializeComponent();
            DataContext = this;
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

            //If no states are selected for plotting, inform user
            if (!pop.Cell.death_driver.plotStates.Contains(true) &&
                  !pop.Cell.diff_scheme.Driver.plotStates.Contains(true) &&
                  !pop.Cell.div_scheme.Driver.plotStates.Contains(true))
            {
                MessageBox.Show("No states have been selected for plotting. Please run the simulation before running the analysis.", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Get the dynamics data for this cell pop - if null, that means the simulation has not been run - inform user
            CellPopulationDynamicsData data = MainWindow.Sim.Reporter.ProvideCellPopulationDynamicsData(pop);
            if (data == null) 
            {
                MessageBox.Show("There is no data to plot. Please run the simulation before running the analysis.", "Plotting Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return; 
            }

            //*********************************************************
            //If we get here, then we have the data and we can plot it.
            //*********************************************************

            int interval = data.Times.Count / 100;      //arbitrary number of points to graph so we don't graph all points - see reaction complex and see what we do there
            if (interval <= 0) interval = 1;            //cannot be zero because it is used as a divisor below
            
            int pointsToGraph = 0;

            //death driver
            ////NEED TO CHANGE Y VALUES FROM DOUBLE TO INT!
            for (int i = 0; i < pop.Cell.death_driver.states.Count; i++)
            {
                if (pop.Cell.death_driver.plotStates[i] == true) // states and plotStates are parallel lists
                {
                    List<int> series = data.GetState(CellPopulationDynamicsData.State.DEATH, i); // the first parameter is an enum
                    // can now plot series against data.Times; they are also parallel lists, i.e. times[j] belongs to series[j] – they form a point

                    //convert to trimmed list - plot every interval'th item and make it a double - SHOULD BE INT!
                    List<double> dSeries = new List<double>();
                    List<double> dTimes = new List<double>();
                    pointsToGraph = 0;

                    for (int j = 0; j < series.Count; j++)
                    {
                        //skip graphing points unless current index modulus interval = 0
                        if (j % interval == 0)
                        {
                            dSeries.Add(series[j]);
                            dTimes.Add(data.Times[j]);
                            pointsToGraph++;
                        }
                    }

                    var newSeries = new XyDataSeries<double, double> { SeriesName = pop.Cell.death_driver.states[i] };
                    newSeries.Append(dTimes, dSeries);

                    FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                    flrs.SeriesColor = Colors.Red;
                    //flrs.PointMarker = new TrianglePointMarker { Fill = Colors.Green };
                    flrs.DataSeries = newSeries;

                    mySciChart.RenderableSeries.Add(flrs);
                }
            }

            //diff driver
            for (int i = 0; i < pop.Cell.diff_scheme.Driver.states.Count; i++)
            {
                //pop.Cell.diff_scheme.Driver.plotStates[i] = true;  //For testing only - selects all states
                if (pop.Cell.diff_scheme.Driver.plotStates[i] == true) // states and plotStates are parallel lists
                {
                    List<int> series = data.GetState(CellPopulationDynamicsData.State.DIFF, i); // the first parameter is an enum
                    // can now plot series against data.Times; they are also parallel lists, i.e. times[j] belongs to series[j] – they form a point

                    //convert to trimmed list - plot every interval'th item and make it a double - SHOULD BE INT!
                    List<double> dSeries = new List<double>();
                    List<double> dTimes = new List<double>();
                    pointsToGraph = 0;

                    for (int j = 0; j < series.Count; j++ )
                    {
                        //skip graphing points unless current index modulus interval = 0
                        if (j % interval == 0)
                        {
                            dSeries.Add(series[j]);
                            dTimes.Add(data.Times[j]);
                            pointsToGraph++;
                        }
                    }

                    var newSeries = new XyDataSeries<double, double> { SeriesName = pop.Cell.diff_scheme.Driver.states[i] };

                    newSeries.Append(dTimes, dSeries);
                    ////allSeries.Add(newSeries);

                    FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                    flrs.SeriesColor = Colors.Green;
                    //flrs.PointMarker = new EllipsePointMarker { Fill = Colors.Red };
                    flrs.DataSeries = newSeries;

                    mySciChart.RenderableSeries.Add(flrs);
                }
                

            }

            // div driver
            for (int i = 0; i < pop.Cell.div_scheme.Driver.states.Count; i++)
            {
                //pop.Cell.div_scheme.Driver.plotStates[i] = true;  //For testing only - selects all states
                if (pop.Cell.div_scheme.Driver.plotStates[i] == true) // states and plotStates are parallel lists
                {
                    List<int> series = data.GetState(CellPopulationDynamicsData.State.DIV, i); // the first parameter is an enum
                    // can now plot series against data.Times; they are also parallel lists, i.e. times[j] belongs to series[j] – they form a point

                    //convert to trimmed list - plot every interval'th item and make it a double - SHOULD BE INT!
                    List<double> dSeries = new List<double>();
                    List<double> dTimes = new List<double>();
                    pointsToGraph = 0;

                    for (int j = 0; j < series.Count; j++)
                    {
                        //skip graphing points unless current index modulus interval = 0
                        if (j % interval == 0)
                        {
                            dSeries.Add(series[j]);
                            dTimes.Add(data.Times[j]);
                            pointsToGraph++;
                        }
                    }

                    var newSeries = new XyDataSeries<double, double> { SeriesName = pop.Cell.div_scheme.Driver.states[i] };
                    newSeries.Append(dTimes, dSeries);

                    FastLineRenderableSeries flrs = new FastLineRenderableSeries();
                    flrs.SeriesColor = Colors.Blue;
                    //flrs.PointMarker = new SquarePointMarker { Fill = Colors.Yellow };
                    flrs.DataSeries = newSeries;

                    mySciChart.RenderableSeries.Add(flrs);
                }
            }

            //This draws the graph
            mySciChart.ZoomExtents();


            //Tried to serialize the data so would not have to "run" repeatedly, but this did not work - will try again

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
