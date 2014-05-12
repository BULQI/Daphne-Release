//#define ALL_GRAPHICS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Data;
using System.Windows.Forms.Integration;

using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;
using Kitware.VTK;

using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// maintains each vtk prop in the scene
    /// </summary>
    public class GraphicsProp
    {
        /// <summary>
        /// simple constructor
        /// </summary>
        public GraphicsProp()
        {
            inScene = false;
            prop = null;
        }

        /// <summary>
        /// cleanup vtk
        /// </summary>
        public void cleanup(vtkRenderWindow rw)
        {
            if (prop != null)
            {
                // TODO: Need to add a vtkRenderWindow reference to each GraphicsProp so
                //   I can uncomment this addToScene call and then revert to definition of
                //   addToScene to not need a render window passed to it...
                addToScene(rw, false);
                prop.Dispose();
                prop = null;
            }
        }

        /// <summary>
        /// add/remove the prop; prevent multiple insertions/deletions
        /// </summary>
        /// <param name="rw">handle to the render window</param>
        /// <param name="add">indicates action</param>
        public void addToScene(vtkRenderWindow rw, bool add)
        {
            // TODO: Probably need to be passing a rwc or renderer to addToScene...
            if (prop != null)
            {
                if (add == true && inScene == false)
                {
                    inScene = true;
                    rw.GetRenderers().GetFirstRenderer().AddViewProp(prop);
                }
                else if (add == false && inScene == true)
                {
                    inScene = false;
                    rw.GetRenderers().GetFirstRenderer().RemoveViewProp(prop);
                }
            }
        }

        /// <summary>
        /// is the actor in the scene?
        /// </summary>
        public bool InScene
        {
            // TODO: Need to pass the rwc or something here...
            get { return inScene; }
        }

        /// <summary>
        /// retrieve the prop
        /// </summary>
        public vtkProp Prop
        {
            get { return prop; }
            set { prop = value; }
        }

        private vtkProp prop;
        private bool inScene;
    }

    /// <summary>
    /// encapsulates the VTK rendering pipeline for the environment
    /// </summary>
    public class VTKEnvironmentController
    {
        private GraphicsProp boxActor;
        private bool renderBox;
        private vtkRenderWindow rw;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="_rw">handle to the render window</param>
        public VTKEnvironmentController(vtkRenderWindow _rw)
        {
            boxActor = new GraphicsProp();
            renderBox = true;
            rw = _rw;
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the box actor
        /// </summary>
        public GraphicsProp BoxActor
        {
            get { return boxActor; }
        }

        /// <summary>
        /// release allocated memory
        /// </summary>
        public void Cleanup()
        {
            boxActor.cleanup(rw);
        }

        /// <summary>
        /// set up the graphics pipeline for the environment box
        /// </summary>
        public void setupPipeline()
        {
            // a simple box for quick and non-occluding indication of a volume
            vtkActor box = vtkActor.New();
            vtkOutlineFilter outlineFilter = vtkOutlineFilter.New();
            outlineFilter.SetInput(MainWindow.VTKBasket.EnvironmentController.BoxSource.GetOutput());
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(outlineFilter.GetOutputPort());
            box.SetMapper(mapper);
            box.GetProperty().SetColor(0.4, 0.4, 0.4);
            box.SetPickable(0);
            boxActor.Prop = box;
        }

        /// <summary>
        /// accessor for the renderBox member variable
        /// </summary>
        public bool RenderBox
        {
            get { return renderBox; }
            set { renderBox = value; }
        }

        /// <summary>
        /// draw the environment box
        /// </summary>
        public void drawEnvBox()
        {
            // handle the box
            if (boxActor != null)
            {
                boxActor.addToScene(rw, renderBox);
            }
        }
    }

    /// <summary>
    /// encapsulates the VTK rendering pipeline for the ecs
    /// </summary>
    public class VTKECSController
    {
        private GraphicsProp gradientActor;
        private bool renderGradient;
        private vtkRenderWindow rw;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKECSController(vtkRenderWindow _rw)
        {
            rw = _rw;
            gradientActor = new GraphicsProp();
            renderGradient = false;
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the gradient actor
        /// </summary>
        public GraphicsProp GradientActor
        {
            get { return gradientActor; }
        }

        /// <summary>
        /// release allocated memory
        /// </summary>
        public void Cleanup()
        {
            gradientActor.cleanup(rw);
        }

        /// <summary>
        /// finish the pipelines for all molpop in the ecs
        /// </summary>
        public void finish3DPipelines()
        {
            Compartment ecs = Simulation.dataBasket.ECS.Space;
            // create a transfer function mapping scalar value to opacity
            vtkPiecewiseFunction fOpacity = vtkPiecewiseFunction.New();
            // set the opacity: assume it is one along the volume's diagonal
            double diag = Math.Sqrt(ecs.Interior.Extent(0) * ecs.Interior.Extent(0) + ecs.Interior.Extent(1) * ecs.Interior.Extent(1) + ecs.Interior.Extent(2) * ecs.Interior.Extent(2));

            fOpacity.AddPoint(0, 1.0 / diag);
            fOpacity.AddPoint(ecs.Interior.NodesPerSide(0) * ecs.Interior.NodesPerSide(1) * ecs.Interior.NodesPerSide(2), 1.0 / diag);

            vtkVolumeProperty volProp = vtkVolumeProperty.New();
            // create a transfer function mapping scalar value to color
            vtkColorTransferFunction fColor = vtkColorTransferFunction.New();
            fColor.AddRGBPoint(0.0, 0.0, 0.0, 0.0);
            fColor.AddRGBPoint(Byte.MaxValue, 1.0, 1.0, 1.0);

            volProp.SetIndependentComponents(0);
            volProp.SetColor(fColor);
            volProp.SetScalarOpacity(fOpacity);
            volProp.SetInterpolationTypeToLinear();//SetInterpolationTypeToNearest();

            // we have to call Render() before using IsRenderSupported
            rw.Render();

            // begin load openGL extensions; it seems this should not be necessary but we have seen problems on certain cards
            vtkOpenGLExtensionManager extMgr = new vtkOpenGLExtensionManager();

            extMgr.SetRenderWindow(rw);
            // need to call this in order to be able to extract the extensions string
            extMgr.Update();

            string es = extMgr.GetExtensionsString();
            string[] extensions = es.Split(' ');

            // iterate over all extensions and load the ones indicating an openGL version (non-deprecated)
            foreach (string e in extensions)
            {
                if (e.StartsWith("GL_VERSION") == true && e.EndsWith("DEPRECATED") == false)
                {
                    extMgr.LoadSupportedExtension(e);
                    // Note: to test successful load we'd have to do this
                    //if (extMgr.LoadSupportedExtension(e) > 0)
                    //{
                    //  success!
                    //}
                }
            }
            // end load openGL extensions

            vtkSmartVolumeMapper vmSmart = vtkSmartVolumeMapper.New();
            vmSmart.SetRequestedRenderModeToDefault();
            vmSmart.SetInput(MainWindow.VTKBasket.ECSController.ImageGrid);

            // the actual volume
            vtkVolume volume = vtkVolume.New();
            volume.SetMapper(vmSmart);
            volume.SetProperty(volProp);
            // set non-pickable
            volume.SetPickable(0);
            gradientActor.Prop = volume;
        }

        /// <summary>
        /// indexer for the renderGradient member variable
        /// </summary>
        public bool RenderGradient
        {
            get { return renderGradient; }
            set { renderGradient = value; }
        }

        /// <summary>
        /// draw the 3D ecs
        /// </summary>
        public void draw3D()
        {
            // handle the gradient
            if (gradientActor != null)
            {
                gradientActor.addToScene(rw, renderGradient);
            }
        }
    }
#if ALL_GRAPHICS
    /// <summary>
    /// entity encapsulating a cell track
    /// </summary>
    public class VTKCellTrack
    {
        private GraphicsProp actualTrack,
                             standardTrack,
                             zeroForceTrack;
        private vtkRenderWindow rw;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellTrack(vtkRenderWindow _rw)
        {
            rw = _rw;
            actualTrack = new GraphicsProp();
            standardTrack = new GraphicsProp();
            zeroForceTrack = new GraphicsProp();
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the actual track
        /// </summary>
        public GraphicsProp ActualTrack
        {
            get { return actualTrack; }
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the standard track
        /// </summary>
        public GraphicsProp StandardTrack
        {
            get { return standardTrack; }
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the zero force track
        /// </summary>
        public GraphicsProp ZeroForceTrack
        {
            get { return zeroForceTrack; }
        }

        /// <summary>
        /// release track objects (VTK)
        /// </summary>
        public void Cleanup()
        {
            actualTrack.cleanup(rw);
            standardTrack.cleanup(rw);
            zeroForceTrack.cleanup(rw);
        }

        /// <summary>
        /// draw a tube path for the selected cell
        /// </summary>
        /// <param name="cellID">integer id for the cell</param>
        /// <param name="zeroForce">true to indicate zero force fit</param>
        /// <returns>a pointer to the inserted vtk actor, null for error</returns>
        public void GenerateTubeFitPathProp(int cellID, bool zeroForce)
        {
            vtkPolyData poly;
            if (zeroForce)
                poly = MainWindow.VTKBasket.GetCellTrack(cellID).ZeroForceTrack;
            else
                poly = MainWindow.VTKBasket.GetCellTrack(cellID).StandardTrack;

            vtkTubeFilter tubeFilter = vtkTubeFilter.New();
            tubeFilter.SetInputConnection(poly.GetProducerPort());
            tubeFilter.SetRadius(0.5); //default is .5
            tubeFilter.SetNumberOfSides(20);
            tubeFilter.CappingOn();

            vtkLookupTable lut = vtkLookupTable.New();
            lut.SetValueRange(0.5, 1.0);
            lut.SetSaturationRange(1.0, 1.0);
            if (zeroForce)
            {
                lut.SetHueRange(0.10, 0.17);
            }
            else
            {
                lut.SetHueRange(0.33, 0.17);
            }
            lut.SetRampToLinear();
            // When using a vector component for coloring
            // lut.SetVectorModeToComponent();
            // lut.SetVectorComponent(1);
            // When using vector magnitude for coloring
            lut.SetVectorModeToMagnitude();
            lut.Build();

            vtkPolyDataMapper tubeMapper = vtkPolyDataMapper.New();
            tubeMapper.SetInputConnection(tubeFilter.GetOutputPort());
            tubeMapper.SetLookupTable(lut);
            tubeMapper.ScalarVisibilityOn();
            tubeMapper.SetScalarModeToUsePointFieldData();
            tubeMapper.SelectColorArray("predicted_velocity");
            // When using a vector component for coloring
            // tubeMapper.SetScalarRange(velocity.GetRange(1));
            // When using a vector component for coloring
            double[] range = new double[2];
            range = poly.GetPointData().GetArray("predicted_velocity").GetRange(-1);
            tubeMapper.SetScalarRange(range[0], range[1]);

            vtkActor tubeActor = vtkActor.New();
            tubeActor.SetMapper(tubeMapper);
            //tubeActor.GetProperty().SetColor(1.0, 1.0, 0.0);

            if (zeroForce)
            {
                this.zeroForceTrack.Prop = tubeActor;
            }
            else
            {
                this.standardTrack.Prop = tubeActor;
            }
        }

        public void GenerateActualPathProp(int cellID)
        {
            vtkPolyData poly_track = MainWindow.VTKBasket.GetCellTrack(cellID).ActualTrack;

            vtkTubeFilter tubeFilter = vtkTubeFilter.New();
            tubeFilter.SetInputConnection(poly_track.GetProducerPort());
            tubeFilter.SetRadius(0.25); //default is .5
            tubeFilter.SetNumberOfSides(10);
            tubeFilter.CappingOn();

            vtkSphereSource sph = vtkSphereSource.New();
            sph.SetThetaResolution(8);
            sph.SetPhiResolution(8);
            sph.SetRadius(0.5);

            vtkGlyph3D glyp = vtkGlyph3D.New();
            glyp.SetSourceConnection(sph.GetOutputPort(0));
            glyp.SetInput(poly_track);
            // Tell glyph which arrays to use for 'scalars' and 'vectors'
            glyp.SetInputArrayToProcess(0, 0, 0, 0, "time");		// scalars
            // glyp.SetInputArrayToProcess(1,0,0,0,'RTDataGradient');		// vectors

            // glyp.SetVectorModeToUseVector();
            // glyp.SetScaleFactor(0.075);
            glyp.SetColorModeToColorByScalar();
            glyp.ScalingOff();
            // glyp.SetScaleModeToScaleByVector();
            glyp.OrientOff();

            vtkAppendPolyData appendPoly = vtkAppendPolyData.New();
            appendPoly.AddInputConnection(tubeFilter.GetOutputPort(0));
            appendPoly.AddInputConnection(glyp.GetOutputPort(0));

            vtkLookupTable lut = vtkLookupTable.New();
            lut.SetValueRange(0.4, 1.0);
            lut.SetSaturationRange(0.0, 0.0);
            lut.SetHueRange(0.0, 0.0);  // doesn't matter if saturation = 0
            lut.SetRampToLinear();
            lut.Build();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(appendPoly.GetOutputPort());
            mapper.SetLookupTable(lut);
            mapper.ScalarVisibilityOn();
            mapper.SetScalarModeToUsePointFieldData();
            mapper.SelectColorArray("time");
            double[] range = new double[2];
            range = poly_track.GetPointData().GetArray("time").GetRange();
            mapper.SetScalarRange(range[0], range[1]);

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            // actor.GetProperty().SetRepresentationToWireframe();
            actor.GetProperty().SetRepresentationToSurface();
            //tubeActor.GetProperty().SetColor(1.0, 1.0, 0.0);

            this.actualTrack.Prop = actor;
        }
    }
#endif
    /// <summary>
    /// enum to control the cell render state
    /// </summary>
    public enum CellRenderMethod
    {
        /// <summary>
        /// render 3d spheres; least efficient
        /// </summary>
        CELL_RENDER_SPHERES,
        /// <summary>
        /// render polys (hexagons); more efficient
        /// </summary>
        CELL_RENDER_POLYS,
        /// <summary>
        /// render vertices; most efficient
        /// </summary>
        CELL_RENDER_VERTS
    };

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(CellRenderMethod), typeof(string))]
    public class CellRenderMethodToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _cell_render_method_strings = new List<string>()
                                {
                                    "Spheres",
                                    "Polygons",
                                    "Points"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _cell_render_method_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _cell_render_method_strings.FindIndex(item => item == str);
            return (CellRenderMethod)Enum.ToObject(typeof(CellRenderMethod), (int)idx);
        }
    }

    /// <summary>
    /// encapsulate the needed VTK for handling all cells
    /// </summary>
    public class VTKCellController
    {
        // data related to the cells
        private vtkPolyData glyphData;
        public vtkGlyph3D GlyphFilter { get; set; }
        private vtkPolyDataMapper cellGlyphMapper;
        private GraphicsProp cellActor;
        private vtkArrayCalculator receptorCalculator;
        // private vtkLookupTable cellIDsColorTable, cellAttributesColorTable;
        private vtkRenderWindow rw;
        /// <summary>
        /// accessor for the cell render method
        /// </summary>
        public CellRenderMethod render_method { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellController(vtkRenderWindow _rw)
        {
            rw = _rw;
            // cells
            cellActor = new GraphicsProp();
            // cellIDsColorTable = MainWindow.VTKBasket.CellController.CellIDsColorTable;
            // cellAttributesColorTable = MainWindow.VTKBasket.CellController.CellAttributesColorTable;
            receptorCalculator = vtkArrayCalculator.New();
            render_method = CellRenderMethod.CELL_RENDER_SPHERES;
        }

        /// <summary>
        /// retrieve the VTK mapper for the cell glyphs
        /// </summary>
        public vtkPolyDataMapper CellMapper
        {
            get { return cellGlyphMapper; }
        }

        /// <summary>
        /// retrieve the GraphicsProp encapsulating the cell actor
        /// </summary>
        public GraphicsProp CellActor
        {
            get { return cellActor; }
        }

        public vtkPolyData GlyphData
        {
            get { return glyphData; }
        }

        public vtkArrayCalculator ReceptorCalculator
        {
            get { return receptorCalculator; }
        }

        /// <summary>
        /// retrieve a point's grid index given it's internal vtk id
        /// </summary>
        /// <param name="id">vtk id</param>
        /// <returns>point's grid index</returns>
        public int GetPointIndex(int id)
        {
            return (int)glyphData.GetPointData().GetArray("InputPointIds").GetTuple1(id);
        }

        /// <summary>
        /// retrieve the cell index that's represented by a grid point given the internal vtk id
        /// </summary>
        /// <param name="id">vtk id</param>
        /// <returns>cell index</returns>
        public int GetCellIndex(int id)
        {
            return (int)glyphData.GetPointData().GetArray("cellID").GetTuple1(id);
        }

        /// <summary>
        /// retrieve the color index that's represented by a grid point given the internal vtk id
        /// </summary>
        /// <param name="id">vtk id</param>
        /// <returns>color index</returns>
        public int GetColorIndex(int id)
        {
            return (int)glyphData.GetPointData().GetArray("cellSet").GetTuple1(id);
        }

        /// <summary>
        /// retrieve the cell index that's represented by a grid point given the internal vtk id
        /// </summary>
        /// <param name="id">vtk id</param>
        /// <returns>cell index</returns>
        public int GetCellGeneration(int id)
        {
            return (int)glyphData.GetPointData().GetArray("generation").GetTuple1(id);
        }

        /// <summary>
        /// finish the cells and glyph them
        /// </summary>
        public void GlyphCells()
        {
            // Put a calculator filter in first to do relative receptor to 2D bivariate color map calculation
            // TODO: This should maybe go in VTKDataBasket so it would be consistent across views...
            //   ==> Problem is that the formula needs the colormap scaling factor, which is specific to a GraphicsController right now.
            //       This means that the colormap scaling factor had better be part of VTKDataBasket, too, and GC can just reveal it for
            //       binding to a specific view.
            //   ==> The scaling factor had better be variable/colormap-specific, then, somehow...
#if ALL_GRAPHICS
            if (MainWindow.VTKBasket.CompReceptors == true)
            {
                receptorCalculator.SetFunction("");
                receptorCalculator.RemoveAllVariables();
                receptorCalculator.SetInputConnection(MainWindow.VTKBasket.CellController.Poly.GetProducerPort());
                foreach (KeyValuePair<string, double> kvp in MainWindow.VTKBasket.CellReceptorMaxConcs)
                {
                    receptorCalculator.AddScalarArrayName(kvp.Key, 0);
                }
                this.UpdateReceptorCalcFormula(1.0);
                receptorCalculator.SetResultArrayName("receptorComp");
            }
#endif
            GlyphFilter = vtkGlyph3D.New();

            // Glyph source can be verts, polys or spheres
            this.SetGlyphSource();
#if ALL_GRAPHICS
            if (MainWindow.VTKBasket.CompReceptors == true)
            {
                GlyphFilter.SetInputConnection(receptorCalculator.GetOutputPort());
            }
            else
            {
                GlyphFilter.SetInputConnection(MainWindow.VTKBasket.CellController.Poly.GetProducerPort());
            }
#else
            GlyphFilter.SetInputConnection(MainWindow.VTKBasket.CellController.Poly.GetProducerPort());
#endif
            GlyphFilter.Update();   // so glyphData will be valid right away
            glyphData = GlyphFilter.GetOutput();

            GlyphFilter.ScalingOff();
            GlyphFilter.OrientOff();

            cellGlyphMapper = vtkPolyDataMapper.New();
            // immediate mode rendering may speed up rendering for large scenes, but can adversely affect smaller ones
            // in reality, there is only a minor effect but other side effects, such as the vtkPolyDataWriter for the cells crashing
            //cellGlyphMapper.ImmediateModeRenderingOn();
            cellGlyphMapper.SetInputConnection(GlyphFilter.GetOutputPort());
            //cellGlyphMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellSetColorTable);
            cellGlyphMapper.ScalarVisibilityOn();
            cellGlyphMapper.SetScalarModeToUsePointFieldData();
            //cellGlyphMapper.SelectColorArray("cellSet");
            //cellGlyphMapper.SetScalarRange(0, MainWindow.VTKBasket.CellController.CellSetColorTable.GetNumberOfTableValues() - 1);

            vtkActor actor = vtkActor.New();
            actor.SetMapper(cellGlyphMapper);
            //actor.GetProperty().SetRepresentationToWireframe();
            actor.GetProperty().SetRepresentationToSurface();
            // point size should only get used with vert representation
            actor.GetProperty().SetPointSize(8);
            cellActor.Prop = actor;
            cellActor.addToScene(rw, true);
        }

        public void SetGlyphSource()
        {
            vtkAlgorithm source;

            if (render_method == CellRenderMethod.CELL_RENDER_VERTS)
            {
                source = vtkGlyphSource2D.New();
                ((vtkGlyphSource2D)source).SetGlyphTypeToVertex();
            }
            else if (render_method == CellRenderMethod.CELL_RENDER_POLYS)
            {
                source = vtkRegularPolygonSource.New();
                ((vtkRegularPolygonSource)source).SetRadius(Cell.defaultRadius);
            }
            // default: spheres
            else
            {
                source = vtkSphereSource.New();
                ((vtkSphereSource)source).SetThetaResolution(16);
                ((vtkSphereSource)source).SetPhiResolution(16);
                ((vtkSphereSource)source).SetRadius(Cell.defaultRadius);
            }

            GlyphFilter.SetSourceConnection(source.GetOutputPort(0));
        }
#if ALL_GRAPHICS
        /// <summary>
        /// Update the formula string to be used in the calculator filter for receptor comparison.
        /// This shouldn't even be called if receptor comparison is disabled.
        /// </summary>
        /// <param name="scale_factor"></param>
        public void UpdateReceptorCalcFormula(double scale_factor)
        {
            string xx = MainWindow.VTKBasket.CompReceptor1 + "/" + MainWindow.VTKBasket.CellReceptorMaxConcs[MainWindow.VTKBasket.CompReceptor1].ToString();
            string yy = MainWindow.VTKBasket.CompReceptor2 + "/" + MainWindow.VTKBasket.CellReceptorMaxConcs[MainWindow.VTKBasket.CompReceptor2].ToString();
            string zz = scale_factor.ToString();
            string function_str = xx + " + " + zz + " * floor(4*(( " + yy + " ) / " + zz + " ))";
            receptorCalculator.SetFunction(function_str);
        }
#endif
        ///// <summary>
        ///// Set the attribute to color cells by.
        ///// Right now "colorIDs", "cell type" (equivalent to previous) and "generation" are the only valid possibilities,
        ///// and if one of those aren't passed, then it defaults to "colorIDs".
        ///// </summary>
        ///// <param name="attribute_name">attribute name string</param>
        //public void SetCellsColorAttribute(string attribute_name)
        //{
        //    if (cellGlyphMapper != null && MainWindow.VTKBasket.CellController.CellGenerationColorTable != null && MainWindow.VTKBasket.CellController.CellSetColorTable != null)
        //    {
        //        if (attribute_name.ToLower() == "generation")
        //        {
        //            cellGlyphMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellGenerationColorTable);
        //            cellGlyphMapper.ScalarVisibilityOn();
        //            cellGlyphMapper.SetScalarModeToUsePointFieldData();
        //            cellGlyphMapper.SelectColorArray("generation");
        //            cellGlyphMapper.SetScalarRange(0, 4);
        //        }
        //        else if (attribute_name.ToLower() == "cell type")
        //        {
        //            cellGlyphMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellSetColorTable);
        //            cellGlyphMapper.ScalarVisibilityOn();
        //            cellGlyphMapper.SetScalarModeToUsePointFieldData();
        //            cellGlyphMapper.SelectColorArray("cellSet");
        //            cellGlyphMapper.SetScalarRange(0, MainWindow.VTKBasket.CellController.CellSetColorTable.GetNumberOfTableValues() - 1);
        //        }
        //        else
        //        {
        //            cellGlyphMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellSetColorTable);
        //            cellGlyphMapper.ScalarVisibilityOn();
        //            cellGlyphMapper.SetScalarModeToUsePointFieldData();
        //            cellGlyphMapper.SelectColorArray("cellSet");
        //            cellGlyphMapper.SetScalarRange(0, MainWindow.VTKBasket.CellController.CellSetColorTable.GetNumberOfTableValues() - 1);
        //        }
        //    }
        //}
        
        /// <summary>
        /// set the cell opacities
        /// </summary>
        /// <param name="opacity">opacity value</param>
        public void SetCellOpacities(double opacity = 1.0)
        {
            if (cellActor.Prop != null)
            {
                ((vtkActor)cellActor.Prop).GetProperty().SetOpacity(opacity);
            }
        }

        /// <summary>
        /// cleanup data
        /// </summary>
        public void CleanupCells()
        {
            if (cellActor != null)
            {
                cellActor.cleanup(rw);
                if (glyphData != null)
                {
                    glyphData.Dispose();
                    glyphData = null;
                }
            }
        }

        /// <summary>
        /// cleanup the cell controller
        /// </summary>
        public void Cleanup()
        {
            CleanupCells();
        }
    }

    /// <summary>
    /// entity encapsulating the control of a simulation's 3D VTK graphics
    /// </summary>
    public class VTKGraphicsController : EntityModelBase
    {
        // the environment outline box controller
        private VTKEnvironmentController environmentController;
        // simulation progress string
        private GraphicsProp cornerAnnotation;
        // the cells
        private VTKCellController cellController;
        // the ecs
        private VTKECSController ecsController;
#if ALL_GRAPHICS
        // dictionary holding track data keyed by cell id
        private Dictionary<int, VTKCellTrack> cellTracks;
#endif
        // dictionary holding all of the region widgets for this VTK window
        private Dictionary<string, RegionWidget> regions;
        private RenderWindowControl rwc;
        private vtkRenderWindow rw;
        private WindowsFormsHost wfh;
#if ALL_GRAPHICS
        private static LPManager lpm;
#endif
        private vtkOrientationMarkerWidget axesTool;
        private vtkScalarBarWidget scalarBar;

        private static byte CURSOR_ARROW = 1,
                            CURSOR_HAND = 9;
        // variables for binding to toolbar buttons
        private bool whArrowToolButton_IsEnabled = false;
        private bool whArrowToolButton_IsChecked = true;
        private bool handToolButton_IsEnabled = false;
        private bool handToolButton_IsChecked = false;
        private bool previewButton_IsEnabled = false;
        private bool previewButton_IsChecked = true;
        private bool toolsToolbar_IsEnabled = true;
        private bool resetCameraButton_IsChecked = false;
        private bool orientationMarker_IsChecked = false;
        private bool scalarBarMarker_IsChecked = false;
        private System.Windows.Visibility colorScaleSlider_IsEnabled = System.Windows.Visibility.Visible;
        private double colorScaleMaxFactor = 1.0;

        // Variables for binding cell and molpop rendering options to toolbar combo boxes
        private CellRenderMethod cellRenderMethod;
        public ObservableCollection<string> CellAttributeArrayNames { get; set; }
        private string cellColorArrayName;
        public ObservableCollection<string> ECSRenderingMethodNames { get; set; }
        private string ecsRenderingMethod;
        private MainWindow MW;

        public static byte GET_CURSOR_ARROW
        {
            get
            {
                return CURSOR_ARROW;
            }
        }

        public static byte GET_CURSOR_HAND
        {
            get
            {
                return CURSOR_HAND;
            }
        }
       
        /// <summary>
        /// constructor
        /// </summary>
        public VTKGraphicsController(MainWindow mw)
        {
            // Trying to get a link to the main window so can activate toolwindow from a callback here...
            MW = mw;
            
            // create a VTK output control and make the forms host point to it
            rwc = new RenderWindowControl();
            wfh = new WindowsFormsHost();
            wfh.Child = rwc;
            rwc.CreateGraphics();

            // set up basic viewing
            rw = rwc.RenderWindow;
            vtkRenderer ren = rw.GetRenderers().GetFirstRenderer();

            // background color
            ren.SetBackground(0.0, 0.0, 0.0);

            // interactor style
            vtkInteractorStyleSwitch istyle = vtkInteractorStyleSwitch.New();
            rw.GetInteractor().SetInteractorStyle(istyle);
            rw.GetInteractor().SetPicker(vtkCellPicker.New());
            (istyle).SetCurrentStyleToTrackballCamera();

            // add events to the iren instead of Observers
            rw.GetInteractor().LeftButtonPressEvt += new vtkObject.vtkObjectEventHandler(leftMouseDown);

            //skg 7/10/12 - Experimenting how to display cell info - playing with right mouse down
            rw.GetInteractor().RightButtonPressEvt += new vtkObject.vtkObjectEventHandler(rightMouseDown);

            // progress
            cornerAnnotation = new GraphicsProp();
            vtkCornerAnnotation prop = vtkCornerAnnotation.New();
            prop.SetLinearFontScaleFactor(2);
            prop.SetNonlinearFontScaleFactor(1);
            prop.SetMaximumFontSize(14);
            prop.GetTextProperty().SetColor(1, 1, 1);
            cornerAnnotation.Prop = prop;
            cornerAnnotation.addToScene(rw, true);

            // create the axes tool but don't enable it yet
            vtkAxesActor axesActor = vtkAxesActor.New();
            axesActor.SetTotalLength(50, 50, 50);
            axesActor.SetNormalizedShaftLength(0.85, 0.85, 0.85);
            axesActor.SetNormalizedTipLength(0.15, 0.15, 0.15);
            axesActor.SetShaftTypeToCylinder();
            axesActor.SetCylinderRadius(0.05);
            axesActor.SetConeRadius(0.5);
            axesActor.AxisLabelsOn();
            axesActor.GetXAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();
            axesActor.GetYAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();
            axesActor.GetZAxisCaptionActor2D().GetCaptionTextProperty().ShadowOff();

            axesTool = new vtkOrientationMarkerWidget();
            axesTool.SetOrientationMarker(axesActor);
            axesTool.SetInteractor(rwc.RenderWindow.GetInteractor());
            axesTool.SetEnabled(0);

            this.scalarBar = vtkScalarBarWidget.New();
            this.scalarBar.SetInteractor(rwc.RenderWindow.GetInteractor());
            this.scalarBar.GetScalarBarActor().SetNumberOfLabels(3);
            this.scalarBar.SetEnabled(0);

            // environment box
            environmentController = new VTKEnvironmentController(rw);

            // cells
            cellController = new VTKCellController(rw);
            
            // chemokine
            ecsController = new VTKECSController(rw);
#if ALL_GRAPHICS
            // cell tracks
            cellTracks = new Dictionary<int, VTKCellTrack>();
#endif
            // This list will be regenerated on each CreatePipelines() call
            this.CellAttributeArrayNames = new ObservableCollection<string>();

            // Fixed set of molpop rendering options, so pre-generate this list
            ECSRenderingMethodNames = new ObservableCollection<string>();
            ECSRenderingMethodNames.Add("No Rendering");
            ECSRenderingMethodNames.Add("Outline");
            ECSRenderingMethodNames.Add("Volume");
            ECSRenderingMethodNames.Add("Outlined Volume");
            // regions
            regions = new Dictionary<string, RegionWidget>();
        }
        
        /// <summary>
        /// free allocated memory
        /// </summary>
        public void Cleanup()
        {
            foreach (KeyValuePair<string, RegionWidget> kvp in regions)
            {
                kvp.Value.ShowWidget(false);
                kvp.Value.ShowActor(Rwc.RenderWindow, false);
                kvp.Value.CleanUp();
            }
            regions.Clear();
            environmentController.Cleanup();
            cellController.Cleanup();
            ecsController.Cleanup();
#if ALL_GRAPHICS
            CleanupTracks();
#endif
            CellAttributeArrayNames.Clear();
            // ColorScaleMaxFactor = 1.0;
        }

        /// <summary>
        /// retrieve the list of regions
        /// </summary>
        public Dictionary<string, RegionWidget> Regions
        {
            get { return regions; }
        }

        public bool OrientationMarker_IsChecked
        {
            get { return orientationMarker_IsChecked; }
            set
            {
                if (value == orientationMarker_IsChecked)
                    return;
                else
                {
                    orientationMarker_IsChecked = value;
                    axesTool.SetEnabled(value ? 1 : 0);
                    rwc.Invalidate();
                    base.OnPropertyChanged("OrientationMarker_IsChecked");
                }
            }
        }

        public bool ResetCamera_IsChecked
        {
            get { return resetCameraButton_IsChecked; }
            set 
            { 
                // Using a cheat to make toggle button act like regular button
                this.recenterCamera();
                this.Rwc.Invalidate();
                base.OnPropertyChanged("ResetCamera_IsChecked"); 
            }
        }

        public bool ScalarBarMarker_IsChecked
        {
            get { return scalarBarMarker_IsChecked; }
            set
            {
                if (value == scalarBarMarker_IsChecked)
                    return;
                else
                {
                    scalarBarMarker_IsChecked = value;
                    this.scalarBar.SetEnabled(value ? 1 : 0);
                    this.Rwc.Invalidate();
                    base.OnPropertyChanged("ScalarBarMarker_IsChecked");
                }
            }
        }
        
        public string ECSRenderingMethod
        {
            get { return ecsRenderingMethod; }
            set
            {
                if (value == this.ecsRenderingMethod)
                {
                    return;
                }
                else
                {
                    this.ecsRenderingMethod = value;
                    // Set up ecs rendering based on value
                    if (value != null)
                    {

                        if (value == "Outlined Volume")
                        {
                            EnvironmentController.RenderBox = true;
                            ECSController.RenderGradient = true;
                        }
                        else if (value == "Outline")
                        {
                            EnvironmentController.RenderBox = true;
                            ECSController.RenderGradient = false;
                        }
                        else if (value == "Volume")
                        {
                            EnvironmentController.RenderBox = false;
                            ECSController.RenderGradient = true;
                        }
                        else
                        {
                            EnvironmentController.RenderBox = false;
                            ECSController.RenderGradient = false;
                        }
                        EnvironmentController.drawEnvBox();
                        ECSController.draw3D();
                        Rwc.Invalidate();
                    }
                    base.OnPropertyChanged("ECSRenderingMethod");
                }
            }
        }

        public string CellColorArrayName
        {
            get { return this.cellColorArrayName; }
            set
            {
                if (value == this.cellColorArrayName)
                    return;
                else
                {
                    if (value != null)
                    {
                        // NOTE: Not sure if this is a good idea, but not letting name get reset to null
                        //   when collection for combo box gets cleared (for continuity across Reset presses)
                        this.cellColorArrayName = value;
                        this.setColorMap();
                        // NOTE: If allow null reset of name, move this back outside the if() brackets
                        base.OnPropertyChanged("CellColorArrayName");
                    }
                }
            }
        }

        private void setColorMap()
        {
            this.CellController.CellMapper.SelectColorArray(this.cellColorArrayName);
            if (this.CellController.GlyphData != null)
            {
                double[] rr = new double[2];
                rr = this.CellController.GlyphData.GetPointData().GetArray(this.cellColorArrayName).GetRange();
                // TODO: Change this so that lookup tables are indexed by array name...
                //   Hard-coding names for now...
                if (this.cellColorArrayName == "cellID")
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellGenericColorTable);
                    this.CellController.CellMapper.SetScalarRange(rr[0], rr[1]);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Collapsed;
                }
                else if (this.cellColorArrayName == "cellSet")
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellSetColorTable);
                    this.CellController.CellMapper.SetScalarRange(0, MainWindow.VTKBasket.CellController.CellSetColorTable.GetNumberOfTableValues() - 1);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Collapsed;
                }
                else if (this.cellColorArrayName == "generation")
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellGenerationColorTable);
                    this.CellController.CellMapper.SetScalarRange(0, 4);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Collapsed;
                }
