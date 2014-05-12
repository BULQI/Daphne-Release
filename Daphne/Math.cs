using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System.IO;

using System.Diagnostics;

namespace Daphne
{
    //public class Vector
    //{
    //    public int Dim;
    //    public Vector(int dim)
    //    {
    //        Dim = dim;
    //        array = new double[dim];
    //    }

    //    public Vector(double[] _array)
    //    {
    //        Dim = _array.Length;
    //        array = (double[])_array.Clone();
    //    }

    //    private double[] array;
    //    public double this[int i]
    //    {
    //        get { return array[i]; }
    //        set { array[i] = value; }
    //    }

    //    public double[] ToDouble()
    //    {
    //        return array;
    //    }

    //    public static Vector operator +(Vector a, Vector b)
    //    {
    //        if (a.Dim != b.Dim)
    //        {
    //            throw (new Exception("Vector dimension mismatch."));
    //        }

    //        Vector sum = new Vector(a.Dim);
    //        for (int i = 0; i < a.Dim; i++)
    //        {
    //            sum[i] = a[i] + b[i];
    //        }

    //        return sum;
    //    }

    //    public static Vector operator -(Vector a, Vector b)
    //    {
    //        if (a.Dim != b.Dim)
    //        {
    //            throw (new Exception("Vector dimension mismatch."));
    //        }

    //        Vector difference = new Vector(a.Dim);
    //        for (int i = 0; i < a.Dim; i++)
    //        {
    //            difference[i] = a[i] - b[i];
    //        }

    //        return difference;
    //    }

    //    public static Vector operator *(double a, Vector b)
    //    {
    //        Vector product = new Vector(b.Dim);
    //        for (int i = 0; i < b.Dim; i++)
    //        {
    //            product[i] = a * b[i];
    //        }

    //        return product;
    //    }

    //    public static Vector operator *(Vector a, Vector b)
    //    {
    //        Vector product = new Vector(b.Dim);
    //        for (int i = 0; i < b.Dim; i++)
    //        {
    //            product[i] = a[i] * b[i];
    //        }

    //        return product;
    //    }

    //}

    public class VectorField
    {
        public DiscretizedManifold M;
        public Vector[] vector;

        public Vector Value(int i)
        {
            return this[i];
        }

        public Vector Value(double[] point)
        {
            LocalMatrix[] lm = M.Interpolation(point);
            Vector value = new Vector(M.Dim);

            if (lm != null)
            {
                for (int j = 0; j < M.Dim; j++)
                {
                    for (int i = 0; i < lm.Length; i++)
                    {
                        value[j] = value[j] + ( lm[i].Coefficient * vector[lm[i].Index][j]);
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// The dimension of each vector in the vector field is determined by the dimension of the manifold
        /// </summary>
        /// <param name="m">Manifold on which the vector field resides</param>
        public VectorField(DiscretizedManifold m)
        {
            M = m;
            vector = new Vector[M.ArraySize];

            for (int i = 0; i < M.ArraySize; i++)
            {
                vector[i] = new Vector(M.Dim);
            }
        }
        
        /// <summary>
        /// The dimension of each vector in the vector field is determined by the integer input parameter
        /// </summary>
        /// <param name="m">Manifold on which the vector field resides</param>
        /// <param name="i">The dimension of the vectors</param>
        public VectorField(DiscretizedManifold m, int dim)
        {
            M = m;
            vector = new Vector[M.ArraySize];

            for (int i = 0; i < M.ArraySize; i++)
            {
                vector[i] = new Vector(dim);
            }
        }

        public Vector this[int i]
        {
            get { return vector[i]; }
            set { vector[i] = value; }
        }

        public static VectorField operator +(VectorField a, VectorField b)
        {
            if (a.M != b.M)
            {
                throw (new Exception("Manifolds must be identical for scalar field addition."));
            }

            VectorField c = new VectorField(a.M);

            for (int i = 0; i < a.M.ArraySize; i++)
            {
                c.vector[i] = a.vector[i] + b.vector[i];
            }

            return c;
        }

        public static VectorField operator -(VectorField a, VectorField b)
        {
            if (a.M != b.M)
            {
                throw (new Exception("Manifolds must be identical for scalar field addition."));
            }

            VectorField c = new VectorField(a.M);

            for (int i = 0; i < a.M.ArraySize; i++)
            {
                c.vector[i] = a.vector[i] - b.vector[i];
            }

            return c;
        }

        public static VectorField operator *(double a, VectorField b)
        {
            VectorField product = new VectorField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                product[i] = a * b[i];
            }

            return product;
        }

        public static VectorField operator *(ScalarField a, VectorField b)
        {
            VectorField product = new VectorField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                product[i] = a.array[i] * b[i];
            }

            return product;
        }
        
        public static VectorField operator *(VectorField a, VectorField b)
        {
            VectorField product = new VectorField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                for (int j = 0; j < b.M.Dim; j++)
                {
                    product[i][j] = a[i][j] * b[i][j];
                }
            }

            return product;
        }

        public static VectorField operator /(VectorField a, ScalarField b)
        {
            VectorField product = new VectorField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                product[i] = a[i] / b.array[i];
            }

            return product;
        }

    }


    public abstract class Manifold
    {
        public int Dim;
    }

    public abstract class DiscretizedManifold : Manifold
    {
        public int ArraySize;
        public LocalMatrix[][] Laplacian;
        public Dictionary<Manifold,Embedding> Boundaries;
        public int[] NumPoints;
        protected LocalMatrix[] interpolator;
        protected LocalMatrix[][] gradientOperator;

        public double[,] Coordinates;
        // extent in each dimension
        public double[] Extents;
        public double[] StepSize;

        public abstract LocalMatrix[] Interpolation(double[] point);
        public abstract LocalMatrix[][] GradientOperator(int index);

        public virtual int[] localToArr(double[] loc)
        {
            if (loc == null)
            {
                return null;
            }
            return new int[1];
        }

        public virtual int arrToIndex(int[] arr)
        {
            if (arr == null)
            {
                return -1;
            }
            return 0;
        }

