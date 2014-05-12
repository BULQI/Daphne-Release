﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MathNet.Numerics.LinearAlgebra;


namespace Daphne
{
    public abstract class ScalarField
    {
        public Manifold M { get { return m; } }

        protected readonly Manifold m;

        public ScalarField(Manifold m)
        {
            this.m = m;
        }

        public abstract double Get(double[] point);
        public abstract double Get(int i);
        public abstract void Set(int i, double d);

        public double this[int i]
        {
            get
            {
                return Get(i);
            }
            set
            {
                Set(i, value);
            }
        }

        public abstract double[] Gradient(double[] point);
        public abstract double Laplacian(double[] point);

        public abstract ScalarField mult(double d);
        public static ScalarField operator *(ScalarField f, double d)
        {
            return f.mult(d);
        }

        public static ScalarField operator *(double d, ScalarField f)
        {
            return f.mult(d);
        }

        public abstract ScalarField mult(ScalarField f);
        public static ScalarField operator *(ScalarField f1, ScalarField f2)
        {
            return f1.mult(f2);
        }

        public abstract ScalarField div(ScalarField f);
        public static ScalarField operator /(ScalarField f1, ScalarField f2)
        {
            return f1.div(f2);
        }

        public abstract ScalarField div(double d);
        public static ScalarField operator /(ScalarField f, double d)
        {
            return f.div(d);
        }

        public abstract ScalarField add(ScalarField f);
        public static ScalarField operator +(ScalarField f1, ScalarField f2)
        {
            return f1.add(f2);
        }

        public abstract ScalarField sub(ScalarField f);
        public static ScalarField operator -(ScalarField f1, ScalarField f2)
        {
            return f1.sub(f2);
        }
    }

    public class DiscreteScalarField : ScalarField
    {
        protected double[] array;

        public DiscreteScalarField(Manifold m) : base(m)
        {
            array = new double[m.ArraySize];
        }

        public DiscreteScalarField(Manifold m, double c): this(m)
        {
            for (int i = 0; i < m.ArraySize; i++)
            {
                array[i] = c;
            }
        }

        public override ScalarField mult(double d)
        {
            DiscreteScalarField c = new DiscreteScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                c.array[i] = d * array[i];
            }

            return c;
        }

        public override ScalarField mult(ScalarField f)
        {
            if (GetType() != f.GetType())
            {
                throw new Exception("Scalar field multiplication is only defined between fields of the same type");
            }
            if (m != f.M)
            {
                throw new Exception("Manifolds must be identical for scalar field multiplication.");
            }

            DiscreteScalarField c = new DiscreteScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                c.array[i] = array[i] * ((DiscreteScalarField)f).array[i];
            }

