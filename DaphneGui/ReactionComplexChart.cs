using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Workbench;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using Daphne;

namespace DaphneGui
{
    public class ReactionComplexChart : System.Windows.Forms.DataVisualization.Charting.Chart
    {
        //Private variables
        private Dictionary<int, Color> colorTable;
        private bool bDrag = false;
        private Series SeriesToDrag = null;

        //Public variables
        public Panel panelRC;

        //Properties
        public ChartViewToolWindow ToolWin { get; set; }    //?? - Actually, ToolWin contains this chart!
        public bool IsXLogarithmic { get; set; }
        public bool IsYLogarithmic { get; set; }
        public string LabelX { get; set; }
        public string LabelY { get; set; }
        public string TitleXY { get; set; }
        public bool DrawLine { get; set; }

        //Dictionaries and lists for concentrations and times
        private Dictionary<string, List<double>> dictConcs;
        public  Dictionary<string, List<double>> DictConcs
        {
            get
            {
                return dictConcs;
            }
            set
            {
                dictConcs = value;
            }
        }
        private List<double> listTimes; 
        public List<double> ListTimes
        {
            get
            {
                return listTimes;
            }
            set
            {
                listTimes = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReactionComplexChart()
        {
            dictConcs = new Dictionary<string, List<double>>();
            listTimes = new List<double>();

            colorTable = new Dictionary<int, Color>();
            colorTable.Add(0, Color.Red);
            colorTable.Add(1, Color.Green);
            colorTable.Add(2, Color.Blue);
            colorTable.Add(3, Color.Brown);
            colorTable.Add(4, Color.Gold);
            colorTable.Add(5, Color.Fuchsia);
            colorTable.Add(6, Color.Lime);
            colorTable.Add(7, Color.Violet);

            Size = new System.Drawing.Size(700, 300);
            IsXLogarithmic = false;
            IsYLogarithmic = false;
            DrawLine = true;
            LabelX = "Time (linear)";
            LabelY = "Concentration (linear)";

            MouseDown += new MouseEventHandler(this.Chart_MouseDown);
            MouseUp += new MouseEventHandler(this.Chart_MouseUp);
            MouseMove += new MouseEventHandler(this.Chart_MouseMove);
            BackColor = System.Drawing.SystemColors.ControlDark;
        }

        /// <summary>
        /// Initialize chart
        /// </summary>
        public void Initialize()
        {
            LabelX = "Time";
            LabelY = "Concentration";
            TitleXY = "Time Trajectory of Molecular Concentrations";
            DrawLine = true;

            System.Windows.Forms.MenuItem[] menuItems = 
            {   
                new System.Windows.Forms.MenuItem("Zoom in"),
                new System.Windows.Forms.MenuItem("Zoom out"),
                new System.Windows.Forms.MenuItem("Save Changes"),
                new System.Windows.Forms.MenuItem("Discard Changes"),
            };

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu(menuItems);
            SetContextMenu(menu);
        }

        /// <summary>
        /// Sets the x and y size of the chart window
        /// </summary>
        /// <param name="size"></param>
        public void SetSize(Size size)
        {
            this.Size = size;
        }

        //------------------------
        //Helper methods

        public string ConvertMolGuidToMolName(string guid)
        {
            string ret = "";
            if (MainWindow.SOP.Protocol.entity_repository.molecules_dict.ContainsKey(guid))
                ret = MainWindow.SOP.Protocol.entity_repository.molecules_dict[guid].Name;
            return ret;
        }

        private string ConvertMolNameToMolGuid(string name)
        {
            string ret = "";
            foreach (KeyValuePair<string, ConfigMolecule> kvp in MainWindow.SOP.Protocol.entity_repository.molecules_dict)
            {
                if (kvp.Value.Name == name)
                    return kvp.Key;
            }
            return ret;
        }

        public void SetXAxisLimits()
        {
            if (ChartAreas == null || ChartAreas.Count == 0)
                return;

            ChartAreas.First().AxisX.Minimum = getMin_Time();
            ChartAreas.First().AxisX.Maximum = getMax_Time() * 1.1;
        }

        public void SetYAxisLimits()
        {
            if (ChartAreas == null || ChartAreas.Count == 0)
                return;

            ChartAreas.First().AxisY.Minimum = getMin_Series(DictConcs);
            ChartAreas.First().AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;
        }

        private double getMin_Series(Dictionary<string, List<double>> dt)
        {
            // Seed the minimum with something from the data
            double min = getSeriesMin( dt.First().Value, IsYLogarithmic);
            double seriesMin;
            foreach (KeyValuePair<string, List<double>> e in dt)
            {
                seriesMin = getSeriesMin(e.Value, IsYLogarithmic);
                if (!double.IsNaN(seriesMin) && min > seriesMin)
                {
                    min = seriesMin;
                }
            }

            return min;
        }

        private double getSeriesMin(List<double> e, bool IsLogarithmic)
        {
            if (IsLogarithmic)
            {
                if (e.Where(a => (a > 0) && !(double.IsNaN(a))).Count() > 0)
                {
                    return e.Where(a => (a > 0) && !(double.IsNaN(a))).Min();
                }
                else
                {
                    return double.NaN;
                }
            }

            return e.Where(a => !(double.IsNaN(a)) ).Min();
        }

        private double getMax_Series(Dictionary<string, List<double>> dt)
        {
            // Seed the maximum with something from the data
            double max = getSeriesMax(dt.First().Value, IsYLogarithmic);
            double seriesMax;

            foreach (KeyValuePair<string, List<double>> e in dt)
            {
                seriesMax = getSeriesMax(e.Value, IsYLogarithmic);
                if (!double.IsNaN(seriesMax) && max < seriesMax)
                {
                    max = seriesMax;
                }
            }

            return max;
        }

        private double getSeriesMax(List<double> e, bool IsLogarithmic)
        {
            if (IsLogarithmic)
            {
                if (e.Where(a => (a > 0) && !(double.IsNaN(a))).Count() > 0)
                {
                    return e.Where(a => (a > 0) && !(double.IsNaN(a))).Max();
                }
                else
                {
                    return double.NaN;
                }
            }
            
            return e.Where(a => !(double.IsNaN(a))).Max();
        }

        private double getMin_Time()
        {
            return getSeriesMin(ListTimes, IsXLogarithmic); 
        }

        private double getMax_Time()
        {
            return getSeriesMax(ListTimes, IsXLogarithmic); 
        }

        public void SetContextMenu(ContextMenu menu)
        {
            ContextMenu = menu;
        }

        //------------------------
        //Event handlers

        /// <summary>
        /// On a mouse down, see if user clicked on the y-axis on a series' y-intercept which is its initial concentration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            HitTestResult result = HitTest(e.X, e.Y);
            System.Drawing.Point mouseDownLocation = new System.Drawing.Point(e.X, e.Y);

            if (e.Button != MouseButtons.Left)
                return;

            bDrag = false;

            double min = getMin_Time();
            double max = getMax_Time();
            double low = 0 - (max - min) * 0.1;
            double high = 0 + (max - min) * 0.1;

            //If user clicked on an axis or on tick marks
            if (result.ChartElementType == ChartElementType.Axis || result.ChartElementType == ChartElementType.TickMarks)
            {
                double valX = ChartAreas.First().AxisX.PixelPositionToValue(mouseDownLocation.X);
                double valY = ChartAreas.First().AxisY.PixelPositionToValue(mouseDownLocation.Y);

                if (ChartAreas.First().AxisY.IsLogarithmic)
                {
                    valY = Math.Pow(10, valY);
                }

                //Don't know which axis user clicked on.  Just that he/she clicked on an axis.  Hmm...
                //User must have clicked on Y axis!
                if (result.ChartElementType == ChartElementType.Axis ||
                    result.ChartElementType == ChartElementType.TickMarks && valX >= low && valX <= high)
                {
                    //FIND WHICH SERIES USER CLICKED ON.
                    //I.E., FIND A SERIES IF ANY, WHOSE FIRST POINT IS THIS POINT.  IF FOUND, THEN SET SeriesToDrag and bDrag.    
                    //If user clicked on X-axis, then FindSeriesAtPoint will return null.
                    Series ser = FindSeriesAtPoint(valY);
                    if (ser != null)
                    {
                        SeriesToDrag = ser;
                        bDrag = true;
                        MainWindow.SetControlFlag(MainWindow.CONTROL_MOUSE_DRAG, true);
                        ser.Points[0].MarkerColor = Color.Red;  //  .DataPoints.Item(1).Marker.Visible = True    
                        SeriesToDrag.Points[0].IsValueShownAsLabel = true;
                        SeriesToDrag.Points[0].LabelFormat = "F3";

                    }
                }
            }
        }

        /// <summary>
        /// On mouse up, not much to do.  Just reset a couple of things.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            bDrag = false;
            //Circle was red during drag so change it back to gold
            if (SeriesToDrag != null)
            {
                SeriesToDrag.Points[0].MarkerColor = Color.Gold;
                SeriesToDrag.Points[0].Label = "";
                SeriesToDrag.Points[0].IsValueShownAsLabel = false;
            }

            SeriesToDrag = null;
            ToolWin.dblMouseHover.Number = 0;

            SetYAxisLimits();
            Focus();
            Invalidate();
        }

