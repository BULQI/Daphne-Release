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

        // NOTE: Put this here so json deserialization would work. gmk
        // NOTE HS: we should have single constructors for Ninject to work;
        // the other entities seem to deserialize without a default constructor, at least they have none
        // revisit and reevealuate if needed
        //public MolecularPopulation()
        //{
        //}

        public MolecularPopulation(Molecule mol, Compartment comp)
        {
            concentration = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", comp.Interior));
            manifold = comp.Interior;
            Molecule = mol;
            compartment = comp;

            // true boundaries
            boundaryFluxes = new Dictionary<int, ScalarField>();
            boundaryConcs = new Dictionary<int, ScalarField>();
            foreach (KeyValuePair<int, Compartment> kvp in compartment.Boundaries)
            {
                //boundaryFluxes.Add(kvp.Key, new ScalarField(kvp.Value.Interior));
                //boundaryConcs.Add(kvp.Key, new ScalarField(kvp.Value.Interior));
                ScalarField boundFlux = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", kvp.Value.Interior));
                boundaryFluxes.Add(kvp.Key, boundFlux);
                ScalarField boundConc = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", kvp.Value.Interior));
                boundaryConcs.Add(kvp.Key, boundConc);
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
        /// moved some initalization from ... to here - axin
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        public void Initialize(string type, double[] parameters)
        {

            this.Conc.Initialize(type, parameters);
            //for boundaryConc - only one bounary exist for cell and only cell boundary are saved
            if (type == "explicit" && parameters.Length > concentration.M.ArraySize)
            {
                //reset boundary conc and flux, only for cell and only one boundary per molpop
                int src_index = Conc.M.ArraySize;                
                int arr_len = BoundaryConcs.First().Value.M.ArraySize;
                double[] newvals = new double[arr_len];
                Array.Copy(parameters, src_index, newvals, 0, arr_len);
                this.boundaryConcs.First().Value.Initialize(type, newvals);
                src_index += arr_len;
                Array.Copy(parameters, src_index, newvals, 0, arr_len);
                BoundaryFluxes.First().Value.Initialize(type, newvals);
            }
        }



        /// <summary>
        /// At each array point in the embedded manifold, update the values of the concentration 
        /// and global gradient of the embedding manifold MolecularPopulation.
        /// </summary>
        public void UpdateBoundary()
        {
            //take the bulk at each boundary's location and write it into the boundary space, converting to the boundary scalar field type if needed
            //translation is already the position of the boundary in the containing frame; no coordinate conversion needed
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
            // Update boundary concentrations and global gradients
            UpdateBoundary();

            concentration += dt * Molecule.DiffusionCoefficient * concentration.Laplacian();

            // Apply boundary fluxes 
            foreach (KeyValuePair<int, ScalarField> kvp in boundaryFluxes.ToList())
            {
                concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.BoundaryTransforms[kvp.Key]);
                //kvp.Value.reset(); //reset to 0
            }

            // Apply Neumann natural boundary conditions
            foreach (KeyValuePair<int, ScalarField> kvp in NaturalBoundaryFluxes)
            {
                if (compartment.NaturalBoundaryTransforms[kvp.Key].Neumann)
                {
                    concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.NaturalBoundaryTransforms[kvp.Key]) / Molecule.DiffusionCoefficient;
                }
            }

            // Apply Dirichlet natural boundary conditions
            foreach (KeyValuePair<int, ScalarField> kvp in NaturalBoundaryConcs)
            {
                if (compartment.NaturalBoundaryTransforms[kvp.Key].Dirichlet)
                {
                    concentration = concentration.DirichletBC(kvp.Value, compartment.NaturalBoundaryTransforms[kvp.Key]);
                }
            }
        }
    }

}
