//#define WRITE_VTK_DATA
//#define ALL_VTK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Daphne;

using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;
using Kitware.VTK;

namespace DaphneGui
{
    /// <summary>
    /// entity encapsulating a molpop controller
    /// </summary>
    public class MolPopTypeController
    {
        // molpop properties
        private bool renderGradient;
        private double blendingWeight;
        // color as rgba
        private double[] color = new double[4] { 0, 0, 0, 0 };
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
            renderGradient = false;
            blendingWeight = 1.0;
        }

        /// <summary>
        /// accessor for the render gradient state variable
        /// </summary>
        public bool RenderGradient
        {
            get { return renderGradient; }
            set { renderGradient = value; }
        }

        /// <summary>
        /// retrieve the color array
        /// </summary>
        public double[] Color
        {
            get { return color; }
        }

        /// <summary>
        /// retrieve the render weight for this molpop
        /// </summary>
        public double BlendingWeight
        {
            get { return blendingWeight; }
            set { blendingWeight = value; }
        }

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
        private Dictionary<string, MolPopTypeController> molpopTypeControllers;
        // TODO: Need to add repository for ecs color maps

        /// <summary>
        /// constructor
        /// </summary>
        public VTKECSDataController()
        {
            molpopTypeControllers = new Dictionary<string, MolPopTypeController>();
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
        }

