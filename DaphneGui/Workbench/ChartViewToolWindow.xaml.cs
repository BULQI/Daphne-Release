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
using ActiproSoftware.Windows.Controls.Docking;
using DaphneGui;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

using Daphne;

namespace Workbench
{
    /// <summary>
    /// Interaction logic for ChartViewToolWindow.xaml
    /// </summary>
    public partial class ChartViewToolWindow : DocumentWindow
    {
        public Dictionary<string, List<double>> dictConcs = new Dictionary<string, List<double>>();
        public List<double> lTimes = new List<double>();
        private ChartManager cm;
        private System.Drawing.Size chartSize;
        public ReactionComplexProcessor RC { get; set; }

        public ToggleButton toggleButton { get; set; }

        public ChartViewToolWindow()
        {
            InitializeComponent();
            chartSize = new System.Drawing.Size(700, 300);
            DataContext = RC;
        }

        public void ClearChart()
        {
            if (cm == null)
                return;

            cm.ClearChart();
        }
        
        public void Render()
        {
            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;

            if (lTimes.Count > 0 && dictConcs.Count > 0)
            {
                cm = new ChartManager(this, chartSize);
                cm.PChart = pChartMolConcs;
                
                cm.ListTimes = lTimes;
                cm.DictConcs = dictConcs;                

                cm.LabelX = "Time";
                cm.LabelY = "Concentration";
                cm.TitleXY = "Time Trajectory of Molecular Concentrations";
                cm.DrawLine = true;

                System.Windows.Forms.MenuItem[] menuItems = 
                {   
                    new System.Windows.Forms.MenuItem("Zoom in"),
                    new System.Windows.Forms.MenuItem("Zoom out"),
                    new System.Windows.Forms.MenuItem("Save Changes"),
                    new System.Windows.Forms.MenuItem("Discard Changes"),
                };

                System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(menuItems);
                cm.SetContextMenu(menu);
                menu.MenuItems[2].Click += new System.EventHandler(this.btnSave_Click);
                menu.MenuItems[3].Click += new System.EventHandler(this.btnDiscard_Click);

                btnIncSize.IsEnabled = true;
                btnDecSize.IsEnabled = true;
                btnDiscard.IsEnabled = true;
                btnSave.IsEnabled = true;

                cm.DrawChart();

                dgInitConcs.ItemsSource = RC.initConcs;
                dgReactionRates.ItemsSource = RC.CRC.ReactionRates;

            }
        }

        
        private void btnIncSize_Click(object sender, RoutedEventArgs e)
        {
            if (cm == null)
                return;

            System.Drawing.Size sz = cm.ChartSize;
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
            
            chartSize = sz;
            cm.ChartSize = sz;

            cm.DrawChart();
                       
        }

        private void btnDecSize_Click(object sender, RoutedEventArgs e)
        {
            if (cm == null)
                return;

            System.Drawing.Size sz = cm.ChartSize;

            sz.Width = (int)(sz.Width * 0.9);
            sz.Height = (int)(sz.Height * 0.9);

            if (sz.Width < 300 || sz.Height < 200)
                return;

            windowsFormsHost1.Width = windowsFormsHost1.Width * 0.9;
            windowsFormsHost1.Height = windowsFormsHost1.Height * 0.9;            

            chartSize = sz;
            cm.ChartSize = sz;
            cm.DrawChart();            
        }        

        private void btnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (RC == null)
                return;

            RC.RestoreOriginalConcs();
            RC.RestoreOriginalRateConstants();

            if (toggleButton != null)
            {
                //This causes a redraw
                toggleButton.IsChecked = true;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cm != null)
            {
                cm.SaveChanges();
            }
        }
        private void btnDiscard_Click(object sender, EventArgs e)
        {
            RC.RestoreOriginalConcs();
            RC.Go();
            cm.ListTimes = RC.ListTimes;
            cm.DictConcs = RC.DictGraphConcs;
            cm.DrawChart();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cm != null)
            {
                cm.SaveChanges();
            }
        }
                
        private void slConc_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = sender as Slider;
            if (!s.IsLoaded)
                return;

            if (e.OldValue == e.NewValue)
                return;

            foreach (MolConcInfo mci in RC.initConcs)
            {
                RC.EditConc(mci.molguid, mci.conc);
            }

            cm.RedrawSeries();
            cm.RecalculateYMax();
        }

        private void slRate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Slider s = sender as Slider;
            double m = s.Minimum;
        }

        private string GetNumerics(string input)
        {
            var sb = new StringBuilder();
            string goodChars = "0123456789.eE+-";
            foreach (var c in input)
            {                
                if (goodChars.IndexOf(c) >=0 )
                    sb.Append(c);
            }
            string output = sb.ToString();
            return output;
        }

        private void dblReacRate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                RC.UpdateRateConstants();
                RC.Sim.Load(MainWindow.SC.SimConfig, true, true);
                cm.RedrawSeries();
                cm.RecalculateYMax();
            }
        }

        private void btnX_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (cm == null)
                return;

            cm.IsXLogarithmic = !cm.IsXLogarithmic;
            cm.DrawChart();
        }

        private void btnY_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (cm == null)
                return;

            cm.IsYLogarithmic = !cm.IsYLogarithmic;
            cm.DrawChart();
        }

        private void dblConcs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                foreach (MolConcInfo mci in RC.initConcs)
                {
                    RC.EditConc(mci.molguid, mci.conc);
                }
                cm.RedrawSeries();
                cm.RecalculateYMax();
            }
        }

        private void dblMaxTime_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (RC != null && cm != null)
            {
                cm.RedrawSeries();
                cm.RecalculateYMax();
            }
        }

        private void btnRedraw_Click(object sender, RoutedEventArgs e)
        {
            foreach (MolConcInfo mci in RC.initConcs)
            {
                RC.EditConc(mci.molguid, mci.conc);
            }
            RC.UpdateRateConstants();
            cm.RedrawSeries();
            cm.RecalculateYMax();

            //This causes a refresh of the conc data grid
            UpdateGrids();
        }

        public void UpdateGrids()
        {
            dgInitConcs.ItemsSource = null;
            dgInitConcs.ItemsSource = RC.initConcs;
        }

        
    }
}
