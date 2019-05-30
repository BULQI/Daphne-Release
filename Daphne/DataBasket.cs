/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

//using DivisionLib;
//using LangProcLib;
using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;
using Gene = NativeDaphne.Nt_Gene;
namespace Daphne
{
    /// <summary>
    /// entity holding data used by the simulation and other components
    /// </summary>
    public class DataBasket
    {
        /// <summary>
        /// environment data object
        /// </summary>
        private EnvironmentBase environment;
        /// <summary>
        /// dictionary of cells
        /// </summary>
        private Dictionary<int, Cell> cells;
        /// <summary>
        /// cell populations
        /// </summary>
        private Dictionary<int, CellsPopulation> populations;
        /// <summary>
        /// dictionary of molecules
        /// </summary>
        private Dictionary<string, Molecule> molecules;
        /// <summary>
        /// dictionary of genes
        /// </summary>
        private Dictionary<string, Gene> genes;
        /// <summary>
        /// handle to the simulation
        /// </summary>
        private SimulationBase hSim;
        /// <summary>
        /// dictionary of raw cell track sets data
        /// </summary>
        private Dictionary<int, CellTrackData> trackData;
        // safe cell id
        public static int SafeCell_id = 0;

        /// <summary>
        /// generate a safe cell id, update safeCell_id when needed
        /// </summary>
        /// <param name="id">reference id</param>
        /// <returns>the id to use</returns>
        public static int GenerateSafeCellId(int id)
        {
            // the safe id must be larger than the largest one in use
            // if the cell id is legitimate (> 0), use it
            if (id > -1)
            {
                if (id >= DataBasket.SafeCell_id)
                {
                    DataBasket.SafeCell_id = id + 1;
                }
            }
            else
            {
                id = DataBasket.SafeCell_id++;
            }
            return id;
        }

        /// <summary>
        /// constructor
        /// </summary>
        public DataBasket(SimulationBase s)
        {
            hSim = s;
            cells = new Dictionary<int,Cell>();
            populations = new Dictionary<int, CellsPopulation>();
            molecules = new Dictionary<string, Molecule>();
            genes = new Dictionary<string, Gene>();
            ResetTrackData();
        }

        /// <summary>
        /// clear the dictionaries
        /// </summary>
        public void Clear()
        {
            cells.Clear();
            SafeCell_id = 0;
            populations.Clear();
            molecules.Clear();
            genes.Clear();
        }

        /// <summary>
        /// accessor for the environment
        /// </summary>
        public EnvironmentBase Environment
        {
            get { return environment; }
            set { environment = value; }
        }

        /// <summary>
        /// accessor for the dictionary of cells
        /// </summary>
        public Dictionary<int, Cell> Cells
        {
            get { return cells; }
            set { cells = value; }
        }

        /// <summary>
        /// accessor for the populations
        /// </summary>
        public Dictionary<int, CellsPopulation> Populations
        {
            get { return populations; }
        }

        /// <summary>
        /// accessor for the dictionary of molecules
        /// </summary>
        public Dictionary<string, Molecule> Molecules
        {
            get { return molecules; }
            set { molecules = value; }
        }

        /// <summary>
        /// accessor for the dictionary of genes
        /// </summary>
        public Dictionary<string, Gene> Genes
        {
            get { return genes; }
            set { genes = value; }
        }

        /// <summary>
        /// accessor for the dictionary of cell tracks
        /// </summary>
        public Dictionary<int, CellTrackData> TrackData
        {
            get { return trackData; }
        }

        /// <summary>
        /// reset and clear all track data
        /// </summary>
        public void ResetTrackData()
        {
            if (trackData == null)
            {
                trackData = new Dictionary<int, CellTrackData>();
            }
            else
            {
                trackData.Clear();
            }
        }

        /// <summary>
        /// add a cell population
        /// </summary>
        /// <param name="id">population id</param>
        /// <returns>true if it was added, false if it existed already</returns>
        public bool AddPopulation(int id)
        {
            if (populations.ContainsKey(id) == false)
            {
                populations.Add(id, new CellsPopulation(id));
                return true;
            }
            return false;
        }

        /// <summary>
        /// add a given cell
        /// </summary>
        /// <param name="cell">the cell to add</param>
        /// <returns>true for success</returns>
        public bool AddCell(Cell cell)
        {
            if (cell != null)
            {
                // cause the cell to be updated into the grid in the next simulation round
                cell.GridIndex[0] = cell.GridIndex[1] = cell.GridIndex[2] = -1;
                // add the cell
                cells.Add(cell.Cell_id, cell);

                //add the cell to the population, which have a middle layer instance
                populations[cell.Population_id].AddCell(cell.Cell_id, cell);

                //the is for global access in the middle layer, this is needed since the
                //middle layer does not have access to the databasket.
                CellManager.cellDictionary.Add(cell.Cell_id, cell);
                return true;
            }
            return false;
        }


