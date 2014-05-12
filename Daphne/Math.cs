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
        public int[] Extents = null;
    }

    public abstract class DiscretizedManifold : Manifold
    {
        public int ArraySize;
        public LocalMatrix[][] Laplacian;
        public abstract LocalMatrix[] Interpolation(double[] point);
        public double[,] Coordinates;
        public Dictionary<Manifold,Embedding> Boundaries;
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

            //Laplacian = new LocalMatrix[0][];
            // TODO: Check this
            // Do this for now so Step() in MolecularPopulations won't crash
            Laplacian = new LocalMatrix[1][];
            for (int n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[1];
                Laplacian[n][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
            }

            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
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

            //Laplacian = new LocalMatrix[0][];
            // TODO: Check this
            // Do this for now so Step() in MolecularPopulations won't crash
            Laplacian = new LocalMatrix[1][];
            for (int n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[1];
                Laplacian[n][0] = new LocalMatrix() { Coefficient = 1.0, Index = 0 };
            }

            interpolator = new LocalMatrix[1] { new LocalMatrix() { Coefficient = 1.0, Index = 0 } };
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            return interpolator;
        }

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

    }

    public class BoundedRectangularPrism : DiscretizedManifold
    {
        LocalMatrix[] interpolator;
        // min and max in each dimension
        double[] spatialExtent;
        double[] StepSize;

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
            StepSize = new double[Dim];
            StepSize[0] = (spatialExtent[1] - spatialExtent[0]) / (Extents[0] - 1);
            StepSize[1] = (spatialExtent[3] - spatialExtent[2]) / (Extents[1] - 1);
            StepSize[2] = (spatialExtent[5] - spatialExtent[4]) / (Extents[2] - 1);

            int n = 0;
            for (int i = 1; i < Extents[0]; i++)
            {
                for (int j = 1; j < Extents[1]; j++)
                {
                    for (int k = 1; k < Extents[2]; k++)
                    {
                        Coordinates[n, 0] = i * StepSize[0];
                        Coordinates[n, 1] = j * StepSize[1];
                        Coordinates[n, 2] = k * StepSize[2];
                        n++;
                    }
                }
            }

            for (n = 0; n < ArraySize; n++)
            {
                Laplacian[n] = new LocalMatrix[7];
            }
            // Initialize Laplacian coefficients to zero.
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

            ////// We may not need this level of detail
            //////
            ////int[] numRectGridPts = new int[2];

            ////numRectGridPts[0] = numGridPts[0];
            ////numRectGridPts[1] = numGridPts[1];
            ////Manifold xyLower = new Rectangle(numRectGridPts);
            ////Manifold xyUpper = new Rectangle(numRectGridPts);

            ////numRectGridPts[0] = numGridPts[0];
            ////numRectGridPts[1] = numGridPts[2];
            ////Manifold xzLower = new Rectangle(numRectGridPts);
            ////Manifold xzUpper = new Rectangle(numRectGridPts);

            ////numRectGridPts[0] = numGridPts[1];
            ////numRectGridPts[1] = numGridPts[2];
            ////Manifold yzLower = new Rectangle(numRectGridPts);
            ////Manifold yzUpper = new Rectangle(numRectGridPts);

            //Manifold xyLower = new Rectangle();
            //Manifold xyUpper = new Rectangle();
            //Manifold xzLower = new Rectangle();
            //Manifold xzUpper = new Rectangle();
            //Manifold yzLower = new Rectangle();
            //Manifold yzUpper = new Rectangle();

            //Embedding xyLowerEmbed = new Embedding(this, xyLower);
            //Embedding xyUpperEmbed = new Embedding(this, xyUpper);
            //Embedding xzLowerEmbed = new Embedding(this, xzLower);
            //Embedding xzUpperEmbed = new Embedding(this, xzUpper);
            //Embedding yzLowerEmbed = new Embedding(this, yzLower);
            //Embedding yzUpperEmbed = new Embedding(this, yzUpper);

            //Boundaries.Add(xyLower, xyLowerEmbed);
            //Boundaries.Add(xyUpper, xyUpperEmbed);
            //Boundaries.Add(xzLower, xzLowerEmbed);
            //Boundaries.Add(xzUpper, xzUpperEmbed);
            //Boundaries.Add(yzLower, yzLowerEmbed);
            //Boundaries.Add(yzUpper, yzUpperEmbed);

        }

        private int[] localToArr(double[] loc)
        {
            if (loc != null)
            {
                return new int[] { (int)(loc[0] / StepSize[0]), (int)(loc[1] / StepSize[1]), (int)(loc[2] / StepSize[2]) };
            }
            return null;
        }

        public override LocalMatrix[] Interpolation(double[] point)
        {
            // Linear interpolation
            // The values in point are in terms of manifold coordinates

            int[] idx = localToArr(point);
            if (idx.Length == 0) return interpolator;

            int i = idx[0];
            int j = idx[1];
            int k = idx[2];

            if ((i < 0) || (i > Extents[0] - 1 ) ||
                (j < 0) || (j > Extents[1] - 1 ) ||
                (k < 0) || (k > Extents[2] - 1 ))
                return interpolator;

            double dx = point[0] / StepSize[0] - i;
            double dy = point[1] / StepSize[1] - j;
            double dz = point[2] / StepSize[2] - k;

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
    }

    //     /// <summary>
    //    /// Convert an index n from a 1D array int coordinate indices
    //    /// </summary>
    //    public int[] IndexToCoordIndices(int n, int[] Extents)
    //    {
    //        int[] idx = new int[Extents.Length];
    //        int[] N = new int[Extents.Length];
    //        int prod=1, sum=0;

    //        for ( int m=0; m<Extents.Length-1; m++ )
    //        {
    //            prod = prod*Extents[m];
    //            N[m+1] = prod;
    //        }

    //        for (int m = 0; m < Extents.Length; m++)
    //        {
    //            idx[Extents.Length - m - 1] = (int)(n / N[m]);
    //            sum = sum + idx[Extents.Length - m - 1] * N[m];
    //        }

    //        return idx;

    //    }
    //}


    

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

    public class Embedding
    {
        public Manifold Domain;
        public Manifold Range;

        // gmk
        public Embedding(Manifold domain, Manifold range)
        {
            Domain = domain;
            Range = range;
        }
        // gmk
        public Embedding()
        {
        }

        public double[] WhereIs(double[] point)
        {
            return new double[Range.Dim];
        }

        public int WhereIs(int index)
        {
            return new int();
        }
    }

}
