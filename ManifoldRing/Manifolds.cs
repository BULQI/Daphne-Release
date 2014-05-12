using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra;

namespace ManifoldRing
{
    /// <summary>
    /// describes the position and orientation of an object;
    /// the rotation matrix denotes a coordinate frame (x, y, z); always 3d
    /// </summary>
    public class Transform
    {
        private Vector pos;
        private Matrix rot;
        /// <summary>
        /// true when rotation is present
        /// </summary>
        public bool HasRot { get; private set; }
        public static int Dim = 3;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="hasRot">true when the transform has rotation</param>
        public Transform(bool hasRot = true)
        {
            pos = new Vector(Dim);
            this.HasRot = hasRot;

            if (HasRot == true)
            {
                // set up the rotation to be aligned with the canonical world coordinates
                Vector tmp = new double[] { 1, 0, 0 };

                rot = new Matrix(Dim, Dim);

                // 1 0 0
                rot.SetColumnVector(tmp, 0);
                tmp[0] = 0;
                tmp[1] = 1;
                // 0 1 0
                rot.SetColumnVector(tmp, 1);
                tmp[1] = 0;
                tmp[2] = 1;
                // 0 0 1
                rot.SetColumnVector(tmp, 2);
            }
        }

        /// <summary>
        /// retrieve the translation component
        /// </summary>
        public Vector Translation
        {
            get { return pos; }
            set
            {
                if (pos.Length != value.Length)
                {
                    throw new Exception("Dimension mismatch.");
                }

                pos[0] = value[0]; pos[1] = value[1]; pos[2] = value[2];
            }
        }

        /// <summary>
        /// make the tranlation point to an external position vector; will stay in synch
        /// </summary>
        /// <param name="x">the position vector to synch with</param>
        public void setTranslationByReference(Vector x)
        {
            if (pos.Length != x.Length)
            {
                throw new Exception("Dimension mismatch.");
            }

            pos = x;
        }

        /// <summary>
        /// retrieve the rotation component
        /// </summary>
        public Matrix Rotation
        {
            get { return rot; }
            set
            {
                if (HasRot == true)
                {
                    if (rot.ColumnCount != value.ColumnCount || rot.RowCount != value.RowCount)
                    {
                        throw new Exception("Dimension mismatch.");
                    }

                    rot[0, 0] = value[0, 0]; rot[1, 0] = value[1, 0]; rot[2, 0] = value[2, 0];
                    rot[0, 1] = value[0, 1]; rot[1, 1] = value[1, 1]; rot[2, 1] = value[2, 1];
                    rot[0, 2] = value[0, 2]; rot[1, 2] = value[1, 2]; rot[2, 2] = value[2, 2];
                }
            }
        }

        /// <summary>
        /// make the rotation matrix point to an external rotation matrix; will stay in synch
        /// </summary>
        /// <param name="m">the rotation matrix to synch with</param>
        public void setRotationByReference(Matrix m)
        {
            if (HasRot == true)
            {
                if (rot.ColumnCount != m.ColumnCount || rot.RowCount != m.RowCount)
                {
                    throw new Exception("Dimension mismatch.");
                }

                rot = m;
            }
        }

        /// <summary>
        /// translate by x
        /// </summary>
        /// <param name="x">delta x</param>
        public void translate(Vector x)
        {
            if (pos.Length != x.Length)
            {
                throw new Exception("Dimension mismatch.");
            }

            pos += x;
        }

