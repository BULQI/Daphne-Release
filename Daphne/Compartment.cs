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
    /// </summary>
    public class Compartment : IDynamic
    {
        public Compartment(Manifold interior)
        {
            Interior = interior;
            Populations = new Dictionary<string, MolecularPopulation>();
            BulkReactions = new List<Reaction>();
            BoundaryReactions = new Dictionary<int, List<Reaction>>();
            Boundaries = new Dictionary<int, Compartment>();
            BoundaryTransforms = new Dictionary<int, Transform>();
            NaturalBoundaries = new Dictionary<int, Manifold>();
            NaturalBoundaryTransforms = new Dictionary<int, Transform>();
        }

        public void AddMolecularPopulation(Molecule mol, string moleculeKey, string type, double[] parameters)
        {
            MolecularPopulation mp = SimulationModule.kernel.Get<MolecularPopulation>(new ConstructorArgument("mol", mol), new ConstructorArgument("moleculeKey", moleculeKey), new ConstructorArgument("comp", this));

            mp.Initialize(type, parameters);
            if (mp.Molecule.DiffusionCoefficient == 0)
            {
                mp.IsDiffusing = false;
            }
            else
            {
                mp.IsDiffusing = true;
            }

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
        /// <summary>
        /// Carries out the dynamics in-place for its molecular populations over time interval dt.
        /// </summary>
        /// <param name="dt">The time interval.</param>
        public void Step(double dt)
        {
            // the step method may organize the reactions in a more sophisticated manner to account
            // for different rate constants etc.
            foreach (Reaction r in BulkReactions)
            {
                r.Step(dt);
            }

            if (!(this.Interior is PointManifold))
            {
                foreach (List<Reaction> rlist in BoundaryReactions.Values)
                {
                    foreach (Reaction r in rlist)
                    {
                        r.Step(dt);
                    }
                }

                foreach (KeyValuePair<string, MolecularPopulation> molpop in Populations)
                {
                    // Update boundary concentrations
                    molpop.Value.UpdateBoundary();

                    // Apply Laplacian and boundary fluxes
                    if (molpop.Value.IsDiffusing == true)
                    {
                        molpop.Value.Step(dt);
                    }
                }
            }
        }

        public void AddBoundaryReaction(int key, Reaction r)
        {
            // create the list if it doesn't exist
            if(BoundaryReactions.ContainsKey(key) == false)
            {
                BoundaryReactions.Add(key, new List<Reaction>());
            }

            // add the reaction
            BoundaryReactions[key].Add(r);
        }

        public Dictionary<string, MolecularPopulation> Populations { get; private set; }
        public List<Reaction> BulkReactions { get; private set; }
        public Dictionary<int, List<Reaction>> BoundaryReactions { get; private set; }
        public Manifold Interior { get; private set; }
        public Dictionary<int, Compartment> Boundaries { get; private set; }
        public Dictionary<int, Transform> BoundaryTransforms { get; set; }
        public Dictionary<int, Manifold> NaturalBoundaries { get; private set; }
        public Dictionary<int, Transform> NaturalBoundaryTransforms { get; private set; }
    }

    public abstract class EnvironmentBase
    {
        protected Compartment comp;

        public EnvironmentBase()
        {
        }

        public Compartment Comp
        {
            get { return comp; }
        }
    }

    public class PointEnvironment : EnvironmentBase
    {
 	    public PointEnvironment()
	    {
            PointManifold p = SimulationModule.kernel.Get <PointManifold>();

		    comp = new Compartment(p);
        }
    }

    public class RectEnvironment : EnvironmentBase
    {
        private Dictionary<string, int> sides;
        public bool toroidal { get; private set; }

        public RectEnvironment(int[] numGridPts, double gridStep, bool toroidal)
        {
            InterpolatedRectangle r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            double[] data = new double[] { numGridPts[0], numGridPts[1], gridStep, Convert.ToDouble(toroidal) };

            // boundary condition
            this.toroidal = toroidal;

            r.Initialize(data);
            comp = new Compartment(r);
            sides = new Dictionary<string, int>();

            // add the sides and transforms

            InterpolatedLine l;
            Transform t;
            double[] axis = new double[Transform.Dim];

            data = new double[comp.Interior.Dim + 1];
            data[1] = comp.Interior.StepSize();
            // Toroidal BCs are not relevant for sides
            data[3] = Convert.ToDouble(false);

            // right: rotate by pi/2 about z, translate +x
            data[0] = comp.Interior.NodesPerSide(1);
            l = SimulationModule.kernel.Get<InterpolatedLine>();
            l.Initialize(data);
            t = new Transform();
            axis[2] = 1;
            t.rotate(axis, Math.PI / 2.0);
            t.translate(new double[] { comp.Interior.Extent(0), 0, 0 });
            comp.NaturalBoundaries.Add(l.Id, l);
            comp.NaturalBoundaryTransforms.Add(l.Id, t);
            sides.Add("right", l.Id);

            // left: rotate by pi/2 about z, no translation
            l = SimulationModule.kernel.Get<InterpolatedLine>();
            l.Initialize(data);
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            comp.NaturalBoundaries.Add(l.Id, l);
            comp.NaturalBoundaryTransforms.Add(l.Id, t);
            sides.Add("left", l.Id);

            // top: no rotation, translate +y
            data[0] = comp.Interior.NodesPerSide(0);
            l = SimulationModule.kernel.Get<InterpolatedLine>();
            l.Initialize(data);
            t = new Transform();
            t.translate(new double[] { 0, comp.Interior.Extent(1), 0 });
            comp.NaturalBoundaries.Add(l.Id, l);
            comp.NaturalBoundaryTransforms.Add(l.Id, t);
            sides.Add("top", l.Id);

            // bottom: no rotation, no translation
            l = SimulationModule.kernel.Get<InterpolatedLine>();
            l.Initialize(data);
            t = new Transform();
            comp.NaturalBoundaries.Add(l.Id, l);
            comp.NaturalBoundaryTransforms.Add(l.Id, t);
            sides.Add("bottom", l.Id);
        }

        public Dictionary<string, int> Sides
        {
            get { return sides; }
        }

        /// <summary>
        /// add a boundary manifold, i.e. insert it to each ecs molecular population's boundary and flux dictionary
        /// </summary>
        /// <param name="m">the manifold</param>
        public void AddBoundaryManifold(Manifold m)
        {
            foreach (MolecularPopulation mp in comp.Populations.Values)
            {
                mp.AddBoundaryFluxConc(m.Id, m);
            }
        }
    }

    public class ECSEnvironment : EnvironmentBase
    {
        private Dictionary<string, int> sides;
        public bool toroidal { get; private set; }

        public ECSEnvironment(int[] numGridPts, double gridStep, bool toroidal)
        {
            InterpolatedRectangularPrism p = SimulationModule.kernel.Get<InterpolatedRectangularPrism>();
            double[] data = new double[] { numGridPts[0], numGridPts[1], numGridPts[2], gridStep, Convert.ToDouble(toroidal) };

            // boundary condition
            this.toroidal = toroidal;

            p.Initialize(data);
            comp = new Compartment(p);
            sides = new Dictionary<string, int>();

            // add the sides and transforms

            InterpolatedRectangle r;
            Transform t;
            double[] axis = new double[Transform.Dim];

            data = new double[comp.Interior.Dim+1];
            // front: no rotation, translate +z
            data[0] = comp.Interior.NodesPerSide(0);
            data[1] = comp.Interior.NodesPerSide(1);
            data[2] = comp.Interior.StepSize();
            // Toroidal BCs are not relevant for sides
            data[3] = Convert.ToDouble(false);
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.translate(new double[] { 0, 0, comp.Interior.Extent(2) });
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("front", r.Id);

            // back: rotate by pi about y, translate +x
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            axis[1] = 1;
            t.rotate(axis, Math.PI);
            t.translate(new double[] { comp.Interior.Extent(0), 0, 0 });
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("back", r.Id);

            // right: rotate by pi/2 about y, translate +x, +z
            data[0] = comp.Interior.NodesPerSide(2);
            data[1] = comp.Interior.NodesPerSide(1);
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            t.translate(new double[] { comp.Interior.Extent(0), 0, comp.Interior.Extent(2) });
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("right", r.Id);

            // left: rotate by -pi/2 about y, no translation
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, -Math.PI / 2.0);
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("left", r.Id);

            // top: rotate by -pi/2 about x, translate +y, +z
            data[0] = comp.Interior.NodesPerSide(0);
            data[1] = comp.Interior.NodesPerSide(2);
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            axis[0] = 1;
            axis[1] = 0;
            t.rotate(axis, -Math.PI / 2.0);
            t.translate(new double[] { 0, comp.Interior.Extent(1), comp.Interior.Extent(2) });
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("top", r.Id);

            // bottom: rotate by pi/2 about x, no translation
            r = SimulationModule.kernel.Get<InterpolatedRectangle>();
            r.Initialize(data);
            t = new Transform();
            t.rotate(axis, Math.PI / 2.0);
            comp.NaturalBoundaries.Add(r.Id, r);
            comp.NaturalBoundaryTransforms.Add(r.Id, t);
            sides.Add("bottom", r.Id);
        }

        public Dictionary<string, int> Sides
        {
            get { return sides; }
        }

        /// <summary>
        /// add a boundary manifold, i.e. insert it to each ecs molecular population's boundary and flux dictionary
        /// </summary>
        /// <param name="m">the manifold</param>
        public void AddBoundaryManifold(Manifold m)
        {
            foreach (MolecularPopulation mp in comp.Populations.Values)
            {
                mp.AddBoundaryFluxConc(m.Id, m);
            }
        }
    }

}