        // gmk NOTE: Calculate the total quantity on the manifold using a simple integration algorithm.
        // Used to test diffusion with zero flux boundary conditions for "leaks".
        // TinySphere.Integrate(s) returns s*4*pi*r^2
        // TinyBall.Integrate(s) returns s*4*pi*r^3/3
        public abstract double Integrate(ScalarField s);

    }

    public class ScalarField
    {
        public DiscretizedManifold M;
        public double Value(double[] point)
        {
            LocalMatrix[] lm = M.Interpolation(point);
            double value = 0;

            if (lm != null)
            {
                for (int i = 0; i < lm.Length; i++)
                {                   
                    value += lm[i].Coefficient * array[lm[i].Index];

                    //System.Console.WriteLine(lm[i].Index + "\t" + lm[i].Coefficient + "\t"
                    //        + M.Coordinates[lm[i].Index, 0] + ", " + M.Coordinates[lm[i].Index, 1] + ", " + M.Coordinates[lm[i].Index, 2]);
                }
            }

            return value;
        }

        public double[] array;

        public ScalarField(DiscretizedManifold m)
        {
            M = m;
            array = new double[M.ArraySize];
        }

        public ScalarField(DiscretizedManifold m, double c)
        {
            M = m;
            array = new double[M.ArraySize];
            for (int i = 0; i < M.ArraySize; i++)
            {
                array[i] = c;
            }
        }

        public static ScalarField operator +(ScalarField a, ScalarField b)
        {
            if (a.M != b.M)
            {
                throw (new Exception("Manifolds must be identical for scalar field addition."));
            }

            ScalarField c = new ScalarField(a.M);
            
            for (int i = 0; i < a.M.ArraySize; i++)
            {
                c.array[i] = a.array[i] + b.array[i];                
            }

            return c;
        }

        public static ScalarField operator -(ScalarField a, ScalarField b)
        {
            if (a.M != b.M)
            {
                throw (new Exception("Manifolds must be identical for scalar field addition."));
            }

            ScalarField c = new ScalarField(a.M);

            for (int i = 0; i < a.M.ArraySize; i++)
            {
                c.array[i] = a.array[i] - b.array[i];
            }

            return c;
        }

        public static ScalarField operator *(double a, ScalarField b)
        {
            ScalarField c = new ScalarField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                c.array[i] = a * b.array[i];
            }

            return c;
        }

        public ScalarField MultInPlace(double f)
        {
            for (int i = 0; i < M.ArraySize; i++)
            {
                array[i] *= f;
            }
            return this;
        }

        public static ScalarField operator *(ScalarField a, ScalarField b)
        {
            if (a.M != b.M)
            {
                throw (new Exception("Manifolds must be identical for scalar field multiplication."));
            }

            ScalarField c = new ScalarField(a.M);

            for (int i = 0; i < a.M.ArraySize; i++)
            {
                c.array[i] = a.array[i] * b.array[i];
            }

            return c;
        }

        public static ScalarField operator +(double a, ScalarField b)
        {
            ScalarField c = new ScalarField(b.M);
            for (int i = 0; i < b.M.ArraySize; i++)
            {
                c.array[i] = a + b.array[i];
            }

            return c;
        }


