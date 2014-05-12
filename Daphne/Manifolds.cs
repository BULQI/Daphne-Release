using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace Daphne
{
    public abstract class Manifold
    {
        public Manifold(int dim)
        {
            Id = safeId++;
            Dim = dim;
        }

        public int Dim { get; private set; }
        public int Id { get; set; }
        private static int safeId = 0;
        public int ArraySize { get; set; }
        public LocalMatrix[][] Laplacian { get; set; }
        public Dictionary<int, Embedding> Boundaries { get; set; }
        public int[] NumPoints { get; set; }
        protected LocalMatrix[] interpolator { get; set; }
        protected LocalMatrix[][] gradientOperator { get; set; }

        public double[,] Coordinates { get; set; }
        // extent in each dimension
        public double[] Extents { get; set; }
        public double[] StepSize { get; set; }

        // abstract functions

        public abstract LocalMatrix[] Interpolation(double[] point);
        public abstract LocalMatrix[][] GradientOperator(int index);
        // gmk NOTE: Calculate the total quantity on the manifold using a simple integration algorithm.
        // Used to test diffusion with zero flux boundary conditions for "leaks".
        // TinySphere.Integrate(s) returns s*4*pi*r^2
        // TinyBall.Integrate(s) returns s*4*pi*r^3/3
        public abstract double Integrate(ScalarField s);
        public abstract int[] localToArr(double[] loc);
        public abstract int arrToIndex(int[] arr);


        public bool isIn(double[] loc)
        {
            for (int i = 0; i < Extents.Length; i++)
            {
                if (loc[i] < 0 || loc[i] > Extents[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool isOnBoundary(double[] loc)
        {
            double E_BOUNDARY_THICKNESS = 0.001;

            for (int i = 0; i < Extents.Length; i++)
            {
                if (loc[i] < 0 || loc[i] > E_BOUNDARY_THICKNESS && loc[i] < Extents[i] - E_BOUNDARY_THICKNESS || loc[i] > Extents[i])
                {
                    return false;
                }
            }
            return true;
        }

        public double distance(double[] p1, double[] p2)
        {
            if (isIn(p1) == true && isIn(p2) == true)
            {
                int[] pi1 = localToArr(p1), pi2 = localToArr(p2);
                double dist = 0;

                for (int i = 0; i < pi1.Length; i++)
                {
                    dist += (pi1[i] - pi2[i]) * (pi1[i] - pi2[i]);
                }
                return Math.Sqrt(dist);
            }
            return -1;
        }

    }

    public class TinySphere : Manifold
    {
        public TinySphere(double[] extent) : base(0)
        {
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
            // The radius of the sphere
            Extents = (double[])extent.Clone();
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
            return s[0] * Math.PI * Extents[0] * Extents[0];
        }

        public override int[] localToArr(double[] loc)
        {
            if (loc == null || loc.Length != Dim)
            {
                throw new Exception("Bad argument in localToArr");
            }
            return new int[1];
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr == null || arr.Length != Dim)
            {
                throw new Exception("Bad argument in arrToIndex");
            }
            return 0;
        }
    }

    public class TinyBall : Manifold
    {
        public TinyBall(double[] extent) : base(0)
        {
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
            // The radius of the sphere
            Extents = (double[])extent.Clone();
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
            return s[0] * 4.0 * Math.PI * Extents[0] * Extents[0] * Extents[0] / 3.0;
        }

        public override int[] localToArr(double[] loc)
        {
            if (loc == null || loc.Length != Dim)
            {
                return null;
            }
            return new int[1];
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr == null || arr.Length != Dim)
            {
                return -1;
            }
            return 0;
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
    public class BoundedRectangle : Manifold
    {
        public BoundedRectangle(int[] numGridPts, double[] extent) : base(2)
        {
            Debug.Assert(Dim == numGridPts.Length && Dim == extent.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1];
            Boundaries = new Dictionary<int, Embedding>();

            // TODO: Implement these properly
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
            if (loc == null || loc.Length != Dim)
            {
                return null;
            }
            return new int[] { (int)(loc[0] / StepSize[0]), (int)(loc[1] / StepSize[1]) };
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr == null || arr.Length != Dim)
            {
                return -1;
            }
            return arr[0] + arr[1] * NumPoints[0];
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
            interpolator[2].Coefficient = dx * dy;
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
                    quantity += s.Get(point);
                }
            }

            return quantity * voxel;
        }

    }

    public class BoundedRectangularPrism : Manifold
    {
        public BoundedRectangularPrism(int[] numGridPts, double[] extent) : base(3)
        {
            Debug.Assert(Dim == numGridPts.Length && Dim == extent.Length);
            NumPoints = (int[])numGridPts.Clone();
            ArraySize = NumPoints[0] * NumPoints[1] * NumPoints[2];
            Coordinates = new double[ArraySize, 3];
            Boundaries = new Dictionary<int, Embedding>();
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
            int N01 = NumPoints[0] * NumPoints[1];

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
                        Laplacian[n][6].Coefficient = coeff;
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

            //Boundaries.Add(xyLower.Id, xyLowerEmbed);
            //Boundaries.Add(xyUpper.Id, xyUpperEmbed);
            //Boundaries.Add(xzLower.Id, xzLowerEmbed);
            //Boundaries.Add(xzUpper.Id, xzUpperEmbed);
            //Boundaries.Add(yzLower.Id, yzLowerEmbed);
            //Boundaries.Add(yzUpper.Id, yzUpperEmbed);
        }

        public override int[] localToArr(double[] loc)
        {
            if (loc == null || loc.Length != Dim)
            {
                return null;
            }
            return new int[] { (int)(loc[0] / StepSize[0]), (int)(loc[1] / StepSize[1]), (int)(loc[2] / StepSize[2]) };
        }

        public override int arrToIndex(int[] arr)
        {
            if (arr == null || arr.Length != Dim)
            {
                return -1;
            }
            return arr[0] + arr[1] * NumPoints[0] + arr[2] * NumPoints[0] * NumPoints[1];
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
                        index = i + j * NumPoints[0] + k * NumPoints[0] * NumPoints[1];
                        point[0] = Coordinates[index, 0] + StepSize[0] / 2.0;
                        point[1] = Coordinates[index, 1] + StepSize[1] / 2.0;
                        point[2] = Coordinates[index, 2] + StepSize[2] / 2.0;

                        // The value at the center of the voxel
                        quantity += s.Get(point);
                    }
                }
            }
            return quantity * voxel;
        }

    }

}
