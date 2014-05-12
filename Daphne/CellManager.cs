using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Daphne
{
    public class CellManager : IDynamic
    {
        public CellManager()
        {
        }

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                // cell takes a step
                kvp.Value.Step(dt);

                // still alive and motile
                if (kvp.Value.Alive == true && kvp.Value.IsMotile == true)
                {
                    // For TinySphere cytosol, the force is determined by the gradient of the driver molecule at position (0,0,0).
                    // add the chemotactic force (accumulate it into the force variable)
                    kvp.Value.addForce(kvp.Value.Force(new double[3] { 0.0, 0.0, 0.0 }));
                    // apply the boundary force
                    kvp.Value.BoundaryForce();

                    // A simple implementation of movement. For testing.
                    for (int i = 0; i < kvp.Value.State.X.Length; i++)
                    {
                        kvp.Value.State.X[i] += kvp.Value.State.V[i] * dt;
                        kvp.Value.State.V[i] += (-kvp.Value.DragCoefficient * kvp.Value.State.V[i] + kvp.Value.State.F[i]) * dt;
                    }

                    // enforce boundary condition
                    kvp.Value.EnforceBC();
                }

                // if the cell died (true death or it moved out of bounds) schedule its removal
                if (kvp.Value.Alive == false)
                {
                    if (removalList == null)
                    {
                        removalList = new List<int>();
                    }
                    removalList.Add(kvp.Value.Cell_id);
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
        
        public void WriteStates(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                int n = 0;
                foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
                {
                    writer.Write(n + "\t"
                                 + kvp.Value.State.X[0] + "\t" + kvp.Value.State.X[1] + "\t" + kvp.Value.State.X[2] + "\t"
                                 + kvp.Value.State.V[0] + "\t" + kvp.Value.State.V[1] + "\t" + kvp.Value.State.V[2]
                                 + "\n");

                    n++;
                }
            }
        }
    }
}
