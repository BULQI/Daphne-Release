using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kitware.VTK;

using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// entity to implement a region widget
    /// </summary>
    public class RegionWidget
    {
        /// <summary>
        /// allow connecting a user callback
        /// </summary>
        public delegate void CallbackHandler(RegionWidget rw, bool userFlag);

        // TODO: Need to either split this into a central transform in VTKDataBasket that real RegionWidgets
        //   update from, or need to make sure when different VTK doc windows are activated, the region widgets
        //   (but maybe not their shape actors) get swapped between windows. Not supposed to use the same actor
        //   in multiple renderers. Should have separate mappers and actors for each renderer...

        private GraphicsProp shapeActor;
        private vtkBoxWidget2 boxWidget;
        private vtkTransform savedTransform;
        private List<CallbackHandler> callbacks;
        private byte dirtyFlags;
        private double red, green, blue, opacity;
        private RegionShape shape;
        private GaussianSpecification gaussian;

        // NOTE: Static flags for transform options and dirty are in RegionControl

        public RegionWidget(vtkRenderWindow rw, GaussianSpecification gs, RegionShape shape = RegionShape.Rectangular)
        {
            boxWidget = vtkBoxWidget2.New();
            boxWidget.SetInteractor(rw.GetInteractor());
            //boxWidget.CreateDefaultRepresentation();
            // boxWidget.StartInteractionEvt += new vtkObject.vtkObjectEventHandler(TransformShapeHandler);
            boxWidget.InteractionEvt += new vtkObject.vtkObjectEventHandler(TransformShapeHandler);

            vtkBoxRepresentation boxRepresentation = vtkBoxRepresentation.New();
            boxRepresentation.GetOutlineProperty().SetColor(0.4, 0.4, 0.4);
            boxRepresentation.GetOutlineProperty().SetLineWidth(0.5f);
            ////boxRepresentation.OutlineFaceWiresOn();
            boxWidget.SetRepresentation(boxRepresentation);

            dirtyFlags = RegionControl.DIRTY_ALL;
            savedTransform = vtkTransform.New();

            // set some agreeable defaults
            red = green = blue = opacity = 0.5;

            shapeActor = new GraphicsProp(rw);

            // create the associated actor
            if (shape == RegionShape.Rectangular)
            {
                SetCube();
            }
            else if (shape == RegionShape.Ellipsoid)
            {
                ////Add this after 2/4/14
                ////bool wireframe = false;
                ////if (gs != null)
                ////    wireframe = gs.DrawAsWireframe;
                ////SetSphere(wireframe);      
                SetSphere();
            }
            else
            {
                // error
                CleanUp();
                return;
            }
            if (shapeActor.Prop != null)
            {
                ((vtkActor)shapeActor.Prop).SetPickable(0);
            }
            this.shape = shape;

            gaussian = gs;

            this.callbacks = new List<CallbackHandler>();
        }

        /// <summary>
        /// retrieve a handle to the box widget object
        /// </summary>
        public vtkBoxWidget2 BoxWidget
        {
            get { return boxWidget; }
        }

        /// <summary>
        /// retrieve a handle to the encapsulated Gaussian
        /// </summary>
        public GaussianSpecification Gaussian
        {
            get { return gaussian; }
        }

        /// <summary>
        /// set the region shape
        /// </summary>
        /// <param name="rw">VTK render window object</param>
        /// <param name="shape">value indicating the shape</param>
        public void SetShape(vtkRenderWindow rw, RegionShape shape)
        {
            if (this.shape == shape || shapeActor.Prop == null)
            {
                return;
            }

            if (Showing() == true)
            {
                shapeActor.addToScene(false);
            }

            if (shape == RegionShape.Rectangular)
            {
                SetCube();
            }
            else if (shape == RegionShape.Ellipsoid)
            {
                SetSphere();
            }
            AdjustShape(GetTransform());
            ((vtkActor)shapeActor.Prop).SetPickable(0);

            if (Showing() == true)
            {
                shapeActor.addToScene(true);
            }
            this.shape = shape;
        }

        /// <summary>
        /// retrieve the region's shape
        /// </summary>
        public RegionShape Shape
        {
            get { return shape; }
        }

        /// <summary>
        /// connect a user callback to the list
        /// </summary>
        /// <param name="callback">pointer to the user callback</param>
        public void AddCallback(CallbackHandler callback)
        {
            this.callbacks.Add(callback);
        }

        /// <summary>
        /// remove a user callback from the list
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveCallback(CallbackHandler callback)
        {
            this.callbacks.Remove(callback);
        }

        /// <summary>
        /// clear the list of user callbacks
        /// </summary>
        public void ClearCallbacks()
        {
            this.callbacks.Clear();
        }

        /// <summary>
        /// retrieve the implicit function of the enclosed shape
        /// </summary>
        /// <returns>an implicit function object</returns>
        public vtkImplicitFunction GetImplicitFunction()
        {
            if (boxWidget == null)
            {
                return null;
            }

            vtkImplicitFunction iFunction = null;

            if (shape == RegionShape.Ellipsoid)
            {
                iFunction = vtkSphere.New();
                ((vtkSphere)iFunction).SetRadius(RegionControl.UNIT_SHAPE);
                iFunction.SetTransform(GetTransform().GetInverse());
            }
            else if (shape == RegionShape.Rectangular)
            {
                iFunction = vtkBox.New();
                ((vtkBox)iFunction).SetBounds(-RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE,
                                              -RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE,
                                              -RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE);
                iFunction.SetTransform(GetTransform().GetInverse());
            }
            return iFunction;
        }

        /// <summary>
        /// return the widget's transform
        /// </summary>
        /// <param name="param">specify special handling instructions (deep or scale); only use outside of this class</param>
        /// <returns>pointer to the transform</returns>
        public vtkTransform GetTransform(byte param = 0)
        {
            if (boxWidget == null)
            {
                return null;
            }

            // only update the saved transform once
            if ((dirtyFlags & RegionControl.DIRTY_TRANSFORM) != 0)
            {
                ((vtkBoxRepresentation)boxWidget.GetRepresentation()).GetTransform(savedTransform);
                dirtyFlags &= (byte)(~RegionControl.DIRTY_TRANSFORM);
            }

            // if the caller will modify the transform or a scale correction is needed then make a deep copy
            if ((param & RegionControl.PARAM_DEEP_COPY) != 0 || (param & RegionControl.PARAM_SCALE) != 0)
            {
                vtkTransform t = vtkTransform.New();

                t.DeepCopy(savedTransform);
                if ((param & RegionControl.PARAM_SCALE) != 0)
                {
                    t.Scale(1.0 / RegionControl.SCALE_CORRECTION, 1.0 / RegionControl.SCALE_CORRECTION, 1.0 / RegionControl.SCALE_CORRECTION);
                }
                return t;
            }
            else
            {
                return savedTransform;
            }
        }

        /// <summary>
        /// sets the widget transform; effectively sizes and places the widget
        /// </summary>
        /// <param name="transform">the transform</param>
        /// <param name="param">can specify to apply the scale correction</param>
        public void SetTransform(vtkTransform transform, byte param = 0)
        {
            if (boxWidget != null)
            {
                if ((param & RegionControl.PARAM_SCALE) != 0)
                {
                    transform.Scale(RegionControl.SCALE_CORRECTION, RegionControl.SCALE_CORRECTION, RegionControl.SCALE_CORRECTION);
                }
                ((vtkBoxRepresentation)boxWidget.GetRepresentation()).SetTransform(transform);
                AdjustShape(transform);
                dirtyFlags = RegionControl.DIRTY_ALL;
            }
        }

        /// <summary>
        /// sets the transform from a matrix
        /// </summary>
        /// <param name="matrix">the matrix as an array, row by row</param>
        /// <param name="param">can specify to apply the scale correction</param>
        public void SetTransform(double[][] matrix, byte param = 0)
        {
            if (matrix.Length != 4 || matrix[0].Length != 4 || matrix[1].Length != 4 || matrix[2].Length != 4 || matrix[3].Length != 4 || boxWidget == null)
            {
                return;
            }

            vtkMatrix4x4 mat = vtkMatrix4x4.New();
            vtkTransform transform = vtkTransform.New();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    mat.SetElement(i, j, matrix[i][j]);
                }
            }
            transform.SetMatrix(mat);
            SetTransform(transform, param);
        }

        /// <summary>
        /// handle widget event: apply to underlying shape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TransformShapeHandler(vtkObject sender, vtkObjectEventArgs e)
        {
            // we always need to update the saved transform when the user interacts with the shape
            dirtyFlags = RegionControl.DIRTY_ALL;

            vtkTransform transform = GetTransform();

            if (transform != null)
            {
                AdjustShape(transform);
                foreach (CallbackHandler callback in this.callbacks)
                {
                    callback(this, e.EventId == (uint)vtkCommand.EventIds.InteractionEvent);
                }
            }
        }

        private void AdjustShape(vtkTransform transform)
        {
            if (shapeActor.Prop != null)
            {
                ((vtkActor)shapeActor.Prop).SetUserTransform(transform);
            }
        }

        /// <summary>
        /// retrieve the transform in scale, rotation, translation format
        /// </summary>
        /// <param name="scale">3-dim vector for scale</param>
        /// <param name="rotation">4-dim vector for rotation</param>
        /// <param name="translation">3-dim vector for translation</param>
        /// <returns>true for success</returns>
        public bool GetScaleRotationTranslation(ref double[] scale, ref double[] rotation, ref double[] translation)
        {
            vtkTransform transform = GetTransform();

            if (transform == null)
            {
                return false;
            }

            scale       = transform.GetScale();
            rotation    = transform.GetOrientationWXYZ();
            translation = transform.GetPosition();
            return true;
        }

        /// <summary>
        /// set the widget transform when given scale, rotation, translation
        /// </summary>
        /// <param name="scale">3-dim vector for scale</param>
        /// <param name="rotation">4-dim vector for rotation</param>
        /// <param name="translation">3-dim vector for translation</param>
        /// <param name="param">can specify to apply the scale correction</param>
        public void SetScaleRotationTranslation(double[] scale, double[] rotation, double[] translation, byte param)
        {
            if (boxWidget == null)
            {
                return;
            }

            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            transform.Translate(translation[0], translation[1], translation[2]);
            transform.RotateWXYZ(rotation[0], rotation[1], rotation[2], rotation[3]);
            transform.Scale(scale[0], scale[1], scale[2]);
            SetTransform(transform, param);
        }

        /// <summary>
        /// set visibility state of the widget
        /// </summary>
        /// <param name="show">true (default) for visible</param>
        public void ShowWidget(bool show = true)
        {
            if (boxWidget == null)
            {
                return;
            }

            if (show == true)
            {
                boxWidget.On();
            }
            else
            {
                boxWidget.Off();
            }
        }

        /// <summary>
        /// set the visibility state of the contained shape
        /// </summary>
        /// <param name="rw">the VTK render window handling the display</param>
        /// <param name="show">true (default) for visible</param>
        public void ShowActor(vtkRenderWindow rw, bool show = true)
        {
            if (boxWidget == null)
            {
                return;
            }

            if (show == true)
            {
                // add the actor to the renderer
                shapeActor.addToScene(true);
            }
            else
            {
                // remove the actor from the renderer
                shapeActor.addToScene(false);
            }
        }

        /// <summary>
        /// set the visibility state of the handles and the contained shape
        /// </summary>
        /// <param name="rw">the VTK render window handling the display</param>
        /// <param name="show">true (default) for visible</param>
        public void Show(vtkRenderWindow rw, bool show = true)
        {
            ShowActor(rw, show);
            ShowWidget(show);
        }

        /// <summary>
        /// indicates the widget's visibility state
        /// </summary>
        /// <returns>true for visible</returns>
        public bool Showing()
        {
            if (boxWidget == null)
            {
                return false;
            }

            return boxWidget.GetEnabled() > 0;
        }

        /// <summary>
        /// remove all vtk-related objects
        /// </summary>
        public void CleanUp()
        {
            if (boxWidget != null)
            {
                // boxWidget.StartInteractionEvt -= new vtkObject.vtkObjectEventHandler(TransformShapeHandler);
                boxWidget.InteractionEvt -= new vtkObject.vtkObjectEventHandler(TransformShapeHandler);
                ClearCallbacks();
                boxWidget.Dispose();
                boxWidget = null;
            }

            if (shapeActor.Prop != null)
            {
                ((vtkActor)shapeActor.Prop).Dispose();
                shapeActor.Prop = null;
            }

            if (savedTransform != null)
            {
                savedTransform.Dispose();
                savedTransform = null;
            }
        }

        /// <summary>
        /// set the shape actor's color
        /// </summary>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        public void SetColor(double r, double g, double b)
        {
            red = r;
            green = g;
            blue = b;
            if (shapeActor.Prop != null)
            {
                ((vtkActor)shapeActor.Prop).GetProperty().SetColor(red, green, blue);
            }
        }

        /// <summary>
        /// set the region's opacity
        /// </summary>
        /// <param name="o">opacity</param>
        public void SetOpacity(double o)
        {
            opacity = o;
            if (shapeActor.Prop != null)
            {
                ((vtkActor)shapeActor.Prop).GetProperty().SetOpacity(opacity);
            }
        }

        private void SetCube()
        {
            vtkCubeSource cube = vtkCubeSource.New();

            cube.SetBounds(-RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE,
                           -RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE,
                           -RegionControl.UNIT_SHAPE, RegionControl.UNIT_SHAPE);

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(cube.GetOutputPort());

            // Link the data pipeline to the rendering subsystem
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetOpacity(opacity);
            actor.GetProperty().SetColor(red, green, blue);
            shapeActor.Prop = actor;
        }

        ////Add this line after 2/4/14
        ////private void SetSphere(bool draw_as_wireframe = false)
        private void SetSphere()
        {
            vtkSphereSource sphere = vtkSphereSource.New();
            sphere.SetRadius(RegionControl.UNIT_SHAPE);
            sphere.SetThetaResolution(16);
            sphere.SetPhiResolution(16);

            vtkShrinkPolyData shrink = vtkShrinkPolyData.New();
            shrink.SetInputConnection(sphere.GetOutputPort());
            shrink.SetShrinkFactor(1.0);

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(shrink.GetOutputPort());

            // Link the data pipeline to the rendering subsystem
            vtkActor actor = vtkActor.New();            
            actor.SetMapper(mapper);
            actor.GetProperty().SetOpacity(opacity);
            actor.GetProperty().SetColor(red, green, blue);

            ////Add this after 2/4/14
            ////skg new - this sets the blob to display a wireframe - it should do so only for cell populations, not mol pops
            ////if (draw_as_wireframe)
            ////{
            ////    actor.GetProperty().SetRepresentationToWireframe();
            ////    actor.GetProperty().SetOpacity(0);
            ////}

            shapeActor.Prop = actor;
        }

    }
}
