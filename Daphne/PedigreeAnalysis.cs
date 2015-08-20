using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

using System.Numerics;
using System.Drawing;

namespace Daphne
{
    public class PedigreeAnalysis
    {
        //NOTE: in this current version, we treat EXIT the same as it dies.!!!!

        //also, when calling this class, we assume the the outside caller need to set up the reporter. and provide 
        //the founderID, then we can go ahead to do the analysis and finally return the series containing the
        //lines and points and annotation and color, etc.

        /// <summary>
        /// constructor
        /// </summary>
        public PedigreeAnalysis()
        {
            //empty constructor
        }
        public void SetReport(ReporterBase r)
        {
           reporter=r;
        }
        /// <summary>
        /// clears current data
        /// </summary>
        private void resetData()
        {
            familytree = new Dictionary<BigInteger, GenealogyInfo>();
            cellsets = new Dictionary<int, string>();
            experiment_id = -1;
            //reporter = null;
        }

        private void ExtractPedigreeData(FounderInfo founder)
        {
            resetData();
            //need to build family tree first.
            familytree = reporter.ProvideGenealogyData(founder);
        }

        /// <summary>
        /// upmost caller to generate a divTree
        /// </summary>
        /// <param name="cellSetId">this is not used actually, just keep this for now. since this is upmost call, only founderId is used</param>
        /// <param name="founderId">this is the foundID used to draw one peditree.</param>
        /// <returns>divTree dataStructure is a dictinary for one founder pedigree, keyed by generation number, valued by another dictionary keyed by position of node (division) and valued by division/death/exit</returns>
        private Dictionary<int, Dictionary<int, double>> GetPedDivTree(int cellSetId, BigInteger founderId)
        {
            Dictionary<int, Dictionary<int, double>> divtree = new Dictionary<int, Dictionary<int, double>>();
            int index = 0;
            addPedGeneration(ref divtree, founderId, index);
            return divtree;
        }

        /// <summary>
        /// similar to getPedDivTree, but this one contains cellID instead of cell division/death time
        /// </summary>
        /// <param name="cellSetId"></param>
        /// <param name="founderId"></param>
        /// <returns>the datastructure contains the cellID information for all the nodes keyed by indexs and then by generation</returns>
        public Dictionary<int, Dictionary<int, BigInteger>> GetPedDivTreeCellID(int cellSetId, BigInteger founderId)
        {
            Dictionary<int, Dictionary<int, BigInteger>> divtreeCellID = new Dictionary<int, Dictionary<int, BigInteger >>();
            int index = 0;
            addPedGenerationCellID(ref divtreeCellID, founderId, index);
            return divtreeCellID;
        }

        /// <summary>
        /// return true if this current cell has divided and it must have 2 daughter.
        /// </summary>
        /// <param name="cellID">the mother cell, we want to test on</param>
        /// <returns>true or false</returns>
        private bool CellHasDaughters(BigInteger cellID)
        {
            BigInteger daughter0 = familytree[cellID].Daughter(0);
            BigInteger daughter1 = familytree[cellID].Daughter(1);

            if (familytree.ContainsKey(daughter0) && familytree.ContainsKey(daughter1))
            {
                return true;
            }
            else  //there will be never the case that only one daughter exist in the tree;
            {
                return false;
            }
        }


