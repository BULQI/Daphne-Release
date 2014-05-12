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
            Populations = new List<MolecularPopulation>();
            reactions = new List<Reaction>();
            rtList = new List<ReactionTemplate>();
            molpopDict = new Dictionary<string,MolecularPopulation>();
       }

        public void AddMolecule(Molecule molecule)
        {
            Populations.Add(new MolecularPopulation(molecule, Interior));
        }

        public void AddMolecularPopulation(MolecularPopulation molpop)
        {
            Populations.Add(molpop);
            molpopDict.Add(molpop.Name, molpop);
        }

        // gmk
        public void AddMolecularPopulation(string name, Molecule mol, double initConc)
        {
            ScalarField s = new ScalarField(Interior, initConc);
            MolecularPopulation molpop = new MolecularPopulation(name, mol, Interior, s);
            Populations.Add(molpop);
            molpopDict.Add(molpop.Molecule.Name, molpop);
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

            foreach (MolecularPopulation molpop in Populations)
            {
                molpop.Step(dt);
            }
        }

        public List<MolecularPopulation> Populations;
        public List<Reaction> reactions;
        public DiscretizedManifold Interior;
        public List<ReactionTemplate> rtList;
        public Dictionary<string, MolecularPopulation> molpopDict;
    }
}
