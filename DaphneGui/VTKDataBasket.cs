/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿//#define WRITE_VTK_DATA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Daphne;

//using MathNet.Numerics.Distributions;
//using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;
using Kitware.VTK;
using System.Diagnostics;
using System.Windows.Media;
using Nt_ManifoldRing;



namespace DaphneGui
{
    /// <summary>
    /// entity encapsulating a molpop controller
    /// </summary>
    public class MolPopTypeController
    {

        // color as rgba
        //private double[] color = new double[4] { 0, 0, 0, 0 };
        /// <summary>
        /// identifier indicating the molpop distribution type
        /// </summary>
        protected MolPopDistributionType type;
        private string type_guid;

        /// <summary>
        /// constructor
        /// </summary>
        public MolPopTypeController()
        {

        }


        public string renderLabel { get; set; }

        /// <summary>
        /// accessor for the molpop's type variable
        /// </summary>
        public MolPopDistributionType Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// accessor for the type guid
        /// </summary>
        public string TypeGUID
        {
            get { return type_guid; }
            set { type_guid = value; }
        }
    }

    /// <summary>
    /// homogeneous molpop
    /// </summary>
    public class MolpopTypeHomogeneousController : MolPopTypeController
    {
        /// <summary>
        /// constant concentration
        /// </summary>
        public double level { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public MolpopTypeHomogeneousController(double lev)
        {
            level = lev;
            type = MolPopDistributionType.Homogeneous;
        }
    }

    /// <summary>
    /// linear molpop gradient
    /// </summary>
    public class MolpopTypeLinearController : MolPopTypeController
    {
        
        public double c1 { get; set; }  //1st face of boundary
        public double c2 { get; set; }  //2nd face of boundary
        public double x1 { get; set; }  //0
        public double x2 { get; set; }  //max extent of cube - x or y or z depending on dim
        public int dim { get; set; }    //0=YZ plane, 1=XZ plane, 2=XY plane

        /// <summary>
        /// constructor
        /// </summary>
        public MolpopTypeLinearController(double a, double b, double c, double d, int e)
        {
            c1 = a;
            c2 = b;
            x1 = c;
            x2 = d;
            dim = e;
            type = MolPopDistributionType.Linear;
        }
    }

    /// <summary>
    /// Gaussian distribution molpop
    /// </summary>
    public class MolpopTypeGaussianController : MolPopTypeController
    {
        /// <summary>
        /// the peak amplitude
        /// </summary>
        public double amplitude { get; set; }
        /// <summary>
        /// the controller region's name, if any
        /// </summary>
        public string regionName { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public MolpopTypeGaussianController(double amp, string region)
        {
            amplitude = amp;
            regionName = region;
            type = MolPopDistributionType.Gaussian;
        }
    }

    public class MolpopTypeExplicitController : MolPopTypeController
    {
        public double max;
        public MolpopTypeExplicitController(double max_conc)
        {
            type = MolPopDistributionType.Explicit;
            max = max_conc;
        }
    }

    ///// <summary>
    ///// custom distribution molpop
    ///// </summary>
    //public class MolpopTypeCustomController : MolPopTypeController
    //{
    //    /// <summary>
    //    /// name of the file containing the data
    //    /// </summary>
    //    public string datafile { get; set; }
    //    /// <summary>
    //    /// minimum concentration
    //    /// </summary>
    //    public double min { get; set; }
    //    /// <summary>
    //    /// maximum concentration
    //    /// </summary>
    //    public double max { get; set; }

    //    /// <summary>
    //    /// constructor
    //    /// </summary>
    //    /// <param name="file">data file name</param>
    //    public MolpopTypeCustomController(string file)
    //    {
    //        datafile = file;
    //        type = MolPopDistributionType.Custom;
    //    }
    //}

    /// <summary>
    /// encapsulates the environment data controller, in essence, the box for the outline
    /// </summary>
    public class VTKEnvironmentDataController
    {
        vtkCubeSource box;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKEnvironmentDataController()
        {
            box = vtkCubeSource.New();
        }

        /// <summary>
        /// retrieve the environment box for use in VTK pipelines
        /// </summary>
        public vtkCubeSource BoxSource
        {
            get { return box; }
        }

        /// <summary>
        /// set up the box for the environment
        /// </summary>
        public void setupBox(double extX, double extY, double extZ)
        {
            box.SetBounds(0, extX, 0, extY, 0, extZ);
        }
    }

    /// <summary>
    /// encapsulates the basic VTK data for the ecs chemistry rendering (vtkImageData)
    /// along with the individual molecular population controllers
    /// </summary>
    public class VTKECSDataController
    {
        private vtkImageData imageGrid;

        public Dictionary<string, RenderMol> RenderMolDict;

        public Dictionary<string, RenderPop> RenderPopDict;

        private Dictionary<string, MolPopTypeController> molpopTypeControllers;

        // TODO: Need to add repository for ecs color maps

        /// <summary>
        /// constructor
        /// </summary>
        public VTKECSDataController()
        {
            molpopTypeControllers = new Dictionary<string, MolPopTypeController>();
            RenderMolDict = new Dictionary<string,RenderMol>();
            RenderPopDict = new Dictionary<string,RenderPop>();
        }

        /// <summary>
        /// retrieve the dictionary of molpop type controllers
        /// </summary>
        public Dictionary<string, MolPopTypeController> MolpopTypeControllers
        {
            get { return molpopTypeControllers; }
        }

        /// <summary>
        /// retrieve the molpop vtkImageData grid for use in VTK pipelines
        /// </summary>
        public vtkImageData ImageGrid
        {
            get { return imageGrid; }
        }

        /// <summary>
        /// release allocated memory
        /// </summary>
        public void Cleanup()
        {
            molpopTypeControllers.Clear();
            RenderMolDict.Clear();
            RenderPopDict.Clear();
        }

