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

    // Fundamental reactions

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
            reactant.Conc += intensity;
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

    //public class AutocatalyticTransformation : Reaction
    //{
    //    MolecularPopulation catalyst;
    //    MolecularPopulation reactant1;

    //    public AutocatalyticTransformation(MolecularPopulation _catalyst, MolecularPopulation _reactant1, double _RateConst)
    //    {
    //        catalyst = _catalyst;
    //        reactant1 = _reactant1;
    //        RateConstant = _RateConst;
    //    }

    //    public override void Step(double dt)
    //    {
    //        intensity = (RateConstant) * dt * catalyst.Conc * reactant1.Conc;
    //        catalyst.Conc += intensity;
    //        reactant1.Conc -= intensity;
    //    }
    //}

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
            intensity = RateConstant * dt * reactant.Conc;
            reactant.Conc -= 2*intensity;
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
            product.Conc += 2*intensity;
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

    // Boundary reactions

    public class BoundaryAssociation : Reaction
    {
        MolecularPopulation receptor;
        MolecularPopulation ligand;
        MolecularPopulation complex;
        DiscretizedManifold boundary;
        double fluxIntensityConstant;

        public BoundaryAssociation(MolecularPopulation _receptor, MolecularPopulation _ligand, MolecularPopulation _complex, double _RateConst)
        {
            // TODO: check to ensure that the manifolds have the appropriate relationships

            receptor = _receptor;
            ligand = _ligand;
            complex = _complex;
            boundary = complex.Man;
            fluxIntensityConstant = 1.0 / ligand.Molecule.DiffusionCoefficient;
            RateConstant = _RateConst;
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

            ligand.Fluxes[boundary] -= fluxIntensityConstant * intensity;
            receptor.Conc += intensity;
            complex.Conc -= intensity;
        }
    }

    // Transport

}


