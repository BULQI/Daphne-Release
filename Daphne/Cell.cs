using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

using MathNet.Numerics.LinearAlgebra;

using Ninject;

using ManifoldRing;

namespace Daphne
{
    public struct SpatialState
    {
        public double[] X;
        public double[] V;
        public double[] F;

        public static int Dim = 9;
    }

    public class Cytosol : Attribute { }
    public class Membrane : Attribute { }

    /// <summary>
    /// The basic representation of a biological cell. 
    /// </summary>
    public class Cell : IDynamic
    {
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
        //private bool Alive;
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        //private bool Cytokinetic;
        /// <summary>
        /// a flag that signals that the cell is motile
        /// </summary>
        private bool isMotile = true;

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
            //Alive = true;
            //Cytokinetic = false;
            this.radius = radius;

            Cell_id = SafeCell_id++;
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

        public void setState(double[] s)
        {
            if(s.Length != SpatialState.Dim)
            {
                throw new Exception("Cell state length implausible.");
            }
            state.X = new double[] { s[0], s[1], s[2] };
            state.V = new double[] { s[3], s[4], s[5] };
            state.F = new double[] { s[6], s[7], s[8] };
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
        public double[] Force(double[] position)
        {
            return Locomotor.Force(position);
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
        public double DragCoefficient { get; set; }

        public int Cell_id { get; private set; }
        public static int SafeCell_id = 0;
        public int Population_id { get; set; }
        protected int[] gridIndex = { -1, -1, -1 };
        public static double defaultRadius = 5.0;

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

        public double Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// retrieve the cell's grid index
        /// </summary>
        public int[] GridIndex
        {
            get { return gridIndex; }
        }

        /// <summary>
        /// set force to zero
        /// </summary>
        public void resetForce()
        {
            state.F[0] = state.F[1] = state.F[2] = 0;
        }

        /// <summary>
        /// accumulate the force vector
        /// </summary>
        /// <param name="f"></param>
        public void addForce(double[] f)
        {
            state.F[0] += f[0];
            state.F[1] += f[1];
            state.F[2] += f[2];
        }

        /// <summary>
        /// calculate the boundary force when approaching the environment walls
        /// </summary>
        /// <param name="normal">direction of the force</param>
        /// <param name="dist">distance of the cell to the wall</param>
        private void applyBoundaryForce(Vector normal, double dist)
        {
            if (dist != 0.0)
            {
                double force = Pair.Phi1 * (1.0 / dist - 1.0 / radius);

                addForce(normal * force);
            }
        }

        /// <summary>
        /// boundary forces
        /// </summary>
        public void BoundaryForce()
        {
            // boundary force
            if (Simulation.dataBasket.ECS.toroidal == false)
            {
                double dist = 0.0;

                // X
                // left
                if ((dist = State.X[0]) < radius)
                {
                    applyBoundaryForce(new double[] { 1, 0, 0 }, dist);
                }
                // right
                else if ((dist = Simulation.dataBasket.ECS.Space.Interior.Extent(0) - State.X[0]) < radius)
                {
                    applyBoundaryForce(new double[] { -1, 0, 0 }, dist);
                }

                // Y
                // bottom
                if ((dist = State.X[1]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 1, 0 }, dist);
                }
                // top
                else if ((dist = Simulation.dataBasket.ECS.Space.Interior.Extent(1) - State.X[1]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, -1, 0 }, dist);
                }

                // Z
                // far
                if ((dist = State.X[2]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 0, 1 }, dist);
                }
                // near
                else if ((dist = Simulation.dataBasket.ECS.Space.Interior.Extent(2) - State.X[2]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 0, -1 }, dist);
                }
            }
        }

        /// <summary>
        /// toroidal boundary condition
        /// </summary>
        public void ToroidalBC()
        {
            // toroidal boundary conditions, wrap around
            // NOTE: this assumes the environment has a lower bound of (0, 0, 0)
            if (Simulation.dataBasket.ECS.toroidal == true)
            {
                for (int i = 0; i < Simulation.dataBasket.ECS.Space.Interior.Dim; i++)
                {
                    double safetySlab = 1e-3;

                    // displace the cell such that it wraps around
                    if (State.X[i] < 0.0)
                    {
                        // use a small fudge factor to displace the cell just back into the grid
                        State.X[i] = Simulation.dataBasket.ECS.Space.Interior.Extent(i) - safetySlab;
                    }
                    else if (State.X[i] > Simulation.dataBasket.ECS.Space.Interior.Extent(i))
                    {
                        State.X[i] = 0.0;
                    }
                }
            }
        }
    }
}