        /// <summary>
        /// set up the image grid and box outline for the ecs
        /// </summary>
        public void setupGradient3D()
        {
            imageGrid = vtkImageData.New();

            // set up the grid and allocate data
            imageGrid.SetExtent(0, SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(0), 0, SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(1), 0, SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(2));
            imageGrid.SetSpacing(SimulationBase.dataBasket.Environment.Comp.Interior.StepSize(), SimulationBase.dataBasket.Environment.Comp.Interior.StepSize(), SimulationBase.dataBasket.Environment.Comp.Interior.StepSize());
            //imageGrid.SetOrigin(0.0, 0.0, 0.0);
            // the four component scalar data requires the type to be uchar
            imageGrid.SetScalarTypeToUnsignedChar();
            imageGrid.SetNumberOfScalarComponents(4);
            imageGrid.AllocateScalars();

            //the allocated space is not zeroed. when no data is being rendered,  cause random bits of color
            int NodesPerSide2 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(2);
            int NodesPerSide1 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(1);
            int NodesPerside0 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(0);
            for (int iz = 0; iz <= NodesPerSide2; iz++)
            {
                for (int iy = 0; iy <= NodesPerSide1; iy++)
                {
                    for (int ix = 0; ix <= NodesPerside0; ix++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            imageGrid.SetScalarComponentFromDouble(ix, iy, iz, i, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// set up the ecs gradient in 3D
        /// </summary>
        /// <param name="molpop">entity describing the molpop</param>
        /// <param name="region">pointer to the region controlling this gradient, if any</param>
        public void addGradient3D(ConfigMolecularPopulation molpop, RegionControl region)
        {
            if (molpopTypeControllers.ContainsKey(molpop.molpop_guid) == true)
            {
                MessageBox.Show("Duplicate molpop guid! Aborting insertion.");
                return;
            }

            MolPopTypeController molpopControl;

            // check here for linear, Gaussian, homogeneous...
            if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
            {
                molpopControl = new MolpopTypeGaussianController(((MolPopGaussian)molpop.mp_distribution).peak_concentration,
                                                                 ((MolPopGaussian)molpop.mp_distribution).gauss_spec.box_spec.box_guid);
            }
            else if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
            {
                ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;
                double x2 = envHandle.extent_x;

                switch (((MolPopLinear)(molpop.mp_distribution)).dim)
                {
                    case 0:
                        x2 = envHandle.extent_x;
                        break;
                    case 1:
                        x2 = envHandle.extent_y;
                        break;
                    case 2:
                        x2 = envHandle.extent_z;
                        break;
                    default:
                        break;
                }

                molpopControl = new MolpopTypeLinearController(((MolPopLinear)molpop.mp_distribution).boundaryCondition[0].concVal,
                                                               ((MolPopLinear)molpop.mp_distribution).boundaryCondition[1].concVal,
                                                               ((MolPopLinear)molpop.mp_distribution).x1,
                                                               x2,
                                                               ((MolPopLinear)molpop.mp_distribution).dim);

            }
            else if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
            {
                molpopControl = new MolpopTypeHomogeneousController(((MolPopHomogeneousLevel)molpop.mp_distribution).concentration);
            }
            else if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
            {
                MolPopExplicit mpc = (MolPopExplicit)molpop.mp_distribution;
                double max = mpc.conc.Max();
                molpopControl = new MolpopTypeExplicitController(max);
            }
            //else if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Custom)
            //{
            //    molpopControl = new MolpopTypeCustomController(((MolPopCustom)molpop.mp_distribution).custom_gradient_file_uri.LocalPath);
            //    //if (chemokine.populateSolfacCustom(molpop, ref molpopControl) == false)
            //    //{
            //    //    // there was a problem with creating the custom chemokine, do not proceed with inserting the molpop controller
            //    //    string messageBoxText = "Aborting custom chemokine insertion, possible file format problem in\n\n" + ((MolPopCustomGradient)molpop.mp_distribution).custom_gradient_file_uri.LocalPath;
            //    //    string caption = "Error creating custom chemokine";
            //    //    MessageBoxButton button = MessageBoxButton.OK;
            //    //    MessageBoxImage icon = MessageBoxImage.Error;

            //    //    // display the message box
            //    //    MessageBox.Show(messageBoxText, caption, button, icon);
            //    //    return;
            //    //}
            //}
            else
            {
                return;
            }

            molpopControl.TypeGUID = molpop.molecule.entity_guid;

            molpopControl.renderLabel = molpop.renderLabel;

            // add the controller to the dictionary
            molpopTypeControllers.Add(molpop.molpop_guid, molpopControl);
        }

        /// <summary>
        /// update the image gradient according to all ecs concentrations
        /// </summary>
        /// <param name="update">external flag to indicate if ecs rendering is on</param>
        /// <param name="modified">cause a redraw</param>
        public void updateGradients3D(bool update, bool modified)
        {
            if (update == false)
            {
                return;
            }

            bool first = true;
            //these are pre-evalulated/allocated to improve performance - axin
            int NodesPerSide2 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(2);
            int NodesPerSide1 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(1);
            int NodesPerside0 = SimulationBase.dataBasket.Environment.Comp.Interior.NodesPerSide(0);
            double[] point = new double[3];

            foreach (KeyValuePair<string, MolPopTypeController> kvp in molpopTypeControllers)
            {

                string renderLabel = kvp.Value.renderLabel;
                if (RenderPopDict[renderLabel].renderOn == false)continue;

                RenderMethod render_method = RenderPopDict[renderLabel].renderMethod;
                RenderMol render_mol = RenderMolDict[renderLabel];
                double[] mp_color = new double[4];
                mp_color[0] = render_mol.color.EntityColor.R;
                mp_color[1] = render_mol.color.EntityColor.G;
                mp_color[2] = render_mol.color.EntityColor.B;
                mp_color[3] = render_mol.color.EntityColor.A;
                if (render_mol.max == 0.0) continue;


                // generate scalar data
                double step_size = SimulationBase.dataBasket.Environment.Comp.Interior.StepSize();
                ScalarField molpop_sf = SimulationBase.dataBasket.Environment.Comp.Populations[kvp.Value.TypeGUID].Conc;

                double color_scale_factor = 0;
                double conc_scale_factor = render_mol.blendingWeight / (render_mol.max - render_mol.min);
                //conc range per shade.
                double conc_shade_factor = (render_mol.max - render_mol.min) / render_mol.shades;

                double val, conc;

                for (int iz = 0; iz < NodesPerSide2; iz++)
                {
                    for (int iy = 0; iy < NodesPerSide1; iy++)
                    {  
                        for (int ix = 0; ix < NodesPerside0; ix++)
                        {
                            //double[] point = { step_size * ix, step_size * iy, step_size * iz };
                            point[0] = step_size * ix;
                            point[1] = step_size * iy;
                            point[2] = step_size * iz;
                            conc = molpop_sf.Value(point);
                            if (conc < render_mol.min)conc = render_mol.min;
                            else if (conc > render_mol.max)conc = render_mol.max;

                            if (render_method == RenderMethod.MP_CONC)
                            {
                                color_scale_factor = (conc - render_mol.min) * conc_scale_factor;
                            }
                            else if (render_method == RenderMethod.MP_CONC_SHADE)
                            {
                                int nth = (int)((conc - render_mol.min) / conc_shade_factor);
                                color_scale_factor = nth * conc_shade_factor * conc_scale_factor;
                            }
                            else if (render_method == RenderMethod.MP_CONC_MIX_COLOR)
                            {
                                int nth = (int)((conc - render_mol.min) / conc_shade_factor);
                                Color c = render_mol.GetConcColor(nth);
                                mp_color[0] = c.R;
                                mp_color[1] = c.G;
                                mp_color[2] = c.B;
                                mp_color[3] = c.A;
                                color_scale_factor = 1;
                            }

                            // rgba
                            for (int i = 0; i < 4; i++)
                            {
                                if (first == true)
                                {
                                    val = mp_color[i] * color_scale_factor;
                                }
                                else
                                {
                                    val = imageGrid.GetScalarComponentAsDouble(ix, iy, iz, i) + mp_color[i] * color_scale_factor;
                                }
                                if (val > 255.0) val = 255.0;
                                // set the scalar data according to rgba
                                imageGrid.SetScalarComponentFromDouble(ix, iy, iz, i, val);
                            }
                        }
                    }
                }
                first = false;
            }

            // causes a redraw in the pipeline
            if (modified == true)
            {
                imageGrid.Modified();
            }
        }
    }

    /// <summary>
    /// entity encapsulating a cell track
    /// </summary>
    public class VTKCellTrackData
    {
        private vtkPolyData actualTrack;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellTrackData()
        {
            actualTrack = vtkPolyData.New();
        }

        /// <summary>
        /// clear the polydata
        /// </summary>
        public void Cleanup()
        {
            actualTrack.Dispose();
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the actual track
        /// </summary>
        public vtkPolyData ActualTrack
        {
            get { return actualTrack; }
        }

        public void GenerateActualPathPolyData(CellTrackData data)
        {
            int nPoints = data.Times.Count();

            vtkPoints points = vtkPoints.New();
            vtkPolyLine line = vtkPolyLine.New();

            line.GetPointIds().SetNumberOfIds(nPoints);
            for (int i = 0; i < nPoints; i++)
            {
                double[] position = data.Positions[i];

                points.InsertNextPoint(position[0], position[1], position[2]);
                line.GetPointIds().SetId(i, i);
            }

            vtkCellArray cells = vtkCellArray.New();

            cells.Allocate(1, 1);
            cells.InsertNextCell(line);

            vtkDoubleArray time_vtk = vtkDoubleArray.New();

            time_vtk.SetNumberOfComponents(1);
            time_vtk.SetNumberOfTuples(nPoints);
            time_vtk.SetName("time");
            for (int i = 0; i < nPoints; i++)
            {
                time_vtk.SetTuple1(i, data.Times[i]);
            }

            actualTrack.SetPoints(points);
            actualTrack.SetLines(cells);
            actualTrack.GetPointData().AddArray(time_vtk);
            actualTrack.GetPointData().SetScalars(time_vtk);
        }
    }

    /// <summary>
    /// encapsulate the needed VTK for handling all cells
    /// </summary>
    public class VTKCellDataController
    {
        // data related to the cells
        private vtkPolyData poly;
        private vtkPoints points;

        /// <summary>
        /// mapping cell index to color table entry
        /// </summary>
        private vtkIntArray cellColorMapper;

        private vtkIntArray cellID;
        private vtkLookupTable cellGenericColorTable, cellColorTable;

        // colormap - changed meaning in rendering scheme
        //now means <population_id, color_index>
        private Dictionary<int, int> colorMap;


        //record assigned cells index, incremented in AssignCell
        private long assignCellIndex = 0;
        
        //Dictionary<string, RenderCell> RenderCellDict;

        public Dictionary<string, RenderPop> RenderPopDict;


#if WRITE_VTK_DATA
        // data writer
        vtkPolyDataWriter writer;
#endif

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellDataController()
        {
            // Math functions (using here for bivariate colormap generation)
            //vtkColorTransferFunction cconv = vtkColorTransferFunction.New();
            //cconv.SetColorSpaceToHSV();
            //cconv.AddHSVPoint(0.0, 0.0, 1.0, 1.0);
            //double r = cconv.GetRedValue(0.0);
            //double g = cconv.GetGreenValue(0.0);
            //double b = cconv.GetBlueValue(0.0);
            
            // color map
            colorMap = new Dictionary<int, int>();

            // ColorBrewer RdPu5, but going from white (low) to RdPu (high, ending at 2nd to darkest color)
            List<uint[]> colorVals2 = new List<uint[]>();
            colorVals2.Add(new uint[3] { 254, 235, 226 });
            colorVals2.Add(new uint[3] { 251, 180, 185 });
            colorVals2.Add(new uint[3] { 247, 104, 161 });
            colorVals2.Add(new uint[3] { 197, 27, 138 });

            //generic "other attributes" vtkLookupTable
            int numColors = 256;
            cellGenericColorTable = vtkLookupTable.New();
            cellGenericColorTable.SetNumberOfTableValues(numColors);
            cellGenericColorTable.Build();
            vtkColorTransferFunction ctf = vtkColorTransferFunction.New();
            ctf.SetColorSpaceToRGB();
            for (int ii = 0; ii < colorVals2.Count; ii++)
            {
                ctf.AddRGBPoint((float)ii/(float)(colorVals2.Count - 1), (float)colorVals2[ii][0] / 255f, (float)colorVals2[ii][1] / 255f, (float)colorVals2[ii][2] / 255f);
            }
            double[] cv = new double[3];
            for (int jj = 0; jj < numColors; jj++)
            {
                float vv = (float)jj / (float)numColors;
                cv = ctf.GetColor(vv);
                cellGenericColorTable.SetTableValue(jj, cv[0], cv[1], cv[2], 1.0f);
            }


            //RenderCellDict = new Dictionary<string, RenderCell>();
            RenderPopDict = new Dictionary<string, RenderPop>();

#if WRITE_VTK_DATA
            writer = vtkPolyDataWriter.New();
#endif
        }

        /// <summary>
        /// accessor for the color table
        /// </summary>
        public vtkLookupTable CellGenericColorTable
        {
            get { return cellGenericColorTable; }
        }

        public vtkLookupTable CellColorTable
        {
            get { return cellColorTable; }
        }

        /// <summary>
        /// Retrieve the color map
        /// This is a dictionary mapping the cell set ID to the index in the ColorMap
        /// </summary>
        public Dictionary<int, int> ColorMap
        {
            get { return colorMap; }
        }

        /// <summary>
        /// allocate the color table
        /// </summary>
        /// <param name="num">number of entries</param>
        public void CreateCellColorTable(long num)
        {
            cellColorTable = vtkLookupTable.New();
            cellColorTable.SetNumberOfTableValues(num);
        }

        public void AddToCellColorTable(long idx, double r, double g, double b, double a)
        {
            cellColorTable.SetTableValue(idx, r, g, b, a);
        }

        /// <summary>
        /// retrieve the cell poly data
        /// </summary>
        public vtkPolyData Poly
        {
            get { return poly; }
        }

        /// <summary>
        /// set up the data structures to append cells
        /// </summary>
        /// <param name="numCells">number of cells</param>
        /// <param name="receptorInfo">Dictionary of guid / receptor name pairs</param>
        public void StartAllocatedCells(long numCells)
        {
            poly = vtkPolyData.New();
            
            points = vtkPoints.New();
            points.SetNumberOfPoints(numCells);

            cellID = vtkIntArray.New();
            cellID.SetNumberOfComponents(1);
            cellID.SetNumberOfValues(numCells);
            cellID.SetName("cellID");

            cellColorMapper = vtkIntArray.New();
            cellColorMapper.SetNumberOfComponents(1);
            cellColorMapper.SetNumberOfValues(numCells);
            cellColorMapper.SetName("cellColorMapper");

            // try this to start out with twice the array sizes needed to avoid the vtk getting stuck problem
            allocateArrays(numCells, 2, true);
        }


        public void resetAssignCellIndex()
        {
            assignCellIndex = 0;
        }

        public long getAssignCellIndex()
        {
            return assignCellIndex;
        }

        /// <summary>
        /// assign the attributes to a cell where the arrays have already been pre-allocated
        /// using StartAllocatedCells(int numCells)
        /// </summary>
        /// <param name="idx">VTK cell index</param>
        /// <param name="pos">cell position</param>
        /// <param name="id">cell id</param>
        /// <param name="color">cell color index (already mapped through ColorMap)</param>
        /// <param name="generation">division generation number</param>
        public void AssignCell(Cell cell)
        {

            //may cause this if user add and then remove cells without applying
            if (colorMap.ContainsKey(cell.Population_id) == false) return;
            int index = colorMap[cell.Population_id];
            if (index == -1) return;
            int color_start_index = index >> 16;
            int color_end_index = (index << 16) >> 16;

            long idx = assignCellIndex;
            assignCellIndex++;

            cellID.SetValue(idx, cell.Cell_id);

            var pos = cell.SpatialState.X;
            points.SetPoint(idx, pos[0], pos[1], pos[2]);

            //this should not happen
            if (RenderPopDict.ContainsKey(cell.renderLabel) == false)
            {
                return;
            }
            RenderPop render_pop = RenderPopDict[cell.renderLabel];
            int color_index;
            switch (render_pop.renderMethod)
            {
                case RenderMethod.CELL_TYPE:
                case RenderMethod.CELL_POP:
                    cellColorMapper.SetValue(idx, color_start_index);
                    break;
                case RenderMethod.CELL_DIV_STATE:
                case RenderMethod.CELL_DIV_SHADE:
                    color_index = color_start_index + cell.DividerState;
                    if (color_index > color_end_index) color_index = color_end_index;
                    cellColorMapper.SetValue(idx, color_index);
                    break;
                case RenderMethod.CELL_DIFF_STATE:
                case RenderMethod.CELL_DIFF_SHADE:
                    color_index = color_start_index + cell.DifferentiationState;
                    if (color_index > color_end_index) color_index = color_end_index;
                    cellColorMapper.SetValue(idx, color_index);
                    break;
                case RenderMethod.CELL_DEATH_STATE:
                case RenderMethod.CELL_DEATH_SHADE:
                    cellColorMapper.SetValue(idx, color_start_index + (cell.Alive ? 0 : 1));
                    break;
                case RenderMethod.CELL_GEN:
                case RenderMethod.CELL_GEN_SHADE:
                    //for generation, looping over available colors.
                    color_index = color_start_index + (cell.generation % (color_end_index - color_start_index + 1));
                    cellColorMapper.SetValue(idx, color_index);
                    break;
            }
        }

        /// <summary>
        /// finish the cell data structure by creating the poly data
        /// </summary>
        public void FinishCells()
        {
            //todo change pont allocation to points needed.
            var nPoints = points.GetNumberOfPoints();
            if (nPoints != this.assignCellIndex)
            {
                vtkPoints vp = vtkPoints.New();
                vp.SetNumberOfPoints(this.assignCellIndex);
                for (int i = 0; i < this.assignCellIndex; i++)
                {
                    double[] p = points.GetPoint((long)i);
                    vp.SetPoint(i, p[0], p[1], p[2]);
                }
                this.points = vp;
            }
            poly.SetPoints(points);
            poly.GetPointData().AddArray(cellColorMapper);
            poly.GetPointData().AddArray(cellID);
            //poly.GetPointData().AddArray(cellSet);
            //poly.GetPointData().AddArray(cellGeneration);
        }

        private void allocateArrays(long size, long factor, bool force = false)
        {
            if (cellID != null)
            {
                if (force == true || size > cellID.GetSize())
                {
                    // sleep (in units of 1ms) until redraw is completed
                    ((VTKFullGraphicsController)MainWindow.GC).WaitForRedraw(1);

                    points.SetNumberOfPoints(size * factor);
                    cellID.SetNumberOfValues(size * factor);
                    //cellSet.SetNumberOfValues(size * factor);
                    //cellGeneration.SetNumberOfValues(size * factor);
                    cellColorMapper.SetNumberOfValues(size * factor);
                }
                // look at the data size and set the max allowable index
                if (size != cellID.GetDataSize())
                {
                    points.SetNumberOfPoints(size);
                    cellID.SetNumberOfValues(size);
                    //cellSet.SetNumberOfValues(size);
                    //cellGeneration.SetNumberOfValues(size);
                    cellColorMapper.SetNumberOfValues(size);
                }
            }
        }

        /// <summary>
        /// update all cell positions using pre-allocated arrays
        /// </summary>
        public void UpdateAllocatedCells()
        {
            // allow zero arrays; that's needed in order to totally clear the cells after all of them die
            if (SimulationBase.dataBasket.Cells != null)// && Simulation.dataBasket.Cells.Count > 0)
            {
                // NOTE: Make sure that all arrays get updated or there will be memory problems.
                allocateArrays(SimulationBase.dataBasket.Cells.Count, 2);


                resetAssignCellIndex();
                foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
                {
                    AssignCell(kvp.Value);
                }
                if (points != null)
                {
                    points.Modified();
                }

#if WRITE_VTK_DATA
                writeVTKfile("cells", MainWindow.Sim.RenderFrame);
#endif
            }
        }

#if WRITE_VTK_DATA
        /// <summary>
        /// write the cell state (poly) to a file
        /// </summary>
        /// <param name="fName">file name</param>
        /// <param name="num">number indicating the file name</param>
        private void writeVTKfile(string fName, int num)
        {
            if (writer == null)
            {
                return;
            }
            writer.SetInput(poly);
            writer.SetFileName(fName + num.ToString("D4") + ".vtk");
            writer.SetFileTypeToBinary();
            writer.Write();
        }
#endif

        /// <summary>
        /// cleanup data
        /// </summary>
        public void CleanupCells()
        {
            if (poly != null)
            {
                poly.Dispose();
                poly = null;
            }
            if (points != null)
            {
                points.Dispose();
                points = null;
            }
            if (cellID != null)
            {
                cellID.Dispose();
                cellID = null;
            }
            //if (cellSet != null)
            //{
            //    cellSet.Dispose();
            //    cellSet = null;
            //}
            //if (cellGeneration != null)
            //{
            //    cellGeneration.Dispose();
            //    cellGeneration = null;
            //}
            if (cellColorTable != null)
            {
                cellColorTable.Dispose();
                cellColorTable = null;
            }
        }

        /// <summary>
        /// cleanup the cell controller
        /// </summary>
        public void Cleanup()
        {
            CleanupCells();
            colorMap.Clear();
            RenderPopDict.Clear();
        }
    }

    /// <summary>
    /// no vtk graphics connected, as is the case for the VatRC
    /// </summary>
    public class VTKNullDataBasket : IVTKDataBasket
    {
        public VTKNullDataBasket()
        {
        }

        public void SetupVTKData(Protocol protocol)
        {
        }

        public void UpdateData()
        {
        }

        public void Cleanup()
        {
        }
    }

    /// <summary>
    /// entity encapsulating the control of a simulation's graphics
    /// </summary>
    public class VTKFullDataBasket : IVTKDataBasket
    {
        // the environment
        private VTKEnvironmentDataController environmentDataController;
        // the cells
        private VTKCellDataController cellDataController;
        //  the ecs
        private VTKECSDataController ecsDataController;
        // dictionary of regions
        private Dictionary<string, RegionControl> regions;
        // dictionary holding track data keyed by cell id
        private Dictionary<int, VTKCellTrackData> cellTracks;
        /// <summary>
        /// constructor
        /// </summary>
        public VTKFullDataBasket()
        {
            // environment
            environmentDataController = new VTKEnvironmentDataController();

            // cells
            cellDataController = new VTKCellDataController();

            // ecs
            ecsDataController = new VTKECSDataController();

            // regions
            regions = new Dictionary<string, RegionControl>();

            // cell tracks
            cellTracks = new Dictionary<int, VTKCellTrackData>();
        }

        public void SetupVTKData(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                // for now
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;
            TissueScenario scenario = (TissueScenario)protocol.scenario;
            double useThisZValue,
                   gridStep = Cell.defaultRadius * 2;

            // clear VTK
            MainWindow.VTKBasket.Cleanup();
            SimulationBase.dataBasket.ResetTrackData();

            if (envHandle.extent_z < gridStep)
            {
                useThisZValue = gridStep;
            }
            else
            {
                useThisZValue = envHandle.extent_z;
            }
            environmentDataController.setupBox(envHandle.extent_x, envHandle.extent_y, useThisZValue);

            //compute how many color entries we need
            int nColor = 0;
            Dictionary<int, int> colorStartIndexMap = new Dictionary<int,int>();
            Dictionary<int, int> colorEndIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < scenario.cellpopulations.Count; i++)
            {
                string label = scenario.cellpopulations[i].renderLabel;
                //may happen for scenarios built earlier...
                if (label == null)label = scenario.cellpopulations[i].Cell.entity_guid;
                RenderPop rp = scenario.popOptions.GetCellRenderPop(label);
                //if no render options specified, assign a new one.
                if (rp == null)
                {
                    CellPopulation cp = scenario.cellpopulations[i];
                    string cellname = cp.Cell.CellName;
                    scenario.popOptions.AddRenderOptions(label, cp.Cell.CellName, true);
                    rp = scenario.popOptions.GetCellRenderPop(label);
                }

                if (cellDataController.RenderPopDict.ContainsKey(label) == false)
                {
                    cellDataController.RenderPopDict.Add(label, rp);
                }
                if (rp.renderOn == false)
                {
                    colorStartIndexMap.Add(scenario.cellpopulations[i].cellpopulation_id, -1);
                    colorEndIndexMap.Add(scenario.cellpopulations[i].cellpopulation_id, -1);
                    continue;
                }
                colorStartIndexMap.Add(scenario.cellpopulations[i].cellpopulation_id, nColor);
                RenderCell rc = MainWindow.SOP.GetRenderCell(label);
                //if not renderCell Exist for this by default
                if (rc == null)
                {
                    MainWindow.SOP.SelectedRenderSkin.AddRenderCell(label, scenario.cellpopulations[i].Cell.CellName);
                    rc = MainWindow.SOP.GetRenderCell(label);
                }
                switch (rp.renderMethod)
                {
                    case RenderMethod.CELL_TYPE:
                        nColor++;
                        break;
                    case RenderMethod.CELL_POP:
                        nColor++;
                        break;
                    case RenderMethod.CELL_DIV_STATE:
                        nColor += rc.div_state_colors.Count;
                        break;
                    case RenderMethod.CELL_DIV_SHADE:
                        nColor += rc.div_shade_colors.Count;
                        break;
                    case RenderMethod.CELL_DIFF_STATE:
                        nColor += rc.diff_state_colors.Count;
                        break;
                    case RenderMethod.CELL_DIFF_SHADE:
                        nColor += rc.diff_shade_colors.Count;
                        break;
                    case RenderMethod.CELL_DEATH_STATE:
                        nColor += rc.death_state_colors.Count;
                        break;
                    case RenderMethod.CELL_DEATH_SHADE:
                        nColor += rc.death_shade_colors.Count;
                        break;
                    case RenderMethod.CELL_GEN:
                        nColor += rc.gen_colors.Count;
                        break;
                    case RenderMethod.CELL_GEN_SHADE:
                        nColor += rc.gen_shade_colors.Count;
                        break;
                }
                colorEndIndexMap.Add(scenario.cellpopulations[i].cellpopulation_id, nColor-1);
            }
            //passing each cellpop's color index in color tabel to cellDataController
            cellDataController.ColorMap.Clear();
            foreach( var item in colorStartIndexMap)
            {
                int start_index = item.Value;
                int end_index = colorEndIndexMap[item.Key];
                //the first 16 bit is the start index, the second 16 bit is the end index
                int value = start_index == -1 ? -1 : ((start_index << 16) + end_index);
                cellDataController.ColorMap.Add(item.Key, value);
            }
            cellDataController.CreateCellColorTable(nColor);

            //add color
            Color color = Colors.Transparent;
            //cell pops index for sample type
            Dictionary<string, int> cellPopIndex = new Dictionary<string, int>();
            for (int i = 0; i < scenario.cellpopulations.Count; i++)
            {
                string label = scenario.cellpopulations[i].renderLabel;
                RenderPop rp = scenario.popOptions.GetCellRenderPop(label);
                if (rp == null || rp.renderOn == false) continue; //old senario may have rp =null
                RenderCell rc = MainWindow.SOP.GetRenderCell(label);
                if (rc == null) continue; //skin color for this missing?
                int color_index = colorStartIndexMap[scenario.cellpopulations[i].cellpopulation_id];

                //for diffrent cell populaiton of same type
                int pop_index = 0;
                if (cellPopIndex.ContainsKey(label) == false)
                {
                    cellPopIndex.Add(label, 1);
                }
                else
                {
                    pop_index = cellPopIndex[label];
                    //use last color if more cellpops of a cell type than colors specified.
                    if (pop_index >= rc.cell_pop_colors.Count)
                    {
                        pop_index = rc.cell_pop_colors.Count - 1;
                    }
                    cellPopIndex[label]++;
                }
                switch (rp.renderMethod)
                {
                    case RenderMethod.CELL_TYPE:
                        color = rc.base_color.EntityColor;
                        cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        break;
                    case RenderMethod.CELL_POP:
                        color = rc.cell_pop_colors[pop_index].EntityColor;
                        cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        break;
                    case RenderMethod.CELL_DIV_STATE:
                        for (int j = 0; j < rc.div_state_colors.Count; j++, color_index++)
                        {
                            color = rc.div_state_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;
                    case RenderMethod.CELL_DIV_SHADE:
                        for (int j = 0; j < rc.div_shade_colors.Count; j++, color_index++)
                        {
                            color = rc.div_shade_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;

                    case RenderMethod.CELL_DIFF_STATE:
                        for (int j = 0; j < rc.diff_state_colors.Count; j++, color_index++)
                        {
                            color = rc.diff_state_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;
                    case RenderMethod.CELL_DIFF_SHADE:
                        for (int j = 0; j < rc.diff_shade_colors.Count; j++, color_index++)
                        {
                            color = rc.diff_shade_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;

                    case RenderMethod.CELL_DEATH_STATE:
                        for (int j = 0; j < rc.death_state_colors.Count; j++, color_index++)
                        {
                            color = rc.death_state_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;
                    case RenderMethod.CELL_DEATH_SHADE:
                        for (int j = 0; j < rc.death_shade_colors.Count; j++, color_index++)
                        {
                            color = rc.death_shade_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;

                    case RenderMethod.CELL_GEN:
                        for (int j = 0; j < rc.gen_colors.Count; j++, color_index++)
                        {
                            color = rc.gen_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;

                    case RenderMethod.CELL_GEN_SHADE:
                        for (int j = 0; j < rc.gen_shade_colors.Count; j++, color_index++)
                        {
                            color = rc.gen_shade_colors[j].EntityColor;
                            cellDataController.AddToCellColorTable(color_index, color.ScR, color.ScG, color.ScB, color.ScA);
                        }
                        break;
                }
            }
            createAllocatedCells();

            // region controls
            createRegionControls();

            // ecs rendering
            // set up the 3d image grid for the ecs
            if (SimulationBase.dataBasket.Environment is ECSEnvironment)
            {
                ecsDataController.setupGradient3D();

                for (int i = 0; i < protocol.scenario.environment.comp.molpops.Count; i++)
                {
                    RegionControl region = null;

                    if (protocol.scenario.environment.comp.molpops[i].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                    {
                        region = regions[((MolPopGaussian)protocol.scenario.environment.comp.molpops[i].mp_distribution).gauss_spec.box_spec.box_guid];
                    }

                    string renderLabel = protocol.scenario.environment.comp.molpops[i].renderLabel;
                    //to accommodate older senario
                    if (renderLabel == null)
                    {
                        renderLabel = protocol.scenario.environment.comp.molpops[i].renderLabel = protocol.scenario.environment.comp.molpops[i].molecule.entity_guid;
                    }

                    // 3D gradient
                    ecsDataController.addGradient3D(protocol.scenario.environment.comp.molpops[i], region);

                    if (ecsDataController.RenderPopDict.ContainsKey(renderLabel) == false)
                    {
                        RenderPop rp = scenario.popOptions.GetMolRenderPop(renderLabel);
                        if (rp == null)
                        {
                            scenario.popOptions.AddRenderOptions(renderLabel, protocol.scenario.environment.comp.molpops[i].Name, false);
                            rp = scenario.popOptions.GetMolRenderPop(renderLabel);
                        }
                        ecsDataController.RenderPopDict.Add(renderLabel, rp);
                        RenderMol rm = MainWindow.SOP.GetRenderMol(renderLabel);
                        if (rm == null)
                        {
                            MainWindow.SOP.SelectedRenderSkin.AddRenderMol(renderLabel, protocol.scenario.environment.comp.molpops[i].Name);
                            rm = MainWindow.SOP.GetRenderMol(renderLabel);
                        }
                        ecsDataController.RenderMolDict.Add(renderLabel, rm);
                    }

                    // finish 3d gradient-related graphics after processing the last molpop
                    if (i == protocol.scenario.environment.comp.molpops.Count - 1)
                    {
                        // update all gradients; do not cause a redraw
                        ecsDataController.updateGradients3D(true, false);
                    }
                }
            }
        }
        
        /// <summary>
        /// free allocated memory
        /// </summary>
        public void Cleanup()
        {
            foreach (KeyValuePair<string, RegionControl> kvp in regions)
            {
                kvp.Value.CleanUp();
            }
            regions.Clear();

            cellDataController.Cleanup();
            ecsDataController.Cleanup();
            CleanupTracks();
        }

        /// <summary>
        /// cleanup the cell tracks
        /// </summary>
        public void CleanupTracks()
        {
            // reset the polydata
            foreach (KeyValuePair<int, VTKCellTrackData> kvp in cellTracks)
            {
                kvp.Value.Cleanup();
            }
            cellTracks.Clear();
        }

        public void AddGaussSpecRegionControl(GaussianSpecification gs)
        {
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                // for now
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)scenario.environment;
            // Find the box spec that goes with this gaussian spec
            BoxSpecification bs = gs.box_spec;

            RegionControl rc = new RegionControl(RegionShape.Ellipsoid);

            // box transform
            rc.SetTransform(bs.transform_matrix, RegionControl.PARAM_SCALE);
            // outer bounds of environment (not really needed for gauss_spec)
            rc.SetExteriorBounds(new double[] { 0, envHandle.extent_x,
                                                0, envHandle.extent_y,
                                                0, envHandle.extent_z });

            // NOTE: Not doing any callbacks or property changed notifications right now...

            regions.Add(bs.box_guid, rc);
        }
#if ALL_VTK
        public void AddRegionRegionControl(Region rr)
        {
            string box_guid = rr.region_box_spec_guid_ref;
            // Find the box spec that goes with this region
            BoxSpecification bs = MainWindow.SOP.Protocol.box_guid_box_dict[box_guid];

            RegionControl rc = new RegionControl(rr.region_type);

            // box transform
            rc.SetTransform(bs.transform_matrix, RegionControl.PARAM_SCALE);
            // outer bounds of environment
            rc.SetExteriorBounds(new double[] { 0, MainWindow.SOP.Protocol.scenario.environment.extent_x,
                                                0, MainWindow.SOP.Protocol.scenario.environment.extent_y,
                                                0, MainWindow.SOP.Protocol.scenario.environment.extent_z });

            // NOTE: Not doing any callbacks or property changed notifications right now...

            Regions.Add(box_guid, rc);
        }
#endif
        private void createRegionControls()
        {
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                // for now
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            GaussianSpecification next;

            scenario.resetGaussRetrieve();
            Dictionary<string, int> region_indexes = new Dictionary<string, int>();
            while ((next = scenario.nextGaussSpec()) != null)
            {
                AddGaussSpecRegionControl(next);
            }
#if ALL_VTK
            // Regions
            foreach (Region rr in MainWindow.SOP.Protocol.scenario.regions)
            {
                AddRegionRegionControl(rr);
            }
#endif
        }

        public void RemoveRegionControl(string current_guid)
        {
            regions[current_guid].CleanUp();
            regions.Remove(current_guid);
        }

        /// <summary>
        /// create the graphics for all cells, but use pre-allocated arrays for speed
        /// </summary>
        private void createAllocatedCells()
        {
            if (SimulationBase.dataBasket.Cells != null)
            {
                if (SimulationBase.dataBasket.Cells.Count > 0)
                {
                    cellDataController.StartAllocatedCells(SimulationBase.dataBasket.Cells.Count);

                    cellDataController.resetAssignCellIndex();
                    foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
                    {
                        cellDataController.AssignCell(kvp.Value);
                    }
                    cellDataController.FinishCells();
                }
            }
        }

        /// <summary>
        /// retrieve the environmentController object
        /// </summary>
        public VTKEnvironmentDataController EnvironmentController
        {
            get { return environmentDataController; }
        }

        /// <summary>
        /// retrieve the cellController object
        /// </summary>
        public VTKCellDataController CellController
        {
            get { return cellDataController; }
        }

        /// <summary>
        /// retrieve the ecsController object
        /// </summary>
        public VTKECSDataController ECSController
        {
            get { return ecsDataController; }
        }

        /// <summary>
        /// retrieve the list of regions
        /// </summary>
        public Dictionary<string, RegionControl> Regions
        {
            get { return regions; }
        }

        /// <summary>
        /// retrieve the cell tracks
        /// </summary>
        public Dictionary<int, VTKCellTrackData> CellTracks
        {
            get { return cellTracks; }
        }

        /// <summary>
        /// update the data in this repository for a simulation frame
        /// </summary>
        /// <param name="cells">dictionary of cells</param>
        public void UpdateData()
        {
            // update all the cells
            cellDataController.UpdateAllocatedCells();

            // ecs
            if (SimulationBase.dataBasket.Environment is ECSEnvironment)
            {
                ecsDataController.updateGradients3D(((VTKFullGraphicsController)MainWindow.GC).ECSController.RenderGradient, true);
            }
        }
    }

    /// <summary>
    /// Accumulate chart data for all molecular populations in a (simulation) compartment.
    /// Will be used by VatRC and CellRC workbenches.
    ///     VatRC one ChartDatBasket for environment
    ///     CellRC one ChartDataBasket each for environment, cytosol, and membrane
    /// </summary>
    public class ChartDataBasket
    {
        private Compartment comp;
        private double[] defaultLoc;
        private Dictionary<string, List<double>> DictGraphConcs;
        private List<double> ListTimes;

        public ChartDataBasket()
        {
        }

        public void SetupChartData(Compartment _comp, double[] _defaultLoc, List<double> listTimes, Dictionary<string, List<double>> dictGraphConcs, RenderPopOptions rpo)
        {
            comp = _comp;
            defaultLoc = (double[])_defaultLoc.Clone();
            ListTimes = listTimes;
            DictGraphConcs = dictGraphConcs;

            Cleanup();
            
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molname = kvp.Value.Molecule.Name;
                if (rpo.molPopOptions.Where(m => m.name == kvp.Value.Molecule.Name).Any())
                {
                    RenderPop rp = rpo.molPopOptions.Where(m => m.name == kvp.Value.Molecule.Name).First();

                    if (rp.renderOn)
                    {
                        DictGraphConcs.Add(kvp.Key, new List<double>());
                    }
                }
            }
        }

        public void UpdateData(double accumulatedTime)
        {
            ListTimes.Add(accumulatedTime);
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                if (DictGraphConcs.ContainsKey(kvp.Key) == true)
                {
                    DictGraphConcs[kvp.Key].Add(comp.Populations[kvp.Key].Conc.Value(defaultLoc));
                }
            }
        }

        public void Cleanup()
        {
            DictGraphConcs.Clear();
            ListTimes.Clear();
        }

    }

    /// <summary>
    /// chart graphics for the VatRC
    /// </summary>
    public class VTKVatRCDataBasket : IVTKDataBasket
    {
        private VatReactionComplex hSim;
        private ChartDataBasket chartData;

        public VTKVatRCDataBasket()
        {
            chartData = new ChartDataBasket();
        }

        public void SetupVTKData(Protocol protocol)
        {
            hSim = (VatReactionComplex)MainWindow.Sim;
            Compartment comp = SimulationBase.dataBasket.Environment.Comp;

            VatReactionComplexScenario scenarioHandle = (VatReactionComplexScenario)protocol.scenario;
            RenderPopOptions rpo = scenarioHandle.popOptions;

            chartData.SetupChartData(comp, new double[]{0.0, 0.0, 0.0}, hSim.ListTimes, hSim.DictGraphConcs, rpo);
        }

        public void UpdateData()
        {
            if (MainWindow.Sim.CheckFlag(SimulationBase.SIMFLAG_RENDER) == true)
            {
                chartData.UpdateData(hSim.AccumulatedTime);
            }
        }

        public void Cleanup()
        {
            chartData.Cleanup();
        }
    }

}
