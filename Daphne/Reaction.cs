/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NativeDaphne;
using Gene = NativeDaphne.Nt_Gene;
using Nt_ManifoldRing;

namespace Daphne
{
    public interface Reaction
    {
        double RateConstant { get; set; }
        void Step(double dt);
    }

    // Fundamental reactions

    public class Annihilation : Nt_Annihilation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }

        public Annihilation(MolecularPopulation _reactant, double _RateConst)
            : base(_reactant, _RateConst)
        {
            reactant = _reactant;
            intensity = new ScalarField(_reactant.Man);
        }

        /// <summary>
        /// step handled by base class.
        /// </summary>
        /// <param name="dt"></param>
        public override void Step(double dt)
        {
            intensity.reset(reactant.Conc).Multiply(dt * RateConstant);
            reactant.Conc.Subtract(intensity);
        }
    }

    public class Association : Nt_Association, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant1 { get; set; }
        public MolecularPopulation reactant2 { get; set; }
        public MolecularPopulation product { get; set; }

        public Association(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
            : base(_reactant1, _reactant2, _product, _RateConst)
        {
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant1.Man);
            //Type = ReactionType.Association;
        }

        public override void Step(double dt)
        {
            //base.Step(dt);
            //return;
            intensity = intensity.reset(reactant1.Conc).Multiply(reactant2.Conc).Multiply(RateConstant * dt);
            reactant1.Conc.Subtract(intensity);
            reactant2.Conc.Subtract(intensity);
            product.Conc.Add(intensity);
        }
    }

    public class Dimerization : Nt_Dimerization, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public Dimerization(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_reactant, _product, _RateConst)
        {
            reactant = _reactant;
            product = _product;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.Dimerization;
        }

        public override void Step(double dt)
        {
            intensity.reset(reactant.Conc).Multiply(reactant.Conc).Multiply(RateConstant * dt);
            product.Conc.Add(intensity);
            reactant.Conc.Subtract(intensity.Multiply(2));
        }
    }

    public class DimerDissociation : Nt_DimerDissociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public DimerDissociation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_reactant, _product, _RateConst)
        {
            reactant = _reactant;
            product = _product;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.DimerDissociation;
        }

        public override void Step(double dt)
        {
            intensity.reset(reactant.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product.Conc.Add(intensity.Multiply(2));
        }
    }

    public class Dissociation : Nt_Dissociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product1 { get; set; }
        public MolecularPopulation product2 { get; set; }

        public Dissociation(MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
            : base(_reactant, _product1, _product2, _RateConst)
        {
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.Dissociation;
        }

        public override void Step(double dt)
        {
            //base.Step(dt);
            //return;
            intensity.reset(reactant.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product1.Conc.Add(intensity);
            product2.Conc.Add(intensity);
        }
    }

    public class Transformation : Nt_Transformation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public Transformation(MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_reactant, _product, _RateConst)
        {
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.Transformation;
            intensity = new ScalarField(reactant.Man);
        }

        public override void Step(double dt)
        {
            //base.Step(dt);
            //return;
            intensity.reset(reactant.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product.Conc.Add(intensity);
        }
    }

    // Catalyzed reactions
    /// <summary>
    /// a + e -> 2e
    /// NOTE: Not a catalyzed reaction in the strict sense, since the catalyst stoichiometry changes in this reaction.
    /// </summary>
    public class AutocatalyticTransformation : Nt_AutocatalyticTransformation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public AutocatalyticTransformation(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
            : base(_reactant1, _reactant2, _product, _RateConst)
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
            intensity = new ScalarField(reactant.Man);
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(reactant.Conc).Multiply(RateConstant * dt);
            catalyst.Conc.Add(intensity);
            reactant.Conc.Subtract(intensity);
        }
    }

    public class CatalyzedAnnihilation : Nt_CatalyzedAnnihilation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }

        public CatalyzedAnnihilation(MolecularPopulation _catalyst, MolecularPopulation _reactant, double _RateConst)
            : base(_catalyst, _reactant, _RateConst)
        {
            reactant = _reactant;
            catalyst = _catalyst;
            //Type = ReactionType.CatalyzedAnnihilation;
            intensity = new ScalarField(reactant.Man);
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(reactant.Conc).Multiply(dt * RateConstant);
            reactant.Conc.Subtract(intensity);
        }
    }

    public class CatalyzedAssociation : Nt_CatalyzedAssociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant1 { get; set; }
        public MolecularPopulation reactant2 { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedAssociation(MolecularPopulation _catalyst, MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product, double _RateConst)
            : base(_catalyst, _reactant1, _reactant2, _product, _RateConst)
        {
            catalyst = _catalyst;
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
            RateConstant = _RateConst;
            //Type = ReactionType.CatalyzedAssociation;
            intensity = new ScalarField(reactant1.Man);
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(reactant1.Conc).Multiply(reactant2.Conc).Multiply(RateConstant * dt);
            reactant1.Conc.Subtract(intensity);
            reactant2.Conc.Subtract(intensity);
            product.Conc.Add(intensity);
        }
    }

    public class CatalyzedCreation : Nt_CatalyzedCreation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedCreation(MolecularPopulation _catalyst, MolecularPopulation _product, double _RateConst)
            : base(_catalyst, _product, _RateConst)
        {
            if (_catalyst.Man != _product.Man)
            {
                throw new Exception("Manifold mismatch");
            }

            catalyst = _catalyst;
            product = _product;
            //Type = ReactionType.CatalyzedCreation;
            intensity = new ScalarField(catalyst.Man);
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(dt * RateConstant);
            product.Conc.Add(intensity);
        }
    }

    public class CatalyzedDimerization : Nt_CatalyzedDimerization, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public CatalyzedDimerization(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_catalyst, _reactant, _product, _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.CatalyzedDimerization;
        }

        public override void Step(double dt)
        {
            intensity.reset(reactant.Conc).Multiply(reactant.Conc).Multiply(catalyst.Conc).Multiply(RateConstant * dt);
            product.Conc.Add(intensity);
            reactant.Conc.Subtract(intensity.Multiply(2));
        }
    }

    public class CatalyzedDimerDissociation : Nt_CatalyzedDimerDissociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }
        public MolecularPopulation catalyst { get; set; }

        public CatalyzedDimerDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_catalyst, _reactant, _product, _RateConst)
        {
            reactant = _reactant;
            product = _product;
            catalyst = _catalyst;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.CatalyzedDimerDissociation;
        }

        public override void Step(double dt)
        {
            intensity.reset(reactant.Conc).Multiply(catalyst.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product.Conc.Add(intensity.Multiply(2));
        }
    }

    public class CatalyzedDissociation : Nt_CatalyzedDissociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product1 { get; set; }
        public MolecularPopulation product2 { get; set; }

        public CatalyzedDissociation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2, double _RateConst)
            : base(_catalyst, _reactant, _product1, _product2, _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.CatalyzedDissociation;
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(reactant.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product1.Conc.Add(intensity);
            product2.Conc.Add(intensity);
        }
    }

    public class CatalyzedTransformation : Nt_CatalyzedTransformation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation catalyst { get; set; }
        public MolecularPopulation reactant { get; set; }
        public MolecularPopulation product { get; set; }

        public CatalyzedTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product, double _RateConst)
            : base(_catalyst, _reactant, _product, _RateConst)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product = _product;
            RateConstant = _RateConst;
            intensity = new ScalarField(reactant.Man);
            //Type = ReactionType.CatalyzedTransformation;
        }

        public override void Step(double dt)
        {
            intensity.reset(catalyst.Conc).Multiply(reactant.Conc).Multiply(RateConstant * dt);
            reactant.Conc.Subtract(intensity);
            product.Conc.Add(intensity);
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
    //Native instance to be imlemented.
    public class GeneralizedReaction : Reaction
    {
        public double RateConstant { get; set; }

        private ScalarField intensity;
        // int[] = [reaction, product] stoichiometric coefficients for each MolecularPopulation
        Dictionary<MolecularPopulation, int[]> genReac;

        public GeneralizedReaction(Dictionary<MolecularPopulation, int[]> _genReac, double _RateConst)
        {
            genReac = new Dictionary<MolecularPopulation, int[]>();
            genReac = _genReac;
            RateConstant = _RateConst;
            //Type = ReactionType.Generalized;
            if (genReac.Count > 0)
            {
                MolecularPopulation mp = genReac.First().Key;
                intensity = new ScalarField(mp.Man);
            }
        }

        //public override void Step(double dt)
        public void Step(double dt)
        {
            // Catalysts have equal reaction and product stoichiometries
            // stoich[.,0] = stoich[.,1]

            // Calculate the product of the powers of all reactant concentrations.
            // power = reactant stoichiometry
            // MolecularPopulations that do not participate as reactants will not 
            // contribute due to kvp.Value[0]=0.
            bool intensity_initalized = false;
            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
                for (int j = 0; j < kvp.Value[0]; j++)
                {
                    if (!intensity_initalized)
                    {
                        intensity.reset(kvp.Key.Conc);
                        intensity_initalized = true;
                    }
                    else
                    {
                        intensity.Multiply(kvp.Key.Conc);
                    }
                }
            }
            intensity.Multiply(dt * RateConstant);

            // Update the concentration according to the difference between the 
            // product and reactant stoichiometries.
            foreach (KeyValuePair<MolecularPopulation, int[]> kvp in genReac)
            {
                kvp.Key.Conc.Add(intensity * (kvp.Value[1] - kvp.Value[0]));
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
    public class BoundaryAssociation : Nt_BoundaryAssociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation receptor { get; set; }
        public MolecularPopulation ligand { get; set; }
        public MolecularPopulation complex { get; set; }
        Manifold boundary;

        public BoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst) :
            base(_receptor, _ligand, _complex, _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            boundaryId = boundary.Id;
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
            intensity = new ScalarField(this.receptor.Man);
        }

        public override void Step(double dt)
        {
            ScalarField boundarConc = ligand.BoundaryConcs[boundary.Id];
            intensity.reset(receptor.Conc).Multiply(ligand.BoundaryConcs[boundary.Id]).Multiply(RateConstant);
            ligand.BoundaryFluxes[boundary.Id].Add(intensity);
            intensity.Multiply(dt);
            receptor.Conc.Subtract(intensity);
            complex.Conc.Add(intensity);
        }
    }

    public class BoundaryDissociation : Nt_BoundaryDissociation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation receptor { get; set; }
        public MolecularPopulation ligand { get; set; }
        public MolecularPopulation complex { get; set; }
        Manifold boundary;

        public BoundaryDissociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst) :
            base(_receptor, _ligand, _complex, _RateConst)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryDissociation;
            intensity = new ScalarField(this.boundary);
        }

        public override void Step(double dt)
        {
            intensity.reset(complex.Conc).Multiply(RateConstant);

            ligand.BoundaryFluxes[boundary.Id].Subtract(intensity);
            intensity.Multiply(dt);
            receptor.Conc.Add(intensity);
            complex.Conc.Subtract(intensity);
        }
    }

    /// <summary>
    /// Transport of material from boundary to bulk
    /// </summary>
    public class BoundaryTransportFrom : Nt_BoundaryTransportFrom, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation membrane { get; set; }
        public MolecularPopulation bulk { get; set; }
        Manifold boundary;

        public BoundaryTransportFrom(MolecularPopulation _membrane, MolecularPopulation _bulk, double _RateConst)
            : base(_membrane, _bulk, _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            boundaryId = boundary.Id;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryTransportFrom;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
            intensity = new ScalarField(boundary);
        }

        public override void Step(double dt)
        {
            intensity.reset(membrane.Conc).Multiply(RateConstant);

            bulk.BoundaryFluxes[boundary.Id].Subtract(intensity);
            membrane.Conc.Subtract(intensity.Multiply(dt));
        }
    }

    /// <summary>
    /// Transport of material from bulk to boundary
    /// </summary>
    public class BoundaryTransportTo : Nt_BoundaryTransportTo, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation membrane { get; set; }
        public MolecularPopulation bulk { get; set; }
        Manifold boundary;

        public BoundaryTransportTo(MolecularPopulation _bulk, MolecularPopulation _membrane, double _RateConst)
            : base(_bulk, _membrane, _RateConst)
        {
            bulk = _bulk;
            membrane = _membrane;
            boundary = membrane.Man;
            boundaryId = boundary.Id;
            RateConstant = _RateConst;
            //Type = ReactionType.BoundaryTransportTo;

            if (bulk.BoundaryConcs[boundary.Id].M != membrane.Man)
            {
                throw new Exception("Membrane and boundary concentration manifolds are unequal.");
            }
            intensity = new ScalarField(boundary);
        }

        public override void Step(double dt)
        {
            intensity.reset(bulk.BoundaryConcs[boundary.Id]).Multiply(RateConstant);

            bulk.BoundaryFluxes[boundary.Id].Add(intensity);
            membrane.Conc.Add(intensity.Multiply(dt));
        }
    }

    public class CatalyzedBoundaryActivation : Nt_CatalyzedBoundaryActivation, Reaction
    {
        private ScalarField intensity;
        public MolecularPopulation bulk { get; set; }
        public MolecularPopulation bulkActivated { get; set; }
        public MolecularPopulation receptor { get; set; }
        Manifold boundary;

        public CatalyzedBoundaryActivation(MolecularPopulation _bulk, MolecularPopulation _bulkActivated, MolecularPopulation _receptor, double _RateConst) :
            base(_bulk, _bulkActivated, _receptor, _RateConst)
        {
            bulk = _bulk;
            bulkActivated = _bulkActivated;
            receptor = _receptor;
            boundary = receptor.Man;
            boundaryId = boundary.Id;
            RateConstant = _RateConst;

            if (bulk.BoundaryConcs[boundary.Id].M != receptor.Man)
            {
                throw new Exception("Receptor and ligand boundary concentration manifolds are unequal.");
            }
            if (receptor.Man != receptor.Man)
            {
                throw new Exception("Receptor and complex manifolds are unequal.");
            }
            intensity = new ScalarField(boundary);
        }
        public override void Step(double dt)
        {

            intensity.reset(receptor.Conc).Multiply(RateConstant).Multiply(bulk.BoundaryConcs[boundary.Id]);

            // fluxes, so multiplication by time step in diffusion
            bulk.BoundaryFluxes[boundary.Id].Add(intensity);
            bulkActivated.BoundaryFluxes[boundary.Id].Subtract(intensity);
        }

    }

    /// <summary>
    /// Gene transcription with molecular population as a product.
    /// The gene activation level may be modified during the simulation, depending on the state of the cell.
    /// </summary>
    public class Transcription : Nt_Transcription, Reaction
    {
        private double intensity;
        public Gene gene { get; set; }
        public MolecularPopulation product { get; set; }

        public Transcription(Gene _gene, MolecularPopulation _product, double _rate)
            : base(_gene, _product, _rate)
        {
            gene = _gene;
            product = _product;
            RateConstant = _rate;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * gene.CopyNumber * gene.ActivationLevel * dt;
            product.Conc.Add(intensity);
        }

    }
}


