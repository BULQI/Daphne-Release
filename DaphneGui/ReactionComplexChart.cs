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

        public void CalculateXMax()
        {
            double max = -1 * 2E100;
            if (ListTimes.Count > 0)
                max = ListTimes.Max();
            ChartAreas[0].AxisX.Maximum = max * 1.1;
        }

        public void RecalculateYMax()
        {
            ChartAreas[0].AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;
        }

        private double getMin_Series(Dictionary<string, List<double>> dt)
        {
            double min = 2E100;
            //go through the dictionary and get the min across
            foreach (KeyValuePair<string, List<double>> e in dt)
            {
                if (IsYLogarithmic) //(cChart.ChartAreas[0].AxisY.IsLogarithmic)
                {
                    if (e.Value.Where(a => a > 0).Count() > 0 && min > e.Value.Where(a => a > 0).Min())
                    {
                        min = e.Value.Min();
                    }
                }
                else
                {
                    if (min > e.Value.Min())
                    {
                        min = e.Value.Min();
                    }
                }
            }

            return min;
        }

        private double getMax_Series(Dictionary<string, List<double>> dt)
        {
            double max = -1 * 2E100;
            //go through the dictionary and get the min across
            foreach (KeyValuePair<string, List<double>> e in dt)
            {
                if (max < e.Value.Max())
                {
                    max = e.Value.Max();
                }
            }

            return max;
        }

        private double getMin_Time()
        {
            double min = 2E100;
            //go through the list and get the min across
            foreach (double d in ListTimes)
            {
                if (d < min)
                {
                    min = d;
                }
            }

            return min;
        }

        private double getMax_Time()
        {
            double max = -1 * 2E100;
            //go through the list and get the min across
            foreach (double d in ListTimes)
            {
                if (d > max)
                {
                    max = d;
                }
            }

            return max;
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
                double valX = ChartAreas[0].AxisX.PixelPositionToValue(mouseDownLocation.X);
                double valY = ChartAreas[0].AxisY.PixelPositionToValue(mouseDownLocation.Y);

                if (ChartAreas[0].AxisY.IsLogarithmic)
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
            ChartAreas[0].AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;

            ToolWin.UpdateGrids();
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

                double valu = ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
                if (ChartAreas[0].AxisY.IsLogarithmic)
                {
                    valu = Math.Pow(10, valu);
                }

                if (valu > 0)
                {
                    ToolWin.dblMouseHover.Number = valu;
                    string guid = ConvertMolNameToMolGuid(SeriesToDrag.Name);
                    //ToolWin.redraw_flag = true;
                    //ToolWin.MW.RerunSimulation();
                    
                    //Fix this
                    //ToolWin.RC.EditConc(guid, valu);
                    //RedrawSeries();


                    ConfigMolecularPopulation molpop = ToolWin.CRC.molpops.Where(m => m.molecule.entity_guid == guid).First();
                    if (molpop != null)
                    {
                        MolPopHomogeneousLevel homogeneous = molpop.mp_distribution as MolPopHomogeneousLevel;
                        homogeneous.concentration = valu;
                        ToolWin.redraw_flag = true;
                        ToolWin.MW.runButton_Click(null, null);
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

            ChartAreas.Clear();
            Series.Clear();
            Legends.Clear();
            Titles.Clear();
            
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

            if (IsXLogarithmic)
            {
                chartArear1.AxisX.Minimum = ListTimes.Where(a => a > 0).Min();
                chartArear1.AxisX.Minimum = Math.Pow(10, Math.Floor(Math.Log10(chartArear1.AxisX.Minimum)));
            }
            else
            {
                chartArear1.AxisX.Minimum = 0;
            }

            chartArear1.AxisY.Minimum = getMin_Series(DictConcs) * 0.9;
            if (IsYLogarithmic)
            {
                chartArear1.AxisY.Minimum = Math.Pow(10, Math.Floor(Math.Log10(chartArear1.AxisY.Minimum)));
            }

            LabelX = "Time (linear)";
            LabelY = "Concentration (linear)";

            //LOGARITHMIC Y Axis
            if (IsYLogarithmic)
            {
                chartArear1.AxisY.IsLogarithmic = false; // true;
                chartArear1.AxisY.LogarithmBase = 10;
                LabelY = "Concentration (log)";
            }
            //LOGARITHMIC X Axis
            if (IsXLogarithmic)
            {
                chartArear1.AxisX.IsLogarithmic = false; // true;
                chartArear1.AxisX.LogarithmBase = 10;
                LabelX = "Time (log)";
            }

            chartArear1.AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;

            chartArear1.AxisX.Maximum = x.Max() * 1.11;   //x.Max() * 1.1 + 0.01;
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
                double yval = y[i];
                double xval = x[i];
                //if logarithmic
                if (!((IsYLogarithmic && yval <= 0) || (IsXLogarithmic && xval <= 0)))
                {
                    s.Points.AddXY(xval, yval);
                }
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

            s.MarkerStep = 1;
            if (n / 10 > 1)
                s.MarkerStep = n / 10;

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

            double min = getMin_Series(DictConcs);      //gets min of all series
            double max = getMax_Series(DictConcs);      //gets max of all series
            double low = startY - (max - min) * 0.05;   //5% is calculated as 5% of range of all series'
            double high = startY + (max - min) * 0.05;

            double lowest = 1000000000;

            foreach (KeyValuePair<String, List<Double>> entry in DictConcs)
            {
                double[] y;
                string seriesName = "";
                y = entry.Value.ToArray();


                if ((y[0] <= 0) && ChartAreas[0].AxisY.IsLogarithmic)
                    continue;

                //Select the point if user clicked within 5% of it              
                if (y[0] >= low && y[0] <= high)
                {
                    double dist = Math.Abs(startY - y[0]);
                    if (dist < lowest)
                    {
                        lowest = dist;
                        seriesName = ConvertMolGuidToMolName(entry.Key);
                        s = Series.FindByName(seriesName);
                    }
                }

                //Must find the closest series, not the first one 'near' the click
            }

            return s;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RedrawSeries()
        {
            //ToolWin.RC.reset();
            //ToolWin.RC.RunForward();
            
            ListTimes = ToolWin.RC.ListTimes;
            DictConcs = ToolWin.RC.DictGraphConcs;

            double[] x;
            double[] y;
            x = ListTimes.ToArray();

           
            foreach (Series s in Series)
            {
                s.Points.Clear();
                string guid = ConvertMolNameToMolGuid(s.Name);
                List<double> values = DictConcs[guid];
                y = values.ToArray();

                int n = x.Count() <= y.Count() ? x.Count() : y.Count();
                for (int i = 0; i < n; i++)
                {
                    double xval = x[i];
                    double yval = y[i];
                    if (!((IsYLogarithmic && yval <= 0) || (IsXLogarithmic && xval <= 0)))
                    {
                        s.Points.AddXY(xval, yval);
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
                            //s.Points[0].Label = ToolWin.txtMouseHover.Text; 
                            s.Points[0].Label = ToolWin.dblMouseHover.FNumber;
                        }
                    }
                    s.Points[0].MarkerBorderColor = Color.Black;
                    s.Points[0].MarkerSize = 10;
                    s.Points[0].MarkerStyle = MarkerStyle.Circle;
                }
            }

            //HAVE TO UPDATE X AXIS MAX TOO
            CalculateXMax();

            Focus();
            Invalidate();
        }


    }
}
