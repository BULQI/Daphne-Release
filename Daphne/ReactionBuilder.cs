using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    /// <summary>
    /// Provides the means for creating arrays of reactions from SBML or other formal specification.
    /// </summary>
    public static class ReactionBuilder
    {
        /// <summary>
        /// Takes a set of reactions encoded in SBML and returns an array of objects of type Reaction.
        /// </summary>
        /// <param name="ReactionSpecs">
        /// A string in SBML that specifies the reactions to be carried out.</param>
        /// <param name="mpops">
        /// The molecular populations that will engage in the reactions. The array must be exactly the same
        /// form as the one that will be used in simulating the reactions.
        /// </param>
        /// <returns>
        /// An array of Reactions that can be used in the simulator.</returns>
        public static Reaction[] Go(string ReactionSpecs, MolecularPopulation[] mpops)
        {
            List<Reaction> reactions = new List<Reaction>();

            return reactions.ToArray();
        }

    }
}