        /// <summary>
        /// If a series is being dragged, get the new mouse position and redraw
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //if a series is being dragged
            if (bDrag)
            {
                if (e.Y < 0 || e.Y >= Size.Height)
                    return;

                double valu = ChartAreas.First().AxisY.PixelPositionToValue(e.Y);
                if (ChartAreas.First().AxisY.IsLogarithmic)
                {
                    valu = Math.Pow(10, valu);
                }

                if (valu > 0)
                {
                    ToolWin.dblMouseHover.Number = valu;
                    string guid = ConvertMolNameToMolGuid(SeriesToDrag.Name);

                    //Just change the value in the molpop and that will lead to a call to dblConcs_PropertyChanged in ChartViewToolWindow.xaml.cs
                    VatReactionComplexScenario s = ToolWin.protocol.scenario as VatReactionComplexScenario;
                    ConfigMolecularPopulation molpop = s.AllMols.Where(m => m.molecule.entity_guid == guid).First();
                    if (molpop != null)
                    {
                        MolPopHomogeneousLevel homogeneous = molpop.mp_distribution as MolPopHomogeneousLevel;
                        homogeneous.concentration = valu;
                    }
                }
            }            
        }

        //------------------------
        //Main drawing methods

        /// <summary>
        /// This clears everything from the panel (that contains the chart), that is inside ToolWin        
        /// </summary>
        public void Clear()
        {
            if (panelRC == null)
                return;

            foreach (Control c in panelRC.Controls)
            {
                panelRC.Controls.Remove(c);
            }

            bDrag = false;
            SeriesToDrag = null;
            ChartAreas.Clear();
            Series.Clear();
            Legends.Clear();
            Titles.Clear();
            
        }
        public void DrawBlank()
        {
            Clear();
            ChartArea chartAreaBlank = new ChartArea("Blank Chart Area");
            ChartAreas.Add(chartAreaBlank);

            LabelX = "Time";
            LabelY = "Concentration";
            TitleXY = "Time Trajectory of Molecular Concentrations";

            chartAreaBlank.AxisX.Title = LabelX;
            chartAreaBlank.AxisY.Title = LabelY;

            Titles.Add("Title_1");
            BackColor = Color.White;

            Titles[0].Text = TitleXY;

            ChartAreas.First().AxisX.Minimum = 0;
            ChartAreas.First().AxisX.Maximum = 8;
            ChartAreas.First().AxisY.Minimum = 0;
            ChartAreas.First().AxisY.Maximum = 5;

            LabelX = "Time (linear)";
            LabelY = "Concentration (linear)";
            chartAreaBlank.AxisX.Title = LabelX;
            chartAreaBlank.AxisY.Title = LabelY;

            Location = new System.Drawing.Point(1, 8);

            double[] x;
            double[] y;

            List<double> tempX = new List<double>();
            List<double> tempY = new List<double>();
            tempX.Add(0);
            tempX.Add(0.00000000001); //tempX.Add(1);
            tempY.Add(0); //tempY.Add(1);
            tempY.Add(0.00000000001);
            x = tempX.ToArray();
            y = tempY.ToArray();
            drawSeries(x, y, chartAreaBlank, TitleXY, "Blank", 0);

            Focus();
            Invalidate();

            panelRC.Controls.Add(this);
        }

