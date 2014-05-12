using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

namespace Daphne
{
    /// <summary>
    /// Distributions and random number sources.
    /// This code was copied from Plaza Sur.
    /// </summary>
    class Rand
    {
        public static MersenneTwisterRandomSource MersenneTwister;
        public static NormalDistribution NormalDist;
        public static ContinuousUniformDistribution UniformDist;
        //public static SystemRandomSource SystemRandom;

        static Rand()
        {
            MersenneTwister = new MersenneTwisterRandomSource();
            NormalDist = new NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            UniformDist = new ContinuousUniformDistribution(MersenneTwister);
            UniformDist.SetDistributionParameters(0.0, 1.0);
        }

        public static void ReseedAll(int seed)
        {
            MersenneTwister = new MersenneTwisterRandomSource(seed);
            NormalDist = new NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            UniformDist = new ContinuousUniformDistribution(MersenneTwister);
            UniformDist.SetDistributionParameters(0.0, 1.0);
            //SystemRandom = new SystemRandomSource(seed);
        }

        public static void ReseedNormalDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwisterRandomSource(seed);
                NormalDist = new NormalDistribution(MersenneTwister);
                NormalDist.SetDistributionParameters(0.0, 1.0);
            }
        }

        public static void ReseedUniformDist(int seed)
        {
            if (seed > 0)
            {
                MersenneTwister = new MersenneTwisterRandomSource(seed);
                UniformDist = new ContinuousUniformDistribution(MersenneTwister);
                UniformDist.SetDistributionParameters(0.0, 1.0);
            }
        }
    }

}