        /// <summary>
        /// rotate by rad about axis
        /// </summary>
        /// <param name="axis">rotation axis</param>
        /// <param name="rad">rotation angle in radians</param>
        public void rotate(Vector axis, double rad)
        {
            if (HasRot == true)
            {
                if (axis.Length != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new Matrix(Dim, Dim);

                // make sure the axis is normalized
                axis = axis.Normalize();
                // column 0
                tmp[0, 0] = Math.Cos(rad) + axis[0] * axis[0] * (1 - Math.Cos(rad));
                tmp[1, 0] = axis[1] * axis[0] * (1 - Math.Cos(rad)) + axis[2] * Math.Sin(rad);
                tmp[2, 0] = axis[2] * axis[0] * (1 - Math.Cos(rad)) - axis[1] * Math.Sin(rad);

                // column 1
                tmp[0, 1] = axis[0] * axis[1] * (1 - Math.Cos(rad)) - axis[2] * Math.Sin(rad);
                tmp[1, 1] = Math.Cos(rad) + axis[1] * axis[1] * (1 - Math.Cos(rad));
                tmp[2, 1] = axis[2] * axis[1] * (1 - Math.Cos(rad)) + axis[0] * Math.Sin(rad);

                // column 2
                tmp[0, 2] = axis[0] * axis[2] * (1 - Math.Cos(rad)) + axis[1] * Math.Sin(rad);
                tmp[1, 2] = axis[1] * axis[2] * (1 - Math.Cos(rad)) - axis[0] * Math.Sin(rad);
                tmp[2, 2] = Math.Cos(rad) + axis[2] * axis[2] * (1 - Math.Cos(rad));

                rot = rot.Multiply(tmp);
            }
        }

        /// <summary>
        /// convert the argument into the parent frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the parent frame</returns>
        public Vector toContaining(Vector x)
        {
            if (x.Length != Dim)
            {
                throw new Exception("Dimension mismatch.");
            }

            if (HasRot == true)
            {
                Matrix tmp = new Matrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp = rot.Multiply(tmp);

                tmp[0, 0] += pos[0];
                tmp[1, 0] += pos[1];
                tmp[2, 0] += pos[2];

                return tmp.GetColumnVector(0);
            }
            else
            {
                return x + pos;
            }
        }

        /// <summary>
        /// convert the argument into the local frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the local frame</returns>
        public Vector toLocal(Vector x)
        {
            if (HasRot == true)
            {
                if (x.Length != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new Matrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp[0, 0] -= pos[0];
                tmp[1, 0] -= pos[1];
                tmp[2, 0] -= pos[2];

                tmp = rot.Inverse().Multiply(tmp);

                return tmp.GetColumnVector(0);
            }
            else
            {
                return x - pos;
            }
        }
    }

    /// <summary>
    /// This puts the responsibility for scalar field multiplication in the manifold class.
    /// We may regard this as the specification of a scalar field handling class.
    /// </summary>
    public abstract class Manifold
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dim">manifold dimension</param>
        public Manifold(int dim)
        {
            Id = safeId++;
            Dim = dim;
        }

        /// <summary>
        /// manifold dimension
        /// </summary>
        public int Dim { get; private set; }
        /// <summary>
        /// unique id; serves as key
        /// </summary>
        public int Id { get; set; }
        private static int safeId = 0;
        /// <summary>
        /// the size of the generated underlying array
        /// </summary>
        public int ArraySize { get; set; }

        protected const double E_BOUNDARY_THICKNESS = 0.001;

        /// <summary>
        /// manifold extent
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>extent as double</returns>
        public abstract double Extent(int i);
        /// <summary>
        /// manifold stepsize
        /// </summary>
        /// <returns>stepsize as double</returns>
        public abstract double StepSize();
        /// <summary>
        /// nodes per side
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>number of nodes as int</returns>
        public abstract int NodesPerSide(int i);
        /// <summary>
        /// value at position, delegates to scalar field and interpolates if needed
        /// </summary>
        /// <param name="x">position</param>
        /// <param name="sf">underlying scalar field</param>
        /// <returns>value as double</returns>
        public abstract double Value(double[] x, ScalarField sf);
        /// <summary>
        /// checks if a point in local coordinates is on the manifold
        /// </summary>
        /// <param name="x">the point, assume to be in local coordinates</param>
        /// <returns>true if the point is on the manifold</returns>
        protected abstract bool localIsOn(double[] x);

        // operators

        /// <summary>
        /// scalarfield multiplication
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public abstract ScalarField Multiply(ScalarField sf1, ScalarField sf2);
        /// <summary>
        /// scalarfield division
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public abstract ScalarField Divide(ScalarField sf1, ScalarField sf2);
        /// <summary>
        /// Laplacian field
        /// </summary>
        /// <param name="sf">field operand</param>
        /// <returns>resulting field</returns>
        public abstract ScalarField Laplacian(ScalarField sf);
        /// <summary>
        /// gradient
        /// </summary>
        /// <param name="x">local position</param>
        /// <param name="sf">field operand</param>
        /// <returns>gradient vector</returns>
        public abstract double[] Grad(double[] x, ScalarField sf);
        /// <summary>
        /// field diffusion, flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public abstract ScalarField DiffusionFluxTerm(ScalarField flux);
        /// <summary>
        /// integrate over the whole field
        /// </summary>
        /// <param name="sf">field parameter</param>
        /// <returns>integral value</returns>
        public abstract double Integrate(ScalarField sf);
        /// <summary>
        /// manifold area
        /// </summary>
        /// <returns>area as double</returns>
        public abstract double Area();
        /// <summary>
        /// manifold volume
        /// </summary>
        /// <returns>volume as double</returns>
        public abstract double Volume();
        /// <summary>
        /// manifold voxel volume
        /// </summary>
        /// <returns>voxel volume as double</returns>
        public abstract double VoxelVolume();
    }

