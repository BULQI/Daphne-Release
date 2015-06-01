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
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation pop = plotOptions.lbPlotCellPops.SelectedItem as CellPopulation;
            CellPopulationDynamicsData data = MainWindow.Sim.Reporter.ProvideCellPopulationDynamicsData(pop);

            // Add some data series of type X=double, Y=double
            var dataSeries0 = new XyDataSeries<double, double> { SeriesName = "Curve A" };
            var dataSeries1 = new XyDataSeries<double, double> { SeriesName = "Curve B" };
            var dataSeries2 = new XyDataSeries<double, double> { SeriesName = "Curve C" };
            var dataSeries3 = new XyDataSeries<double, double> { SeriesName = "Curve D" };

            var data1 = GetStraightLine(1000, 1.0, 10);
            var data2 = GetStraightLine(2000, 1.0, 10);
            var data3 = GetStraightLine(3000, 1.0, 10);
            var data4 = GetStraightLine(4000, 1.0, 10);
        }

        public DoubleSeries GetStraightLine(double gradient, double yIntercept, int pointCount)
        {
            var doubleSeries = new DoubleSeries(pointCount);

            for (int i = 0; i <= pointCount; i++)
            {
                double x = i + 1;
                double y = gradient * x + yIntercept;
                doubleSeries.Add(new XYPoint() { X = x, Y = y });
            }

            return doubleSeries;
        }
    }
}
