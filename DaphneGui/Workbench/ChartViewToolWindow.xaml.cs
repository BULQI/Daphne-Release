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

        ////For testing
        //public static byte TestMethod;
        //public static byte TEST_BY_CONFIG = 0,
        //                   TEST_BY_SIM = 1;

        public ChartViewToolWindow()
        {
            InitializeComponent();
            //chartSize = new System.Drawing.Size(700, 300);
            CRC = DataContext as ConfigReactionComplex;
            Chart = new ReactionComplexChart();
            Chart.panelRC = panelRC;
            Chart.ToolWin = this;
            redraw_flag = false;

            //TestMethod = ChartViewToolWindow.TEST_BY_CONFIG;


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
                    //menu.MenuItems[2].Click += new System.EventHandler(this.btnSave_Click);
                    //menu.MenuItems[3].Click += new System.EventHandler(this.btnDiscard_Click);

                    btnIncSize.IsEnabled = true;
                    btnDecSize.IsEnabled = true;
                    //btnDiscard.IsEnabled = true;
                    //btnSave.IsEnabled = true;

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
        
#if OLD_RC
        //DELETE?
        //private string GetNumerics(string input)
        //{
        //    var sb = new StringBuilder();
        //    string goodChars = "0123456789.eE+-";
        //    foreach (var c in input)
        //    {                
        //        if (goodChars.IndexOf(c) >=0 )
        //            sb.Append(c);
        //    }
        //    string output = sb.ToString();
        //    return output;
        //}
#endif

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
                //if (TestMethod == TEST_BY_CONFIG)
                    MW.runButton_Click(null, null);
                //else
                //    MW.SimulationTestFunction();
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

        /// <summary>
        /// This handler is called if user changes a molecular concentration by slider or in text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dblConcs_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Number")
            {
                //if (significantChange() == true)
                //{

                //}

                redraw_flag = true;

                //if (TestMethod == TEST_BY_CONFIG)
                    MW.runButton_Click(null, null);
                //else
                //    MW.SimulationTestFunction();
            }
        }

        //private bool significantChange()
        //{
        //    double d = CRC.
        //    foreach (f


        //    return true;
        //}

        //private void btnTest_Click(object sender, RoutedEventArgs e)
        //{
        //    //if (TestMethod == TEST_BY_SIM)
        //    //{
        //    //    TestMethod = TEST_BY_CONFIG;
        //    //    txtTestMethod.Text = "Using Config";
        //    //}
        //    //else 
        //    //{
        //    //    TestMethod = TEST_BY_SIM;
        //    //    txtTestMethod.Text = "Using Simulation";
        //    //}
        //}

        private void btnSaveReport_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
