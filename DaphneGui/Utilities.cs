using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RandomSources;


namespace DaphneGui
{
    class Utilities
    {
        public static MersenneTwisterRandomSource MersenneTwister;
        public static NormalDistribution NormalDist;
        public static SystemRandomSource SystemRandom;
        public static Troschuetz.Random.MT19937Generator TroschuetzMTGenerator;
        public static Troschuetz.Random.BernoulliDistribution TroschuetzBernoulli;

        static Utilities()
        {
            MersenneTwister = new MersenneTwisterRandomSource();
            NormalDist = new NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            SystemRandom = new SystemRandomSource();
            TroschuetzMTGenerator = new Troschuetz.Random.MT19937Generator();
            TroschuetzBernoulli = new Troschuetz.Random.BernoulliDistribution(TroschuetzMTGenerator);
        }

        public static void ReseedAll(int seed)
        {
            MersenneTwister = new MersenneTwisterRandomSource(seed);
            NormalDist = new NormalDistribution(MersenneTwister);
            NormalDist.SetDistributionParameters(0.0, 1.0);
            SystemRandom = new SystemRandomSource(seed);
            TroschuetzMTGenerator = new Troschuetz.Random.MT19937Generator(seed);
            TroschuetzBernoulli = new Troschuetz.Random.BernoulliDistribution(TroschuetzMTGenerator);
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

        /// <summary>
        /// add up the values in a dictionary; useful for adding up concentrations in a chemokine for one solfac type
        /// </summary>
        /// <param name="dict">the dictionary</param>
        /// <returns>the sum of entries</returns>
        public static double AddDoubleValues(Dictionary<string, double> dict)
        {
            double result = 0;

            foreach (double d in dict.Values)
            {
                result += d;
            }
            return result;
        }

        /// <summary>
        /// add up the vectors in a dictionary; useful for adding up gradients in a chemokine for one solfac type
        /// </summary>
        /// <param name="dict">the dictionary</param>
        /// <param name="dim">the vector's dimension</param>
        /// <returns>the sum of vectors</returns>
        public static Vector AddVectorValues(Dictionary<string, Vector> dict, int dim)
        {
            double[] result = new double[dim];

            foreach (double[] v in dict.Values)
            {
                result[0] += v[0];
                result[1] += v[1];
                result[2] += v[2];
            }
            return result;
        }

        public static void UpdateOrder(ref int[] arr, int num, bool randomized, int randomUpdateFraction)
        {
            if (arr == null || arr.Length != num)
            {
                arr = new int[num];
                for (int i = 0; i < num; i++)
                {
                    arr[i] = i;
                }
            }
            if (randomized == true)
            {
                int i1, i2, tmp;

                for (int i = 0; i < num / randomUpdateFraction; i++)
                {
                    do
                    {
                        i1 = SystemRandom.Next(0, num);
                        i2 = SystemRandom.Next(0, num);
                    } while (i1 == i2);

                    // swap
                    tmp     = arr[i1];
                    arr[i1] = arr[i2];
                    arr[i2] = tmp;
                }
            }
        }
    }
}
