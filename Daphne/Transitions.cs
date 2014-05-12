using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Daphne
{
    /// <summary>
    /// base class for the transition driver
    /// </summary>
    public abstract class ITransitionDriver : IDynamic
    {
        public bool TransitionOccurred { get; set; }
        public int CurrentState { get; set; }
        public abstract void AddDriverElement(int origin, int destination, TransitionDriverElement driverElement);
        public abstract Dictionary<int, Dictionary<int, TransitionDriverElement>> Drivers { get; }
        public abstract void Step(double dt);
    }

    /// <summary>
    /// the transition driver will hold a matrix of elements of this type
    /// </summary>
    public class TransitionDriverElement
    {
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public MolecularPopulation DriverPop { get; set; }

        public double RateConstant()
        {
            if (DriverPop == null)
            {
                return 0;
            }
            return Alpha + Beta * DriverPop.Conc.MeanValue();
        }
    }

    public class PMF<T>
    {
        private T[] keys;
        private double[] cumulatives;
        private Dictionary<T, double> probability;

        static PMF()
        {
        }

        public PMF()
        {
            probability = new Dictionary<T, double>();
        }

        public void Initialize(T[] _keys, double[] _probabilities)
        {
            if (_keys.Length != _probabilities.Length)
            {
                throw new ArgumentException("Argument lengths are not the same.");
            }
            if (_keys.Length == 0)
            {
                throw new ArgumentException("Attempting to use zero length arrays.");
            }

            probability.Clear();
            for (int i = 0; i < _probabilities.Length; i++)
            {
                probability.Add(_keys[i], _probabilities[i]);
            }

            keys = (T[])_keys.Clone();
            Array.Sort(keys);
            Array.Reverse(keys);

            cumulatives = new double[probability.Count];

            cumulatives[0] = probability[keys[0]];
            for (int i = 1; i < cumulatives.Length; i++)
            {
                cumulatives[i] = cumulatives[i - 1] + probability[keys[i]];
            }
        }

        public T Next()
        {
            double u = Rand.TroschuetzCUD.NextDouble();

            for (int i = 0; i < cumulatives.Length; i++)
            {
                if (u <= cumulatives[i])
                {
                    return keys[i];
                }
            }
            return keys.Last();
        }
    }

    /// <summary>
    /// currently used for all transitions
    /// </summary>
    public class TransitionDriver : ITransitionDriver
    {
        private Dictionary<int, Dictionary<int, TransitionDriverElement>> drivers;
        private PMF<int> destinationPMF;

        /// <summary>
        /// Constructor
        /// </summary>
        public TransitionDriver()
        {
            drivers = new Dictionary<int, Dictionary<int, TransitionDriverElement>>();
            TransitionOccurred = false;
            CurrentState = 0;
            destinationPMF = new PMF<int>();
        }

        /// <summary>
        /// add a new driver element
        /// </summary>
        /// <param name="origin">origin state</param>
        /// <param name="destination">destination state</param>
        /// <param name="driverElement">driver element with alpha, beta, signaling molpop</param>
        public override void AddDriverElement(int origin, int destination, TransitionDriverElement driverElement)
        {
            if (!drivers.ContainsKey(origin))
            {
                drivers.Add(origin, new Dictionary<int, TransitionDriverElement>());
            }
            if (drivers[origin].ContainsKey(destination))
            {
                drivers.Remove(destination);
            }
            drivers[origin].Add(destination, driverElement);
        }

        /// <summary>
        /// accessor for drivers
        /// </summary>
        public override Dictionary<int, Dictionary<int, TransitionDriverElement>> Drivers
        {
            get { return drivers; }
        }

        /// <summary>
        /// Executes a step of the stochastic dynamics for the Transition
        /// </summary>
        /// <param name="dt">The time interval for the evolution (double).</param>
        public override void Step(double dt)
        {
            if (drivers.Count == 0)
            {
                return;
            }

            double TotalRate = 0,
                   u = Rand.TroschuetzCUD.NextDouble();

            foreach (KeyValuePair<int, TransitionDriverElement> kvp in drivers[CurrentState])
            {
                TotalRate += kvp.Value.RateConstant();
            }
            if (u < TotalRate * dt)
            {
                TransitionOccurred = true;

                double[] probabilities = new double[drivers[CurrentState].Count];
                int[] destinations = new int[drivers[CurrentState].Count];
                int iDest = 0;

                foreach (KeyValuePair<int, TransitionDriverElement> kvp in drivers[CurrentState])
                {
                    probabilities[iDest] = kvp.Value.RateConstant() / TotalRate;
                    destinations[iDest] = kvp.Key;
                    iDest++;
                }
                destinationPMF.Initialize(destinations, probabilities);
                CurrentState = destinationPMF.Next();
            }
        }
    }
}