        /// <summary>
        /// This function creates and draws the entire graph.  It uses ListTimes and DictConcs to draw all the series in the graph.
        /// Each molecule's concentrations are drawn as a series.  It calls the DrawSeries function to draw each series. 
        /// We are using only 1 chart area.  If we use more than 1, then the DrawSeries function should be made more general!
        /// </summary>
        public void Draw()
        {
            Clear();

            ChartArea chartArear1 = new ChartArea("default");

            ChartAreas.Add(chartArear1);
            double[] x;
            double[] y;
            x = ListTimes.ToArray();
            int colorCount = 0;

            //For each molecule, call a function to create and draw the series 
            foreach (KeyValuePair<String, List<Double>> entry in DictConcs)
            {
                y = entry.Value.ToArray();
                string molname = ConvertMolGuidToMolName(entry.Key);
                drawSeries(x, y, chartArear1, TitleXY, molname, colorCount);
                colorCount++;
            }

            if (Series.Count > 0)
            {
                SetXAxisLimits();
                SetYAxisLimits();
            }

            LabelX = "Time (linear)";
            LabelY = "Concentration (linear)";

            //LOGARITHMIC Y Axis
            if (IsYLogarithmic)
            {
                chartArear1.AxisY.IsLogarithmic = IsYLogarithmic; // true;
                chartArear1.AxisY.LogarithmBase = 10;
                LabelY = "Concentration (log)";
            }
            //LOGARITHMIC X Axis
            if (IsXLogarithmic)
            {
                chartArear1.AxisX.IsLogarithmic = IsXLogarithmic; // true;
                chartArear1.AxisX.LogarithmBase = 10;
                LabelX = "Time (log)";
            }

            chartArear1.AxisX.Title = LabelX;
            chartArear1.AxisY.Title = LabelY;
            chartArear1.AxisX.TitleFont = new Font("Arial", 8);
            chartArear1.AxisY.TitleFont = new Font("Arial", 8);

            chartArear1.AxisX.LabelStyle.Font = new Font("Arial", 7);
            chartArear1.AxisY.LabelStyle.Font = new Font("Arial", 7);

            chartArear1.AxisX.LabelStyle.Format = "##,#.00";
            chartArear1.AxisY.LabelStyle.Format = "e2";
            chartArear1.Visible = true;
            chartArear1.Position.Auto = true;

            chartArear1.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArear1.AxisY.MajorGrid.LineColor = Color.LightGray;

            chartArear1.AxisX.LineWidth = 1;
            chartArear1.AxisY.LineWidth = 1;
            chartArear1.AxisX.LineColor = Color.Black;
            chartArear1.AxisY.LineColor = Color.Black;

            Titles.Add("Title_1");
            BackColor = Color.White;

            Titles[0].Text = TitleXY;

            Legends.Add("default");
            Legends["default"].Docking = Docking.Right;
            Legends["default"].BackColor = Color.AliceBlue;
            Legends["default"].BorderColor = Color.Black;
            Legends["default"].BorderWidth = 1;

            // Set chart control location
            Location = new System.Drawing.Point(1, 8);

            if (Series.Count > 0)
            {
                SetXAxisLimits();
                SetYAxisLimits();
            }
            
            //// For debugging
            //Console.WriteLine("Draw() final: {0}, {1}\t {2}, {3}", ChartAreas.First().AxisX.Minimum, ChartAreas.First().AxisX.Maximum, 
            //                                                        ChartAreas.First().AxisY.Minimum, ChartAreas.First().AxisY.Maximum);

            Focus();
            Invalidate();

            panelRC.Controls.Add(this);
        }

