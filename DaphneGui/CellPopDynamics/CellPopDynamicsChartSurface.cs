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

        private Dictionary<int, Color> lineColors;
        private int NextColorIndex = 0;

        public CellPopDynamicsChartSurface()
        {
            XAxisLabel = "Time in minutes";
            XScale = 1.0;

            lineColors = new Dictionary<int, Color>();
            lineColors.Add(0, Colors.Blue);
            lineColors.Add(1, Colors.Red);
            lineColors.Add(2, new Color { A = 255, R = 8, G = 251, B = 3 });   //bright green
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

    }
}