#if ALL_GRAPHICS
                else if (this.cellColorArrayName == "receptorComp")
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.BivariateColorTable);
                    this.CellController.UpdateReceptorCalcFormula(this.colorScaleMaxFactor);
                    this.CellController.CellMapper.SetScalarRange(0, 4 * this.colorScaleMaxFactor);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Visible;
                }
                else if (MainWindow.VTKBasket.CellReceptorMaxConcs.ContainsKey(this.cellColorArrayName))
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellGenericColorTable);

                    // Scale color map range by max value of receptor concentration rather than current values
                    this.CellController.CellMapper.SetScalarRange(0f, this.ColorScaleMaxFactor * MainWindow.VTKBasket.CellReceptorMaxConcs[this.cellColorArrayName]);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Visible;
                }
#endif
                else
                {
                    this.CellController.CellMapper.SetLookupTable(MainWindow.VTKBasket.CellController.CellGenericColorTable);
                    this.CellController.CellMapper.SetScalarRange(rr[0], rr[1]);
                    this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Visible;
                }
                this.scalarBar.GetScalarBarActor().SetLookupTable(cellController.CellMapper.GetLookupTable());
                this.scalarBar.GetScalarBarActor().SetTitle(this.CellColorArrayName);
                Rwc.Invalidate();
            }
        }

        public CellRenderMethod CellRenderMethod
        {
            get { return cellRenderMethod; }
            set
            {
                if (value == cellRenderMethod)
                {
                    return;
                }
                else
                {
                    cellRenderMethod = value;
                    CellController.render_method = value;

                    CellController.SetGlyphSource();

                    // NOTE: If allow null reset of name, move this back outside the if() brackets
                    base.OnPropertyChanged("CellRenderMethod");
                    Rwc.Invalidate();
                }
            }
        }

        // Public accessors for toolbar button binding w/property changed notification
        public bool WhArrowToolButton_IsEnabled
        {
            get { return whArrowToolButton_IsEnabled; }
            set
            {
                if (whArrowToolButton_IsEnabled == value)
                    return;
                else
                {
                    whArrowToolButton_IsEnabled = value;
                    base.OnPropertyChanged("WhArrowToolButton_IsEnabled");
                }
            }
        }

        public bool WhArrowToolButton_IsChecked
        {
            get { return whArrowToolButton_IsChecked; }
            set
            {
                if (whArrowToolButton_IsChecked == value)
                    return;
                else
                {
                    whArrowToolButton_IsChecked = value;
                    HandToolButton_IsChecked = !value;
                    PreviewButton_IsEnabled = !value;
                    MainWindow.SetControlFlag(MainWindow.CONTROL_PICKING_ENABLED, !value);
                    CellController.SetCellOpacities(value ? MainWindow.cellOpacity : 1.0);
                    Rwc.RenderWindow.SetCurrentCursor(value ? CURSOR_ARROW : CURSOR_HAND);
                    Rwc.Invalidate();
                    base.OnPropertyChanged("WhArrowToolButton_IsChecked");
                }
            }
        }

        public bool HandToolButton_IsEnabled
        {
            get { return handToolButton_IsEnabled; }
            set
            {
                if (handToolButton_IsEnabled == value)
                    return;
                else
                {
                    handToolButton_IsEnabled = value;
                    base.OnPropertyChanged("HandToolButton_IsEnabled");
                }
            }
        }

        public bool HandToolButton_IsChecked
        {
            get { return handToolButton_IsChecked; }
            set
            {
                if (handToolButton_IsChecked == value)
                    return;
                else
                {
                    handToolButton_IsChecked = value;
                    WhArrowToolButton_IsChecked = !value;
                    PreviewButton_IsEnabled = value;
                    MainWindow.SetControlFlag(MainWindow.CONTROL_PICKING_ENABLED, value);
                    CellController.SetCellOpacities(!value ? MainWindow.cellOpacity : 1.0);
                    Rwc.RenderWindow.SetCurrentCursor(!value ? CURSOR_ARROW : CURSOR_HAND);
                    Rwc.Invalidate();
                    base.OnPropertyChanged("HandToolButton_IsChecked");
                }
            }
        }

        public bool PreviewButton_IsEnabled
        {
            get { return previewButton_IsEnabled; }
            set
            {
                if (previewButton_IsEnabled == value)
                    return;
                else
                {
                    previewButton_IsEnabled = value;
                    base.OnPropertyChanged("PreviewButton_IsEnabled");
                }
            }
        }

        public bool PreviewButton_IsChecked
        {
            get { return previewButton_IsChecked; }
            set
            {
                if (previewButton_IsChecked == value)
                    return;
                else
                {
                    previewButton_IsChecked = value;
                    base.OnPropertyChanged("PreviewButton_IsChecked");
                    // show the fitting tool panel if we are not in preview
                    // this is a workaround, at least for now, to address the fit -> Activate() -> camera rotation problem
                    // if the user then closes the fitting panel and continues fitting, then the panel will not automatically open
                    if (previewButton_IsChecked == false)
                    {
                        MW.LPFittingToolWindow.Activate();
                    }
                }
            }
        }

        public bool ToolsToolbar_IsEnabled
        {
            get { return toolsToolbar_IsEnabled; }
            set
            {
                if (toolsToolbar_IsEnabled == value)
                    return;
                else
                {
                    toolsToolbar_IsEnabled = value;
                    base.OnPropertyChanged("ToolsToolbar_IsEnabled");
                }
            }
        }

        public System.Windows.Visibility ColorScaleSlider_IsEnabled
        {
            get { return colorScaleSlider_IsEnabled; }
            set
            {
                if (colorScaleSlider_IsEnabled == value)
                    return;
                else
                {
                    colorScaleSlider_IsEnabled = value;
                    base.OnPropertyChanged("ColorScaleSlider_IsEnabled");
                }
            }
        }

        public double ColorScaleMaxFactor
        {
            get { return colorScaleMaxFactor; }
            set
            {
                if (colorScaleMaxFactor == value)
                    return;
                else
                {
                    colorScaleMaxFactor = value;
                    // NOTE: This is a bit overkill for just changing scaling factor, but it's the easiest for now
                    //   since there are many rules for coloring different scalars...
                    this.setColorMap();
                    base.OnPropertyChanged("ColorScaleMaxFactor");
                }
            }
        }

        public void EnablePickingButtons()
        {
            WhArrowToolButton_IsEnabled = true;
            WhArrowToolButton_IsChecked = true;
            HandToolButton_IsEnabled = true;
            HandToolButton_IsChecked = false;
            PreviewButton_IsEnabled = true;
            PreviewButton_IsChecked = true;
            Rwc.RenderWindow.SetCurrentCursor(CURSOR_ARROW);
        }

        public void DisablePickingButtons()
        {
            WhArrowToolButton_IsEnabled = false;
            WhArrowToolButton_IsChecked = true;
            HandToolButton_IsEnabled = false;            
            HandToolButton_IsChecked = false;                        
            PreviewButton_IsEnabled = false;
            PreviewButton_IsChecked = true;
            Rwc.RenderWindow.SetCurrentCursor(CURSOR_ARROW);
        }

        public RenderWindowControl Rwc
        {
            get { return rwc; }
        }
        
        public WindowsFormsHost Wfh
        {
            get { return wfh; }
        }

        private void WigetTransformToBoxMatrix(RegionWidget rw, BoxSpecification bs)
        {
            vtkMatrix4x4 mat = rw.GetTransform(RegionControl.PARAM_SCALE).GetMatrix();
            double[][] array2d = new double[4][];

            for (int row = 0; row < 4; row++)
            {
                array2d[row] = new double[4];
                for (int col = 0; col < 4; col++)
                {
                    array2d[row][col] = mat.GetElement(row, col);
                }
            }
            bs.SetMatrix(array2d);
        }

        /// <summary>
        /// widget interaction callback to catch when a widget changes and respond to it
        /// </summary>
        /// <param name="rw">pointer to the region widget that got changed</param>
        /// <param name="transferMatrix">true if transferring the widget matrix to the gui is desired</param>
        public void WidgetInteractionToGUICallback(RegionWidget rw, bool transferMatrix)
        {
            // identify the widget's key
            string key = "";

            // NOTE: This callback specific to this GC (referencing Regions). 
            //   Not sure what will happen with mutiple GCs...
            if (rw != null && Regions.ContainsValue(rw) == true)
            {
                foreach (KeyValuePair<string, RegionWidget> kvp in Regions)
                {
                    if (kvp.Value == rw)
                    {
                        key = kvp.Key;
                        break;
                    }
                }

                // found?
                if (key != "")
                {
                    if (transferMatrix == true)
                    {
                        double[] r = null, t = null, s = null;
                        bool changed = false;

                        // get the scale, rotation, translation, check against their min/max values, and correct if needed
                        rw.GetScaleRotationTranslation(ref s, ref r, ref t);
                        // when the region gets turned inside out the scales all become negative
                        if (s[0] < 0 && s[1] < 0 && s[2] < 0)
                        {
                            // restore the box matrix; the latter is known to be good
                            rw.SetTransform(MainWindow.SC.SimConfig.box_guid_box_dict[key].transform_matrix, RegionControl.PARAM_SCALE);
                            // transfer transform to VTKDataBasket
                            MainWindow.VTKBasket.Regions[key].SetTransform(rw.GetTransform(), 0);
                            return;
                        }

                        // translation
                        if (t[0] < MainWindow.SC.SimConfig.box_guid_box_dict[key].x_trans_min)
                        {
                            t[0] = MainWindow.SC.SimConfig.box_guid_box_dict[key].x_trans_min;
                            changed = true;
                        }
                        if (t[0] > MainWindow.SC.SimConfig.box_guid_box_dict[key].x_trans_max)
                        {
                            t[0] = MainWindow.SC.SimConfig.box_guid_box_dict[key].x_trans_max;
                            changed = true;
                        }
                        if (t[1] < MainWindow.SC.SimConfig.box_guid_box_dict[key].y_trans_min)
                        {
                            t[1] = MainWindow.SC.SimConfig.box_guid_box_dict[key].y_trans_min;
                            changed = true;
                        }
                        if (t[1] > MainWindow.SC.SimConfig.box_guid_box_dict[key].y_trans_max)
                        {
                            t[1] = MainWindow.SC.SimConfig.box_guid_box_dict[key].y_trans_max;
                            changed = true;
                        }
                        if (t[2] < MainWindow.SC.SimConfig.box_guid_box_dict[key].z_trans_min)
                        {
                            t[2] = MainWindow.SC.SimConfig.box_guid_box_dict[key].z_trans_min;
                            changed = true;
                        }
                        if (t[2] > MainWindow.SC.SimConfig.box_guid_box_dict[key].z_trans_max)
                        {
                            t[2] = MainWindow.SC.SimConfig.box_guid_box_dict[key].z_trans_max;
                            changed = true;
                        }
                        // scale
                        if (s[0] < RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].x_scale_min)
                        {
                            s[0] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].x_scale_min;
                            changed = true;
                        }
                        if (s[0] > RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].x_scale_max)
                        {
                            s[0] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].x_scale_max;
                            changed = true;
                        }
                        if (s[1] < RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].y_scale_min)
                        {
                            s[1] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].y_scale_min;
                            changed = true;
                        }
                        if (s[1] > RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].y_scale_max)
                        {
                            s[1] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].y_scale_max;
                            changed = true;
                        }
                        if (s[2] < RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].z_scale_min)
                        {
                            s[2] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].z_scale_min;
                            changed = true;
                        }
                        if (s[2] > RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].z_scale_max)
                        {
                            s[2] = RegionControl.SCALE_CORRECTION * MainWindow.SC.SimConfig.box_guid_box_dict[key].z_scale_max;
                            changed = true;
                        }

                        // apply if needed
                        if(changed == true)
                        {
                            rw.SetScaleRotationTranslation(s, r, t, 0);
                        }
                        WigetTransformToBoxMatrix(rw, MainWindow.SC.SimConfig.box_guid_box_dict[key]);
                        // Transfer transform to VTKDataBasket
                        MainWindow.VTKBasket.Regions[key].SetTransform(rw.GetTransform(), 0);
                    }
                }
            }
        }

        /// <summary>
        /// handler for left mouse button down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void leftMouseDown(vtkObject sender, vtkObjectEventArgs e)
        {
            // picking
            if (MainWindow.CheckControlFlag(MainWindow.CONTROL_PICKING_ENABLED) == false)
            {
                //Console.WriteLine("exit picking disabled");
                return;
            }

            vtkRenderWindowInteractor interactor = rwc.RenderWindow.GetInteractor();
            int[] x = interactor.GetEventPosition();
            int p = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).Pick(x[0], x[1], 0, rwc.RenderWindow.GetRenderers().GetFirstRenderer());
