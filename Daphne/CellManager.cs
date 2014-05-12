using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                double[] force = kvp.Key.Force;
                
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
        }

        private Compartment extracellularSpace;
        private CellManager cellManager;
        private Dictionary<int, Cell> cells;
    }
}
