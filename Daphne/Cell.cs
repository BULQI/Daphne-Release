using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

namespace Daphne
{
    /// <summary>
    /// The basic representation of a biological cell. 
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
        public bool Alive;

        /// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        public bool Cytokinetic;

        public Locator Loc;

        public Cell()
        {
            Alive = true;
            Cytokinetic = false;
            PlasmaMembrane = new Compartment(new TinySphere());
            Cytosol = new Compartment(new TinyBall());
            //OneToOneEmbedding cellEmbed = new OneToOneEmbedding(PlasmaMembrane.Interior, Cytosol.Interior);
            DirectTranslEmbedding cellEmbed = new DirectTranslEmbedding(PlasmaMembrane.Interior, Cytosol.Interior, new int[1]{0}, new double[1]{0.0});
            Cytosol.Interior.Boundaries = new Dictionary<Manifold, Embedding>();
            Cytosol.Interior.Boundaries.Add(PlasmaMembrane.Interior,cellEmbed);

            Index = safeIndex++;
        }

        public Cell(double[] position)
        {
            Alive = true;
            Cytokinetic = false;
            PlasmaMembrane = new Compartment(new TinySphere());
            Cytosol = new Compartment(new TinyBall());
            //OneToOneEmbedding cellEmbed = new OneToOneEmbedding(PlasmaMembrane.Interior, Cytosol.Interior);
            DirectTranslEmbedding cellEmbed = new DirectTranslEmbedding(PlasmaMembrane.Interior, Cytosol.Interior, new int[1]{0}, new double[1]{0.0});
            Cytosol.Interior.Boundaries = new Dictionary<Manifold, Embedding>();
            Cytosol.Interior.Boundaries.Add(PlasmaMembrane.Interior, cellEmbed);

            Index = safeIndex++;

            Loc = new Locator();
            // NOTE: This assumes the usual case - that the cell is moving in 3 dimensions
            // Not sure if we will ever have cells moving in only 2 dimensions - constrained to a surface
            Loc.position = new double[3];
            Loc.position[0] = position[0];
            Loc.position[1] = position[1];
            Loc.position[2] = position[2];
        }

        /// <summary>
        /// Drives the cell's dynamics though time-step dt. The dyanamics is applied in-place: the
        /// cell's state is changed directly through this method.
        /// </summary>
        /// <param name="dt">Time interval.</param>
        public void Step(double dt) 
        {
            // we are using the simplest kind of integrator here. It should be made more sophisticated at some point.
            Cytosol.Step(dt);
            PlasmaMembrane.Step(dt);
            //Differentiator.Step(dt);
            
        }

        /// <summary>
        /// Returns the force the cell applies to the environment.
        /// </summary>
        public double[] Force
        {
            get { return Locomotor.Force(); }
        }

        /// <summary>
        /// Carries out cell division. In addition to returning a daughter cell, the cell's own state is reset is appropriate.
        /// </summary>
        /// <returns>A new daughter cell.</returns>
        public Cell Divide()
        {
            // the daughter is contstructed on the same blueprint as the mother.
            Cell daughter = null;
            // the daughter's state dependent on the mother's pre-division state
            
            // set the mother's new post-division state

            return daughter;
        }

        public int DifferentiationState;

        public Locomotor Locomotor;
        public Compartment Cytosol;
        public Compartment PlasmaMembrane;
        public Differentiator Differentiator;

        public int Index;
        private static int safeIndex = 0;

        // There may be other components specific to a given cell type.
    }

    /// <summary>
    /// Contains information about the cell's location
    /// To be used in cell manager?
    /// NOTE: This information used by MotileTSEmbedding
    /// </summary>
    public struct Locator
    {
        // The position of the embedded manifolds origin in the embedding manifold
        public double[] position;
    }

}



