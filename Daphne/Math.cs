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
        public abstract LocalMatrix[] Interpolation(double[] point);
        //public double[,] Coordinates;
        public Dictionary<Manifold,Embedding> Boundaries;
        public int[] Extents = null;

        public double[,] Coordinates;
        // min and max in each dimension
        public double[] spatialExtent;
        public double[] StepSize;
        public double[] Origin;        
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

    public class TinySphere : DiscretizedManifold
    {
        LocalMatrix[] interpolator;

        public TinySphere()
        {
            Dim = 0;
            ArraySize = 1;
            Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1,1];
            Coordinates[0, 0] = 0.0;
            Origin = new double[1] {0.0};
            StepSize = new double[1] {1.0};
         }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }
    }

    public class TinyBall : DiscretizedManifold
    {
        LocalMatrix[] interpolator;

        public TinyBall()
        {
            Dim = 0;
            ArraySize = 1;
            Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
            Coordinates = new double[1, 1];
            Coordinates[0, 0] = 0.0;
            Origin = new double[1] {0.0};
            StepSize = new double[1] { 1.0 };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }

        //public override int PointToArray(double[] point)
        //{
        //    return 0;
        //}
    }

    public class Rectangle : DiscretizedManifold
    {
        LocalMatrix[] interpolator;

        public Rectangle()
        {
            Dim = 2;
            Extents = null;
            ArraySize = 1;

            Boundaries = null;
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public Rectangle(int[] numGridPts)
        {
            Dim = 2;
            Debug.Assert(Dim == numGridPts.Length);
            Extents = (int[])numGridPts.Clone();
            ArraySize = Extents[0] * Extents[1];

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
        LocalMatrix[] interpolator;

        public RectangularPrism(int[] numGridPts)
        {
            Dim = 3;
            Debug.Assert(Dim == numGridPts.Length);
            Extents = (int[])numGridPts.Clone();
            ArraySize = Extents[0] * Extents[1] * Extents[2];

            Boundaries = new Dictionary<Manifold,Embedding>();
            Laplacian = new LocalMatrix[0][];
            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }

        //public override int PointToArray(double[] point)
        //{
        //    return new int();
        //}

    }

    public class BoundedRectangle : DiscretizedManifold
    {
        LocalMatrix[] interpolator;

        public BoundedRectangle(int[] numGridPts, double[] _spatialExtent)
        {
            Dim = 2;
            Debug.Assert(Dim == numGridPts.Length);
            Extents = (int[])numGridPts.Clone();
            ArraySize = Extents[0] * Extents[1];
            Boundaries = new Dictionary<Manifold, Embedding>();

            // TODO: Implement these properly
            Laplacian = new LocalMatrix[ArraySize][];
            interpolator = new LocalMatrix[4];

            Coordinates = new double[ArraySize, 2];
            spatialExtent = (double[])_spatialExtent.Clone();

            Origin = new double[Dim];
            Origin[0] = spatialExtent[0];
            Origin[1] = spatialExtent[2];

            StepSize = new double[Dim];
            StepSize[0] = (spatialExtent[1] - spatialExtent[0]) / (Extents[0] - 1);
            StepSize[1] = (spatialExtent[3] - spatialExtent[2]) / (Extents[1] - 1);

            int n = 0;
            for (int j = 0; j < Extents[1]; j++)
            {
                for (int i = 0; i < Extents[0]; i++)
                {
                    Coordinates[n, 0] = i * StepSize[0] + Origin[0];
                    Coordinates[n, 1] = j * StepSize[1] + Origin[1];
                    n++;
                }
            }

        }

         private int[] localToArr(double[] loc)
        {
            if (loc != null)
            {
                return new int[] { (int)((loc[0] - Origin[0]) / StepSize[0]), (int)((loc[1] - Origin[1])/ StepSize[1]) };
            }
            return null;
        }

        // TODO: This needs to be checked for correctness
        public override LocalMatrix[] Interpolation(double[] point)
        {
            // Linear interpolation
            // The values in point are in terms of manifold coordinates

            int[] idx = localToArr(point);
            if (idx.Length == 0) return interpolator;

            int i = idx[0];
            int j = idx[1];

            if ((i < 0) || (i > Extents[0] - 1 ) ||
                (j < 0) || (j > Extents[1] - 1 ) )
                 return interpolator;

            double dx = point[0] / StepSize[0] - i;
            double dy = point[1] / StepSize[1] - j;

            // 00
            interpolator[0].Coefficient = (1 - dx) * (1 - dy);
            interpolator[0].Index = i + j * Extents[0] ;
            // 10
            interpolator[1].Coefficient = dx * (1 - dy);
            interpolator[1].Index = (i + 1) + j * Extents[0];
            // 11
            interpolator[2].Coefficient = dx * dy ;
            interpolator[2].Index = (i + 1) + (j + 1) * Extents[0];
            // 01
            interpolator[3].Coefficient = (1 - dx) * dy;
            interpolator[3].Index = i + (j + 1) * Extents[0];

            return interpolator;
        }

    }

    public class BoundedRectangularPrism : DiscretizedManifold
    {
        LocalMatrix[] interpolator;
        // min and max in each dimension
        //public double[] spatialExtent;
        //public double[] StepSize;
        //public double[] Origin;

        public BoundedRectangularPrism(int[] numGridPts, double[] _spatialExtent)
        {
            Dim = 3;
            Debug.Assert(Dim == numGridPts.Length);
            Extents = (int[])numGridPts.Clone();
            ArraySize = Extents[0] * Extents[1] * Extents[2];
            Coordinates = new double[ArraySize, 3];
            Boundaries = new Dictionary<Manifold, Embedding>();
            Laplacian = new LocalMatrix[ArraySize][];
            interpolator = new LocalMatrix[8];

            spatialExtent = (double[])_spatialExtent.Clone();

            Origin = new double[Dim];
            Origin[0] = spatialExtent[0];
            Origin[1] = spatialExtent[2];
            Origin[2] = spatialExtent[4];

            StepSize = new double[Dim];
            StepSize[0] = (spatialExtent[1] - spatialExtent[0]) / (Extents[0] - 1);
            StepSize[1] = (spatialExtent[3] - spatialExtent[2]) / (Extents[1] - 1);
            StepSize[2] = (spatialExtent[5] - spatialExtent[4]) / (Extents[2] - 1);

            int n = 0;
            for (int k = 0; k < Extents[2]; k++)
            {
                for (int j = 0; j < Extents[1]; j++)
                {
                    for (int i = 0; i < Extents[0]; i++)
                    {
                        Coordinates[n, 0] = i * StepSize[0] + Origin[0];
                        Coordinates[n, 1] = j * StepSize[1] + Origin[1];
                        Coordinates[n, 2] = k * StepSize[2] + Origin[2];
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
            for (int i = 0; i < Extents[0]; i++)
            {
                for (int j = 0; j < Extents[1]; j++)
                {
                    for (int k = 0; k < Extents[2]; k++)
                    {
                        // Laplacian index n corresponds to grid indices (i,j,k)

                        // Intialize (i,j,k) coefficient to zero
                        Laplacian[n][0].Coefficient = 0;
                        Laplacian[n][0].Index = i + j * Extents[0] + k * Extents[0] * Extents[1];

                        if ( (i == 0) || (i == Extents[0] - 1) )
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
                            idxplus = (i + 1) + j * Extents[0] + k * Extents[0] * Extents[1];
                            idxminus = (i - 1) + j * Extents[0] + k * Extents[0] * Extents[1];
                        }
                        // (i+1), j, k
                        Laplacian[n][1].Coefficient = coeff;
                        Laplacian[n][1].Index = idxplus;

                        // (i-1), j, k
                        Laplacian[n][2].Coefficient = coeff;
                        Laplacian[n][2].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        if ((j == 0) || (j == Extents[1] - 1))
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
                            idxplus = i + (j + 1) * Extents[0] + k * Extents[0] * Extents[1];
                            idxminus = i + (j - 1) * Extents[0] + k * Extents[0] * Extents[1];
                        }

                        // i, (j+1), k
                        Laplacian[n][3].Coefficient = coeff;
                        Laplacian[n][3].Index = idxplus;

                        // i, (j-1), k
                        Laplacian[n][4].Coefficient = coeff;
                        Laplacian[n][4].Index = idxminus;

                        // i,j,k
                        Laplacian[n][0].Coefficient = Laplacian[n][0].Coefficient + coeff0;

                        if ((k == 0) || (k == Extents[2] - 1))
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
                            idxplus = i + j * Extents[0] + (k+1) * Extents[0] * Extents[1];
                            idxminus = i + j * Extents[0] + (k+1) * Extents[0] * Extents[1];
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

            numRectGridPts[0] = numGridPts[0];
            numRectGridPts[1] = numGridPts[1];
            rectSpatialExtents = new double[4] { spatialExtent[0], spatialExtent[1], spatialExtent[2], spatialExtent[3] };
            DiscretizedManifold xyLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            DiscretizedManifold xyUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            rectSpatialExtents = new double[4] { spatialExtent[0], spatialExtent[1], spatialExtent[4], spatialExtent[5] };
            numRectGridPts[0] = numGridPts[0];
            numRectGridPts[1] = numGridPts[2];
            DiscretizedManifold xzLower = new BoundedRectangle(numRectGridPts, rectSpatialExtents);
            DiscretizedManifold xzUpper = new BoundedRectangle(numRectGridPts, rectSpatialExtents);

            rectSpatialExtents = new double[4] { spatialExtent[2], spatialExtent[3], spatialExtent[4], spatialExtent[5] };
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
            origin = new double[3] {Origin[0], Origin[1], Origin[2]};
            DirectTranslEmbedding xyLowerEmbed = new DirectTranslEmbedding(xyLower, this, dimensionsMap, origin);
            // xyUpper
            origin = new double[3] { Origin[0], Origin[1], spatialExtent[5] };
            DirectTranslEmbedding xyUpperEmbed = new DirectTranslEmbedding(xyUpper, this, dimensionsMap, origin);

            dimensionsMap = new int[2] { 0, 2 };
            // xzLower
            origin = new double[3] { Origin[0], Origin[1], Origin[2] };
            DirectTranslEmbedding xzLowerEmbed = new DirectTranslEmbedding(xzLower, this, dimensionsMap, origin);
            // xzUpper
            origin = new double[3] { Origin[0], spatialExtent[3], Origin[2] };
            DirectTranslEmbedding xzUpperEmbed = new DirectTranslEmbedding(xzUpper, this, dimensionsMap, origin);

            dimensionsMap = new int[2] { 1, 2 };
            // yzLower
            origin = new double[3] { Origin[0], Origin[1], Origin[2] };
            DirectTranslEmbedding yzLowerEmbed = new DirectTranslEmbedding(yzLower, this, dimensionsMap, origin);
            // yzLower
            origin = new double[3] { spatialExtent[1], Origin[1], Origin[2] };
            DirectTranslEmbedding yzUpperEmbed = new DirectTranslEmbedding(yzUpper, this, dimensionsMap, origin);

            Boundaries.Add(xyLower, xyLowerEmbed);
            Boundaries.Add(xyUpper, xyUpperEmbed);
            Boundaries.Add(xzLower, xzLowerEmbed);
            Boundaries.Add(xzUpper, xzUpperEmbed);
            Boundaries.Add(yzLower, yzLowerEmbed);
            Boundaries.Add(yzUpper, yzUpperEmbed);

        }

        //public override int PointToArray(double[] loc)
        //{
        //    if (loc != null)
        //    {
        //        // NOTE: modify for origin not equal to 0,0,0
        //        // spatialExtents information??? 
        //        return (int)(loc[0] / StepSize[0]) + ((int)(loc[1] / StepSize[1])) * Extents[0] + ((int)(loc[2] / StepSize[2])) * Extents[0] * Extents[1];

        //    }
        //    return new int();
        //}

        private int[] localToArr(double[] loc)
        {
            if (loc != null)
            {
                return new int[] { (int)((loc[0]-Origin[0]) / StepSize[0]), (int)((loc[1]-Origin[1]) / StepSize[1]), (int)((loc[2]-Origin[2]) / StepSize[2]) };
            }
            return null;
        }


        // TODO: This needs to be checked for correctness
        public override LocalMatrix[] Interpolation(double[] point)
        {
            // Linear interpolation
            // The values in point are in terms of manifold coordinates

            int[] idx = localToArr(point);
            if (idx.Length == 0)
            {
                // TODO: Send an error message
                return interpolator;
            }

            int i = idx[0];
            int j = idx[1];
            int k = idx[2];

            // TODO: Send an error message
            if ((i < 0) || (i > Extents[0] - 1 ) ||
                (j < 0) || (j > Extents[1] - 1 ) ||
                (k < 0) || (k > Extents[2] - 1 ))
                return interpolator;

            // When i== Extents[0]-1, we can't look at the (i+1)th grid point
            // In this case we can decrement the origin of the interpolation voxel and get the same result
            // When we decrement i -> i-1, then dx = 1
            // Similarly, for j and k.
            if (i == Extents[0] - 1) i--;
            if (j == Extents[0] - 1) j--;
            if (k == Extents[0] - 1) k--;
 


            double dx = (point[0] - Origin[0])/ StepSize[0] - i;
            double dy = (point[1] - Origin[1])/ StepSize[1] - j;
            double dz = (point[2] - Origin[2])/ StepSize[2] - k;

            // 000
            interpolator[0].Coefficient = (1 - dx) * (1 - dy) * (1 - dz);
            interpolator[0].Index = i + j * Extents[0] + k * Extents[0] * Extents[1];
            // 100
            interpolator[1].Coefficient = dx * (1 - dy) * (1 - dz);
            interpolator[1].Index = (i + 1) + j * Extents[0] + k * Extents[0] * Extents[1];
            // 110
            interpolator[2].Coefficient = dx * dy * (1 - dz);
            interpolator[2].Index = (i + 1) + (j + 1) * Extents[0] + k * Extents[0] * Extents[1];
            // 101
            interpolator[3].Coefficient = dx * (1 - dy) * dz;
            interpolator[3].Index = (i + 1) + j * Extents[0] + (k + 1) * Extents[0] * Extents[1];
            // 010
            interpolator[4].Coefficient = (1 - dx) * dy * (1 - dz);
            interpolator[4].Index = i + (j + 1) * Extents[0] + k * Extents[0] * Extents[1];
            // 011
            interpolator[5].Coefficient = (1 - dx) * dy * dz;
            interpolator[5].Index = i + (j + 1) * Extents[0] + (k + 1) * Extents[0] * Extents[1];
            // 001
            interpolator[6].Coefficient = (1 - dx) * (1 - dy) * dz;
            interpolator[6].Index = i + j * Extents[0] + (k + 1) * Extents[0] * Extents[1];
            // 111
            interpolator[7].Coefficient = dx * dy * dz;
            interpolator[7].Index = (i + 1) + (j + 1) * Extents[0] + (k + 1) * Extents[0] * Extents[1];

            return interpolator;

        }

        /// <summary>
        /// Calculate and return values of a Gaussian density funtion at each array point in the manifold
        /// The value at the center is 1
        /// </summary>
        public ScalarField GaussianField(double[] x0, double[] sigma)
        {
            // QUESTION: Do we need this to avoid negative concentrations in the diffusion algorithm?
            double SMALL = 1e-4;

            // Any reason not to allow the center of the distribution to be outside the Extents?
 
            //// Check that x0 is in the bounded manifold
            //if ((x0[0] < this.spatialExtent[0]) || (x0[0] > this.spatialExtent[0]) ||
            //    (x0[1] < this.spatialExtent[1]) || (x0[1] > this.spatialExtent[1]) ||
            //    (x0[2] < this.spatialExtent[2]) || (x0[2] > this.spatialExtent[2]))
            //{
            //    // The center of the distribution is 
            //    double d = 1.0/((Extents[1] - Extents[0]) * (Extents[3] - Extents[2]) * (Extents[5] - Extents[4]));
            //    ScalarField s = new ScalarField(this, d);
            //}
            //else
            //{
                ScalarField s = new ScalarField(this);
                double f;

                double d = 1.0;
                //double d = Math.Pow(2.0 * Math.PI, 1.5) * Math.Sqrt(sigma[0] * sigma[1] * sigma[2]);

                for (int i = 0; i < ArraySize; i++)
                {
                    f = 0;
                    for (int j = 0; j < Dim; j++)
                    {
                        f = f + (x0[j] - Coordinates[i, j]) * (x0[j] - Coordinates[i, j]) / (2 * sigma[j]);
                    }
                    s.array[i] = Math.Exp(-f) / d;

                    if (s.array[i] < SMALL)
                    {
                        s.array[i] = 0;
                    }
                }
            //}

            return s; 
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

        //// For an index in the embedded (Domain) manifold array,
        //// return the corresponding index in the embedding (Range) manifold array
        //abstract public int WhereIs(int index);

        /// <summary>
        /// Given an index into the embedded manifold array, return the spatial position in the embedding manifold
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        abstract public double[] WhereIs(int index);
    }

    /// <summary>
    /// An embedding in which the coordinates of the embedded manifold can be converted into coordinates in thet
    /// embedding manifold using a mapping of dimensions and translation.
    /// If dimensionMap[i] = j, then the ith dimension in the embedded manifold maps to the jth dimension in the embedding manifold,
    /// where we count dimensions starting with 0.
    /// Examples with the BoundedRectangularPrism as the embedding manifold: 
    ///     XY rectangular face - dimensionsMap = {0,1}
    ///     YZ rectangular face - dimensionsMap = {1,2}
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

            position = new double[_pos.Length];
            Array.Copy(_pos, position, Range.Dim);
        }

        public override double[] WhereIs(int index)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding point in embedding manifold Range

            double[] point = new double[position.Length];

            // Intialize point to the position in the embedding manifold of the embedded manifolds origin
            for (int j = 0; j < position.Length; j++)
            {
                point[j] = position[j];
            }

            for (int i=0; i<dimensionsMap.Length; i++)
            {
                point[dimensionsMap[i]] = point[dimensionsMap[i]] + (Domain.Coordinates[index,i] - Domain.Origin[i] ) ;
            }

            return point;
        }

    }

    /// <summary>
    /// A TranslEmbedding in which there is a one-to-one correspondance between grid points in the embedded and
    /// embedding manifolds.
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

        // An array that maps the indices of the embedded manifold array to the corresponding indices of the 
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

            // Establish the one-to-one correspondance between the embedding and embedded manifold arrays

            double[] point = new double[position.Length];
            int prod;

            for (int i = 0; i < Domain.ArraySize; i++)
            {
                // The spatial position in the embedding manifold of the grid point on the embedding manifold
                point = WhereIs(i);

                prod = 1;

                // QUESTION: Is there a better way to do this that doesn't require converting to grid point indices?
                indexMap[i] = indexMap[i] + (int)Math.Round((point[0] - Range.Origin[0]) / Range.StepSize[0]);
                for (int j = 1; j < Range.Dim; j++)
                {
                    prod = prod * Range.Extents[j - 1];
                    indexMap[i] = indexMap[i] + prod* (int)Math.Round((point[j] - Range.Origin[j]) / Range.StepSize[j]);
                }
            }
        }

        public override double[] WhereIs(int index)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding point in embedding manifold Range

            double[] point = new double[position.Length];

            // Intialize point to the position in the embedding manifold of the embedded manifolds origin
            for (int j = 0; j < position.Length; j++)
            {
                point[j] = position[j];
            }

            for (int i = 0; i < dimensionsMap.Length; i++)
            {
                point[dimensionsMap[i]] = point[dimensionsMap[i]] + (Domain.Coordinates[index, i] - Domain.Origin[i]);
            }

            return point;
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

    ///// <summary>
    ///// Embedding in which there is a one-to-one correspondance between the grid points 
    ///// of the embedded and embedding manifolds (e.g., cytoplasm and plasma membrane)
    ///// </summary>
    //public class OneToOneEmbedding : Embedding
    //{

    //    // NOTE: This is a very subclass of Embedding which is used for TinySphere embedded
    //    // in TinyBall

    //    // A structure that contains information about the location of the cell in the embedding environment
    //    Locator Loc;

    //    public OneToOneEmbedding(DiscretizedManifold domain, DiscretizedManifold range)
    //    {
    //        Domain = domain;
    //        Range = range;
    //    }

    //    public override double[] WhereIs(int index)
    //    {

    //        return Loc.position;
    //    }

    //    //public override double[] WhereIs(double[] point)
    //    //{
    //    //    // Spatial location not applicable here
    //    //    return new double[Range.Dim];
    //    //}

    //    //public override int WhereIs(int index)
    //    //{
    //    //    return index;
    //    //}
    //}



}
