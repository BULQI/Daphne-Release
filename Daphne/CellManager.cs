using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Daphne
{
    public struct SpatialState
    {
        public double[] X;
        public double[] V;
    }

    public class CellManager
    {
        private Dictionary<Cell, SpatialState> spatialStates;

        public CellManager()
        {
            spatialStates = new Dictionary<Cell, SpatialState>();
        }

        public void AddState(Cell c, SpatialState s)
        {
            spatialStates.Add(c, s);
        }

        public Dictionary<Cell, SpatialState> States
        {
            get { return spatialStates; }
        }

        public void Step(double dt)
        {
            foreach (KeyValuePair<Cell, SpatialState> kvp in spatialStates)
            {
                kvp.Key.Step(dt);

                if (kvp.Key.IsMotile == true)
                {
                    double[] force = kvp.Key.Force(dt, kvp.Value.X);

                    // A simple implementation of movement. For testing.
                    for (int i = 0; i < kvp.Value.X.Length; i++)
                    {
                        kvp.Value.X[i] += kvp.Value.V[i] * dt;
                        kvp.Value.V[i] += -1.0 * kvp.Value.V[i] + force[i] * dt;
                    }
                }
            }
        }

        public void WriteStates(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                int n = 0;
                foreach (KeyValuePair<Cell, SpatialState> kvp in spatialStates)
                {
                    writer.Write(n + "\t"
                        + kvp.Value.X[0] + "\t" + kvp.Value.X[1] + "\t" + kvp.Value.X[2] + "\t"
                        + kvp.Value.V[0] + "\t" + kvp.Value.V[1] + "\t" + kvp.Value.V[2]
                        + "\n");

                    n++;

                }
            }

        }
    }

    public class Simulation
    {
        public Simulation()
        {
            cellManager = new CellManager();
            cells = new Dictionary<int, Cell>();
        }

        public void AddCell(double[] pos, double[] vel)
        {
            Cell c = new Cell();
            SpatialState s = new SpatialState();

            s.X = pos;
            s.V = vel;
            cellManager.AddState(c, s);
            cells.Add(c.Index, c);
        }

        public void AddCell(double[] pos, double[] vel, double radius)
        {
            Cell c = new Cell(radius);
            SpatialState s = new SpatialState();

            s.X = pos;
            s.V = vel;
            cellManager.AddState(c, s);
            cells.Add(c.Index, c);
        }

        public Compartment ECS
        {
            get { return extracellularSpace; }
            set { extracellularSpace = value; }
        }

        public CellManager CMGR
        {
            get { return cellManager; }
        }

        public Dictionary<int, Cell> Cells
        {
            get { return cells; }
            set { cells = value; }
        }

        private Compartment extracellularSpace;
        private CellManager cellManager;
        private Dictionary<int, Cell> cells;
    }
}
