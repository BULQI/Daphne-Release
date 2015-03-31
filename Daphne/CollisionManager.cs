//#define ALL_COLLISIONS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NativeDaphne;

using MathNet.Numerics.LinearAlgebra.Double;

namespace Daphne
{
    /// <summary>
    /// the cell manager class handles collisions and general management of cell motion
    /// </summary>
    public class CollisionManager : Grid, IDynamic
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="gridSize">3D grid size in microns</param>
        /// <param name="gridStep">voxel width in microns</param>
        public CollisionManager(Vector gridSize, double gridStep) : base(gridSize, gridStep)
        {
            grid = new Dictionary<int, Cell>[gridPts[0], gridPts[1], gridPts[2]];

            bool toroidal = false;
            if (SimulationBase.dataBasket.Environment is ECSEnvironment)
            {
                toroidal = (SimulationBase.dataBasket.Environment as ECSEnvironment).toroidal;
            }

            nt_collisionManager = new Nt_CollisionManager(gridSize.ToArray(), gridStep, toroidal);
        }

        public void Step(double dt)
        {
            //update(dt);

            nt_collisionManager.Step(dt);
        }

        private bool clearSeparation(Pair p)
        {
             int maxSep = (int)Math.Ceiling((p.Cell(0).Radius + p.Cell(1).Radius) / gridStep);

            int dx, dy, dz;

            // correction for periodic boundary conditions
            if (SimulationBase.dataBasket.Environment is ECSEnvironment && (SimulationBase.dataBasket.Environment as ECSEnvironment).toroidal == true)
            {
                dx = p.GridIndex_dx;
                if (dx > 0.5 * gridPts[0])
                {
                    dx = gridPts[0] - dx;
                }
                if (dx > maxSep) return true;
                dy = p.GridIndex_dy;
                if (dy > 0.5 * gridPts[1])
                {
                    dy = gridPts[1] - dy;
                }
                if (dy > maxSep) return true;
                dz = p.GridIndex_dz;
                if (dz > 0.5 * gridPts[2])
                {
                    dz = gridPts[2] - dz;
                }
                return dz > maxSep;
            }
            //dx = Math.Abs(p.Cell(0).GridIndex[0] - p.Cell(1).GridIndex[0]);

            return (p.GridIndex_dx > maxSep || p.GridIndex_dy > maxSep || p.GridIndex_dz > maxSep);

            // the separation distance in units of voxels
            //return Math.Max(Math.Max(dx, dy), dz) > maxSep;
        }

        // think high and low byte but using integer logic
        private int pairHash(int idx1, int idx2)
        {
            return Math.Max(idx1, idx2) * pairKeyMultiplier + Math.Min(idx1, idx2);
        }

        // find a neighbor index of a grid tile; return -1 for illegal index
        private int[] neighbor(int[] current, int dx, int dy, int dz)
        {
            int[] idx = new int[] { current[0], current[1], current[2] };

            idx[0] += dx;
            idx[1] += dy;
            idx[2] += dz;

            if (legalIndex(idx) == false)
            {
                // correction for periodic boundary conditions
                if (SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == true)
                {
                    if (idx[0] < 0 || idx[0] >= gridPts[0])
                    {
                        idx[0] %= gridPts[0];
                        if (idx[0] < 0)
                        {
                            idx[0] += gridPts[0];
                        }
                    }
                    if (idx[1] < 0 || idx[1] >= gridPts[1])
                    {
                        idx[1] %= gridPts[1];
                        if (idx[1] < 0)
                        {
                            idx[1] += gridPts[1];
                        }
                    }
                    if (idx[2] < 0 || idx[2] >= gridPts[2])
                    {
                        idx[2] %= gridPts[2];
                        if (idx[2] < 0)
                        {
                            idx[2] += gridPts[2];
                        }
                    }
                }
                else
                {
                    idx[0] = idx[1] = idx[2] = -1;
                }
            }
            return idx;
        }

        /// <summary>
        /// remove all pairs containing a cell
        /// </summary>
        /// <param name="del">cell to be deleted</param>
        public void RemoveAllPairsContainingCell(Cell del)
        {
            if (pairs != null && pairs.Count > 0 && del != null)
            {
                foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
                {
                    // no self-pairing
                    if (del.Cell_id == kvp.Value.Cell_id)
                    {
                        continue;
                    }

                    int hash = pairHash(del.Cell_id, kvp.Value.Cell_id);

                    // remove the pair; will only act if the pair exists
                    if (pairs.Remove(hash))
                    {
                        //Console.WriteLine("removal of pair " + del.Index + " " + kvp.Value.Index);
                    }
                }
            }
        }

