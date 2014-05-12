using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

using Ninject;

using ManifoldRing;

namespace Daphne
{
    public struct SpatialState
    {
        public double[] X;
        public double[] V;
    }

    public class Cytosol : Attribute { }
    public class Membrane : Attribute { }

    /// <summary>
    /// The basic representation of a biological cell. 
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
        private bool Alive;
        /// <summary>
        /// a flag that signals that the cell is motile
        /// </summary>
        private bool isMotile = true;
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        private bool Cytokinetic;

        /// <summary>
        /// The radius of the cell
        /// </summary>
        private double radius;


        public Cell(double radius)
        {
            if (radius <= 0)
            {
                throw new Exception("Cell radius must be greater than zero.");
            }
            Alive = true;
            Cytokinetic = false;
            this.radius = radius;

            Index = safeIndex++;
        }

        [Inject]
        [Cytosol]
        public void InjectCytosol(Compartment c)
        {
            Cytosol = c;
            Cytosol.Interior.Initialize(new double[] { radius });
            if (PlasmaMembrane != null)
            {
                initBoundary();
            }
        }

        [Inject]
        [Membrane]
        public void InjectMembrane(Compartment c)
        {
            PlasmaMembrane = c;
            PlasmaMembrane.Interior.Initialize(new double[] { radius });
            if (Cytosol != null)
            {
                initBoundary();
            }
        }

        private void initBoundary()
        {
            // boundary and position
            Cytosol.Boundaries.Add(PlasmaMembrane.Interior.Id, PlasmaMembrane);
            Cytosol.BoundaryTransforms.Add(PlasmaMembrane.Interior.Id, new Transform(false));
        }

        public void setState(double[] pos, double[] vel)
        {
            state.X = pos;
            state.V = vel;
        }
        
        /// <summary>
        /// Drives the cell's dynamics through time-step dt. The dynamics is applied in-place: the
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
        public double[] Force(double dt, double[] position)
        {
            return Locomotor.Force(position);
           // get { return Locomotor.Force(dt, position); }
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

        public Locomotor Locomotor { get; set; }
        public Compartment Cytosol { get; private set; }
        public Compartment PlasmaMembrane { get; private set; }
        public Differentiator Differentiator { get; private set; }
        private SpatialState state;

        public int Index { get; private set; }
        private static int safeIndex = 0;

        public SpatialState State
        {
            get { return state; }
            set { state = value; }
        }

        public bool IsMotile
        {
            get { return isMotile; }
            set { isMotile = value; }
        }

        // There may be other components specific to a given cell type.
    }
}



