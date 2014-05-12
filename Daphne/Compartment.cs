using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ninject;
using Ninject.Parameters;

using ManifoldRing;

namespace Daphne
{
    /// <summary>
    /// Manages the chemical reactions that occur within the interior manifold of the compartment and between the interior and the boundaries. 
    /// All molecular populations must be defined on either the interior manifold or one of the boundary manifolds.
    /// rtList keeps track of the ReactionTemplates that have been assigned to this compartment. Avoids duplication
    /// </summary>
    public class Compartment : IDynamic
    {
        public Compartment(Manifold interior)
        {
            Interior = interior;
            Populations = new Dictionary<string, MolecularPopulation>();
            Reactions = new List<Reaction>();
            RTList = new List<ReactionTemplate>();
            Boundaries = new Dictionary<int, Compartment>();
            BoundaryTransforms = new Dictionary<int, Transform>();
            NaturalBoundaries = new Dictionary<int, Manifold>();
            NaturalBoundaryTransforms = new Dictionary<int, Transform>();
        }

        public void AddMolecularPopulation(string moleculeKey, string type, double[] parameters)
        {
            if (Simulation.dataBasket.Molecules.ContainsKey(moleculeKey) == false)
            {
                throw new Exception("Invalid molecule key.");
            }

            Molecule mol = Simulation.dataBasket.Molecules[moleculeKey];
            MolecularPopulation mp = SimulationModule.kernel.Get<MolecularPopulation>(new ConstructorArgument("mol", mol), new ConstructorArgument("comp", this));

            mp.Initialize(type, parameters);

            if (Populations.ContainsKey(moleculeKey) == false)
            {
                Populations.Add(moleculeKey, mp);
            }
            else
            {
                // add together
                Populations[moleculeKey].Conc += mp.Conc;
                // NOTE: presumably, we need to also add the boundaries here
            }
        }

        public bool HasThisReaction(ReactionTemplate rt)
        {
            if (RTList.Count == 0)
            {
                return false;
            }

            return RTList.Contains(rt);
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
                    AddMolecularPopulation(spRef.species, "const", new double[] { 0.0 });
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
            foreach (Reaction r in Reactions)
            {
                r.Step(dt);
            }

            //double[] pos;
            foreach (KeyValuePair<string, MolecularPopulation> molpop in Populations)
            {
                // Update boundary concentrations
                molpop.Value.Step(dt);
 
                // Diffusion

                //// molpop.Value.Conc += dt * molpop.Value.Molecule.DiffusionCoefficient * molpop.Value.Conc.Laplacian();
                //foreach (KeyValuePair<int, Compartment> kvp in Boundaries)
                //{
                //    pos = BoundaryTransforms[kvp.Key].Translation;
                //    molpop.Value.Conc += -dt * molpop.Value.Conc.DiffusionFluxTerm(molpop.Value.BoundaryFluxes[kvp.Key], pos);
                //}

                //foreach (KeyValuePair<int, Manifold> kvp in NaturalBoundaries)
                //{
                //    pos = NaturalBoundaryTransforms[kvp.Key].Translation;
                //    molpop.Value.Conc += -dt * molpop.Value.Conc.DiffusionFluxTerm(molpop.Value.NaturalBoundaryFluxes[kvp.Key], pos);
                //}
            }
        }

        public Dictionary<string, MolecularPopulation> Populations { get; private set; }
        public List<Reaction> Reactions { get; private set; }
        public Manifold Interior { get; private set; }
        public List<ReactionTemplate> RTList { get; private set; }
        public Dictionary<int, Compartment> Boundaries { get; private set; }
        public Dictionary<int, Transform> BoundaryTransforms { get; set; }
        public Dictionary<int, Manifold> NaturalBoundaries { get; private set; }
        public Dictionary<int, Transform> NaturalBoundaryTransforms { get; private set; }
    }

    public class ExtraCellularSpace
    {
        private Compartment space;
        private Dictionary<string, int> sides;
        public double Gamma { get; set; }
        public bool toroidal { get; private set; }

        public ExtraCellularSpace(int[] numGridPts, double gridStep, bool toroidal, IKernel kernel)
        {
            InterpolatedRectangularPrism p = kernel.Get<InterpolatedRectangularPrism>();
            double[] data = new double[] { numGridPts[0], numGridPts[1], numGridPts[2], gridStep, Convert.ToDouble(toroidal) };

            // boundary condition
            this.toroidal = toroidal;

            p.Initialize(data);
            space = new Compartment(p);
            sides = new Dictionary<string, int>();

            // add the sides and transforms

            InterpolatedRectangle r;
            Transform t;
            double[] axis = new double[Transform.Dim];

            data = new double[space.Interior.Dim+1];
            // front: no rotation, translate +z
            data[0] = space.Interior.NodesPerSide(0);
            data[1] = space.Interior.NodesPerSide(1);
            data[2] = space.Interior.StepSize();
            // Toroidal BCs are not relevant for sides
            data[3] = Convert.ToDouble(false);
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.translate(new double[] { 0, 0, space.Interior.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("front", r.Id);

            // back: rotate by pi about y, translate +x
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            axis[1] = 1;
            t.rotate(axis, Math.PI);
            t.translate(new double[] { space.Interior.Extent(0), 0, 0 });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("back", r.Id);

            // right: rotate by pi/2 about y, translate +x, +z
            data[0] = space.Interior.NodesPerSide(2);
            data[1] = space.Interior.NodesPerSide(1);
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            t.translate(new double[] { space.Interior.Extent(0), 0, space.Interior.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("right", r.Id);

            // left: rotate by -pi/2 about y, no translation
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, -Math.PI / 2.0);
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("left", r.Id);

            // top: rotate by -pi/2 about x, translate +y, +z
            data[0] = space.Interior.NodesPerSide(0);
            data[1] = space.Interior.NodesPerSide(2);
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            axis[0] = 1;
            axis[1] = 0;
            t.rotate(axis, -Math.PI / 2.0);
            t.translate(new double[] { 0, space.Interior.Extent(1), space.Interior.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("top", r.Id);

            // bottom: rotate by pi/2 about x, no translation
            r = kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("bottom", r.Id);

            // drag coefficient
            Gamma = 0;
        }

        public Compartment Space
        {
            get { return space; }
        }

        public Dictionary<string, int> Sides
        {
            get { return sides; }
        }
    }

}
