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
using System.Diagnostics;

namespace Interface
{
    /* Interfaces */

    public interface IDynamic
    {
        void step(double dt);
    }

    public interface IMolecule
    {
        string name { get; }
	    double molecularWeight { get; }
	    double stokesRadius { get; }
    }

    public interface IFieldInitializer
    {
        double initialize(double[] point);
    }

    /* Manifolds */
    public class Manifold
    {
        public int dim { get; private set; }

        public Manifold(int dim)
        {
            this.dim = dim;
        }

        public double distance(double[] source, double[] target)
        {
            return 0;
        }
    }

    public class TinySphere : Manifold
    {
        public TinySphere(int dim)
            : base(dim)
        {
        }
    }

    public class TinyBall : Manifold
    {
        public TinyBall(int dim)
            : base(dim)
        {
        }
    }

    /* ScalarField */

    // Initializer classes for scalar fields
    public class ConstFieldInitializer : IFieldInitializer
    {
        private double cVal;

        public ConstFieldInitializer(double c)
        {
            cVal = c;
        }

        public double initialize(double[] point)
        {
            return cVal;
        }
    }

    public class GaussianFieldInitializer : IFieldInitializer
    {
        private double[] center;
        private double[] sigma;
        private double max;

        public GaussianFieldInitializer(double[] center, double[] sigma, double max)
        {
            this.center = center;
            this.sigma = sigma;
            this.max = max;
        }

        public double initialize(double[] point)
        {
            // return the Gaussian intensity at point
            return 0;
        }
    }

    public abstract class ScalarField
    {
        protected readonly Manifold m;

        public ScalarField(Manifold m, IFieldInitializer initializer)
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
        public DiscreteScalarField(Manifold m, IFieldInitializer initializer)
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
        public MomentExpansionScalarField(Manifold m, IFieldInitializer initializer)
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
        public double molecularWeight { get; private set; }
        public double stokesRadius { get; private set; }

        public Molecule(string name, double molecularWeight, double stokesRadius)
        {
            this.name = name;
            this.molecularWeight = molecularWeight;
            this.stokesRadius = stokesRadius;
        }

    }

    /* MolecularPopulation */
    public class MolecularPopulation : IDynamic
    {
        private readonly Manifold manifold;
        private readonly IMolecule molecule;
        private ScalarField concentration;
        private readonly ScalarField flux; // must this be a dictionary as it is currently in Daphne?

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


    // /* Reaction Complex */
    // public class ReactionComplex : IDynamic
    // {
    //     private Reaction[] reactions;

    //     public ReactionComplex(Reaction[] reactions)
    //     {
    //         this.reactions = reactions;
    //     }

    //     public void step(double dt)
    //     {
    //         foreach (Reaction reaction in reactions)
    //         {
    //             reaction.step(dt);
    //         }
    //     }
    // }

    /* Driver */
    public class Driver
    {
        public static void Main(string[] args)
        {
            // Eventually move this section to an IOC container
            Manifold cytosol = new TinyBall(3);
            Manifold membrane = new TinySphere(2);
            double[] origin = new double[3];
            double[,] rotation = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            IFieldInitializer init = new ConstFieldInitializer(0);
            ScalarField concentration = new DiscreteScalarField(cytosol, init);
            ScalarField flux = new DiscreteScalarField(membrane, init);
            IMolecule a = new Molecule("molecule", 0.0, 0.0);
            IMolecule b = new Molecule("molecule", 0.0, 0.0);
            IMolecule c = new Molecule("molecule", 0.0, 0.0);
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
            //ReactionComplex reactionComplex = new ReactionComplex(reactions);

            Debug.Print("Construction success!");
        }
    }
}
