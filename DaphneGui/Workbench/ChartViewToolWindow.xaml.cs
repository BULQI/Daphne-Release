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
        public ReactionComplexProcessor RC;

        public ToggleButton toggleButton { get; set; }
        private bool dragging = false;

        public ChartViewToolWindow()
        {
            InitializeComponent();
            chartSize = new System.Drawing.Size(500, 300);
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
                    //new System.Windows.Forms.MenuItem("Rate Constants...")
                    //new System.Windows.Forms.MenuItem("View Initial Concentrations...") 
                };

                System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(menuItems);
                cm.SetContextMenu(menu);
                menu.MenuItems[0].Click += new System.EventHandler(this.btnIncSize_Click);
                menu.MenuItems[1].Click += new System.EventHandler(this.btnDecSize_Click);                
                menu.MenuItems[2].Click += new System.EventHandler(this.btnSave_Click);
                menu.MenuItems[3].Click += new System.EventHandler(this.btnDiscard_Click);

                btnIncSize.IsEnabled = true;
                btnDecSize.IsEnabled = true;
                btnDiscard.IsEnabled = true;
                btnSave.IsEnabled = true;

                cm.DrawChart();

                dgInitConcs.ItemsSource = RC.initConcs;
                dgReactionRates.ItemsSource = RC.ReactionsInComplex;

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
        private void btnIncSize_Click(object sender, EventArgs e)
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

            windowsFormsHost1.Width = windowsFormsHost1.Width * 1.1;
            windowsFormsHost1.Height = windowsFormsHost1.Height * 1.1;

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
        private void btnDecSize_Click(object sender, EventArgs e)
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

        private void slMaxTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RC != null && cm != null && RC.dInitialTime != slMaxTime.Value)
            {
                //RC.MaxTime = (int)slMaxTime.Value;
                RC.dInitialTime = slMaxTime.Value;
                cm.RedrawSeries();                      
            }
        }

        

        private void btnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (RC == null)
                return;

            RC.RestoreOriginalConcs();
            RC.Go();

            if (cm == null)
                return;

            cm.ListTimes = RC.ListTimes;
            cm.DictConcs = RC.DictGraphConcs;
            cm.DrawChart();
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
        
        private void btnRedraw_Click(object sender, RoutedEventArgs e)
        {
            if (toggleButton != null)
                toggleButton.IsChecked = true;
        }

        private void slConc_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (MolConcInfo mci in RC.initConcs)
            {
                RC.EditConc(mci.molguid, mci.conc);
            }

            cm.RedrawSeries();
            cm.RecalculateYMax();
        }

        private void slRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sl = sender as Slider;

            if (dragging == false)
            {
                sl.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(slRate_MouseLeftButtonDown), true);
                sl.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(slRate_MouseLeftButtonUp), true);
            }
        }

        private void slRate_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Slider sl = sender as Slider;

            if (toggleButton != null && dragging == true)
            {
                dragging = false;

                //This causes a redraw
                toggleButton.IsChecked = true;
            }
        }

        private void slRate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
        }

        private void txtFormattedValue_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            ConfigReaction reac = (ConfigReaction)tb.DataContext;
            tb.Text = reac.daph_rate_const.Value.ToString();
        }

        private void txtFormattedValue_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string token = tb.Text;
            token = GetNumerics(token);

            double d = double.Parse(token);

            ConfigReaction reac = (ConfigReaction)tb.DataContext;
            reac.daph_rate_const.Value = d;
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
        
    }
}
