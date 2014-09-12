using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

using Ninject;
using Ninject.Parameters;

using ManifoldRing;

namespace Daphne
{
    public class Molecule
    {
        public string Name { get; private set; }
        public double MolecularWeight { get; private set; }
        public double EffectiveRadius { get; private set; }
        public double DiffusionCoefficient { get; private set; }
        private static double boltzmannConstant = 0;

        public Molecule(string name, double mw, double effRad, double diffCoeff)
        {
            Name = name;
            MolecularWeight = mw;
            EffectiveRadius = effRad;
            DiffusionCoefficient = diffCoeff;
        }

        public void ComputeDiffusionCoefficient(double viscosity, double temperature)
        {
            DiffusionCoefficient = boltzmannConstant * temperature / (6 * Math.PI * viscosity * EffectiveRadius);
        }
    }


    public class MolecularPopulation : IDynamic
    {
        // the individuals that make up this MolecularPopulation
        public Molecule Molecule { get; private set; }
        private Compartment compartment;
        private readonly Manifold manifold;
        private ScalarField concentration;
        private Dictionary<int, ScalarField> boundaryFluxes;
        private readonly Dictionary<int, ScalarField> boundaryConcs,
                                                      naturalBoundaryFluxes,
                                                      naturalBoundaryConcs;
        // Switch that allows us to turn off diffusion.
        // Diffusion is on, by default.
        public bool IsDiffusing { get; set; }
        public Dictionary<int, MolBoundaryType> boundaryCondition;
        // the molecule guid reference
        public string MoleculeKey { get; set; }

        public Manifold Man
        {
            get { return manifold; }
        }

        public ScalarField Conc
        {
            get { return concentration; }
            set { concentration = value; }
        }

        public Dictionary<int, ScalarField> BoundaryFluxes
        {
            get { return boundaryFluxes; }
            set { boundaryFluxes = value; }
        }

        public Dictionary<int, ScalarField> BoundaryConcs
        {
            get { return boundaryConcs; }
        }

        public Dictionary<int, ScalarField> NaturalBoundaryFluxes
        {
            get { return naturalBoundaryFluxes; }
        }

        public Dictionary<int, ScalarField> NaturalBoundaryConcs
        {
            get { return naturalBoundaryConcs; }
        }

        public MolecularPopulation(Molecule mol, string moleculeKey, Compartment comp)
        {
            concentration = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", comp.Interior));
            manifold = comp.Interior;
            Molecule = mol;
            MoleculeKey = moleculeKey;
            compartment = comp;
            boundaryCondition = new Dictionary<int, MolBoundaryType>();

            // true boundaries
            boundaryFluxes = new Dictionary<int, ScalarField>();
            boundaryConcs = new Dictionary<int, ScalarField>();
            foreach (KeyValuePair<int, Compartment> kvp in compartment.Boundaries)
            {
                AddBoundaryFluxConc(kvp.Key, kvp.Value.Interior);
            }

            // natural boundaries
            naturalBoundaryFluxes = new Dictionary<int, ScalarField>();
            naturalBoundaryConcs = new Dictionary<int, ScalarField>();
            foreach (KeyValuePair<int, Manifold> kvp in compartment.NaturalBoundaries)
            {
                naturalBoundaryFluxes.Add(kvp.Key, SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", kvp.Value)));
                naturalBoundaryConcs.Add(kvp.Key, SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", kvp.Value)));
            }
        }

        /// <summary>
        /// add boundary flux and concentration for a manifold
        /// </summary>
        /// <param name="key">dictionary key</param>
        /// <param name="m">boundary manifold</param>
        public void AddBoundaryFluxConc(int key, Manifold m)
        {
            ScalarField boundFlux = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            ScalarField boundConc = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));

            boundaryFluxes.Add(key, boundFlux);
            boundaryConcs.Add(key, boundConc);
        }


        public void Initialize(string type, double[] parameters)
        {
            this.Conc.Initialize(type, parameters);
            //for boundaryConc - only one bounary exist for cell and only cell boundary are saved
            if (type == "explicit" && parameters.Length > concentration.M.ArraySize)
            {
                //reset boundary conc and flux, only for cell and only one boundary per molpop
                int src_index = Conc.M.ArraySize;
                foreach (KeyValuePair<int, ScalarField> kvp in boundaryConcs)
                {
                    int arr_len = kvp.Value.M.ArraySize;
                    double[] newvals = new double[arr_len];

                    Array.Copy(parameters, src_index, newvals, 0, arr_len);
                    kvp.Value.Initialize(type, newvals);
                    src_index += arr_len;
                }

                foreach (KeyValuePair<int, ScalarField> kvp in boundaryFluxes)
                {
                    int arr_len = kvp.Value.M.ArraySize;
                    double[] newvals = new double[arr_len];

                    Array.Copy(parameters, src_index, newvals, 0, arr_len);
                    kvp.Value.Initialize(type, newvals);
                    src_index += arr_len;
                }
            }
        }

        /// <summary>
        /// retrieve the field data as one array
        /// </summary>
        /// <returns></returns>
        public double[] CopyArray()
        {
            int arr_len = Conc.M.ArraySize;

            foreach (ScalarField s in BoundaryConcs.Values)
            {
                arr_len += s.M.ArraySize;
            }
            foreach (ScalarField s in BoundaryFluxes.Values)
            {
                arr_len += s.M.ArraySize;
            }

            double[] valarr = new double[arr_len];
            int dst_index = Conc.CopyArray(valarr);

            foreach (ScalarField s in BoundaryConcs.Values)
            {
                dst_index += s.CopyArray(valarr, dst_index);
            }
            foreach (ScalarField s in BoundaryFluxes.Values)
            {
                s.CopyArray(valarr, dst_index);
            }

            return valarr;
        }

        /// <summary>
        /// Upadate boundaryConcs.
        /// boundaryConc is the representation of the (bulk) concentration at the boundary surface.
        /// bouncaryConcs are used in bulk/boundary reactions.
        /// </summary>
        public void UpdateBoundary()
        {
            foreach (KeyValuePair<int, ScalarField> kvp in boundaryConcs)
            {
                kvp.Value.Restrict(concentration, compartment.BoundaryTransforms[kvp.Key]);
            }
        }

        /// <summary>
        /// The evolution native to a molecular population is diffusion.
        /// The step method evolves the diffusion through dt time units.
        /// </summary>
        /// <param name="dt">The time interval over which to integrate the diffusion equation.</param>
        public void Step(double dt)
        {
            // Laplacian
            concentration.Add(concentration.Laplacian().Multiply(dt * Molecule.DiffusionCoefficient));

            // Boundary fluxes
            foreach (KeyValuePair<int, ScalarField> kvp in boundaryFluxes)
            {
                concentration.Add(concentration.DiffusionFluxTerm(kvp.Value, compartment.BoundaryTransforms[kvp.Key]).Multiply(-dt));
                kvp.Value.reset(0);
            }

            // Natural boundary conditions
            // NOTE: we should be able to incorporate these into Laplacian()
            foreach (KeyValuePair<int, MolBoundaryType> bc in boundaryCondition)
            {
                if (bc.Value == MolBoundaryType.Dirichlet)
                {
                    concentration = concentration.DirichletBC(NaturalBoundaryConcs[bc.Key], compartment.NaturalBoundaryTransforms[bc.Key]);
                }
                else
                {
                    concentration.Add(concentration.DiffusionFluxTerm(NaturalBoundaryFluxes[bc.Key], compartment.NaturalBoundaryTransforms[bc.Key]).Multiply(-dt / Molecule.DiffusionCoefficient));
                }
            }
        }
    }
}