    /// <summary>
    /// base for the ME manifolds
    /// </summary>
    public abstract class MomentExpansionManifold : Manifold
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dim">dimension</param>
        public MomentExpansionManifold(int dim) : base(dim)
        {
            ArraySize = 4;
        }

        /// <summary>
        /// ME scalarfield multiplication
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Multiply(ScalarField sf1, ScalarField sf2)
        {
            ScalarField product = new ScalarField(this);

            product.array[0] = sf1.array[0] * sf2.array[0];
            for (int i = 1; i < ArraySize; i++)
            {
                product.array[i] = sf1.array[0] * sf2.array[i] + sf1.array[i] * sf2.array[0];
            }

            return product;
        }

        /// <summary>
        /// ME scalarfield division
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Divide(ScalarField sf1, ScalarField sf2)
        {
            if (sf2.array[0] == 0)
            {
                throw new Exception("Moment expansion division by zero.");
            }

            ScalarField c = new ScalarField(this);

            c.array[0] = sf1.array[0] / sf2.array[0];

            for (int i = 1; i < ArraySize; i++)
            {
                c.array[i] = (sf1.array[i] - sf1.array[0] * sf2.array[i] / sf2.array[0]) / sf2.array[0];
            }

            return c;
        }
    }

    /// <summary>
    /// tiny sphere manifold
    /// </summary>
    public class TinySphere : MomentExpansionManifold
    {
        private double radius;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="radius">sphere radius</param>
        public TinySphere(double radius) : base(2)
        {
            this.radius = radius;
        }

        /// <summary>
        /// TinySphere manifold extent
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>extent as double</returns>
        public override double Extent(int i)
        {
            if (i < 0 || i > 0)
            {
                throw new Exception("Extent out of range.");
            }

            return radius;
        }

        /// <summary>
        /// TinySphere manifold stepsize
        /// </summary>
        /// <returns>stepsize as double</returns>
        public override double StepSize()
        {
            return 0;
        }

        /// <summary>
        /// TinySphere nodes per side
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>number of nodes as int</returns>
        public override int NodesPerSide(int i)
        {
            return 0;
        }

        /// <summary>
        /// TinySphere manifold area
        /// </summary>
        /// <returns>area as double</returns>
        public override double Area()
        {
            return 4 * Math.PI * radius * radius;
        }

        /// <summary>
        /// TinySphere manifold volume
        /// </summary>
        /// <returns>volume as double</returns>
        public override double Volume()
        {
            return 0;
        }

        /// <summary>
        /// TinySphere manifold voxel volume
        /// </summary>
        /// <returns>voxel volume as double</returns>
        public override double VoxelVolume()
        {
            return 0;
        }

        /// <summary>
        /// TinySphere value at position, delegates to scalar field
        /// </summary>
        /// <param name="x">position</param>
        /// <param name="sf">underlying scalar field</param>
        /// <returns>value as double</returns>
        public override double Value(double[] x, ScalarField sf)
        {
            double value = sf.array[0],
                   norm = 0;

            for (int i = 0; i < 3; i++)
            {
                norm += x[i] * x[i];
            }
            // returns the mean value only for positions very close to the center of the sphere
            if (norm < 1e-6)
            {
                return value;
            }

            norm = 1.0 / Math.Sqrt(norm);
            for (int i = 1; i < 4; i++)
            {
                value += norm * x[i - 1] * sf.array[i];
            }

            return value;
        }

        /// <summary>
        /// checks if a point in local coordinates is on the manifold
        /// </summary>
        /// <param name="x">the point, assume to be in local coordinates</param>
        /// <returns>true if the point is on the manifold</returns>
        protected override bool localIsOn(double[] x)
        {
            double length = Math.Sqrt(x[0] * x[0] + x[1] * x[1] + x[2] * x[2]);

            return Math.Abs(length - radius) <= E_BOUNDARY_THICKNESS / 2.0;
        }

        /// <summary>
        /// TinySphere Laplacian field
        /// </summary>
        /// <param name="sf">field operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Laplacian(ScalarField sf)
        {
            ScalarField s = new ScalarField(this);

            s.array[0] = 0;
            s.array[1] = sf.array[1];
            s.array[2] = sf.array[2];
            s.array[3] = sf.array[3];
            s *= -2.0 / (radius * radius);
            return s;
        }