        /// <summary>
        /// Adds this cellID's info the the divtree and repeats the call for its daughter cells
        /// </summary>
        /// <param name="divtree"></param>
        /// <param name="cellID"></param>
        private void addPedGeneration(ref Dictionary<int, Dictionary<int, double>> divtree, BigInteger cellID, int index)
        {
            if (!divtree.ContainsKey(familytree[cellID].Generation))
            {
                Dictionary<int, double> thisgeneration = new Dictionary<int, double>();
                divtree.Add(familytree[cellID].Generation, thisgeneration);
            }
            int sign = 1;
            if (familytree[cellID].EventType == GenealogyInfo.GI_DIE || familytree[cellID].EventType == GenealogyInfo.GI_EXIT)
                sign = -1;
            divtree[familytree[cellID].Generation].Add(index, sign * (familytree[cellID].EventTime));
            if (CellHasDaughters(cellID))//it has two daughters.
            {
                addPedGeneration(ref divtree, familytree[cellID].Daughter(0), 2 * index);
                addPedGeneration(ref divtree, familytree[cellID].Daughter(1), 2 * index + 1);
            }
        }
        /// <summary>
        /// similar to addPedGeneration, this one is recusion method to add the cellID instead of event time to the tree.
        /// </summary>
        /// <param name="divtreeCellID"></param>
        /// <param name="cellID"></param>
        /// <param name="index"></param>
        private void addPedGenerationCellID(ref Dictionary<int, Dictionary<int, BigInteger >> divtreeCellID, BigInteger  cellID, int index)
        {
            if (!divtreeCellID.ContainsKey(familytree[cellID].Generation))
            {
                Dictionary<int, BigInteger > thisgeneration = new Dictionary<int, BigInteger >();
                divtreeCellID.Add(familytree[cellID].Generation, thisgeneration);
            }
            divtreeCellID[familytree[cellID].Generation].Add(index, cellID);
            if (CellHasDaughters(cellID))
            {
                addPedGenerationCellID(ref divtreeCellID, familytree[cellID].Daughter(0), 2 * index);
                addPedGenerationCellID(ref divtreeCellID, familytree[cellID].Daughter(1), 2 * index + 1);
            }
        }

        //---------------------------------------seperation line-----------------------
        //below are the sections put the information into data structure for charting

        private int calculate2RaiseToN(int n)
        {
            int ret = 1;
            for (int i = 0; i < n; i++)
                ret *= 2;
            return ret;
        }

        private double determineXCoordiateStarting(Dictionary<int, Dictionary<int, double>> dic, int generationID, int indexID)
        {
            double X_Coordinate;
            int motherIndexID = (int)indexID / 2;
            //get the event_time as the X coordinate for this current timepoint
            if (generationID <= 0)
                X_Coordinate = 0;
            else
                X_Coordinate = dic[generationID - 1][motherIndexID];
            return X_Coordinate;
        }

        private string determineXCoordiateStartingLabel(Dictionary<int, Dictionary<int, int>> dicCellID, Dictionary<int, GenealogyInfo> familytree, int generationID, int indexID)
        {
            string X_CoordinateString;
            int X_coordinateID;
            int motherIndexID = (int)indexID / 2;
            //get the event_time as the X coordinate for this current timepoint
            if (generationID <= 0)
                X_coordinateID = dicCellID[0][0];//this is cellID for the founder cell at generation 0 and index 0;
            else
                X_coordinateID = dicCellID[generationID - 1][motherIndexID];
            X_CoordinateString = "cell:" + X_coordinateID;// +"\n"; +familytree[X_coordinateID].IgAffinity;  //here we remove the IgAffinity display for now. might need it later
            return X_CoordinateString;
        }
        //assume the eventTime has been update by adding the LastEventTime
        //Xcoordinate could be negative, means death.
        private double determineXCoordiateEnding(Dictionary<int, Dictionary<int, double>> dic, int generationID, int indexID)
        {
            double X_Coordinate;
            //int motherIndexID = (int)indexID / 2;
            //get the event_time as the X coordinate for this current timepoint

            X_Coordinate = dic[generationID][indexID];
            return X_Coordinate;
        }
        private string determineXCoordiateEndingLabel(Dictionary<int, Dictionary<int, BigInteger>> dicCellID, Dictionary<BigInteger, GenealogyInfo> familytree,
            int generationID, int indexID)
        {
            string X_CoordinateString;
            BigInteger  X_CoordinateCellID;
            //int motherIndexID = (int)indexID / 2;
            //get the event_time as the X coordinate for this current timepoint

            X_CoordinateCellID = dicCellID[generationID][indexID];
            X_CoordinateString = "Cell:" + X_CoordinateCellID;// +"\n" + familytree[X_CoordinateCellID].IgAffinity.ToString("e2");//remove this IgAffinity display for now and might need it later.
            return X_CoordinateString;
        }

