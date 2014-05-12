using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    abstract public class Embedding
    {
        public Manifold Domain;
        public Manifold Range;

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

        public TranslEmbedding(Manifold domain, Manifold range, int[] _dimMap, double[] _pos)
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
                point[dimensionsMap[i]] += Domain.Coordinates[index, i];
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

        // precompute an array that maps the indices of the embedded manifold array to the corresponding indices of the 
        // embedding manifold array
        int[] indexMap;

        public DirectTranslEmbedding(Manifold domain, Manifold range, int[] _dimMap, double[] _pos)
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

        public override int WhereIsIndex(int idx)
        {
            // Input: array index in embedded manifold Domain
            // Output: corresponding index in embedding manifold Range from precomputed index map
            if (idx < 0 || idx >= indexMap.Length)
            {
                return -1;
            }
            return indexMap[idx];
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
        public OneToOneEmbedding(Manifold domain, Manifold range)
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