        /// <summary>
        /// TinySphere gradient
        /// </summary>
        /// <param name="x">local position</param>
        /// <param name="sf">field operand</param>
        /// <returns>gradient vector</returns>
        public override double[] Grad(double[] x, ScalarField sf)
        {
            Vector u = new Vector(x);

            u = u.Normalize();
            return new double[] { sf.array[1] - u[0] * u[0] * sf.array[1], sf.array[2] - u[1] * u[1] * sf.array[2], sf.array[3] - u[2] * u[2] * sf.array[3] };
        }

        /// <summary>
        /// TinySphere field diffusion, flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public override ScalarField DiffusionFluxTerm(ScalarField flux)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// TinySphere integrate over the whole field
        /// </summary>
        /// <param name="sf">field parameter</param>
        /// <returns>integral value</returns>
        public override double Integrate(ScalarField sf)
        {
            return sf.array[0] * 4 * Math.PI * radius * radius;
        }
    }

    /// <summary>
    /// tiny ball manifold
    /// </summary>
    public class TinyBall : MomentExpansionManifold
    {
        private double radius;
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="radius">ball radius</param>
        public TinyBall(double radius) : base(3)
        {
            this.radius = radius;
        }

        /// <summary>
        /// TinyBall manifold extent
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>extent as double</returns>
        public override double Extent(int i)
        {
            if (i < 0 || i > 0)
            {
                throw new Exception("Extent out of range.");
            }

            return radius;
        }

        /// <summary>
        /// TinyBall manifold stepsize
        /// </summary>
        /// <returns>stepsize as double</returns>
        public override double StepSize()
        {
            return 0;
        }

        /// <summary>
        /// TinyBall nodes per side
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>number of nodes as int</returns>
        public override int NodesPerSide(int i)
        {
            return 0;
        }

        /// <summary>
        /// TinyBall manifold area
        /// </summary>
        /// <returns>area as double</returns>
        public override double Area()
        {
            return 4 * Math.PI * radius * radius;
        }

        /// <summary>
        /// TinyBall manifold volume
        /// </summary>
        /// <returns>volume as double</returns>
        public override double Volume()
        {
            return 4.0 / 3.0 * Math.PI * radius * radius * radius;
        }

        /// <summary>
        /// TinyBall manifold voxel volume
        /// </summary>
        /// <returns>voxel volume as double</returns>
        public override double VoxelVolume()
        {
            return 0;
        }

        /// <summary>
        /// TinyBall value at position, delegates to scalar field
        /// </summary>
        /// <param name="x">position</param>
        /// <param name="sf">underlying scalar field</param>
        /// <returns>value as double</returns>
        public override double Value(double[] x, ScalarField sf)
        {
            // test for out of bounds
            if (localIsOn(x) == false)
            {
                // Return zero when x is not in the ball.
                return 0;
            }

            double value = sf.array[0];

            for (int i = 1; i < 4; i++)
            {
                value += x[i - 1] * sf.array[i];
            }

            return value;
        }

        /// <summary>
        /// checks if a point in local coordinates is on the manifold
        /// </summary>
        /// <param name="x">the point, assume to be in local coordinates</param>
        /// <returns>true if the point is on the manifold</returns>
        protected override bool localIsOn(double[] x)
        {
            double length = Math.Sqrt(x[0] * x[0] + x[1] * x[1] + x[2] * x[2]);

            return length <= radius;
        }

        /// <summary>
        /// TinyBall Laplacian field
        /// </summary>
        /// <param name="sf">field operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Laplacian(ScalarField sf)
        {
            ScalarField s = new ScalarField(this);

            s.array[0] = 0;
            s.array[1] = sf.array[1];
            s.array[2] = sf.array[2];
            s.array[3] = sf.array[3];
            s *= -5.0 / (radius * radius);
            return s;
        }

        /// <summary>
        /// TinyBall gradient
        /// </summary>
        /// <param name="x">local position</param>
        /// <param name="sf">field operand</param>
        /// <returns>gradient vector</returns>
        public override double[] Grad(double[] x, ScalarField sf)
        {
            if (localIsOn(x) == false)
            {
                return new double[] { 0, 0, 0 };
            }

            return new double[] { sf.array[1], sf.array[2], sf.array[3] };
        }

        /// <summary>
        /// TinyBall field diffusion, flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public override ScalarField DiffusionFluxTerm(ScalarField flux)
        {
            if(flux.M.GetType() != typeof(TinySphere))
            {
                throw new Exception("Manifold mismatch: flux for TinyBall must be on TinySphere.");
            }

            ScalarField s = new ScalarField(this);

            s.array[0] = 3 * flux.array[0] / radius;
            s.array[1] = 5 * flux.array[1] / (radius * radius * radius);
            s.array[2] = 5 * flux.array[2] / (radius * radius * radius);
            s.array[3] = 5 * flux.array[3] / (radius * radius * radius);
            
            return s;
        }