        /// <summary>
        /// remove a cell.
        /// when a cell is dead but before removed, they don't participate in chemistry, but they still
        /// particiapte in collision, the complete removal is set to false to indicate that.
        /// </summary>
        /// <param name="key">Cell_id</param>
        /// <param name="complete_removal">false if removing chemistry only</param>
        /// <returns>false if cel l not found in the system, otherwise true</returns>
        public bool RemoveCell(int key, bool complete_removal = true)
        {

            if (cells.ContainsKey(key) == true)
            {
                Cell cell = cells[key];

                Populations[cell.Population_id].RemoveCell(cell.Cell_id, complete_removal);
                //remove chemistry if exists
                if (Environment.Comp.Boundaries.ContainsKey(cell.PlasmaMembrane.Interior.Id) == true)
                {
                    hSim.RemoveCell(cell);
                }

                if (complete_removal == true)
                {
                    // remove all pairs that contain this cell
                    hSim.CollisionManager.RemoveAllPairsContainingCell(cell);
                    // remove the cell from the grid
                    hSim.CollisionManager.RemoveCellFromGrid(cell);
                    CellManager.cellDictionary.Remove(cell.Cell_id);

                    Cells.Remove(cell.Cell_id);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// rekey the cell and its pairs and grid locations containing it
        /// </summary>
        /// <param name="oldKey">the cell's old key</param>
        /// <returns>true if the cell was successfully rekeyed</returns>
        public bool RekeyCell(int oldKey)
        {
            if (cells.ContainsKey(oldKey) == true)
            {
                Cell cell = cells[oldKey];

                // rekey all pairs that contain this cell
                hSim.CollisionManager.RekeyAllPairsContainingCell(cell, oldKey);
                // rekey the cell in the grid
                hSim.CollisionManager.RekeyCellInGrid(cell, oldKey);
                // add the new key in the population
                populations[cell.Population_id].AddCell(cells[oldKey].Cell_id, cell);
                // remove the old key from the population
                populations[cell.Population_id].RemoveCell(oldKey);
                // add the new key
                cells.Add(cells[oldKey].Cell_id, cell);
                // remove the old key
                cells.Remove(oldKey);
                return true;
            }
            return false;
        }

        /// <summary>
        /// update all cells given a list of db rows
        /// </summary>
        /// <param name="list">the frame data</param>
        public void UpdateCells(TissueSimulationFrameData frame)
        {
            List<int> removalList = new List<int>();
            CellState state = new CellState();
            Cell cell;

            foreach (int key in cells.Keys)
            {
                removalList.Add(key);
            }

            for (int i = 0; i < frame.CellCount; i++)
            {
                frame.applyStateByIndex(i, ref state);

                // take off the removal list
                removalList.Remove(state.Cell_id);

                // if the cell doesn't exist, create it
                if (cells.ContainsKey(state.Cell_id) == false)
                {
                    ConfigCompartment[] configComp = new ConfigCompartment[2];
                    List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
                    List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
                    List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();
                    int cellPopId = frame.CellPopIDs[i];

                    if (((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict.ContainsKey(cellPopId) == false)
                    {
                        throw new Exception("Cell population id invalid.");
                    }

                    CellPopulation cp = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict[cellPopId];

                    // create the cell
                    hSim.prepareCellInstantiation(cp, configComp, bulk_reacs, ref boundary_reacs, ref transcription_reacs);
                    cell = hSim.instantiateCell(cp, configComp, bulk_reacs, boundary_reacs, transcription_reacs, false);
                }
                else
                {
                    cell = cells[state.Cell_id];
                }
                // now apply the state
                cell.SetCellState(state);
                // add the cell
                if (cells.ContainsKey(state.Cell_id) == false)
                {
                    SimulationBase.AddCell(cell);
                }
            }

            // remove cells
            foreach (int key in removalList)
            {
                RemoveCell(key);
            }
        }

        /// <summary>
        /// update all ecs molpop concentrations
        /// </summary>
        /// <param name="frame">the frame object containing the data</param>
        public void UpdateECSMolpops(TissueSimulationFrameData frame)
        {
            int i = 0;

            foreach (ConfigMolecularPopulation cmp in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
            {
                MolecularPopulation cur_mp = SimulationBase.dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid];

                cur_mp.Initialize("explicit", frame.ECSMolPops[i]);
                i++;
            }
        }

        public void DeathEvent(int key)
        {
            if (cells.ContainsKey(key) == true)
            {
                Cell cell = cells[key];

                hSim.Reporter.AppendDeathEvent(cell);
            }
        }

        public void DivisionEvent(Cell mother, Cell daughter)
        {
            hSim.Reporter.AppendDivisionEvent(mother, daughter);
        }

        public void ExitEvent(int key)
        {
            if (cells.ContainsKey(key) == true)
            {
                Cell cell = cells[key];

                hSim.Reporter.AppendExitEvent(cell);
            }
        }
    }

}
