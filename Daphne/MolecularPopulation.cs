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
            // take the bulk at each boundary's location and write it into the boundary space, converting to the boundary scalar field type if needed
            // translation is already the position of the boundary in the containing frame; no coordinate conversion needed
            foreach(KeyValuePair<int, ScalarField> kvp in boundaryConcs)
            {
                kvp.Value.Convert(concentration, compartment.BoundaryTransforms[kvp.Key].Translation);
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

            if (IsDiffusing == true)
            {
                concentration += dt * Molecule.DiffusionCoefficient * concentration.Laplacian();
                // boundary fluxes
                foreach (ScalarField boundaryFlux in boundaryFluxes.Values)
                {
                    concentration += -dt * concentration.DiffusionFluxTerm(boundaryFlux);
                }

                //this.Conc.WriteToFile("CXCL13 final.txt");

                // TODO: Implement diffusion of gradients

                // The code below is intended to impose the flux boundary conditions

                // The surface area of the flux

                /* NOTE: needs flux distribution
                double cellSurfaceArea;
                double voxelVolume = manifold.VoxelVolume();
                int n = 0;

                foreach (KeyValuePair<int, ScalarField> kvp in boundaryFluxes)
                {
                    if (manifold.GetType() == typeof(InterpolatedRectangularPrism) && kvp.Value.M.GetType() == typeof(TinySphere))
                    {
                        if (voxelVolume == 0)
                        {
                            throw new Exception("Molecular population Step division by zero.");
                        }

                        cellSurfaceArea = kvp.Value.M.Area();

                        // Returns an interpolation stencil for the 8 grid points surrounding the cell position
                        lm = manifold.Interpolation(manifold.Boundaries[kvp.Key].WhereIs(n));
                        // Distribute the flux to or from the cell surface to the surrounding nodes
                        for (int k = 0; k < lm.Length; k++)
                        {
                            concentration[lm[k].Index] += lm[k].Coefficient * kvp.Value[n] * cellSurfaceArea / voxelVolume;
                        }
                    }
                    else if (manifold.Boundaries[kvp.Key].NeedsInterpolation())
                    {
                        // TODO: implement in general case
                    }
                    else
                    {
                        // TODO: implement in general case
                    }
                }*/
            }
        }
    }

}
