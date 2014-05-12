using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ManifoldRing;


namespace Daphne
{
    public class CellManager
    {
        public CellManager()
        {
        }

        public void Step(double dt)
        {
            foreach (KeyValuePair<int, Cell> kvp in Sim.Cells)
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
                foreach (KeyValuePair<int, Cell> kvp in Sim.Cells)
                {
                    writer.Write(n + "\t"
                        + kvp.Value.State.X[0] + "\t" + kvp.Value.State.X[1] + "\t" + kvp.Value.State.X[2] + "\t"
                        + kvp.Value.State.V[0] + "\t" + kvp.Value.State.V[1] + "\t" + kvp.Value.State.V[2]
                        + "\n");

                    n++;
                }
            }
        }

        public Simulation Sim { get; set; }
    }

    public class Simulation
    {
        public Simulation()
        {
            cellManager = new CellManager();
            cellManager.Sim = this;
            cells = new Dictionary<int, Cell>();
        }

        public void AddCell(Cell c)
        {
            cells.Add(c.Index, c);
            // add the cell membrane to the ecs
            if (extracellularSpace == null)
            {
                throw new Exception("Need to create the ECS before adding cells.");
            }

            // no cell rotation currently
            Transform t = new Transform(false);

            extracellularSpace.Space.Boundaries.Add(c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);
            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.State.X);
            extracellularSpace.Space.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        public void CreateECS(Manifold m)
        {
            extracellularSpace = new ExtraCellularSpace(m);
        }

        public ExtraCellularSpace ECS
        {
            get { return extracellularSpace; }
            //set { extracellularSpace = value; }
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

        private ExtraCellularSpace extracellularSpace;
        private CellManager cellManager;
        private Dictionary<int, Cell> cells;
    }
}
