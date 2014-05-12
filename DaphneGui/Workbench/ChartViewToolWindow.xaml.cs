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

        public ObservableCollection<MolConcInfo> tester { get; set; }

        public ChartViewToolWindow()
        {
            InitializeComponent();
            chartSize = new System.Drawing.Size(500, 300);
            DataContext = RC;
            //ChartMainGrid.DataContext = RC;
            
            //txtTestBox.DataContext = RC;
            tester = new ObservableCollection<MolConcInfo>();

            
       
        }

        public void ClearChart()
        {
            if (cm == null)
                return;

            cm.ClearChart();
        }
        
        public void Render()
        {
            tester = RC.initConcs;

            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;
            //txtTest2.Text = RC.nTestVariable.ToString();

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
                //menu.MenuItems[4].Click += new System.EventHandler(this.btnChange_Click);

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
        private void btnChange_Click(object sender, EventArgs e)
        {
            //////////EditRCReactions er = new EditRCReactions(RC);

            //////////if (er.ShowDialog() == true)
            //////////{
            //////////}
        }

        private void btnRedraw_Click(object sender, RoutedEventArgs e)
        {
            foreach (MolConcInfo mci in RC.initConcs)
            {
                RC.EditConc(mci.molguid, mci.conc);
            }
            
            cm.RedrawSeries();
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
            cm.RedrawSeries();
        }
        
    }
}
