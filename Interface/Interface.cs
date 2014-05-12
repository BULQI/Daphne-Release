using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Interface
{

    /* Interfaces */

    public interface IDynamic
    {
        void step(double dt);
    }


    public interface IEmbedding
    {
        double[] map(double[] point);
    }

    public interface IMolecule
    {
        string name { get; }
    }

    /* Manifolds */
    public abstract class Manifold
    {
        public int dim { get; private set; }

        public Manifold(int dim)
        {
            this.dim = dim;
        }

        public abstract double distance(double[] source, double[] target);
    }

    public class TinyManifold : Manifold
    {
        public TinyManifold(int dim)
            : base(dim)
        {
        }

        public override double distance(double[] source, double[] target)
        {
            return 0;
        }

    }

    /* ScalarField */
    // Initializer delegate for scalar field
    public delegate double Del(double[] point);

    public abstract class ScalarField
    {
        protected readonly Manifold m;

        public ScalarField(Manifold m, Del initializer)
        {
            this.m = m;
            // do something with initializer
        }

        public abstract double get(double[] point);
        public abstract double get(int i);
        public abstract void set(int i, double d);

        public double this[int i]
        {
            get { return get(i); }
            set { set(i, value); }
        }

        // how operators are to be defined etc is not clear
        public abstract void plus(ScalarField s);
        public abstract void times(ScalarField s);
        public abstract void times(double x);

        public abstract double laplacian(double[] point);
        public abstract double[] gradient(double[] point);
    }

    public class DiscreteScalarField : ScalarField
    {
        public DiscreteScalarField(Manifold m, Del initializer)
            : base(m, initializer)
        {
        }


        public override double get(double[] point)
        {
            return 0;
        }

        public override double get(int i)
        {
            return 0;
        }

        public override void set(int i, double d)
        {
            throw new NotImplementedException();
        }

        public override void plus(ScalarField s)
        {
        }

        public override void times(ScalarField s)
        {
        }


        public override void times(double x)
        {
        }

        public override double laplacian(double[] point)
        {
            return 0;
        }

        public override double[] gradient(double[] point)
        {
            return new double[m.dim];
        }

    }


    public class MomentExpansionScalarField : ScalarField
    {
        public MomentExpansionScalarField(Manifold m, Del initializer)
            : base(m, initializer)
        {
        }


        public override double get(double[] point)
        {
            return 0;
        }

        public override double get(int i)
        {
            return 0;
        }

        public override void set(int i, double d)
        {
            throw new NotImplementedException();
        }

        public override void plus(ScalarField s)
        {
        }

        public override void times(ScalarField s)
        {
        }


        public override void times(double x)
        {
        }

        public override double laplacian(double[] point)
        {
            return 0;
        }

        public override double[] gradient(double[] point)
        {
            return new double[m.dim];
        }
    }

    /* Molecule */

    public class Molecule : IMolecule
    {
        public string name { get; private set; }
        public double mass { get; private set; }

        public Molecule(string name, double mass)
        {
            this.name = name;
            this.mass = mass;
        }

    }

    /* MolecularPopulation */
    public class MolecularPopulation : IDynamic
    {
        private readonly IMolecule molecule;
        private readonly ScalarField concentration;
        private readonly ScalarField flux;

        public MolecularPopulation(IMolecule molecule, ScalarField concentration, ScalarField flux)
        {
            this.molecule = molecule;
            this.concentration = concentration;
            this.flux = flux;
        }

        public void step(double dt)
        {
        }
    }

    /* Embedding */
    public class Embedding : IEmbedding
    {
        private readonly Manifold domain;
        private readonly Manifold range;
        private double[] translation;
        private double[,] rotation;

        public Embedding(Manifold domain, Manifold range, double[] translation, double[,] rotation)
        {
            this.domain = domain;
            this.range = range;
            this.translation = translation;
            this.rotation = rotation;
        }

        public double[] map(double[] point)
        {
            return new double[range.dim];
        }
    }

    /* Reaction */
    public class Reaction : IDynamic
    {
        private double[] reactantStoichiometricCoefficients;
        private double[] productStoichiometricCoefficients;
        private double rateConstant;

        public Reaction(double[] reactantStoichiometricCoefficients, double[] productStoichiometricCoefficients, double rateConstant)
        {
            this.reactantStoichiometricCoefficients = reactantStoichiometricCoefficients;
            this.productStoichiometricCoefficients = productStoichiometricCoefficients;
            this.rateConstant = rateConstant;
        }

        public void step(double dt)
        {
        }
    }

    /* Concrete Reaction classes */
    public class InteriorReaction : Reaction
    {
        private List<MolecularPopulation> reactants;
        private List<MolecularPopulation> products;

        public InteriorReaction(double[] reactantStoichiometricCoefficients, double[] productStoichiometricCoefficients,
            List<MolecularPopulation> reactants, List<MolecularPopulation> products, double rateConstant) :
            base(reactantStoichiometricCoefficients, productStoichiometricCoefficients, rateConstant)
        {
            this.reactants = reactants;
            this.products = products;
        }
    }

    public class BoundaryReaction : Reaction
    {
        private List<MolecularPopulation> interiorReactants;
        private List<MolecularPopulation> interiorProducts;
        private List<MolecularPopulation> boundaryReactants;
        private List<MolecularPopulation> boundaryProducts;

        public BoundaryReaction(double[] reactantStoichiometricCoefficients, double[] productStoichiometricCoefficients,
            List<MolecularPopulation> interiorReactants, List<MolecularPopulation> interiorProducts,
            List<MolecularPopulation> boundaryReactants, List<MolecularPopulation> boundaryProducts, double rateConstant) :
            base(reactantStoichiometricCoefficients, productStoichiometricCoefficients, rateConstant)
        {
            this.interiorReactants = interiorReactants;
            this.interiorProducts = interiorProducts;
            this.boundaryReactants = boundaryReactants;
            this.boundaryProducts = boundaryProducts;
        }
    }


    /* Reaction Complex */
    public class ReactionComplex : IDynamic
    {
        private Reaction[] reactions;

        public ReactionComplex(Reaction[] reactions)
        {
            this.reactions = reactions;
        }

        public void step(double dt)
        {
            foreach (Reaction reaction in reactions)
            {
                reaction.step(dt);
            }
        }
    }

    /* Driver */
    public class Driver
    {
        public static void Main(string[] args)
        {
            // Eventually move this section to an IOC container
            Manifold cytosol = new TinyManifold(3);
            Manifold membrane = new TinyManifold(2);
            double[] origin = new double[3];
            double[,] rotation = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            Embedding cytosolToMembrane = new Embedding(cytosol, membrane, origin, rotation);
            ScalarField concentration = new DiscreteScalarField(cytosol, _ => 0);
            ScalarField flux = new DiscreteScalarField(membrane, _ => 0);
            IMolecule a = new Molecule("molecule", 0.0);
            IMolecule b = new Molecule("molecule", 0.0);
            IMolecule c = new Molecule("molecule", 0.0);
            MolecularPopulation molecularPopulationA = new MolecularPopulation(a, concentration, flux);
            MolecularPopulation molecularPopulationB = new MolecularPopulation(b, concentration, flux);
            MolecularPopulation molecularPopulationC = new MolecularPopulation(c, concentration, flux);
            double[] sigmas = new double[] { 1, 1, 1 };
            double[] taus = new double[] { 0, 0, 0 };
            List<MolecularPopulation> reactants = new List<MolecularPopulation> { molecularPopulationA, molecularPopulationB };
            List<MolecularPopulation> products = new List<MolecularPopulation> { molecularPopulationC };
            List<MolecularPopulation> interiorReactants = new List<MolecularPopulation> { molecularPopulationA, molecularPopulationB };
            List<MolecularPopulation> interiorProducts = new List<MolecularPopulation> { molecularPopulationC };
            List<MolecularPopulation> boundaryReactants = new List<MolecularPopulation> { molecularPopulationA, molecularPopulationB };
            List<MolecularPopulation> boundaryProducts = new List<MolecularPopulation> { molecularPopulationC };
            double rate = 1.0;
            Reaction interiorReaction = new InteriorReaction(sigmas, taus, reactants, products, rate);
            Reaction boundaryReaction = new BoundaryReaction(sigmas, taus, interiorReactants, interiorProducts, boundaryReactants, boundaryProducts, rate);
            Reaction[] reactions = new Reaction[] { interiorReaction, boundaryReaction };
            ReactionComplex reactionComplex = new ReactionComplex(reactions);

            Debug.Print("Construction success!");
        }
    }
}
