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

        public Cell()
        {
            // Original
            //Alive = true;
            //Cytokinetic = false;
            //Cytosol = new Compartment(new TinyBall());
            //PlasmaMembrane = new Compartment(new TinySphere());
            //Cytosol.Interior.Boundaries.Add(PlasmaMembrane.Interior, new Embedding());

            // gmk
            Alive = true;
            Cytokinetic = false;
            PlasmaMembrane = new Compartment(new TinySphere());
            Cytosol = new Compartment(new TinyBall());
            Embedding cellEmbed = new Embedding(PlasmaMembrane.Interior, Cytosol.Interior);
            Cytosol.Interior.Boundaries = new Dictionary<Manifold, Embedding>();
            Cytosol.Interior.Boundaries.Add(PlasmaMembrane.Interior,cellEmbed);

            Index = safeIndex++;
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
            Differentiator.Step(dt);
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

}
