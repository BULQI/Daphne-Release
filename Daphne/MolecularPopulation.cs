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
        // the individuals that make up this MolecularPopulation
        public Molecule Molecule;
        public DiscretizedManifold Man;

        public ScalarField Conc;

        // it seems necessary to make these fluxes and boundary concentrations dictionaries
        // so that the fluxes and boundary concentrations can be identified as sharing a manifold
        // These keep track of the fluxes and concentrations at the boundaries of the manifold 
        // that contains the molecular population
        public Dictionary<DiscretizedManifold,ScalarField> Fluxes;
        public Dictionary<DiscretizedManifold,ScalarField> BoundaryConcs;

        public MolecularPopulation(Molecule mol, DiscretizedManifold man)
        {
            Molecule = mol;
            Man = man;
            Conc = new ScalarField(man);

            if (Man.Boundaries != null)
            {
                Fluxes = new Dictionary<DiscretizedManifold, ScalarField>();
                BoundaryConcs = new Dictionary<DiscretizedManifold, ScalarField>();

                foreach (DiscretizedManifold m in Man.Boundaries.Keys)
                {
                    Fluxes.Add(m, new ScalarField(m));
                    BoundaryConcs.Add(m, new ScalarField(m));
                }
            }
        }

        public MolecularPopulation(Molecule mol, DiscretizedManifold man, ScalarField initConc)
        {
            Molecule = mol;
            Man = man;
            Conc = new ScalarField(man);

            if (Man.Boundaries != null)
            {
                Fluxes = new Dictionary<DiscretizedManifold, ScalarField>();
                BoundaryConcs = new Dictionary<DiscretizedManifold, ScalarField>();

                foreach (DiscretizedManifold m in Man.Boundaries.Keys)
                {
                    Fluxes.Add(m, new ScalarField(m));
                    BoundaryConcs.Add(m, new ScalarField(m));
                }
            }

            Initialize(initConc);

        }

        public void Initialize(ScalarField initialConcentration)
        {
            Conc = initialConcentration;
        }

        /// <summary>
        /// Calculate the value of the concentration in the embedding manifold at each array point in the embedded manifold
        /// </summary>
        public void UpdateBoundaryConcs()
        {
            if (Man.Boundaries != null)
            {
                foreach (DiscretizedManifold m in Man.Boundaries.Keys)
                {
                    // Cases:
                    //
                    // PlasmaMembrane    -> Cytosol: no spatial, arraySize = 1
                    //
                    // PlasmaMembrane    -> Extracellular fluid: depends on position of cell in extracellular medium, 
                    //                      arraySize = 1, one-to-one correspondence
                    //
                    // BoundedRectangles -> BoundedRectangularPrism
                    //                      arraySize > 1, one-to-one correspondence
                    // 
                    for (int k = 0; k < BoundaryConcs[m].array.Length; k++)
                    {
                        // NOTE: This corresponds to the case where there is a one-to-one
                        // correspondance between the array points of the embedding and embedded manifolds

                        // Feed embedded manifold array index k to WhereIs and return a double[] point in the embedding manifold
                        // request interpolation if needed
                        if (Man.Boundaries[m].NeedsInterpolation() == true)
                        {
                            BoundaryConcs[m].array[k] = Concentration(Man.Boundaries[m].WhereIs(k));
                        }
                        else
                        {
                            BoundaryConcs[m].array[k] = Concentration(Man.Boundaries[m].WhereIsIndex(k));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Interpolates the value of the concentration of the molecular population at point
        /// </summary>
        /// <param name="point">A point in the manifold containing the molecular population</param>
        /// <returns></returns>
        public double Concentration(double[] point)
        {
            LocalMatrix[] lm = Man.Interpolation(point);
            double concentration = 0;

            if (lm != null)
            {
                for (int i = 0; i < lm.Length; i++)
                {
                    concentration += lm[i].Coefficient * Conc.array[lm[i].Index];
                }
            }
            return concentration;
        }

        public double Concentration(int idx)
        {
            if (idx < 0 || idx >= Conc.array.Length)
            {
                return 0;
            }
            return Conc.array[idx];
        }

        // TODO: Implement gradient
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
            UpdateBoundaryConcs();

            // Note: this IF statement prevents diffusion for TinyBall or TinySphere
            // We may need a better way to indicate when diffusion should take place
            if (Man.ArraySize > 1)
            {
                // this is a simplified version. The real one uses the boundary fluxes in the computation.
                double[] temparray = new double[Man.ArraySize];
                for (int i = 0; i < Man.ArraySize; i++)
                {
                    for (int j = 0; j < Man.Laplacian[i].Length; j++)
                    {
                        temparray[i] += Man.Laplacian[i][j].Coefficient * Conc.array[Man.Laplacian[i][j].Index] * dt;

                        // TODO - change the grid size, time step, or use implicit method to eliminate this next statement
                        if (temparray[i] < 0)
                        {
                            temparray[i] = 0;
                        }
                    }
                }
                Conc.array = temparray;
            }
        }
    }

}
