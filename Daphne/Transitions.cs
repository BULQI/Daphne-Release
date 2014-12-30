using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    /// <summary>
    /// base class for the transition driver
    /// </summary>
    public abstract class ITransitionDriver : IDynamic
    {
        public bool TransitionOccurred { get; set; }
        public int CurrentState { get; set; }
        public int PreviousState { get; set; }
        public int FinalState { get; set; }
        public abstract void AddDriverElement(int origin, int destination, TransitionDriverElement driverElement);
        public abstract Dictionary<int, Dictionary<int, TransitionDriverElement>> Drivers { get; }
        public abstract void Step(double dt);
    }

    /// <summary>
    /// the transition driver will hold a matrix of elements of this type
    /// </summary>
    public class TransitionDriverElement
    {
        public virtual bool TransitionOccurred(double clock)
        {
            return false;
        }

        public virtual void Initialize()
        {
        }

        public virtual void Reset()
        {
        }
    }

    public class MolTransitionDriverElement : TransitionDriverElement
    {
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public MolecularPopulation DriverPop { get; set; }

        public MolTransitionDriverElement()
        {
        }

        public MolTransitionDriverElement(double _alpha, double _beta, MolecularPopulation molpop)
        {
            Alpha = _alpha;
            Beta = _beta;
            DriverPop = molpop;
        }

        public double RateConstant()
        {
            if (DriverPop == null)
            {
                return 0;
            }
            return Alpha + Beta * DriverPop.Conc.MeanValue();
        }

        public override bool TransitionOccurred(double clock)
        {
            // this transition driver does not use clock
            if (Rand.UniformDist.Sample() < RateConstant())
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// the distribution transition driver will hold a matrix of elements of this type
    /// </summary>
    public class DistrTransitionDriverElement : TransitionDriverElement
    {
        public ParameterDistribution distr { get; set; }

        public double timeToNextEvent;

        public DistrTransitionDriverElement()
        {
        }

        public override void Initialize()
        {
            timeToNextEvent = distr.Sample();
        }

        public override bool TransitionOccurred(double clock)
        {
            if (timeToNextEvent <= clock)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// currently used for all transitions
    /// </summary>
    public class TransitionDriver : ITransitionDriver
    {
        private Dictionary<int, Dictionary<int, TransitionDriverElement>> drivers;
        private double clock;
        private List<int> events;

        /// <summary>
        /// Constructor
        /// </summary>
        public TransitionDriver()
        {
            drivers = new Dictionary<int, Dictionary<int, TransitionDriverElement>>();
            TransitionOccurred = false;
            CurrentState = 0;
            PreviousState = 0;
            FinalState = 0;
            events = new List<int>();
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
            if (origin > FinalState)
            {
                FinalState = origin;
            }
            if (destination > FinalState)
            {
                FinalState = destination;
            }
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
            if ( (drivers.Count == 0) || (!drivers.ContainsKey(CurrentState)) )
            {
                return;
            }

            clock += dt;

            foreach (KeyValuePair<int, TransitionDriverElement> kvp in drivers[CurrentState])
            {
                if (kvp.Value.TransitionOccurred(clock) == true)
                {
                    events.Add(kvp.Key);
                }
            }

            if (events.Count > 0)
            {
                int newState = 0;

                if (events.Count > 1)
                {
                    // randomly choose one of the transition events
                    double d = events.Count * Rand.UniformDist.Sample();
                    for (int j = 0; j < events.Count; j++)
                    {
                        if (d <= j + 1)
                        {
                            newState = j;
                            break;
                        }
                    }
                }
                else
                {
                    newState = events.First();
                }

                PreviousState = CurrentState;
                CurrentState = newState;
                TransitionOccurred = true;
                events.Clear();
                clock = 0;
                InitializeState();
            }
        }

        /// <summary>
        /// Causes clock-drivent events to select a new time-to-next-event
        /// </summary>
        public void InitializeState()
        {
            if (drivers.ContainsKey(CurrentState))
            {
                foreach (KeyValuePair<int, TransitionDriverElement> kvp in drivers[CurrentState])
                {
                    kvp.Value.Initialize();
                }
            }
        }
    }

    /// <summary>
    /// base class for a differentiatior and divider
    /// </summary>
    public abstract class ITransitionScheme : IDynamic
    {
        public bool TransitionOccurred { get; set; }
        public int CurrentState { get; set; }
        public int PreviousState { get; set; }
        public int nStates { get; set; }
        public int nGenes { get; set; }
        public abstract void AddActivity(int _state, int _gene, double _activity);
        public abstract void AddGene(int _state, string _gene_id);
        public abstract void AddState(int _state, string stateName);
        public string[] gene_id { get; set; }
        public double[,] activity { get; set; }
        public abstract void Step(double dt);
        public string[] State { get; set; }

        public abstract void Initialize(int _nStates, int _nGenes);

        [Inject]
        public void InjectDiffBehavior(ITransitionDriver _behavior)
        {
            behavior = _behavior;
        }

        protected ITransitionDriver behavior;

        public ITransitionDriver Behavior
        {
            get { return behavior; }
        }
    }

    /// <summary>
    /// Contains TransitionDriver, epigenetic map, and genes for differentiation or division schemes.
    /// </summary>
    public class TransitionScheme : ITransitionScheme
    {
        public TransitionScheme()
        {
        }

        public override void Initialize(int _nStates, int _nGenes)
        {
            CurrentState = 0;
            PreviousState = 0;
            TransitionOccurred = false;
            nStates = _nStates;
            nGenes = _nGenes;
            activity = new double[nStates, nGenes];
            gene_id = new string[nGenes];
            State = new string[nStates];
        }

        public override void AddActivity(int _state, int _gene, double _activity)
        {
            activity[_state, _gene] = _activity;
        }

        public override void AddGene(int _gene, string _gene_guid)
        {
            gene_id[_gene] = _gene_guid;
        }

        public override void AddState(int _state, string stateName)
        {
            State[_state] = stateName;
        }

        public override void Step(double dt)
        {
            Behavior.Step(dt);
            if (Behavior.TransitionOccurred == true)
            {
                // Epigentic changes are implemented by Cell
                CurrentState = Behavior.CurrentState;
                PreviousState = Behavior.PreviousState;
                TransitionOccurred = true;
                Behavior.TransitionOccurred = false;
            }
        }
    }
}
