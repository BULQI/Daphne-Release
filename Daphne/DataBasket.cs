using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

//using DivisionLib;
//using LangProcLib;
using MathNet.Numerics.LinearAlgebra;
//using Meta.Numerics.Matrices;

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
        private Dictionary<int, Dictionary<int, Cell>> populations;
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

        /// <summary>
        /// constructor
        /// </summary>
        public DataBasket(SimulationBase s)
        {
            hSim = s;
            cells = new Dictionary<int,Cell>();
            populations = new Dictionary<int, Dictionary<int, Cell>>();
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
            Cell.SafeCell_id = 0;
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
        public Dictionary<int, Dictionary<int, Cell>> Populations
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
                populations.Add(id, new Dictionary<int, Cell>());
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
                // add it to the population
                populations[cell.Population_id].Add(cell.Cell_id, cell);
                return true;
            }
            return false;
        }

        /// <summary>
        /// remove a cell
        /// </summary>
        /// <param name="key">the cell's key</param>
        /// <returns>true if the cell was successfully removed</returns>
        public bool RemoveCell(int key)
        {
            if (cells.ContainsKey(key) == true)
            {
                Cell cell = cells[key];

                // remove all pairs that contain this cell
                hSim.CollisionManager.RemoveAllPairsContainingCell(cell);
                // remove the cell from the grid
                hSim.CollisionManager.RemoveCellFromGrid(cell);
                // remove the cell from the population
                populations[cell.Population_id].Remove(cell.Cell_id);
                // remove the cell itself
                hSim.RemoveCell(cell);
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
                populations[cell.Population_id].Add(cells[oldKey].Cell_id, cell);
                // remove the old key from the population
                populations[cell.Population_id].Remove(oldKey);
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

            foreach (int key in cells.Keys)
            {
                removalList.Add(key);
            }

            for (int i = 0; i < frame.CellCount; i++)
            {
                int cell_id = frame.CellIDs[i];

                // take off the removal list
                removalList.Remove(cell_id);

                frame.applyStateByIndex(i, ref state);

                // if the cell doesn't exist, create it
                if (cells.ContainsKey(cell_id) == false)
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
                    hSim.instantiateCell(cell_id, cp, configComp, bulk_reacs, boundary_reacs, transcription_reacs, false);
                }
                // now apply the state
                cells[cell_id].SetCellState(state);
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
                hSim.Reporter.AppendDeathEvent(cell.Cell_id, cell.Population_id);
            }
        }

        public void DivisionEvent(int mother_id, int pop_id, int daughter_id, int generation)
        {
            hSim.Reporter.AppendDivisionEvent(mother_id, pop_id, daughter_id, generation);
        }

        public void ExitEvent(int key)
        {
            if (cells.ContainsKey(key) == true)
            {
                Cell cell = cells[key];
                hSim.Reporter.AppendExitEvent(cell.Cell_id, cell.Population_id);
            }
        }
    }

}
