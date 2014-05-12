using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace Daphne
{
    public abstract class Reaction
    {
        public double RateConstant;
        protected ScalarField intensity;
        protected VectorField gradIntensity;

        public abstract void Step(double dt);
    }

    // Fundamental reactions
    // See 'Dynamics for MolPops.pdf' for mathematical detail on the gradient update equations.

    public class Annihilation : Reaction
    {
        MolecularPopulation reactant;

        public Annihilation(MolecularPopulation _reactant, double _RateConst)
        {
            reactant = _reactant;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * reactant.Conc;
            reactant.Conc -= intensity;

            gradIntensity = (dt * RateConstant) * reactant.GlobalGrad;
            reactant.GlobalGrad -= gradIntensity;
      }
    }

    public class Association : Reaction
    {
        MolecularPopulation reactant1;
        MolecularPopulation reactant2;
        MolecularPopulation product;

        public Association(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
        {
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant) * dt * reactant1.Conc * reactant2.Conc;
            reactant1.Conc -= intensity;
            reactant2.Conc -= intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * (reactant1.Conc * reactant1.GlobalGrad + reactant1.Conc * reactant1.GlobalGrad);
            reactant1.GlobalGrad -= gradIntensity;
            reactant2.GlobalGrad -= gradIntensity;
            product.GlobalGrad += gradIntensity;
        }
    }

    public class Dimerization : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product;

        public Dimerization(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc * reactant.Conc;
            reactant.Conc -= 2*intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * 2 * reactant.Conc * reactant.GlobalGrad;
            reactant.GlobalGrad -= 2 * gradIntensity;
            product.GlobalGrad += gradIntensity;
        }
    }

    public class DimerDissociation : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product;

        public DimerDissociation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= intensity;
            product.Conc += 2*intensity;

            gradIntensity = RateConstant * dt * reactant.GlobalGrad;
            reactant.GlobalGrad -= gradIntensity;
            product.GlobalGrad += 2 * gradIntensity;
        }
    }
   
    public class Dissociation : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product1, product2;

        public Dissociation(MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
        {
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= intensity;
            product1.Conc += intensity;
            product2.Conc += intensity;

            gradIntensity = RateConstant * dt * reactant.GlobalGrad;
            reactant.GlobalGrad -= gradIntensity;
            product1.GlobalGrad += gradIntensity;
            product2.GlobalGrad += gradIntensity;
        }
    }

    public class Transformation : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product;

        public Transformation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;

        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * reactant.GlobalGrad;
            reactant.GlobalGrad -= gradIntensity;
            product.GlobalGrad += gradIntensity;
        }
    }
    
    // Catalyzed reactions

    public class AutocatalyticTransformation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation reactant;

        public AutocatalyticTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant1, double _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant1;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant) * dt * catalyst.Conc * reactant.Conc;
            catalyst.Conc += intensity;
            reactant.Conc -= intensity;

            gradIntensity = RateConstant * dt * (catalyst.Conc* reactant.GlobalGrad + reactant.Conc * catalyst.GlobalGrad);
            reactant.GlobalGrad -= gradIntensity;
            catalyst.GlobalGrad += gradIntensity;
        }
    }

    public class CatalyzedAnnihilation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation reactant;

        public CatalyzedAnnihilation(MolecularPopulation _catalyst, MolecularPopulation _reactant, double _RateConst)
        {
            reactant = _reactant;
            catalyst = _catalyst;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc * reactant.Conc;
            reactant.Conc -= intensity;

            gradIntensity = (dt * RateConstant) * ( reactant.Conc * catalyst.GlobalGrad + catalyst.Conc * reactant.GlobalGrad );
            reactant.GlobalGrad -= gradIntensity;
        }
    }

    public class CatalyzedAssociation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation reactant1;
        MolecularPopulation reactant2;
        MolecularPopulation product;

        public CatalyzedAssociation(MolecularPopulation _catalyst, MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
        {
            catalyst = _catalyst;
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant) * dt * catalyst.Conc * reactant1.Conc * reactant2.Conc;
            reactant1.Conc -= intensity;
            reactant2.Conc -= intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * (catalyst.Conc * reactant1.Conc * reactant2.GlobalGrad 
                                + catalyst.Conc * reactant2.Conc * reactant1.GlobalGrad 
                                + reactant1.Conc * reactant2.Conc * catalyst.GlobalGrad );
            reactant1.GlobalGrad -= gradIntensity;
            reactant2.GlobalGrad -= gradIntensity;
            product.GlobalGrad += gradIntensity;
        }
    }

    public class CatalyzedCreation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation product;

        public CatalyzedCreation(MolecularPopulation _catalyst, MolecularPopulation _product, double _RateConst)
        {
            if (_catalyst.Man != _product.Man)
            {
                throw new Exception("Manifold mismatch");
            }

            catalyst = _catalyst;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * catalyst.GlobalGrad;
            product.GlobalGrad += gradIntensity;
        }
    }

    public class CatalyzedDimerization : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product;
        MolecularPopulation catalyst;

        public CatalyzedDimerization(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc * catalyst.Conc;
            reactant.Conc -= 2 * intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * ( 2 * catalyst.Conc * reactant.Conc * reactant.GlobalGrad
                                + reactant.Conc * reactant.Conc * catalyst.GlobalGrad) ;
            reactant.GlobalGrad -= 2 * gradIntensity;
            product.GlobalGrad += gradIntensity;

        }
    }

    public class CatalyzedDimerDissociation : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product;
        MolecularPopulation catalyst;

        public CatalyzedDimerDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc * catalyst.Conc;
            reactant.Conc -= intensity;
            product.Conc += 2 * intensity;

            gradIntensity = RateConstant * dt * (catalyst.Conc * reactant.GlobalGrad + reactant.Conc * catalyst.GlobalGrad);
            reactant.GlobalGrad -= gradIntensity;
            product.GlobalGrad += 2 * gradIntensity;

        }
    }
    
    public class CatalyzedDissociation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation reactant;
        MolecularPopulation product1, product2;

        public CatalyzedDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * catalyst.Conc * reactant.Conc;
            reactant.Conc -= intensity;
            product1.Conc += intensity;
            product2.Conc += intensity;

            gradIntensity = RateConstant * dt * (catalyst.Conc * reactant.GlobalGrad + reactant.Conc * catalyst.GlobalGrad);
            reactant.GlobalGrad -= gradIntensity;
            product1.GlobalGrad += gradIntensity;
            product2.GlobalGrad += gradIntensity;
        }
    }

    public class CatalyzedTransformation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation reactant;
        MolecularPopulation product;

        public CatalyzedTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc * reactant.Conc;
            reactant.Conc -= intensity;
            product.Conc += intensity;

            gradIntensity = RateConstant * dt * (catalyst.Conc * reactant.GlobalGrad + reactant.Conc * catalyst.GlobalGrad);
            reactant.GlobalGrad -= gradIntensity;
            product.GlobalGrad += gradIntensity;
        }
    }

    /// <summary>
    /// Generalized reaction.
    /// The array int[,] stoich stores the reactant and product stochiometric coefficients for each MolecularPopulation 
    /// MolecularPopulations can be both products and reactants
    /// MolecularPopulation concentrations are incremented proportional to reaction intensity and the difference between 
    /// the product and reactant stoichiometries.
    /// Example 1:  A + 2B + E -> C + E
    ///     genReac = 
    ///         A, {1,0}
    ///         B, {2,0}
    ///         C, {1,0}
    ///         E, {1,1}
    ///     intensity = RateConstant*dt*A*B^2*E
    ///     A += (0-1)*intensity
    ///     B += (0-2)*intensity
    ///     C += (1-0)*intensity
    ///     E += (1-1)*intensity
    ///     For mathematical detail, see 'Dynamics for MolPops.pdf'.
    /// </summary>
    public class GeneralizedReaction : Reaction
    {
        // int[] = [reaction, product] stoichiometric coefficients for each MolecularPopulation
        Dictionary<MolecularPopulation, int[]> genReac;

        public GeneralizedReaction(Dictionary<MolecularPopulation, int[]> _genReac, double _RateConst)
        {
            genReac = new Dictionary<MolecularPopulation, int[]>();
            genReac = _genReac;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            // Catalysts have equal reaction and product stoichiometries
            // stoich[.,0] = stoich[.,1]

            // Calculate the product of the powers of all reactant concentrations.
            // power = reactant stoichiometry
            // MolecularPopulations that do not participate as reactants will not 
            // contribute due to kvp.Value[0]=0.
            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
                for (int j = 0; j < kvp.Value[0]; j++)
                {
                    if (j == 0)
                    {
                        intensity = kvp.Key.Conc;
                    }
                    else
                    {
                        intensity *= kvp.Key.Conc;
                    }
                }
            }
            intensity = (dt * RateConstant) * intensity;

            // Update the concentration according to the difference between the 
            // product and reactant stoichiometries.
            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
                kvp.Key.Conc += (kvp.Value[1] - kvp.Value[0]) * intensity;
            }

            // Update the gradients

            List<MolecularPopulation> mp = new List<MolecularPopulation>(genReac.Keys);
            VectorField gradIntensity = new VectorField(mp[0].Man);

            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
               if (kvp.Value[0] != 0)
                {
                    gradIntensity += kvp.Value[0]*kvp.Key.GlobalGrad / kvp.Key.Conc;
                }
            }

            gradIntensity = intensity * gradIntensity;

            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
                kvp.Key.GlobalGrad += (kvp.Value[1] - kvp.Value[0]) * gradIntensity;
            }
        }
    }

    // Boundary reactions

    /// <summary>
    /// Appropriate for boundary manifolds that are not zero-dimensional.
    /// For boundary association on TinySphere, use TinyBoundaryAssociation
    /// </summary>
    public class BoundaryAssociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        DiscretizedManifold boundary;
        double fluxIntensityConstant;

        public BoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;

            if (ligand.BoundaryConcs[boundary.Id].M != receptor.Man)
            {
                throw (new Exception("Receptor and ligand boundary concentration manifolds are unequal."));

                if (receptor.Man != complex.Man)
                {
                    throw (new Exception("Receptor and complex manifolds are unequal."));
                }
            }
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * receptor.Conc * ligand.BoundaryConcs[boundary.Id];

            ligand.Fluxes[boundary.Id] += fluxIntensityConstant * intensity;
            receptor.Conc -= intensity;
            complex.Conc += intensity;

            gradIntensity = (RateConstant * dt) * ( ligand.BoundaryConcs[boundary.Id] * receptor.GlobalGrad 
                + receptor.Conc * ligand.BoundaryGlobalGrad[boundary.Id]); 
            receptor.GlobalGrad -= gradIntensity;
            complex.GlobalGrad += gradIntensity;

        }
    }

    public class BoundaryDissociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        DiscretizedManifold boundary;
        double fluxIntensityConstant;

        public BoundaryDissociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * complex.Conc;

            ligand.Fluxes[boundary.Id] -= fluxIntensityConstant * intensity;
            receptor.Conc += intensity;
            complex.Conc -= intensity;

            gradIntensity = (RateConstant * dt) * complex.GlobalGrad;
            receptor.GlobalGrad += gradIntensity;
            complex.GlobalGrad -= gradIntensity;
        }
    }

    // Transport

    
    // The following reactions are utilized in the implementation of driver-driven locomotion 
    // for the case that TinySphere and TinyBall comprise the cell manifolds

    public class TinyBoundaryAssociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        DiscretizedManifold boundary;
        double fluxIntensityConstant;
        private double cellRadius;

        public TinyBoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            // NOTE: Not sure why this doesn't work. At runtime, seems to think that _receptor.Man is empty.
            //if (_ligand.BoundaryConcs[boundary].M != _receptor.Man)
            //{
            //    throw (new Exception("Receptor and ligand boundary concentration manifolds are unequal."));

            //    if (_receptor.Man != _complex.Man)
            //    {
            //        throw (new Exception("Receptor and complex manifolds are unequal."));
            //    }
            //}

            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;
            cellRadius = complex.Man.Extents[0];

            if (ligand.BoundaryConcs[boundary.Id].M != receptor.Man)
            {
                throw (new Exception("Receptor and ligand boundary concentration manifolds are unequal."));

                if (receptor.Man != complex.Man)
                {
                    throw (new Exception("Receptor and complex manifolds are unequal."));
                }
            }

        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * receptor.Conc * ligand.BoundaryConcs[boundary.Id];

            ligand.Fluxes[boundary.Id] += fluxIntensityConstant * intensity;
            receptor.Conc -= intensity;
            complex.Conc += intensity;

            gradIntensity = (RateConstant * dt) * (ligand.BoundaryConcs[boundary.Id] * receptor.GlobalGrad
                + cellRadius * receptor.Conc * ligand.BoundaryGlobalGrad[boundary.Id]); 
            receptor.GlobalGrad -= gradIntensity;
            complex.GlobalGrad += gradIntensity;

        }
    }

    /// <summary>
    /// The evolution equations for driver activation by complex under the assumption
    /// that driver and receptor molecules are conserved.
    /// See 'driver molecules dynamics.docx' and 'Dynamics for MolPops.pdf'
    /// </summary>
    public class CatalyzedConservedBoundaryActivation : Reaction
    {
        MolecularPopulation driver;
        MolecularPopulation complex;
        // ScalarField driverTotal;
        private double cellRadius;

        double driverTotal;
        private double driverConc, complexConc;
        private Vector driverGlobalGrad, complexGlobalGrad;

        public CatalyzedConservedBoundaryActivation(MolecularPopulation _driver, MolecularPopulation _complex, double _RateConst, double _driverTotal)
        {
            driver = _driver;
            complex = _complex;
            RateConstant = _RateConst;
            cellRadius = complex.Man.Extents[0];
            // driverTotal = new ScalarField(_driver.Man, _driverTotal);
            driverTotal = _driverTotal;
        }

        public override void  Step(double dt)
        {
            // NOTE: We cannot implement using ScalarField or VectorField objects, because driver and complex are on different manifolds.
            
            // For manifolds that are not zero-dimensional, we would use the boundary concentrations of the driver molecules.
            //      driver.Conc -> driver.BoundaryConcs[driver.Man]
            //      driver.GlobalGrad -> driver.BoundaryGlobalGrad[driver.Man]
            // Instead of updating driver.Conc, we would update a flux driver.Fluxes[boundary] then update driver.Conc during diffusion.
            // 
            //driver.Conc += dt * (RateConstant / cellRadius) * (driverTotal - driver.Conc) * complex.Conc;
            //driver.GlobalGrad += dt * (3 * RateConstant / (cellRadius * cellRadius)) *
                                     //((driverTotal - driver.Conc) * complex.GlobalGrad - cellRadius * complex.Conc.array[0] * driver.GlobalGrad);

            driverConc = driver.Conc.array[0];
            complexConc = complex.Conc.array[0];
            driverGlobalGrad = driver.GlobalGrad[0];
            complexGlobalGrad = complex.GlobalGrad[0];

            driverConc += dt * (RateConstant / cellRadius) * (driverTotal - driverConc) * complexConc ;
            driverGlobalGrad += dt * (3 * RateConstant / (cellRadius * cellRadius)) *
                                     ( (driverTotal - driverConc) * complexGlobalGrad - cellRadius* complexConc * driverGlobalGrad);

            driver.Conc.array[0] = driverConc;
            driver.GlobalGrad[0] = driverGlobalGrad;
        }

    }

    public class BoundaryConservedDeactivation : Reaction
    {
        MolecularPopulation driver;
        MolecularPopulation complex;

        public BoundaryConservedDeactivation(MolecularPopulation _driver, MolecularPopulation _complex, double _RateConst)
        {
            driver = _driver;
            complex = _complex;
            RateConstant = _RateConst;
        }

        public override void Step(double dt)
        {
            driver.Conc += -dt * RateConstant * driver.Conc;
            driver.GlobalGrad += -dt * RateConstant * driver.GlobalGrad;
        }

    }

    /// <summary>
    /// Evolution of driver gradient due to diffusion
    /// Implemented as a reaction for TinyBall
    /// </summary>
    public class DriverDiffusion : Reaction
    {
        MolecularPopulation driver;
        private double cellRadius;

        public DriverDiffusion(MolecularPopulation _driver)
        {
            driver = _driver;
            cellRadius = driver.Man.Extents[0];
        }

        public override void Step(double dt)
        {
            driver.GlobalGrad += - (dt * 3 * driver.Molecule.DiffusionCoefficient / (cellRadius * cellRadius)) * driver.GlobalGrad;
        }

    }


}