        /// <summary>
        /// This function draws one series whose points are passed in the "x" and "y" arrays.  
        /// "x" has time points, "y" has concs. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cA"></param>
        /// <param name="title"></param>
        /// <param name="seriesName"></param>
        /// <param name="_color"></param>
        /// <param name="drawLine"></param>
        private void drawSeries(double[] x, double[] y, ChartArea CA, string title, string seriesName = "", int _color = 0, bool drawLine = true)
        {
            if (x.Count() == 0 && y.Count() == 0)
            {
                //no points to draw, return
                return;
            }

            //draw points first
            int n = x.Count() <= y.Count() ? x.Count() : y.Count();
            Series s = new Series(seriesName);

            Series.Add(s);
            for (int i = 0; i < n; i++)
            {
                if (ValidPoint(x[i], y[i]))
                {
                    s.Points.AddXY(x[i], y[i]);
                }
            }

            if (s.Points.Count == 0)
            {
                Series.Remove(s);
            }

            // Add series to the chart
            if (drawLine)
            {
                s.ChartType = SeriesChartType.Line;
            }
            else
            {
                s.ChartType = SeriesChartType.Point;
            }
            s.ChartArea = CA.Name;
            s.MarkerSize = 4;
            s.MarkerStyle = MarkerStyle.None;

            // Plot every data point
            s.MarkerStep = 1;

            s.Color = colorTable[_color % colorTable.Count];

            if (s.Points.Count > 0 && s.Points[0].XValue == 0)
            {
                s.Points[0].MarkerColor = Color.Gold;
                s.Points[0].MarkerBorderColor = Color.Black;
                s.Points[0].MarkerSize = 10;
                s.Points[0].MarkerStyle = MarkerStyle.Circle;
            }
        }


        //------------------------
        //Other helper methods
        

