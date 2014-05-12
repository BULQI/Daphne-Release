using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using DaphneGui;

namespace Workbench
{
    //NOTE: This implementation assumes we will only use 1 chart area (only 1 graph), not multiple graphs.
    //      If multiple graphs need to be supported, some changes will be needed.
    public class ChartManager
    {
        public Size ChartSize { get; set; }
        private System.Windows.Forms.DataVisualization.Charting.Chart cChart;
        public Panel PChart;
        private Dictionary<int, Color> colorTable;        
        //private int fontSize = 20;
        private ChartViewToolWindow ToolWin;
        private ContextMenu contextMenu = new ContextMenu();

        private Dictionary<string, List<double>> dictConcs = new Dictionary<string, List<double>>();
        public Dictionary<string, List<double>> DictConcs
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

        private List<double> listTimes = new List<double>();
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
        
        public string LabelX { get; set; }
        public string LabelY { get; set; }
        public string TitleXY { get; set; }
        public bool DrawLine { get; set; }
        private bool bDrag = false;
        private Series SeriesToDrag = null;

        //Constructor
        public ChartManager(ChartViewToolWindow win, Size sz)
        {
            //build color table for later use
            colorTable = new Dictionary<int, Color>();
            colorTable.Add(0, Color.Red);
            colorTable.Add(1, Color.Green);
            colorTable.Add(2, Color.Blue);
            colorTable.Add(3, Color.Brown);
            colorTable.Add(4, Color.Gold);
            colorTable.Add(5, Color.Fuchsia);        
            ChartSize = sz;
            ToolWin = win;
            DrawLine = true;
        }

        public ChartManager(ChartViewToolWindow win, Size sz, string title, string xlabel, string ylabel, bool line)
        {
            //build color table for later use
            colorTable = new Dictionary<int, Color>();
            colorTable.Add(0, Color.Red);
            colorTable.Add(1, Color.Green);
            colorTable.Add(2, Color.Blue);
            colorTable.Add(3, Color.Brown);
            colorTable.Add(4, Color.Gold);
            colorTable.Add(5, Color.Fuchsia); 
            ChartSize = sz;
            ToolWin = win;
            TitleXY = title;
            LabelX = xlabel;
            LabelY = ylabel;
            DrawLine = line;
        }

        //This function creates and draws the entire graph.  It uses ListTimes and DictConcs to draw all the series in the graph.
        //Each molecule's concentrations are drawn as a series.  It calls the DrawSeries function to draw each series. 
        //We are using only 1 chart area.  If we use more than 1, then the DrawSeries function should be made more general.
        public void DrawChart()
        {
            foreach (Control c in PChart.Controls)
            {
                PChart.Controls.Remove(c);
            }

            cChart = new Chart();            
            cChart.MouseDown += new MouseEventHandler(this.cChart_MouseDown);
            cChart.MouseUp += new MouseEventHandler(this.cChart_MouseUp);
            cChart.MouseMove += new MouseEventHandler(this.cChart_MouseMove);            
            cChart.BackColor = System.Drawing.SystemColors.ControlDark;
            cChart.ContextMenu = contextMenu;
  
            ChartArea chartArear1 = new ChartArea("default");
                       
            cChart.ChartAreas.Add(chartArear1);
            double[] x; double[] y; x = ListTimes.ToArray();
            int count = 0;

            //For each molecule, call a function to create and draw the series 
            foreach (KeyValuePair<String, List<Double>> entry in DictConcs)
            {
                y = entry.Value.ToArray();
                drawSeries(x, y, chartArear1, TitleXY, entry.Key, count, DrawLine);
                count++;
            }
            //**********
            chartArear1.AxisY.Minimum = 0;  // getMin_Series(DictConcs) * 0.9;

            if (chartArear1.AxisY.Maximum <= 0)  
                chartArear1.AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;

            //if (chartArear1.AxisY.Maximum > 100000)
            //    chartArear1.AxisY.Maximum = 100000;
            //TickMark tick = new TickMark();
            //tick.Interval = 0.1;

            chartArear1.AxisX.Minimum = 0;  // x.Min() - 0.1;
            chartArear1.AxisX.Maximum = x.Max() * 1.11;   //x.Max() * 1.1 + 0.01;
            chartArear1.AxisX.Title = LabelX;
            chartArear1.AxisY.Title = LabelY;
            //chartArear1.AxisX.MajorTickMark = tick;
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
            //*********************
            //chartArear1.AxisY.ScrollBar.Enabled = true;

            cChart.Titles.Add("Title_1");
            cChart.BackColor = Color.White;

            cChart.Titles[0].Text = TitleXY;

            cChart.Legends.Add("default"); cChart.Legends["default"].Position.Auto = true;
            cChart.Legends["default"].InsideChartArea = "default";
            //cChart.Legends["default"].BackColor = Color.Transparent;
            cChart.Legends["default"].BackColor = Color.AliceBlue;
            cChart.Legends["default"].BorderColor = Color.Black;
            cChart.Legends["default"].BorderWidth = 1;
            // Set chart control location
            cChart.Location = new System.Drawing.Point(1, 8);

            cChart.Size = ChartSize;
            
            PChart.Controls.Add(cChart);
            
            return;
        }

