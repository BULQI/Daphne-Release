using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Daphne
{
    public class CellManager : IDynamic
    {
        private Dictionary<int, double[]> deadDict = null;
        private int[] tempDeadKeys = null;
        public DistributedParameter Phagocytosis;

        public CellManager()
        {
            deadDict = new Dictionary<int, double[]>();
        }

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                // cell takes a step
                if (kvp.Value.Alive == true)
                {
                    kvp.Value.Step(dt);
                }

                // motile cells
                if (kvp.Value.IsMotile == true && kvp.Value.Exiting == false)
                {
                    if (kvp.Value.IsChemotactic && kvp.Value.Alive == true)
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
                        // start clock at 0 and sample the distribution for the time of removal
                        deadDict.Add(kvp.Value.Cell_id, new double[] {0.0, Phagocytosis.Sample()});
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

                    SimulationBase.dataBasket.DivisionEvent(kvp.Value.Cell_id, kvp.Value.Population_id, c.Cell_id);
                }
           }

            // process removal list
            if (removalList != null)
            {
                foreach (int key in removalList)
                {
                    SimulationBase.dataBasket.ExitEvent(key);
                    SimulationBase.dataBasket.RemoveCell(key);
                }
            }

            // process death list
            if (deadDict != null)
            {
                tempDeadKeys = deadDict.Keys.ToArray<int>();
                foreach(int key in tempDeadKeys)
                {
                    // increment elapsed time since death
                    double[] d = deadDict[key];
                    d[0] += dt;

                    if (d[0] >= d[1])
                    {
                        SimulationBase.dataBasket.DeathEvent(key);
                        SimulationBase.dataBasket.RemoveCell(key);
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
                    SimulationBase.AddCell(c);
                    // cell's membrane was added to the ecs in Cell.Divide()
                }
            }
        }

        /// <summary>
        /// zero all cell forces
        /// </summary>
        public void ResetCellForces()
        {
            foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
            {
                c.resetForce();
            }
        }
    }
}
