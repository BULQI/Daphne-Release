using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    public struct Molecule
    {
        public string Name;
        public double MolecularWeight;
        public double EffectiveRadius;
        public double DiffusionCoefficient;
        private static double boltzmannConstant = 0;

        public void ComputeDiffusionCoefficient(double viscosity, double temperature)
        {
            DiffusionCoefficient = boltzmannConstant * temperature / (6 *  Math.PI * viscosity * EffectiveRadius);
        }
    }

    public class MolecularPopulation
    {
        // name given to molecular population. May not be necessary.
        public string Name;
        // the individuals that make up this MolecularPopulation
        public Molecule Molecule;
        public DiscretizedManifold Man;

        public ScalarField Conc;

        // it seems necessary to make these fluxes and boundary concentrations dictionaries
        // so that the fluxes and boundary concentrations can be identified as sharing a manifold
        public Dictionary<Manifold,ScalarField> Fluxes;
        public Dictionary<Manifold,ScalarField> BoundaryConcs;

        public MolecularPopulation(Molecule mol, DiscretizedManifold man)
        {
            Molecule = mol;
            Man = man;
            Conc = new ScalarField(man);
            foreach (DiscretizedManifold m in Man.Boundaries.Keys)
            {
                Fluxes.Add(m, new ScalarField(m));
                BoundaryConcs.Add(m, new ScalarField(m));
            }
        }

        public void Initialize(ScalarField initialConcentration)
        {
            Conc = initialConcentration;
            // NB! also need to initialize bondary concentrations and fluxes
        }

        public double Concentration(double[] point)
        {
            LocalMatrix[] lm = Man.Interpolation(point);
            double concentration = 0;
            for (int i = 0; i < lm.Length; i++)
            {
                concentration += lm[i].Coefficient * Conc.array[lm[i].Index];
            }
            return concentration;
        }

        public double[] Gradient(double[] point)
        {
            // compute gradient by interpolation, return it
            return new double[Man.Dim];
        }

        /// <summary>
        /// The evolution native to a molecular population is diffusion.
        /// The step method evolves the diffusion through dt time units.
        /// </summary>
        /// <param name="dt">The time interval over which to integrate the diffusion equation.</param>
        public void Step(double dt)
        {
            // this is a simplified version. The real one uses the boundary fluxes in the computation.
            double[] temparray = new double[Man.ArraySize];
            for (int i = 0; i < Man.ArraySize; i++)
            {
                for (int j = 0; j < Man.Laplacian[i].Length; j++)
                {
                    temparray[i] += Man.Laplacian[i][j].Coefficient * Conc.array[Man.Laplacian[i][j].Index] * dt;
                }
            }
            Conc.array = temparray;

            foreach (Manifold m in Man.Boundaries.Keys)
            {
                for (int i = 0; i < BoundaryConcs[m].array.Length; i++)
                {
                    BoundaryConcs[m].array[i] = Conc.array[Man.Boundaries[m].WhereIs(i)];
                }
            }

        }
    }

}
