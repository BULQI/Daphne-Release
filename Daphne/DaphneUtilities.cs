using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;
using Troschuetz.Random;


namespace Daphne
{
    /// <summary>
    /// Distributions and random number sources.
    /// This code was copied from Plaza Sur.
    /// </summary>
    class Rand
    {
        public static MersenneTwisterRandomSource MersenneTwister;
        public static MathNet.Numerics.Distributions.NormalDistribution NormalDist;
        public static MathNet.Numerics.Distributions.ContinuousUniformDistribution UniformDist;
        //public static SystemRandomSource SystemRandom;
        public static Troschuetz.Random.MT19937Generator MT19937gen;
        public static Troschuetz.Random.ContinuousUniformDistribution TroschuetzCUD;

        static Rand()
        {
            MersenneTwister = new MersenneTwisterRandomSource();
            NormalDist = new MathNet.Numerics.Distributions.NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            UniformDist = new MathNet.Numerics.Distributions.ContinuousUniformDistribution(MersenneTwister);
            UniformDist.SetDistributionParameters(0.0, 1.0);
            MT19937gen = new Troschuetz.Random.MT19937Generator();
            TroschuetzCUD = new Troschuetz.Random.ContinuousUniformDistribution(Rand.MT19937gen);
        }

        public static void ReseedAll(int seed)
        {
            MersenneTwister = new MersenneTwisterRandomSource(seed);
            NormalDist = new MathNet.Numerics.Distributions.NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            UniformDist = new MathNet.Numerics.Distributions.ContinuousUniformDistribution(MersenneTwister);
            UniformDist.SetDistributionParameters(0.0, 1.0);
            MT19937gen = new Troschuetz.Random.MT19937Generator();
            TroschuetzCUD = new Troschuetz.Random.ContinuousUniformDistribution(Rand.MT19937gen);
            //SystemRandom = new SystemRandomSource(seed);
        }

        public static void ReseedNormalDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwisterRandomSource(seed);
                NormalDist = new MathNet.Numerics.Distributions.NormalDistribution(MersenneTwister);
                NormalDist.SetDistributionParameters(0.0, 1.0);
            }
        }

        public static void ReseedUniformDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwisterRandomSource(seed);
                UniformDist = new MathNet.Numerics.Distributions.ContinuousUniformDistribution(MersenneTwister);
                UniformDist.SetDistributionParameters(0.0, 1.0);
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
        public static Vector RandomDirection(int dim)
        {
            // random direction
            Vector dir = new double[dim];

            do
            {
                for (int i = 0; i < dim; i++)
                {
                    dir[i] = NormalDist.NextDouble();
                }
            }
            while (dir.Norm() == 0.0);

            return dir.Normalize();
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
