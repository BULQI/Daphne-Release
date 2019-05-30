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
using ActiproSoftware.Windows.Controls.Docking;
using DaphneGui;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

using Daphne;
using DaphneUserControlLib;
using System.Threading;

namespace Workbench
{
    /// <summary>
    /// Interaction logic for ChartViewToolWindow.xaml
    /// </summary>
    public partial class ChartViewToolWindow : DocumentWindow
    {
        public Dictionary<string, List<double>> dictConcs = new Dictionary<string, List<double>>();
        public List<double> lTimes = new List<double>();
        private ReactionComplexChart Chart;
        public VatReactionComplex RC { get; set; }      //Simulation object - used to plot graph
        public Protocol protocol { get; set; }
        public MainWindow MW;       //handle to main window
        public bool redraw_flag;    //true if redraw and not creating a new chart

        /// <summary>
        /// Constructor
        /// </summary>
        public ChartViewToolWindow()
        {
            InitializeComponent();
            protocol = DataContext as Protocol;
            Chart = new ReactionComplexChart();
            Chart.panelRC = panelRC;
            Chart.ToolWin = this;
            redraw_flag = false;
            windowsFormsHost1.Width = Chart.Width;
            windowsFormsHost1.Height = Chart.Height;
            Chart.MouseUp += new System.Windows.Forms.MouseEventHandler(Chart_MouseUp);
        }

        /// <summary>
        /// This is called for initial draw and for redraws
        /// Tag is used to store a pointer to VatReactionComplex (from simulation side).  This is what is drawn.
        /// DataContext is Protocol. When the user changes mol concs or reac rates on right side, the changes are made on Config side.
        /// </summary>
        public void Render()
        {
            RC = Tag as VatReactionComplex;       
            protocol = DataContext as Protocol;

            if (protocol == null)
                return;

            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;

            if (redraw_flag == true)
            {
                Chart.UpdateSeries();
                Chart.RedrawSeries();
            }
            else
            {
                Chart.Clear();
                if (lTimes.Count > 0 && dictConcs.Count > 0)
                {
                    Chart.Initialize();
                    Chart.ListTimes = lTimes;
                    Chart.DictConcs = dictConcs;

                    btnIncSize.IsEnabled = true;
                    btnDecSize.IsEnabled = true;
                    btnLogX.IsEnabled = true;
                    btnLogY.IsEnabled = true;

                    Chart.Draw();
                }
            }
        }

        public void Reset()
        {
            lTimes.Clear();
            dictConcs.Clear();
            if (Chart != null) {
                //Chart.Clear();
                Chart.DrawBlank();
            }            
        }
        
        /// <summary>
        /// Zoom In
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnIncSize_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            System.Drawing.Size sz = Chart.Size;
            int w = sz.Width;
            int h = sz.Height;

            w = (int)(w * 1.1);
            h = (int)(h * 1.1);
            sz.Width = w;
            sz.Height = h;

            if (sz.Width > 1200 || sz.Height > 800)
                return;

            //resize underlying panel
            windowsFormsHost1.Width = w;
            windowsFormsHost1.Height = h;
            
            Chart.Size = sz;
            Chart.Draw();
        }

        /// <summary>
        /// Zoom Out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDecSize_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            System.Drawing.Size sz = Chart.Size;

            sz.Width = (int)(sz.Width * 0.9);
            sz.Height = (int)(sz.Height * 0.9);

            if (sz.Width < 300 || sz.Height < 200)
                return;

            windowsFormsHost1.Width = windowsFormsHost1.Width * 0.9;
            windowsFormsHost1.Height = windowsFormsHost1.Height * 0.9;            

            Chart.Size = sz;
            Chart.Draw();            
        }        

        /// <summary>
        /// This handler is called if user changes a reaction rate by slider or in text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dblReacRate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (MW == null || RC == null || lTimes.Count == 0 || dictConcs.Count == 0)
                return;

            if (e.PropertyName == "Number")
            {
                if (lTimes.Count > 1)
                {
                    if (Chart.Legends.Count > 0)    //this means graph is already created
                    {
                        redraw_flag = true;
                        MW.runSim(true);
                    }
                }
                else
                {
                    redraw_flag = false;
                    MW.runButton_Click(null, null);
                }
            }
        }

        /// <summary>
        /// Push button to toggle X-Axis between linear and logarithmic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnX_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            Chart.IsXLogarithmic = !Chart.IsXLogarithmic;
            if (Chart.IsXLogarithmic == true)
            {
                btnLogX.Content = "X-Axis: Logarithmic";
            }
            else
            {
                btnLogX.Content = "X-Axis: Linear";
            }
            Chart.Draw();
        }

        /// <summary>
        /// Push button to toggle Y-Axis between linear and logarithmic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnY_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            Chart.IsYLogarithmic = !Chart.IsYLogarithmic;
            if (Chart.IsYLogarithmic == true)
            {
                btnLogY.Content = "Y-Axis: Logarithmic";
            }
            else
            {
                btnLogY.Content = "Y-Axis: Linear";
            }
            Chart.Draw();
        }

        /// <summary>
        /// This handler is called if user changes a molecular concentration by slider or in text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dblConcs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (MW == null || RC == null || lTimes.Count == 0 || dictConcs.Count == 0)
                return;

            if (e.PropertyName == "Number")
            {
                if (lTimes.Count > 1)
                {
                    if (Chart.Legends.Count > 0)    //this means graph is already created
                    {
                        redraw_flag = true;
                        MW.runSim(true);
                    }
                }
                else
                {
                    redraw_flag = false;
                    MW.runButton_Click(null, null);
                }
            }
        }

        private void dgInitConcs_DragStarted(object sender, DragStartedEventArgs e)
        {
            MainWindow.SetControlFlag(MainWindow.CONTROL_MOUSE_DRAG, true);
        }

        private void dgInitConcs_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MainWindow.SetControlFlag(MainWindow.CONTROL_MOUSE_DRAG, false);
            ThreadPool.RegisterWaitForSingleObject(MW.runFinishedEvent,
                       new WaitOrTimerCallback(DelayedRunSim),
                       null, 50, true);

        }

        private void DelayedRunSim(object state, bool timedOut)
        {
            redraw_flag = true;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { MW.runSim(true); }), null);
        }

        private void Chart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (MainWindow.CheckControlFlag(MainWindow.CONTROL_MOUSE_DRAG) == true)
            {
                dgInitConcs_DragCompleted(null, null);
            }
        }
    }
}
