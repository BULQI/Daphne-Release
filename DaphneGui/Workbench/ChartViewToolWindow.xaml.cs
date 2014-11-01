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
using DaphneUserControlLib;
using DaphneGui;

namespace Workbench
{
    /// <summary>
    /// Interaction logic for ChartViewToolWindow.xaml
    /// </summary>
    public partial class ChartViewToolWindow : DocumentWindow
    {
        public Dictionary<string, List<double>> dictConcs = new Dictionary<string, List<double>>();
        public List<double> lTimes = new List<double>();
        private ChartManager ChartOld;
        private ReactionComplexChart Chart;
        private System.Drawing.Size chartSize;
        public VatReactionComplex RC { get; set; }
        public MainWindow MW;

        public ChartViewToolWindow()
        {
            InitializeComponent();
            chartSize = new System.Drawing.Size(700, 300);
            //RC = DataContext; // as VatReactionComplex;
            ChartOld = new ChartManager(this, chartSize);
            ChartOld.panelRC = panelRC;

            Chart = new ReactionComplexChart();
            Chart.panelRC = panelRC;
        }

        //public void ClearChart()
        //{
        //    if (Chart == null)
        //        return;

        //    Chart.ClearChart();
        //}
        
#if OLD_RC
        public void RenderOld()
        {
            RC = Tag as VatReactionComplex;

            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;

            //////TEMP CODE FOR TESTING
            ////List<Reaction> mylist = VatReactionComplex.dataBasket.Environment.Comp.BulkReactions;
            ////List<double> templist = dictConcs.First().Value;
            ////double[] val = new double[3] { 0,0,0 };

            ////int n = 0;
            ////foreach (KeyValuePair<string, List<double>> kvp in dictConcs)
            ////{
            ////    val[n] = kvp.Value[0];
            ////    kvp.Value.Clear();
            ////    n++;
            ////}

            ////n = 0;
            ////foreach (KeyValuePair<string, List<double>> kvp in dictConcs)
            ////{
            ////    //val = 1 + n;
            ////    double delta = 0;
            ////    for (int i = 0; i < 100; i++)
            ////    {
            ////        delta = delta + i/100.0;
            ////        val[n] = val[n] + delta;
            ////        if (val[n] <= 0)
            ////            val[n] = 0.1;
            ////        kvp.Value.Add(val[n]);
            ////    }
            ////    n++;
            ////}
            //////END TEST CODE

            if (lTimes.Count > 0 && dictConcs.Count > 0)
            {
                //Chart = new ChartManager(this, chartSize);
                //Chart.PChart = pChartMolConcs;

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
                menu.MenuItems[2].Click += new System.EventHandler(this.btnSave_Click);
                menu.MenuItems[3].Click += new System.EventHandler(this.btnDiscard_Click);

                btnIncSize.IsEnabled = true;
                btnDecSize.IsEnabled = true;
                btnDiscard.IsEnabled = true;
                btnSave.IsEnabled = true;

                Chart.DrawChart();

                //dgInitConcs.ItemsSource = RC.initConcs;
#if OLD_RC
                dgReactionRates.ItemsSource = RC.CRC.ReactionRates;
#endif

            }
        }
#endif

        public void Render()
        {
            RC = Tag as VatReactionComplex;

            lTimes = RC.ListTimes;
            dictConcs = RC.DictGraphConcs;

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
                //menu.MenuItems[2].Click += new System.EventHandler(this.btnSave_Click);
                //menu.MenuItems[3].Click += new System.EventHandler(this.btnDiscard_Click);

                btnIncSize.IsEnabled = true;
                btnDecSize.IsEnabled = true;
                btnDiscard.IsEnabled = true;
                btnSave.IsEnabled = true;

                Chart.Draw();
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
            
            chartSize = sz;
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

            chartSize = sz;
            Chart.Size = sz;
            Chart.Draw();            
        }        

        ////private void btnDiscard_Click(object sender, RoutedEventArgs e)
        ////{
        ////    if (RC == null)
        ////        return;

        ////    RC.RestoreOriginalConcs();
        ////    //Fix this
        ////    ////RC.RestoreOriginalRateConstants();

        ////    //if (toggleButton != null)
        ////    //{
        ////    //    //This causes a redraw
        ////    //    toggleButton.IsChecked = true;
        ////    //}
        ////}

        //private void btnSave_Click(object sender, RoutedEventArgs e)
        //{
        //    if (Chart != null)
        //    {
        //        Chart.SaveChanges();
        //    }
        //}
        private void btnDiscard_Click(object sender, EventArgs e)
        {
            ////HERE JUST NEED TO COPY FROM PROTOCOL TO ENTITY!!

            //RC.RestoreOriginalConcs();
            //RC.RunForward();
            //Chart.ListTimes = RC.ListTimes;
            //Chart.DictConcs = RC.DictGraphConcs;
            //Chart.DrawChart();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //if (Chart != null)
            //{
            //    Chart.SaveChanges();
            //}
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
                //Fix this
                RC.EditConc(mci.molguid, mci.conc);
            }

            Chart.RedrawSeries();
            Chart.RecalculateYMax();
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
                //Fix this
                ////RC.UpdateRateConstants();
                ////RC.Sim.Load(MainWindow.SOP.Protocol, true);
                Chart.RedrawSeries();
                Chart.RecalculateYMax();
            }
        }

        private void btnX_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            Chart.IsXLogarithmic = !Chart.IsXLogarithmic;
            Chart.Draw();
        }

        private void btnY_Axis_Click(object sender, RoutedEventArgs e)
        {
            if (Chart == null)
                return;

            Chart.IsYLogarithmic = !Chart.IsYLogarithmic;
            Chart.Draw();
        }

        private void dblConcs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                //ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)dgInitConcs.SelectedItem;
                //((MolPopHomogeneousLevel)(cmp.mp_distribution)).concentration = ((DoublesBox)sender).Number;
                foreach (MolConcInfo mci in RC.initConcs)
                {
                    //Fix this
                    RC.EditConc(mci.molguid, mci.conc);
                }
                RC.Load(MainWindow.SOP.Protocol, true);

                Chart.RedrawSeries();
                Chart.RecalculateYMax();
                //MW.runSim();
                
            }
        }

        private void dblMaxTime_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                if (RC != null && Chart != null)
                {
                    Chart.RedrawSeries();
                    Chart.RecalculateYMax();
                }
            }
        }

        private void btnRedraw_Click(object sender, RoutedEventArgs e)
        {
            foreach (MolConcInfo mci in RC.initConcs)
            {
                //Fix this
                ////RC.EditConc(mci.molguid, mci.conc);
            }

            //Fix this
            ////RC.UpdateRateConstants();
            Chart.RedrawSeries();
            Chart.RecalculateYMax();

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
