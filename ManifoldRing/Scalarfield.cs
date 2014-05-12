using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace ManifoldRing
{
    /// <summary>
    /// helper for field initialization
    /// </summary>
    public interface IFieldInitializer
    {
        /// <summary>
        /// the initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>field value at point</returns>
        double initialize(double[] point = null);
    }

    /// <summary>
    /// field initialization with a constant
    /// </summary>
    public class ConstFieldInitializer : IFieldInitializer
    {
        private double cVal;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="c">constant initialization value</param>
        public ConstFieldInitializer(double c)
        {
            cVal = c;
        }

        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>constant value regardless of point</returns>
        public double initialize(double[] point)
        {
            return cVal;
        }
    }

    /// <summary>
    /// Gaussian field initializer
    /// </summary>
    public class GaussianFieldInitializer : IFieldInitializer
    {
        private double[] center;
        private double[] sigma;
        private double max;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="center">the Gaussian's center</param>
        /// <param name="sigma">sigma/decay vector</param>
        /// <param name="max">maximum value</param>
        public GaussianFieldInitializer(double[] center, double[] sigma, double max)
        {
            this.center = center;
            this.sigma = sigma;
            this.max = max;
        }

        /// <summary>
        /// Gaussian initializer routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>the Gaussian's value at point</returns>
        public double initialize(double[] point)
        {
            if (point == null)
            {
                throw new Exception("Initializing Gaussian needs a valid point.");
            }
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

    /// <summary>
    /// scalar field class with operations
    /// </summary>
    public class ScalarField
    {
        internal double[] array;
        private readonly Manifold m;
        /// <summary>
        /// underlying manifold
        /// </summary>
        public Manifold M { get { return m; } }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="m">manifold</param>
        public ScalarField(Manifold m)
        {
            this.m = m;
            array = new double[m.ArraySize];
        }

        /// <summary>
        /// constructor with initialization
        /// </summary>
        /// <param name="m">underlying manifold</param>
        /// <param name="init">initializer</param>
        public ScalarField(Manifold m, IFieldInitializer init) : this(m)
        {
            if (m.GetType() == typeof(InterpolatedRectangle) || m.GetType() == typeof(InterpolatedRectangularPrism))
            {
                for (int i = 0; i < m.ArraySize; i++)
                {
                    array[i] = init.initialize(((InterpolatedNodes)m).linearIndexToLocal(i));
                }
            }
            else
            {
                // for now only the constant field initializer is supported
                if (init.GetType() == typeof(ConstFieldInitializer))
                {
                    // initialize the zero-th moment for ME fields; leave gradient equal to zero
                    array[0] = init.initialize();
                }
                else
                {
                    throw new Exception("Currently only the constant initializer is supported for ME fields.");
                }
            }
        }

        /// <summary>
        /// retrieve field value at a point
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>the field value</returns>
        public double Value(double[] point)
        {
            return m.Value(point, this);
        }

        /// <summary>
        /// field gradient at a location
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>gradient vector</returns>
        public double[] Gradient(double[] point)
        {
            return m.Grad(point, this);
        }

        /// <summary>
        /// field Laplacian
        /// </summary>
        /// <returns>Laplacian as field</returns>
        public ScalarField Laplacian()
        {
            return m.Laplacian(this);
        }

        /// <summary>
        /// field diffusion flux term
        /// </summary>
        /// <param name="flux">flux involved</param>
        /// <returns>diffusion flux term as field</returns>
        public ScalarField DiffusionFluxTerm(ScalarField flux, Transform t)
        {
            return m.DiffusionFluxTerm(flux,t);
        }

        /// <summary>
        /// integrate the field
        /// </summary>
        /// <returns>integral value</returns>
        public double Integrate()
        {
            return m.Integrate(this);
        }

        /// <summary>
        ///  convert 'from' to the format of this at pos
        /// </summary>
        /// <param name="from">field to convert</param>
        /// <param name="pos">position at which to convert</param>
        //public void Convert(ScalarField from, double[] pos)
        //{
        //    if(from.M.GetType() == typeof(InterpolatedRectangularPrism) && m.GetType() == typeof(TinySphere))
        //    {
        //        double[] grad = m.Grad(pos, from);

        //        array[0] = from.Value(pos);
        //        array[1] = grad[0];
        //        array[2] = grad[1];
        //        array[3] = grad[2];
        //    }
        //    else if (from.M.GetType() == typeof(TinyBall) && m.GetType() == typeof(TinySphere))
        //    {
        //        // same representation
        //        for (int i = 0; i < m.ArraySize; i++)
        //        {
        //            array[i] = from.array[i];
        //        }
        //    }
        //    else if (from.M.GetType() == typeof(InterpolatedRectangle) && m.GetType() == typeof(InterpolatedRectangularPrism))
        //    {
        //        throw new NotImplementedException();
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        /// <summary>
        /// multiply the field by a scalar
        /// </summary>
        /// <param name="s">scalar multiplier</param>
        /// <returns>resulting field</returns>
        public ScalarField Multiply(double s)
        {
            ScalarField product = new ScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                product.array[i] = s * this.array[i];
            }

            return product;
        }

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="f">field</param>
        /// <param name="s">scalar multiplier</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator *(ScalarField f, double s)
        {
            return f.Multiply(s);
        }

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="s">scalar multiplier</param>
        /// <param name="f">field</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator *(double s, ScalarField f)
        {
            return f.Multiply(s);
        }

        /// <summary>
        /// scalar field multiplication operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator *(ScalarField f1, ScalarField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Scalar field multiplicands must share a manifold.");
            }

            return f1.m.Multiply(f1, f2);
        }

        /// <summary>
        /// scalar field division operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator /(ScalarField f1, ScalarField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Scalar field multiplicands must share a manifold.");
            }

            return f1.m.Divide(f1, f2);
        }

        /// <summary>
        /// scalar field division by scalar
        /// </summary>
        /// <param name="s">scalar divisor</param>
        /// <returns>resulting field</returns>
        public ScalarField Divide(double s)
        {
            if (s == 0)
            {
                throw new Exception("Scalar field division by zero.");
            }

            ScalarField product = new ScalarField(m);

            for (int i = 0; i < m.ArraySize; i++)
            {
                product.array[i] = this.array[i] / s;
            }

            return product;
        }

        /// <summary>
        /// scalar field division operator by scalar
        /// </summary>
        /// <param name="f">field</param>
        /// <param name="s">divisor</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator /(ScalarField f, double s)
        {
            return f.Divide(s);
        }

        /// <summary>
        /// scalar field division operator by scalar
        /// </summary>
        /// <param name="s">divisor</param>
        /// <param name="f">field</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator /(double s, ScalarField f)
        {
            return f.Divide(s);
        }

        /// <summary>
        /// scalar field addition
        /// </summary>
        /// <param name="f">field addend</param>
        /// <returns>resulting field</returns>
        public ScalarField Add(ScalarField f)
        {
            if (f.m != this.m)
            {
                throw new Exception("Scalar field addends must share a manifold.");
            }

            ScalarField sum = new ScalarField(f.m);

            for (int i = 0; i < f.m.ArraySize; i++)
            {
                sum.array[i] = this.array[i] + f.array[i];
            }

            return sum;
        }

        /// <summary>
        /// field addition operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator +(ScalarField f1, ScalarField f2)
        {
            return f1.Add(f2);
        }

        /// <summary>
        /// scalar field subtraction
        /// </summary>
        /// <param name="f">field subtrahend</param>
        /// <returns>resulting field</returns>
        public ScalarField Subtract(ScalarField f)
        {
            if (f.m != this.m)
            {
                throw new Exception("Scalar field addends must share a manifold.");
            }

            ScalarField difference = new ScalarField(f.m);

            for (int i = 0; i < f.m.ArraySize; i++)
            {
                difference.array[i] = this.array[i] - f.array[i];
            }

            return difference;
        }

        /// <summary>
        /// field subtraction operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator -(ScalarField f1, ScalarField f2)
        {
            return f1.Subtract(f2);
        }

        /// <summary>
        /// Restrict the scalar field to a boundary
        /// </summary>
        /// <param name="from">The scalar field to be restricted</param>
        /// <param name="pos">The position of the restricted manifold in the space</param>
        // public void Restrict(ScalarField from, double[] pos)
        public void Restrict(ScalarField from, Transform t)
        {
            // this.M.Restrict(from, pos, this);
            this.M.Restrict(from, t, this);
        }

        /// <summary>
        /// For debugging purposes
        /// </summary>
        /// <param name="filename"></param>
        public void WriteToFile(string filename)
        {

            if (M.GetType() == typeof(InterpolatedRectangle) || M.GetType() == typeof(InterpolatedRectangularPrism)) 
            {
                using (StreamWriter writer = File.CreateText(filename))
                {
                    int n = 0;
                    MathNet.Numerics.LinearAlgebra.Vector v;

                    InterpolatedNodes m;
                    m = (InterpolatedNodes)M;

                    for (int i = 0; i < array.Length; i++)
                    {
                        writer.Write(n + "\t");

                        v = m.indexArrayToLocal(m.linearIndexToIndexArray(i));

                        for (int j = 0; j < M.Dim; j++)
                        {
                            writer.Write(v[j] + "\t");
                        }
                        writer.Write(array[n] + "\n");

                        n++;
                    }
                }
            }
        }
    }
}