        /// <summary>
        /// TinyBall integrate over the whole field
        /// </summary>
        /// <param name="sf">field parameter</param>
        /// <returns>integral value</returns>
        public override double Integrate(ScalarField sf)
        {
            return sf.array[0] * 4 * Math.PI * radius * radius * radius / 3;
        }
    }

    /// <summary>
    /// interpolated lattice base
    /// </summary>
    public abstract class InterpolatedNodes : Manifold
    {
        protected int[] nNodesPerSide;
        protected double stepSize;
        protected double[] extent;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="nNodesPerSide">nodes per side array</param>
        /// <param name="stepSize">uniform stepsize</param>
        /// <param name="dim">dimension</param>
        public InterpolatedNodes(int[] nNodesPerSide, double stepSize, int dim) : base(dim)
        {
            if (nNodesPerSide.Length != Dim)
            {
                throw new Exception("Dimension mismatch in interpolated manifold.");
            }

            this.stepSize = stepSize;
            this.nNodesPerSide = (int[])nNodesPerSide.Clone();
            extent = new double[Dim];
            // accumulate array size and compute extents
            ArraySize = 1;
            for (int i = 0; i < Dim; i++)
            {
                ArraySize *= nNodesPerSide[i];
                extent[i] = (nNodesPerSide[i] - 1) * stepSize;
            }
        }

        /// <summary>
        /// IL manifold extent
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>extent as double</returns>
        public override double Extent(int i)
        {
            if (i < 0 || i > extent.Length - 1)
            {
                throw new Exception("Extent out of range.");
            }

            return extent[i];
        }

        /// <summary>
        /// IL manifold stepsize
        /// </summary>
        /// <returns>stepsize as double</returns>
        public override double StepSize()
        {
            return stepSize;
        }

        /// <summary>
        /// IL nodes per side
        /// </summary>
        /// <param name="i">direction index</param>
        /// <returns>number of nodes as int</returns>
        public override int NodesPerSide(int i)
        {
            if (i < 0 || i > nNodesPerSide.Length - 1)
            {
                throw new Exception("NodesPerSide out of range.");
            }

            return nNodesPerSide[i];
        }

        /// <summary>
        /// IL scalarfield multiplication
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Multiply(ScalarField sf1, ScalarField sf2)
        {
            ScalarField product = new ScalarField(this);

            for (int i = 0; i < ArraySize; i++)
            {
                product.array[i] = sf1.array[i] * sf2.array[i];
            }

            return product;
        }

        /// <summary>
        /// IL scalarfield division
        /// </summary>
        /// <param name="sf1">lh operand</param>
        /// <param name="sf2">rh operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Divide(ScalarField sf1, ScalarField sf2)
        {
            ScalarField c = new ScalarField(this);

            for (int i = 0; i < ArraySize; i++)
            {
                if (sf2.array[i] == 0)
                {
                    throw new Exception("Scalar field division by zero.");
                }
                c.array[i] = sf1.array[i] / sf2.array[i];
            }

            return c;
        }

        /// <summary>
        /// local point to multidimensional index
        /// </summary>
        /// <param name="x">local point</param>
        /// <returns>index array</returns>
        public int[] localToIndexArray(Vector x)
        {
            int[] tmp = new int[Dim];

            for (int i = 0; i < Dim; i++)
            {
                tmp[i] = (int)(x[i] / stepSize);
            }

            return tmp;
        }

        /// <summary>
        /// multidimensional index to local point
        /// </summary>
        /// <param name="idx">index array</param>
        /// <returns>local point</returns>
        public Vector indexArrayToLocal(int[] idx)
        {
            Vector tmp = new Vector(Transform.Dim);

            for (int i = 0; i < Dim; i++)
            {
                tmp[i] = idx[i] * stepSize;
            }

            return tmp;
        }

        /// <summary>
        /// multidimensional index to linear index
        /// </summary>
        /// <param name="idx">multidimensional index</param>
        /// <returns>linear index</returns>
        public int indexArrayToLinearIndex(int[] idx)
        {
            int lin = 0;

            if (idx.Length <= 0 || idx.Length > Dim)
            {
                throw new Exception("Index dimension doesn't match manifold.");
            }

            lin = idx[0];
            if (idx.Length >= 2)
            {
                lin += idx[1] * nNodesPerSide[0];
                if (idx.Length == 3)
                {
                    lin += idx[2] * nNodesPerSide[0] * nNodesPerSide[1];
                }
                else
                {
                    throw new Exception("Manifold and index dimension larger than three.");
                }
            }

            return lin;
        }

