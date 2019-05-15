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
using Abt.Controls.SciChart.Themes;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Abt.Controls.SciChart.Visuals.PointMarkers;
using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Visuals.Axes;
using System.Collections.ObjectModel;

using System.Windows.Forms.DataVisualization.Charting;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for CellPopDynToolWindow.xaml
    /// </summary>
    public partial class CellPopDynToolWindow : ToolWinBase
    {
        public CellPopDynToolWindow()
        {
            InitializeComponent();
        }

        private void plotButton_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.Plot(this, plotOptions);
        }

        /// <summary>
        /// Export the chart to a file. 
        /// Must have first plotted the chart on screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void plotExportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Png Image|*.png|Pdf Image|*.pdf|Tiff Image|*.tif|Xps Image|*.xps";
            dlg.Title = "Export to File";
            dlg.RestoreDirectory = true;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                //Set legend to show only visible series for export
                legendModifier.GetLegendDataFor = SourceMode.AllVisibleSeries;
                legendModifier.UpdateLegend();

                //Export to file
                if (dlg.FileName.EndsWith("pdf"))
                {
                    mySciChart.SaveCellPopDynToPdf(dlg.FileName);
                }
                else
                {
                    mySciChart.SaveToFile(dlg.FileName);
                }

                //Set legend back to show all series
                legendModifier.GetLegendDataFor = SourceMode.AllSeries;
                legendModifier.UpdateLegend();                
            }

        }

        /// <summary>
        /// Event handler called when user changes time units (minutes, hours, days, weeks).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeUnitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null || combo.SelectedIndex == -1)
                return;
            // Only want to respond to purposeful user interaction - so if initializing combo box, ignore                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count == 0)
                return;

            mySciChart.SetTimeUnits(combo.SelectedIndex);
            mySciChart.Plot(this, plotOptions);

        }

        /// <summary>
        /// User selected "zoom out" from right-click context menu on chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            mySciChart.ZoomExtents();            
        }

        private void CellPopDynWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //actionsToolWindow.Dock(this.actionsContainer, ActiproSoftware.Windows.Controls.Docking.Direction.Bottom);
            //To zoom in and out: \n  Use the mouse wheel\n or select a rectangular area.\n\nTo pan, right-click and drag.
            tbSurfaceTooltip.Text = "";
            tbSurfaceTooltip.AppendText("To zoom in:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use the mouse wheel OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Select a rectangular area.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To zoom out:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Double click OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Use mouse wheel OR");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Right-click + Zoom out.");

            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("To pan:");
            tbSurfaceTooltip.AppendText(Environment.NewLine);
            tbSurfaceTooltip.AppendText("    Press mouse wheel and drag.");
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {            
        }

        private bool allStatesChecked = false;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                DataGrid dg = plotOptions.deathStatesGrid;
                
                CellPopulation pop = (CellPopulation)(plotOptions.lbPlotCellPops.SelectedItem);
                ObservableCollection<bool> states = pop.Cell.death_driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {
                    states[i] = !allStatesChecked;
                }

                states = pop.Cell.div_scheme.Driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {                    
                    states[i] = !allStatesChecked;
                }

                states = pop.Cell.diff_scheme.Driver.plotStates;

                for (int i = 0; i < states.Count; i++)
                {
                    states[i] = !allStatesChecked;
                }

                allStatesChecked = !allStatesChecked;
            }
        }
    }
}
