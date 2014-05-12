using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

namespace Daphne
{
    /// <summary>
    /// Class used to determine whether a cell has died and to signal its vital status to the relevant controller.
    /// </summary>
    public class Terminator
    {
        static private Troschuetz.Random.MT19937Generator gen;
        static Terminator()
        {
            gen = new MT19937Generator();
        }
        /// <summary>
        /// Constructor. Sets the death flag to zero and initializes the signaling components.
        /// </summary>
        /// <param name="alpha">The background death rate (double).</param>
        /// <param name="beta">The coefficient of the death rate's linear dependence on the signaling molecule (double).</param>
        /// <param name="sigMol">The molecular population whose mean concentration serves as the death signal (MoleculaPopulation).</param>
        public Terminator(double alpha, double beta, MolecularPopulation sigMol)
        {
            Flag = 0;
            Alpha = alpha;
            Beta = beta;
            SignalingMolecule = sigMol;
        }

        /// <summary>
        /// The background death rate.
        /// </summary>
        public double Alpha {get; set;}
        /// <summary>
        /// The coefficient of the death rate's linear dependence on the signaling molecule (double)
        /// </summary>
        public double Beta {get; set;}
        /// <summary>
        /// The molecular population whose mean concentration serves as the death signal (MoleculaPopulation).
        /// </summary>
        public MolecularPopulation SignalingMolecule { get; set; }
        /// <summary>
        /// Flag indicating whether the cell is alive (Flag = 0) or dead (Flag = 1).
        /// </summary>
        public int Flag;

        /// <summary>
        /// Executes a step of the stochastic dynamics for Terminator.
        /// </summary>
        /// <param name="dt">The time interval for the evolution (double).</param>
        public void Step(double dt)
        {
            if (gen.Next() < dt * (Alpha + Beta * SignalingMolecule.Conc.MeanValue()))
            {
                Flag = 1;
            }
        }
    }
}
