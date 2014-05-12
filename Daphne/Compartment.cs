using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    /// <summary>
    /// Manages the chemical reactions that occur within the interior manifold of the compartment and between the interior and the boundaries. 
    /// All molecular populations must be defined on either the interior manifold or one of the boundary manifolds.
    /// </summary>
    public class Compartment
    {
        public Compartment(DiscretizedManifold interior)
        {
            Interior = interior;
        }

        public void AddMolecule(Molecule molecule)
        {
            Populations.Add(new MolecularPopulation(molecule, Interior));
        }

        public void AddMolecularPopulation(MolecularPopulation molpop)
        {
            Populations.Add(molpop);
        }

        /// <summary>
        /// Carries out the dynamics in-place for its molecular populations over time interval dt.
        /// </summary>
        /// <param name="dt">The time interval.</param>
        public void Step(double dt)
        {
            // the step method may organize the reactions in a more sophisticated manner to account
            // for different rate constants etc.
            foreach (Reaction r in reactions)
            {
                r.Step(dt);
            }
        }

        List<MolecularPopulation> Populations;
        List<Reaction> reactions;
        public DiscretizedManifold Interior;

    }
}
