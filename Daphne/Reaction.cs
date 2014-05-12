using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

namespace Daphne
{
    public abstract class Reaction : IDynamic
    {
        public double RateConstant;
        protected ScalarField intensity;

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
            reactant.Conc -= 2 * intensity;
            product.Conc += intensity;
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
            product.Conc += 2 * intensity;
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
        }
    }

    // Boundary reactions
    // Use the convention that positive flux reduces the concentration.
    // Flux terms are accumulating and need to be zeroed in the diffusion step.

    /// <summary>
    /// Appropriate for boundary manifolds that are not zero-dimensional.
    /// </summary>
    public class BoundaryAssociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        Manifold boundary;
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
                throw new Exception("Receptor and ligand boundary concentration manifolds are unequal.");
            }
            if (receptor.Man != complex.Man)
            {
                throw new Exception("Receptor and complex manifolds are unequal.");
            }
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * receptor.Conc * ligand.BoundaryConcs[boundary.Id];

            ligand.BoundaryFluxes[boundary.Id] += fluxIntensityConstant * intensity;
            receptor.Conc -= intensity;
            complex.Conc += intensity;
        }
    }

    public class BoundaryDissociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        Manifold boundary;
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

            ligand.BoundaryFluxes[boundary.Id] -= fluxIntensityConstant * intensity;
            receptor.Conc += intensity;
            complex.Conc -= intensity;
        }
    }

    /// <summary>
    /// Transport of material from boundary to bulk
    /// </summary>
    public class BoundaryTransportFrom : Reaction
    {
        MolecularPopulation membrane;
        MolecularPopulation bulk;
        Manifold boundary;
        double fluxIntensityConstant;

        public BoundaryTransportFrom(MolecularPopulation _membrane, MolecularPopulation _bulk, double _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            fluxIntensityConstant = 1.0 / bulk.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * bulk.BoundaryConcs[boundary.Id];

            bulk.BoundaryFluxes[boundary.Id] -= fluxIntensityConstant * intensity;
            membrane.Conc -= intensity;
        }
    }

    /// <summary>
    /// Transport of material from bulk to boundary
    /// </summary>
    public class BoundaryTransportTo : Reaction
    {
        MolecularPopulation membrane;
        MolecularPopulation bulk;
        Manifold boundary;
        double fluxIntensityConstant;

        public BoundaryTransportTo(MolecularPopulation _bulk, MolecularPopulation _membrane, double _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            fluxIntensityConstant = 1.0 / bulk.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * bulk.BoundaryConcs[boundary.Id];

            bulk.BoundaryFluxes[boundary.Id] += fluxIntensityConstant * intensity;
            membrane.Conc += intensity;
        }
    }

    public class CatalyzedBoundaryActivation : Reaction
    {
        MolecularPopulation bulk;
        MolecularPopulation bulkActivated;
        MolecularPopulation receptor;
        Manifold boundary;
        double fluxIntensityConstant;

        public CatalyzedBoundaryActivation(MolecularPopulation _bulk, MolecularPopulation _bulkActivated, MolecularPopulation _receptor, double _RateConst)
        {
            bulk = _bulk;
            bulkActivated = _bulkActivated;
            receptor = _receptor;
            boundary = receptor.Man;
            fluxIntensityConstant = 1.0 / bulk.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;

            if (bulk.BoundaryConcs[boundary.Id].M != receptor.Man)
            {
                throw new Exception("Receptor and ligand boundary concentration manifolds are unequal.");
            }
            if (receptor.Man != receptor.Man)
            {
                throw new Exception("Receptor and complex manifolds are unequal.");
            }

        }
        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * receptor.Conc * bulk.BoundaryConcs[boundary.Id];

            bulk.BoundaryFluxes[boundary.Id] += fluxIntensityConstant * intensity;
            bulkActivated.BoundaryFluxes[boundary.Id] -= fluxIntensityConstant * intensity;
        }

    }
}