        /// <summary>
        /// This function finds a series at the given point on the y-axis, if user clicks "near" it
        /// </summary>
        private Series FindSeriesAtPoint(double startY)
        {
            Series s = null;
            double lowest = ChartAreas.First().AxisY.Maximum - ChartAreas.First().AxisY.Minimum;
            double delta = lowest * 0.05; 
            double low = startY - delta;   
            double high = startY + delta;

            double y;
            foreach (KeyValuePair<String, List<Double>> entry in DictConcs)
            {
                y = entry.Value[0];

                if ((y <= 0) && ChartAreas.First().AxisY.IsLogarithmic)
                    continue;

                //Consider this series if the user clicked within 5% of it's first value            
                if (y >= low && y <= high)
                {
                    double dist = Math.Abs(startY - y);
                    if (dist < lowest)
                    {
                        lowest = dist;
                        s = Series.FindByName(ConvertMolGuidToMolName(entry.Key));
                    }
                }
            }

            return s;
        }

        /// <summary>
        /// This updates the list of series for this graph
        /// </summary>
        public void UpdateSeries()
        {
            ListTimes = ToolWin.RC.ListTimes;
            DictConcs = ToolWin.RC.DictGraphConcs;

            //Remove any series that is no longer in DictConcs
            foreach (Series s in Series.ToList())
            {
                string guid = ConvertMolNameToMolGuid(s.Name);
                if (DictConcs.ContainsKey(guid) == false)
                {
                    Series.Remove(s);
                }
            }

            //Add any series that are in DictConcs but not in this.Series

            if (this.ChartAreas == null || this.ChartAreas.Count == 0)
                return;

            foreach (string guid in DictConcs.Keys)
            {
                string molname = ConvertMolGuidToMolName(guid);
                bool exists = Series.Where(ser => ser.Name == molname).Any();
                if (exists == false)
                {
                    Series s = new Series(molname);
                    s.ChartType = SeriesChartType.Line;
                    s.ChartArea = this.ChartAreas.First().Name;
                    s.MarkerSize = 4;
                    s.MarkerStyle = MarkerStyle.None;
                    s.MarkerStep = 1;
                    s.Color = colorTable[0 % colorTable.Count];
                    this.Series.Add(s);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RedrawSeries()
        {
            ListTimes = ToolWin.RC.ListTimes;
            DictConcs = ToolWin.RC.DictGraphConcs;

            double[] x;
            double[] y;
            x = ListTimes.ToArray();
           
            foreach (Series s in Series)
            {
                s.Points.Clear();
                
                string guid = ConvertMolNameToMolGuid(s.Name);

                //This prevents a crash in case DictConcs contains fewer items than Series (if user removed 
                //a reaction or turned off rendering for a molecule and did not click the Run button).
                //If UpdateSeries was called before this function, this should not happen anyway.
                if (DictConcs.ContainsKey(guid) == false)
                    continue;

                List<double> values = DictConcs[guid];
                y = values.ToArray();

                int n = x.Count() <= y.Count() ? x.Count() : y.Count();
                for (int i = 0; i < n; i++)
                {
                    if (ValidPoint(x[i], y[i]))
                    {
                        s.Points.AddXY(x[i], y[i]);
                    }
                }

                if (s.Points.Count <= 0)
                    continue;

                if (s.Points[0].XValue == 0)
                {
                    s.Points[0].MarkerColor = Color.Gold;
                    if (SeriesToDrag != null)
                    {
                        if (s == SeriesToDrag)
                        {
                            s.Points[0].MarkerColor = Color.Red;
                            s.Points[0].Label = ToolWin.dblMouseHover.FNumber;
                        }
                    }
                    s.Points[0].MarkerBorderColor = Color.Black;
                    s.Points[0].MarkerSize = 10;
                    s.Points[0].MarkerStyle = MarkerStyle.Circle;
                }
            }

            //HAVE TO UPDATE X AXIS MAX TOO
            if (Series.Count > 0)
            {
                SetXAxisLimits();
                SetYAxisLimits();
            }

            // For debugging
            ////Console.WriteLine("RedrawSeries(): {0}, {1}\t {2}, {3}",
            //    ChartAreas.First().AxisX.Minimum, ChartAreas.First().AxisX.Maximum, ChartAreas.First().AxisY.Minimum, ChartAreas.First().AxisY.Maximum);

            //Focus();
            Invalidate();
        }

        public bool ValidPoint(double xval, double yval)
        {
            if (IsYLogarithmic && yval <= 0)
            {
                return false;
            }

            if (IsXLogarithmic && xval <= 0)
            {
                return false;
            }

            if (double.IsNaN(yval))
            {
                return false;
            }

            if (double.IsNaN(xval) )
            {
                return false;
            }

            return true;
        }


    }
}