#if ALL_GRAPHICS
            if (p > 0)
            {
                p = (int)((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetPointId();
                if (p >= 0)
                {
                    int cellID = CellController.GetCellIndex(p);
                    // only allow one activity that is related to fitting or changing/accessing the selected cell at a time
                    lock (MainWindow.cellFitLock)
                    {
                        // NOTE: we may have other cells in the future that require path fitting besides motile cells
                        if (MainWindow.Basket.Cells[cellID].isMotileBaseType() == true)
                        {
                            MainWindow.selectedCell = (MotileCell)MainWindow.Basket.Cells[cellID];
                        }
                        else
                        {
                            MainWindow.selectedCell = null;
                        }

                        if (MainWindow.selectedCell != null)
                        {
                            // do the tube track fits only when not in fast preview mode
                            if ((MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE) == false && GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.Prop == null ||
                                 MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE) == true && GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.Prop == null) && PreviewButton_IsChecked == false ||
                                PreviewButton_IsChecked == true && GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.Prop == null)
                            {
                                if (lpm == null)
                                {
                                    //Console.WriteLine("Created new LPM");
                                    lpm = new LPManager();
                                }

                                // check if file exists and whether it is a new file
                                if (MainWindow.CheckControlFlag(MainWindow.CONTROL_NEW_RUN) == true)
                                {
                                    MainWindow.SetControlFlag(MainWindow.CONTROL_NEW_RUN, false);
                                    MainWindow.Basket.ConnectToExperiment(MainWindow.SC.SimConfig.experiment_db_id);
                                }

                                // allow for the quick preview
                                if (PreviewButton_IsChecked == true)
                                {
                                    // need to initialize the data arrays, then do the fast preview
                                    // TODO: Can remove LoadTrackData since GetCellTrack should do this...
                                    if (MainWindow.Basket.LoadTrackData(MainWindow.selectedCell.CellIndex) == true)
                                    {
                                        GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.addToScene(rw, true);
                                        zoomToPath(GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.Prop);
                                        rwc.Invalidate();
                                    }
                                }
                                else
                                {
                                    // Note: having this here is a good idea, but it causes a relative mouse coordinate change,
                                    // which in turn initiates a camera rotation
                                    //MW.LPFittingToolWindow.Activate();
                                    
                                    Thread fitThread = new Thread(new ThreadStart(fit)) { IsBackground = true };
                                    fitThread.Start();
                                }
                            }
                            // toggle visibility
                            else
                            {
                                // switch the tubes on only if the mode is not fast preview
                                if (GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.Prop != null && (GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.InScene == true || PreviewButton_IsChecked == false))
                                {
                                    GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.addToScene(rw, MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE) == false && !GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.InScene);
                                }
                                if (GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.Prop != null && (GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.InScene == true || PreviewButton_IsChecked == false))
                                {
                                    GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.addToScene(rw, MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE) == true && !GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.InScene);
                                }

                                // switch off the actual path only if the mode is fast preview or the other two are also off
                                if (GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.InScene == false || (PreviewButton_IsChecked == true ||
                                    GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.InScene == false && GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.InScene == false))
                                {
                                    GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.addToScene(rw, !GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.InScene);
                                }
                                MainWindow.SetControlFlag(MainWindow.CONTROL_UPDATE_GUI, true);
                                rwc.Invalidate();
                            }
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// the main logic for the fit thread
        /// NOTE: This used to be static when in MainWindow... Does it need to be for the threading???
        /// </summary>
        private void fit()
        {
#if ALL_GRAPHICS
            string optStr,
                   paramStr;

            // only allow one activity that is related to fitting or changing/accessing the selected cell at a time
            lock (MainWindow.cellFitLock)
            {
                if (MainWindow.selectedCell != null)
                {
                    MainWindow.fitStatus = MainWindow.PROGRESS_INIT;
                    //Console.WriteLine("Performing fit!");
                    if (lpm.fit(MainWindow.selectedCell.CellIndex, MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE), out optStr, out paramStr) == true)
                    {
                        if (MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE) == true)
                        {
                            // force to show
                            GetCellTrack(MainWindow.selectedCell.CellIndex).ZeroForceTrack.addToScene(rw, true);
                            if (GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.Prop != null && !GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.InScene)
                            {
                                GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.addToScene(rw, true);
                            }
                        }
                        else
                        {
                            // force to show
                            GetCellTrack(MainWindow.selectedCell.CellIndex).StandardTrack.addToScene(rw, true);
                            if (GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.Prop != null && !GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.InScene)
                            {
                                GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.addToScene(rw, true);
                            }
                        }
                        GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.addToScene(rw, true);

                        // user option: update the 3d render window to zoom in on newly fit track
                        zoomToPath(GetCellTrack(MainWindow.selectedCell.CellIndex).ActualTrack.Prop);
                        rwc.Invalidate();
                    }
                    MainWindow.fitStatus = MainWindow.PROGRESS_COMPLETE;
                    MainWindow.SetControlFlag(MainWindow.CONTROL_UPDATE_GUI, true);
                }
            }
#endif
        }

        public void zoomToPath(vtkProp p)
        {
            if (p != null && Properties.Settings.Default.autoZoomFit == true && PreviewButton_IsChecked == false)
            {
                double[] bounds = new double[6];

                bounds = p.GetBounds();
                // increase z by cell radius
                bounds[4] -= Cell.defaultRadius;
                bounds[5] += Cell.defaultRadius;
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().ResetCamera(bounds[0], bounds[1], bounds[2], bounds[3], bounds[4], bounds[5]);
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().ResetCameraClippingRange(bounds[0], bounds[1], bounds[2], bounds[3], bounds[4], bounds[5]);
            }
        }

        /// <summary>
        /// recenter the camera and adjust the zoom to display the whole ecs
        /// </summary>
        public void recenterCamera()
        {
            if (EnvironmentController.BoxActor.Prop != null)
            {
                // center the camera around the ecs and adjust it to make sure it fits into the fov
                double[] bounds = new double[6];

                bounds = EnvironmentController.BoxActor.Prop.GetBounds();
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().ResetCamera(bounds[0],
                                                                               bounds[1],
                                                                               bounds[2],
                                                                               bounds[3],
                                                                               bounds[4],
                                                                               bounds[5]);
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().ResetCameraClippingRange(bounds[0],
                                                                                            bounds[1],
                                                                                            bounds[2],
                                                                                            bounds[3],
                                                                                            bounds[4],
                                                                                            bounds[5]);
            }
        }
