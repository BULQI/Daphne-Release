using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    /// <summary>
    /// Manages the chemical reactions that occur within the interior manifold of the compartment and between the interior and the boundaries. 
    /// All molecular populations must be defined on either the interior manifold or one of the boundary manifolds.
    /// rtList keeps track of the ReactionTemplates that have been assigned to this compartment. Avoids duplication
    /// </summary>
    public class Compartment
    {
        public Compartment(Manifold interior)
        {
            Interior = interior;
            Populations = new Dictionary<string, MolecularPopulation>();
            reactions = new List<Reaction>();
            rtList = new List<ReactionTemplate>();
        }

        // gmk
        public void AddMolecularPopulation(Molecule mol, double initConc)
        {
            ScalarField s = new DiscreteScalarField(Interior, new ConstFieldInitializer(initConc));
            MolecularPopulation molpop = new MolecularPopulation(mol, s, null);

            Populations.Add(molpop.Molecule.Name, molpop);
        }

        public void AddMolecularPopulation(Molecule mol, ScalarField initConc)
        {
            // Add the molecular population with concentration specified with initConc
            MolecularPopulation molpop = new MolecularPopulation(mol, initConc, null);

            Populations.Add(molpop.Molecule.Name, molpop);
        }

        public void AddMolecularPopulation(Molecule mol, double initConc, double[] initGrad)
        {
            // Add the molecular population with concentration specified with initConc
            ScalarField s = new DiscreteScalarField(Interior, new ConstFieldInitializer(initConc));
            VectorField v = new VectorField(Interior, initGrad);
            MolecularPopulation molpop = new MolecularPopulation(mol, s, v);

            Populations.Add(molpop.Molecule.Name, molpop);
        }

        public bool HasThisReaction(ReactionTemplate rt)
        {
            if (rtList.Count == 0) return false;

            return rtList.Contains(rt) == true;
        }

        public bool HasAllReactants(ReactionTemplate rt)
        {
            // find if all the species in the reaction template have matches in the molecular populations
            if (rt.listOfReactants.Count == 0)
            {
                return true;
            }

            foreach (SpeciesReference spRef in rt.listOfReactants)
            {
                // as soon as there is one not found we can return false
                if (Populations.ContainsKey(spRef.species) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAllModifiers(ReactionTemplate rt)
        {
            // find if all the species in the reaction template have matches in the molecular populations
            if (rt.listOfModifiers.Count == 0)
            {
                return true;
            }

            foreach (SpeciesReference spRef in rt.listOfModifiers)
            {
                // as soon as there is one not found we can return false
                if (Populations.ContainsKey(spRef.species) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public void AddNeededProducts(ReactionTemplate rt, Dictionary<string, Molecule> MolDict)
        {
            // Check the product molecules exist as MolecularPopulations in the Compartment
            // If not, add a molecular population to the compartment
            foreach (SpeciesReference spRef in rt.listOfProducts)
            {
                // not contained? add it
                if (Populations.ContainsKey(spRef.species) == false)
                {
                    AddMolecularPopulation(MolDict[spRef.species], 0.0);
                }
            }
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

            foreach (KeyValuePair<string, MolecularPopulation> molpop in Populations)
            {
                molpop.Value.Step(dt);
            }
        }

        public Dictionary<string, MolecularPopulation> Populations;
        public List<Reaction> reactions;
        public Manifold Interior;
        public List<ReactionTemplate> rtList;
    }

    public class ExtracellularTypeI
    {
        public Compartment XCellSpace;

        public ExtracellularTypeI(int[] numGridPts, double[] XCellSpatialExtent)
        {
            BoundedRectangularPrism b = new BoundedRectangularPrism(numGridPts, XCellSpatialExtent);
            Compartment XCellSpace = new Compartment(b);

        }
    }

}
