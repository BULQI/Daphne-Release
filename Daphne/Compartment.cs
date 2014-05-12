using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ManifoldRing;

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
            Reactions = new List<Reaction>();
            RTList = new List<ReactionTemplate>();
            Boundaries = new Dictionary<int, Compartment>();
            BoundaryTransforms = new Dictionary<int, Transform>();
            NaturalBoundaries = new Dictionary<int, Manifold>();
            NaturalBoundaryTransforms = new Dictionary<int, Transform>();
        }

        // gmk
        public void AddMolecularPopulation(Molecule mol, double initConc)
        {
            ScalarField s = new ScalarField(Interior, new ConstFieldInitializer(initConc));
            MolecularPopulation molpop = new MolecularPopulation(mol, s, this);

            Populations.Add(molpop.Molecule.Name, molpop);
        }

        public void AddMolecularPopulation(Molecule mol, IFieldInitializer initConc)
        {
            // Add the molecular population with concentration specified with initConc
            ScalarField s = new ScalarField(Interior, initConc);
            MolecularPopulation molpop = new MolecularPopulation(mol, s, this);

            Populations.Add(molpop.Molecule.Name, molpop);
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
            foreach (Reaction r in Reactions)
            {
                r.Step(dt);
            }

            foreach (KeyValuePair<string, MolecularPopulation> molpop in Populations)
            {
                molpop.Value.Step(dt);
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

        public ExtraCellularSpace(Manifold m)
        {
            // at least for now, have this
            if (m.GetType() != typeof(InterpolatedRectangularPrism))
            {
                throw new Exception("The ECS must be based on an InterpolatedRectangularPrism manifold");
            }

            space = new Compartment(m);
            // add the sides and transforms
            
            InterpolatedRectangle r;
            Transform t;
            int[] nodes = new int[m.Dim - 1]; // can't access r.Dim before creating an instance
            double[] axis = new double[Transform.Dim];
            
            // front: no rotation, translate +z
            nodes[0] = m.NodesPerSide(0);
            nodes[1] = m.NodesPerSide(1);
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            t.translate(new double[] { 0, 0, m.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);

            // back: rotate by pi about y, translate +x
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            axis[1] = 1;
            t.rotate(axis, Math.PI);
            t.translate(new double[] { m.Extent(0), 0, 0 });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);

            // right: rotate by pi/2 about y, translate +x, +z
            nodes[0] = m.NodesPerSide(2);
            nodes[1] = m.NodesPerSide(1);
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            t.translate(new double[] { m.Extent(0), 0, m.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);

            // left: rotate by -pi/2 about y, no translation
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            t.rotate(axis, -Math.PI / 2.0);
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);

            // top: rotate by -pi/2 about x, translate +y, +z
            nodes[0] = m.NodesPerSide(0);
            nodes[1] = m.NodesPerSide(2);
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            axis[0] = 1;
            axis[1] = 0;
            t.rotate(axis, -Math.PI / 2.0);
            t.translate(new double[] { 0, m.Extent(1), m.Extent(2) });
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);

            // bottom: rotate by pi/2 about x, no translation
            r = new InterpolatedRectangle(nodes, m.StepSize());
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            space.NaturalBoundaries.Add(r.Id, r);
            space.NaturalBoundaryTransforms.Add(r.Id, t);
        }

        public Compartment Space
        {
            get { return space; }
        }
    }

}