        /// <summary>
        /// rekey pairs containing a cell
        /// </summary>
        /// <param name="cell">the changed cell with the new key</param>
        /// <param name="oldKey">the old cell key</param>
        public void RekeyAllPairsContainingCell(Cell cell, int oldKey)
        {
            if (pairs != null && pairs.Count > 0 && cell != null)
            {
                foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
                {
                    // no self-pairing
                    if (oldKey == kvp.Value.Cell_id)
                    {
                        continue;
                    }

                    int hash = pairHash(oldKey, kvp.Value.Cell_id);

                    // remove the pair; will only act if the pair exists
                    if (pairs.ContainsKey(hash) == true)
                    {
                        // insert with new key
                        pairs.Add(pairHash(cell.Cell_id, kvp.Value.Cell_id), pairs[hash]);
                        // remove old key
                        pairs.Remove(hash);
                        //Console.WriteLine("rekeying of pair " + oldKey + " " + kvp.Value.Index);
                    }
                }
            }
        }

        /// <summary>
        /// remove a cell from the grid
        /// </summary>
        /// <param name="del">the cell to be removed</param>
        public void RemoveCellFromGrid(Cell del)
        {
            // NOTE: if FDCs start to move, die, divide, we'll have to account for that here
            if (legalIndex(del.GridIndex) == true && grid[del.GridIndex[0], del.GridIndex[1], del.GridIndex[2]] != null)
            {
                grid[del.GridIndex[0], del.GridIndex[1], del.GridIndex[2]].Remove(del.Cell_id);
            }
        }

        /// <summary>
        /// rekey a cell in the grid
        /// </summary>
        /// <param name="cell">the cell to be rekeyed with its new key</param>
        /// <param name="oldKey">the cell's old key</param>
        public void RekeyCellInGrid(Cell cell, int oldKey)
        {
            // NOTE: if FDCs start to move, die, divide, we'll have to account for that here
            if (legalIndex(cell.GridIndex) == true && grid[cell.GridIndex[0], cell.GridIndex[1], cell.GridIndex[2]] != null)
            {
                grid[cell.GridIndex[0], cell.GridIndex[1], cell.GridIndex[2]].Add(cell.Cell_id, cell);
                grid[cell.GridIndex[0], cell.GridIndex[1], cell.GridIndex[2]].Remove(oldKey);
            }
        }

        /// <summary>
        /// multiplier to calculate the pair hash key
        /// </summary>
        /// <returns></returns>
        private int multiplier()
        {
            return (int)Math.Pow(10, Math.Round(0.5 + Math.Log10(Cell.SafeCell_id)));
        }

        /// <summary>
        /// recalculates and updates the distance for existing pairs
        /// </summary>
        private void updateExistingPairs()
        {
            if (pairs != null)
            {
                foreach (KeyValuePair<int, Pair> kvp in pairs)
                {
                    // recalculate the distance for pairs
                    //kvp.Value.distance(gridSize);
                    kvp.Value.distance(gridSize.ToArray());
                }
            }
        }