#if ALL_GRAPHICS
        /// <summary>
        /// retrieve the cell tracks
        /// </summary>
        public Dictionary<int, VTKCellTrack> CellTracks
        {
            get { return cellTracks; }
        }

        /// <summary>
        /// cleanup the cell tracks
        /// </summary>
        public void CleanupTracks()
        {
            // the tracks
            foreach (KeyValuePair<int, VTKCellTrack> kvp in cellTracks)
            {
                kvp.Value.Cleanup();
            }
            cellTracks.Clear();
        }

        /// <summary>
        /// toggle drawing of fitted cell tracks on/off
        /// </summary>
        /// <param name="zeroForce">specifies zero force/standard fit</param>
        public void ToggleCellFitTracks(bool zeroForce)
        {
            foreach (KeyValuePair<int, VTKCellTrack> kvp in cellTracks)
            {
                kvp.Value.StandardTrack.addToScene(rw, !zeroForce && kvp.Value.ActualTrack.InScene);
                kvp.Value.ZeroForceTrack.addToScene(rw, zeroForce && kvp.Value.ActualTrack.InScene);
            }
        }
#endif
        /// <summary>
        /// Create the VTK graphics pipelines for all cells and ecs, but use pre-allocated arrays for speed
        /// This will clear all old pipelines and generate new ones based on VTKDataBasket contents
        /// </summary>
        public void CreatePipelines()
        {
            Cleanup();

            // Regions
            CreateRegionWidgets();
            // Cells
            if (Simulation.dataBasket.Cells != null && MainWindow.VTKBasket.CellController.Poly != null)
            {
                // Finish VTK pipeline by glyphing cells
                cellController.GlyphCells();
                // Set up the array of cell attributes for "color by" combo box
                // Base this on Glyphed data so will catch attributes added to the pipeline in GraphicsController
                for (int c = 0; c < this.CellController.GlyphData.GetPointData().GetNumberOfArrays(); c++)
                {
                    // Specifically skip "Normals" array
                    var array_name = this.CellController.GlyphData.GetPointData().GetArrayName(c);
                    if (array_name != "Normals")
                    {
                        this.CellAttributeArrayNames.Add(array_name);
                    }
                }
                
                if (this.CellColorArrayName == null && this.CellAttributeArrayNames.Contains("cellSet"))
                {
                    // Make "cellSet" a hard-coded first-pass default for now when it's available
                    this.CellColorArrayName = "cellSet";
                }
                else if (this.CellAttributeArrayNames.Contains(this.CellColorArrayName))
                {
                    // Pull a background switcheroo to force color map to be applied and property change notice to be fired
                    // to keep continuity of old colormap choice across Resets
                    var tmp = this.CellColorArrayName;
                    this.cellColorArrayName = "";
                    this.CellColorArrayName = tmp;
                }
                else if (!this.CellAttributeArrayNames.Contains(this.CellColorArrayName) && this.CellAttributeArrayNames.Contains("cellSet"))
                {
                    // If CellColorArrayName isn't null, but the existing name isn't in the current list (after reset) then default to cellSet if can
                    this.CellColorArrayName = "cellSet";
                }
                else
                {
                    this.CellColorArrayName = this.CellAttributeArrayNames[0];
                }

                this.scalarBar.GetScalarBarActor().SetLookupTable(cellController.CellMapper.GetLookupTable());
                this.scalarBar.GetScalarBarActor().SetTitle(this.CellColorArrayName);
            }

            // Set up cell rendering now that glyphing is done
            if (this.cellRenderMethod != CellRenderMethod.CELL_RENDER_SPHERES)
            {
                var tmp = this.cellRenderMethod;
                this.cellRenderMethod = CellRenderMethod.CELL_RENDER_SPHERES;
                this.CellRenderMethod = tmp;
            }

            // environment
            EnvironmentController.setupPipeline();

            // ecs
            if (Simulation.dataBasket.ECS != null && MainWindow.VTKBasket.ECSController.ImageGrid != null)
            {
                ecsController.finish3DPipelines();
                // Make "Outline" a hard-coded first-pass default for now, otherwise keep old value
                if (ECSRenderingMethod == null)
                {
                    ECSRenderingMethod = "Outline";
                }
            }
        }

        public void AddGaussSpecRegionWidget(GaussianSpecification gs)
        {
            string box_guid = gs.gaussian_spec_box_guid_ref;
            // Find the box spec that goes with this gaussian spec
            BoxSpecification bs = MainWindow.SC.SimConfig.box_guid_box_dict[box_guid];

            RegionWidget rw = new RegionWidget(Rwc.RenderWindow, RegionShape.Ellipsoid);

            // color
            rw.SetColor(gs.gaussian_spec_color.ScR,
                        gs.gaussian_spec_color.ScG,
                        gs.gaussian_spec_color.ScB);
            // alpha channel/opacity
            rw.SetOpacity(gs.gaussian_spec_color.ScA);
            // box transform
            rw.SetTransform(bs.transform_matrix, RegionControl.PARAM_SCALE);
            // box visibility
            rw.ShowWidget(bs.box_visibility);
            // contained shape visibility
            rw.ShowActor(Rwc.RenderWindow, gs.gaussian_region_visibility);
            // NOTE: Callback being added afterwards in MainWindow for now...

            Regions.Add(box_guid, rw);
        }