        private double determineYCoordiate(Dictionary<int, Dictionary<int, double>> dic, int generationID, int indexID, double totalHeight)
        {
            //now let's determine the Y_Coordinate
            //Y_Coordinate determined by the indexID and generation#
            double Y_Coordinate;
            bool flag_half_bottom = false;
            int updateIndexID = indexID;
            if (generationID == 0)
                return 0;
            if (indexID >= Math.Pow(2, generationID) / 2) //this is upper half uinsg
            {
                flag_half_bottom = true;
                updateIndexID = calculate2RaiseToN(generationID) - 1 - indexID;
            }

            //now decide it
            Y_Coordinate = totalHeight * (1 - ((double)(updateIndexID + 1)) / (calculate2RaiseToN(generationID) / 2 + 2 - 1));
            if (flag_half_bottom)
                Y_Coordinate *= -1;
            return Y_Coordinate;
        }
        /// <summary>
        /// working horse to calculate the points and draw the tree.
        /// </summary>
        /// <param name="dic">pedigree tree data structure hold the tree </param>
        /// <param name="cA"></param>
        /// <param name="title"></param>
        private List<Series> drawChartPedTree(Dictionary<int, Dictionary<int, double>> dicDivTime,
                Dictionary<int, Dictionary<int, BigInteger>> dicCellID, Dictionary<BigInteger, GenealogyInfo> familyTree)
        {
            List<Series> chartingSeries = new List<Series>();
            //draw points first
            int n = dicDivTime.Count();//get the total generation numbers
            if (n <= 1)
            {

                return chartingSeries;//in this case, nothing wrong, but we just don't draw anything, since it has one one node
            }
            double max_time = 10;//starting at some value;

            //the total length is for Y axis.
            double totalHeight = Math.Pow(2, n);
            for (int i = 0; i < n; i++)//for each generation  and starting from the zero generation till the last one
            {
                //***get starting point first, 
                double X_CoordinateStarting, X_CoordinateEnding;
                double Y_CoordinateStarting, Y_CoordinateEnding;
                Dictionary<int, double> dicByGeneration = dicDivTime[i];//for this current generation
                List<int> keys = new List<int>(dicByGeneration.Keys);//keyed by index, which is relative to the Y position
                
                foreach (int indexID in keys)
                {
                    //List<Series> lst = new List<Series>();
                    Series s = new Series();
                    chartingSeries.Add(s);
                    //geting X coordinate first, this X coordinate is the based its mother cell,
                    //it is decided by the generation and the index in the last generation
                    X_CoordinateStarting = determineXCoordiateStarting(dicDivTime, i, indexID);
                    //also update the current event_time
                    if (X_CoordinateStarting < 0)//this is impossible, since the last one was dead
                    {
                        System.Console.WriteLine("something wrong in determine the pedigree data when drawing the tree, please check the data intergrity\n");
                        return null;
                    }
                    if (dicByGeneration[indexID] < 0)
                    {
                        dicByGeneration[indexID] = -1 * (dicByGeneration[indexID] * (-1) + X_CoordinateStarting);
                    }
                    else
                    {
                        dicByGeneration[indexID] = (dicByGeneration[indexID] + X_CoordinateStarting);
                    }
                    //check to get the maxTime for setting the scale of the x-axis
                    if (Math.Abs(dicByGeneration[indexID]) > max_time)
                        max_time = Math.Abs(dicByGeneration[indexID]);

                    //now let's determine the Y_Coordinate
                    //Y_Coordinate determined by the indexID and generation#

                    Y_CoordinateStarting = determineYCoordiate(dicDivTime, i, indexID, totalHeight);

                    s.Points.AddXY(Math.Abs(X_CoordinateStarting), Y_CoordinateStarting);
                    //s.Points[s.Points.Count - 1].Label = determineXCoordiateStartingLabel(dicCellID, familyTree, i, indexID);

                    //for the ending points, it could be two ending points for the division, 
                    //or single points for a death
                    //try to dicide how many points

                    //now for the ending point (1)**********
                    //X_coordinate is determined by the current event assuming it has been updated
                    X_CoordinateEnding = determineXCoordiateEnding(dicDivTime, i, indexID);
                    //Y_coordinate is determined differently
                    if (i < n - 1 && dicDivTime[i + 1].ContainsKey(indexID * 2)) //this case, the point has daughters
                    //we will get the ending point by detemine Y and  current eventTime as the X_coordinate
                    {
                        Y_CoordinateEnding = determineYCoordiate(dicDivTime, i + 1, indexID * 2, totalHeight);
                        s.Points.AddXY(Math.Abs(X_CoordinateEnding), Y_CoordinateEnding);
                        s.Points[s.Points.Count - 1].Label = determineXCoordiateEndingLabel(dicCellID, familyTree, i + 1, indexID * 2);
                        //s.SmartLabelStyle.Enabled = true;
                        //s.SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                    }
                    else   //for this case, we will get the ending point by taking the same level of Y, 
                    //but current eventTime as the x_coordinate
                    {
                        Y_CoordinateEnding = Y_CoordinateStarting + 0;//Y donesn't change
                        s.Points.AddXY(Math.Abs(X_CoordinateEnding), Y_CoordinateEnding);
                        //no label for this case.
                    }

                    //???decide the color
                    // Add series to the chart
                    //s.ChartType = SeriesChartType.Line;

                    //s.ChartArea = cA.Name;
                    s.MarkerSize = 5;
                    s.MarkerStyle = MarkerStyle.Circle;

                    s.Color = Color.Red;
                    if (X_CoordinateEnding < 0)
                    {
                        s.Color = Color.Blue;
                    }

                    //now for the ending point (2)**********
                    if (i < n - 1 && dicDivTime[i + 1].ContainsKey(indexID * 2))//there is second ending point
                    {
                        Series s1 = new Series();
                        chartingSeries.Add(s1);
                        X_CoordinateEnding = X_CoordinateEnding + 0;//X_coordinate is the same
                        //Y_coordinate is determined differently
                        Y_CoordinateEnding = determineYCoordiate(dicDivTime, i + 1, indexID * 2 + 1, totalHeight);
                        s1.Points.AddXY(Math.Abs(X_CoordinateStarting), Y_CoordinateStarting);
                        s1.Points.AddXY(Math.Abs(X_CoordinateEnding), Y_CoordinateEnding);
                        s1.Points[s1.Points.Count - 1].Label = determineXCoordiateEndingLabel(dicCellID, familyTree, i + 1, indexID * 2 + 1);
                        //s1.SmartLabelStyle.Enabled = true;
                        //s1.SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Partial;
                        //s1.SmartLabelStyle.
                        // Add series to the chart
                        //s1.ChartType = SeriesChartType.Line;

                        //s1.ChartArea = cA.Name;
                        s1.MarkerSize = 5;
                        s1.MarkerStyle = MarkerStyle.Circle;

                        s1.Color = Color.Red;
                        if (X_CoordinateEnding < 0)
                        {
                            s.Color = Color.Blue;
                        }
                    }
                    //chartingSeries.Add(lst);
                }//for each point/node in centain generation
            }//for each generation

            return chartingSeries;

            /*
            //string[] chartAxisLabel = { "x", "y", "z" };
            cA.AxisY.Minimum = -1 * this.calculate2RaiseToN(n) * 1.001;
            cA.AxisY.Maximum = this.calculate2RaiseToN(n) * 1.001;
            cA.AxisX.Minimum = 0 - 0.1;
            cA.AxisX.Maximum = max_time * 1.01;
            cA.AxisX.Title = "Time (mins)";
            cA.AxisY.Title = title;
            cA.AxisX.TitleFont = new Font("Arial", 8);
            cA.AxisY.TitleFont = new Font("Arial", 8);

            cA.AxisX.LabelStyle.Font = new Font("Arial", 5);
            cA.AxisY.LabelStyle.Font = new Font("Arial", 5);

            cA.AxisX.LabelStyle.Format = "##,#.0";
            cA.AxisY.LabelStyle.Format = "##,#.0";

            cA.Visible = true;
            cA.AxisY.MajorTickMark.Enabled = false;
            //cA.AxisY.MinorTickMark.Enabled = false ;
            cA.AxisY.LabelStyle.Enabled = false;
            //cA.AxisY.MajorGrid.Enabled = false;
            //cA.AxisX.MajorGrid.Enabled = false;
            cA.Position.Auto = true;
            //cA.Position.X = 3 ;
            //cA.Position.Y = 10 ;
            //cA.Position.Height = 90;//80-offset*5;
            //cA.Position.Width = 90;// 80 - offset * 5;
            //Color[] cls = { Color.LightCyan, Color.LightCyan, Color.LightCyan };
            //cA.BackColor = Color.Red;
            cA.AxisX.MajorGrid.LineColor = Color.LightGray;
            cA.AxisY.MajorGrid.LineColor = Color.LightGray;

            //zooming enabled
            cA.CursorY.IsUserEnabled = true;
            cA.CursorY.IsUserSelectionEnabled = true;
            cA.AxisY.ScaleView.Zoomable = true;
            cA.AxisY.ScrollBar.IsPositionedInside = true;*/
        }

        public List<Series> GetPedigreeTreeSeries(FounderInfo founder/*Dictionary<int, Dictionary<int, double>> dicDivTime, Dictionary<int, Dictionary<int, int>> dicDivCellID,
            Dictionary<int, GenealogyInfo> familyTree, string title, string note*/)
        {
            
            List<Series> chartingSeries = new List<Series>();//return data structure holding all the points/lines,etc

            if (reporter == null)
            {
                return null;
            }
            //here we assume, the reporter has been set up correctly, now we need to populate the familytree
            ExtractPedigreeData(founder);

            if (familytree == null)
                return null;

            //now we have familytree, get the pedgree data
            Dictionary<int, Dictionary<int, double>> pedTreeEventTime=GetPedDivTree(0, founder.Lineage_Id);
            Dictionary<int, Dictionary<int, BigInteger>> pedTreeCellID = GetPedDivTreeCellID(0, founder.Lineage_Id);

            //now we have these things, we can call to do draw the tree
            
            /*foreach (Control c in pChart.Controls)
                pChart.Controls.Remove(c);


            cChart = new Chart();
            ChartArea chartArear1 = new ChartArea();
            cChart.ChartAreas.Add(chartArear1);
            */

            chartingSeries=drawChartPedTree(pedTreeEventTime, pedTreeCellID, familytree);

            //*********************
            /*
            cChart.Titles.Add("Title_1");
            cChart.BackColor = Color.White;

            cChart.Titles[0].Text = title;

            // Set chart control location
            cChart.Location = new System.Drawing.Point(1, 8);

            // Set Chart control size
            //need to dynamically calculate the size based on the generation numbers.
            int n = dicDivTime.Count();//get the total generation numbers
            cChart.Size = new System.Drawing.Size(690, 690);
            //cChart.Size = new System.Drawing.Size((int)100*n, (int)(5*Math.Pow(2,n)));

            //pChart.AutoScroll = true;
            pChart.Controls.Add(cChart);*/

            chartTitle = "Cell Division Pedigree\nFounder Cell " + founder.Lineage_Id;// +"; Affinity: " + familytree[founder.Lineage_Id].IgAffinity.ToString("e2"); //remove this IgAffinity diplay for now and might need it later.
            chartXTitle = " Time ";
            chartYTitle = chartTitle;
            return chartingSeries;
        }

        public string GetChartTitle()
        {
            return chartTitle;
        }
        public string GetChartXTitle()
        {
            return chartXTitle;
        }
        public string GetChartYTitle()
        {
            return chartYTitle;
        }

        //======member declaration
        private Dictionary<BigInteger, GenealogyInfo> familytree;
        private Dictionary<int, string> cellsets;
        private int experiment_id;
        private ReporterBase reporter;
        private string chartTitle;
        private string chartXTitle;
        private string chartYTitle;

        //this structure contains following information <generation#,<index of cells, <cell_alt_id, eventTime>>>
    }//end of class

}//end of namespace