            return c;
        }

        public override ScalarField div(ScalarField f)
        {
            if (GetType() != f.GetType())
            {
                throw new Exception("Scalar field division is only defined between fields of the same type");
            }
            if (m != f.M)
            {
                throw new Exception("Manifolds must be identical for scalar field division.");
            }

            DiscreteScalarField c = new DiscreteScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                if (((DiscreteScalarField)f).array[i] == 0)
                {
                    throw new Exception("Division by zero.");
                }
                c.array[i] = array[i] / ((DiscreteScalarField)f).array[i];
            }

            return c;
        }

        public override ScalarField div(double d)
        {
            if (d == 0)
            {
                throw new Exception("Division by zero.");
            }
            return this * (1.0 / d);
        }

        public override ScalarField add(ScalarField f)
        {
            if (GetType() != f.GetType())
            {
                throw new Exception("Scalar field addition is only defined between fields of the same type");
            }
            if (m != f.M)
            {
                throw new Exception("Manifolds must be identical for scalar field addition.");
            }

            DiscreteScalarField c = new DiscreteScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                c.array[i] = array[i] + ((DiscreteScalarField)f).array[i];
            }
            return c;
        }

        public override ScalarField sub(ScalarField f)
        {
            if (GetType() != f.GetType())
            {
                throw new Exception("Scalar field subtraction is only defined between fields of the same type");
            }
            if (m != f.M)
            {
                throw new Exception("Manifolds must be identical for scalar field subtraction.");
            }

            DiscreteScalarField c = new DiscreteScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                c.array[i] = array[i] - ((DiscreteScalarField)f).array[i];
            }
            return c;
        }

        public override double Get(double[] point)
        {
            LocalMatrix[] lm = m.Interpolation(point);
            double value = 0;

            if (lm != null)
            {
                for (int i = 0; i < lm.Length; i++)
                {
                    value += lm[i].Coefficient * array[lm[i].Index];
                }
            }

            return value;
        }

        public override double Get(int i)
        {
            if (i < 0 || i >= m.ArraySize)
            {
                throw new Exception("Scalarfield array index out of bounds.");
            }

            return array[i];
        }

        public override void Set(int i, double d)
        {
            if (i < 0 || i >= m.ArraySize)
            {
                throw new Exception("Scalarfield array index out of bounds.");
            }

            array[i] = d;
        }

        public override double[] Gradient(double[] point)
        {
            return null;
        }

        public override double Laplacian(double[] point)
        {
            return 0;
        }

    }

    public class MomentExpansionScalarField : ScalarField
    {
        public MomentExpansionScalarField(Manifold m) : base(m)
        {
        }

        public MomentExpansionScalarField(Manifold m, double c) : this(m)
        {
        }

        public override ScalarField mult(double d)
        {
            return null;
        }

        public override ScalarField mult(ScalarField f)
        {
            //if (GetType() != f.GetType())
            //{
            //    throw new Exception("Scalar field multiplication is only defined between fields of the same type");
            //}
            //if (m != f.m)
            //{
            //    throw new Exception("Manifolds must be identical for scalar field multiplication.");
            //}
            return null;
        }

        public override ScalarField div(ScalarField f)
        {
            //if (GetType() != f.GetType())
            //{
            //    throw new Exception("Scalar field division is only defined between fields of the same type");
            //}
            //if (m != f.m)
            //{
            //    throw new Exception("Manifolds must be identical for scalar field division.");
            //}
            return null;
        }

        public override ScalarField div(double d)
        {
            //if (d == 0)
            //{
            //    throw new Exception("Division by zero.");
            //}
            return null;
        }

        public override ScalarField add(ScalarField f)
        {
            //if (GetType() != f.GetType())
            //{
            //    throw new Exception("Scalar field addition is only defined between fields of the same type");
            //}
            //if (m != f.m)
            //{
            //    throw new Exception("Manifolds must be identical for scalar field addition.");
            //}
            return null;
        }

        public override ScalarField sub(ScalarField f)
        {
            //if (GetType() != f.GetType())
            //{
            //    throw new Exception("Scalar field subtraction is only defined between fields of the same type");
            //}
            //if (m != f.m)
            //{
            //    throw new Exception("Manifolds must be identical for scalar field subtraction.");
            //}
            return null;
        }

        public override double Get(double[] point)
        {
            // return value at point
            return 0;
        }

        public override double Get(int i)
        {
            // return value at i-th point of the underlying manifold
            return 0;
        }

        public override void Set(int i, double d)
        {
            // does this even make sense for the MESF?
        }

        public override double[] Gradient(double[] point)
        {
            return null;
        }

        public override double Laplacian(double[] point)
        {
            return 0;
        }
    }

    public class GaussianScalarField : DiscreteScalarField
    {
        public GaussianScalarField(Manifold m) : base(m)
        {
        }

        /// <summary>
        /// Calculate and return values of a Gaussian scalar field at each array point in the manifold
        /// The value at the center is max
        /// </summary>
        public bool Initialize(double[] x0, double[] sigma, double max)
        {
            if (m.Dim != x0.Length || m.Dim != sigma.Length)
            {
                return false;
            }

            double f = 0, d = 1.0;
            // double d = Math.Pow(2.0 * Math.PI, 1.5) * sigma[0] * sigma[1] * sigma[2];

            for (int i = 0; i < m.ArraySize; i++)
            {
                f = 0;
                for (int j = 0; j < m.Dim; j++)
                {
                    f += (x0[j] - m.Coordinates[i, j]) * (x0[j] - m.Coordinates[i, j]) / (2 * sigma[j] * sigma[j]);
                }
                array[i] = max * Math.Exp(-f) / d;
            }
            return true;
        }

    }

    public class VectorField
    {
        private double[][] vector;
        private Manifold m;
        private int dim;

        public Manifold M { get { return m; } }

        /// <summary>
        /// The dimension of each vector in the vector field is determined by the integer input parameter
        /// </summary>
        /// <param name="m">Manifold on which the vector field resides</param>
        /// <param name="i">The dimension of the vectors</param>
        public VectorField(Manifold m, int dim)
        {
            vector = new double[m.ArraySize][];
            for(int i = 0; i < m.ArraySize; i++)
            {
                vector[i] = new double[dim];
            }
            this.dim = dim;
            this.m = m;
        }

        /// <summary>
        /// The dimension of each vector in the vector field is determined by the dimension of the initializing array v.
        /// Each vector in the vector field is initialized with the values of v.
        /// </summary>
        /// <param name="m">Manifold on which the vector field resides</param>
        public VectorField(Manifold m, double[] v) : this(m, v.Length)
        {
            for (int i = 0; i < m.ArraySize; i++)
            {
                for (int j = 0; j < v.Length; j++)
                {
                    vector[i][j] = v[j];
                }
            }
        }

        public double[] Value(double[] point)
        {
            LocalMatrix[] lm = m.Interpolation(point);
            double[] value = new double[dim];

            if (lm != null)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int i = 0; i < lm.Length; i++)
                    {
                        value[j] += lm[i].Coefficient * vector[lm[i].Index][j];
                    }
                }
            }
            return value;
        }
        
        public double[] this[int i]
        {
            get
            {
                if (i < 0 || i >= m.ArraySize)
                {
                    throw new Exception("Vector field array index out of bounds.");
                }
                return vector[i];
            }
            set
            {
                if (i < 0 || i >= m.ArraySize || dim != value.Length)
                {
                    throw new Exception("Vector field array index out of bounds.");
                }
                for (int j = 0; j < dim; j++)
                {
                    vector[i][j] = value[j];
                }
            }
        }

        public static ScalarField operator *(VectorField f1, VectorField f2)
        {
            if (f1.m != f2.m || f1.dim != f2.dim)
            {
                throw new Exception("Field manifolds and dimensions must be identical for vector field dot product.");
            }

            DiscreteScalarField product = new DiscreteScalarField(f1.m, 0);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                for (int j = 0; j < f1.dim; j++)
                {
                    product[i] += f1.vector[i][j] * f2.vector[i][j];
                }
            }
            return product;
        }

        public static VectorField operator *(VectorField f1, ScalarField f2)
        {
            if (f1.m != f2.M)
            {
                throw new Exception("Manifolds must be identical for vector field multiplication.");
            }

            VectorField product = new VectorField(f1.m, f1.dim);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                for (int j = 0; j < f1.dim; j++)
                {
                    product.vector[i][j] = f1.vector[i][j] * f2[i];
                }
            }
            return product;
        }

        public static VectorField operator *(ScalarField f1, VectorField f2)
        {
            return f2 * f1;
        }

        public static VectorField operator *(double d, VectorField f)
        {
            VectorField product = new VectorField(f.m, f.dim);

            for (int i = 0; i < f.m.ArraySize; i++)
            {
                for (int j = 0; j < f.dim; j++)
                {
                    product.vector[i][j] = d * f.vector[i][j];
                }
            }
            return product;
        }

        public static VectorField operator *(VectorField f, double d)
        {
            return d * f;
        }

        public static VectorField operator /(VectorField f, double d)
        {
            if (d == 0)
            {
                throw new Exception("Division by zero.");
            }

            VectorField product = new VectorField(f.m, f.dim);

            for (int i = 0; i < f.m.ArraySize; i++)
            {
                for (int j = 0; j < f.dim; j++)
                {
                    product.vector[i][j] = f.vector[i][j] / d;
                }
            }
            return product;
        }

        public static VectorField operator /(VectorField f1, ScalarField f2)
        {
            if (f1.m != f2.M)
            {
                throw new Exception("Manifolds must be identical for vector field division.");
            }

            VectorField product = new VectorField(f1.m, f1.dim);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                for (int j = 0; j < f1.dim; j++)
                {
                    double d = f2[i];

                    if (d == 0)
                    {
                        throw new Exception("Division by zero.");
                    }
                    product.vector[i][j] = f1.vector[i][j] / d;
                }
            }
            return product;
        }

        public static VectorField operator +(VectorField f1, VectorField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Manifolds must be identical for vector field addition.");
            }

            VectorField c = new VectorField(f1.m, f1.dim);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                for (int j = 0; j < f1.dim; j++)
                {
                    c.vector[i][j] = f1.vector[i][j] + f2.vector[i][j];
                }
            }
            return c;
        }

        public static VectorField operator -(VectorField f1, VectorField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Manifolds must be identical for vector field subtraction.");
            }

            VectorField c = new VectorField(f1.m, f1.dim);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                for (int j = 0; j < f1.dim; j++)
                {
                    c.vector[i][j] = f1.vector[i][j] - f2.vector[i][j];
                }
            }
            return c;
        }

    }

}