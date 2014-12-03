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
        public ConfigReactionComplex CRC { get; set; }  //ConfigReactionComplex object - used for gui display and changes and not graph data
        public MainWindow MW;       //handle to main window
        public bool redraw_flag;    //true if redraw and not creating a new chart


        public ChartViewToolWindow()
        {
            InitializeComponent();
            //chartSize = new System.Drawing.Size(700, 300);
            CRC = DataContext as ConfigReactionComplex;
            Chart = new ReactionComplexChart();
            Chart.panelRC = panelRC;
            Chart.ToolWin = this;
            redraw_flag = false;
            windowsFormsHost1.Width = Chart.Width;
            windowsFormsHost1.Height = Chart.Height;
            Chart.MouseUp += new System.Windows.Forms.MouseEventHandler(Chart_MouseUp);
        }

        public void Render()
        {
            RC = Tag as VatReactionComplex;
            CRC = DataContext as ConfigReactionComplex;

            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;

            if (redraw_flag == true)
            {
                Chart.RedrawSeries();
            }
            else
            {
                Chart.Clear();
                if (lTimes.Count > 0 && dictConcs.Count > 0)
                {
                    Chart.ListTimes = lTimes;
                    Chart.DictConcs = dictConcs;

                    Chart.LabelX = "Time";
                    Chart.LabelY = "Concentration";
                    Chart.TitleXY = "Time Trajectory of Molecular Concentrations";
                    Chart.DrawLine = true;

                    System.Windows.Forms.MenuItem[] menuItems = 
                    {   
                        new System.Windows.Forms.MenuItem("Zoom in"),
                        new System.Windows.Forms.MenuItem("Zoom out"),
                        new System.Windows.Forms.MenuItem("Save Changes"),
                        new System.Windows.Forms.MenuItem("Discard Changes"),
                    };

                    System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(menuItems);
                    Chart.SetContextMenu(menu);

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
            if (Chart != null) {
                Chart.Clear();
            }

        }
        
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
            
            //chartSize = sz;
            Chart.Size = sz;

            Chart.Draw();
                       
        }

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

            //chartSize = sz;
            Chart.Size = sz;
            Chart.Draw();            
        }        

        //////KEEP THIS
        //////private void btnDiscard_Click(object sender, RoutedEventArgs e)
        //////{
        //////    if (RC == null)
        //////        return;

        //////    RC.RestoreOriginalConcs();
        //////    //Fix this
        //////    ////RC.RestoreOriginalRateConstants();

        //////    //if (toggleButton != null)
        //////    //{
        //////    //    //This causes a redraw
        //////    //    toggleButton.IsChecked = true;
        //////    //}
        //////}


        //////KEEP THIS
        ////private void btnSave_Click(object sender, RoutedEventArgs e)
        ////{
        ////    if (Chart != null)
        ////    {
        ////        Chart.SaveChanges();
        ////    }
        ////}
        //private void btnDiscard_Click(object sender, EventArgs e)
        //{
        //    ////HERE JUST NEED TO COPY FROM PROTOCOL TO ENTITY!!

        //    //RC.RestoreOriginalConcs();
        //    //RC.RunForward();
        //    //Chart.ListTimes = RC.ListTimes;
        //    //Chart.DictConcs = RC.DictGraphConcs;
        //    //Chart.DrawChart();
        //}

        //private void btnSave_Click(object sender, EventArgs e)
        //{
        //    //if (Chart != null)
        //    //{
        //    //    Chart.SaveChanges();
        //    //}
        //}

        /// <summary>
        /// This handler is called if user changes a reaction rate by slider or in text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dblReacRate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                redraw_flag = true;
                MW.runSim();
                //MW.runButton_Click(null, null);
            }
        }

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
            if (e.PropertyName == "Number")
            {
                redraw_flag = true;
                MW.runSim();
                //MW.runButton_Click(null, null);
            }
        }

        private void btnSaveReport_Click(object sender, RoutedEventArgs e)
        {

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
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { MW.runSim(); }), null);
            Console.WriteLine("last run fired");
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