        //This function draws one series whose points are passed in the "x" and "y" arrays.  
        //"x" has time points, "y" has concs.
        private void drawSeries(double[] x, double[] y, ChartArea cA, string title, string seriesName = "", int _color = 0, bool drawLine = false)
        {
            if (x.Count() == 0 && y.Count() == 0)
            {
                //no point to draw, return
                return;
            }
            //draw points first
            int n = x.Count() <= y.Count() ? x.Count() : y.Count();
            Series s = new Series(seriesName);

            cChart.Series.Add(s);
            for (int i = 0; i < n; i++)
            {
                s.Points.AddXY(x[i], y[i]);
            }

            // Add series to the chart
            if (drawLine)
            {
                //s.ChartType = SeriesChartType.FastLine;
                s.ChartType = SeriesChartType.Line;                
            }
            else
            {
                s.ChartType = SeriesChartType.Point;
            }            
            s.ChartArea = cA.Name;
            s.MarkerSize = 4; 
            s.MarkerStyle = MarkerStyle.None;
            s.MarkerStep = n / 10;
            //s.MarkerBorderColor = Color.Black;
            //s.MarkerColor = Color.Gold;
            s.Color = colorTable[_color % colorTable.Count];          

            //cA.AxisY.Minimum = 0.0;//y.Min();  // *0.9;
            cA.AxisY.Maximum = y.Max() * 1.1 + 0.0001;

            //This puts a max on y axis.  Not sure if we should have a max.  Should there be a limit??
            //if (cA.AxisY.Maximum > 100000)
            //    cA.AxisY.Maximum = 100000;

            //------------------------
            s.Points[0].MarkerColor = Color.Gold;  
            s.Points[0].MarkerBorderColor = Color.Black;
            s.Points[0].MarkerSize = 10;
            s.Points[0].MarkerStyle = MarkerStyle.Circle;           
            //-------------------------

        }

