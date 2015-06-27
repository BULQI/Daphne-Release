﻿using System;
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

namespace DaphneGui.CellLineage
{
    /// <summary>
    /// Interaction logic for CellLineageControl.xaml
    /// </summary>
    public partial class CellLineageControl : ToolWindow
    {
        private readonly Random _random = new Random();

        public CellLineageControl()
        {
            InitializeComponent();
        }

        private void LineageSciChart_Loaded(object sender, RoutedEventArgs e)
        {
            //var dataSeries = CreateDataSeries();
            CreateFakeDataSeries();

            //renderableLineSeries.DataSeries = dataSeries;

            //This draws the graph
            LineageSciChart.YAxis.VisibleRange = new DoubleRange(-1, 50);
            LineageSciChart.XAxis.VisibleRange = new DoubleRange(-1, 30);
            //LineageSciChart.ZoomExtentsX();
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
            marker.Height = 8; marker.Width = 8;
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
            flrs.SeriesColor = Colors.Green;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 35);
            dataSeries.Append(20, 30);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Green;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 15);
            dataSeries.Append(20, 20);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Green;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

            dataSeries = new XyDataSeries<double, double>();
            dataSeries.AcceptsUnsortedData = true;
            dataSeries.Append(10, 15);
            dataSeries.Append(20, 10);
            flrs = new FastLineRenderableSeries();
            flrs.DataSeries = dataSeries;
            flrs.SeriesColor = Colors.Green;
            flrs.PointMarker = marker;
            LineageSciChart.RenderableSeries.Add(flrs);

        }
    }
}
