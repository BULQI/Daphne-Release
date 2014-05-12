using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    struct SpatialState
    {
        public double[] X;
        public double[] V;
    }

    public class CellManager
    {
        Dictionary<Cell, SpatialState> spatialStates;

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
            CellManager = new CellManager();
        }
        public Compartment ExtracellularSpace;
        public CellManager CellManager;
        public Dictionary<int, Cell> cells;
    }
}
