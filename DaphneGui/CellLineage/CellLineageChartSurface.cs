/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