#if CELL_REGIONS
        public void AddRegionRegionWidget(Region rr)
        {
            string box_guid = rr.region_box_spec_guid_ref;
            // Find the box spec that goes with this region
            BoxSpecification bs = MainWindow.SC.SimConfig.box_guid_box_dict[box_guid];

            RegionWidget rw = new RegionWidget(Rwc.RenderWindow, rr.region_type);

            // color
            rw.SetColor(rr.region_color.ScR,
                        rr.region_color.ScG,
                        rr.region_color.ScB);
            // alpha channel/opacity
            rw.SetOpacity(rr.region_color.ScA);
            // box transform
            rw.SetTransform(bs.transform_matrix, RegionControl.PARAM_SCALE);
            // box visibility
            rw.ShowWidget(bs.box_visibility);
            // contained shape visibility
            rw.ShowActor(Rwc.RenderWindow, rr.region_visibility);
            // NOTE: Callback being added afterwards in MainWindow for now...

            Regions.Add(box_guid, rw);
        }
#endif
        public void RemoveRegionWidget(string current_guid)
        {
            Regions[current_guid].ShowWidget(false);
            Regions[current_guid].ShowActor(Rwc.RenderWindow, false);
            Regions[current_guid].CleanUp();
            Regions.Remove(current_guid);
        }

        public void CreateRegionWidgets()
        {
            // Gaussian specs
            foreach (GaussianSpecification gs in MainWindow.SC.SimConfig.entity_repository.gaussian_specifications)
            {
                AddGaussSpecRegionWidget(gs);
            }

#if CELL_REGIONS
            // Regions
            foreach (Region rr in MainWindow.SC.SimConfig.scenario.regions)
            {
                AddRegionRegionWidget(rr);
            }
#endif
        }

        /// <summary>
        /// retrieve the cellController object
        /// </summary>
        public VTKCellController CellController
        {
            get { return cellController; }
        }

        /// <summary>
        /// retrieve the environmentController object
        /// </summary>
        public VTKEnvironmentController EnvironmentController
        {
            get { return environmentController; }
        }

        /// <summary>
        /// retrieve the ecsController object
        /// </summary>
        public VTKECSController ECSController
        {
            get { return ecsController; }
        }
