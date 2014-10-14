//#define ALL_REGIONS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kitware.VTK;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RandomSources;

using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// entity to implement a region control
    /// </summary>
    public class RegionControl
    {
        private vtkTransform savedTransform;
        // the box axes, local to absolute matrix, absolute to local matrix
        private double[,] savedAxes, savedLTA, savedATL;
        private byte dirtyFlags;
        private RegionShape shape;
        private double[] extBounds = null;
        public const double UNIT_SHAPE = 0.25, SCALE_CORRECTION = 0.5 / UNIT_SHAPE;

        /// <summary>
        /// parameter bit flags for transform access
        /// PARAM_DEEP_COPY causes GetTransform to return a copy, not a handle to the original transform
        /// PARAM_SCALE applies for transform transfers from box to widget and vice versa
        /// </summary>
        public static byte PARAM_DEEP_COPY = (1 << 0),
                           PARAM_SCALE     = (1 << 1);

        // dirty flags for reference frame recalculation - once after it changed
        public static byte DIRTY_TRANSFORM = (1 << 0),
                           DIRTY_AXES      = (1 << 1),
                           DIRTY_LTA       = (1 << 2),
                           DIRTY_ATL       = (1 << 3),
                           DIRTY_ALL       = (byte)(DIRTY_TRANSFORM | DIRTY_AXES | DIRTY_LTA | DIRTY_ATL);

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="rw">VTK render window</param>
        /// <param name="shape">region shape</param>
        public RegionControl(RegionShape shape = RegionShape.Rectangular)
        {
            dirtyFlags = DIRTY_ALL;
            savedTransform = vtkTransform.New();
            savedAxes = new double[3, 3];
            savedLTA = new double[3, 4];
            savedATL = new double[3, 4];

            if ((shape != RegionShape.Rectangular) && (shape != RegionShape.Ellipsoid))
            {
                // error
                CleanUp();
                return;
            }
            this.shape = shape;
        }

        /// <summary>
        /// set the region shape
        /// </summary>
        /// <param name="rw">VTK render window object</param>
        /// <param name="shape">value indicating the shape</param>
        public void SetShape(RegionShape shape)
        {
            if (this.shape == shape)
            {
                return;
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
        /// return the widget's transform
        /// </summary>
        /// <param name="param">specify special handling instructions (deep or scale); only use outside of this class</param>
        /// <returns>pointer to the transform</returns>
        public vtkTransform GetTransform(byte param = 0)
        {
            // NOTE: In the old version there was a re-grabbing here of savedTransform
            // if ((dirtyFlags & DIRTY_TRANSFORM) != 0)

            // if the caller will modify the transform or a scale correction is needed then make a deep copy
            if ((param & PARAM_DEEP_COPY) != 0 || (param & PARAM_SCALE) != 0)
            {
                vtkTransform t = vtkTransform.New();

                t.DeepCopy(savedTransform);
                if ((param & PARAM_SCALE) != 0)
                {
                    t.Scale(1.0 / SCALE_CORRECTION, 1.0 / SCALE_CORRECTION, 1.0 / SCALE_CORRECTION);
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
            if ((param & PARAM_SCALE) != 0)
            {
                transform.Scale(SCALE_CORRECTION, SCALE_CORRECTION, SCALE_CORRECTION);
            }
            // NOTE: Making deep copy for now...
            savedTransform.DeepCopy(transform);
            // TODO: Not sure if we need these dirty flags in RegionControl...
            dirtyFlags = DIRTY_ALL;
        }

        /// <summary>
        /// sets the transform from a matrix
        /// </summary>
        /// <param name="matrix">the matrix as an array, row by row</param>
        /// <param name="param">can specify to apply the scale correction</param>
        public void SetTransform(double[][] matrix, byte param = 0)
        {
            if (matrix.Length != 4 || matrix[0].Length != 4 || matrix[1].Length != 4 || matrix[2].Length != 4 || matrix[3].Length != 4)
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
        /// retrieve the transform in scale, rotation, translation format
        /// </summary>
        /// <param name="scale">3-dim vector for scale</param>
        /// <param name="rotation">4-dim vector for rotation</param>
        /// <param name="translation">3-dim vector for translation</param>
        /// <returns>true for success</returns>
        public bool GetScaleRotationTranslation(ref double[] scale, ref double[] rotation, ref double[] translation)
        {
            vtkTransform transform = GetTransform();

            if(transform == null)
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
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            transform.Scale(scale[0], scale[1], scale[2]);
            transform.RotateWXYZ(rotation[0], rotation[1], rotation[2], rotation[3]);
            transform.Translate(translation[0], translation[1], translation[2]);
            SetTransform(transform, param);
        }

        /// <summary>
        /// remove all vtk-related objects
        /// </summary>
        public void CleanUp()
        {
            if (savedTransform != null)
            {
                savedTransform.Dispose();
                savedTransform = null;
            }
            savedAxes = null;
            savedLTA  = null;
            savedATL  = null;
        }

        /// <summary>
        /// test a local point for containment
        /// </summary>
        /// <param name="x">local x coordinate</param>
        /// <param name="y">local y coordinate</param>
        /// <param name="z">local z coordinate</param>
        /// <returns>true for containment</returns>
        public bool LocalPointIsInside(double x, double y, double z)
        {
            vtkTransform transform = GetTransform();

            if (transform == null)
            {
                return false;
            }

            double[] scale = transform.GetScale();

            // empty box can't contain anything
            if (scale[0] == 0 || scale[1] == 0 || scale[2] == 0)
            {
                return false;
            }

            // test the outside box first for quick filtering
            if (x < -UNIT_SHAPE || x > UNIT_SHAPE ||
                y < -UNIT_SHAPE || y > UNIT_SHAPE ||
                z < -UNIT_SHAPE || z > UNIT_SHAPE)
            {
                return false;
            }
            // if this is a box shape then we are done
            if (shape == RegionShape.Rectangular)
            {
                return true;
            }

            if (shape == RegionShape.Ellipsoid)
            {
                if (x * x + y * y + z * z > UNIT_SHAPE * UNIT_SHAPE)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// sets the bounds as dictated by some exterior constraint; assumes the exterior constraint is an axis-aligned box
        /// </summary>
        /// <param name="bounds">bounds in -x, x, -y, y, -z, z  format</param>
        public void SetExteriorBounds(double[] bounds)
        {
            if (bounds == null || bounds.Length != 6)
            {
                return;
            }

            extBounds = new double[6];
            for (int i = 0; i < 6; i++)
            {
                extBounds[i] = bounds[i];
            }
        }

        /// <summary>
        /// for debugging: draw outline around exterior bounds
        /// </summary>
        public void DrawExteriorBounds()
        {
            vtkCubeSource cube = vtkCubeSource.New();
            cube.SetBounds(extBounds[0], extBounds[1], extBounds[2], extBounds[3], extBounds[4], extBounds[5]);
            //cube.SetCenter(x, y, z);

            vtkOutlineFilter outlineFilter = vtkOutlineFilter.New();
            outlineFilter.SetInput(cube.GetOutput());

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(outlineFilter.GetOutputPort());

            // Link the data pipeline to the rendering subsystem
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(0.5, 0.5, 1.0);
            ((VTKFullGraphicsController)MainWindow.GC).Rwc.RenderWindow.GetRenderers().GetFirstRenderer().AddViewProp(actor);
        }

        /// <summary>
        /// for debugging: draw a point set
        /// </summary>
        /// <param name="source">the point source</param>
        private void GlyphOutputSource(vtkAlgorithmOutput source)
        {
            vtkSphereSource sph = vtkSphereSource.New();
            sph.SetThetaResolution(8);
            sph.SetPhiResolution(8);
            sph.SetRadius(0.01);

            vtkGlyph3D glyp = vtkGlyph3D.New();
            glyp.SetSourceConnection(sph.GetOutputPort(0));
            glyp.SetInputConnection(source);

            glyp.ScalingOff();
            glyp.OrientOff();

            vtkAppendPolyData appendPoly = vtkAppendPolyData.New();
            appendPoly.AddInputConnection(glyp.GetOutputPort(0));

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(appendPoly.GetOutputPort());

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            //actor.GetProperty().SetRepresentationToWireframe();
            actor.GetProperty().SetRepresentationToSurface();
            // TODO: This should not be dependent on any given render window control...
            ((VTKFullGraphicsController)MainWindow.GC).Rwc.RenderWindow.GetRenderers().GetFirstRenderer().AddViewProp(actor);
        }

        /// <summary>
        /// do an obb separation test
        /// </summary>
        /// <param name="e0">extents of box A</param>
        /// <param name="c0">center of box A</param>
        /// <param name="r0">frame of box A</param>
        /// <param name="e1">extents of box B</param>
        /// <param name="c1">center of box B</param>
        /// <param name="r1">frame of box B</param>
        /// <returns>true for separation</returns>
        private bool SeparatedGeneralBoxes(Vector e0, Vector c0, Matrix r0, Vector e1, Vector c1, Matrix r1)
        {
            Vector[] axis0 = new Vector[3];
            for (int i = 0; i < 3; i++)
            {
                axis0[i] = r0.GetColumnVector(i);
            }

            Vector[] axis1 = new Vector[3];
            for (int i = 0; i < 3; i++)
            {
                axis1[i] = r1.GetColumnVector(i);
            }

            // translation, in parent frame
            Vector v = c1 - c0;
            // translation, in A's frame
            Vector T = new Vector(new double[] { v.ScalarMultiply(axis0[0]), v.ScalarMultiply(axis0[1]), v.ScalarMultiply(axis0[2]) });

            // B's basis with respect to A's local frame
            // expressing B's basis this way will simplify the following tests such that comparisons can be done per component,
            // and decisions can be made as soon as one component violates the criterion
            double[,] Ba = new double[3, 3];
            double[,] absBa = new double[3, 3];
            double ra, rb, TL;

            // calculate rotation matrix
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Ba[i, k] = axis0[i].ScalarMultiply(axis1[k]);
                    // maintain a matrix with the absolute values in it as they are employed in the simplification mentioned in Gottschalk's paper
                    absBa[i, k] = Math.Abs(Ba[i, k]);
                }
            }

            // test A's basis vectors, L = A_j
            // rb = sum(|e1_i * B_i * A_j|) =
            //      sum(|e1_i * Ba_ji|) =
            //      sum(e1_i * |Ba_ji|) =
            //      sum(e1_i * absBa_ji) =
            //      e1_0 * absBa_j0 + e1_1 * absBa_j0 + e1_2 absBa_j2
            for (int i = 0; i < 3; i++)
            {
                // ra in A's own frame
                ra = e0[i];
                // rb with respect to A
                rb = e1[0] * absBa[i, 0] + e1[1] * absBa[i, 1] + e1[2] * absBa[i, 2];
                // TL = T * u, where u are the unit vectors of an orthonormal, axis-aligned basis in world coordinates, i.e. (1 0 0), (0 1 0), (0 0 1)
                TL = Math.Abs(T[i]);
                if (TL > ra + rb)
                {
                    return true;
                }
            }

            // test B's basis vectors
            for (int i = 0; i < 3; i++)
            {
                // ra with respect to B, use absBa's inverse to get from A to B
                // NOTE: M_inv = 1 / det(M) * M_transposed with det(M) == 1 for a rotation matrix, i.e. M_inv == M_transposed
                ra = e0[0] * absBa[0, i] + e0[1] * absBa[1, i] + e0[2] * absBa[2, i];
                // rb in B's own frame
                rb = e1[i];
                // T expressed in B, use Ba's inverse similar to above
                // TL = T * Ba_inv_i
                TL = Math.Abs(T[0] * Ba[0, i] + T[1] * Ba[1, i] + T[2] * Ba[2, i]);
                if (TL > ra + rb)
                {
                    return true;
                }
            }

            // test the 9 cross products of the pairwise box axes

            // a0 x b0
            // ra = sum(|e0_i * A_i * (A_0 x B_0)|) =
            //      sum(|e0_i * B_0 * (A_i x A_0)|) =
            // note that A_i x A_i == 0
            //      |e0_1 * B_0 * (A_1 x A_0)| + |e0_2 * B_0 * (A_2 x A_0)| =
            //      |e0_1 * B_0 * A_2| + |e0_2 * B_0 * A_1| =
            //      |e0_1 * Ba_20| + |e0_2 * Ba_10| =
            //      e0_1 * |Ba_20| + e0_2 * |Ba_10| = 
            //      e0_1 * absBa_20 + e0_2 * absBa_10
            // etc.
            ra = e0[1] * absBa[2, 0] + e0[2] * absBa[1, 0];
            rb = e1[1] * absBa[0, 2] + e1[2] * absBa[0, 1];
            // TL = T * L = T * (a0 x b0) = T * ((1 0 0) x (Ba_00 Ba_10 Ba_20)) = T * (0 -Ba_20 Ba_10) = -T_1 * Ba_20 + T_2 * Ba_10
            TL = Math.Abs(T[2] * Ba[1, 0] - T[1] * Ba[2, 0]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a0 x b1
            ra = e0[1] * absBa[2, 1] + e0[2] * absBa[1, 1];
            rb = e1[0] * absBa[0, 2] + e1[2] * absBa[0, 0];
            TL = Math.Abs(T[2] * Ba[1, 1] - T[1] * Ba[2, 1]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a0 x b2
            ra = e0[1] * absBa[2, 2] + e0[2] * absBa[1, 2];
            rb = e1[0] * absBa[0, 1] + e1[1] * absBa[0, 0];
            TL = Math.Abs(T[2] * Ba[1, 2] - T[1] * Ba[2, 2]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a1 x b0
            ra = e0[0] * absBa[2, 0] + e0[2] * absBa[0, 0];
            rb = e1[1] * absBa[1, 2] + e1[2] * absBa[1, 1];
            TL = Math.Abs(T[0] * Ba[2, 0] - T[2] * Ba[0, 0]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a1 x b1
            ra = e0[0] * absBa[2, 1] + e0[2] * absBa[0, 1];
            rb = e1[0] * absBa[1, 2] + e1[2] * absBa[1, 0];
            TL = Math.Abs(T[0] * Ba[2, 1] - T[2] * Ba[0, 1]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a1 x b2
            ra = e0[0] * absBa[2, 2] + e0[2] * absBa[0, 2];
            rb = e1[0] * absBa[1, 1] + e1[1] * absBa[1, 0];
            TL = Math.Abs(T[0] * Ba[2, 2] - T[2] * Ba[0, 2]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a2 x b0
            ra = e0[0] * absBa[1, 0] + e0[1] * absBa[0, 0];
            rb = e1[1] * absBa[2, 2] + e1[2] * absBa[2, 1];
            TL = Math.Abs(T[1] * Ba[0, 0] - T[0] * Ba[1, 0]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a2 x b1
            ra = e0[0] * absBa[1, 1] + e0[1] * absBa[0, 1];
            rb = e1[0] * absBa[2, 2] + e1[2] * absBa[2, 0];
            TL = Math.Abs(T[1] * Ba[0, 1] - T[0] * Ba[1, 1]);
            if (TL > ra + rb)
            {
                return true;
            }

            // a2 x b2
            ra = e0[0] * absBa[1, 2] + e0[1] * absBa[0, 2];
            rb = e1[0] * absBa[2, 1] + e1[1] * absBa[2, 0];
            TL = Math.Abs(T[1] * Ba[0, 2] - T[0] * Ba[1, 2]);
            if (TL > ra + rb)
            {
                return true;
            }

            // if we reach this point, i.e. no test was triggered, we know that the boxes are overlapping (not separated)
            return false;
        }
#if BOX_FEASIBILITY_TEST
        /// <summary>
        /// test the feasibility for box-box using the OBB test for testing 'inside'
        /// </summary>
        /// <param name="location">indicates the location where we want to create the points, i.e. the region of interest</param>
        /// <returns>true for non-empty</returns>
        private bool BoxBoxFeasibilityTest(RelativePosition location)
        {
            if (location == RelativePosition.Outside)
            {
                // as long as at least one environment vertex lies outside the region the set is non-empty
                // we have the env. bounds as an array -x, x, -y, y, -z, z; assemble the vertices and test them
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 2; y <= 3; y++)
                    {
                        for (int z = 4; z <= 5; z++)
                        {
                            // vertex in local coordinates
                            double[] point = AbsoluteToLocal(extBounds[x], extBounds[y], extBounds[z]);

                            // as soon as we find one vertex that is not contained in the region we know the set is non-empty
                            if (LocalPointIsInside(point[0], point[1], point[2]) == false)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            // treat surface and inside the same, find overlap with the OBB test
            else
            {
                Matrix regAxes = Matrix.Create(GetAxes()),
                       extAxes = Matrix.Create(new double[,] { { 1.0, 0.0, 0.0 },
                                                               { 0.0, 1.0, 0.0 },
                                                               { 0.0, 0.0, 1.0 } });
                Vector regCenter = GetPosition(),
                       extCenter = new double[] { (extBounds[0] + extBounds[1]) / 2.0,
                                                  (extBounds[2] + extBounds[3]) / 2.0,
                                                  (extBounds[4] + extBounds[5]) / 2.0 },
                       regExtent = GetExtents(),
                       extExtent = new double[] { (extBounds[1] - extBounds[0]) / 2.0,
                                                  (extBounds[3] - extBounds[2]) / 2.0,
                                                  (extBounds[5] - extBounds[4]) / 2.0 };

                // check for overlap
                if (SeparatedGeneralBoxes(extExtent, extCenter, extAxes, regExtent, regCenter, regAxes) == true)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// test the ellipsoid's center (sphere at (0, 0, 0) since we transformed the box to local widget coordinates) against the edge AB
        /// and indicate whether the edge intersects the sphere
        /// </summary>
        /// <param name="a">first endpoint of the edge</param>
        /// <param name="b">second endpoint of the edge</param>
        /// <returns>true for intersection</returns>
        private bool testEdge(Vector a, Vector b)
        {
            Vector n = (b - a).Normalize();
            double len = n.ScalarMultiply(-a);

            // does the sphere's center project onto the edge?
            if (len >= 0 && len <= (b - a).Norm())
            {
                // does the edge intersect the sphere?
                if ((a + n * len).Norm() < UNIT_SHAPE)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// test the face spanned by edges AB and AC with AB x AC giving the outside pointing normal
        /// </summary>
        /// <param name="a">vertex a</param>
        /// <param name="b">vertex b</param>
        /// <param name="c">vertex c</param>
        /// <returns>true if the face intersects the sphere, false otherwise</returns>
        private bool testFace(Vector a, Vector b, Vector c)
        {
            Vector ab = b - a,
                   ac = c - a,
                   n = ab.CrossMultiply(ac).Normalize();
            Matrix m = new Matrix(3, 3),
                   rhs = new Matrix(3, 1),
                   sol = new Matrix(3, 1);

            // solve a linear system Ax = -a: here A = (n, ab, ac)
            // this finds whether the sphere's center can be expressed through viable combinations of the vectors n, ab, ac
            // the matrix
            m.SetColumnVector(n, 0);
            m.SetColumnVector(ab, 1);
            m.SetColumnVector(ac, 2);
            // right hand side
            rhs.SetColumnVector(-a, 0);
            // solve
            sol = m.Solve(rhs);

            // does the sphere's center project onto the face?
            if (sol[1, 0] >= 0 && sol[1, 0] <= 1 && sol[2, 0] >= 0 && sol[2, 0] <= 1)
            {
                // does the face intersect the sphere?
                if (sol[0, 0] < UNIT_SHAPE)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// test the feasibility for box-ellipsoid
        /// </summary>
        /// <param name="location">indicates the location where we want to create the points, i.e. the region of interest</param>
        /// <returns>true for non-empty</returns>
        private bool BoxEllipsoidFeasibilityTest(RelativePosition location)
        {
            Vector[] vertex = new Vector[8];
            bool[] visible = new bool[9];
            int i;

            // vertex numbering and orientation
            //    +2-----6+
            //   /|      /|
            //  / |     / |
            // +3-----7+  |
            // |  +0---|-4+
            // | /     | /
            // |/      |/
            // +1-----5+

            // check face visibility
            Vector eCenter = GetPosition(), normal;

            // right
            normal = new double[] { 1, 0, 0 };
            if (normal * eCenter > extBounds[1])
            {
                visible[4] = visible[5] = visible[6] = visible[7] = visible[8] = true;
            }

            // left
            normal[0] *= -1;
            if (normal * eCenter > extBounds[0])
            {
                visible[0] = visible[1] = visible[2] = visible[3] = visible[8] = true;
            }

            // up
            normal[0] = 0; normal[1] = 1; normal[2] = 0;
            if (normal * eCenter > extBounds[3])
            {
                visible[2] = visible[3] = visible[6] = visible[7] = visible[8] = true;
            }

            // down
            normal[1] *= -1;
            if (normal * eCenter > extBounds[2])
            {
                visible[0] = visible[1] = visible[4] = visible[5] = visible[8] = true;
            }

            // forward
            normal[0] = 0; normal[1] = 0; normal[2] = 1;
            if (normal * eCenter > extBounds[5])
            {
                visible[1] = visible[3] = visible[5] = visible[7] = visible[8] = true;
            }

            // back
            normal[2] *= -1;
            if (normal * eCenter > extBounds[4])
            {
                visible[0] = visible[2] = visible[4] = visible[6] = visible[8] = true;
            }

            // if we are not testing for outside and the ellipsoid center is inside the box we know the set is non-empty
            if (location != RelativePosition.Outside && visible[8] == false)
            {
                return true;
            }

            // generate the box vertices in local widget coordinates
            i = 0;
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 2; y <= 3; y++)
                {
                    for (int z = 4; z <= 5; z++)
                    {
                        // vertices in local coordinates; this simplifies the tests because the region is always a sphere with center at the origin
                        vertex[i] = AbsoluteToLocal(extBounds[x], extBounds[y], extBounds[z]);

                        // if we are testing for outside, then as soon as we find one vertex that is not contained in the region we know the set is non-empty
                        if (location == RelativePosition.Outside && LocalPointIsInside(vertex[i][0], vertex[i][1], vertex[i][2]) == false)
                        {
                            return true;
                        }
                        i++;
                    }
                }
            }

            // if we reach here and the test is for outside we know the set is infeasible
            if (location == RelativePosition.Outside)
            {
                return false;
            }
            else
            {
                // test vertices
                for (i = 0; i < 8; i++)
                {
                    // a vertes lies inside the sphere
                    if (visible[i] == true && vertex[i].Norm() < UNIT_SHAPE)
                    {
                        return true;
                    }
                }

                // test edges
                if (visible[0] == true && visible[1] == true && testEdge(vertex[0], vertex[1]) ||
                    visible[1] == true && visible[3] == true && testEdge(vertex[1], vertex[3]) ||
                    visible[3] == true && visible[2] == true && testEdge(vertex[3], vertex[2]) ||
                    visible[2] == true && visible[0] == true && testEdge(vertex[2], vertex[0]) ||
                    visible[0] == true && visible[4] == true && testEdge(vertex[0], vertex[4]) ||
                    visible[2] == true && visible[6] == true && testEdge(vertex[2], vertex[6]) ||
                    visible[3] == true && visible[7] == true && testEdge(vertex[3], vertex[7]) ||
                    visible[1] == true && visible[5] == true && testEdge(vertex[1], vertex[5]) ||
                    visible[4] == true && visible[5] == true && testEdge(vertex[4], vertex[5]) ||
                    visible[5] == true && visible[7] == true && testEdge(vertex[5], vertex[7]) ||
                    visible[7] == true && visible[6] == true && testEdge(vertex[7], vertex[6]) ||
                    visible[6] == true && visible[4] == true && testEdge(vertex[6], vertex[4]))
                {
                    return true;
                }

                // test faces
                if (visible[0] == true && visible[1] == true && visible[2] == true && testFace(vertex[0], vertex[1], vertex[2]) ||
                    visible[4] == true && visible[6] == true && visible[5] == true && testFace(vertex[4], vertex[6], vertex[5]) ||
                    visible[6] == true && visible[2] == true && visible[7] == true && testFace(vertex[6], vertex[2], vertex[7]) ||
                    visible[5] == true && visible[7] == true && visible[1] == true && testFace(vertex[5], vertex[7], vertex[1]) ||
                    visible[0] == true && visible[2] == true && visible[4] == true && testFace(vertex[0], vertex[2], vertex[4]) ||
                    visible[4] == true && visible[5] == true && visible[0] == true && testFace(vertex[4], vertex[5], vertex[0]))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// looks at the exterior bounds and determines if the desired point set is feasible (non-empty)
        /// </summary>
        /// <param name="location">indicates the location where we want to create the points, i.e. the region of interest</param>
        /// <returns>true for non-empty</returns>
        public bool IsFeasible(RelativePosition location)
        {
            if (extBounds == null)
            {
                return false;
            }

            if (shape == RegionShape.Ellipsoid)
            {
                return BoxEllipsoidFeasibilityTest(location);
            }
            else if (shape == RegionShape.Rectangular)
            {
                return BoxBoxFeasibilityTest(location);
            }
            else
            {
                return false;
            }
        }
#endif
#if ALL_REGIONS
        /// <summary>
        /// generate a random point inside, outside, or on the surface of the region in local coordinates
        /// </summary>
        /// <param name="location">specify where we want our point to be generated</param>
        /// <returns>a local 3d point</returns>
        public double[] GenerateLocalPoint(RelativePosition location)
        {
            if (extBounds == null)
            {
                return null;
            }

            double[] pos = new double[3],
                     intBounds = new double[6];

            // set the bounds
            for (int i = 0; i < 6; i++)
            {
                // a minimum?
                if (i % 2 == 0)
                {
                    intBounds[i] = -UNIT_SHAPE;
                }
                else
                {
                    intBounds[i] = UNIT_SHAPE;
                }
            }

            // handle the box
            if (shape == RegionShape.Rectangular)
            {
                if (location == RelativePosition.Inside)
                {
                    // x
                    pos[0] = Utilities.SystemRandom.NextDouble(intBounds[0], intBounds[1]);
                    // y
                    pos[1] = Utilities.SystemRandom.NextDouble(intBounds[2], intBounds[3]);
                    // z
                    pos[2] = Utilities.SystemRandom.NextDouble(intBounds[4], intBounds[5]);
                }
                else
                {
                    // pick a random face on which the point lies
                    int coordinate = Utilities.SystemRandom.Next(0, 6);

                    if (location == RelativePosition.Surface)
                    {
                        // coordinate in the direction of the picked face is on the face
                        pos[coordinate / 2] = intBounds[coordinate];
                        coordinate /= 2;
                        // the other two can be anywhere within the interior extrema
                        for (int i = 0; i < 2; i++)
                        {
                            coordinate++;
                            coordinate %= 3;
                            pos[coordinate] = Utilities.SystemRandom.NextDouble(intBounds[coordinate * 2], intBounds[coordinate * 2 + 1]);
                        }
                    }
                    else if (location == RelativePosition.Outside)
                    {
                        // generate a point inside the exterior bounds (absolute coordinates)
                        double[] absP = { 0, 0, 0 };

                        for (int i = 0; i < 3; i++)
                        {
                            absP[i] = Utilities.SystemRandom.NextDouble(extBounds[i * 2], extBounds[i * 2 + 1]);
                        }

                        // transform to local, test if within local bounds, and if so, discard
                        pos = AbsoluteToLocal(absP[0], absP[1], absP[2]);

                        if (intBounds[0] <= pos[0] && pos[0] <= intBounds[1] &&
                            intBounds[2] <= pos[1] && pos[1] <= intBounds[3] &&
                            intBounds[4] <= pos[2] && pos[2] <= intBounds[5])
                        {
                            return null;
                        }
                    }
                }
            }
            else if (shape == RegionShape.Ellipsoid)
            {
                // random direction
                double[] dir = Utilities.RandomDirection(3);

                if (dir == null)
                {
                    return null;
                }

                if (location == RelativePosition.Surface)
                {
                    // point on the surface
                    for (int i = 0; i < 3; i++)
                    {
                        pos[i] = dir[i] * UNIT_SHAPE;
                    }
                }
                else if (location == RelativePosition.Inside)
                {
                    // inverse of cumulative distribution for uniform point spread
                    double r = UNIT_SHAPE * Math.Pow(Utilities.SystemRandom.NextDouble(), 1.0 / 3.0);

                    for (int i = 0; i < 3; i++)
                    {
                        pos[i] = dir[i] * r;
                    }
                }
                else if (location == RelativePosition.Outside)
                {
                    // generate a point inside the exterior bounds (absolute coordinates)
                    double[] absP = { 0, 0, 0 };

                    for (int i = 0; i < 3; i++)
                    {
                        absP[i] = Utilities.SystemRandom.NextDouble(extBounds[i * 2], extBounds[i * 2 + 1]);
                    }

                    // transform to local, test if within local bounds, and if so, discard
                    pos = AbsoluteToLocal(absP[0], absP[1], absP[2]);

                    if (Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]) <= UNIT_SHAPE)
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }

            // check exterior bounds and discard point if out of bounds
            // no need to check if we generate a point outside, as in this case the exterior bounds are known to be met
            if (location != RelativePosition.Outside)
            {
                double[] absP = LocalToAbsolute(pos[0], pos[1], pos[2]);

                for (int i = 0; i < 3; i++)
                {
                    if (absP[i] < extBounds[2 * i] || absP[i] > extBounds[2 * i + 1])
                    {
                        return null;
                    }
                }
            }
            return pos;
        }
#endif
        /// <summary>
        /// retrieve the box axes as the columns of a matrix
        /// </summary>
        /// <returns>axes in the columns of a 3x3 matrix</returns>
        public double[,] GetAxes()
        {
            // only update the axes once
            if ((dirtyFlags & DIRTY_AXES) != 0)
            {
                vtkTransform transform = GetTransform(PARAM_DEEP_COPY);

                if (transform == null)
                {
                    return null;
                }

                double[] scale = transform.GetScale();
                vtkMatrix4x4 mat = vtkMatrix4x4.New();

                // prevent division by zero
                for (int i = 0; i < 3; i++)
                {
                    if (scale[i] == 0)
                    {
                        scale[i] = 1;
                    }
                }
                // undo the scaling
                transform.Scale(1.0 / scale[0], 1.0 / scale[1], 1.0 / scale[2]);

                transform.GetMatrix(mat);

                // initialize the axes arrays
                for (int i = 0; i < 3; i++)
                {
                    savedAxes[i, 0] = mat.GetElement(i, 0);
                    savedAxes[i, 1] = mat.GetElement(i, 1);
                    savedAxes[i, 2] = mat.GetElement(i, 2);
                }
                dirtyFlags &= (byte)(~DIRTY_AXES);
            }
            return savedAxes;
        }

        /// <summary>
        /// convert a local point to absolute coordinates
        /// </summary>
        /// <param name="x">local x coordinate</param>
        /// <param name="y">local y coordinate</param>
        /// <param name="z">local z coordinate</param>
        /// <returns>absolute 3d point</returns>
        public double[] LocalToAbsolute(double x, double y, double z)
        {
            // only update the matrix once
            if ((dirtyFlags & DIRTY_LTA) != 0)
            {
                vtkTransform transform = GetTransform();

                if (transform == null)
                {
                    return null;
                }

                vtkMatrix4x4 mat = vtkMatrix4x4.New();

                transform.GetMatrix(mat);

                // initialize the axes arrays
                for (int i = 0; i < 3; i++)
                {
                    savedLTA[i, 0] = mat.GetElement(i, 0);
                    savedLTA[i, 1] = mat.GetElement(i, 1);
                    savedLTA[i, 2] = mat.GetElement(i, 2);
                    savedLTA[i, 3] = mat.GetElement(i, 3);
                }
                dirtyFlags &= (byte)(~DIRTY_LTA);
            }

            double[] pos = new double[3];

            // calculate point
            for (int i = 0; i < 3; i++)
            {
                pos[i] = x * savedLTA[i, 0] +
                         y * savedLTA[i, 1] +
                         z * savedLTA[i, 2] +
                         savedLTA[i, 3];
            }
            return pos;
        }

        /// <summary>
        /// convert an absolute point to local coordinates
        /// </summary>
        /// <param name="x">absolute x coordinate</param>
        /// <param name="y">absolute y coordinate</param>
        /// <param name="z">absolute z coordinate</param>
        /// <returns>local 3d point</returns>
        public double[] AbsoluteToLocal(double x, double y, double z)
        {
            // only update the matrix once
            if ((dirtyFlags & DIRTY_ATL) != 0)
            {
                vtkTransform transform = GetTransform();

                if (transform == null)
                {
                    return null;
                }

                vtkMatrix4x4 mat = vtkMatrix4x4.New();
                transform.GetInverse(mat);

                // initialize the axes arrays
                for (int i = 0; i < 3; i++)
                {
                    savedATL[i, 0] = mat.GetElement(i, 0);
                    savedATL[i, 1] = mat.GetElement(i, 1);
                    savedATL[i, 2] = mat.GetElement(i, 2);
                    savedATL[i, 3] = mat.GetElement(i, 3);
                }
                dirtyFlags &= (byte)(~DIRTY_ATL);
            }

            double[] pos = new double[3];

            // calculate point
            for (int i = 0; i < 3; i++)
            {
                pos[i] = x * savedATL[i, 0] +
                         y * savedATL[i, 1] +
                         z * savedATL[i, 2] +
                         savedATL[i, 3];
            }
            return pos;
        }

        /// <summary>
        /// convert an absolute point to local coordinates but preserve the scale
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        /// <param name="z">the z coordinate</param>
        /// <returns>local 3d point</returns>
        public double[] AbsoluteToLocalPreserveScale(double x, double y, double z)
        {
            double[] pos = AbsoluteToLocal(x, y, z),
                     scale = GetTransform().GetScale();

            pos[0] *= scale[0];
            pos[1] *= scale[1];
            pos[2] *= scale[2];
            return pos;
        }

        /// <summary>
        /// align the widget and contained shape with the axes
        /// </summary>
        public void AxisAlign()
        {
            vtkTransform transform = GetTransform(PARAM_DEEP_COPY);

            if(transform == null)
            {
                return;
            }

            double[] pos = transform.GetPosition(),
                     scale = transform.GetScale();
            transform.Identity();
            transform.Translate(pos[0], pos[1], pos[2]);
            transform.Scale(scale[0], scale[1], scale[2]);
            SetTransform(transform);
        }

        /// <summary>
        /// sets the position of the widget and contained shape
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">z coordinate</param>
        public void SetPosition(double x, double y, double z)
        {
            vtkTransform transform = GetTransform(PARAM_DEEP_COPY);

            if (transform == null)
            {
                return;
            }

            double[] pos = transform.GetPosition();
            transform.Translate(x - pos[0], y - pos[1], z - pos[2]);
            SetTransform(transform);
        }

        /// <summary>
        /// returns the widget's position in absolute coordinates
        /// </summary>
        /// <returns>position as 3d vector</returns>
        public double[] GetPosition()
        {
            vtkTransform transform = GetTransform();

            if (transform == null)
            {
                return null;
            }

            return transform.GetPosition();
        }

        /// <summary>
        /// sets the half extents of the widget and contained shape
        /// </summary>
        /// <param name="x">x extent</param>
        /// <param name="y">y extent</param>
        /// <param name="z">z extent</param>
        public void SetExtents(double x, double y, double z)
        {
            vtkTransform transform = GetTransform(PARAM_DEEP_COPY);

            if (transform == null)
            {
                return;
            }

            double[] scale = transform.GetScale();
            if (scale[0] != 0)
            {
                scale[0] = x / (scale[0] * UNIT_SHAPE);
            }
            if (scale[1] != 0)
            {
                scale[1] = y / (scale[1] * UNIT_SHAPE);
            }
            if (scale[2] != 0)
            {
                scale[2] = z / (scale[2] * UNIT_SHAPE);
            }
            transform.Scale(scale[0], scale[1], scale[2]);
            SetTransform(transform);
        }

        /// <summary>
        /// retrieves the half extents of the widget
        /// </summary>
        /// <returns>the extents as a 3d vector</returns>
        public double[] GetExtents()
        {
            vtkTransform transform = GetTransform();

            if (transform == null)
            {
                return null;
            }

            double[] scale = transform.GetScale();

            scale[0] *= UNIT_SHAPE;
            scale[1] *= UNIT_SHAPE;
            scale[2] *= UNIT_SHAPE;
            return scale;
        }

    }
}
