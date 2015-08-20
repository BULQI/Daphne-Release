using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Abt.Controls.SciChart.Visuals;
using Abt.Controls.SciChart;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms.DataVisualization.Charting;
using Abt.Controls.SciChart.Model.DataSeries;
using Abt.Controls.SciChart.Visuals.PointMarkers;
using System.Windows.Media;
using Abt.Controls.SciChart.Visuals.RenderableSeries;

using Daphne;
using Abt.Controls.SciChart.Visuals.Axes;

namespace DaphneGui.CellLineage
{
    public class CellLineageChartSurface : CellPopChartSurface
    {
        public CellLineageChartSurface()
        {
        }

        /// <summary>
        /// This sets the scale factor given the time units the user selected.
        /// Also sets x axis label to be selected time units.
        /// </summary>
        /// <param name="selIndex"></param>
        public void SetTimeUnits(int selIndex)
        {
            //if out of bounds, do not change
            if (selIndex < 0 || selIndex >= xScaleValues.Length)
                return;

            XScale = xScaleValues[selIndex];
            XAxisLabel = xAxisLabels[selIndex];
            XAxis.AxisTitle = XAxisLabel;
        }
    }
}
