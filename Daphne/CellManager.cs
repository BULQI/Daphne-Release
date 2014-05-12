﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Daphne
{
    public class CellManager
    {
        public CellManager()
        {
        }

        public void Step(double dt)
        {
            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                kvp.Value.Step(dt);

                if (kvp.Value.IsMotile == true)
                {
                    double[] force = kvp.Value.Force(dt, kvp.Value.State.X);

                    // A simple implementation of movement. For testing.
                    for (int i = 0; i < kvp.Value.State.X.Length; i++)
                    {
                        kvp.Value.State.X[i] += kvp.Value.State.V[i] * dt;
                        kvp.Value.State.V[i] += -1.0 * kvp.Value.State.V[i] + force[i] * dt;
                    }
                }
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
