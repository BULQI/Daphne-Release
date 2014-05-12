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
        /// initialization routine based on 3-dim coordinates
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>field value at point</returns>
        double initialize(double[] point);

        /// <summary>
        /// initialization based on idnex
        /// </summary>
        /// <param name="index">linear index</param>
        /// <returns>field value for the index</returns>
        double initialize(int index);

        void setParameters(double[] parameters);
    }

    /// <summary>
    /// field initialization with a constant
    /// </summary>
    public class ConstFieldInitializer : IFieldInitializer
    {
        private double cVal;
        private bool initialized;

        /// <summary>
        /// constructor
        /// </summary>
        public ConstFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
        public void setParameters(double[] parameters)
        {
            if (parameters.Length != 1)
            {
                throw new Exception("ConstFieldInitializer length must be 1.");
            }

            cVal = parameters[0];
            initialized = true;
        }

        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>constant value regardless of point</returns>
        public double initialize(double[] point)
        {
            if (initialized == false)
            {
                throw new Exception("Must call setParameters prior to using FieldInitializer.");
            }

            return cVal;
        }

        public double initialize(int index)
        {
            throw new Exception("ConstFieldInitializer should not be called by index");
        }

    }

    /// <summary>
    /// field initialization with a linear profile
    /// </summary>
    public class LinearFieldInitializer : IFieldInitializer
    {
        private double c1;
        private double c2;
        private double x1;
        private double x2;
        private int dim;
        private double slope;
        private double intercept;
        private bool initialized;

        /// <summary>
        /// constructor
        /// </summary>
        public LinearFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
        public void setParameters(double[] parameters)
        {
            if (parameters.Length != 5)
            {
                throw new Exception("LinearFieldInitializer length must be 5.");
            }

            c1 = parameters[0];
            c2 = parameters[1];
            x1 = parameters[2];
            x2 = parameters[3];
            dim = (int)parameters[4];
            slope = (c2 - c1) / (x2 - x1);
            intercept = c1 - x1 * slope;

            initialized = true;
        }

        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>linear value using the dim component of point</returns>
        public double initialize(double[] point)
        {
            if (initialized == false)
            {
                throw new Exception("Must call setParameters prior to using FieldInitializer.");
            }

            return slope * point[dim] + intercept;
        }

        public double initialize(int index)
        {
            throw new Exception("LinearFieldInitializer should not be called by index");
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
        private bool initialized;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="c">initialization array</param>
        public GaussianFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the Gaussian parameters
        /// </summary>
        /// <param name="parameters">the Gaussian's center, sigma/decay vector, maximum value packed into an array</param>
        public void setParameters(double[] parameters)
        {
            if (parameters.Length != 7)
            {
                throw new Exception("GaussianFieldInitializer length must be 7.");
            }

            this.center = new double[] { parameters[0], parameters[1], parameters[2] };
            this.sigma = new double[] { parameters[3], parameters[4], parameters[5] };
            this.max = parameters[6];
            initialized = true;
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
            if (initialized == false)
            {
                throw new Exception("Must call setParameters prior to using FieldInitializer.");
            }

            double f = 0, d = 1.0;
            // double d = Math.Pow(2.0 * Math.PI, 1.5) * sigma[0] * sigma[1] * sigma[2];

            for (int i = 0; i < point.Length; i++)
            {
                f += (center[i] - point[i]) * (center[i] - point[i]) / (2 * sigma[i] * sigma[i]);
            }
            return max * Math.Exp(-f) / d;
        }

        public double initialize(int index)
        {
            throw new Exception("GaussianFieldInitializer should not be called by index");
        }
    }

    /// <summary>
    /// field initialization for explicit valus
    /// </summary>
    public class ExplicitFieldInitializer : IFieldInitializer
    {
        private bool initialized;
        double[] exp_vals;

        /// <summary>
        /// constructor
        /// </summary>
        public ExplicitFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
        public void setParameters(double[] parameters)
        {
            exp_vals = new double[parameters.Length];
            Array.Copy(parameters, exp_vals, parameters.Length);
            initialized = true;
        }
        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>constant value regardless of point</returns>
        public double initialize(double[] point)
        {
            throw new Exception("ExcplictFieldInitializer should be called with index");
        }

        /// <summary>
        /// return the field for the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double initialize(int index)
        {

            if (initialized == false)
            {
                throw new Exception("Must call setParameters prior to using FieldInitializer.");
            }

            if (index < 0 || index >= exp_vals.Length)
            {
                throw new Exception("ExplicitFieldInitializer: index out of range");
            }
            return exp_vals[index];
        }
      
    }


    /// <summary>
    /// initializer factory
    /// </summary>
    public interface IFieldInitializerFactory
    {
        IFieldInitializer Initialize(string type);
    }

    /// <summary>
    /// scalar field class with operations
    /// </summary>
    public class ScalarField
    {
        internal double[] array;
        private readonly Manifold m;
        private IFieldInitializer init;

        private static IFieldInitializerFactory factory;

        /// <summary>
        /// underlying manifold
        /// </summary>
        public Manifold M { get { return m; } }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="m">manifold</param>
        public ScalarField(Manifold m, IFieldInitializerFactory _factory = null)
        {
            if (factory == null && _factory != null)
            {
                factory = _factory;
            }
            this.m = m;
            array = new double[m.ArraySize];
        }

        /// <summary>
        /// initialize the field according to the initializer object
        /// </summary>
        /// <param name="init">initializer object</param>
        public void Initialize(string type, double[] parameters)
        {
            if (factory == null)
            {
                throw new Exception("Calling Initialize without a valid factory.");
            }

            init = factory.Initialize(type);
            init.setParameters(parameters);

            if (init.GetType() == typeof(ExplicitFieldInitializer))
            {
                for (int i = 0; i < m.ArraySize; i++)
                {
                    array[i] = init.initialize(i);
                }
            }
            else if (m.GetType() == typeof(InterpolatedRectangle) || m.GetType() == typeof(InterpolatedRectangularPrism))
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
                    array[0] = init.initialize(new double[]{0, 0, 0});
                }
                else
                {
                    throw new Exception("Currently only the constant initializer is supported for ME fields.");
                }
            }
        }

        /// <summary>
        /// initialize fields when no factory exists.
        /// </summary>
        /// <param name="init">initializer object</param>
        public void reset(double[] vals = null)
        {
            if (factory != null)
            {
                throw new Exception("this needs to be Initialized through factory.");
            }
            if (vals == null)
            {
                for (int i = 0; i < array.Length; i++) array[i] = 0;
            }
            else
            {
                if (vals.Length < array.Length)
                {
                    throw new Exception("array length too short");
                }
                Array.Copy(vals, array, array.Length);
            }
        }

        /// <summary>
        /// allow accesss to array from outside of assesmbly
        /// </summary>
        /// <returns></returns>
        public double[] ValueArray
        {
            get
            {
                return array;
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
        /// calculate and return the mean concentration in this scalar field
        /// </summary>
        /// <returns>the mean value</returns>
        public double MeanValue()
        {
            return m.MeanValue(this);
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
        /// <param name="flux">flux from boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <returns>diffusion flux term as field in the interior manifold</returns>
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
        /// Impose Dirichlet boundary conditions
        /// </summary>
        /// <param name="from">Field specified on the boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <returns>The field after imposing Dirichlet boundary conditions</returns>
        public ScalarField DirichletBC(ScalarField from, Transform t)
        {
            return m.DirichletBC(from, t, this);
        }

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

                        v = m.linearIndexToLocal(i);

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
