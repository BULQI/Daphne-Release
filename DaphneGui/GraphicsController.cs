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
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms.Integration;

//using MathNet.Numerics.Distributions;
//using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;
using Kitware.VTK;

using Daphne;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using DaphneUserControlLib;

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
        public GraphicsProp(vtkRenderWindow rw)
        {
            this.rw = rw;
            inScene = false;
            prop = null;
        }

        /// <summary>
        /// cleanup vtk
        /// </summary>
        public void cleanup()
        {
            if (prop != null)
            {
                addToScene(false);
                prop.Dispose();
                prop = null;
            }
        }

        /// <summary>
        /// add/remove the prop; prevent multiple insertions/deletions
        /// </summary>
        /// <param name="add">indicates action</param>
        public void addToScene(bool add)
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

        /// <summary>
        /// render window accessor
        /// </summary>
        public vtkRenderWindow RW
        {
            get
            {
                return rw;
            }
            set
            {
                rw = value;
            }
        }

        private vtkProp prop;
        private vtkRenderWindow rw;
        private bool inScene;
    }

    /// <summary>
    /// encapsulates the VTK rendering pipeline for the environment
    /// </summary>
    public class VTKEnvironmentController
    {
        private GraphicsProp boxActor;
        private bool renderBox;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="rw">handle to the render window</param>
        public VTKEnvironmentController(vtkRenderWindow rw)
        {
            boxActor = new GraphicsProp(rw);
            renderBox = true;
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
            boxActor.cleanup();
        }

        /// <summary>
        /// set up the graphics pipeline for the environment box
        /// </summary>
        public void SetupPipeline()
        {
            // a simple box for quick and non-occluding indication of a volume
            vtkActor box = vtkActor.New();
            vtkOutlineFilter outlineFilter = vtkOutlineFilter.New();
            outlineFilter.SetInput(((VTKFullDataBasket)MainWindow.VTKBasket).EnvironmentController.BoxSource.GetOutput());
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
                boxActor.addToScene(renderBox);
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

        /// <summary>
        /// constructor
        /// </summary>
        public VTKECSController(vtkRenderWindow rw)
        {
            gradientActor = new GraphicsProp(rw);
            // Set renderGradient to true, so rendering capability will be there.
            // The actual control for rendering of ECM molecule concentrations is set by the RenderOn boolean.
            renderGradient = true;
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
            gradientActor.cleanup();
        }

        /// <summary>
        /// finish the pipelines for all molpop in the ecs
        /// </summary>
        public void Finish3DPipelines()
        {
            Compartment ecs = SimulationBase.dataBasket.Environment.Comp;
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
            gradientActor.RW.Render();

            // begin load openGL extensions; it seems this should not be necessary but we have seen problems on certain cards
            vtkOpenGLExtensionManager extMgr = new vtkOpenGLExtensionManager();

            extMgr.SetRenderWindow(gradientActor.RW);
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
            vmSmart.SetInput(((VTKFullDataBasket)MainWindow.VTKBasket).ECSController.ImageGrid);

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
                gradientActor.addToScene(renderGradient);
            }
        }
    }

    /// <summary>
    /// entity encapsulating a cell track
    /// </summary>
    public class VTKCellTrackController
    {
        private GraphicsProp actualTrack;

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellTrackController(vtkRenderWindow rw)
        {
            actualTrack = new GraphicsProp(rw);
        }

        /// <summary>
        /// retrieve the GraphicsProp object encapsulating the vtk actor of the actual track
        /// </summary>
        public GraphicsProp ActualTrack
        {
            get { return actualTrack; }
        }

        /// <summary>
        /// release track objects (VTK)
        /// </summary>
        public void Cleanup()
        {
            actualTrack.cleanup();
        }

        public void GenerateActualPathProp(VTKCellTrackData data)
        {
            vtkTubeFilter tubeFilter = vtkTubeFilter.New();
            tubeFilter.SetInputConnection(data.ActualTrack.GetProducerPort());
            tubeFilter.SetRadius(0.5); //default is .5
            tubeFilter.SetNumberOfSides(10);
            tubeFilter.CappingOn();

            vtkSphereSource sph = vtkSphereSource.New();
            sph.SetThetaResolution(8);
            sph.SetPhiResolution(8);
            sph.SetRadius(0.7);

            vtkGlyph3D glyp = vtkGlyph3D.New();
            glyp.SetSourceConnection(sph.GetOutputPort(0));
            glyp.SetInput(data.ActualTrack);
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
            range = data.ActualTrack.GetPointData().GetArray("time").GetRange();
            mapper.SetScalarRange(range[0], range[1]);

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            // actor.GetProperty().SetRepresentationToWireframe();  // Can add this line after 2/4/14
            actor.GetProperty().SetRepresentationToSurface();
            //tubeActor.GetProperty().SetColor(1.0, 1.0, 0.0);

            this.actualTrack.Prop = actor;
        }
    }

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

        /// <summary>
        /// accessor for the cell render method
        /// </summary>
        public CellRenderMethod render_method { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public VTKCellController(vtkRenderWindow rw)
        {
            // cells
            cellActor = new GraphicsProp(rw);
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
            GlyphFilter = vtkGlyph3D.New();

            // Glyph source can be verts, polys or spheres
            this.SetGlyphSource();
            GlyphFilter.SetInputConnection(((VTKFullDataBasket)MainWindow.VTKBasket).CellController.Poly.GetProducerPort());
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
            actor.GetProperty().SetRepresentationToSurface();
            // point size should only get used with vert representation
            actor.GetProperty().SetPointSize(8);
            cellActor.Prop = actor;
            cellActor.addToScene(true);
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
                cellActor.cleanup();
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

    public class VTKNullGraphicsController : EntityModelBase, IVTKGraphicsController
    {
        // Public accessors for toolbar button binding w/property changed notification
        public bool WhArrowToolButton_IsEnabled {get; set;}
        public bool WhArrowToolButton_IsChecked {get; set;}
        public bool HandToolButton_IsEnabled {get; set; }
        public bool HandToolButton_IsChecked { get; set; }
        public bool ToolsToolbar_IsEnabled {get; set; }
        public System.Windows.Visibility ColorScaleSlider_IsEnabled { get; set; }
        public double ColorScaleMaxFactor {get; set; }
        public ObservableCollection<string> ECSRenderingMethodNames { get; set; }
        public bool OrientationMarker_IsChecked { get; set; }
        public bool ResetCamera_IsChecked {get; set; }
        public bool ScalarBarMarker_IsChecked { get; set; }
        public string ECSRenderingMethod { get; set; }
        public WindowsFormsHost Wfh {get; set;}

        public VTKNullGraphicsController()
        {
        }

        public void Cleanup()
        {
        }

        public void CreatePipelines()
        {
        }

        public void DrawFrame(int progress)
        {
        }

        public void DisableComponents(bool complete)
        {
        }

        public void EnableComponents(bool finished)
        {
        }

        public void ResetGraphics()
        {
        }
    }

    /// <summary>
    /// entity encapsulating the control of a simulation's 3D VTK graphics
    /// </summary>
    public class VTKFullGraphicsController : EntityModelBase, IVTKGraphicsController
    {
        // the environment outline box controller
        private VTKEnvironmentController environmentController;
        // simulation progress string
        private GraphicsProp cornerAnnotation;
        // the cells
        private VTKCellController cellController;
        // the ecs
        private VTKECSController ecsController;
        // dictionary holding track data keyed by cell id
        private Dictionary<int, VTKCellTrackController> cellTrackControllers;
        // dictionary holding all of the region widgets for this VTK window
        private Dictionary<string, RegionWidget> regions;
        private RenderWindowControl rwc;
        private vtkRenderWindow rw;
        private WindowsFormsHost wfh;
        private CellTrackTool trackTool;
        private vtkOrientationMarkerWidget axesTool;
        private vtkScalarBarWidget scalarBar;

        private static byte CURSOR_ARROW = 1,
                            CURSOR_HAND = 9;
        // variables for binding to toolbar buttons
        private bool whArrowToolButton_IsEnabled = false;
        private bool whArrowToolButton_IsChecked = true;
        private bool handToolButton_IsEnabled = true;
        private bool handToolOption_IsEnabled;
        private bool handToolButton_IsChecked = false;
        private bool toolsToolbar_IsEnabled = true;
        private bool resetCameraButton_IsChecked = false;
        private bool orientationMarker_IsChecked = true;
        private bool backgroundButton_IsChecked = false;
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
        public ObservableCollection<string> CellSelectionToolModes { get; set; }
        private string cellSelectionToolMode;

        private bool tracksActive;
        public bool TracksActive
        {
            get
            {
                return tracksActive;
            }
            set
            {
                if (value != tracksActive)
                {
                    tracksActive = value;
                    OnPropertyChanged("TracksActive");
                }
            }
        }

        private System.Windows.Media.Color currentColor;
        public System.Windows.Media.Color CurrentColor
        {
            get
            {
                return currentColor;
            }
            set
            {
                currentColor = value;
                ChangeBackground();
                OnPropertyChanged("CurrentColor");
            }
        }

        /// <summary>
        /// Changes the background color of the 3D window
        /// </summary>
        private void ChangeBackground()
        {
            rw = rwc.RenderWindow;
            vtkRenderer ren = rw.GetRenderers().GetFirstRenderer();
            ren.SetBackground(CurrentColor.R/255.0, CurrentColor.G/255.0, CurrentColor.B/255.0);
            vtkWindow currWindow = ren.GetVTKWindow();
            currWindow.Render();
        }

        private bool leftButtonPressed = false;
        private uint leftButtonPressTimeStamp = 0;
        private int[] leftButtonPressPostion = new int[2];

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
        public VTKFullGraphicsController(MainWindow mw)
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

            // background color - initialize to black
            CurrentColor = Colors.Black;
            ren.SetBackground(0.0, 0.0, 0.0);

            // interactor style
            vtkInteractorStyleSwitch istyle = vtkInteractorStyleSwitch.New();
            rw.GetInteractor().SetInteractorStyle(istyle);
            rw.GetInteractor().SetPicker(vtkCellPicker.New());
            (istyle).SetCurrentStyleToTrackballCamera();

            // add events to the iren instead of Observers
            rw.GetInteractor().LeftButtonPressEvt += new vtkObject.vtkObjectEventHandler(leftMouseDown);
            rw.GetInteractor().EndInteractionEvt += new vtkObject.vtkObjectEventHandler(leftMouseClick);

            rw.GetInteractor().MouseMoveEvt += new vtkObject.vtkObjectEventHandler(onMouseMove);

            // progress
            cornerAnnotation = new GraphicsProp(rw);
            vtkCornerAnnotation prop = vtkCornerAnnotation.New();
            prop.SetLinearFontScaleFactor(2);
            prop.SetNonlinearFontScaleFactor(1);
            prop.SetMaximumFontSize(14);
            prop.GetTextProperty().SetColor(1, 1, 1);
            cornerAnnotation.Prop = prop;
            cornerAnnotation.addToScene(true);

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
            axesTool.SetEnabled(1);

            this.scalarBar = vtkScalarBarWidget.New();
            this.scalarBar.SetInteractor(rwc.RenderWindow.GetInteractor());
            this.scalarBar.GetScalarBarActor().SetNumberOfLabels(3);
            this.scalarBar.SetEnabled(0);

            // environment box
            environmentController = new VTKEnvironmentController(rw);

            // cells
            cellController = new VTKCellController(rw);

            // extracellular medium
            ecsController = new VTKECSController(rw);

            // cell tracks
            cellTrackControllers = new Dictionary<int, VTKCellTrackController>();

            // This list will be regenerated on each CreatePipelines() call
            this.CellAttributeArrayNames = new ObservableCollection<string>();

            // Fixed set of molpop rendering options, so pre-generate this list
            ECSRenderingMethodNames = new ObservableCollection<string>();
            ECSRenderingMethodNames.Add("Outline");
            ECSRenderingMethodNames.Add("No Outline");
            // regions
            regions = new Dictionary<string, RegionWidget>();

            //Cell tool selection modes
            this.CellSelectionToolModes = new ObservableCollection<string>();
            CellSelectionToolModes.Add("None");
            CellSelectionToolModes.Add("Tracks");
            CellSelectionToolModes.Add("Cell Information");
            CellSelectionToolModes.Add("Hovering Tooltip");
            CellSelectionToolMode = CellSelectionToolModes[0];

            trackTool = new CellTrackTool();
            TracksActive = false;
        }

        /// <summary>
        /// free allocated memory
        /// </summary>
        public void Cleanup()
        {
            foreach (KeyValuePair<string, RegionWidget> kvp in regions)
            {
                kvp.Value.ShowWidget(false);
                kvp.Value.ShowActor(RWC.RenderWindow, false);
                kvp.Value.CleanUp();
            }
            regions.Clear();
            environmentController.Cleanup();
            cellController.Cleanup();
            ecsController.Cleanup();
            CleanupTracks();
            CellAttributeArrayNames.Clear();
            // ColorScaleMaxFactor = 1.0;
        }

        public void DisableComponents(bool complete)
        {
            if (complete == true)
            {
                ToolsToolbar_IsEnabled = false;
            }
            DisablePickingButtons();
        }

        public void EnableComponents(bool finished)
        {
            if (finished == true)
            {
                EnablePickingButtons();
            }
            ToolsToolbarEnableAllIcons();
        }

        public void ResetGraphics()
        {
            CellController.SetCellOpacities(1.0);
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

        public bool BackgroundButton_IsChecked
        {
            get { return backgroundButton_IsChecked; }
            set
            {
                if (value == backgroundButton_IsChecked)
                    return;
                else
                {
                    backgroundButton_IsChecked = value;
                    rwc.Invalidate();
                    base.OnPropertyChanged("BackgroundButton_IsChecked");
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
                this.RWC.Invalidate();
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
                    this.RWC.Invalidate();
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
                        if (value == "Outline")
                        {
                            EnvironmentController.RenderBox = true;
                        }
                        else if (value == "No Outline")
                        {
                            EnvironmentController.RenderBox = false;
                        }
                        else
                        {
                            EnvironmentController.RenderBox = true;
                        }
                        EnvironmentController.drawEnvBox();
                        ECSController.draw3D();
                        RWC.Invalidate();
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

        public string CellSelectionToolMode
        {
            get { return this.cellSelectionToolMode; }
            set
            {
                if (value == this.cellSelectionToolMode)
                    return;
                else
                {
                    if (value != null)
                    {
                        this.cellSelectionToolMode = value;
                        base.OnPropertyChanged("CellSelectionToolMode");
                    }
                }
            }
        }

        private void setColorMap()
        {
            this.CellController.CellMapper.SelectColorArray(this.cellColorArrayName);
            if (this.CellController.GlyphData != null)
            {
                var VTKBasket = ((VTKFullDataBasket)MainWindow.VTKBasket);
                this.CellController.CellMapper.SetLookupTable(VTKBasket.CellController.CellColorTable);
                this.CellController.CellMapper.SelectColorArray(this.cellColorArrayName);
                int tmp = (int)VTKBasket.CellController.CellColorTable.GetNumberOfTableValues();
                this.CellController.CellMapper.SetScalarRange(0, tmp - 1);
                this.ColorScaleSlider_IsEnabled = System.Windows.Visibility.Collapsed;
                this.scalarBar.GetScalarBarActor().SetLookupTable(cellController.CellMapper.GetLookupTable());
                this.scalarBar.GetScalarBarActor().SetTitle(this.CellColorArrayName);
                RWC.Invalidate();
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
                    RWC.Invalidate();
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
                    CellController.SetCellOpacities(value ? MainWindow.cellOpacity : 1.0);
                    RWC.RenderWindow.SetCurrentCursor(value ? CURSOR_ARROW : CURSOR_HAND);
                    RWC.Invalidate();
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

        public bool HandToolOption_IsEnabled
        {
            get { return handToolOption_IsEnabled; }
            set
            {
                if (handToolOption_IsEnabled == value)
                    return;
                else
                {
                    handToolOption_IsEnabled = value;
                    base.OnPropertyChanged("HandToolOption_IsEnabled");
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
                    CellController.SetCellOpacities(!value ? MainWindow.cellOpacity : 1.0);
                    RWC.RenderWindow.SetCurrentCursor(!value ? CURSOR_ARROW : CURSOR_HAND);
                    RWC.Invalidate();
                    base.OnPropertyChanged("HandToolButton_IsChecked");
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

        public void ToolsToolbarEnableOnlyHand()
        {
            ToolsToolbar_IsEnabled = true;
            HandToolButton_IsEnabled = true;
            HandToolOption_IsEnabled = true;
            WhArrowToolButton_IsEnabled = false;
            MW.CellOptionsExpander.IsEnabled = true;
            MW.ECMOptionsExpander.IsEnabled = true;
            MW.OrientationMarkerButton.IsEnabled = false;
            MW.ResetCameraButton.IsEnabled = false;
            MW.save3DView.IsEnabled = false;
        }

        public void ToolsToolbarEnableAllIcons()
        {
            ToolsToolbar_IsEnabled = true;
            HandToolButton_IsEnabled = true;
            HandToolOption_IsEnabled = true;
            WhArrowToolButton_IsEnabled = true;
            WhArrowToolButton_IsChecked = true;
            MW.CellOptionsExpander.IsEnabled = true;
            MW.ECMOptionsExpander.IsEnabled = true;
            MW.OrientationMarkerButton.IsEnabled = true;
            MW.ResetCameraButton.IsEnabled = true;
            MW.save3DView.IsEnabled = true;
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
            HandToolOption_IsEnabled = true;
            HandToolButton_IsChecked = false;
            RWC.RenderWindow.SetCurrentCursor(CURSOR_ARROW);
            TracksActive = true;
        }

        public void DisablePickingButtons()
        {
            WhArrowToolButton_IsEnabled = false;
            WhArrowToolButton_IsChecked = true;
            HandToolButton_IsEnabled = false;
            HandToolOption_IsEnabled = false;
            HandToolButton_IsChecked = false;
            RWC.RenderWindow.SetCurrentCursor(CURSOR_ARROW);
            TracksActive = false;
        }

        public RenderWindowControl RWC
        {
            get { return rwc; }
        }

        public WindowsFormsHost Wfh
        {
            get { return wfh; }
        }

        private void WidgetTransformToBoxMatrix(RegionWidget rw, BoxSpecification bs)
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
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;

            // NOTE: This callback specific to this GC (referencing Regions). 
            //   Not sure what will happen with mutiple GCs...
            if (rw != null)
            {
                if (rw.Gaussian != null && rw.Gaussian.box_spec != null)
                {
                    BoxSpecification box = rw.Gaussian.box_spec;

                    if (transferMatrix == true)
                    {
                        double[] r = null, t = null, s = null;

                        // get the scale, rotation, translation, check against their min/max values, and correct if needed
                        rw.GetScaleRotationTranslation(ref s, ref r, ref t);
                        // when the region gets turned inside out the scales all become negative
                        if (s[0] < 0 && s[1] < 0 && s[2] < 0)
                        {
                            // restore the box matrix; the latter is known to be good
                            rw.SetTransform(box.transform_matrix, RegionControl.PARAM_SCALE);
                            // transfer transform to VTKDataBasket
                            ((VTKFullDataBasket)MainWindow.VTKBasket).Regions[box.box_guid].SetTransform(rw.GetTransform(), 0);
                            return;
                        }
#if USE_BOX_LIMITS
                        bool changed = false;

                        // translation
                        if (t[0] < scenario.box_guid_box_dict[key].x_trans_min)
                        {
                            t[0] = scenario.box_guid_box_dict[key].x_trans_min;
                            changed = true;
                        }
                        if (t[0] > scenario.box_guid_box_dict[key].x_trans_max)
                        {
                            t[0] = scenario.box_guid_box_dict[key].x_trans_max;
                            changed = true;
                        }
                        if (t[1] < scenario.box_guid_box_dict[key].y_trans_min)
                        {
                            t[1] = scenario.box_guid_box_dict[key].y_trans_min;
                            changed = true;
                        }
                        if (t[1] > scenario.box_guid_box_dict[key].y_trans_max)
                        {
                            t[1] = scenario.box_guid_box_dict[key].y_trans_max;
                            changed = true;
                        }
                        if (t[2] < scenario.box_guid_box_dict[key].z_trans_min)
                        {
                            t[2] = scenario.box_guid_box_dict[key].z_trans_min;
                            changed = true;
                        }
                        if (t[2] > scenario.box_guid_box_dict[key].z_trans_max)
                        {
                            t[2] = scenario.box_guid_box_dict[key].z_trans_max;
                            changed = true;
                        }
                        // scale
                        if (s[0] < RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].x_scale_min)
                        {
                            s[0] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].x_scale_min;
                            changed = true;
                        }
                        if (s[0] > RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].x_scale_max)
                        {
                            s[0] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].x_scale_max;
                            changed = true;
                        }
                        if (s[1] < RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].y_scale_min)
                        {
                            s[1] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].y_scale_min;
                            changed = true;
                        }
                        if (s[1] > RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].y_scale_max)
                        {
                            s[1] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].y_scale_max;
                            changed = true;
                        }
                        if (s[2] < RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].z_scale_min)
                        {
                            s[2] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].z_scale_min;
                            changed = true;
                        }
                        if (s[2] > RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].z_scale_max)
                        {
                            s[2] = RegionControl.SCALE_CORRECTION * scenario.box_guid_box_dict[key].z_scale_max;
                            changed = true;
                        }

                        // apply if needed
                        if(changed == true)
                        {
                            rw.SetScaleRotationTranslation(s, r, t, 0);
                        }
#endif
                        WidgetTransformToBoxMatrix(rw, box);
                        // Transfer transform to VTKDataBasket
                        ((VTKFullDataBasket)MainWindow.VTKBasket).Regions[box.box_guid].SetTransform(rw.GetTransform(), 0);
                    }
                }
            }
        }

        private Popup infoPop = new Popup();

        public void onMouseMove(vtkObject sender, vtkObjectEventArgs e)
        {
            if (MW.VCRbutton_Play.IsChecked == (bool?)true)
            {
                return;
            }

            vtkRenderWindowInteractor interactor = rwc.RenderWindow.GetInteractor();
            int[] location = interactor.GetEventPosition();

            // Increase the tolerance for locating cells for tracking and display of cell information
            // when cells are rendered as points or polygons.
            double orig_tolerance = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetTolerance();
            if (cellRenderMethod != CellRenderMethod.CELL_RENDER_SPHERES)
            {
                ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(0.01);
            }

            int p = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).Pick(location[0], location[1], 0, rwc.RenderWindow.GetRenderers().GetFirstRenderer());

            if (p > 0)
            {
                p = (int)((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetPointId();

                bool isCellPicked = (((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetDataSet() == cellController.GlyphData);
                
                //If info box already displayed, skip all this
                if (p >= 0 && infoPop.IsOpen == false && isCellPicked)
                {
                    //This statement for debugging only
                    //Console.WriteLine("In onMouseMove over cell");

                    int cellID = CellController.GetCellIndex(p);

                    GraphicsProp prop = CellController.CellActor;
                    vtkProp vProp = prop.Prop;

                    if (MainWindow.CheckMouseLeftState(MainWindow.MOUSE_LEFT_CELL_TOOLTIP) == true)
                    {

                        if (SimulationBase.dataBasket.Cells.ContainsKey(cellID) == false)
                        {
                            //MessageBox.Show("No information available. This cell may no longer exist.", "Mouse hover error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Cell cell = SimulationBase.dataBasket.Cells[cellID];

                        infoPop.AllowsTransparency = true;
                        infoPop.PopupAnimation = PopupAnimation.Fade;
                        infoPop.PlacementTarget = MW.VTKDisplayDocWindow;
                        infoPop.Placement = PlacementMode.Mouse;

                        //Here, gather the necessary information
                        TextBox tb = new TextBox();
                        GetCellInfo(cellID, cell, tb);

                        infoPop.Child = tb;
                        infoPop.IsOpen = true;
                    }
                }
            }
            else if (infoPop.IsOpen == true)
            {
                infoPop.IsOpen = false;
            }
        }

        /// <summary>
        /// Retrieve cell info to be displayed when mouse pointer hovers over a cell
        /// </summary>
        /// <param name="cellID"></param>
        /// <param name="cell"></param>
        /// <param name="tb"></param>
        private void GetCellInfo(int cellID, Cell cell, TextBox tb)
        {
            CellPopulation pop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(cell.Population_id);
            string cellName = pop.Cell.CellName;

            SolidColorBrush brush = new SolidColorBrush(new Color { A = 92, R = 255, G = 255, B = 255 });
            tb.Background = brush;        //was Brushes.Transparent;
            tb.Foreground = Brushes.Yellow;
            tb.BorderThickness = new Thickness { Left = 1, Right = 1, Bottom = 1, Top = 1 };
            tb.BorderBrush = Brushes.White;
            tb.MaxWidth = 200;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.TextAlignment = TextAlignment.Left;

            tb.Text += "Cell Name: " + cellName;
            tb.Text += "\nCell ID: " + cellID.ToString();
            if (cell.Divider.nStates > 0)
            {
                tb.Text += "\nLineage ID: " + cell.Lineage_id.ToString();
            }
            if (cell.Differentiator.nStates > 0)
            {
                string diffstate = pop.Cell.diff_scheme.Driver.states[cell.DifferentiationState];
                tb.Text += "\nDifferentiation state: " + diffstate;
            }
            if (cell.Divider.nStates > 0)
            {
                string divstate = pop.Cell.div_scheme.Driver.states[cell.DividerState];
                tb.Text += "\nDivision state: " + divstate;
                tb.Text += "\nGeneration: " + cell.generation.ToString();
            }
            tb.Text += "\n" + "Location:";

            int X = (int)cell.SpatialState.X[0];
            int Y = (int)cell.SpatialState.X[1];
            int Z = (int)cell.SpatialState.X[2];

            tb.Text += string.Format("  ({0},{1},{2})", X, Y, Z);

        }

        public void leftMouseDown(vtkObject sender, vtkObjectEventArgs e)
        {
            if (!HandToolButton_IsChecked)
                return;

            vtkRenderWindowInteractor interactor = rwc.RenderWindow.GetInteractor();
            int[] x = interactor.GetEventPosition();
            leftButtonPressPostion[0] = x[0];
            leftButtonPressPostion[1] = x[1];
            leftButtonPressed = true;
            leftButtonPressTimeStamp = interactor.GetMTime();
        }

        /// <summary>
        /// handler for left mouse click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void leftMouseClick(vtkObject sender, vtkObjectEventArgs e)
        {
            if (!HandToolButton_IsChecked || !leftButtonPressed)
            {
                return;
            }

            // for testing only
            //vtkRenderWindowInteractor interactor = rwc.RenderWindow.GetInteractor();

            leftButtonPressed = false;
            //if (interactor.GetMTime() - leftButtonPressTimeStamp > 100)
            //{
            //    return;
            //}

            //int[] x = interactor.GetEventPosition();
            int[] x = leftButtonPressPostion;


            // Increase the tolerance for locating cells for tracking and display of cell information
            // when cells are rendered as points or polygons.
            double orig_tolerance = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetTolerance();
            if (cellRenderMethod != CellRenderMethod.CELL_RENDER_SPHERES)
            {
                ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(0.01);
            }

            int p = ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).Pick(x[0], x[1], 0, rwc.RenderWindow.GetRenderers().GetFirstRenderer());

            if (p > 0)
            {
                p = (int)((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).GetPointId();
                if (p >= 0)
                {
                    int cellID = CellController.GetCellIndex(p);

                    if (SimulationBase.dataBasket.Cells.ContainsKey(cellID) == false)
                    {
                        return;
                    }
                    MainWindow.selectedCell = SimulationBase.dataBasket.Cells[cellID];

                    if (MainWindow.CheckMouseLeftState(MainWindow.MOUSE_LEFT_TRACK) == true)
                    {
                        if (trackTool.IsInitialized(cellID) == false)
                        {
                            CellTrackData data = MainWindow.Sim.Reporter.ProvideCellTrackData(cellID);

                            if (data == null)
                            {
                                string detail;

                                if (((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulation_dict.ContainsKey(MainWindow.selectedCell.Population_id) == true)
                                {
                                    detail = "Cell positions must be reported for cell population " +
                                             ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulation_dict[MainWindow.selectedCell.Population_id].cellpopulation_name + ".";
                                }
                                else
                                {
                                    detail = "Cell population " + MainWindow.selectedCell.Population_id + " does not exist.";
                                }

                                MessageBox.Show("The data needed to generate tracks is not present in the report.\n" + detail, "Track warning", MessageBoxButton.OK);
                                ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(orig_tolerance); 
                                return;
                            }
                            trackTool.FilterData(data);
                            // there must be at least 2 points
                            if (data.Times.Count >= 2)
                            {
                                trackTool.InitializeCellTrack(data, cellID);
                            }
                            else
                            {
                                MessageBox.Show("The track data has less than two points. A track cannot get generated for this cell.\n" +
                                                "Possible reasons:\n" +
                                                "-the number of simulation steps is too small.\n" + 
                                                "-the cell does not move significantly: identical points along a track must get removed for computational reasons, " +
                                                "reducing the number of track points.", "Track warning", MessageBoxButton.OK);
                                ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(orig_tolerance); 
                                return;
                            }
                        }
                        trackTool.ToggleCellTrack(cellID);
                    }
                    else if (MainWindow.CheckMouseLeftState(MainWindow.MOUSE_LEFT_CELL_MOLCONCS) == true)
                    {
                        //Cell c = Simulation.dataBasket.Cells[cellID];

                        MW.DisplayCellInfo(cellID);
                    }                    
                    else
                    {
                        ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(orig_tolerance); 
                        return;
                    }
                }
            }
            ((vtkCellPicker)rwc.RenderWindow.GetInteractor().GetPicker()).SetTolerance(orig_tolerance); 
        }

        public void zoomToPath(vtkProp p)
        {
            if (p != null && Properties.Settings.Default.autoZoomFit == true)
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

                var fp = rwc.RenderWindow.GetRenderers().GetFirstRenderer().GetActiveCamera().GetFocalPoint();
                var p = rwc.RenderWindow.GetRenderers().GetFirstRenderer().GetActiveCamera().GetPosition();
                var dist = Math.Sqrt((p[0] - fp[0]) * (p[0] - fp[0]) + (p[1] - fp[1]) * (p[1] - fp[1]) + (p[2] - fp[2]) * (p[2] - fp[2]));
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().GetActiveCamera().SetPosition(fp[0], fp[1], fp[2] + dist);
                rwc.RenderWindow.GetRenderers().GetFirstRenderer().GetActiveCamera().SetViewUp(0.0, 1.0, 0.0);

            }
        }

        /// <summary>
        /// retrieve the cell tracks
        /// </summary>
        public Dictionary<int, VTKCellTrackController> CellTrackControllers
        {
            get { return cellTrackControllers; }
        }

        /// <summary>
        /// cleanup the cell tracks
        /// </summary>
        public void CleanupTracks()
        {
            // the tracks
            foreach (KeyValuePair<int, VTKCellTrackController> kvp in cellTrackControllers)
            {
                kvp.Value.Cleanup();
            }
            cellTrackControllers.Clear();
        }

        /// <summary>
        /// Create the VTK graphics pipelines for all cells and ecs, but use pre-allocated arrays for speed
        /// This will clear all old pipelines and generate new ones based on VTKDataBasket contents
        /// </summary>
        public void CreatePipelines()
        {
            Cleanup();

            // Regions
            CreateRegionWidgets();

            var VTKBasket = (VTKFullDataBasket)MainWindow.VTKBasket;
            // Cells
            if (SimulationBase.dataBasket.Cells != null && VTKBasket.CellController.Poly != null && VTKBasket.CellController.getAssignCellIndex() > 0)
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

                if (this.CellColorArrayName == null && this.CellAttributeArrayNames.Contains("cellColorMapper"))
                {
                    this.CellColorArrayName = "cellColorMapper";
                }
                else if (this.CellAttributeArrayNames.Contains(this.CellColorArrayName))
                {
                    // Pull a background switcheroo to force color map to be applied and property change notice to be fired
                    // to keep continuity of old colormap choice across Resets
                    var tmp = this.CellColorArrayName;
                    this.cellColorArrayName = "";
                    this.CellColorArrayName = tmp;
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
            EnvironmentController.SetupPipeline();

            // ecs
            if (SimulationBase.dataBasket.Environment != null &&
                SimulationBase.dataBasket.Environment is ECSEnvironment &&
                ((VTKFullDataBasket)MainWindow.VTKBasket).ECSController.ImageGrid != null)
            {
                ecsController.Finish3DPipelines();
                // Make "Outline" a hard-coded first-pass default for now, otherwise keep old value
                if (ECSRenderingMethod == null)
                {
                    ECSRenderingMethod = "Outline";
                }
            }
        }

        public void AddGaussSpecRegionWidget(GaussianSpecification gs)
        {
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;

            // Find the box spec that goes with this gaussian spec
            BoxSpecification bs = gs.box_spec;

            RegionWidget rw = new RegionWidget(RWC.RenderWindow, gs, RegionShape.Ellipsoid);

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
            rw.ShowActor(RWC.RenderWindow, gs.gaussian_region_visibility);
            // NOTE: Callback being added afterwards in MainWindow for now...

            Regions.Add(gs.box_spec.box_guid, rw);
        }

        public void RemoveRegionWidget(string current_guid)
        {
            Regions[current_guid].ShowWidget(false);
            Regions[current_guid].ShowActor(RWC.RenderWindow, false);
            Regions[current_guid].CleanUp();
            Regions.Remove(current_guid);
        }

        public void CreateRegionWidgets()
        {
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            // Gaussian specs
            GaussianSpecification next;

            scenario.resetGaussRetrieve();
            while ((next = scenario.nextGaussSpec()) != null)
            {
                AddGaussSpecRegionWidget(next);
            }
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

        public VTKCellTrackController CreateVTKCellTrackController()
        {
            return new VTKCellTrackController(rw);
        }

        //hide tracks
        public void HideCellTracks()
        {
            if (this.trackTool != null)
            {
                trackTool.HideCellTracks();
            }
        }

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
                // special case: vcr
                if (MainWindow.VCR != null && MainWindow.VCR.CheckFlag(VCRControl.VCR_OPEN) == true)
                {
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Progress: " + progress + "%");
                }
                // regular handling
                else if (MainWindow.Sim.RunStatus == SimulationBase.RUNSTAT_OFF)
                {
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "");
                }
                else if (MainWindow.Sim.Burn_inActive() == true)
                {
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Equilibrating...");
                }
                else if (MainWindow.RepeatingRun() == true)
                {
                    int rep = MainWindow.Repetition;
                    int reps = MainWindow.SOP.Protocol.experiment_reps;

                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Rep: " + rep + "/" + reps + " Progress: " + progress + "%");
                }
                else
                {
                    ((vtkCornerAnnotation)cornerAnnotation.Prop).SetText(0, "Progress: " + progress + "%");
                }
            }
            rwc.Invalidate();
            if (progress >= 100 && MainWindow.enable_clock)
            {
                MainWindow.mywatch.Stop();
                var time_elapsed = MainWindow.mywatch.Elapsed.TotalSeconds;
                System.Windows.MessageBox.Show(string.Format("total time of simuliation is {0} seconds", time_elapsed));
            }
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
        public void SaveToFile(string filename, double[] rgb)
        {
            vtkImageWriter imageWriter;

            if (filename.EndsWith("bmp"))
            {
                imageWriter = new vtkBMPWriter();
            }
            else if (filename.EndsWith("jpg"))
            {
                imageWriter = new vtkJPEGWriter();
                ((vtkJPEGWriter)imageWriter).SetQuality(100);
                ((vtkJPEGWriter)imageWriter).SetProgressive(0);
            }
            else if (filename.EndsWith("png"))
            {
                imageWriter = new vtkPNGWriter();
            }
            else if (filename.EndsWith("tif"))
            {
                imageWriter = new vtkTIFFWriter();
            }
            else
            {
                imageWriter = new vtkBMPWriter();
            }

            vtkRenderer ren = rwc.RenderWindow.GetRenderers().GetFirstRenderer();
            vtkWindow currWindow = ren.GetVTKWindow();

            // remember the current color
            double[] currentColor = ren.GetBackground(); 
            // new background color
            ren.SetBackground(rgb[0], rgb[1], rgb[2]);

            vtkWindowToImageFilter w2if = new vtkWindowToImageFilter();
            w2if.SetInput(rwc.RenderWindow);

            //Create Image output file            
            imageWriter.SetInput(w2if.GetOutput());
            imageWriter.SetFileName(filename);
            imageWriter.Write();

            // reset background to original color
            ren.SetBackground(currentColor[0], currentColor[1], currentColor[2]);
            currWindow.Render();
        }
    }
}
