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

        public Molecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
        }

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

            // This isn't working yet - gmk 10/2/2012
            //foreach (DiscretizedManifold m in Man.Boundaries.Keys)
            //{
            //    Fluxes.Add(m, new ScalarField(m));
            //    BoundaryConcs.Add(m, new ScalarField(m));
            //}
        }

        public MolecularPopulation(string name, Molecule mol, DiscretizedManifold man, ScalarField initConc)
        {
            Name = name;
            Molecule = mol;
            Man = man;
            Conc = new ScalarField(man);

            if (man.Boundaries != null)
            {
                Fluxes = new Dictionary<Manifold, ScalarField>();
                BoundaryConcs = new Dictionary<Manifold, ScalarField>();

                foreach (DiscretizedManifold m in Man.Boundaries.Keys)
                {
                    Fluxes.Add(m, new ScalarField(m));
                    BoundaryConcs.Add(m, new ScalarField(m));

                    // Initializing BoundaryConc
                    for (int i = 0; i < BoundaryConcs[m].array.Length; i++)
                    {
                        BoundaryConcs[m].array[i] = Conc.array[Man.Boundaries[m].WhereIs(i)];
                    }
                }
            }

            Initialize(initConc);

        }

        public void Initialize(ScalarField initialConcentration)
        {
            Conc = initialConcentration;

            // Initializing BoundaryConcs doesn't seem to fit here
            //foreach (Manifold m in Man.Boundaries.Keys)
            //{
            //    BoundaryConcs[m] = initialConcentration;
            //}
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

            // TODO: This if statement is here because TinySphere and TinyBall have Boundaries = null
            // This requires a similar if statement in the contructor
            //     MolecularPopulation(string name, Molecule mol, DiscretizedManifold man, ScalarField initConc),
            // so BoundaryConcs are null for those manifolds
            // Is this correct?
            if (Man.Boundaries != null)
            {
                foreach (Manifold m in Man.Boundaries.Keys)
                {
                    for (int k = 0; k < BoundaryConcs[m].array.Length; k++)
                    {
                        BoundaryConcs[m].array[k] = Conc.array[Man.Boundaries[m].WhereIs(k)];
                    }
                }
            }

        }
    }

}