#if ALL_GRAPHICS

        /// <summary>
        /// Access a cell track by key; if it doesn't exist create it
        /// This routine will check the VTK Track data in VTKDataBasket
        /// (which internally tries to load data from the database)
        /// and generate the final VTK GraphicsProp objects to display
        /// in the VTK render window.
        /// </summary>
        /// <param name="key">the cell id is the key</param>
        /// <returns></returns>
        public VTKCellTrack GetCellTrack(int key)
        {
            if (CellTracks.ContainsKey(key) == false)
            {
                CellTracks.Add(key, new VTKCellTrack(rw));
            }
            // VTKDataBasket will make sure we're connected to an experiment
            VTKCellTrackData data = MainWindow.VTKBasket.GetCellTrack(key);
            // Generate track polydata and actors for all available track data
            // but don't regenerate if already done
            if (data.ActualTrack.GetNumberOfPoints() > 0 && CellTracks[key].ActualTrack.Prop == null)
            {
                CellTracks[key].GenerateActualPathProp(key);
            }
            if (data.StandardTrack.GetNumberOfPoints() > 0 && CellTracks[key].StandardTrack.Prop == null)
            {
                CellTracks[key].GenerateTubeFitPathProp(key, false);
            }
            if (data.ZeroForceTrack.GetNumberOfPoints() > 0 && CellTracks[key].ZeroForceTrack.Prop == null)
            {
                CellTracks[key].GenerateTubeFitPathProp(key, true);
            }
            return CellTracks[key];
        }