        /// <summary>
        /// this version of the function avoids excessive loops
        /// </summary>
        private void updateGridAndPairs()
        {
            List<Cell> criticalCells = null;
            List<int> removalPairKeys = null;

            double[] gridSizeArr = gridSize.ToArray();
            // create the pairs dictionary
            if (pairs == null)
            {
                pairs = new Dictionary<int, Pair>();
                pairKeyMultiplier = multiplier();
                fastMultiplierDecide = Cell.SafeCell_id;
            }
            else
            {
                // update the multiplier if needed
                if (Cell.SafeCell_id > fastMultiplierDecide)
                {
                    int tmp = multiplier();

                    fastMultiplierDecide = Cell.SafeCell_id;
                    if (tmp > pairKeyMultiplier)
                    {
                        pairKeyMultiplier = tmp;
                    }
                }

                foreach (KeyValuePair<int, Pair> kvp in pairs)
                {
                    if (kvp.Value.isCriticalPair() == false && clearSeparation(kvp.Value) == true || legalIndex(kvp.Value.Cell(0).GridIndex) == false || legalIndex(kvp.Value.Cell(1).GridIndex) == false)
                    {
                        if (removalPairKeys == null)
                        {
                            removalPairKeys = new List<int>();
                        }
                        removalPairKeys.Add(kvp.Key);
                    }
                    else
                    {
                        // recalculate the distance for pairs that stay
                        //kvp.Value.distance(gridSize);
                        kvp.Value.distance(gridSizeArr);
                    }
                }

                if (removalPairKeys != null)
                {
                    foreach (int key in removalPairKeys)
                    {
                        pairs.Remove(key);
                    }
                }
            }

            int[] idx = new int[3];
            // look at all cells to see if they changed in the grid
            foreach (KeyValuePair<int, Cell> kvpc in SimulationBase.dataBasket.Cells)
            {
                //int[] idx = findGridIndex(kvpc.Value.SpatialState.X);
                findGridIndex(kvpc.Value.SpatialState.X, ref idx);

                // if the grid index changed we have to:
                // -put it in its new one
                // -find new pairs
                if (idx[0] != kvpc.Value.GridIndex[0] || idx[1] != kvpc.Value.GridIndex[1] || idx[2] != kvpc.Value.GridIndex[2])
                {
                    // was inserted before? we have to remove it
                    // NOTE: if fdcs were to start moving we would need special case handling for that here
                    // where the whole array of voxels that had an fdc gets cleared
                    if (legalIndex(kvpc.Value.GridIndex) == true && grid[kvpc.Value.GridIndex[0], kvpc.Value.GridIndex[1], kvpc.Value.GridIndex[2]] != null)
                    {
                        grid[kvpc.Value.GridIndex[0], kvpc.Value.GridIndex[1], kvpc.Value.GridIndex[2]].Remove(kvpc.Value.Cell_id);
                    }

                    // have the cell remember its position in the grid
                    kvpc.Value.GridIndex[0] = idx[0];
                    kvpc.Value.GridIndex[1] = idx[1];
                    kvpc.Value.GridIndex[2] = idx[2];

#if ALL_COLLISIONS
                    // special case handling fdcs: cover an array of cells and do not consider in the critical list; this assumes fdcs never move, appear, or die
                    // NOTE: if they start to move or divide then we need to add them to the critical list (or find a different solution that takes care of it)
                    if (kvpc.Value.isBaseType(CellBaseTypeLabel.FDC) == true && legalIndex(idx) == true)
                    {
                        int span = (int)Math.Ceiling(kvpc.Value.Radius / gridStep);

                        for (int i = -span; i <= span; i++)
                        {
                            for (int j = -span; j <= span; j++)
                            {
                                for (int k = -span; k <= span; k++)
                                {
                                    int[] test = neighbor(kvpc.Value.GridIndex, i, j, k);

                                    if (legalIndex(test) == true)
                                    {
                                        if (grid[test[0], test[1], test[2]] == null)
                                        {
                                            grid[test[0], test[1], test[2]] = new Dictionary<int, Cell>();
                                        }
                                        // use the cell's list index as key
                                        grid[test[0], test[1], test[2]].Add(kvpc.Value.Index, kvpc.Value);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // insert into grid and determine critical cells
                        if (legalIndex(idx) == true)
                        {
                            if (grid[idx[0], idx[1], idx[2]] == null)
                            {
                                grid[idx[0], idx[1], idx[2]] = new Dictionary<int, Cell>();
                            }
                            // use the cell's list index as key
                            grid[idx[0], idx[1], idx[2]].Add(kvpc.Value.Index, kvpc.Value);

                            // schedule to find any new pairs
                            if (criticalCells == null)
                            {
                                criticalCells = new List<Cell>();
                            }
                            criticalCells.Add(kvpc.Value);
                        }
                    }
#else
                    // insert into grid and determine critical cells
                    if (legalIndex(idx) == true)
                    {
                        if (grid[idx[0], idx[1], idx[2]] == null)
                        {
                            grid[idx[0], idx[1], idx[2]] = new Dictionary<int, Cell>();
                        }
                        // use the cell's list index as key
                        grid[idx[0], idx[1], idx[2]].Add(kvpc.Value.Cell_id, kvpc.Value);

                        // schedule to find any new pairs
                        if (criticalCells == null)
                        {
                            criticalCells = new List<Cell>();
                        }
                        criticalCells.Add(kvpc.Value);
                    }
#endif
                }
            }

            // now find the new pairs
            if (criticalCells != null)
            {
                foreach (Cell cell in criticalCells)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                int[] test = neighbor(cell.GridIndex, i, j, k);

                                // don't go outside the grid
                                if (legalIndex(test) == true && grid[test[0], test[1], test[2]] != null)
                                {
                                    // add all pairs in the test grid location
                                    foreach (KeyValuePair<int, Cell> kvpg in grid[test[0], test[1], test[2]])
                                    {
                                        // do not allow self-collisions
                                        if (cell == kvpg.Value)
                                        {
                                            continue;
                                        }

                                        int hash = pairHash(cell.Cell_id, kvpg.Value.Cell_id);

                                        // not already inserted
                                        if (pairs.ContainsKey(hash) == false)
                                        {
                                            // create the pair
                                            Pair p;
#if ALL_COLLISIONS
                                            BCell b = null;
                                            FDC fdc = null;
                                            TCell tc = null;

                                            // NOTE: we may need different pair types also; right now only b-cells (centrocytes) form a pair with fdcs
                                            if (cell.isBaseType(CellBaseTypeLabel.FDC) == true && kvpg.Value.isBaseType(CellBaseTypeLabel.BCell) == true)
                                            {
                                                b   = (BCell)kvpg.Value;
                                                fdc = (FDC)cell;
                                            }
                                            else if (kvpg.Value.isBaseType(CellBaseTypeLabel.FDC) == true && cell.isBaseType(CellBaseTypeLabel.BCell) == true)
                                            {
                                                b   = (BCell)cell;
                                                fdc = (FDC)kvpg.Value;
                                            }
                                            else if (cell.isBaseType(CellBaseTypeLabel.TCell) == true && kvpg.Value.isBaseType(CellBaseTypeLabel.BCell) == true)
                                            {
                                                b = (BCell)kvpg.Value;
                                                tc = (TCell)cell;
                                            }
                                            else if (kvpg.Value.isBaseType(CellBaseTypeLabel.TCell) == true && cell.isBaseType(CellBaseTypeLabel.BCell) == true)
                                            {
                                                b = (BCell)cell;
                                                tc = (TCell)kvpg.Value;
                                            }

                                            // is there an fdc?
                                            if(fdc != null)
                                            {
                                                // ignore collisions of FDCs with anything but centrocytes
                                                if (b != null && b.Phenotype == BCellPhenotype.Centrocyte)
                                                {
                                                    p = new FDCPair(cell, kvpg.Value);
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            // handle t-centrocyte special case
                                            else if (tc != null && b != null && b.Phenotype == BCellPhenotype.Centrocyte)
                                            {
                                                p = new TCentrocytePair(cell, kvpg.Value);
                                            }
                                            else
                                            {
                                                p = new MotilePair(cell, kvpg.Value);
                                            }
#else
                                            
                                            p = new CellPair(cell, kvpg.Value);
#endif

                                            // calculate the distance
                                            //p.distance(gridSize);
                                            p.distance(gridSizeArr);
                                            // insert the pair
                                            pairs.Add(hash, p);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void pairInteractions(double dt)
        {
            // compute interaction forces for all pairs and apply to the cells in the pairs (accumulate)
            foreach (KeyValuePair<int, Pair> kvp in pairs)
            {
                kvp.Value.pairInteract(dt);
            }
        }
#if ALL_COLLISIONS
        private void pairInteractionsIntermediateRK(double dt)
        {
            foreach (KeyValuePair<int, Pair> kvp in pairs)
            {
                kvp.Value.pairInteractIntermediateRK(dt);
            }
        }
#endif
        /// <summary>
        /// update and apply the grid state, pairs, and forces
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        private void update(double dt)
        {
            // update cell locations in the grid tiles and update pairs
            updateGridAndPairs();
            // handle all pairs and find the forces
            pairInteractions(dt);
        }
#if ALL_COLLISIONS
        /// <summary>
        /// update the existing pairs in the grid only
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public void updateIntermediateRK(double dt)
        {
            updateExistingPairs();
            pairInteractionsIntermediateRK(dt);
        }
#endif
        private Dictionary<int, Pair> pairs;
        private int pairKeyMultiplier, fastMultiplierDecide;
        private Dictionary<int, Cell>[, ,] grid;
        object remove_key_pair_lock = new object();

        Nt_CollisionManager nt_collisionManager;
    }
}
