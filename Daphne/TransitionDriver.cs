using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    abstract class TransitionDriver
    {
        /// <summary>
        /// The background rate matrix for the transition from one state to another.
        /// </summary>
        public double[,] Alpha;
        /// <summary>
        /// The coefficient matrix of the transition rate's linear dependence on the relevant signaling molecule.
        /// </summary>
        public double[,] Beta;
        /// <summary>
        /// The matrix of molecular populations whose mean concentration serves as the transition signal.
        /// </summary>
        public MolecularPopulation[,] SignalingMolecule;
        /// <summary>
        /// Flag indicating the state of the cell relevant to this driver.
        /// </summary>
        public int Flag;

        private int presentState;

        /// <summary>
        /// Executes a step of the stochastic dynamics for TransitionDriver from the cell's present state.
        /// If a tranisition occurs during the step, the value of Flag is set to the appropriate value.
        /// </summary>
        /// <param name="dt">The time interval for the evolution (double).</param>
        public abstract void Step(double dt);

    }
}
