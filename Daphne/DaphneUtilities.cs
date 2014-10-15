using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using Troschuetz.Random;


namespace Daphne
{
    /// <summary>
    /// Distributions and random number sources.
    /// This code was copied from Plaza Sur.
    /// </summary>
    class Rand
    {
        public static MersenneTwister MersenneTwister;
        public static MathNet.Numerics.Distributions.Normal NormalDist;
        public static MathNet.Numerics.Distributions.ContinuousUniform UniformDist;
        //public static SystemRandomSource SystemRandom;
        public static Troschuetz.Random.MT19937Generator MT19937gen;
        public static Troschuetz.Random.ContinuousUniformDistribution TroschuetzCUD;
        public static double pNorm = 2.0;

        static Rand()
        {
            MersenneTwister = new MersenneTwister();
            NormalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0, MersenneTwister);
            UniformDist = new MathNet.Numerics.Distributions.ContinuousUniform(0.0, 1.0, MersenneTwister);
            MT19937gen = new Troschuetz.Random.MT19937Generator();
            TroschuetzCUD = new Troschuetz.Random.ContinuousUniformDistribution(Rand.MT19937gen);
        }

        public static void ReseedAll(int seed)
        {
            MersenneTwister = new MersenneTwister(seed);
            NormalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0, MersenneTwister);
            UniformDist = new MathNet.Numerics.Distributions.ContinuousUniform(0.0, 1.0, MersenneTwister);
            MT19937gen = new Troschuetz.Random.MT19937Generator();
            TroschuetzCUD = new Troschuetz.Random.ContinuousUniformDistribution(Rand.MT19937gen);
            //SystemRandom = new SystemRandomSource(seed);
        }

        public static void ReseedNormalDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwister(seed);
                NormalDist = new MathNet.Numerics.Distributions.Normal(0.0, 1.0, MersenneTwister);
            }
        }

        public static void ReseedUniformDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwister(seed);
                UniformDist = new MathNet.Numerics.Distributions.ContinuousUniform(0.0, 1.0, MersenneTwister);
            }
        }

        public static void ReseedTroschuetzCUD()
        {
            MT19937gen = new Troschuetz.Random.MT19937Generator();
            TroschuetzCUD = new Troschuetz.Random.ContinuousUniformDistribution(Rand.MT19937gen);
        }

        /// <summary>
        /// generate a random normal
        /// </summary>
        /// <param name="dim">the normal's dimension</param>
        /// <returns>the normal</returns>
        public static DenseVector RandomDirection(int dim)
        {
            // random direction
            Vector dir = new DenseVector(dim);

            do
            {
                for (int i = 0; i < dim; i++)
                {
                    dir[i] = NormalDist.Sample();
                }
            }
            while (dir.Norm(pNorm) == 0.0);

            return (DenseVector)dir.Normalize(pNorm);
        }
    }

    class Utility
    {
        /// <summary>
        /// creates a clone of an object
        /// </summary>
        /// <param name="o">pointer to the object to be cloned</param>
        /// <returns>a handle to the copy</returns>
        public static object DeepClone(object o)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, o);
                ms.Position = 0;

                return formatter.Deserialize(ms);
            }
        }
    }

}