#endif
        /// <summary>
        /// render a simulation frame
        /// </summary>
        /// <param name="progress">progress in percent</param>
        public void DrawFrame(int progress)
        {
            // environment
            if (environmentController.BoxActor != null)
            {
                environmentController.drawEnvBox();
            }

            // ecs
            if (ecsController.GradientActor != null)
            {
                ecsController.draw3D();
            }

            // progress string bottom left
            if (cornerAnnotation != null && cornerAnnotation.Prop != null)
            {
                if (MainWindow.SC.SimConfig.experiment_reps > 1)
                {
                    int rep = MainWindow.Repetition;
                    int reps = MainWindow.SC.SimConfig.experiment_reps;
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Rep: " + rep + "/" + reps + " Progress: " + progress + "%");
                }
                else
                {
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Progress: " + progress + "%");
                }
            }
            rwc.Invalidate();
        }

        // utility to wait for redraw completion
        public void WaitForRedraw(int ms)
        {
            while (rwc != null && rwc.RenderWindow != null && rwc.RenderWindow.CheckInRenderStatus() != 0)
            {
                Thread.Sleep(ms);
            }
        }

        /// <summary>
        /// Function to save the current 3D image to a file - skg 8/2/12
        /// </summary>
        /// <param name="filename" - File name
        /// <param name="imageWriter" - Object of one of these types depending on what user selected: 
        ///         vtkJPEGWriter, vtkBMPWriter, vtkPNGWriter, vtkTIFFWriter 
        public void SaveToFile(string filename, vtkImageWriter imageWriter)
        {
            //Use "rw" which is the current rendering window variable
            RenderWindowControl myRWC = new RenderWindowControl();
            myRWC = rwc;
            vtkRenderWindow myRW = rwc.RenderWindow;
            vtkRenderer ren = myRW.GetRenderers().GetFirstRenderer();
            // background color
            ren.SetBackground(1.0, 1.0, 1.0);

            vtkWindowToImageFilter w2if = new vtkWindowToImageFilter();
            w2if.SetInput(myRW);

            //Create Image output file            
            imageWriter.SetInput(w2if.GetOutput());
            imageWriter.SetFileName(filename);
            imageWriter.Write();

            ren.SetBackground(0.0, 0.0, 0.0);
        }

        //This code will display cell info in a new tab at the bottom.
        //This will work if user clicks on hand icon and then right clicks on a cell.
        //skg 7/10/12
        public void rightMouseDown(vtkObject sender, vtkObjectEventArgs e)
        {
            // If cell picking is enabled, only then should we proceed, otherwise return
            if (MainWindow.CheckControlFlag(MainWindow.CONTROL_PICKING_ENABLED) == false)
            {
                //Console.WriteLine("exit picking disabled");
                //return;  //skg temporarily removed this - allows us to view concs when paused.

                if (MainWindow.CheckControlFlag(MainWindow.CONTROL_MOLCONCS_ENABLED) == false)
                {
                    return;
                }                
            }                       

            //This section is fields right click on a specific cell
            vtkRenderWindowInteractor interactor = rwc.RenderWindow.GetInteractor();
            int[] x = interactor.GetEventPosition();
            int p = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).Pick(x[0], x[1], 0, rwc.RenderWindow.GetRenderers().GetFirstRenderer());

            if (p > 0)
            {
                p = (int)((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetPointId();
                if (p >= 0)
                {
                    int cellID = CellController.GetCellIndex(p);
#if ALL_GRAPHICS
                    MW.DisplayCellInfo(cellID);
#endif
                    return;                    
                }
            }
        }

    }
}
