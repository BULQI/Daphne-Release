//#define ALL_DATA
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
    /// entity holding data used by the simulation and other components; also provides database retrieval functions
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
        /// data file
        /// </summary>
        public static HDF5File hdf5file;
        /// <summary>
        /// currently handled experimentID
        /// </summary>
        public static int currentExperimentID = -1;
#if ALL_DATA
        /// <summary>
        /// dictionary of raw cell track sets data
        /// </summary>
        private Dictionary<int, CellTrackData> tracks;
        /// <summary>
        /// dictionary of cell names from database
        /// </summary>
        private Dictionary<string, int> cellNames;
        /// <summary>
        /// data reader for grabbing cell data from database
        /// </summary>
        private DataReader dr;

        private bool validFile = false;
        public int dimension { get; set; }
#endif
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
            // create the hdf5 object
            hdf5file = new HDF5File();
#if ALL_DATA
            ResetTrackData();
#endif
        }

        /// <summary>
        /// extract the experiment id from a three part string XX_ID_XX
        /// </summary>
        /// <param name="exp">the experiment string</param>
        /// <returns>-1 for error, id >= 0 otherwise</returns>
        public static int extractExperimentId(string exp)
        {
            string[] parts = exp.Split('_');

            if(parts.Length >= 2)
            {
                return Convert.ToInt32(parts[1]);
            }
            return -1;
        }

        /// <summary>
        /// find the highest experiment id
        /// </summary>
        /// <returns>the highest id or -1 if empty</returns>
        public static int findHighestExperimentId()
        {
            List<string> local = hdf5file.subGroupNames("/Experiments_VCR");
            int max = -1;

            foreach (string s in local)
            {
                int tmp = extractExperimentId(s);

                if (tmp > max)
                {
                    max = tmp;
                }
            }
            return max;
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

#if ALL_DATA
        /// <summary>
        /// accessor for the dictionary of cell tracks
        /// </summary>
        public Dictionary<int, CellTrackData> Tracks
        {
            get { return tracks; }
        }

        /// <summary>
        /// retrieve the valid file status
        /// </summary>
        public bool ValidFile
        {
            get { return validFile; }
        }

        /// <summary>
        /// reset and clear all track data
        /// </summary>
        public void ResetTrackData()
        {
            dimension = 0;

            if (cellNames == null)
            {
                cellNames = new Dictionary<string, int>();
            }
            else
            {
                cellNames.Clear();
            }

            if (tracks == null)
            {
                tracks = new Dictionary<int, CellTrackData>();
            }
            else
            {
                tracks.Clear();
            }
        }

        /// <summary>
        /// accessor for dictionary of cell names from database
        /// </summary>
        public Dictionary<string, int> CellNames
        {
            get { return cellNames; }
        }

        /// <summary>
        /// accessor for data reader for grabbing cell data from database
        /// </summary>
        public DataReader DR
        {
            get { return dr; }
        }
        /// <summary>
        /// used to destroy the database reader when we clear the database. otherwise a trick bug will occur --Feng 9/13/2012
        /// </summary>
        public void ClearReader()
        {
            dr = null;
        }

        /// <summary>
        /// Links the LPManager to the correct DataReader. Extracts and exposes CellNames.
        /// </summary>
        /// <param name="expID">experiment id</param>
        public bool ConnectToExperiment(int expID = -1)
        {
            int ID = expID < 0 ? MainWindow.SOP.Protocol.experiment_db_id : expID;

            // First, check whether already connected to correct experiment
            if (dr != null)
            {
                if (dr.ExperimentID == ID)
                    return true;
            }

            dr = new DataReader(ID);

            if (dr.TimeVals.Count == 0)
            {
                return false;
            }

            dimension = 3;

            cellNames.Clear();
            foreach (int entry in dr.Cells.Keys)
            {
                string cellname = "cell " + entry.ToString();
                if (!cellNames.ContainsKey(cellname))
                {
                    cellNames.Add(cellname, entry);
                }
            }
            validFile = true;
            return true;
        }

        /// <summary>
        /// access a cell track by key; if it doesn't exist create it
        /// and read data from database
        /// </summary>
        /// <param name="key">the cell id is the key</param>
        /// <returns></returns>
        public CellTrackData GetCellTrack(int key)
        {
            bool success = false;
            if (Tracks.ContainsKey(key) == false)
            {
                Tracks.Add(key, new CellTrackData());
                // Try to read in the data to the CellTrackData
                // TODO: Not sure how to notify if fails...
                success = this.LoadTrackData(key);
            }
            return Tracks[key];
        }

        /// <summary>
        /// Fills in time and position with the data for the selected cell.
        /// </summary>
        /// <param name="alt_id"></param>
        public bool LoadTrackData(int alt_id)
        {
            if (validFile == false || dr.HasCell(alt_id) == false)
            {
                return false;
            }

            if (Tracks.ContainsKey(alt_id) == false)
            {
                Tracks.Add(alt_id, new CellTrackData());
                Tracks[alt_id].CellID = alt_id;
            }

            List<DBDict> rows = dr.FetchByCell(alt_id);

            int nSteps = rows.Count();

            //These are the values used to create the trajectory.
            double[] x = new double[nSteps];
            double[] y = new double[nSteps];
            double[] z = new double[nSteps];

            double[] ttime = new double[nSteps];

            double min = rows[0].time_val;

            //These are the values stored in time and position.
            Tracks[alt_id].ActualTrackTimes = new List<double>();
            Tracks[alt_id].ActualTrackPositions = new List<double[]>();

            //Fill in ttime, x, y, z, time, and position from the retrieved DBDict...
            for (int i = 0; i < rows.Count(); i++)
            {
                if (rows[i].state.ContainsKey("_Position.0.LM") && rows[i].state.ContainsKey("_Position.1.LM") && rows[i].state.ContainsKey("_Position.2.LM"))
                {
                    double[] pos = new double[3];
                    pos[0] = rows[i].state["_Position.0.LM"];
                    pos[1] = rows[i].state["_Position.1.LM"];
                    pos[2] = rows[i].state["_Position.2.LM"];
                    Tracks[alt_id].ActualTrackTimes.Add(rows[i].time_val);
                    Tracks[alt_id].ActualTrackPositions.Add(pos);
                    ttime[i] = rows[i].time_val - min;
                    x[i] = pos[0];
                    y[i] = pos[1];
                    z[i] = pos[2];
                }
            }

            List<double[]> tposition = new List<double[]>();
            tposition.Add(x);
            if (dimension >= 2)
            {
                tposition.Add(y);
            }
            if (dimension >= 3)
            {
                tposition.Add(z);
            }
            Tracks[alt_id].ActualTrackTrajectory = new Trajectory(ttime, tposition);
            Tracks[alt_id].ActualTrackTimesArray = ttime;
            return true;
        }

        /// <summary>
        /// create a new cell
        /// </summary>
        /// <param name="type">flag indicating the type (B or T)</param>
        /// <param name="chemokineReceptorInitialStates">initial repector state, null for non-motile cells</param>
        /// <param name="cellset_id">cell set ID</param>
        /// <param name="index">cell index</param>
        /// <param name="addToDict">true for inserting the cell immediately into the dictionary</param>
        /// <returns>pointer to the new cell</returns>
        public BaseCell CreateCell(CellBaseTypeLabel type,
                                   ObservableCollection<ReceptorParameters> chemokineReceptorInitialStates,
                                   int cellset_id,
                                   int index = -1,
                                   bool addToDict = true)
        {
            BaseCell cell;

            if (BaseCell.isMotileBaseType(type) == true && chemokineReceptorInitialStates != null)
            {
                if (type == CellBaseTypeLabel.BCell)
                {
                    cell = new BCell(index > -1 ? index : Cell.SafeCellIndex++, cellset_id);
                }
                else if (type == CellBaseTypeLabel.TCell)
                {
                    cell = new TCell(index > -1 ? index : Cell.SafeCellIndex++, cellset_id);
                }
                else
                {
                    return null;
                }

                // create and add the chemokine receptors
                foreach (ReceptorParameters relp in chemokineReceptorInitialStates)
                {
                    // only add receptors for solfacs that are present in the simulation
                    foreach (KeyValuePair<string, SolfacTypeController> kvp in MainWindow.VTKBasket.ChemokineController.SolfacTypeControllers)
                    {
                        if (kvp.Value.TypeGUID == relp.receptor_solfac_type_guid_ref)
                        {
                            ChemokineReceptor cr = ((MotileCell)cell).AddCKReceptor(relp.receptor_solfac_type_guid_ref);

                            cr.SetParametersFromConfigFile(relp);
                            break;
                        }
                    }
                }
            }
            else if (type == CellBaseTypeLabel.FDC && chemokineReceptorInitialStates == null)
            {
                cell = new FDC(index > -1 ? index : Cell.SafeCellIndex++, cellset_id);
            }
            else
            {
                return null;
            }

            if (addToDict == true)
            {
                cells.Add(cell.CellIndex, cell);
            }
            return cell;
        }

#if FENG_DIVISION
        /// <summary>
        /// create a clone of a cell
        /// </summary>
        /// <param name="mother">the mother cell to be cloned</param>
        /// <param name="initialLocomotorState">inital locomotor state</param>
        /// <param name="seed">seed value for the new cell's locomotor object</param>
        /// <param name="addToDict">true for inserting the cell immediately into the dictionary</param>
        /// <returns>pointer to the cloned cell</returns>
        public BaseCell CloneCell(BaseCell mother, double[] initialLocomotorState, int seed = 0, bool addToDict = true)
        {
            BaseCell clone = (BaseCell)Utilities.DeepClone(mother);

            // set safe index
            clone.CellIndex = Cell.SafeCellIndex++;
            // reset force
            clone.LM.resetForce();
            // create the division model
            if (mother.CellDivides == true)
            {
                ((MotileCell)clone).DivModel = new TwoFactorModel(((MotileCell)mother).DivModel);
            }
            // set initial locomotor state
            clone.LM.ChangeState(initialLocomotorState);
            // reseed the locomotor
            clone.LM.Reseed(seed);

            if (addToDict == true)
            {
                cells.Add(clone.CellIndex, clone);
            }
            return clone;
        }
#endif
#endif
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

            foreach (int key in cells.Keys)
            {
                removalList.Add(key);
            }

            for (int c = 0; c < frame.CellIDs.Length; c++)
            {
                int cell_id = frame.CellIDs[c];

                // take off the removal list
                removalList.Remove(cell_id);

                // if the cell doesn't exist, create it
                if (cells.ContainsKey(cell_id) == false)
                {
                    ConfigCompartment[] configComp = new ConfigCompartment[2];
                    List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
                    List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
                    List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();
                    int cellPopId = frame.CellPopIDs[c];

                    if (((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict.ContainsKey(cellPopId) == false)
                    {
                        throw new Exception("Cell population id invalid.");
                    }

                    CellPopulation cp = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict[cellPopId];
                    CellState state = new CellState();

                    // set the position
                    for (int i = 0; i < CellSpatialState.SingleDim; i++)
                    {
                        state.spState.X[i] = frame.CellPos[c * CellSpatialState.SingleDim + i];
                    }
                    // set the generation
                    state.setCellGeneration(frame.CellGens[c]);
                    hSim.prepareCellInstantiation(cp, configComp, bulk_reacs, ref boundary_reacs, ref transcription_reacs);
                    hSim.instantiateCell(cell_id, cp, configComp, state, bulk_reacs, boundary_reacs, transcription_reacs, false);
                    cells[cell_id].Population_id = cellPopId;
                }
                else // it exists, update it
                {
#if CELL_CREATE_OLD
                    // Note: we may not need this again; it's a leftover from the old
                    // db implementation, but we need some mechanism to set the entire state
                    ObjectLoader.LoadValues(cells[cell_id], kvpc.Value.state);
#else
                    for(int i = 0; i < CellSpatialState.SingleDim; i++)
                    {
                        cells[cell_id].SpatialState.X[i] = frame.CellPos[c * CellSpatialState.SingleDim + i];
                    }
#endif
                }
#if CELL_CREATE_OLD
                // create a new cell
                else
                {
                    // make sure the type exists
                    //ADD THE CODE TO CREATE A NEW CELL
                    if (dr.Cells[kvpc.Value.cell_id].ContainsKey("CellSetId"))
                    {
                        if (MainWindow.VTKBasket.CellController.ColorMap.ContainsKey((int)dr.Cells[kvpc.Value.cell_id]["CellSetId"]) == true)
                        {
                            CellSubset ct = null;
                            int cellSetID = MainWindow.VTKBasket.CellController.ColorMap[(int)dr.Cells[kvpc.Value.cell_id]["CellSetId"]];

                            // find cell type
                            for (int j = 0; j < MainWindow.SOP.Protocol.entity_repository.cell_subsets.Count; j++)
                            {
                                if (MainWindow.SOP.Protocol.entity_repository.cell_subsets[j].cell_subset_guid.CompareTo(MainWindow.SOP.Protocol.scenario.cellpopulations[cellSetID].cell_subset_guid_ref) == 0)
                                {
                                    ct = MainWindow.SOP.Protocol.entity_repository.cell_subsets[j];
                                    break;
                                }
                            }

                            // cell type not found, don't create this cell
                            if (ct == null)
                            {
                                System.Windows.MessageBox.Show("Cell set '" + MainWindow.SOP.Protocol.scenario.cellpopulations[kvpc.Value.cell_id].cellpopulation_name +
                                                               "' could not be extracted from the repository!");
                                continue;
                            }

                            int loco_idx = -1;

                            for (int j = 0; j < MainWindow.SOP.Protocol.global_parameters.Count && loco_idx == -1; j++)
                            {
                                if (MainWindow.SOP.Protocol.global_parameters[j].global_parameter_type == GlobalParameterType.LocomotorParams)
                                {
                                    loco_idx = j;
                                }
                            }

                            // if the locomotor parameter could not be found, don't create this cell
                            if (loco_idx == -1)
                            {
                                System.Windows.MessageBox.Show("Invalid global parameters specified for locomotor params! Skipping creation of cell.");
                                continue;
                            }
                            //skg 6/1/12 changed
                            BaseCell cell;

                            if (ct.cell_subset_type.baseCellType == CellBaseTypeLabel.BCell)
                            {
                                cell = CreateCell(ct.cell_subset_type.baseCellType, ((BCellSubsetType)ct.cell_subset_type).cell_subset_type_receptor_params, 0, 0, false);
                            }
                            else if (ct.cell_subset_type.baseCellType == CellBaseTypeLabel.TCell)
                            {
                                cell = CreateCell(ct.cell_subset_type.baseCellType, ((TCellSubsetType)ct.cell_subset_type).cell_subset_type_receptor_params, 0, 0, false);
                            }
                            else
                            {
                                // for now ignore everything but B and T cells
                                continue;
                            }
                            //end skg

                            cell.LM.SetParametersFromConfigFile((LocomotorParams)MainWindow.SOP.Protocol.global_parameters[loco_idx]);

                            ObjectLoader.LoadValues(cell, kvpc.Value.state);
                            ObjectLoader.LoadValues(cell, dr.Cells[kvpc.Value.cell_id]);
#if FENG_DIVISION
                            cell.CellDivides = true;
#endif
                            AddCell(cell);
                        }
                    }
                }
#endif
            }

            // remove cells
            foreach (int key in removalList)
            {
                RemoveCell(key);
            }
        }
    }

#if ALL_DATA
    /// <summary>
    /// Data structure for single cell track data
    /// Reproduction of data structures and database methods from LPManager so can
    /// transition to this being the central data store for LPManager and all other
    /// track-related methods. Altered to store both standard and zero-force fit
    /// results.
    /// </summary>
    public class CellTrackData
    {
        // TODO: There is duplicate info being stored for time, pos and trajectory
        //   probably from legacy code which needs different forms. Remove redundancy...

        public int CellID { get; set; }
        public List<double> ActualTrackTimes { get; set; }
        public double[] ActualTrackTimesArray { get; set; }
        public List<double[]> ActualTrackPositions { get; set; }
        public Trajectory ActualTrackTrajectory { get; set; }
        public ColumnVector StandardTPredict { get; set; }
        public List<ColumnVector> StandardXPredict { get; set; }
        public List<ColumnVector> StandardVPredict { get; set; }
        public List<ColumnVector> StandardXPredictSE { get; set; }
        public List<ColumnVector> StandardVPredictSE { get; set; }
        public ColumnVector ZeroForceTPredict { get; set; }
        public List<ColumnVector> ZeroForceXPredict { get; set; }
        public List<ColumnVector> ZeroForceVPredict { get; set; }
        public List<ColumnVector> ZeroForceXPredictSE { get; set; }
        public List<ColumnVector> ZeroForceVPredictSE { get; set; }

        private string standardTrackText,
                       zeroForceTrackText;
        public int Dimension { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public CellTrackData()
        {
            CellID = -1;
        }

        /// <summary>
        /// clears the current solution
        /// </summary>
        /// <param name="nPredict">number of points in the prediction</param>
        /// <param name="zeroForce">true for zero force fit</param>
        public void resetSolution(int nPredict, bool zeroForce)
        {
            if (zeroForce)
            {
                ZeroForceTPredict = new ColumnVector(nPredict);
                ZeroForceXPredict = null;
                ZeroForceVPredict = null;
                ZeroForceXPredictSE = null;
                ZeroForceVPredictSE = null;
            }
            else
            {
                StandardTPredict = new ColumnVector(nPredict);
                StandardXPredict = null;
                StandardVPredict = null;
                StandardXPredictSE = null;
                StandardVPredictSE = null;
            }
        }

        /// <summary>
        /// get/set the standard track text string
        /// </summary>
        public string StandardTrackText
        {
            get { return standardTrackText; }
            set { standardTrackText = value; }
        }

        /// <summary>
        /// get/set the zero force track text string
        /// </summary>
        public string ZeroForceTrackText
        {
            get { return zeroForceTrackText; }
            set { zeroForceTrackText = value; }
        }

    }
#endif
}
