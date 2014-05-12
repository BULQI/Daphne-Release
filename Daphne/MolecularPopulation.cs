using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

namespace Daphne
{
    public class Molecule
    {
        public string Name;
        public double MolecularWeight;
        public double EffectiveRadius;
        public double DiffusionCoefficient;
        private static double boltzmannConstant = 0;

        public Molecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
        }

        public void ComputeDiffusionCoefficient(double viscosity, double temperature)
        {
            DiffusionCoefficient = boltzmannConstant * temperature / (6 * Math.PI * viscosity * EffectiveRadius);
        }
    }



    public class MolecularPopulation
    {
        // the individuals that make up this MolecularPopulation
        public Molecule Molecule { get; private set; }
        private Compartment compartment;
        private readonly Manifold manifold;
        private ScalarField concentration;
        private readonly Dictionary<int, ScalarField> boundaryFluxes,
                                                      boundaryConcs,
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
        public MolecularPopulation()
        {
        }

        public MolecularPopulation(Molecule mol, ScalarField init, Compartment comp)
        {
            Molecule = mol;
            manifold = init.M;
            concentration = init;
            compartment = comp;

            // true boundaries
            boundaryFluxes = new Dictionary<int, ScalarField>();
            boundaryConcs = new Dictionary<int, ScalarField>();
            foreach (KeyValuePair<int, Compartment> kvp in compartment.Boundaries)
            {
                boundaryFluxes.Add(kvp.Key, new ScalarField(kvp.Value.Interior));
                boundaryConcs.Add(kvp.Key, new ScalarField(kvp.Value.Interior));
            }

            // natural boundaries
            naturalBoundaryFluxes = new Dictionary<int, ScalarField>();
            naturalBoundaryConcs = new Dictionary<int, ScalarField>();
            foreach (KeyValuePair<int, Manifold> kvp in compartment.NaturalBoundaries)
            {
                naturalBoundaryFluxes.Add(kvp.Key, new ScalarField(kvp.Value));
                naturalBoundaryConcs.Add(kvp.Key, new ScalarField(kvp.Value));
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
            // boundary fluxes
            foreach (KeyValuePair<int, ScalarField> kvp in boundaryFluxes)
            {
                //concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.BoundaryTransforms[kvp.Key].Translation);
                concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.BoundaryTransforms[kvp.Key]);
            }

            foreach (KeyValuePair<int, ScalarField> kvp in NaturalBoundaryFluxes)
            {
                // NOTE: This loop is computationally expensive for molecules in the ECM, especially when most faces have zero flux.
                if (compartment.NaturalBoundaryTransforms[kvp.Key].IsFluxing)
                {
                    // concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.NaturalBoundaryTransforms[kvp.Key].Translation);
                    concentration += -dt * concentration.DiffusionFluxTerm(kvp.Value, compartment.NaturalBoundaryTransforms[kvp.Key]) / Molecule.DiffusionCoefficient;
                }
            }
        }
    }

}
