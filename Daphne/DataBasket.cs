//#define ADVANCED_STUFF
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
#if ADVANCED_STUFF
        /// <summary>
        /// safe cell id, guaranteed to be unused
        /// </summary>
        private int safeCellID;
#endif
        /// <summary>
        /// cell environment data object
        /// </summary>
        private ExtraCellularSpace ecs;
        /// <summary>
        /// dictionary of cells
        /// </summary>
        private Dictionary<int, Cell> cells;
#if ADVANCED_STUFF
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
        /// 


        private DataReader dr;


        private bool validFile = false;
        public int dimension { get; set; }
#endif
        /// <summary>
        /// constructor
        /// </summary>
        public DataBasket()
        {
            cells = new Dictionary<int,Cell>();
#if ADVANCED_STUFF
            safeCellID = 0;
            ResetTrackData();
#endif
        }

#if ADVANCED_STUFF
        /// <summary>
        /// accessor for the safe cell id
        /// </summary>
        public int SafeCellID
        {
            get { return safeCellID; }
            set { safeCellID = value; }
        }
#endif

        /// <summary>
        /// accessor for the cell environment
        /// </summary>
        public ExtraCellularSpace ECS
        {
            get { return ecs; }
            set { ecs = value; }
        }

        /// <summary>
        /// accessor for the dictionary of cells
        /// </summary>
        public Dictionary<int, Cell> Cells
        {
            get { return cells; }
            set { cells = value; }
        }

#if ADVANCED_STUFF
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
            int ID = expID < 0 ? MainWindow.SC.SimConfig.experiment_db_id : expID;

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
                    cell = new BCell(index > -1 ? index : safeCellID++, cellset_id);
                }
                else if (type == CellBaseTypeLabel.TCell)
                {
                    cell = new TCell(index > -1 ? index : safeCellID++, cellset_id);
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
                cell = new FDC(index > -1 ? index : safeCellID++, cellset_id);
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
            clone.CellIndex = safeCellID++;
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
        /// add a given cell
        /// </summary>
        /// <param name="cell">the cell to add</param>
        /// <returns>true for success</returns>
        public bool AddCell(Cell cell)
        {
            if (cell != null)
            {
#if ADVANCED_STUFF
                // cause the cell to be updated into the grid in the next simulation round
                cell.GridIndex[0] = cell.GridIndex[1] = cell.GridIndex[2] = -1;
#endif
                // add the cell
                cells.Add(cell.Index, cell);
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

#if ADVANCED_STUFF
                // remove all pairs that contain this cell
                MainWindow.Sim.CellManager.RemoveAllPairsContainingCell(cell, cells);
                // remove the cell from the grid
                MainWindow.Sim.CellManager.RemoveCellFromGrid(cell);
#endif
                // remove the cell itself
                cells.Remove(cell.Index);
                return true;
            }
            return false;
        }

#if ADVANCED_STUFF
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
                MainWindow.Sim.CellManager.RekeyAllPairsContainingCell(cell, oldKey, cells);
                // rekey the cell in the grid
                MainWindow.Sim.CellManager.RekeyCellInGrid(cell, oldKey);
                // add the new key
                cells.Add(MainWindow.Basket.Cells[oldKey].Index, cell);
                // remove the old key
                cells.Remove(oldKey);
                return true;
            }
            return false;
        }

        /// <summary>
        /// update all cells given a list of db rows
        /// </summary>
        /// <param name="list">the db data</param>
        /// <param name="progress">the progress state of playback</param>
        public void UpdateCells(Dictionary<int, DBDict> list, int progress)
        {
            // iterate through the list and update cells; we have to detect those that are new from division or got deleted by death
            ConnectToExperiment();

            List<int> removalList = new List<int>();

            foreach (int key in cells.Keys)
            {
                removalList.Add(key);
            }

            foreach (KeyValuePair<int, DBDict> kvpc in list)
            {
                // take off the removal list
                removalList.Remove(kvpc.Value.cell_id);

                // if the cell exists update it
                if (cells.ContainsKey(kvpc.Value.cell_id))
                {
                    ObjectLoader.LoadValues(cells[kvpc.Value.cell_id], kvpc.Value.state);
                }
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
                            for (int j = 0; j < MainWindow.SC.SimConfig.entity_repository.cell_subsets.Count; j++)
                            {
                                if (MainWindow.SC.SimConfig.entity_repository.cell_subsets[j].cell_subset_guid.CompareTo(MainWindow.SC.SimConfig.scenario.cellpopulations[cellSetID].cell_subset_guid_ref) == 0)
                                {
                                    ct = MainWindow.SC.SimConfig.entity_repository.cell_subsets[j];
                                    break;
                                }
                            }

                            // cell type not found, don't create this cell
                            if (ct == null)
                            {
                                System.Windows.MessageBox.Show("Cell set '" + MainWindow.SC.SimConfig.scenario.cellpopulations[kvpc.Value.cell_id].cellpopulation_name +
                                                               "' could not be extracted from the repository!");
                                continue;
                            }

                            int loco_idx = -1;

                            for (int j = 0; j < MainWindow.SC.SimConfig.global_parameters.Count && loco_idx == -1; j++)
                            {
                                if (MainWindow.SC.SimConfig.global_parameters[j].global_parameter_type == GlobalParameterType.LocomotorParams)
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

                            cell.LM.SetParametersFromConfigFile((LocomotorParams)MainWindow.SC.SimConfig.global_parameters[loco_idx]);

                            ObjectLoader.LoadValues(cell, kvpc.Value.state);
                            ObjectLoader.LoadValues(cell, dr.Cells[kvpc.Value.cell_id]);
#if FENG_DIVISION
                            cell.CellDivides = true;
#endif
                            AddCell(cell);
                        }
                    }
                }
            }

            // remove cells
            foreach (int key in removalList)
            {
                RemoveCell(key);
            }

            MainWindow.VTKBasket.UpdateData();
            MainWindow.GC.DrawFrame(progress);
        }
    }

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

#endif
    }
}
