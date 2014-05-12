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
            Laplacian = new LocalMatrix[0][];
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

        public RectangularPrism(int[] numDivisions)
        {
            Dim = 3;
            Debug.Assert(Dim == numDivisions.Length);
            Extents = (int[])numDivisions.Clone();
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

    public class Rectangle : DiscretizedManifold
    {
        LocalMatrix[] interpolator;

        public Rectangle(int[] numDivisions)
        {
            Dim = 2;
            Debug.Assert(Dim == numDivisions.Length);
            Extents = (int[])numDivisions.Clone();
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
