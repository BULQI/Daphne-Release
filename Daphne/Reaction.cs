﻿using System;
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
        // to enable, remove all comments at line beginnings involving ReactionType
        //public ReactionType Type { get; set; }

        public abstract void Step(double dt);
    }

    // Fundamental reactions
    // See 'Dynamics for MolPops.pdf' for mathematical detail on the gradient update equations.

    public class Annihilation : Reaction
    {
        public MolecularPopulation reactant { get; set; }

        public Annihilation(MolecularPopulation _reactant, double _RateConst)
        {
            reactant = _reactant;
            RateConstant = _RateConst;
            //Type = ReactionType.Annihilation;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * reactant.Conc;
            reactant.Conc -= intensity;
        }
    }

    public class Association : Reaction
    {
        public MolecularPopulation reactant1 { get; set; }
        public MolecularPopulation reactant2 { get; set; }
        public MolecularPopulation product { get; set; }

        public Association(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
        {
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.Association;
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
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public Dimerization(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.Dimerization;
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
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public DimerDissociation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.DimerDissociation;
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
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product1 { get; set; }
        public MolecularPopulation product2 { get; set; }

        public Dissociation(MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
        {
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
            //Type = ReactionType.Dissociation;
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
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public Transformation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.Transformation;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= intensity;
            product.Conc += intensity;
        }
    }

    // Catalyzed reactions
    /// <summary>
    /// a + e -> 2e
    /// NOTE: Not a catalyzed reaction in the strict sense, since the catalyst stoichiometry changes in this reaction.
    /// </summary>
    public class AutocatalyticTransformation : Reaction
    {
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public AutocatalyticTransformation(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
        {
            if (_reactant1.Molecule.Name == _product.Molecule.Name)
            {
                reactant = _reactant2;
                catalyst = _reactant1;
            }
            else
            {
                reactant = _reactant1;
                catalyst = _reactant2;
            }
            RateConstant = _RateConst;
            //Type = ReactionType.AutocatalyticTransformation;
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
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }

        public CatalyzedAnnihilation(MolecularPopulation _catalyst, MolecularPopulation _reactant, double _RateConst)
        {
            reactant = _reactant;
            catalyst = _catalyst;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedAnnihilation;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc * reactant.Conc;
            reactant.Conc -= intensity;
        }
    }

    public class CatalyzedAssociation : Reaction
    {
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant1 { get; set; }
        public MolecularPopulation reactant2 { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedAssociation(MolecularPopulation _catalyst, MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
        {
            catalyst = _catalyst;
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedAssociation;
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
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedCreation(MolecularPopulation _catalyst, MolecularPopulation _product, double _RateConst)
        {
            if (_catalyst.Man != _product.Man)
            {
                throw new Exception("Manifold mismatch");
            }

            catalyst = _catalyst;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedCreation;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc;
            product.Conc += intensity;
        }
    }

    public class CatalyzedDimerization : Reaction
    {
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public CatalyzedDimerization(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedDimerization;
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
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public CatalyzedDimerDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedDimerDissociation;
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
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product1 { get; set; }
        public MolecularPopulation product2 { get; set; }

        public CatalyzedDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedDissociation;
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
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedTransformation;
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
            //Type = ReactionType.Generalized;
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
    // Concentrations of bulk molecules are updated through the flux terms.
    // Flux of material through a surface is the number of molecules per area per time (molecules / um^2-min)
    // Convention: the outward pointing normal to the surface is defined relative to the bulk volume 
    //             and positive when pointing out of the volume.
    // With this convention, positive flux reduces the bulk concentration.
    // Flux terms are accumulating and need to be zeroed in the diffusion step.
    // NOTE: The flux terms are not multiplied by the time step until the diffusion step. If, in the future,
    //       we use different time step-sizes for reactions and diffusion, will this still work?

    /// <summary>
    /// Appropriate for boundary manifolds that are not zero-dimensional.
    /// </summary>
    public class BoundaryAssociation : Reaction
    {
        public MolecularPopulation receptor { get; set; }
        public MolecularPopulation ligand { get; set; }
        public MolecularPopulation complex { get; set; }
        Manifold boundary;
 
        public BoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryAssociation;

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
            intensity = RateConstant * receptor.Conc * ligand.BoundaryConcs[boundary.Id];

            ligand.BoundaryFluxes[boundary.Id] += intensity;
            receptor.Conc -= intensity * dt;
            complex.Conc += intensity * dt;
        }
    }

    public class BoundaryDissociation : Reaction
    {
        public MolecularPopulation receptor { get; set; }
        public MolecularPopulation ligand { get; set; }
        public MolecularPopulation complex { get; set; }
        Manifold boundary;

        public BoundaryDissociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryDissociation;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * complex.Conc;

            ligand.BoundaryFluxes[boundary.Id] -= intensity;
            receptor.Conc += intensity * dt;
            complex.Conc -= intensity * dt;
        }
    }

    /// <summary>
    /// Transport of material from boundary to bulk
    /// </summary>
    public class BoundaryTransportFrom : Reaction
    {
        public MolecularPopulation membrane { get; set; }
        public MolecularPopulation bulk { get; set; }
        Manifold boundary;

        public BoundaryTransportFrom(MolecularPopulation _membrane, MolecularPopulation _bulk, double _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryTransportFrom;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
        }

        public override void Step(double dt)
        {
            intensity = RateConstant  * membrane.Conc;

            bulk.BoundaryFluxes[boundary.Id] -= intensity;
            membrane.Conc -= intensity * dt;
        }
    }

    /// <summary>
    /// Transport of material from bulk to boundary
    /// </summary>
    public class BoundaryTransportTo : Reaction
    {
        public MolecularPopulation membrane { get; set; }
        public MolecularPopulation bulk { get; set; }
        Manifold boundary;

        public BoundaryTransportTo(MolecularPopulation _bulk, MolecularPopulation _membrane, double _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryTransportTo;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * bulk.BoundaryConcs[boundary.Id];

            bulk.BoundaryFluxes[boundary.Id] += intensity;
            membrane.Conc += intensity * dt;
        }
    }

    public class CatalyzedBoundaryActivation : Reaction
    {
        public MolecularPopulation bulk { get; set; }
        public MolecularPopulation bulkActivated { get; set; }
        public MolecularPopulation receptor { get; set; }
        Manifold boundary;

        public CatalyzedBoundaryActivation(MolecularPopulation _bulk, MolecularPopulation _bulkActivated, MolecularPopulation _receptor, double _RateConst)
        {
            bulk = _bulk;
            bulkActivated = _bulkActivated;
            receptor = _receptor;
            boundary = receptor.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedBoundaryActivation;

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
            intensity = RateConstant * receptor.Conc * bulk.BoundaryConcs[boundary.Id];

            bulk.BoundaryFluxes[boundary.Id] += intensity;
            bulkActivated.BoundaryFluxes[boundary.Id] -= intensity ;
        }

    }
}


