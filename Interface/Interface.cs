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


    public class MomemtnExpansionScalarField : ScalarField
    {
        public MomemtnExpansionScalarField(Manifold m, Del initializer)
            : base(m, initializer)
        {
        }


        public override double get(double[] point)
        {
            return 0;
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
    public abstract class MolecularPopulation : IDynamic
    {
        private readonly IMolecule molecule;
        private readonly ScalarField concentration;
        private readonly ScalarField flux;

        public MolecularPopulation(IMolecule molecule, ScalarField concentraiton, ScalarField flux)
        {
            this.molecule = molecule;
            this.concentration = concentraiton;
            this.flux = flux;
        }

        public abstract void step(double dt);
    }

    public class DiscreteMolecularPopulation : MolecularPopulation
    {
        public DiscreteMolecularPopulation(IMolecule molecule, ScalarField concentraiton, ScalarField flux) :
            base(molecule, concentraiton, flux)
        {
        }

        public override void step(double dt)
        {
        }
    }

    public class ContinuousMolecularPopulation : MolecularPopulation
    {
        public ContinuousMolecularPopulation(IMolecule molecule, ScalarField concentraiton, ScalarField flux) :
            base(molecule, concentraiton, flux)
        {
        }

        public override void step(double dt)
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
        private int[] sigmas;
        private int[] taus;
        private MolecularPopulation[] reactants;
        private double rate;

        public Reaction(int[] sigmas, int[] taus, MolecularPopulation[] reactants, double rate)
        {
            this.sigmas = sigmas;
            this.taus = taus;
            this.reactants = reactants;
            this.rate = rate;
        }

        public void step(double dt)
        {
        }
    }

    /* Reaaction Complex */
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
            ScalarField flux = new MomemtnExpansionScalarField(membrane, _ => 0);
            IMolecule a = new Molecule("molecule", 0.0);
            IMolecule b = new Molecule("molecule", 0.0);
            IMolecule c = new Molecule("mole\tcule", 0.0);
            MolecularPopulation molecularPopulationA = new DiscreteMolecularPopulation(a, concentration, flux);
            MolecularPopulation molecularPopulationB = new DiscreteMolecularPopulation(b, concentration, flux);
            MolecularPopulation molecularPopulationC = new ContinuousMolecularPopulation(c, concentration, flux);
            int[] sigmas = new int[] { 1, 1, 1 };
            int[] taus = new int[] { 0, 0, 0 };
            MolecularPopulation[] reactants = new MolecularPopulation[] { 
				molecularPopulationA, molecularPopulationB, molecularPopulationC };
            double rate = 1.0;
            Reaction reaction = new Reaction(sigmas, taus, reactants, rate);
            Reaction[] reactions = new Reaction[] { reaction };
            ReactionComplex reactionComplex = new ReactionComplex(reactions);

            Debug.Print("Construction success!");
        }
    }
}
