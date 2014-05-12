using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics;

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

    //}

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

        public double[,] Coordinates;
        // extent in each dimension
        public double[] Extents;
        public double[] StepSize;

        public abstract LocalMatrix[] Interpolation(double[] point);

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
    }

    public class ScalarField
    {
        public DiscretizedManifold M;
        public double Value(double[] point) { return double.NaN; }
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

    }

    public class GaussianScalarField : ScalarField
    {
        public GaussianScalarField(DiscretizedManifold m) : base(m)
        {

        }

        /// <summary>
        /// Calculate and return values of a Gaussian density funtion at each array point in the manifold
        /// The value at the center is max
        /// </summary>
        public bool Initialize(double[] x0, double[] sigma, double max)
        {
            if (M.Dim != x0.Length || M.Dim != sigma.Length)
            {
                return false;
            }

            // QUESTION: Do we need this to avoid negative concentrations in the diffusion algorithm?
            double SMALL = 1e-4,
                   f, d = 1.0;
            //double d = Math.Pow(2.0 * Math.PI, 1.5) * Math.Sqrt(sigma[0] * sigma[1] * sigma[2]);

            for (int i = 0; i < M.ArraySize; i++)
            {
                f = 0;
                for (int j = 0; j < M.Dim; j++)
                {
                    f += (x0[j] - M.Coordinates[i, j]) * (x0[j] - M.Coordinates[i, j]) / (2 * sigma[j]);
                }
                array[i] = max * Math.Exp(-f) / d;

                if (array[i] < SMALL)
                {
                    array[i] = 0;
                }
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
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
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
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
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

            // TODO: Implement these properly
            Laplacian = new LocalMatrix[ArraySize][];
            interpolator = new LocalMatrix[4];

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

            // TODO: this needs to be for correctness
            for (n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[7];
            }
            n = 0;

            int idxplus, idxminus;
            double coeff0, coeff;

            for (int i = 0; i < NumPoints[0]; i++)
            {
                for (int j = 0; j < NumPoints[1]; j++)
                {
                    for (int k = 0; k < NumPoints[2]; k++)
                    {
                        // Laplacian index n corresponds to grid indices (i,j,k)

                        // Intialize (i,j,k) coefficient to zero
                        Laplacian[n][0].Coefficient = 0;
                        Laplacian[n][0].Index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];

                        if ((i == 0) || (i == NumPoints[0] - 1))
                        {
                            // No flux BC
                            coeff0 = 0;
                            coeff = 0;
                            idxplus = 0;
                            idxminus = 0;
                        }
                        else
                        {
                            coeff0 = -2.0 / (StepSize[0] * StepSize[0]);
                            coeff = 1.0 / (StepSize[0] * StepSize[0]);
                            idxplus = (i + 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                            idxminus = (i - 1) + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                        }
                        // (i+1), j, k
                        Laplacian[n][1].Coefficient = coeff;
                        Laplacian[n][1].Index = idxplus;

                        // (i-1), j, k
                        Laplacian[n][2].Coefficient = coeff;
                        Laplacian[n][2].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        if ((j == 0) || (j == NumPoints[1] - 1))
                        {
                            // No flux BC
                            coeff0 = 0;
                            coeff = 0;
                            idxplus = 0;
                            idxminus = 0;
                        }
                        else
                        {
                            coeff0 = -2.0 / (StepSize[1] * StepSize[1]);
                            coeff = 1.0 / (StepSize[1] * StepSize[1]);
                            idxplus = i + (j + 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                            idxminus = i + (j - 1) * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                        }

                        // i, (j+1), k
                        Laplacian[n][3].Coefficient = coeff;
                        Laplacian[n][3].Index = idxplus;

                        // i, (j-1), k
                        Laplacian[n][4].Coefficient = coeff;
                        Laplacian[n][4].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        if ((k == 0) || (k == NumPoints[2] - 1))
                        {
                            // No flux BC
                            coeff0 = 0;
                            coeff = 0;
                            idxplus = 0;
                            idxminus = 0;
                        }
                        else
                        {
                            coeff0 = -2.0 / (StepSize[2] * StepSize[2]);
                            coeff = 1.0 / (StepSize[2] * StepSize[2]);
                            idxplus = i + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
                            idxminus = i + j * NumPoints[0] + (k + 1) * NumPoints[0] * NumPoints[1];
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

            double[] rectSpatialExtents;
            int[] numRectGridPts = new int[2];

            rectSpatialExtents = new double[2] { Extents[0], Extents[1] };
            numRectGridPts[0] = numGridPts[0];
            numRectGridPts[1] = numGridPts[1];
            DiscretizedManifold xyLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            DiscretizedManifold xyUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            rectSpatialExtents = new double[2] { Extents[0], Extents[2] };
            numRectGridPts[0] = numGridPts[0];
            numRectGridPts[1] = numGridPts[2];
            DiscretizedManifold xzLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            DiscretizedManifold xzUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            rectSpatialExtents = new double[2] { Extents[1], Extents[2] };
            numRectGridPts[0] = numGridPts[1];
            numRectGridPts[1] = numGridPts[2];
            DiscretizedManifold yzLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            DiscretizedManifold yzUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            // Position of rectangle origin in the BoundedRectangularPrism
            double[] origin;

            // Mapping of dimension in rectangle to dimensions in the BoundedRectangularPrism
            int[] dimensionsMap;

            dimensionsMap = new int[2] { 0, 1 };
            // xyLower
            origin = new double[3] { 0, 0, 0 };
            DirectTranslEmbedding xyLowerEmbed = new DirectTranslEmbedding(xyLower, this, dimensionsMap, origin);
            // xyUpper
            origin = new double[3] { 0, 0, Extents[2] };
            DirectTranslEmbedding xyUpperEmbed = new DirectTranslEmbedding(xyUpper, this, dimensionsMap, origin);

            dimensionsMap = new int[2] { 0, 2 };
            // xzLower
            origin = new double[3] { 0, 0, 0 };
            DirectTranslEmbedding xzLowerEmbed = new DirectTranslEmbedding(xzLower, this, dimensionsMap, origin);
            // xzUpper
            origin = new double[3] { 0, Extents[1], 0 };
            DirectTranslEmbedding xzUpperEmbed = new DirectTranslEmbedding(xzUpper, this, dimensionsMap, origin);

            dimensionsMap = new int[2] { 1, 2 };
            // yzLower
            origin = new double[3] { 0, 0, 0 };
            DirectTranslEmbedding yzLowerEmbed = new DirectTranslEmbedding(yzLower, this, dimensionsMap, origin);
            // yzLower
            origin = new double[3] { Extents[0], 0, 0 };
            DirectTranslEmbedding yzUpperEmbed = new DirectTranslEmbedding(yzUpper, this, dimensionsMap, origin);

            Boundaries.Add(xyLower, xyLowerEmbed);
            Boundaries.Add(xyUpper, xyUpperEmbed);
            Boundaries.Add(xzLower, xzLowerEmbed);
            Boundaries.Add(xzUpper, xzUpperEmbed);
            Boundaries.Add(yzLower, yzLowerEmbed);
            Boundaries.Add(yzUpper, yzUpperEmbed);
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

    public class VectorField
    {

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

        // Coordinates in the embedding manifold of the embedded manifolds origin
        // Has Range.Dim elements
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
