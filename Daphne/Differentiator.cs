using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    /// <summary>
    /// base class for a differentiatior
    /// </summary>
    public abstract class IDifferentiator : IDynamic
    {
        public bool TransitionOccurred { get; set; }
        public int CurrentState { get; set; }
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
        public void InjectDiffBehavior(ITransitionDriver behavior)
        {
            diffBehavior = behavior;
        }

        protected ITransitionDriver diffBehavior;

        public ITransitionDriver DiffBehavior
        {
            get { return diffBehavior; }
        }
    }

    /// <summary>
    /// Contains differentiation TransitionDriver and epigenetic map 
    /// </summary>
    public class Differentiator : IDifferentiator
    {
        public Differentiator()
        {
        }

        public override  void Initialize(int _nStates, int _nGenes)
        {
            CurrentState = 0;
            TransitionOccurred = false;
            nStates = _nStates;
            nGenes = _nGenes;
            activity = new double[nStates, nGenes];
            gene_id = new string[nGenes];
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
            DiffBehavior.Step(dt);
            if (DiffBehavior.TransitionOccurred == true)
            {
                // Epigentic changes are implemented by Cell
                CurrentState = DiffBehavior.CurrentState;
                TransitionOccurred = true;
            }
            DiffBehavior.TransitionOccurred = false;
        }
    }

    public class Gene
    {
        public string Name { get; private set; }
        public int CopyNumber { get; private set; }
        // Activation level may be adjusted depending on cell state 
        public double ActivationLevel { get; set; }

        public Gene(string name, int copyNumber, double actLevel)
        {
            Name = name;
            CopyNumber = copyNumber;
            ActivationLevel = actLevel;
        }
    }

}
