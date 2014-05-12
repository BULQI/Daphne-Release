using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

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
        public Molecule Molecule;
        public Manifold Man;

        public ScalarField Conc;

        // it seems necessary to make these fluxes and boundary concentrations dictionaries
        // so that the fluxes and boundary concentrations can be identified as sharing a manifold
        // These keep track of the fluxes and concentrations at the boundaries of the manifold 
        // that contains the molecular population
        public Dictionary<int, ScalarField> Fluxes;
        public Dictionary<int, ScalarField> BoundaryConcs;

        // Gradient relative to the 3D extracellular environment (global)
        public VectorField GlobalGrad;

        // Similar to BoundaryConcs, but for global gradients of boundary concentrations
        // Used to update the receptor gradient in the BoundaryAssociation class
        public Dictionary<int, VectorField> BoundaryGlobalGrad;
        public int globalGradDim = 3;

        // Switch that allows us to turn off diffusion.
        // Diffusion is on, by default.
        public bool IsDiffusing = true;

        // NOTE: Put this here so json deserialization would work. gmk
        public MolecularPopulation()
        {
        }

        public MolecularPopulation(Molecule mol, Manifold man)
        {
            Molecule = mol;
            Man = man;
            Conc = new DiscreteScalarField(man);
            GlobalGrad = new VectorField(man, globalGradDim);

            if (Man.Boundaries != null)
            {
                Fluxes = new Dictionary<int, ScalarField>();
                BoundaryConcs = new Dictionary<int, ScalarField>();
                BoundaryGlobalGrad = new Dictionary<int, VectorField>();

                foreach (Embedding e in Man.Boundaries.Values)
                {
                    Fluxes.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryConcs.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryGlobalGrad.Add(e.Domain.Id, new VectorField(e.Domain, globalGradDim));
                }
            }
        }

        public MolecularPopulation(Molecule mol, Manifold man, ScalarField initConc)
        {
            Molecule = mol;
            Man = man;
            Conc = new DiscreteScalarField(man);
            GlobalGrad = new VectorField(man, globalGradDim);

            if (Man.Boundaries != null)
            {
                Fluxes = new Dictionary<int, ScalarField>();
                BoundaryConcs = new Dictionary<int, ScalarField>();
                BoundaryGlobalGrad = new Dictionary<int, VectorField>();

                foreach (Embedding e in Man.Boundaries.Values)
                {
                    Fluxes.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryConcs.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryGlobalGrad.Add(e.Domain.Id, new VectorField(e.Domain, globalGradDim));
                }
            }

            Initialize(initConc);

            // The global gradient is the same as the local gradient for 3D manifolds
            // In these cases, use the local gradient to initialize the global gradient
            if (Man.Dim == globalGradDim)
            {
                for (int i = 0; i < Man.ArraySize; i++)
                {
                    GlobalGrad[i] = LocalGradient(i);
                }
            }

        }

        public MolecularPopulation(Molecule mol, Manifold man, ScalarField initConc, VectorField initGrad)
        {
            Molecule = mol;
            Man = man;
            Conc = new DiscreteScalarField(man);
            GlobalGrad = new VectorField(man, globalGradDim);

            if (Man.Boundaries != null)
            {
                Fluxes = new Dictionary<int, ScalarField>();
                BoundaryConcs = new Dictionary<int, ScalarField>();
                BoundaryGlobalGrad = new Dictionary<int, VectorField>();

                foreach (Embedding e in Man.Boundaries.Values)
                {
                    Fluxes.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryConcs.Add(e.Domain.Id, new DiscreteScalarField(e.Domain));
                    BoundaryGlobalGrad.Add(e.Domain.Id, new VectorField(e.Domain, globalGradDim));
                }
            }

            Initialize(initConc);
            GradInitialize(initGrad);

        }

        public void Initialize(ScalarField initialConcentration)
        {
            Conc = initialConcentration;
        }

        public void GradInitialize(VectorField initGrad)
        {
            GlobalGrad = initGrad;
        }

        ///// <summary>
        ///// Calculate the value of the concentration in the embedding manifold at each array point in the embedded manifold
        ///// </summary>
        //public void UpdateBoundaryConcs()
        //{
        //    if (Man.Boundaries != null)
        //    {
        //        foreach (DiscretizedManifold m in Man.Boundaries.Keys)
        //        {
        //            // Cases:
        //            //
        //            // PlasmaMembrane    -> Cytosol: no spatial, arraySize = 1
        //            //
        //            // PlasmaMembrane    -> Extracellular fluid: depends on position of cell in extracellular medium, 
        //            //                      arraySize = 1, one-to-one correspondence
        //            //
        //            // BoundedRectangles -> BoundedRectangularPrism
        //            //                      arraySize > 1, one-to-one correspondence
        //            // 
        //            // NOTE: This corresponds to the case where there is a one-to-one
        //            // correspondance between the array points of the embedding and embedded manifolds

        //            // Feed embedded manifold array index k to WhereIs and return a double[] point in the embedding manifold
        //            // request interpolation if needed
        //            if (Man.Boundaries[m].NeedsInterpolation() == true)
        //            {
        //                for (int k = 0; k < BoundaryConcs[m].array.Length; k++)
        //                {
        //                    BoundaryConcs[m].array[k] = Concentration(Man.Boundaries[m].WhereIs(k));
        //                }
        //            }
        //            else
        //            {
        //                for (int k = 0; k < BoundaryConcs[m].array.Length; k++)
        //                {
        //                    BoundaryConcs[m].array[k] = Concentration(Man.Boundaries[m].WhereIsIndex(k));
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// At each array point in the embedded manifold, update the values of the concentration 
        /// and global gradient of the embedding manifold MolecularPopulation.
        /// </summary>
        public void UpdateBoundary()
        {
            if (Man.Boundaries != null)
            {
                foreach (Embedding e in Man.Boundaries.Values)
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
                    // NOTE: This corresponds to the case where there is a one-to-one
                    // correspondance between the array points of the embedding and embedded manifolds

                    // Feed embedded manifold array index k to WhereIs and return a double[] point in the embedding manifold
                    // request interpolation if needed
                    int id = e.Domain.Id;

                    if (Man.Boundaries[id].NeedsInterpolation() == true)
                    {
                        for (int k = 0; k < BoundaryConcs[id].M.ArraySize; k++)
                        {
                            BoundaryConcs[id][k] = Concentration(Man.Boundaries[id].WhereIs(k));
                            BoundaryGlobalGrad[id][k] = GlobalGradient(Man.Boundaries[id].WhereIs(k));
                        }
                    }
                    else
                    {
                        for (int k = 0; k < BoundaryConcs[id].M.ArraySize; k++)
                        {
                            BoundaryConcs[id][k] = Concentration(Man.Boundaries[id].WhereIsIndex(k));
                            BoundaryGlobalGrad[id][k] = GlobalGradient(Man.Boundaries[id].WhereIsIndex(k));
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
            //LocalMatrix[] lm = Man.Interpolation(point);
            //double concentration = 0;

            //if (lm != null)
            //{
            //    for (int i = 0; i < lm.Length; i++)
            //    {
            //        concentration += lm[i].Coefficient * Conc.array[lm[i].Index];
            //    }
            //}
            return Conc.Get(point);
        }

        public double Concentration(int idx)
        {
            if (idx < 0 || idx >= Conc.M.ArraySize)
            {
                return 0;
            }
            return Conc[idx];
        }


        public Vector GlobalGradient(double[] point)
        {
            return GlobalGrad.Value(point);
        }

        public Vector GlobalGradient(int idx)
        {
            if (idx < 0 || idx >= GlobalGrad.M.ArraySize)
            {
                return new double[] { 0, 0, 0 };
            }
            return GlobalGrad[idx];
        }

        /// <summary>
        /// Returns the local gradient at a grid point specified by index.
        /// Gradient cannot be calculated with a stencil for TinySpehere and TinyBall.
        /// </summary>
        /// <param name="index">The index into the array</param>
        /// <returns>The gradient in each dimension</returns>
        public Vector LocalGradient(int index)
        {
            // TinySphere, TinyBall, and other 0-dimensional manifolds do not compute the gradient using dynamical equations.
            // The gradient is stored in the array.
            // ArraySize = 1 (value) + 3 (gradient values)
            //if ((Man.GetType() == typeof(TinySphere)) || (Man.GetType() == typeof(TinyBall)))
            if (Man.ArraySize == 1)
            {
                return new Vector(Man.Dim);
            }
            else
            {
                LocalMatrix[][] lm = Man.GradientOperator(index);
                Vector gradient = new Vector(Man.Dim);

                if (lm != null)
                {
                    for (int i = 0; i < Man.Dim; i++)
                    {
                        for (int j = 0; j < lm[i].Length; j++)
                        {
                            gradient[i] += lm[i][j].Coefficient * Conc[lm[i][j].Index];
                        }
                    }
                }
                return gradient;
            }
        }

        /// <summary>
        /// The evolution native to a molecular population is diffusion.
        /// The step method evolves the diffusion through dt time units.
        /// </summary>
        /// <param name="dt">The time interval over which to integrate the diffusion equation.</param>
        public void Step(double dt)
        {
            //UpdateBoundaryConcs();

            // Update boundary concentrations and global gradients
            UpdateBoundary();

            // Note: this IF statement prevents diffusion for TinyBall or TinySphere
            // We may need a better way to indicate when diffusion should take place
            if ((Man.ArraySize > 1) && (IsDiffusing == true))
            {
                ScalarField temparray = new DiscreteScalarField(Man, 0);

                for (int i = 0; i < Man.ArraySize; i++)
                {
                    for (int j = 0; j < Man.Laplacian[i].Length; j++)
                    {
                        temparray[i] += Man.Laplacian[i][j].Coefficient * Conc[Man.Laplacian[i][j].Index] * dt;
                    }
                }
                Conc += Molecule.DiffusionCoefficient * temparray;

                //this.Conc.WriteToFile("CXCL13 final.txt");

                // TODO: Implement diffusion of gradients

                // The code below is intended to impose the flux boundary conditions

                // The surface area of the flux
                double cellSurfaceArea;
                double voxelVolume = Man.Extents[0] * Man.Extents[1] * Man.Extents[2];
                int n = 0;
                LocalMatrix[] lm;

                foreach (KeyValuePair<int, ScalarField> kvp in Fluxes)
                {

                    if (kvp.Key.GetType() == typeof(TinySphere) && Man.GetType() == typeof(BoundedRectangularPrism))
                    {
                        cellSurfaceArea = 4 * Math.PI * kvp.Value.M.Extents[0] * kvp.Value.M.Extents[0];

                        // Returns an interpolation stencil for the 8 grid points surrounding the cell position
                        lm = Man.Interpolation(Man.Boundaries[kvp.Key].WhereIs(n));
                        // Distribute the flux to or from the cell surface to the surrounding nodes
                        for (int k = 0; k < lm.Length; k++)
                        {
                            Conc[lm[k].Index] += lm[k].Coefficient * kvp.Value[n] * cellSurfaceArea / voxelVolume;
                        }
                    }
                    else if (Man.Boundaries[kvp.Key].NeedsInterpolation())
                    {
                        // TODO: implement in general case
                    }
                    else
                    {
                        // TODO: implement in general case
                    }

                }
            }
        }


        public double Integrate()
        {
            double d = Man.Integrate(Conc);

            return d;
        }


    }

}
