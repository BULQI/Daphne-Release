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
            List<int> removalList = new List<int>();

            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                // cell takes a step
                kvp.Value.Step(dt);

                if (kvp.Value.IsMotile == true)
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
                    if (kvp.Value.Alive == false)
                    {
                        removalList.Add(kvp.Value.Cell_id);
                    }
                }
            }

            // process removal list
            foreach (int key in removalList)
            {
                Simulation.dataBasket.RemoveCell(key);
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
