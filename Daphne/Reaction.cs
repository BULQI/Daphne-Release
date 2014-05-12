using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    public abstract class Reaction
    {
        public double RateConstant;
        protected ScalarField intensity;

        public abstract void Step(double dt);
    }

    public class Annihilation : Reaction
    {
        MolecularPopulation reactant;

        public Annihilation(MolecularPopulation _reactant)
        {
            reactant = _reactant;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * reactant.Conc;
            reactant.Conc += intensity;
        }
    }
 
    
    public class CatalyzedCreation : Reaction
    {
        MolecularPopulation catalyst;
        MolecularPopulation product;

        public CatalyzedCreation(MolecularPopulation _catalyst, MolecularPopulation _product)
        {
            if (_catalyst.Man != _product.Man)
            {
                throw new Exception("Manifold mismatch");
            }

            catalyst = _catalyst;
            product = _product;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc;
            product.Conc += intensity;
        }
    }

    public class CatalyzedTransformation : Reaction
    {
        private int nReactants;
        private int nProducts;
        MolecularPopulation catalyst;
        MolecularPopulation reactant;
        MolecularPopulation product;

        public CatalyzedTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant, MolecularPopulation _product)
        {
            catalyst = _catalyst;
            reactant = _reactant;
            product = _product;
        }

        public override void Step(double dt)
        {
            intensity = (dt * RateConstant) * catalyst.Conc * reactant.Conc;
            reactant.Conc -= intensity;
            product.Conc += intensity;
        }
    }

    public class Association : Reaction
    {
        MolecularPopulation reactant1;
        MolecularPopulation reactant2;
        MolecularPopulation product;

        public Association(MolecularPopulation _reactant1, MolecularPopulation _reactant2, MolecularPopulation _product)
        {
            reactant1 = _reactant1;
            reactant2 = _reactant2;
            product = _product;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant) * dt * reactant1.Conc * reactant2.Conc;
            reactant1.Conc -= intensity;
            reactant2.Conc -= intensity;
            product.Conc += intensity;
        }
    }

    public class Dissociation : Reaction
    {
        MolecularPopulation reactant;
        MolecularPopulation product1, product2;

        public Dissociation(MolecularPopulation _reactant, MolecularPopulation _product1, MolecularPopulation _product2)
        {
            reactant = _reactant;
            product1 = _product1;
            product2 = _product2;
        }

        public override void Step(double dt)
        {
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= intensity;
            product1.Conc += intensity;
            product2.Conc += intensity;
        }
    }

    public class BoundaryAssociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        Manifold boundary;
        double fluxIntensityConstant;

        public BoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex)
        {
            // TODO: check to ensure that the manifolds have the appropriate relationships

            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * receptor.Conc * ligand.BoundaryConcs[boundary];

            ligand.Fluxes[boundary] += fluxIntensityConstant * intensity;
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

        public BoundaryDissociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex)
        {
            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
        }

        public override void Step(double dt)
        {
            intensity = (RateConstant * dt) * complex.Conc;

            ligand.Fluxes[boundary] -= fluxIntensityConstant * intensity;
            receptor.Conc += intensity;
            complex.Conc -= intensity;
        }
    }
}