        /// <summary>
        /// linear index to multidimensional index
        /// </summary>
        /// <param name="lin">linear index</param>
        /// <returns>multidimensional index</returns>
        public int[] linearIndexToIndexArray(int lin)
        {
            int[] idx = new int[Dim];

            if (Dim >= 1)
            {
                if (Dim >= 2)
                {
                    if (Dim == 3)
                    {
                        idx[2] = lin / (nNodesPerSide[0] * nNodesPerSide[1]);
                        lin %= nNodesPerSide[0] * nNodesPerSide[1];
                    }
                    idx[1] = lin / nNodesPerSide[0];
                    lin %= nNodesPerSide[0];
                }
                idx[0] = lin;
            }

            return idx;
        }

        /// <summary>
        /// linear index to local point
        /// </summary>
        /// <param name="lin">linear index</param>
        /// <returns>local point</returns>
        public Vector linearIndexToLocal(int lin)
        {
            return indexArrayToLocal(linearIndexToIndexArray(lin));
        }

        /// <summary>
        /// checks if a point in local coordinates is on the manifold
        /// </summary>
        /// <param name="x">the point, assume to be in local coordinates</param>
        /// <returns>true if the point is on the manifold</returns>
        protected override bool localIsOn(double[] x)
        {
            for (int i = Dim; i < Transform.Dim; i++)
            {
                if (x[i] != 0)
                {
                    return false;
                }
            }
            for (int i = 0; i < Dim; i++)
            {
                if (x[i] < 0 || x[i] > extent[i])
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// IL rectangle
    /// </summary>
    public class InterpolatedRectangle : InterpolatedNodes
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="nNodesPerSide">nodes per side array</param>
        /// <param name="stepSize">uniform stepsize</param>
        /// <param name="dim">dimension</param>
        public InterpolatedRectangle(int[] nNodesPerSide, double stepSize) : base(nNodesPerSide, stepSize, 2)
        {
        }

        /// <summary>
        /// IL rectangle manifold area
        /// </summary>
        /// <returns>area as double</returns>
        public override double Area()
        {
            return extent[0] * extent[1];
        }

        /// <summary>
        /// IL rectangle manifold volume
        /// </summary>
        /// <returns>volume as double</returns>
        public override double Volume()
        {
            return 0;
        }

        /// <summary>
        /// IL rectangle manifold voxel volume
        /// </summary>
        /// <returns>voxel volume as double</returns>
        public override double VoxelVolume()
        {
            return 0;
        }

        /// <summary>
        /// IL rectangle value at position, delegates to scalar field and interpolates
        /// </summary>
        /// <param name="x">position</param>
        /// <param name="sf">underlying scalar field</param>
        /// <returns>value as double</returns>
        public override double Value(double[] x, ScalarField sf)
        {
            // machinery for interpolation
            // test for out of bounds
            if (localIsOn(x) == false)
            {
                // Note: is returning zero the right thing to do when x is out of bounds?
                return 0;
            }

            int[] idx = localToIndexArray(x);

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1, similarly for j
            if (idx[0] == nNodesPerSide[0] - 1)
            {
                idx[0]--;
            }
            if (idx[1] == nNodesPerSide[1] - 1)
            {
                idx[1]--;
            }

            double dx = x[0] / stepSize - idx[0],
                   dy = x[1] / stepSize - idx[1],
                   dxmult, dymult,
                   value = 0;

            for (int di = 0; di < 2; di++)
            {
                for (int dj = 0; dj < 2; dj++)
                {
                    dxmult = di == 0 ? (1 - dx) : dx;
                    dymult = dj == 0 ? (1 - dy) : dy;
                    value += dxmult * dymult * sf.array[(idx[0] + di) + (idx[1] + dj) * nNodesPerSide[0]];
                }
            }

            return value;
        }

        /// <summary>
        /// IL rectangle Laplacian field
        /// </summary>
        /// <param name="sf">field operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Laplacian(ScalarField sf)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// IL rectangle gradient
        /// </summary>
        /// <param name="x">local position</param>
        /// <param name="sf">field operand</param>
        /// <returns>gradient vector</returns>
        public override double[] Grad(double[] x, ScalarField sf)
        {
            double[] grad = new double[] { 0, 0, 0 };

            // test for out of bounds
            if (localIsOn(x) == false)
            {
                // Note: is returning the null vector the right thing to do when x is out of bounds?
                return grad;
            }

            int[] idx = localToIndexArray(x);

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1, similarly for j
            if (idx[0] == nNodesPerSide[0] - 1)
            {
                idx[0]--;
            }
            if (idx[1] == nNodesPerSide[1] - 1)
            {
                idx[1]--;
            }

            double dx = x[0] / stepSize - idx[0],
                   dy = x[1] / stepSize - idx[1],
                   dxmult, dymult;
            int[] di = new int[2], dj = new int[2];

            for (int i = 0; i < 2; i++)
            {
                for (int d = 0; d < 2; d++)
                {
                    // x-direction
                    if (i == 0)
                    {
                        // interpolation multipliers
                        dxmult = 1;
                        dymult = d == 0 ? (1 - dy) : dy;
                        // index differences
                        di[0] = 1;
                        di[1] = 0;
                        dj[0] = d;
                        dj[1] = d;
                    }
                    else // y-direction
                    {
                        // interpolation multipliers
                        dxmult = d == 0 ? (1 - dx) : dx;
                        dymult = 1;
                        // index differences
                        di[0] = d;
                        di[1] = d;
                        dj[0] = 1;
                        dj[1] = 0;
                    }
                    grad[i] += dxmult * dymult *
                               (sf.array[(idx[0] + di[0]) + (idx[1] + dj[0]) * nNodesPerSide[0]] - sf.array[(idx[0] + di[1]) + (idx[1] + dj[1]) * nNodesPerSide[0]]);
                }
                grad[i] /= stepSize;
            }

            return grad;
        }

        /// <summary>
        /// IL rectangle field diffusion, flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public override ScalarField DiffusionFluxTerm(ScalarField flux)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// IL rectangle integrate over the whole field
        /// </summary>
        /// <param name="sf">field parameter</param>
        /// <returns>integral value</returns>
        public override double Integrate(ScalarField sf)
        {
            double[] point = new double[] { 0, 0, 0 };
            double sum = 0,
                   voxel = stepSize * stepSize;

            for (int j = 0; j < nNodesPerSide[1] - 1; j++)
            {
                for (int i = 0; i < nNodesPerSide[0] - 1; i++)
                {
                    point[0] = (i + 0.5) * stepSize;
                    point[1] = (j + 0.5) * stepSize;

                    // The value at the center of the voxel
                    sum += sf.Value(point);
                }
            }

            return sum * voxel;
        }
    }

    /// <summary>
    /// IL prism
    /// </summary>
    public class InterpolatedRectangularPrism : InterpolatedNodes
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="nNodesPerSide">nodes per side array</param>
        /// <param name="stepSize">uniform stepsize</param>
        /// <param name="dim">dimension</param>
        public InterpolatedRectangularPrism(int[] nNodesPerSide, double stepSize) : base(nNodesPerSide, stepSize, 3)
        {
        }

        /// <summary>
        /// IL prism manifold area
        /// </summary>
        /// <returns>area as double</returns>
        public override double Area()
        {
            return extent[0] * extent[1] * 2 + extent[1] * extent[2] * 2 + extent[2] * extent[0] * 2;
        }

        /// <summary>
        /// IL prism manifold volume
        /// </summary>
        /// <returns>volume as double</returns>
        public override double Volume()
        {
            return extent[0] * extent[1] * extent[2];
        }

        /// <summary>
        /// IL prism manifold voxel volume
        /// </summary>
        /// <returns>voxel volume as double</returns>
        public override double VoxelVolume()
        {
            return stepSize * stepSize * stepSize;
        }

        /// <summary>
        /// IL prism value at position, delegates to scalar field and interpolates
        /// </summary>
        /// <param name="x">position</param>
        /// <param name="sf">underlying scalar field</param>
        /// <returns>value as double</returns>
        public override double Value(double[] x, ScalarField sf)
        {
            // machinery for interpolation
            // test for out of bounds
            if (localIsOn(x) == false)
            {
                // Note: is returning zero the right thing to do when x is out of bounds?
                return 0;
            }

            int[] idx = localToIndexArray(x);

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1
            // Similarly, for j and k.
            if (idx[0] == nNodesPerSide[0] - 1)
            {
                idx[0]--;
            }
            if (idx[1] == nNodesPerSide[1] - 1)
            {
                idx[1]--;
            }
            if (idx[2] == nNodesPerSide[2] - 1)
            {
                idx[2]--;
            }

            double dx = x[0] / stepSize - idx[0],
                   dy = x[1] / stepSize - idx[1],
                   dz = x[2] / stepSize - idx[2],
                   dxmult, dymult, dzmult,
                   value = 0;

            for (int di = 0; di < 2; di++)
            {
                for (int dj = 0; dj < 2; dj++)
                {
                    for (int dk = 0; dk < 2; dk++)
                    {
                        dxmult = di == 0 ? (1 - dx) : dx;
                        dymult = dj == 0 ? (1 - dy) : dy;
                        dzmult = dk == 0 ? (1 - dz) : dz;
                        value += dxmult * dymult * dzmult * sf.array[(idx[0] + di) + (idx[1] + dj) * nNodesPerSide[0] + (idx[2] + dk) * nNodesPerSide[0] * nNodesPerSide[1]];
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// IL prism Laplacian field
        /// </summary>
        /// <param name="sf">field operand</param>
        /// <returns>resulting field</returns>
        public override ScalarField Laplacian(ScalarField sf)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// IL prism gradient
        /// </summary>
        /// <param name="x">local position</param>
        /// <param name="sf">field operand</param>
        /// <returns>gradient vector</returns>
        public override double[] Grad(double[] x, ScalarField sf)
        {
            double[] grad = new double[] { 0, 0, 0 };

            // test for out of bounds
            if (localIsOn(x) == false)
            {
                // Note: is returning the null vector the right thing to do when x is out of bounds?
                return grad;
            }

            int[] idx = localToIndexArray(x);

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1, similarly for j and k
            if (idx[0] == nNodesPerSide[0] - 1)
            {
                idx[0]--;
            }
            if (idx[1] == nNodesPerSide[1] - 1)
            {
                idx[1]--;
            }
            if (idx[2] == nNodesPerSide[2] - 1)
            {
                idx[2]--;
            }

            double dx = x[0] / stepSize - idx[0],
                   dy = x[1] / stepSize - idx[1],
                   dz = x[2] / stepSize - idx[2],
                   dxmult, dymult, dzmult;
            int[] di = new int[2], dj = new int[2], dk = new int[2];

            for (int i = 0; i < 3; i++)
            {
                for (int d = 0; d < 2; d++)
                {
                    // x-direction
                    if (i == 0)
                    {
                        // interpolation multipliers
                        dxmult = 1;
                        dymult = d == 0 ? (1 - dy) : dy;
                        dzmult = d == 0 ? (1 - dz) : dz;
                        // index differences
                        di[0] = 1;
                        di[1] = 0;
                        dj[0] = d;
                        dj[1] = d;
                        dk[0] = d;
                        dk[1] = d;
                    }
                    else if (i == 1) // y-direction
                    {
                        // interpolation multipliers
                        dxmult = d == 0 ? (1 - dx) : dx;
                        dymult = 1;
                        dzmult = d == 0 ? (1 - dz) : dz;
                        // index differences
                        di[0] = d;
                        di[1] = d;
                        dj[0] = 1;
                        dj[1] = 0;
                        dk[0] = d;
                        dk[1] = d;
                    }
                    else // z-direction
                    {
                        // interpolation multipliers
                        dxmult = d == 0 ? (1 - dx) : dx;
                        dymult = d == 0 ? (1 - dy) : dy;
                        dzmult = 1;
                        // index differences
                        di[0] = d;
                        di[1] = d;
                        dj[0] = d;
                        dj[1] = d;
                        dk[0] = 1;
                        dk[1] = 0;
                    }
                    grad[i] += dxmult * dymult * dzmult *
                               (sf.array[(idx[0] + di[0]) + (idx[1] + dj[0]) * nNodesPerSide[0] + (idx[2] + dk[0]) * nNodesPerSide[0] * nNodesPerSide[1]] -
                                sf.array[(idx[0] + di[1]) + (idx[1] + dj[1]) * nNodesPerSide[0] + (idx[2] + dk[1]) * nNodesPerSide[0] * nNodesPerSide[1]]);
                }
                grad[i] /= stepSize;
            }

            return grad;
        }

        /// <summary>
        /// IL prism field diffusion, flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public override ScalarField DiffusionFluxTerm(ScalarField flux)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// IL prism integrate over the whole field
        /// </summary>
        /// <param name="sf">field parameter</param>
        /// <returns>integral value</returns>
        public override double Integrate(ScalarField sf)
        {
            double[] point = new double[3];
            double sum = 0,
                   voxel = stepSize * stepSize * stepSize;

            for (int k = 0; k < nNodesPerSide[2] - 1; k++)
            {
                for (int j = 0; j < nNodesPerSide[1] - 1; j++)
                {
                    for (int i = 0; i < nNodesPerSide[0] - 1; i++)
                    {
                        point[0] = (i + 0.5) * stepSize;
                        point[1] = (j + 0.5) * stepSize;
                        point[2] = (k + 0.5) * stepSize;

                        // The value at the center of the voxel
                        sum += sf.Value(point);
                    }
                }
            }

            return sum * voxel;
        }
    }
}
