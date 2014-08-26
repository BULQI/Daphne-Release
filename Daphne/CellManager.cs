using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Daphne
{
    public class CellManager : IDynamic
    {
        Dictionary<int, double> deadDict = null;
        int[] tempDeadKeys = null;
        public double deathTimeConstant;
        public int deathOrder;

        public CellManager()
        {
            deadDict = new Dictionary<int, double>();
        }

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                // cell takes a step
                if (kvp.Value.Alive == true)
                {
                    kvp.Value.Step(dt);
                }

                // still alive and motile
                if (kvp.Value.Alive == true && kvp.Value.IsMotile == true && kvp.Value.Exiting == false)
                {
                    if (kvp.Value.IsChemotactic)
                    {
                        // For TinySphere cytosol, the force is determined by the gradient of the driver molecule at position (0,0,0).
                        // add the chemotactic force (accumulate it into the force variable)
                        kvp.Value.addForce(kvp.Value.Force(new double[3] { 0.0, 0.0, 0.0 }));
                    }
                    // apply the boundary force
                    kvp.Value.BoundaryForce();
                    // apply stochastic force
                    if (kvp.Value.IsStochastic)
                    {
                        kvp.Value.addForce(kvp.Value.StochLocomotor.Force(dt));
                    }

                    // A simple implementation of movement. For testing.
                    for (int i = 0; i < kvp.Value.SpatialState.X.Length; i++)
                    {
                        kvp.Value.SpatialState.X[i] += kvp.Value.SpatialState.V[i] * dt;
                        kvp.Value.SpatialState.V[i] += (-kvp.Value.DragCoefficient * kvp.Value.SpatialState.V[i] + kvp.Value.SpatialState.F[i]) * dt;
                    }

                    // enforce boundary condition
                    kvp.Value.EnforceBC();
                }

                // if the cell  moved out of bounds schedule its removal
                if (kvp.Value.Exiting == true)
                {
                    if (removalList == null)
                    {
                        removalList = new List<int>();
                    }
                    removalList.Add(kvp.Value.Cell_id);
                }

                // if the cell died schedule its (stochastic) removal
                if (kvp.Value.Alive == false) 
                {
                    if (!deadDict.ContainsKey(kvp.Value.Cell_id))
                    {
                        deadDict.Add(kvp.Value.Cell_id, 0);
                    }
                }
                
                // cell division
                if (kvp.Value.Cytokinetic == true)
                {
                    // divide the cell, return daughter
                    Cell c = kvp.Value.Divide();

                    if (daughterList == null)
                    {
                        daughterList = new List<Cell>();
                    }
                    daughterList.Add(c);

                    //got from release-dev....
                    //Simulation.dataBasket.ReportDaughterCell(kvp.Value.Cell_id, c);
                }
           }

            // process removal list
            if (removalList != null)
            {
                foreach (int key in removalList)
                {
                    Simulation.dataBasket.RemoveCell(key);
                }
            }

            // process death list
            if (deadDict != null)
            {
                tempDeadKeys = deadDict.Keys.ToArray<int>();
                foreach(int key in tempDeadKeys)
                {
                    // increment elapsed time since death
                    deadDict[key] = deadDict[key] + dt;
                    double u = Rand.TroschuetzCUD.NextDouble();
                    if (u < Math.Pow(deadDict[key] * deathTimeConstant, deathOrder))
                    {
                        Simulation.dataBasket.RemoveCell(key);
                        deadDict.Remove(key);
                    }
                }
            }

            // process daughter list
            if (daughterList != null)
            {
                foreach (Cell c in daughterList)
                {
                    // add the cell
                    Simulation.AddCell(c);
                    // add the cell's membrane to the ecs boundary
                    Simulation.dataBasket.ECS.AddBoundaryManifold(c.PlasmaMembrane.Interior);
                }
            }
        }

        /// <summary>
        /// zero all cell forces
        /// </summary>
        public void ResetCellForces()
        {
            foreach (Cell c in Simulation.dataBasket.Cells.Values)
            {
                c.resetForce();
            }
        }
    }
}