        private double getMin_Series(Dictionary<string, List<double>> dt)
        {
            double min = 2E100;
            //go through the dictionary and get the min across
            foreach (KeyValuePair<string, List<double>> e in dt)
            {
                if (min > e.Value.Min())
                {
                    min = e.Value.Min();
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

        
        private void cChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            HitTestResult result = cChart.HitTest(e.X, e.Y);

            System.Drawing.Point mouseDownLocation = new System.Drawing.Point(e.X, e.Y);

            //Most of this is not needed.  We're only interested in Left Mouse Down.
            string eventString = null;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    eventString = "L";
                    break;
                case MouseButtons.Right:
                    eventString = "R";
                    break;
                case MouseButtons.Middle:
                    eventString = "M";
                    break;
                case MouseButtons.XButton1:
                    eventString = "X1";
                    break;
                case MouseButtons.XButton2:
                    eventString = "X2";
                    break;
                case MouseButtons.None:
                default:
                    break;
            }

            bDrag = false;
            //ToolWin.txtMouseClick.Text = "";


            //If Left Mouse Down, then we need to do some work.
            if (eventString == "L")
            {
                double min = getMin_Time();
                double max = getMax_Time();
                double low = 0 - (max - min) * 0.1;
                double high = 0 + (max - min) * 0.1;

                //If user clicked on an axis or on tick marks
                if (result.ChartElementType == ChartElementType.Axis || result.ChartElementType == ChartElementType.TickMarks)
                {
                    double valX = result.Axis.PixelPositionToValue(mouseDownLocation.X);
                    double valY = result.Axis.PixelPositionToValue(mouseDownLocation.Y);

                    //ChartArea ca = cChart.ChartAreas[0];
                    //ca.CursorX.SetCursorPixelPosition(new Point(e.X, e.Y), true);
                    //ca.CursorY.SetCursorPixelPosition(new Point(e.X, e.Y), true);

                    ////This stuff is just for debugging
                    //double pX = ca.CursorX.Position; //X Axis Coordinate of your mouse cursor
                    //double pY = ca.CursorY.Position; //Y Axis Coordinate of your mouse cursor
                    //string output = pX.ToString();
                    //output += ", ";                    
                    //output += valY.ToString("0.###");
                    //output += " : Drag Series";
                    ////

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
                            //ToolWin.txtMouseClick.Text = output;
                            ser.Points[0].MarkerColor = Color.Red;  //  .DataPoints.Item(1).Marker.Visible = True    
                            //SeriesToDrag.ToolTip = "Test";
                            //SeriesToDrag.IsValueShownAsLabel = true;
                            //SeriesToDrag.Label = valY.ToString();
                            SeriesToDrag.Points[0].IsValueShownAsLabel = true;
                            
                        }
                    }
                }
            }

            return;

        }

        private void cChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //Update the mouse path that is drawn onto the Panel. 

            //These 4 lines are only for debugging - they can be removed later on
            int mouseX = e.X;
            int mouseY = e.Y;
            string output = mouseX.ToString() + ", " + mouseY.ToString();
            //ToolWin.txtMouseHover.Text = output;
            

            if (bDrag)
            {
                if (e.Y < 0 || e.Y >= ChartSize.Height)
                    return;

                double valu = cChart.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
                if (valu > 0)
                {
                    ToolWin.txtMouseHover.Text = valu.ToString("#.00000");
                    ToolWin.RC.EditConc(SeriesToDrag.Name, valu);
                    RedrawSeries();
                }
            }
            







            //HitTestResult result;
            //try
            //{
            //   result = cChart.HitTest(e.X, e.Y);               
            //}
            //catch (Exception ex)
            //{
            //    System.Windows.MessageBox.Show(ex.ToString());
            //    return;
            //}

            ////if we're dragging something, then draw it here and erase from previous location            
            //if (bDrag)
            //{
            //    if (result.ChartElementType == ChartElementType.Axis)
            //    {
            //        //ONLY DO THIS IF MOUSE IS ON Y AXIS
            //        Object obj = result.Object;
            //        if (((Axis)(obj)).AxisName == AxisName.Y)
            //        {
            //            double val = result.Axis.PixelPositionToValue(e.Y);

