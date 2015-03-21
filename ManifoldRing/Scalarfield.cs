using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NativeDaphne;

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
        private double[,] Sigma;
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
            if (parameters.Length != 13)
            {
                throw new Exception("GaussianFieldInitializer length must be 13: center, peak value, and sigma matrix by columns.");
            }
            
            this.center = new double[] { parameters[0], parameters[1], parameters[2] };

            // fill by columns
            this.Sigma = new double[3,3];
            int k = 3;
            for (int i = 0; i < 3; i++, k += 3)
            {
                this.Sigma[0, i] = parameters[0 + k];
                this.Sigma[1, i] = parameters[1 + k];
                this.Sigma[2, i] = parameters[2 + k];
            }

            this.max = parameters[12];

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
            if (point.Length != center.Length || point.Length != Sigma.GetLength(0))
            {
                throw new Exception("Exception initializing Gaussian field, array length mismatch.");
            }
            if (initialized == false)
            {
                throw new Exception("Must call setParameters prior to using FieldInitializer.");
            }

            double f = 0;
            double[] temp = new double[3];
            for (int i = 0; i < point.Length; i++)
            {
                temp[i] =   (center[0] - point[0]) * Sigma[0, i]
                          + (center[1] - point[1]) * Sigma[1, i]
                          + (center[2] - point[2]) * Sigma[2, i];
            }

            for (int i = 0; i < point.Length; i++)
            {
                f += temp[i] * (center[i] - point[i]);
            }
            return max * Math.Exp(-f / 2);
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
        public Nt_Darray array;
        private readonly Manifold m;
        private IFieldInitializer init;

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
            array = new Nt_Darray(m.ArraySize);
        }

        /// <summary>
        /// initialize the field according to the initializer object
        /// </summary>
        /// <param name="init">initializer object</param>
        public void Initialize(string type, double[] parameters)
        {
            if (FactoryContainer.fieldInitFactory == null)
            {
                throw new Exception("Calling Initialize without a valid factory.");
            }

            init = FactoryContainer.fieldInitFactory.Initialize(type);
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
                    array[0] = init.initialize(new double[] { 0, 0, 0 });
                }
                else
                {
                    throw new Exception("Currently only the constant initializer is supported for ME fields.");
                }
            }
        }

        public ScalarField reset(ScalarField src)
        {
            for (int i = 0; i < array.Length; i++) array[i] = src.array[i];
            return this;
        }

        public ScalarField reset(double d)
        {
            for (int i = 0; i < array.Length; i++) array[i] = d;
            return this;
        }

        /// <summary>
        /// copy array value to valarr, used to access the array
        /// for saving states only
        /// </summary>
        /// <param name="val">destination array</param>
        /// <param name="start">destination start index</param>
        /// <returns> number of elements copied</returns>
        public int CopyArray(double[] valarr, int start = 0)
        {
            Array.Copy(array.ArrayCopy, 0, valarr, start, array.Length);
            return array.Length;
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
        public ScalarField DiffusionFluxTerm(ScalarField flux, Transform t, double dt)
        {
            return m.DiffusionFluxTerm(flux, t, this, dt);
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
            for (int i = 0; i < m.ArraySize; i++)
            {
                array[i] *= s;
            }
            return this;
        }

        /// <summary>
        /// this multipy will return a new scalarfield
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public ScalarField Multiply(ScalarField f2)
        {

            if (this.m != f2.m)
            {
                throw new Exception("Scalar field multiplicands must share a manifold.");
            }
            this.m.Multiply(this, f2);
            return this;
        }

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="f">field</param>
        /// <param name="s">scalar multiplier</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator *(ScalarField f, double s)
        {
            ScalarField product = new ScalarField(f.m);
            for (int i = 0; i < f.m.ArraySize; i++)
            {
                product.array[i] = s * f.array[i];
            }

            return product;
        }

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="s">scalar multiplier</param>
        /// <param name="f">field</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator *(double s, ScalarField f)
        {
            return f * s;
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
            ScalarField product = new ScalarField(f1.m);
            return f1.m.Multiply(product.reset(f1), f2);
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
            for (int i = 0; i < m.ArraySize; i++)
            {
                array[i] += f.array[i];
            }
            return this;
        }

        /// <summary>
        /// addition of a constant to this scalar field
        /// </summary>
        /// <param name="d">constant</param>
        /// <returns></returns>
        public ScalarField Add(double d)
        {
            m.Add(this, d);
            return this;
        }

        /// <summary>
        /// field addition operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator +(ScalarField f1, ScalarField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Scalar field addends must share a manifold.");
            }

            ScalarField sum = new ScalarField(f1.m);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                sum.array[i] = f1.array[i] + f2.array[i];
            }

            return sum;
        }

        /// <summary>
        /// addition of constant to scalar field
        /// </summary>
        /// <param name="f">scalar field</param>
        /// <param name="d">constant</param>
        /// <returns></returns>
        public static ScalarField operator +(ScalarField f, double d)
        {
            ScalarField sf = new ScalarField(f.m);
            return sf.reset(f).Add(d);
        }

        /// <summary>
        /// addition of constant to scalar field
        /// </summary>
        /// <param name="d">constant</param>
        /// <param name="f">scalar field</param>
        /// <returns></returns>
        public static ScalarField operator +(double d, ScalarField f)
        {
            return f.Add(d);
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

           
            for (int i = 0; i < m.ArraySize; i++)
            {
                array[i] -= f.array[i];
            }
            return this;
        }

        /// <summary>
        /// field subtraction operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
        public static ScalarField operator -(ScalarField f1, ScalarField f2)
        {
            if (f1.m != f2.m)
            {
                throw new Exception("Scalar field addends must share a manifold.");
            }

            ScalarField difference = new ScalarField(f1.m);

            for (int i = 0; i < f1.m.ArraySize; i++)
            {
                difference.array[i] = f1.array[i] - f2.array[i];
            }

            return difference;
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
    }
}