        public void WriteToFile(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                int n = 0;

                for (int i = 0; i < M.ArraySize; i++)
                {
                    writer.Write(n + "\t");

                    for (int j = 0; j < M.Dim; j++)
                    {
                        writer.Write(M.Coordinates[n,j] + "\t");
                    }
                    writer.Write(array[n] + "\n");

                    n++;
                }
            
            }
            return;
        }

    }

    public class GaussianScalarField : ScalarField
    {
        public GaussianScalarField(DiscretizedManifold m) : base(m)
        {

        }

        /// <summary>
        /// Calculate and return values of a Gaussian scalar field at each array point in the manifold
        /// The value at the center is max
        /// </summary>
        public bool Initialize(double[] x0, double[] sigma, double max)
        {
            if (M.Dim != x0.Length || M.Dim != sigma.Length)
            {
                return false;
            }

            double f=0, d = 1.0;
            // double d = Math.Pow(2.0 * Math.PI, 1.5) * sigma[0] * sigma[1] * sigma[2];

            for (int i = 0; i < M.ArraySize; i++)
            {
                f = 0;
                for (int j = 0; j < M.Dim; j++)
                {
                    f += (x0[j] - M.Coordinates[i, j]) * (x0[j] - M.Coordinates[i, j]) / (2 * sigma[j] * sigma[j]);
                }
                array[i] = max * Math.Exp(-f) / d;
            }

            return true;
        }

    }

    public class TinySphere : DiscretizedManifold
    {
        public TinySphere()
        {
            Dim = 0;
            ArraySize = 1;
            //Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1, 1];
            Coordinates[0, 0] = 0.0;
            //StepSize = new double[1] { 1.0 };
            gradientOperator = new LocalMatrix[1][];
            gradientOperator[0] = new LocalMatrix[1];
            gradientOperator[0][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
        }

        public TinySphere(double[] extent)
        {
            // The radius of the sphere
            Extents = (double[])extent.Clone();

            Dim = 0;
            ArraySize = 1;
            //Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1, 1];
            Coordinates[0, 0] = 0.0;
            //StepSize = new double[1] { 1.0 };
            gradientOperator = new LocalMatrix[1][];
            gradientOperator[0] = new LocalMatrix[1];
            gradientOperator[0][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }

        public override LocalMatrix[][] GradientOperator(int index)
        {
            return gradientOperator;
        }

        public override double Integrate(ScalarField s)
        {
            return s.array[0] * Math.PI * Extents[0] * Extents[0];
        }
    }

    public class TinyBall : DiscretizedManifold
    {
        public TinyBall()
        {
            Dim = 0;
            ArraySize = 1;
            //Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1, 1];
            Coordinates[0, 0] = 0.0;
            //StepSize = new double[1] { 1.0 };
            gradientOperator = new LocalMatrix[1][];
            gradientOperator[0] = new LocalMatrix[1];
            gradientOperator[0][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
        }

        public TinyBall(double[] extent)
        {
            // The radius of the sphere
            Extents = (double[])extent.Clone();

            Dim = 0;
            ArraySize = 1;
            //Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1, 1];
            Coordinates[0, 0] = 0.0;
            //StepSize = new double[1] { 1.0 };
            gradientOperator = new LocalMatrix[1][];
            gradientOperator[0] = new LocalMatrix[1];
            gradientOperator[0][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
        }


        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }

        public override LocalMatrix[][] GradientOperator(int index)
        {
            return gradientOperator;
        }

        public override double Integrate(ScalarField s)
        {
            return s.array[0] * 4.0 * Math.PI * Extents[0] * Extents[0] * Extents[0] / 3.0;
        }

    }
/*
    public class Rectangle : DiscretizedManifold
    {
        public Rectangle()
        {
            Dim = 2;
            //NumPoints = null;
            ArraySize = 1;

            //Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public Rectangle(int[] numGridPts)
        {
            Dim = 2;
            Debug.Assert(Dim == numGridPts.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1];

            Boundaries = new Dictionary<Manifold, Embedding>();
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }
    }

    public class RectangularPrism : DiscretizedManifold
    {
        public RectangularPrism(int[] numGridPts)
        {
            Dim = 3;
            Debug.Assert(Dim == numGridPts.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1] * NumPoints[2];

            Boundaries = new Dictionary<Manifold,Embedding>();
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }
    }
*/
    public class BoundedRectangle : DiscretizedManifold
    {
        public BoundedRectangle(int[] numGridPts, double[] extent)
        {
            Dim = 2;
            Debug.Assert(Dim == numGridPts.Length && Dim == extent.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1];
            Boundaries = new Dictionary<Manifold, Embedding>();
            Laplacian = new LocalMatrix[ArraySize][];
            interpolator = new LocalMatrix[4];

            gradientOperator = new LocalMatrix[Dim][];
            for (int i = 0; i < Dim; i++)
            {
                gradientOperator[i] = new LocalMatrix[2];
            }

            Coordinates = new double[ArraySize, 2];
            Extents = (double[])extent.Clone();

            StepSize = new double[Dim];
            StepSize[0] = Extents[0] / (NumPoints[0] - 1);
            StepSize[1] = Extents[1] / (NumPoints[1] - 1);

            int n = 0;
            for (int j = 0; j < NumPoints[1]; j++)
            {
                for (int i = 0; i < NumPoints[0]; i++)
                {
                    Coordinates[n, 0] = i * StepSize[0];
                    Coordinates[n, 1] = j * StepSize[1];
                    n++;
                }
            }

            // The Laplacian with zero gradient boundary conditions 

            for (n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[5];
            }
            n = 0;

            int idxplus, idxminus;
            double coeff0, coeff;

            for (int j = 0; j < NumPoints[1]; j++)
            {
                for (int i = 0; i < NumPoints[0]; i++)
                {
                    // Laplacian index n corresponds to grid indices (i,j)

                        Laplacian[n][0].Coefficient = 0;
                        Laplacian[n][0].Index = i + j * NumPoints[0];


                        coeff0 = -2.0 / (StepSize[0] * StepSize[0]);
                        coeff = 1.0 / (StepSize[0] * StepSize[0]);

                        if (i == 0)
                        {
                            idxplus = (i + 1) + j * NumPoints[0];
                            idxminus = idxplus;
                        }
                        else if (i == NumPoints[0] - 1)
                        {
                            idxminus = (i - 1) + j * NumPoints[0];
                            idxplus = idxminus;
                        }
                        else
                        {
                            idxplus = (i + 1) + j * NumPoints[0];
                            idxminus = (i - 1) + j * NumPoints[0];
                        }

                        // (i+1), j
                        Laplacian[n][1].Coefficient = coeff;
                        Laplacian[n][1].Index = idxplus;

                        // (i-1), j
                        Laplacian[n][2].Coefficient = coeff;
                        Laplacian[n][2].Index = idxminus;

                        // i,j
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        coeff0 = -2.0 / (StepSize[1] * StepSize[1]);
                        coeff = 1.0 / (StepSize[1] * StepSize[1]);

                        if (j == 0)
                        {
                            idxplus = i + (j + 1) * NumPoints[0];
                            idxminus = idxplus;
                        }
                        else if (j == NumPoints[1] - 1)
                        {
                            idxminus = i + (j - 1) * NumPoints[0];
                            idxplus = idxminus;
                        }
                        else
                        {
                            idxplus = i + (j + 1) * NumPoints[0];
                            idxminus = i + (j - 1) * NumPoints[0];
                        }

                        // i, (j+1)
                        Laplacian[n][3].Coefficient = coeff;
                        Laplacian[n][3].Index = idxplus;

                        // i, (j-1)
                        Laplacian[n][4].Coefficient = coeff;
                        Laplacian[n][4].Index = idxminus;

                        // i,j
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        n++;
                }
            }
        }

        public override int[] localToArr(double[] loc)
        {
            if (loc != null)
            {
                return new int[] { (int)(loc[0] / StepSize[0]), (int)(loc[1] / StepSize[1]) };
            }
            return null;
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr != null)
            {
                return arr[0] + arr[1] * NumPoints[0];
            }
            return -1;
        }

        // TODO: This needs to be checked for correctness
        // uses en.wikipedia.org/wiki/Bilinear_interpolation
        public override LocalMatrix[] Interpolation(double[] point)
        {
            // Linear interpolation
            // The values in point are in terms of manifold coordinates

            int[] idx = localToArr(point);

            if (idx == null)
            {
                return null;
            }

            int i = idx[0];
            int j = idx[1];

            if ((i < 0) || (i > NumPoints[0] - 1) || (j < 0) || (j > NumPoints[1] - 1))
            {
                return null;
            }

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1
            // Similarly, for j and k.
            if (i == NumPoints[0] - 1)
            {
                i--;
            }
            if (j == NumPoints[1] - 1)
            {
                j--;
            }

            double dx = point[0] / StepSize[0] - i;
            double dy = point[1] / StepSize[1] - j;

            // 00
            interpolator[0].Coefficient = (1 - dx) * (1 - dy);
            interpolator[0].Index = i + j * NumPoints[0];
            // 10
            interpolator[1].Coefficient = dx * (1 - dy);
            interpolator[1].Index = (i + 1) + j * NumPoints[0];
            // 11
            interpolator[2].Coefficient = dx * dy ;
            interpolator[2].Index = (i + 1) + (j + 1) * NumPoints[0];
            // 01
            interpolator[3].Coefficient = (1 - dx) * dy;
            interpolator[3].Index = i + (j + 1) * NumPoints[0];

            return interpolator;
        }
        public override LocalMatrix[][] GradientOperator(int index)
        {

            int j = (int)(index / NumPoints[0]);
            int i = (int)(index - j * NumPoints[0]);

            if ((i < 0) || (i > NumPoints[0] - 1) || (j < 0) || (j > NumPoints[1] - 1))
            {
                return null;
            }

            double fx = 1.0 / (2 * StepSize[0]);
            double fy = 1.0 / (2 * StepSize[1]);

            if (i == NumPoints[0] - 1)
            {
                // ( c[i,j] - c[i-1,j] ) / dx
                gradientOperator[0][0].Index = i + j * NumPoints[0];
                gradientOperator[0][0].Coefficient = 2 * fx;
                gradientOperator[0][1].Index = (i - 1) + j * NumPoints[0];
                gradientOperator[0][1].Coefficient = -2 * fx;
            }
            else if (i == 0)
            {
                // ( c[i+1,j] - c[i,j] ) / dx
                gradientOperator[0][0].Index = (i + 1) + j * NumPoints[0];
                gradientOperator[0][0].Coefficient = 2 * fx;
                gradientOperator[0][1].Index = i + j * NumPoints[0];
                gradientOperator[0][1].Coefficient = -2 * fx;
            }
            else
            {
                // ( c[i+1,j] - c[i-1,j] ) / 2*dx
                gradientOperator[0][0].Index = (i + 1) + j * NumPoints[0];
                gradientOperator[0][0].Coefficient = fx;
                gradientOperator[0][1].Index = (i - 1) + j * NumPoints[0];
                gradientOperator[0][1].Coefficient = -fx;
            }


            if (j == NumPoints[1] - 1)
            {
                // ( c[i,j] - c[i,j-1] ) / dy
                gradientOperator[1][0].Index = i + j * NumPoints[0];
                gradientOperator[1][0].Coefficient = 2 * fy;
                gradientOperator[1][1].Index = i + (j - 1) * NumPoints[0];
                gradientOperator[1][1].Coefficient = -2 * fy;
            }
            else if (j == 0)
            {
                // ( c[i,j+1] - c[i,j] ) / dy
                gradientOperator[1][0].Index = i + (j + 1) * NumPoints[0];
                gradientOperator[1][0].Coefficient = 2 * fy;
                gradientOperator[1][1].Index = i + j * NumPoints[0];
                gradientOperator[1][1].Coefficient = -2 * fy;
            }
            else
            {
                // ( c[i,j+1] - c[i,j-1] ) / 2*dy
                gradientOperator[1][0].Index = i + (j + 1) * NumPoints[0];
                gradientOperator[1][0].Coefficient = fy;
                gradientOperator[1][1].Index = i + (j - 1) * NumPoints[0];
                gradientOperator[1][1].Coefficient = -fy;
            }

            return gradientOperator;
        }

        public override double Integrate(ScalarField s)
        {
            double[] point = new double[Dim];
            double quantity = 0;
            int index;
            double voxel = StepSize[0] * StepSize[1];

                for (int j = 0; j < NumPoints[1] - 1; j++)
                {
                    for (int i = 0; i < NumPoints[0] - 1; i++)
                    {
                        index = i + j * NumPoints[0];
                        point[0] = Coordinates[index, 0] + StepSize[0] / 2.0;
                        point[1] = Coordinates[index, 1] + StepSize[1] / 2.0;

                        // The value at the center of the voxel
                        quantity += s.Value(point);
                    }
                }

            return quantity * voxel;
        }

    }

    public class BoundedRectangularPrism : DiscretizedManifold
    {
        public BoundedRectangularPrism(int[] numGridPts, double[] extent)
        {
            Dim = 3;
            Debug.Assert(Dim == numGridPts.Length && Dim == extent.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1] * NumPoints[2];
            Coordinates = new double[ArraySize, 3];
            Boundaries = new Dictionary<Manifold, Embedding>();
            Laplacian = new LocalMatrix[ArraySize][];
            interpolator = new LocalMatrix[8];

            gradientOperator = new LocalMatrix[Dim][];
            for (int i = 0; i < Dim; i++)
            {
                gradientOperator[i] = new LocalMatrix[2];
            }

            Extents = (double[])extent.Clone();

            StepSize = new double[Dim];
            StepSize[0] = Extents[0] / (NumPoints[0] - 1);
            StepSize[1] = Extents[1] / (NumPoints[1] - 1);
            StepSize[2] = Extents[2] / (NumPoints[2] - 1);

            int n = 0;

            for (int k = 0; k < NumPoints[2]; k++)
            {
                for (int j = 0; j < NumPoints[1]; j++)
                {
                    for (int i = 0; i < NumPoints[0]; i++)
                    {
                        Coordinates[n, 0] = i * StepSize[0];
                        Coordinates[n, 1] = j * StepSize[1];
                        Coordinates[n, 2] = k * StepSize[2];
                        n++;
                    }
                }
            }

            // The Laplacian with zero gradient boundary conditions 

            for (n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[7];
            }

            n = 0;

            int idxplus, idxminus;
            double coeff0, coeff;
            int     N01 = NumPoints[0] * NumPoints[1];

            for (int k = 0; k < NumPoints[2]; k++)
            {
                for (int j = 0; j < NumPoints[1]; j++)
                {
                    for (int i = 0; i < NumPoints[0]; i++)
                    {

                        // Laplacian index n corresponds to grid indices (i,j,k)

                        Laplacian[n][0].Coefficient = 0;
                        Laplacian[n][0].Index = i + j * NumPoints[0] + k * N01;


                        coeff = 1.0 / (StepSize[0] * StepSize[0]);
                        coeff0 = -2.0 * coeff;

                        if (i == 0) 
                        {
                            idxplus = (i + 1) + j * NumPoints[0] + k * N01;
                            idxminus = idxplus;
                        }
                        else if (i == NumPoints[0] - 1)
                        {
                            idxminus = (i - 1) + j * NumPoints[0] + k * N01;
                            idxplus = idxminus;
                        }
                        else
                        {
                            idxplus = (i + 1) + j * NumPoints[0] + k * N01;
                            idxminus = (i - 1) + j * NumPoints[0] + k * N01;
                        }

                        // (i+1), j, k
                        Laplacian[n][1].Coefficient = coeff;
                        Laplacian[n][1].Index = idxplus;

                        // (i-1), j, k
                        Laplacian[n][2].Coefficient = coeff;
                        Laplacian[n][2].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;


                        coeff = 1.0 / (StepSize[1] * StepSize[1]);
                        coeff0 = -2.0 * coeff;

                        if (j == 0)
                        {
                            idxplus = i + (j + 1) * NumPoints[0] + k * N01;
                            idxminus = idxplus;
                        }
                        else if (j == NumPoints[1] - 1)
                        {
                            idxminus = i + (j - 1) * NumPoints[0] + k * N01;
                            idxplus = idxminus;
                        }
                        else
                        {
                            idxplus = i + (j + 1) * NumPoints[0] + k * N01;
                            idxminus = i + (j - 1) * NumPoints[0] + k * N01;
                        }

                        // i, (j+1), k
                        Laplacian[n][3].Coefficient = coeff;
                        Laplacian[n][3].Index = idxplus;

                        // i, (j-1), k
                        Laplacian[n][4].Coefficient = coeff;
                        Laplacian[n][4].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;


                        coeff = 1.0 / (StepSize[2] * StepSize[2]);
                        coeff0 = -2.0 * coeff;

                        if (k == 0)
                        {
                            idxplus = i + k * NumPoints[0] + (k + 1) * N01;
                            idxminus = idxplus;
                        }
                        else if (k == NumPoints[2] - 1)
                        {
                            idxminus = i + j * NumPoints[0] + (k - 1) * N01;
                            idxplus = idxminus;
                        }
                        else
                        {
                            idxplus = i + j * NumPoints[0] + (k + 1) * N01;
                            idxminus = i + j * NumPoints[0] + (k - 1) * N01;
                        }


                        // i, j, (k+1)
                        Laplacian[n][5].Coefficient = coeff;
                        Laplacian[n][5].Index = idxplus;

                        // i, j, (k-1)
                        Laplacian[n][6].Coefficient = coeff ;
                        Laplacian[n][6].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        n++;
                    }
                }
            }

            // NOTE: We don't require instantiation of these manifolds nor do we require the embeddings,
            // since we are using zero flux boundary conditions for diffusion in the extracellular medium.
            // Therefore, it's best to remove them. Otherwise, we waste time updating boundary concentrations
            // for these embedded manifolds at each time step.

            //double[] rectSpatialExtents;
            //int[] numRectGridPts = new int[2];

            //rectSpatialExtents = new double[2] { Extents[0], Extents[1] };
            //numRectGridPts[0] = numGridPts[0];
            //numRectGridPts[1] = numGridPts[1];
            //DiscretizedManifold xyLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            //DiscretizedManifold xyUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            //rectSpatialExtents = new double[2] { Extents[0], Extents[2] };
            //numRectGridPts[0] = numGridPts[0];
            //numRectGridPts[1] = numGridPts[2];
            //DiscretizedManifold xzLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            //DiscretizedManifold xzUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            //rectSpatialExtents = new double[2] { Extents[1], Extents[2] };
            //numRectGridPts[0] = numGridPts[1];
            //numRectGridPts[1] = numGridPts[2];
            //DiscretizedManifold yzLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            //DiscretizedManifold yzUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            //// Position of rectangle origin in the BoundedRectangularPrism
            //double[] origin;

            //// Mapping of dimension in rectangle to dimensions in the BoundedRectangularPrism
            //int[] dimensionsMap;

            //dimensionsMap = new int[2] { 0, 1 };
            //// xyLower
            //origin = new double[3] { 0, 0, 0 };
            //DirectTranslEmbedding xyLowerEmbed = new DirectTranslEmbedding(xyLower, this, dimensionsMap, origin);
            //// xyUpper
            //origin = new double[3] { 0, 0, Extents[2] };
            //DirectTranslEmbedding xyUpperEmbed = new DirectTranslEmbedding(xyUpper, this, dimensionsMap, origin);

            //dimensionsMap = new int[2] { 0, 2 };
            //// xzLower
            //origin = new double[3] { 0, 0, 0 };
            //DirectTranslEmbedding xzLowerEmbed = new DirectTranslEmbedding(xzLower, this, dimensionsMap, origin);
            //// xzUpper
            //origin = new double[3] { 0, Extents[1], 0 };
            //DirectTranslEmbedding xzUpperEmbed = new DirectTranslEmbedding(xzUpper, this, dimensionsMap, origin);

            //dimensionsMap = new int[2] { 1, 2 };
            //// yzLower
            //origin = new double[3] { 0, 0, 0 };
            //DirectTranslEmbedding yzLowerEmbed = new DirectTranslEmbedding(yzLower, this, dimensionsMap, origin);
            //// yzLower
            //origin = new double[3] { Extents[0], 0, 0 };
            //DirectTranslEmbedding yzUpperEmbed = new DirectTranslEmbedding(yzUpper, this, dimensionsMap, origin);

            //Boundaries.Add(xyLower, xyLowerEmbed);
            //Boundaries.Add(xyUpper, xyUpperEmbed);
            //Boundaries.Add(xzLower, xzLowerEmbed);
            //Boundaries.Add(xzUpper, xzUpperEmbed);
            //Boundaries.Add(yzLower, yzLowerEmbed);
            //Boundaries.Add(yzUpper, yzUpperEmbed);
        }

        public override int[] localToArr(double[] loc)
        {
            if (loc != null)
            {
                return new int[] { (int)(loc[0] / StepSize[0]), (int)(loc[1] / StepSize[1]), (int)(loc[2] / StepSize[2]) };
            }
            return null;
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr != null)
            {
                return arr[0] + arr[1] * NumPoints[0] + arr[2] * NumPoints[0] * NumPoints[1];
            }
            return -1;
        }

        // TODO: This needs to be checked for correctness
        // uses paulbourke.net/miscellaneous/interpolation/
        public override LocalMatrix[] Interpolation(double[] point)
        {
            // Linear interpolation
            // The values in point are in terms of manifold coordinates

            int[] idx = localToArr(point);

            if (idx == null)
            {
                return null;
            }

            int i = idx[0];
            int j = idx[1];
            int k = idx[2];

            if ((i < 0) || (i > NumPoints[0] - 1) || (j < 0) || (j > NumPoints[1] - 1) || (k < 0) || (k > NumPoints[2] - 1))
            {
                return null;
            }

            // When i == NumPoints[0] - 1, we can't look at the (i + 1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1
            // Similarly, for j and k.
            if (i == NumPoints[0] - 1)
            {
                i--;
            }
            if (j == NumPoints[1] - 1)
            {
                j--;
            }
            if (k == NumPoints[2] - 1)
            {
                k--;
            }
 
            double dx = point[0] / StepSize[0] - i;
            double dy = point[1] / StepSize[1] - j;
            double dz = point[2] / StepSize[2] - k;

            // 000
            interpolator[0].Coefficient = (1 - dx) * (1 - dy) * (1 - dz);
            interpolator[0].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            // 100
            interpolator[1].Coefficient = dx * (1 - dy) * (1 - dz);
            interpolator[1].Index = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            // 110
            interpolator[2].Coefficient = dx * dy * (1 - dz);
            interpolator[2].Index = (i + 1) + (j + 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            // 101
            interpolator[3].Coefficient = dx * (1 - dy) * dz;
            interpolator[3].Index = (i + 1) + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
            // 010
            interpolator[4].Coefficient = (1 - dx) * dy * (1 - dz);
            interpolator[4].Index = i + (j + 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            // 011
            interpolator[5].Coefficient = (1 - dx) * dy * dz;
            interpolator[5].Index = i + (j + 1) * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
            // 001
            interpolator[6].Coefficient = (1 - dx) * (1 - dy) * dz;
            interpolator[6].Index = i + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
            // 111
            interpolator[7].Coefficient = dx * dy * dz;
            interpolator[7].Index = (i + 1) + (j + 1) * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];

            return interpolator;
        }


        //public override LocalMatrix[][] GradientOperator(double[] point)
        /// <summary>
        /// Return the local gradient stencil for array point n
        /// </summary>
        /// <param name="n">index into array</param>
        /// <returns>LocalMatrix for interpolating the local gradient</returns>
        public override LocalMatrix[][] GradientOperator(int index)
        {
            // Linear estimate of gradient

            int k = (int)(index / (NumPoints[0] * NumPoints[1]));
            int j = (int)((index - k * NumPoints[0] * NumPoints[1]) / NumPoints[0]);
            int i = (int)(index - k * NumPoints[0] * NumPoints[1] - j * NumPoints[0]);

            //System.Console.WriteLine(index + "\t" + Coordinates[index, 0] + ", " + Coordinates[index, 1] + ", " + Coordinates[index, 2]
            //                        + "\t" + i + ", " + j + ", " + k);

            if ((i < 0) || (i > Extents[0] - 1) || (j < 0) || (j > Extents[1] - 1) || (k < 0) || (k > Extents[2] - 1))
            {
                return null;
            }

            double fx = 1.0 / (2 * StepSize[0]);
            double fy = 1.0 / (2 * StepSize[1]);
            double fz = 1.0 / (2 * StepSize[2]);

            if (i == NumPoints[0] - 1)
            {
                // ( c[i,j] - c[i-1,j] ) / dx
                gradientOperator[0][0].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][0].Coefficient = 2 * fx;
                gradientOperator[0][1].Index = (i - 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][1].Coefficient = -2 * fx;
            }
            else if (i == 0)
            {
                // ( c[i+1,j] - c[i,j] ) / dx
                gradientOperator[0][0].Index = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][0].Coefficient = 2 * fx;
                gradientOperator[0][1].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][1].Coefficient = -2 * fx;
            }
            else
            {
                // ( c[i+1,j] - c[i-1,j] ) / 2*dx
                gradientOperator[0][0].Index = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][0].Coefficient = fx;
                gradientOperator[0][1].Index = (i - 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[0][1].Coefficient = -fx;
            }


            if (j == NumPoints[1] - 1)
            {
                // ( c[i,j] - c[i,j-1] ) / dy
                gradientOperator[1][0].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][0].Coefficient = 2 * fy;
                gradientOperator[1][1].Index = i + (j - 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][1].Coefficient = -2 * fy;
            }
            else if (j == 0)
            {
                // ( c[i,j+1] - c[i,j] ) / dy
                gradientOperator[1][0].Index = i + (j + 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][0].Coefficient = 2 * fy;
                gradientOperator[1][1].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][1].Coefficient = -2 * fy;
            }
            else
            {
                // ( c[i,j+1] - c[i,j-1] ) / 2*dy
                gradientOperator[1][0].Index = i + (j + 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][0].Coefficient = fy;
                gradientOperator[1][1].Index = i + (j - 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[1][1].Coefficient = -fy;
            }

            if (k == NumPoints[2] - 1)
            {
                // ( c[i,j,k] - c[i,j,k-1] ) / dz
                gradientOperator[2][0].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[2][0].Coefficient = 2 * fz;
                gradientOperator[2][1].Index = i + j * NumPoints[0] + (k - 1) * NumPoints[0] * NumPoints[1];
                gradientOperator[2][1].Coefficient = -2 * fy;
            }
            else if (k == 0)
            {
                // ( c[i,j,k+1] - c[i,j,k] ) / dz
                gradientOperator[2][0].Index = i + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
                gradientOperator[2][0].Coefficient = 2 * fz;
                gradientOperator[2][1].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                gradientOperator[2][1].Coefficient = -2 * fz;
            }
            else
            {
                // ( c[i,j,k+1] - c[i,j,k-1] ) / 2*dz
                gradientOperator[2][0].Index = i + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
                gradientOperator[2][0].Coefficient = fz;
                gradientOperator[2][1].Index = i + j * NumPoints[0] + (k - 1) * NumPoints[0] * NumPoints[1];
                gradientOperator[2][1].Coefficient = -fz;
            }

            return gradientOperator;
        }

        /// <summary>
        /// Builds a stencil for diffusion of the gradient (of a molecular population).
        /// Zero flux boundary conditions equate to (zero) Dirichlet boundary conditions for the gradient.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>LocalVectorMatrix[Dim][] GradDiffStencil</returns>
        public LocalVectorMatrix[][] GradDiffusionStencil(int index)
        {
            // Return the diffusion stencil for the nth component of the gradient at 
            // the grid point corresponding to index

            LocalVectorMatrix[][] GradDiffStencil = new LocalVectorMatrix[Dim][];

            // TODO: complete this

            //int k = (int)(index / (Extents[0] * Extents[1]));
            //int j = (int)(index / Extents[0]);
            //int i = (int)(index - k * Extents[0] * Extents[1] - j * Extents[0]);

            //if ((i < 0) || (i > Extents[0] - 1) || (j < 0) || (j > Extents[1] - 1) || (k < 0) || (k > Extents[2] - 1))
            //{
            //    return null;
            //}

            //LocalVectorMatrix[] lm = new LocalVectorMatrix[11];
            //double h0, h1, h2;
            //int idx, idxplus, idxminus;

            //GradDiffStencil[0] = new LocalVectorMatrix[11];

            //// Stencil for component 0 (x-direction)

            //h0 = 1.0 / (StepSize[0] * StepSize[0]);
            //h1 = 1.0 / (4 * StepSize[0] * StepSize[1]);
            //h2 = 1.0 / (4 * StepSize[0] * StepSize[2]);

            //if (i == 0)
            //{
            //    idxplus = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            //    idxminus = idxplus;
            //}
            //else if (i == NumPoints[0] - 1)
            //{
            //    idxminus = (i - 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            //    idxplus = idxminus;
            //}
            //else
            //{
            //    lm = new LocalVectorMatrix[11];
            //    idx = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];

            //    idxplus = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            //    idxminus = (i - 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
            //    lm[0] = new LocalVectorMatrix() { Index = idx, Coefficient = -2 * h0, Component = 0 };
            //    lm[1].Index = idxplus;
            //    lm[1].Coefficient = h1;
            //    lm[1].Component = 0;
            //    lm[2].Index = idxminus;
            //    lm[2].Coefficient = h2;
            //    lm[2].Component = 0;

            //    lm[3].Index = idxminus;
            //    lm[3].Coefficient = h2;
            //    lm[3].Component = 0;

            //}

            return GradDiffStencil;
        }

        /// <summary>
        /// A simple integration scheme.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public override double Integrate(ScalarField s)
        {
            double[] point = new double[Dim];
            double quantity = 0;
            int index;
            double voxel = StepSize[0] * StepSize[1] * StepSize[2];

            for (int k = 0; k < NumPoints[2] - 1; k++)
            {
                for (int j = 0; j < NumPoints[1] - 1; j++)
                {
                    for (int i = 0; i < NumPoints[0] - 1; i++)
                    {
                        index = i + j*NumPoints[0] + k*NumPoints[0]*NumPoints[1];
                        point[0] = Coordinates[index, 0] + StepSize[0] / 2.0;
                        point[1] = Coordinates[index, 1] + StepSize[1] / 2.0;
                        point[2] = Coordinates[index, 2] + StepSize[2] / 2.0;

                        // The value at the center of the voxel
                        quantity += s.Value(point);
                    }
                }
            }
            return quantity*voxel;
        }

    }   

    /// <summary>
    /// LocalMatrix is a struct to facilitate local matrix algebra on a lattice by providing an efficient
    /// representation of a sparse matrix. 
    /// </summary>
    public struct LocalMatrix
    {
        public int Index;
        public double Coefficient;
    }

    /// <summary>
    /// LocalVectorMatrix is similar to local matrix, but indicates which component of a vector to use 
    /// </summary>
    public struct LocalVectorMatrix 
    {
        public int Index;
        public double Coefficient;
        public int Component;
    }


    abstract public class Embedding
    {
        public DiscretizedManifold Domain;
        public DiscretizedManifold Range;

        //// For a point in the embedded (Domain) manifold,
        //// return the corresponding position in the embedding (Range) manifold
        //abstract public double[] WhereIs(double[] point);

        // For an index in the embedded (Domain) manifold array,
        // return the corresponding index in the embedding (Range) manifold array
        public abstract int WhereIsIndex(int index);

        /// <summary>
        /// Given an index into the embedded manifold array, return the spatial position in the embedding manifold
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract double[] WhereIs(int index);

        public abstract bool NeedsInterpolation();

        // Coordinates in the embedding manifold of the embedded manifolds origin
        // Has Range.Dim elements
        // This field will probably be used by TranslEmbedding.
        // This field needs to be accessible from the extracellular mediums list of boundary manifolds,
        // so we can update this field as the cell position changes
        public double[] position;
    }

    /// <summary>
    /// An embedding in which the coordinates of the embedded manifold can be converted into coordinates in the
    /// embedding manifold using a mapping of dimensions and translation.
    /// Not a direct transform; can't be precomputed; needs interpolation
    /// If dimensionMap[i] = j, then the ith dimension in the embedded manifold maps to the jth dimension in the embedding manifold,
    /// where we count dimensions starting with 0.
    /// </summary>
    public class TranslEmbedding : Embedding
    {
        // dimensionsMap has Domain.Dim elements, except for embedded manifolds like TinySphere or TinyBall, in which case it has length 1
        public int[] dimensionsMap;

        //// Coordinates in the embedding manifold of the embedded manifolds origin
        //// Has Range.Dim elements
        public double[] position;

        public TranslEmbedding(DiscretizedManifold domain, DiscretizedManifold range, int[] _dimMap, double[] _pos)
        {
            Domain = domain;
            Range = range;

            dimensionsMap = new int[_dimMap.Length];
            Array.Copy(_dimMap, dimensionsMap, Domain.Dim);

            //position = new double[_pos.Length];
            //Array.Copy(_pos, position, Range.Dim);
            // point to the 'original': when the original changes, position reflects that
            position = _pos;
        }

        public override int WhereIsIndex(int index)
        {
            return -1;
        }

        public override double[] WhereIs(int index)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding point in embedding manifold Range
            if (index < 0 || index >= Domain.ArraySize)
            {
                return null;
            }

            double[] point = new double[position.Length];

            // Intialize point to the position in the embedding manifold of the embedded manifolds origin
            Array.Copy(position, point, position.Length);

            for (int i = 0; i < dimensionsMap.Length; i++)
            {
                point[dimensionsMap[i]] += Domain.Coordinates[index, i] ;
            }

            return point;
        }

        public override bool NeedsInterpolation()
        {
            return true;
        }

    }

    /// <summary>
    /// A TranslEmbedding in which there is a one-to-one correspondance between grid points in the embedded and
    /// embedding manifolds.
    /// A direct transform; can be precomputed; needs no interpolation
    /// Advantage: Boundary values in the embedding manifold can be updated without interpolation.
    /// Example: the rectangle manifolds that are boundary manifolds on the rectangular prism
    /// As currently implemented, this could also be applied to embeddings in which grid points don't coincide in the embedding
    /// and embedded manifold.
    /// </summary>
    public class DirectTranslEmbedding : Embedding
    {
        // dimensionsMap has Domain.Dim elements, except for embedded manifolds like TinySphere or TinyBall, in which case it has length 1
        int[] dimensionsMap;

        // Coordinates in the embedding manifold of the embedded manifolds origin
        // Has Range.Dim elements
        public double[] position;

        // precompute an array that maps the indices of the embedded manifold array to the corresponding indices of the 
        // embedding manifold array
        int[] indexMap;

        public DirectTranslEmbedding(DiscretizedManifold domain, DiscretizedManifold range, int[] _dimMap, double[] _pos)
        {
            Domain = domain;
            Range = range;

            dimensionsMap = new int[_dimMap.Length];
            Array.Copy(_dimMap, dimensionsMap, Domain.Dim);

            position = new double[_pos.Length];
            Array.Copy(_pos, position, Range.Dim);

            indexMap = new int[Domain.ArraySize];

            // Establish the one-to-one correspondence between the embedding and embedded manifold arrays
            for (int index = 0; index < Domain.ArraySize; index++)
            {
                double[] point = new double[position.Length];

                // Intialize point to the position in the embedding manifold of the embedded manifolds origin
                Array.Copy(position, point, position.Length);

                for (int i = 0; i < dimensionsMap.Length; i++)
                {
                    point[dimensionsMap[i]] += Domain.Coordinates[index, i];
                }

                indexMap[index] = Range.arrToIndex(Range.localToArr(point));
            }
        }

        public override int WhereIsIndex(int index)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding index in embedding manifold Range from precomputed index map
            return indexMap[index];
        }

        public override double[] WhereIs(int index)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding point in embedding manifold Range
            index = indexMap[index];
            if (index < 0 || index >= Range.ArraySize)
            {
                return null;
            }

            double[] point = new double[Range.Dim];

            for (int i = 0; i < Range.Dim; i++)
            {
                point[i] = Range.Coordinates[index, i];
            }
            return point;
        }

        public override bool NeedsInterpolation()
        {
            return false;
        }

    }

    /// <summary>
    /// Embedding in which there is a one-to-one correspondence between the grid points 
    /// of the embedded and embedding manifolds (e.g., cytoplasm and plasma membrane)
    /// A direct transform; no need to precomputation; needs no interpolation
    /// </summary>
    public class OneToOneEmbedding : Embedding
    {
        public OneToOneEmbedding(DiscretizedManifold domain, DiscretizedManifold range)
        {
            Domain = domain;
            Range = range;
        }

        public override int WhereIsIndex(int index)
        {
            return 0;
        }

        // there is no offset between domain and range here; the two manifolds share the origin
        // NOTE: for now only we can return the origin itself; for non-zero dimensional manifolds we'd need 
        public override double[] WhereIs(int index)
        {
            return new double[1];
        }

        public override bool NeedsInterpolation()
        {
            return false;
        }

    }

    //public class FixedEmbedding : Embedding
    //{
    //    // Array of indices that map the array of the embedded manifold to the array of the embedding manifold
    //    int[] indexMap;

    //}

    ///// <summary>
    ///// Embedding for a motile tiny sphere (cell)
    ///// 
    ///// </summary>
    //public class MotileTSEmbedding : Embedding
    //{
    //    // A structure that contains information about the location of the cell in the embedding environment
    //    Locator Loc;

    //    // NOTE: We don't need to pass domain if we have comp. domain = comp.Interior
    //    public MotileTSEmbedding(DiscretizedManifold domain, BoundedRectangularPrism range, Locator loc)
    //    {
    //        Domain = domain;
    //        Range = range;
    //        Loc= loc;
    //    }

    //     public override double[] WhereIs(int index)
    //    {
    //        double point 
    //        return Loc.position + Domain.Coordinates[index];
    //    }

    //    //public override double[] WhereIs(double[] point)
    //    //{
    //    //    return Loc.position;
    //    //}

    //    //public override int WhereIs(int index)
    //    //{
    //    //    return Range.PointToArray(Loc.position);
    //    // }
    //}

}
