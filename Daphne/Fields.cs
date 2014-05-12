#if DAPHNE_MATH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MathNet.Numerics.LinearAlgebra;


namespace Daphne
{
    public interface IFieldInitializer
    {
        double initialize(double[] point);
    }

    public class ConstFieldInitializer : IFieldInitializer
    {
        private double cVal;

        public ConstFieldInitializer(double c)
        {
            cVal = c;
        }

        public double initialize(double[] point)
        {
            return cVal;
        }
    }

    public class GaussianFieldInitializer : IFieldInitializer
    {
        private double[] center;
        private double[] sigma;
        private double max;

        public GaussianFieldInitializer(double[] center, double[] sigma, double max)
        {
            this.center = center;
            this.sigma = sigma;
            this.max = max;
        }

        public double initialize(double[] point)
        {
            if (point.Length != center.Length || point.Length != sigma.Length)
            {
                throw new Exception("Exception initializing Gaussian field, array length mismatch.");
            }

            double f = 0, d = 1.0;
            // double d = Math.Pow(2.0 * Math.PI, 1.5) * sigma[0] * sigma[1] * sigma[2];

            for (int i = 0; i < point.Length; i++)
            {
                f += (center[i] - point[i]) * (center[i] - point[i]) / (2 * sigma[i] * sigma[i]);
            }
            return max * Math.Exp(-f) / d;
        }
    }

    public abstract class ScalarField
    {
        public Manifold M { get { return m; } }

        protected readonly Manifold m;
        protected double[] array;

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
        public DiscreteScalarField(Manifold m) : base(m)
        {
            array = new double[m.ArraySize];
        }

        public DiscreteScalarField(Manifold m, IFieldInitializer init): this(m)
        {
            double[] point = new double[m.Dim];

            for (int i = 0; i < m.ArraySize; i++)
            {
                for(int j = 0; j < m.Dim; j++)
                {
                    point[j] = m.Coordinates[i, j];
                }
                array[i] = init.initialize(point);
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

    /// <summary>
    /// A scalar field represented by the zeroth and first order terms in a moment expansion in a 3D space.
    /// </summary>
    public class MomentExpansionScalarField : ScalarField
    {
        private int parameterSize;

        public MomentExpansionScalarField(Manifold m) : base(m)
        {
            parameterSize = 4;
            array = new double[parameterSize];
        }

        // Should allow specification of the the initial 0th and 1st moments. 
        public MomentExpansionScalarField(Manifold m, IFieldInitializer init) : this(m)
        {
        }

        public override ScalarField mult(double d)
        {
            MomentExpansionScalarField c = new MomentExpansionScalarField(m);

            for (int i = 0; i < parameterSize; i++)
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

            MomentExpansionScalarField c = new MomentExpansionScalarField(m);

            c.array[0] = array[0] * ((MomentExpansionScalarField)f).array[0];

            for (int i = 1; i < parameterSize; i++)
            {
                c.array[i] = array[i] * ((MomentExpansionScalarField)f).array[0] + array[0] * ((MomentExpansionScalarField)f).array[i];
            }

            return c;
        }

        /// <summary>
        /// Valid when the dot product of f1 and x is much less than f0
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
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
            if (((MomentExpansionScalarField)f).array[0] == 0)
            {
                throw new Exception("Moment expansion division by zero.");
            }

            MomentExpansionScalarField c = new MomentExpansionScalarField(m);

            c.array[0] = array[0] / ((MomentExpansionScalarField)f).array[0];

            for (int i = 1; i < parameterSize; i++)
            {
                c.array[i] = (array[i] - array[0] * ((MomentExpansionScalarField)f).array[i] / ((MomentExpansionScalarField)f).array[0]) / ((MomentExpansionScalarField)f).array[0];
            }

            return c;
        }

        public override ScalarField div(double d)
        {
            if (d == 0)
            {
                throw new Exception("Moment expansion division by zero.");
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

            MomentExpansionScalarField c = new MomentExpansionScalarField(m);

            for (int i = 0; i < parameterSize; i++)
            {
                c.array[i] = array[i] + ((MomentExpansionScalarField)f).array[i];
            }
            return c;
        }

        public override ScalarField sub(ScalarField f)
        {
            if (GetType() != f.GetType())
            {
                throw new Exception("Scalar field addition is only defined between fields of the same type");
            }
            if (m != f.M)
            {
                throw new Exception("Manifolds must be identical for scalar field addition.");
            }

            MomentExpansionScalarField c = new MomentExpansionScalarField(m);

            for (int i = 0; i < parameterSize; i++)
            {
                c.array[i] = array[i] - ((MomentExpansionScalarField)f).array[i];
            }
            return c;
        }

        public override double Get(double[] point)
        {
            LocalMatrix[] lm = new LocalMatrix[parameterSize];

            if (m.isOnBoundary(point))
            {
                lm[0].Index = 0;
                lm[0].Coefficient = 1;

                for (int i = 1; i < parameterSize; i++)
                {
                    lm[i].Index = i;
                    lm[i].Coefficient = point[i - 1];
                }
            }
            else
            {
                for (int i = 0; i < parameterSize; i++)
                {
                    lm[i].Index = i;
                    lm[i].Coefficient = 0;
                }
            }

            double value = 0;

            for (int i = 0; i < lm.Length; i++)
            {
                value += lm[i].Coefficient * array[lm[i].Index];
            }

            return value;             
        }

        public override double Get(int i)
        {
            if (i < 0 || i >= m.ArraySize)
            {
                throw new Exception("Index out of range.");
            }

            // return value at i-th point of the underlying manifold
            double[] point = new double[m.Dim];

            for(int j = 0; j < m.Dim; j++)
            {
                point[i] = m.Coordinates[i, j];
            }
            return Get(point);
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

            DiscreteScalarField product = new DiscreteScalarField(f1.m);

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
#endif