        /// <summary>
        /// set up the image grid and box outline for the ecs
        /// </summary>
        public void setupGradient3D()
        {
            imageGrid = vtkImageData.New();

            // set up the grid and allocate data
            imageGrid.SetExtent(0, Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0), 0, Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1), 0, Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2));
            imageGrid.SetSpacing(Simulation.dataBasket.ECS.Space.Interior.StepSize(), Simulation.dataBasket.ECS.Space.Interior.StepSize(), Simulation.dataBasket.ECS.Space.Interior.StepSize());
            //imageGrid.SetOrigin(0.0, 0.0, 0.0);
            // the four component scalar data requires the type to be uchar
            imageGrid.SetScalarTypeToUnsignedChar();
            imageGrid.SetNumberOfScalarComponents(4);
            imageGrid.AllocateScalars();
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
                                                                 ((MolPopGaussian)molpop.mp_distribution).gaussgrad_gauss_spec_guid_ref);
            }
            else if (molpop.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
            {
                double x2 = MainWindow.SOP.Protocol.scenario.environment.extent_x;
                switch (((MolPopLinear)(molpop.mp_distribution)).dim)
                {
                    case 0:
                        x2 = MainWindow.SOP.Protocol.scenario.environment.extent_x;
                        break;
                    case 1:
                        x2 = MainWindow.SOP.Protocol.scenario.environment.extent_y;
                        break;
                    case 2:
                        x2 = MainWindow.SOP.Protocol.scenario.environment.extent_z;
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

            // set the remaining molpop controller fields
            molpopControl.RenderGradient = molpop.mp_render_on;
            // assign color and weight
            molpopControl.Color[0] = molpop.mp_color.R;
            molpopControl.Color[1] = molpop.mp_color.G;
            molpopControl.Color[2] = molpop.mp_color.B;
            // NOTE: keep an eye on this; we may have to clamp this to zero
            molpopControl.Color[3] = molpop.mp_color.A;
            molpopControl.BlendingWeight = molpop.mp_render_blending_weight;
            molpopControl.TypeGUID = molpop.molecule.entity_guid;

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

            foreach (KeyValuePair<string, MolPopTypeController> kvp in molpopTypeControllers)
            {
                if (kvp.Value.RenderGradient == false)
                {
                    continue;
                }

                double div = 0.0;

                if (kvp.Value.Type == MolPopDistributionType.Homogeneous)
                {
                    div = ((MolpopTypeHomogeneousController)kvp.Value).level;
                }
                else if (kvp.Value.Type == MolPopDistributionType.Linear)
                {
                    div = ((MolpopTypeLinearController)kvp.Value).c2;
                }
                else if (kvp.Value.Type == MolPopDistributionType.Gaussian)
                {
                    div = ((MolpopTypeGaussianController)kvp.Value).amplitude;
                }
                //else if (kvp.Value.Type == MolPopDistributionType.Custom)
                //{
                //    div = ((MolpopTypeCustomController)kvp.Value).max;
                //}

                if (div == 0.0)
                {
                    continue;
                }

                // generate scalar data
                for (int iz = 0; iz < Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2); iz++)
                {
                    for (int iy = 0; iy < Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1); iy++)
                    {
                        for (int ix = 0; ix < Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0); ix++)
                        {
                            double[] point = { Simulation.dataBasket.ECS.Space.Interior.StepSize() * ix, Simulation.dataBasket.ECS.Space.Interior.StepSize() * iy, Simulation.dataBasket.ECS.Space.Interior.StepSize() * iz };

                            double val,
                                   conc = Simulation.dataBasket.ECS.Space.Populations[kvp.Value.TypeGUID].Conc.Value(point),//Utilities.AddDoubleValues(chemokine.getChemokineConcentrations(idx)[kvp.Value.TypeGUID]),
                                   scaledConcentration = kvp.Value.BlendingWeight * conc / div;

                            // rgba
                            for (int i = 0; i < 4; i++)
                            {
                                if (first == true)
                                {
                                    val = kvp.Value.Color[i] * scaledConcentration;
                                }
                                else
                                {
                                    val = imageGrid.GetScalarComponentAsDouble(ix, iy, iz, i) + kvp.Value.Color[i] * scaledConcentration;
                                }
                                // set the scalar data according to rgba
                                imageGrid.SetScalarComponentFromDouble(ix, iy, iz, i, val > Byte.MaxValue ? Byte.MaxValue : val);
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
#if ALL_VTK
    /// <summary>
    /// entity encapsulating a cell track
    /// </summary>
    public class VTKCellTrackData
    {
        private vtkPolyData actualTrack,
                            standardTrack,
                            zeroForceTrack;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellTrackData()
        {
            actualTrack = vtkPolyData.New();
            standardTrack = vtkPolyData.New();
            zeroForceTrack = vtkPolyData.New();
        }

        /// <summary>
        /// clear the polydata
        /// </summary>
        public void Cleanup()
        {
            actualTrack.Dispose();
            standardTrack.Dispose();
            zeroForceTrack.Dispose();
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the actual track
        /// </summary>
        public vtkPolyData ActualTrack
        {
            get { return actualTrack; }
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the standard track
        /// </summary>
        public vtkPolyData StandardTrack
        {
            get { return standardTrack; }
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the zero force track
        /// </summary>
        public vtkPolyData ZeroForceTrack
        {
            get { return zeroForceTrack; }
        }

        /// <summary>
        /// construct polydata path for the selected cell
        /// </summary>
        /// <param name="tP">times for predicted points</param>
        /// <param name="xPredict">predicted positions</param>
        /// <param name="vPredict">predicted velocities</param>
        /// <returns>a pointer to the inserted vtk actor, null for error</returns>
        public void GenerateFitPathPolyData(int cellID, bool zeroForce)
        {
            int nPoints;
            List<ColumnVector> xPredict;
            List<ColumnVector> vPredict;
            if (zeroForce)
            {
                nPoints = MainWindow.Basket.Tracks[cellID].ZeroForceTPredict.Count();
                xPredict = MainWindow.Basket.Tracks[cellID].ZeroForceXPredict;
                vPredict = MainWindow.Basket.Tracks[cellID].ZeroForceVPredict;
            }
            else
            {
                nPoints = MainWindow.Basket.Tracks[cellID].StandardTPredict.Count();
                xPredict = MainWindow.Basket.Tracks[cellID].StandardXPredict;
                vPredict = MainWindow.Basket.Tracks[cellID].StandardVPredict;
            }

            // we can't draw a tube (line) segment with less than two points
            if (nPoints < 2)
            {
                return;
            }

            vtkPoints points = vtkPoints.New();
            vtkPolyLine line = vtkPolyLine.New();

            line.GetPointIds().SetNumberOfIds(nPoints);
            for (int i = 0; i < nPoints; i++)
            {
                points.InsertNextPoint(xPredict[0][i], xPredict[1][i], xPredict[2][i]);
                line.GetPointIds().SetId(i, i);
            }

            vtkCellArray cells = vtkCellArray.New();
            cells.Allocate(1, 1);
            cells.InsertNextCell(line);

            vtkDoubleArray velocity = vtkDoubleArray.New();
            velocity.SetNumberOfComponents(3);
            velocity.SetNumberOfTuples(nPoints);
            velocity.SetName("predicted_velocity");
            for (int i = 0; i < nPoints; i++)
            {
                velocity.SetTuple3(i, vPredict[0][i], vPredict[1][i], vPredict[2][i]);
            }

            if (zeroForce)
            {
                zeroForceTrack.SetPoints(points);
                zeroForceTrack.SetLines(cells);
                zeroForceTrack.GetPointData().AddArray(velocity);
                zeroForceTrack.GetPointData().SetVectors(velocity);
            }
            else
            {
                standardTrack.SetPoints(points);
                standardTrack.SetLines(cells);
                standardTrack.GetPointData().AddArray(velocity);
                standardTrack.GetPointData().SetVectors(velocity);
            }
        }

        public void GenerateActualPathPolyData(int cellID)
        {
            double[] time = MainWindow.Basket.Tracks[cellID].ActualTrackTimesArray;
            List<double[]> position = MainWindow.Basket.Tracks[cellID].ActualTrackPositions;

            int nPoints = position.Count();

            // we can't draw a tube (line) segment with less than two points
            if (nPoints < 2)
            {
                return;
            }

            vtkPoints points = vtkPoints.New();
            vtkPolyLine line = vtkPolyLine.New();

            line.GetPointIds().SetNumberOfIds(nPoints);
            for (int i = 0; i < nPoints; i++)
            {
                points.InsertNextPoint(position[i][0], position[i][1], position[i][2]);
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
                time_vtk.SetTuple1(i, time[i]);
            }

            actualTrack.SetPoints(points);
            actualTrack.SetLines(cells);
            actualTrack.GetPointData().AddArray(time_vtk);
            actualTrack.GetPointData().SetScalars(time_vtk);
        }
    }
#endif
    /// <summary>
    /// encapsulate the needed VTK for handling all cells
    /// </summary>
    public class VTKCellDataController
    {
        // data related to the cells
        private vtkPolyData poly;
        private vtkPoints points;
        private vtkIntArray cellID, cellSet, cellGeneration;
#if ALL_DATA
        private Dictionary<string, vtkDoubleArray> cellReceptorArrays;
        private vtkLookupTable cellSetColorTable, cellGenerationColorTable, cellGenericColorTable, bivariateColorTable;
#else
        private vtkLookupTable cellSetColorTable, cellGenerationColorTable, cellGenericColorTable;
#endif

        // colormap
        private Dictionary<int, int> colorMap;

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
#if ALL_DATA
            // arrays of receptor level attributes
            cellReceptorArrays = new Dictionary<string, vtkDoubleArray>();
#endif
            // ColorBrewer YlOrBr5
            List<uint[]> colorVals = new List<uint[]>();
            colorVals.Add(new uint[3] { 153, 52, 4 });
            colorVals.Add(new uint[3] { 217, 95, 14 });
            colorVals.Add(new uint[3] { 254, 153, 41 });
            colorVals.Add(new uint[3] { 254, 217, 142 });
            colorVals.Add(new uint[3] { 255, 255, 212 });

            // cell generation vtkLookupTable
            cellGenerationColorTable = vtkLookupTable.New();
            cellGenerationColorTable.SetNumberOfTableValues(colorVals.Count);
            cellGenerationColorTable.Build();
            for (int ii = 0; ii < colorVals.Count; ii++ )
            {
                cellGenerationColorTable.SetTableValue(ii, (float)colorVals[ii][0]/255f, (float)colorVals[ii][1]/255f, (float)colorVals[ii][2]/255f, 1.0f);
            }
            cellGenerationColorTable.SetRange(0, colorVals.Count - 1);
            cellGenerationColorTable.Build();

            // ColorBrewer RdPu5, but going from white (low) to RdPu (high, ending at 2nd to darkest color)
            List<uint[]> colorVals2 = new List<uint[]>();
            colorVals2.Add(new uint[3] { 254, 235, 226 });
            colorVals2.Add(new uint[3] { 251, 180, 185 });
            colorVals2.Add(new uint[3] { 247, 104, 161 });
            colorVals2.Add(new uint[3] { 197, 27, 138 });

            // generic "other attributes" vtkLookupTable
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
#if ALL_DATA
            // Red-Cyan 4 x 4 bivariate colormap
            List<uint[]> bvColors = new List<uint[]>();
            bvColors.Add(new uint[3] { 220, 220, 220 });
            bvColors.Add(new uint[3] { 226, 164, 143 });
            bvColors.Add(new uint[3] { 222, 110, 85 });
            bvColors.Add(new uint[3] { 219, 33, 40 });

            bvColors.Add(new uint[3] { 150, 197, 216 });
            bvColors.Add(new uint[3] { 160, 160, 160 });
            bvColors.Add(new uint[3] { 166, 102, 90 });
            bvColors.Add(new uint[3] { 167, 44, 50 });

            bvColors.Add(new uint[3] { 44, 176, 213 });
            bvColors.Add(new uint[3] { 93, 134, 144 });
            bvColors.Add(new uint[3] { 105, 105, 105 });
            bvColors.Add(new uint[3] { 116, 50, 55 });

            bvColors.Add(new uint[3] { 0, 160, 210 });
            bvColors.Add(new uint[3] { 0, 124, 145 });
            bvColors.Add(new uint[3] { 15, 91, 95 });
            bvColors.Add(new uint[3] { 54, 54, 54 });

            bivariateColorTable = vtkLookupTable.New();
            bivariateColorTable.SetNumberOfTableValues(bvColors.Count);
            bivariateColorTable.Build();
            for (int ii = 0; ii < bvColors.Count; ii++)
            {
                bivariateColorTable.SetTableValue(ii, (float)bvColors[ii][0] / 255f, (float)bvColors[ii][1] / 255f, (float)bvColors[ii][2] / 255f, 1.0f);
            }
            // Range is 0-4, and position is calculated as (i + 4*j) with i,j in range 0-1
            bivariateColorTable.SetRange(0, 4);
            bivariateColorTable.Build();
#endif

#if WRITE_VTK_DATA
            writer = vtkPolyDataWriter.New();
#endif
        }

        /// <summary>
        /// accessor for the color table
        /// </summary>
        public vtkLookupTable CellSetColorTable
        {
            get { return cellSetColorTable; }
        }

        /// <summary>
        /// accessor for the color table
        /// </summary>
        public vtkLookupTable CellGenerationColorTable
        {
            get { return cellGenerationColorTable; }
        }

        /// <summary>
        /// accessor for the color table
        /// </summary>
        public vtkLookupTable CellGenericColorTable
        {
            get { return cellGenericColorTable; }
        }
#if ALL_DATA
        /// <summary>
        /// accessor for the bivariate color table
        /// </summary>
        public vtkLookupTable BivariateColorTable
        {
            get { return bivariateColorTable; }
        }
#endif
        /// <summary>
        /// Retrieve the color map
        /// This is a dictionary mapping the cell set ID to the index in the CellSetColorTable
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
            // color table
            cellSetColorTable = vtkLookupTable.New();
            cellSetColorTable.SetNumberOfTableValues(num);
        }

        /// <summary>
        /// add a new color to the table
        /// </summary>
        /// <param name="idx">index of the color</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="a">opacity/alpha</param>
        public void AddCellSetColor(long idx, double r, double g, double b, double a)
        {
            cellSetColorTable.SetTableValue(idx, r, g, b, a);
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
#if ALL_DATA
        public void StartAllocatedCells(long numCells, Dictionary<string, string> receptorInfo)
#else
        public void StartAllocatedCells(long numCells)
#endif
        {
            poly = vtkPolyData.New();
            
            points = vtkPoints.New();
            points.SetNumberOfPoints(numCells);

            cellID = vtkIntArray.New();
            cellID.SetNumberOfComponents(1);
            cellID.SetNumberOfValues(numCells);
            cellID.SetName("cellID");

            cellSet = vtkIntArray.New();
            cellSet.SetNumberOfComponents(1);
            cellSet.SetNumberOfValues(numCells);
            cellSet.SetName("cellSet");

            cellGeneration = vtkIntArray.New();
            cellGeneration.SetNumberOfComponents(1);
            cellGeneration.SetNumberOfValues(numCells);
            cellGeneration.SetName("generation");
#if ALL_DATA
            foreach (KeyValuePair<string, string> kvp in receptorInfo)
            {
                vtkDoubleArray cellReceptorRatio = vtkDoubleArray.New();
                cellReceptorRatio.SetNumberOfComponents(1);
                cellReceptorRatio.SetNumberOfValues(numCells);
                // rely on receptor name being "nice" and unique already
                cellReceptorRatio.SetName(kvp.Value);
                cellReceptorArrays.Add(kvp.Key, cellReceptorRatio);
            }
#endif
            // try this to start out with twice the array sizes needed to avoid the vtk getting stuck problem
            allocateArrays(numCells, 2, true);
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
        public void AssignCell(long idx, Cell cell)
        {
            double[] pos = cell.SpatialState.X;
            int id = cell.Cell_id;
            int color = ColorMap[cell.Population_id];
            int generation = 0;

            points.SetPoint(idx, pos[0], pos[1], pos[2]);
            cellID.SetValue(idx, id);
            cellSet.SetValue(idx, color);
            cellGeneration.SetValue(idx, generation);
#if ALL_VTK
            // NOTE: there may be other cells in the future that have chemokine receptors besides motile cells
            if (motile == true)
            {
                foreach (KeyValuePair<string, ChemokineReceptor> kvp in ((MotileCell)cell).ChemokineReceptors)
                {
                    cellReceptorArrays[kvp.Key].SetValue(idx, kvp.Value.U);
                }
            }
            else
            {
                foreach (KeyValuePair<string, vtkDoubleArray> kvp in cellReceptorArrays)
                {
                    kvp.Value.SetValue(idx, 0.0);
                }
            }
#endif
        }

        /// <summary>
        /// finish the cell data structure by creating the poly data
        /// </summary>
        public void FinishCells()
        {
            cellSetColorTable.Build();
            poly.SetPoints(points);
            poly.GetPointData().AddArray(cellID);
            poly.GetPointData().AddArray(cellSet);
            poly.GetPointData().AddArray(cellGeneration);
#if ALL_DATA
            foreach (KeyValuePair<string, vtkDoubleArray> kvp in cellReceptorArrays)
            {
                poly.GetPointData().AddArray(kvp.Value);
            }
#endif
        }

        private void allocateArrays(long size, long factor, bool force = false)
        {
            if (cellID != null)
            {
                if (force == true || size > cellID.GetSize())
                {
                    // sleep (in units of 1ms) until redraw is completed
                    MainWindow.GC.WaitForRedraw(1);

                    points.SetNumberOfPoints(size * factor);
                    cellID.SetNumberOfValues(size * factor);
                    cellSet.SetNumberOfValues(size * factor);
                    cellGeneration.SetNumberOfValues(size * factor);
#if ALL_DATA
                    foreach (KeyValuePair<string, vtkDoubleArray> kvp in cellReceptorArrays)
                    {
                        kvp.Value.SetNumberOfValues(size * factor);
                    }
#endif
                }
                // look at the data size and set the max allowable index
                if (size != cellID.GetDataSize())
                {
                    points.SetNumberOfPoints(size);
                    cellID.SetNumberOfValues(size);
                    cellSet.SetNumberOfValues(size);
                    cellGeneration.SetNumberOfValues(size);
#if ALL_DATA
                    foreach (KeyValuePair<string, vtkDoubleArray> kvp in cellReceptorArrays)
                    {
                        kvp.Value.SetNumberOfValues(size);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// update all cell positions using pre-allocated arrays
        /// </summary>
        public void UpdateAllocatedCells()
        {
            // allow zero arrays; that's needed in order to totally clear the cells after all of them die
            if (Simulation.dataBasket.Cells != null)// && Simulation.dataBasket.Cells.Count > 0)
            {
                // NOTE: Make sure that all arrays get updated or there will be memory problems.
                allocateArrays(Simulation.dataBasket.Cells.Count, 2);

                long i = 0;

                foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
                {
                    AssignCell(i++, kvp.Value);
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
            if (cellSet != null)
            {
                cellSet.Dispose();
                cellSet = null;
            }
            if (cellGeneration != null)
            {
                cellGeneration.Dispose();
                cellGeneration = null;
            }
#if ALL_DATA
            List<string> list = new List<string>();

            foreach(string key in cellReceptorArrays.Keys)
            {
                list.Add(key);
            }
            foreach (string key in list)
            {
                if (cellReceptorArrays[key] != null)
                {
                    cellReceptorArrays[key].Dispose();
                    cellReceptorArrays[key] = null;
                }
            }
#endif
        }

        /// <summary>
        /// cleanup the cell controller
        /// </summary>
        public void Cleanup()
        {
            CleanupCells();
            colorMap.Clear();
#if ALL_DATA
            cellReceptorArrays.Clear();
#endif
        }
    }

    /// <summary>
    /// entity encapsulating the control of a simulation's graphics
    /// </summary>
    public class VTKDataBasket
    {
        // the environment
        private VTKEnvironmentDataController environmentDataController;
        // the cells
        private VTKCellDataController cellDataController;
        //  the ecs
        private VTKECSDataController ecsDataController;
        // dictionary of regions
        private Dictionary<string, RegionControl> regions;
#if ALL_VTK
        // dictionary holding track data keyed by cell id
        private Dictionary<int, VTKCellTrackData> cellTracks;
        // dictionary relating cell receptor guids to names
        private Dictionary<string, string> cellReceptorGuidNames;
        // reverse dictionary relating cell receptor names to guids
        private Dictionary<string, string> cellReceptorNameGuids;
        // dictionary storing cell receptor max concentrations keyed by receptor name
        // so can get values easily based on "color by" receptor name string in GraphicsController
        private Dictionary<string, double> cellReceptorMaxConcs;
        // receptors to compare
        private bool compReceptors = false;
        private string compReceptor1;
        private string compReceptor2;
#endif
        /// <summary>
        /// constructor
        /// </summary>
        public VTKDataBasket()
        {
            // environment
            environmentDataController = new VTKEnvironmentDataController();

            // cells
            cellDataController = new VTKCellDataController();

            // ecs
            ecsDataController = new VTKECSDataController();

            // regions
            regions = new Dictionary<string, RegionControl>();
#if ALL_VTK
            // cell tracks
            cellTracks = new Dictionary<int, VTKCellTrackData>();

            // cell receptor info
            cellReceptorGuidNames = new Dictionary<string, string>();
            cellReceptorNameGuids = new Dictionary<string, string>();
            cellReceptorMaxConcs = new Dictionary<string, double>();
#endif
        }

        public void SetupVTKData(Protocol protocol)
        {
            double useThisZValue,
                   gridStep = Cell.defaultRadius * 2;

            // clear VTK
            MainWindow.VTKBasket.Cleanup();
#if ALL_VTK
            MainWindow.Basket.ResetTrackData();
#endif

            if (protocol.scenario.environment.extent_z < gridStep)
            {
                useThisZValue = gridStep;
            }
            else
            {
                useThisZValue = protocol.scenario.environment.extent_z;
            }
            environmentDataController.setupBox(protocol.scenario.environment.extent_x, protocol.scenario.environment.extent_y, useThisZValue);

            cellDataController.CreateCellColorTable(protocol.scenario.cellpopulations.Count);
            for (int i = 0; i < protocol.scenario.cellpopulations.Count; i++)
            {
                // add the cell set's color to the color table
                cellDataController.AddCellSetColor(i,
                                               protocol.scenario.cellpopulations[i].cellpopulation_color.ScR,
                                               protocol.scenario.cellpopulations[i].cellpopulation_color.ScG,
                                               protocol.scenario.cellpopulations[i].cellpopulation_color.ScB,
                                               protocol.scenario.cellpopulations[i].cellpopulation_color.ScA);
                // create the color map entry
                if (cellDataController.ColorMap.ContainsKey(protocol.scenario.cellpopulations[i].cellpopulation_id) == false)
                {
                    cellDataController.ColorMap.Add(protocol.scenario.cellpopulations[i].cellpopulation_id, i);
                }
            }
            CreateAllocatedCells();

            // region controls
            MainWindow.VTKBasket.CreateRegionControls();

            // ecs rendering
            // set up the 3d image grid for the ecs
            ecsDataController.setupGradient3D();

            for (int i = 0; i < protocol.scenario.environment.ecs.molpops.Count; i++)
            {
                RegionControl region = null;

                if (protocol.scenario.environment.ecs.molpops[i].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    region = regions[((MolPopGaussian)protocol.scenario.environment.ecs.molpops[i].mp_distribution).gaussgrad_gauss_spec_guid_ref];
                }

                // 3D gradient
                ecsDataController.addGradient3D(protocol.scenario.environment.ecs.molpops[i], region);

                // finish 3d gradient-related graphics after processing the last molpop
                if (i == protocol.scenario.environment.ecs.molpops.Count - 1)
                {
                    // update all gradients; do not cause a redraw
                    ecsDataController.updateGradients3D(true, false);
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
#if ALL_VTK
            CleanupTracks();
            cellReceptorGuidNames.Clear();
            cellReceptorNameGuids.Clear();
            cellReceptorMaxConcs.Clear();
#endif
        }
#if ALL_VTK

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
#endif
        public void AddGaussSpecRegionControl(GaussianSpecification gs)
        {
            string box_guid = gs.gaussian_spec_box_guid_ref;
            // Find the box spec that goes with this gaussian spec
            BoxSpecification bs = MainWindow.SOP.Protocol.scenario.box_guid_box_dict[box_guid];

            RegionControl rc = new RegionControl(RegionShape.Ellipsoid);

            // box transform
            rc.SetTransform(bs.transform_matrix, RegionControl.PARAM_SCALE);
            // outer bounds of environment (not really needed for gauss_spec)
            rc.SetExteriorBounds(new double[] { 0, MainWindow.SOP.Protocol.scenario.environment.extent_x,
                                                0, MainWindow.SOP.Protocol.scenario.environment.extent_y,
                                                0, MainWindow.SOP.Protocol.scenario.environment.extent_z });

            // NOTE: Not doing any callbacks or property changed notifications right now...

            regions.Add(box_guid, rc);
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
        public void CreateRegionControls()
        {
            // Gaussian specs
            foreach (GaussianSpecification gs in MainWindow.SOP.Protocol.scenario.gaussian_specifications)
            {
                AddGaussSpecRegionControl(gs);
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
        public void CreateAllocatedCells()
        {
            if (Simulation.dataBasket.Cells != null)
            {
                if (Simulation.dataBasket.Cells.Count > 0)
                {
#if ALL_VTK
                    // NOTE: For now take the receptor info from an example cell. Should probably use the Chemokine
                    // or, like in the chemokine construction, go through the Protocol molpops to see which solfac types are
                    // actually used, and then get receptor guid/name pairs from molpop types...
                    MotileCell example_cell = null;
                    int jj = 0;

                    foreach (KeyValuePair<int, BaseCell> kvp in cells)
                    {
                        if (kvp.Value.isMotileBaseType() == true)
                        {
                            example_cell = (MotileCell)kvp.Value;
                            break;
                        }
                    }
                    if (example_cell == null)
                    {
                        return;
                    }

                    System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9]");
                    foreach (KeyValuePair<string, ChemokineReceptor> kvp in example_cell.ChemokineReceptors)
                    {
                        string receptor_name = "xx";
                        foreach (SolfacType st in MainWindow.SOP.Protocol.entity_repository.solfac_types)
                        {
                            if (st.solfac_type_guid == kvp.Key)
                            {
                                // make the name "nice" for things like VTK array names
                                receptor_name = rgx.Replace(st.solfac_type_receptor_name, "_");
                                // make sure receptor name string is unique
                                if (this.CellReceptorMaxConcs.ContainsKey(receptor_name))
                                {
                                    receptor_name = receptor_name + "_" + jj;
                                }
                            }
                        }
                        this.cellReceptorGuidNames.Add(kvp.Key, receptor_name);
                        this.cellReceptorNameGuids.Add(receptor_name, kvp.Key);
                        this.cellReceptorMaxConcs.Add(receptor_name, 0f);
                        jj += 1;
                    }
                    // Loop through cell types, and then through receptor params for each type to find max receptor conc over all cell types
                    foreach (CellSubset ct in MainWindow.SOP.Protocol.entity_repository.cell_subsets)
                    {
                        //skg 6/1/12
                        if (ct.cell_subset_type.baseCellType == CellBaseTypeLabel.BCell)
                        {
                            BCellSubsetType bcst = (BCellSubsetType)ct.cell_subset_type;
                            foreach (ReceptorParameters rp in bcst.cell_subset_type_receptor_params)
                            {
                                double a = 1.0;
                                double max = (rp.receptor_params.ckr_pi * a) / rp.receptor_params.ckr_delta;
                                // if (this.cellReceptorMaxConcs.ContainsKeythis.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]) && max > this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]])
                                if (this.cellReceptorGuidNames.ContainsKey(rp.receptor_solfac_type_guid_ref) && max > this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]])
                                {
                                    this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]] = max;
                                }
                            }
                        }
                        else if (ct.cell_subset_type.baseCellType == CellBaseTypeLabel.TCell)
                        {
                            TCellSubsetType tcst = (TCellSubsetType)ct.cell_subset_type;
                            foreach (ReceptorParameters rp in tcst.cell_subset_type_receptor_params)
                            {
                                double a = 1.0;
                                double max = (rp.receptor_params.ckr_pi * a) / rp.receptor_params.ckr_delta;
                                // if (this.cellReceptorMaxConcs.ContainsKeythis.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]) && max > this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]])
                                if (this.cellReceptorGuidNames.ContainsKey(rp.receptor_solfac_type_guid_ref) && max > this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]])
                                {
                                    this.cellReceptorMaxConcs[this.cellReceptorGuidNames[rp.receptor_solfac_type_guid_ref]] = max;
                                }
                            }
                        }
                    }

                    // TODO: Bind these to GUI somehow instead of hard-coding...
                    compReceptors = false;
                    List<string> receptorNames = this.cellReceptorMaxConcs.Keys.ToList<string>();
                    if (receptorNames.Count > 1)
                    {
                        compReceptors = true;
                        compReceptor1 = receptorNames[0];
                        compReceptor2 = receptorNames[1];
                    }
                    else
                    {
                        compReceptors = false;
                        compReceptor1 = "";
                        compReceptor2 = "";
                    }
                    cellController.StartAllocatedCells(Simulation.dataBasket.Cells.Count, this.cellReceptorGuidNames);
#else
                    cellDataController.StartAllocatedCells(Simulation.dataBasket.Cells.Count);
#endif

                    long i = 0;

                    foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
                    {
                        cellDataController.AssignCell(i++, kvp.Value);
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
#if ALL_VTK
        /// <summary>
        /// retrieve the cell tracks
        /// </summary>
        public Dictionary<int, VTKCellTrackData> CellTracks
        {
            get { return cellTracks; }
        }

        /// <summary>
        /// retrieve dictionary of maximum concentrations of cell receptors keyed by receptor name string
        /// </summary>
        public Dictionary<string, double> CellReceptorMaxConcs
        {
            get { return cellReceptorMaxConcs; }
        }

        /// <summary>
        /// First receptor name to use in relative level comparison
        /// </summary>
        public string CompReceptor1
        {
            get { return compReceptor1; }
        }

        /// <summary>
        /// Second receptor name to use in relative level comparison
        /// </summary>
        public string CompReceptor2
        {
            get { return compReceptor2; }
        }

        /// <summary>
        /// Boolean flag which indicates whether receptor comparison should be done at all.
        /// </summary>
        public bool CompReceptors
        {
            get { return compReceptors; }
        }

        /// <summary>
        /// access a cell track by key; if it doesn't exist create it
        /// and load data from database into main databasket
        /// then generate polydata for any available original track and track fit data
        /// </summary>
        /// <param name="key">the cell id is the key</param>
        /// <returns></returns>
        public VTKCellTrackData GetCellTrack(int key)
        {
            if (CellTracks.ContainsKey(key) == false)
            {
                CellTracks.Add(key, new VTKCellTrackData());
            }
            // Try to load actual trajectory data in to DataBasket
            if (MainWindow.Basket.ConnectToExperiment())
            {
                CellTrackData data = MainWindow.Basket.GetCellTrack(key);
                // Generate track polydata for all available track data
                // but don't regenerate if already done
                if (data.ActualTrackTimes != null && CellTracks[key].ActualTrack.GetNumberOfPoints() == 0)
                {
                    CellTracks[key].GenerateActualPathPolyData(key);
                }
                if (data.StandardTPredict != null && CellTracks[key].StandardTrack.GetNumberOfPoints() == 0)
                {
                    CellTracks[key].GenerateFitPathPolyData(key, false);
                }
                if (data.ZeroForceTPredict != null && CellTracks[key].ZeroForceTrack.GetNumberOfPoints() == 0)
                {
                    CellTracks[key].GenerateFitPathPolyData(key, true);
                }
            }
            return CellTracks[key];
        }
#endif
        /// <summary>
        /// update the data in this repository for a simulation frame
        /// </summary>
        /// <param name="cells">dictionary of cells</param>
        public void UpdateData()
        {
            // update all the cells
            cellDataController.UpdateAllocatedCells();

            // ecs
            ecsDataController.updateGradients3D(MainWindow.GC.ECSController.RenderGradient, true);
        }
    }
}