            //            //THE FOLLOWING HAS TO BE DONE TO REDRAW THE WHOLE GRAPH UPON MOUSE MOVE
            //            if (val > 0 && val <= 100000)
            //            {
            //                ToolWin.txtMouseHover.Text = val.ToString("#.00000");
            //                ToolWin.RC.EditConc(SeriesToDrag.Name, val);
            //                RedrawSeries();
            //            }
            //        }
            //    }
            //    else 
            //    {
            //        double val = cChart.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
            //        if (val > 0 && val <= 100000)
            //        {
            //            ToolWin.txtMouseHover.Text = val.ToString("#.00000");
            //            ToolWin.RC.EditConc(SeriesToDrag.Name, val);
            //            RedrawSeries();
            //        }
            //    }
            //}
        }

        //On mouse up, not much to do.  Just reset a couple of things.
        private void cChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
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
            ToolWin.txtMouseHover.Text = "";
            cChart.ChartAreas[0].AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;
            //cChart.Focus();
            //cChart.Invalidate();
        }

        //This function finds a series at the given point on the y-axis, if user clicks "near" it
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

                //Select the point if user clicked within 5% of it              
                if (y[0] >= low && y[0] <= high)
                {
                    double dist = Math.Abs(startY - y[0]);
                    if (dist < lowest)
                    {
                        lowest = dist;
                        seriesName = entry.Key;
                        s = cChart.Series.FindByName(seriesName);
                    }
                    //break;
                }

                //Must find the closest series, not the first one 'near' the click
            }

            return s;
        }        

        public void RedrawSeries()
        {
            ToolWin.RC.Go();
            ListTimes = ToolWin.RC.ListTimes;
            DictConcs = ToolWin.RC.DictGraphConcs;

            double[] x; 
            double[] y; 
            x = ListTimes.ToArray();

            if (cChart != null)
            {
                foreach (Series s in cChart.Series)
                {
                    s.Points.Clear();
                    List<double> values = DictConcs[s.Name];
                    y = values.ToArray();

                    int n = x.Count() <= y.Count() ? x.Count() : y.Count();
                    for (int i = 0; i < n; i++)
                    {
                        s.Points.AddXY(x[i], y[i]);
                    }

                    s.Points[0].MarkerColor = Color.Gold;
                    if (SeriesToDrag != null)
                    {
                        if (s == SeriesToDrag)
                        {
                            s.Points[0].MarkerColor = Color.Red;
                            s.Points[0].Label = ToolWin.txtMouseHover.Text;
                        }
                    }
                    s.Points[0].MarkerBorderColor = Color.Black;
                    s.Points[0].MarkerSize = 10;
                    s.Points[0].MarkerStyle = MarkerStyle.Circle;                    
                }
            }
            
            //cChart.ChartAreas[0].AxisY.Maximum = getMax_Series(DictConcs) * 1.1 + 0.0001;

            //HAVE TO UPDATE X AXIS MAX TOO
            cChart.ChartAreas[0].AxisX.Maximum = x.Max() * 1.11;

            cChart.Focus();
            cChart.Invalidate(); 
            
        }

        //This gets called if user clicks Save button on the Chart View Tool Window
        public void SaveChanges()
        {
            if (cChart != null)
            {
                foreach (Series s in cChart.Series)
                {
                    ToolWin.RC.EditConc(s.Name, s.Points[0].YValues[0]);                    
                }

                ToolWin.RC.SaveOriginalConcs();
            }
        }    
   
        public void SetContextMenu(MenuItem[] menuItems)
        {
            //MenuItem[] menuItems = 
            //    {   
            //        new MenuItem("Bigger"),
            //        new MenuItem("Smaller"),
            //        new MenuItem("Save Changes"),
            //        new MenuItem("Discard Changes"),
            //        new MenuItem("Change Rate Constants..."),
            //        new MenuItem("View Initial Concentrations...") 
            //    }; 
            
            cChart.ContextMenu = new ContextMenu(menuItems);
            
        }
        public System.Windows.Forms.ContextMenu GetContextMenu()
        {
            return cChart.ContextMenu;
        }

        public void SetContextMenu(ContextMenu menu)
        {
            contextMenu = menu;
        }

    }
}
