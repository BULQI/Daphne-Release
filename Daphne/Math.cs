#if DAPHNE_MATH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
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

}
#endif