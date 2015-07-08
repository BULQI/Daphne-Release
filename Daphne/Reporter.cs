using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using System.IO;

using ManifoldRing;
using System.Globalization;

namespace Daphne
{
    public abstract class ReporterBase
    {
        protected DateTime startTime;
        protected string fileNameBase, fileNameAssembled;
        public bool NeedsFileNameWrite { get; set; }
        public string AppPath { get; set; } // non uri
        public string UniquePath { get; set; }

        public ReporterBase()
        {
            NeedsFileNameWrite = false;
        }

        /// <summary>
        /// the starting string for the file name
        /// </summary>
        public string FileNameBase
        {
            get { return fileNameBase; }
            set { fileNameBase = value; }
        }

        /// <summary>
        /// the assembled file name with extension, etc., but not the full path
        /// </summary>
        public string FileNameAssembled
        {
            get { return fileNameAssembled; }
            set { fileNameAssembled = value; }
        }

        /// <summary>
        /// create a unique folder name inside appPath
        /// </summary>
        /// <param name="protocolFileName">if a file name base does not exist we use part of the protocol name</param>
        /// <param name="create">true when immediate creation of the folder is desired</param>
        protected void createUniqueFolderName(string protocolFileName, bool create)
        {
            int index = 1, upTo = 8;
            string name = "";
            
            if (fileNameBase == "")
            {
                string protocol = System.IO.Path.GetFileName(protocolFileName);
                int period = protocol.LastIndexOf('.');

                name = protocol.Substring(0, Math.Min(upTo, period));
            }
            else
            {
                name = fileNameBase;
            }
            do
            {
                UniquePath = AppPath + name + "_" + index + @"\";
                index++;
            } while(Directory.Exists(UniquePath) == true);

            if (create == true && UniquePath != "" && Directory.Exists(UniquePath) == false)
            {
                Directory.CreateDirectory(UniquePath);
            }
        }

        /// <summary>
        /// general function to create a stream for a reporter file
        /// </summary>
        /// <param name="file">part of the file name</param>
        /// <param name="extension">file extension</param>
        /// <returns></returns>
        protected StreamWriter createStreamWriter(string file, string extension)
        {
            int version = 1;
            string nameStart,
                   fullPath;

            if (fileNameBase == "")
            {
                nameStart = startTime.Month + "." + startTime.Day + "." + startTime.Year + "_" + startTime.Hour + "h" + startTime.Minute + "m" + startTime.Second + "s_";
            }
            else
            {
                nameStart = fileNameBase + "_";
            }

            fileNameAssembled = nameStart + file + "." + extension;
            fullPath = UniquePath + fileNameAssembled;

            do
            {
                if (File.Exists(fullPath) == true)
                {
                    fileNameAssembled = nameStart + "_" + file + "(" + version + ")." + extension;
                    fullPath = UniquePath + fileNameAssembled;
                    version++;
                }
                else
                {
                    if (UniquePath != "" && Directory.Exists(UniquePath) == false)
                    {
                        Directory.CreateDirectory(UniquePath);
                    }
                    return File.CreateText(fullPath);
                }
            } while (true);
        }

        /// <summary>
        /// utility to convert to a safe string
        /// </summary>
        /// <param name="s">string to convert</param>
        /// <returns>original string or "null"</returns>
        protected string stringSafety(string s)
        {
            if (s == "null")
            {
                return "";
            }
            if (s == null || s == "")
            {
                return "null";
            }
            return s;
        }

        protected void WriteReactionsList(StreamWriter writer, List<ConfigReaction> reactions)
        {
            foreach (ConfigReaction cr in reactions)
            {
                writer.WriteLine("{0:G4}\t{1}", cr.rate_const, cr.TotalReactionString);
            }
        }

        public virtual CellTrackData ProvideCellTrackData(int cellID)
        {
            return null;
        }

        public virtual CellPopulationDynamicsData ProvideCellPopulationDynamicsData(CellPopulation pop)
        {
            return null;
        }

        public virtual Dictionary<int, FounderInfo> ProvideFounderCells()
        {
            return null;
        }

        public virtual Dictionary<BigInteger, GeneologyInfo> ProvideGeneologyData(FounderInfo founder)
        {
            return null;
        }

        public abstract void StartReporter(string protocolFileName);
        public abstract void AppendReporter();
        public abstract void CloseReporter();

        public abstract void WriteReporterFileNamesToHDF5(HDF5FileBase hdf5File);
        public abstract void ReadReporterFileNamesFromHDF5(HDF5FileBase hdf5File);

        public abstract void AppendDeathEvent(Cell cell);
        public abstract void AppendDivisionEvent(Cell cell, Cell daughter);
        public abstract void AppendExitEvent(Cell cell);

        public abstract void ReactionsReport();
    }

    public abstract class SimulationReporterFiles
    {
        public SimulationReporterFiles()
        {
        }

        public abstract void clearFileStrings();
    }

    public class TissueSimulationReporterFiles : SimulationReporterFiles
    {
        public string ECMMeanReport { get; set; }
        public string ReactionsReport { get; set; }
        public Dictionary<double, string> ECMReportStep { get; set; }
        public Dictionary<int, string> CellTypeReport { get; set; }
        public Dictionary<int, string> CellTypeDeath { get; set; }
        public Dictionary<int, string> CellTypeDivision { get; set; }
        public Dictionary<int, string> CellTypeExit { get; set; }

        public TissueSimulationReporterFiles()
        {
            clearFileStrings();
        }

        public override void clearFileStrings()
        {
            ECMMeanReport = "";
            ReactionsReport = "";
            if (ECMReportStep == null)
            {
                ECMReportStep = new Dictionary<double, string>();
            }
            else
            {
                ECMReportStep.Clear();
            }
            if (CellTypeReport == null)
            {
                CellTypeReport = new Dictionary<int, string>();
            }
            else
            {
                CellTypeReport.Clear();
            }
            if (CellTypeDeath == null)
            {
                CellTypeDeath = new Dictionary<int, string>();
            }
            else
            {
                CellTypeDeath.Clear();
            }
            if (CellTypeDivision == null)
            {
                CellTypeDivision = new Dictionary<int, string>();
            }
            else
            {
                CellTypeDivision.Clear();
            }
            if (CellTypeExit == null)
            {
                CellTypeExit = new Dictionary<int, string>();
            }
            else
            {
                CellTypeExit.Clear();
            }
        }
    }

    public class TissueSimulationReporter : ReporterBase
    {
        private StreamWriter ecm_mean_file;
        private Dictionary<int, StreamWriter> cell_files;
        private TissueSimulation hSim;
        private Dictionary<int, TransitionEventReporter> deathEvents;
        private Dictionary<int, TransitionEventReporter> divisionEvents;
        private Dictionary<int, TransitionEventReporter> exitEvents;
        private TissueSimulationReporterFiles tsFiles;

        public TissueSimulationReporter(SimulationBase sim)
        {
            if (sim is TissueSimulation == false)
            {
                throw new InvalidCastException();
            }

            hSim = sim as TissueSimulation;
            cell_files = new Dictionary<int, StreamWriter>();
            deathEvents = new Dictionary<int, TransitionEventReporter>();
            divisionEvents = new Dictionary<int, TransitionEventReporter>();
            exitEvents = new Dictionary<int, TransitionEventReporter>();
            tsFiles = new TissueSimulationReporterFiles();
        }

        public override void StartReporter(string protocolFileName)
        {
            NeedsFileNameWrite = true;
            tsFiles.clearFileStrings();
            startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            CloseReporter();
            createUniqueFolderName(protocolFileName, false);
            startECM();
            startCells();
            startEvents();
            ReactionsReport();
        }

        public override void AppendReporter()
        {
            appendECM();
            appendCells();
        }

        public override void CloseReporter()
        {
            closeECM();
            closeCells();
            closeEvents();
        }

        private void startECM()
        {
            string header = "time";
            bool create = false;

            // mean
            foreach (ConfigMolecularPopulation c in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
            {
                if (((ReportECM)c.report_mp).mean == true)
                {
                    header += "\t" + SimulationBase.ProtocolHandle.entity_repository.molecules_dict[c.molecule.entity_guid].Name;
                    create = true;
                }
            }
            // was at least one molecule selected?
            if(create == true)
            {
                ecm_mean_file = createStreamWriter("ecm_mean_report", "txt");
                tsFiles.ECMMeanReport = fileNameAssembled;
                ecm_mean_file.WriteLine("ECM mean report from {0} run on {1}.", SimulationBase.ProtocolHandle.experiment_name, startTime);
                ecm_mean_file.WriteLine(header);
            }
        }

        private void appendECM()
        {
            // mean
            if (ecm_mean_file != null)
            {
                // simulation time
                ecm_mean_file.Write(hSim.AccumulatedTime);
                foreach (ConfigMolecularPopulation c in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
                {
                    if (((ReportECM)c.report_mp).mean == true)
                    {
                        // mean concentration of this ecm molecular population
                        ecm_mean_file.Write("\t{0:G4}", SimulationBase.dataBasket.Environment.Comp.Populations[c.molecule.entity_guid].Conc.MeanValue());
                    }
                }
                // terminate line
                ecm_mean_file.WriteLine();
            }

            // extended
            foreach (ConfigMolecularPopulation c in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
            {
                if (c.report_mp.mp_extended > ExtendedReport.NONE)
                {
                    string name = SimulationBase.ProtocolHandle.entity_repository.molecules_dict[c.molecule.entity_guid].Name;
                    StreamWriter writer = createStreamWriter("ecm_" + name + "_report_step" + hSim.AccumulatedTime, "txt");
                    string header = "x\ty\tz\tconc\tgradient_x\tgradient_y\tgradient_z";

                    tsFiles.ECMReportStep.Add(hSim.AccumulatedTime, fileNameAssembled);
                    writer.WriteLine("ECM {0} report at {1}min from {2} run on {3}.", name, hSim.AccumulatedTime, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine(header);

                    InterpolatedRectangularPrism prism = (InterpolatedRectangularPrism)SimulationBase.dataBasket.Environment.Comp.Interior;
                    MolecularPopulation mp = SimulationBase.dataBasket.Environment.Comp.Populations[c.molecule.entity_guid];

                    for (int i = 0; i < prism.ArraySize; i++)
                    {
                        double[] pos = prism.linearIndexToLocal(i);

                        writer.Write("{0:G4}\t{1:G4}\t{2:G4}\t{3:G4}", pos[0], pos[1], pos[2], mp.Conc.Value(pos));

                        // gradient
                        if (c.report_mp.mp_extended == ExtendedReport.COMPLETE)
                        {
                             double[] grad = mp.Conc.Gradient(pos);

                            writer.Write("\t{0:G4}\t{1:G4}\t{2:G4}", grad[0], grad[1], grad[2]);
                        }
                        writer.WriteLine();
                    }
                    writer.Close();
                }
            }
        }

        private void closeECM()
        {
            if (ecm_mean_file != null)
            {
                ecm_mean_file.Close();
                ecm_mean_file = null;
            }
        }

        private void startCells()
        {
            // create a file stream for each cell population
            foreach (CellPopulation cp in ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulations)
            {
                string header = "cell_id\ttime\tlineage_id";
                bool create = false;

                if (cp.report_xvf.position == true)
                {
                    header += "\tpos_x\tpos_y\tpos_z";
                    create = true;
                }
                if (cp.report_xvf.velocity == true)
                {
                    header += "\tvel_x\tvel_y\tvel_z";
                    create = true;
                }
                if (cp.report_xvf.force == true)
                {
                    header += "\tforce_x\tforce_y\tforce_z";
                    create = true;
                }

                if (cp.reportStates.Differentiation == true)
                {
                    if (cp.Cell.diff_scheme != null && cp.Cell.diff_scheme.Driver.states.Count > 0)
                    {
                        header += "\tDiffState";
                        create = true;
                    }

                }
                if (cp.reportStates.Division == true)
                {
                    if (cp.Cell.div_scheme != null && cp.Cell.div_scheme.Driver.states.Count > 0)
                    {
                        header += "\tDivState";
                        create = true;
                    }
                }
                if (cp.reportStates.Generation == true)
                {
                        header += "\tgeneration";
                        create = true;
                }
                if (cp.reportStates.Death == true)
                {
                    if (cp.Cell.death_driver != null)
                    {
                        header += "\tDeathState";
                        create = true;
                    }
                }


                // cell molpop concentrations
                for (int i = 0; i < 2; i++)
                {
                    // 0: cytosol, 1: membrane
                    //ConfigCompartment comp = (i == 0) ? protocol.entity_repository.cells_dict[cp.Cell.entity_guid].cytosol : protocol.entity_repository.cells_dict[cp.Cell.entity_guid].membrane;
                    ConfigCompartment comp = (i == 0) ? cp.Cell.cytosol : cp.Cell.membrane;

                    foreach (ConfigMolecularPopulation mp in comp.molpops)
                    {
                        string name = SimulationBase.ProtocolHandle.entity_repository.molecules_dict[mp.molecule.entity_guid].Name;

                        if (mp.report_mp.mp_extended > ExtendedReport.NONE)
                        {
                            header += "\t" + name;
                            create = true;

                            // gradient
                            if (mp.report_mp.mp_extended == ExtendedReport.COMPLETE)
                            {
                                header += "\t" + name + "_grad_x\t" + name + "_grad_y\t" + name + "_grad_z";
                            }
                        }
                    }
                }

                // ecm probe concentrations
                foreach (ConfigMolecularPopulation mp in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
                {
                    string name = SimulationBase.ProtocolHandle.entity_repository.molecules_dict[mp.molecule.entity_guid].Name;

                    if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended > ExtendedReport.NONE)
                    {
                        header += "\t" + name + "_probe";
                        create = true;

                        // gradient
                        if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended == ExtendedReport.COMPLETE)
                        {
                            header += "\t" + name + "_probe_grad_x\t" + name + "_probe_grad_y\t" + name + "_probe_grad_z";
                        }
                    }
                }

                // create file, write header
                if (create == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_report", "txt");

                    tsFiles.CellTypeReport.Add(cp.cellpopulation_id, fileNameAssembled);
                    writer.WriteLine("Cell {0} report from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine(header);
                    cell_files.Add(cp.cellpopulation_id, writer);
                }
            }
        }

        private void appendCells()
        {
            // create a file stream for each cell population
            foreach (CellPopulation cp in ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulations)
            {
                if (cell_files.ContainsKey(cp.cellpopulation_id) == false)
                {
                    continue;
                }

                foreach (Cell c in SimulationBase.dataBasket.Populations[cp.cellpopulation_id].Values)
                {
                    // cell_id lineage_id time
                    cell_files[cp.cellpopulation_id].Write("{0}\t{1}\t{2}", c.Cell_id, hSim.AccumulatedTime, c.Lineage_id);

                    if (cp.report_xvf.position == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0:G4}\t{1:G4}\t{2:G4}", c.SpatialState.X[0], c.SpatialState.X[1], c.SpatialState.X[2]);
                    }
                    if (cp.report_xvf.velocity == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0:G4}\t{1:G4}\t{2:G4}", c.SpatialState.V[0], c.SpatialState.V[1], c.SpatialState.V[2]);
                    }
                    if (cp.report_xvf.force == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0:G4}\t{1:G4}\t{2:G4}", c.SpatialState.F[0], c.SpatialState.F[1], c.SpatialState.F[2]);
                    }

                    if (cp.reportStates.Differentiation == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.Differentiator.CurrentState);
                    }
                    if (cp.reportStates.Division == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.Divider.CurrentState);
                    }
                    if (cp.reportStates.Generation == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.generation);
                    }
                    if (cp.reportStates.Death == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.DeathBehavior.CurrentState);
                    }

                    // cell molpop concentrations
                    for (int i = 0; i < 2; i++)
                    {
                        // 0: cytosol, 1: membrane
                        ConfigCompartment configComp = (i == 0) ? cp.Cell.cytosol : cp.Cell.membrane;
                        Compartment comp = (i == 0) ? c.Cytosol : c.PlasmaMembrane;
                        double[] pos = new double[] { 0, 0, 0 };

                        foreach (ConfigMolecularPopulation cmp in configComp.molpops)
                        {
                            MolecularPopulation mp = comp.Populations[cmp.molecule.entity_guid];

                            // concentration
                            if (cmp.report_mp.mp_extended > ExtendedReport.NONE)
                            {
                                cell_files[cp.cellpopulation_id].Write("\t{0:G4}", mp.Conc.MeanValue());

                                // gradient
                                if (cmp.report_mp.mp_extended == ExtendedReport.COMPLETE)
                                {
                                    double[] grad = mp.Conc.Gradient(pos);

                                    cell_files[cp.cellpopulation_id].Write("\t{0:G4}\t{1:G4}\t{2:G4}", grad[0], grad[1], grad[2]);
                                }
                            }
                        }
                    }

                    // ecm probe concentrations
                    foreach (ConfigMolecularPopulation mp in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
                    {
                        string name = SimulationBase.ProtocolHandle.entity_repository.molecules_dict[mp.molecule.entity_guid].Name;

                        if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended > ExtendedReport.NONE)
                        {
                            cell_files[cp.cellpopulation_id].Write("\t{0:G4}", SimulationBase.dataBasket.Environment.Comp.Populations[mp.molecule.entity_guid].Conc.Value(c.SpatialState.X));

                            // gradient
                            if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended == ExtendedReport.COMPLETE)
                            {
                                double[] grad = SimulationBase.dataBasket.Environment.Comp.Populations[mp.molecule.entity_guid].Conc.Gradient(c.SpatialState.X);

                                cell_files[cp.cellpopulation_id].Write("\t{0:G4}\t{1:G4}\t{2:G4}", grad[0], grad[1], grad[2]);
                            }
                        }
                    }
                    // terminate the line
                    cell_files[cp.cellpopulation_id].WriteLine();
                }
            }
        }

        private void closeCells()
        {
            if (cell_files != null)
            {
                // close streams
                foreach (StreamWriter writer in cell_files.Values)
                {
                    writer.Close();
                }
                // remove the entries from the cell files dictionary
                cell_files.Clear();
            }
        }

        private void startEvents()
        {
            foreach (CellPopulation cp in ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulations)
            {
                if (cp.reportStates.Death == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_deathEvents", "txt");

                    tsFiles.CellTypeDeath.Add(cp.cellpopulation_id, fileNameAssembled);
                    writer.WriteLine("Cell {0} death events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine("cell_id\ttime\tlineage_id");
                    deathEvents.Add(cp.cellpopulation_id, new TransitionEventReporter(writer));
                }
                if (cp.reportStates.Division == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_divisionEvents", "txt");

                    tsFiles.CellTypeDivision.Add(cp.cellpopulation_id, fileNameAssembled);
                    writer.WriteLine("Cell {0} division events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine("cell_id\ttime\tdaughter_id\tgeneration\tmother_lineage_id\tdaughter1_lineage_id\tdaughter2_lineage_id");
                    divisionEvents.Add(cp.cellpopulation_id, new TransitionEventReporter(writer));
                }
                if (cp.reportStates.Exit == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_exitEvents", "txt");

                    tsFiles.CellTypeExit.Add(cp.cellpopulation_id, fileNameAssembled);
                    writer.WriteLine("Cell {0} exit events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine("cell_id\ttime\tlineage_id");
                    exitEvents.Add(cp.cellpopulation_id, new TransitionEventReporter(writer));
                }
            }
        }

        private void closeEvents()
        {
            foreach (KeyValuePair<int,TransitionEventReporter> kvp in deathEvents)
            {
                kvp.Value.WriteEvents();
            }
            foreach (KeyValuePair<int, TransitionEventReporter> kvp in divisionEvents)
            {
                kvp.Value.WriteEvents();
            }
            foreach (KeyValuePair<int, TransitionEventReporter> kvp in exitEvents)
            {
                kvp.Value.WriteEvents();
            }

            deathEvents.Clear();
            divisionEvents.Clear();
            exitEvents.Clear();
        }

        public override void AppendDeathEvent(Cell cell)
        {
            if (deathEvents != null)
            {
                if (deathEvents.ContainsKey(cell.Population_id))
                {
                    deathEvents[cell.Population_id].AddEvent(new TransitionEvent(hSim.AccumulatedTime, cell));
                }
            }
        }

        public override void AppendDivisionEvent(Cell cell, Cell daughter)
        {
            if (divisionEvents != null)
            {
                if (divisionEvents.ContainsKey(cell.Population_id))
                {
                    divisionEvents[cell.Population_id].AddEvent(new DivisionEvent(hSim.AccumulatedTime, cell, daughter));
                }
            }
        }

        public override void AppendExitEvent(Cell cell)
        {
            if (exitEvents != null)
            {
                if (exitEvents.ContainsKey(cell.Population_id))
                {
                    exitEvents[cell.Population_id].AddEvent(new TransitionEvent(hSim.AccumulatedTime, cell));
                }
            }
        }

        public override void ReactionsReport()
        {
            if (SimulationBase.ProtocolHandle.scenario.reactionsReport  == true)
            {
                StreamWriter writer = createStreamWriter("reactions_report", "txt");

                tsFiles.ReactionsReport = fileNameAssembled;
                writer.WriteLine("Reactions from {0} run on {1}.", SimulationBase.ProtocolHandle.experiment_name, startTime);
                writer.WriteLine("rate constant\treaction");
                writer.WriteLine();

                writer.WriteLine("ECM");
                writer.WriteLine();
                WriteReactionsList(writer, SimulationBase.ProtocolHandle.scenario.environment.comp.Reactions.ToList());
                foreach (ConfigReactionComplex crc in SimulationBase.ProtocolHandle.scenario.environment.comp.reaction_complexes)
                {
                    writer.WriteLine();
                    writer.WriteLine("Reaction Complex: {0}", crc.Name);
                    writer.WriteLine();
                    WriteReactionsList(writer, crc.reactions.ToList());
                }
                writer.WriteLine();

                foreach (CellPopulation cp in ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulations)
                {
                    writer.WriteLine("Cell population {0}, cell type {1}", cp.cellpopulation_id, cp.Cell.CellName);
                    writer.WriteLine();

                    writer.WriteLine("\nmembrane");
                    writer.WriteLine();
                    WriteReactionsList(writer, cp.Cell.membrane.Reactions.ToList());
                    foreach (ConfigReactionComplex crc in cp.Cell.membrane.reaction_complexes)
                    {
                        writer.WriteLine();
                        writer.WriteLine("Reaction Complex: {0}", crc.Name);
                        WriteReactionsList(writer, crc.reactions.ToList());
                    }
                    writer.WriteLine();

                    writer.WriteLine("cytosol");
                    writer.WriteLine();
                    WriteReactionsList(writer, cp.Cell.cytosol.Reactions.ToList());
                    foreach (ConfigReactionComplex crc in cp.Cell.cytosol.reaction_complexes)
                    {
                        writer.WriteLine();
                        writer.WriteLine("Reaction Complex: {0}", crc.Name);
                        writer.WriteLine();
                        WriteReactionsList(writer, crc.reactions.ToList());
                    }
                }

                writer.Close();
            }
        }

        /// <summary>
        /// write the reporter files group to file
        /// </summary>
        /// <param name="hdf5File">the file to write the data to</param>
        public override void WriteReporterFileNamesToHDF5(HDF5FileBase hdf5File)
        {
            // write only once
            if (NeedsFileNameWrite == false)
            {
                return;
            }
            NeedsFileNameWrite = false;

            int i;

            // create group entry
            hdf5File.createGroup("ReporterFiles");

            // single files
            // non-existing files get saved as "null"
            hdf5File.writeString("ECMMeanReport", stringSafety(tsFiles.ECMMeanReport));
            hdf5File.writeString("ReactionsReport", stringSafety(tsFiles.ReactionsReport));

            // groups with potentially multiple files in them
            
            // ecm report step, one file per time step
            hdf5File.createGroup("ECMReportStep");
            i = 0;
            foreach (KeyValuePair<double, string> kvp in tsFiles.ECMReportStep)
            {
                hdf5File.createGroup("ECMReportStep_" + i);
                hdf5File.writeDouble(kvp.Key, "Timestep");
                hdf5File.writeString("Filename", kvp.Value);
                hdf5File.closeGroup();
                i++;
            }
            hdf5File.closeGroup();

            // cell report, one file per cell population
            hdf5File.createGroup("CellTypeReport");
            i = 0;
            foreach (KeyValuePair<int, string> kvp in tsFiles.CellTypeReport)
            {
                hdf5File.createGroup("CellTypeReport_" + i);
                hdf5File.writeInt(kvp.Key, "Celltype");
                hdf5File.writeString("Filename", kvp.Value);
                hdf5File.closeGroup();
                i++;
            }
            hdf5File.closeGroup();

            // cell death report, one file per cell population
            hdf5File.createGroup("CellTypeDeath");
            i = 0;
            foreach (KeyValuePair<int, string> kvp in tsFiles.CellTypeDeath)
            {
                hdf5File.createGroup("CellTypeDeath_" + i);
                hdf5File.writeInt(kvp.Key, "Celltype");
                hdf5File.writeString("Filename", kvp.Value);
                hdf5File.closeGroup();
                i++;
            }
            hdf5File.closeGroup();

            // cell division report, one file per cell population
            hdf5File.createGroup("CellTypeDivision");
            i = 0;
            foreach (KeyValuePair<int, string> kvp in tsFiles.CellTypeDivision)
            {
                hdf5File.createGroup("CellTypeDivision_" + i);
                hdf5File.writeInt(kvp.Key, "Celltype");
                hdf5File.writeString("Filename", kvp.Value);
                hdf5File.closeGroup();
                i++;
            }
            hdf5File.closeGroup();

            // cell exit report, one file per cell population
            hdf5File.createGroup("CellTypeExit");
            i = 0;
            foreach (KeyValuePair<int, string> kvp in tsFiles.CellTypeExit)
            {
                hdf5File.createGroup("CellTypeExit_" + i);
                hdf5File.writeInt(kvp.Key, "Celltype");
                hdf5File.writeString("Filename", kvp.Value);
                hdf5File.closeGroup();
                i++;
            }
            hdf5File.closeGroup();
            // reporter files group close
            hdf5File.closeGroup();
        }

        /// <summary>
        /// read the reporter file names group from the file
        /// </summary>
        /// <param name="hdf5File">file to read from</param>
        public override void ReadReporterFileNamesFromHDF5(HDF5FileBase hdf5File)
        {
            string tmp = null;

            // reset file names here
            tsFiles.clearFileStrings();
            // open the parent group; this assumes the file is open and groups higher in the hierarchy have been opened already
            hdf5File.openGroup("ReporterFiles");
            // single files
            hdf5File.readString("ECMMeanReport", ref tmp);
            // a non-existing file got saved as "null" - convert that back to "" here
            tsFiles.ECMMeanReport = stringSafety(tmp);
            hdf5File.readString("ReactionsReport", ref tmp);
            tsFiles.ReactionsReport = stringSafety(tmp);

            // groups with potentially multiple files in them
            List<string> files;
            double dkey;
            int ikey;
            string file = null;

            // find the number of entries (files), then extract the key and file name string, and build the associated dictionaries
            files = hdf5File.subGroupNames("/Experiment/ReporterFiles/ECMReportStep");
            if (files.Count > 0)
            {
                hdf5File.openGroup("ECMReportStep");
                foreach (string group in files)
                {
                    hdf5File.openGroup(group);
                    dkey = hdf5File.readDouble("Timestep");
                    hdf5File.readString("Filename", ref file);
                    tsFiles.ECMReportStep.Add(dkey, file);
                    hdf5File.closeGroup();
                }
                hdf5File.closeGroup();
            }

            files = hdf5File.subGroupNames("/Experiment/ReporterFiles/CellTypeReport");
            if (files.Count > 0)
            {
                hdf5File.openGroup("CellTypeReport");
                foreach (string group in files)
                {
                    hdf5File.openGroup(group);
                    ikey = hdf5File.readInt("Celltype");
                    hdf5File.readString("Filename", ref file);
                    tsFiles.CellTypeReport.Add(ikey, file);
                    hdf5File.closeGroup();
                }
                hdf5File.closeGroup();
            }

            files = hdf5File.subGroupNames("/Experiment/ReporterFiles/CellTypeDeath");
            if (files.Count > 0)
            {
                hdf5File.openGroup("CellTypeDeath");
                foreach (string group in files)
                {
                    hdf5File.openGroup(group);
                    ikey = hdf5File.readInt("Celltype");
                    hdf5File.readString("Filename", ref file);
                    tsFiles.CellTypeDeath.Add(ikey, file);
                    hdf5File.closeGroup();
                }
                hdf5File.closeGroup();
            }

            files = hdf5File.subGroupNames("/Experiment/ReporterFiles/CellTypeDivision");
            if (files.Count > 0)
            {
                hdf5File.openGroup("CellTypeDivision");
                foreach (string group in files)
                {
                    hdf5File.openGroup(group);
                    ikey = hdf5File.readInt("Celltype");
                    hdf5File.readString("Filename", ref file);
                    tsFiles.CellTypeDivision.Add(ikey, file);
                    hdf5File.closeGroup();
                }
                hdf5File.closeGroup();
            }

            files = hdf5File.subGroupNames("/Experiment/ReporterFiles/CellTypeExit");
            if (files.Count > 0)
            {
                hdf5File.openGroup("CellTypeExit");
                foreach (string group in files)
                {
                    hdf5File.openGroup(group);
                    ikey = hdf5File.readInt("Celltype");
                    hdf5File.readString("Filename", ref file);
                    tsFiles.CellTypeExit.Add(ikey, file);
                    hdf5File.closeGroup();
                }
                hdf5File.closeGroup();
            }

            // reporter files group close
            hdf5File.closeGroup();
        }

        /// <summary>
        /// extracts and returns the track data for a cell
        /// </summary>
        /// <param name="cellID">the cell's id</param>
        /// <returns>track data with lists of times and matching positions</returns>
        public override CellTrackData ProvideCellTrackData(int cellID)
        {
            CellTrackData data = null;

            // does the cell exist?
            if(SimulationBase.dataBasket.Cells.ContainsKey(cellID) == true)
            {
                Cell c = SimulationBase.dataBasket.Cells[cellID];
                CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(c.Population_id);

                // is the needed data there?
                if (cellPop.report_xvf.position == true && tsFiles.CellTypeReport.ContainsKey(c.Population_id) == true)
                {
                    string file = tsFiles.CellTypeReport[c.Population_id],
                           path = hSim.HDF5FileHandle.FilePath,
                           line;
                    string[] parts;
                    int cell_id = 0, time = 0, pos_x = 0, pos_y = 0, pos_z = 0, total = 5, assigned = 0;
                    StreamReader stream = new StreamReader(path + file);

                    // read description
                    stream.ReadLine();
                    // read header
                    line = stream.ReadLine();
                    // find indices of interest
                    parts = line.Split();
                    for(int i = 0; i < parts.Length && assigned < total; i++)
                    {
                        if(parts[i] == "cell_id")
                        {
                            cell_id = i;
                            assigned++;
                        }
                        else if(parts[i] == "time")
                        {
                            time = i;
                            assigned++;
                        }
                        else if(parts[i] == "pos_x")
                        {
                            pos_x = i;
                            assigned++;
                        }
                        else if(parts[i] == "pos_y")
                        {
                            pos_y = i;
                            assigned++;
                        }
                        else if(parts[i] == "pos_z")
                        {
                            pos_z = i;
                            assigned++;
                        }
                    }
                    data = new CellTrackData(cellID);
                    while ((line = stream.ReadLine()) != null)
                    {
                        parts = line.Split('\t');
                        if (Convert.ToInt32(parts[cell_id]) == cellID)
                        {
                            data.Times.Add(Convert.ToDouble(parts[time]));
                            data.Positions.Add(new double[] { Convert.ToDouble(parts[pos_x]), Convert.ToDouble(parts[pos_y]), Convert.ToDouble(parts[pos_z]) });
                        }
                    }
                    stream.Close();
                }
            }
            if (data != null)
            {
                // sort by time
                data.Sort();
            }
            return data;
        }

        /// <summary>
        /// extract the cell population dynamics data
        /// </summary>
        /// <param name="pop">cell population we want to plot</param>
        /// <returns>an object with lists for the state names for processing and lists for the times and matching states</returns>
        public override CellPopulationDynamicsData ProvideCellPopulationDynamicsData(CellPopulation pop)
        {
            CellPopulationDynamicsData data = null;

            // is the needed data there?
            if ((pop.reportStates.Death == true || pop.reportStates.Differentiation == true || pop.reportStates.Division == true) &&
                tsFiles.CellTypeReport.ContainsKey(pop.cellpopulation_id) == true)
            {
                string file = tsFiles.CellTypeReport[pop.cellpopulation_id],
                       path = hSim.HDF5FileHandle.FilePath,
                       line;
                string[] parts;
                int time = 0, death = -1, diff = -1, div = -1, total = 1, assigned = 0,
                    ipart;
                double dtime;
                StreamReader stream = new StreamReader(path + file);

                // create the data
                data = new CellPopulationDynamicsData(pop);
                // we can only read out states that were reported
                if (pop.reportStates.Death == true)
                {
                    total++;
                }
                if (pop.reportStates.Differentiation == true)
                {
                    total++;
                }
                if (pop.reportStates.Division == true)
                {
                    total++;
                }
                // read description
                stream.ReadLine();
                // read header
                line = stream.ReadLine();
                // find indices of interest
                parts = line.Split();
                for (int i = 0; i < parts.Length && assigned < total; i++)
                {
                    if (parts[i] == "time")
                    {
                        time = i;
                        assigned++;
                    }
                    else if (parts[i] == "DeathState")
                    {
                        death = i;
                        assigned++;
                    }
                    else if (parts[i] == "DiffState")
                    {
                        diff = i;
                        assigned++;
                    }
                    else if (parts[i] == "DivState")
                    {
                        div = i;
                        assigned++;
                    }
                }

                // now read all remaining lines and process them / add up the dynamics values per cell
                while ((line = stream.ReadLine()) != null)
                {
                    parts = line.Split('\t');
                    dtime = Convert.ToDouble(parts[time]);
                    // add zero entries for this time if it is a new time (checked in the function)
                    data.AddSet(dtime);

                    // now the individual states
                    // death
                    if (death >= 0)
                    {
                        // the entry converted to int
                        ipart = Convert.ToInt32(parts[death]);
                        // increment that state
                        data.IncrementState(CellPopulationDynamicsData.State.DEATH, ipart, dtime);
                    }
                    // diff
                    if (diff >= 0)
                    {
                        // the entry converted to int
                        ipart = Convert.ToInt32(parts[diff]);
                        // increment that state
                        data.IncrementState(CellPopulationDynamicsData.State.DIFF, ipart, dtime);
                    }
                    // div
                    if (div >= 0)
                    {
                        // the entry converted to int
                        ipart = Convert.ToInt32(parts[div]);
                        // increment that state
                        data.IncrementState(CellPopulationDynamicsData.State.DIV, ipart, dtime);
                    }
                }
                stream.Close();
            }
            if (data != null)
            {
                // sort by the time
                data.Sort();
            }
            return data;
        }

        /// <summary>
        /// return a dictionary of founder cells, can be null if none exist or the needed reporting is off
        /// </summary>
        /// <returns></returns>
        public override Dictionary<int, FounderInfo> ProvideFounderCells()
        {
            Dictionary<int, FounderInfo> data = null;

            foreach (CellPopulation cp in ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulations)
            {
                // a founder cell needs these reporting files present; no need to look at reporting options, presence of files is sufficient in this case
                if (tsFiles.CellTypeReport.ContainsKey(cp.cellpopulation_id) == true &&
                    tsFiles.CellTypeDivision.ContainsKey(cp.cellpopulation_id) == true &&
                    tsFiles.CellTypeDeath.ContainsKey(cp.cellpopulation_id) == true &&
                    tsFiles.CellTypeExit.ContainsKey(cp.cellpopulation_id) == true)
                {
                    string file = tsFiles.CellTypeReport[cp.cellpopulation_id],
                           path = hSim.HDF5FileHandle.FilePath,
                           line;
                    string[] parts;
                    int time = 0, cell_id = 0, lineage_id = 0, total = 3, assigned = 0;
                    StreamReader stream = new StreamReader(path + file);

                    // read description
                    stream.ReadLine();
                    // read header
                    line = stream.ReadLine();
                    // find indices of interest
                    parts = line.Split();
                    for (int i = 0; i < parts.Length && assigned < total; i++)
                    {
                        if (parts[i] == "time")
                        {
                            time = i;
                            assigned++;
                        }
                        else if (parts[i] == "cell_id")
                        {
                            cell_id = i;
                            assigned++;
                        }
                        else if (parts[i] == "lineage_id")
                        {
                            lineage_id = i;
                            assigned++;
                        }
                    }
                    data = new Dictionary<int, FounderInfo>();
                    while ((line = stream.ReadLine()) != null)
                    {
                        parts = line.Split('\t');
                        if (Convert.ToDouble(parts[time]) == 0)
                        {
                            data.Add(Convert.ToInt32(parts[cell_id]), new FounderInfo(BigInteger.Parse(parts[lineage_id]), cp.cellpopulation_id));
                        }
                    }
                    stream.Close();
                }
            }

            return data;
        }

        /// <summary>
        /// extract the genealogy information for a founder cell
        /// </summary>
        /// <param name="founder">data for the founder cell</param>
        /// <returns>null on error, the dictionary of genealogy objects otherwise</returns>
        public override Dictionary<BigInteger, GeneologyInfo> ProvideGeneologyData(FounderInfo founder)
        {
            Dictionary<BigInteger, GeneologyInfo> data = new Dictionary<BigInteger, GeneologyInfo>();
            string file = tsFiles.CellTypeDivision[founder.Population_Id],
                   path = hSim.HDF5FileHandle.FilePath,
                   line;
            string[] parts;
            int time = 0, generation = 0, mother_lineage_id = 0, daughter1_lineage_id = 0, daughter2_lineage_id = 0, total = 5, assigned = 0;
            StreamReader stream = new StreamReader(path + file);

            // add the founder cell
            data.Add(founder.Lineage_Id, new GeneologyInfo(0, founder.Lineage_Id, 0));

            // process divisions
            // read description
            stream.ReadLine();
            // read header
            line = stream.ReadLine();
            // find indices of interest
            parts = line.Split();
            for (int i = 0; i < parts.Length && assigned < total; i++)
            {
                if (parts[i] == "time")
                {
                    time = i;
                    assigned++;
                }
                else if (parts[i] == "generation")
                {
                    generation = i;
                    assigned++;
                }
                else if (parts[i] == "mother_lineage_id")
                {
                    mother_lineage_id = i;
                    assigned++;
                }
                else if (parts[i] == "daughter1_lineage_id")
                {
                    daughter1_lineage_id = i;
                    assigned++;
                }
                else if (parts[i] == "daughter2_lineage_id")
                {
                    daughter2_lineage_id = i;
                    assigned++;
                }
            }

            List<DivisionContainer> divList = new List<DivisionContainer>();
            DivisionContainer div;

            // gather the division events first to sort them before usage
            while ((line = stream.ReadLine()) != null)
            {
                parts = line.Split('\t');
                div = new DivisionContainer();
                div.time = Convert.ToDouble(parts[time]);
                div.generation = Convert.ToInt32(parts[generation]);
                div.mother = BigInteger.Parse(parts[mother_lineage_id]);
                div.daughter1 = BigInteger.Parse(parts[daughter1_lineage_id]);
                div.daughter2 = BigInteger.Parse(parts[daughter2_lineage_id]);
                divList.Add(div);
            }
            stream.Close();
            // sort by time
            divList = divList.OrderBy(o => o.time).ToList();

            // now update existing GenealogyInfo objects (mother), add new ones (daughters)
            foreach(DivisionContainer dc in divList)
            {
                // an entry for this cell must exist
                if (data.ContainsKey(dc.mother) == true)
                {
                    GeneologyInfo entry = data[dc.mother];

                    // update the mother
                    entry.EventType = GeneologyInfo.GI_DIVIDE;
                    entry.EventTime = dc.time;
                    // create daughter 1
                    entry = new GeneologyInfo(dc.time, dc.daughter1, dc.generation);
                    data.Add(dc.daughter1, entry);
                    // create daughter 2
                    entry = new GeneologyInfo(dc.time, dc.daughter2, dc.generation);
                    data.Add(dc.daughter2, entry);
                }
                else
                {
                    return null;
                }
            }

            // process deaths
            int lineage_id = 0;

            file = tsFiles.CellTypeDeath[founder.Population_Id];
            total = 2;
            assigned = 0;
            stream = new StreamReader(path + file);

            // read description
            stream.ReadLine();
            // read header
            line = stream.ReadLine();
            // find indices of interest
            parts = line.Split();
            for (int i = 0; i < parts.Length && assigned < total; i++)
            {
                if (parts[i] == "time")
                {
                    time = i;
                    assigned++;
                }
                else if (parts[i] == "lineage_id")
                {
                    lineage_id = i;
                    assigned++;
                }
            }

            List<DeathExitContainer> deathExitList = new List<DeathExitContainer>();
            DeathExitContainer deathExit;

            // gather the death events first to sort them before usage
            while ((line = stream.ReadLine()) != null)
            {
                parts = line.Split('\t');
                deathExit = new DeathExitContainer();
                deathExit.time = Convert.ToDouble(parts[time]);
                deathExit.lineage = BigInteger.Parse(parts[lineage_id]);
                deathExitList.Add(deathExit);
            }
            stream.Close();
            // sort by time
            deathExitList = deathExitList.OrderBy(o => o.time).ToList();

            // update existing GenealogyInfo objects
            foreach(DeathExitContainer dec in deathExitList)
            {
                // an entry for this cell must exist
                if (data.ContainsKey(dec.lineage) == true)
                {
                    GeneologyInfo entry = data[dec.lineage];

                    // update the existing cell
                    entry.EventType = GeneologyInfo.GI_DIE;
                    entry.EventTime = dec.time;
                }
                else
                {
                    return null;
                }
            }

            // process exits
            file = tsFiles.CellTypeExit[founder.Population_Id];
            total = 2;
            assigned = 0;
            stream = new StreamReader(path + file);

            // read description
            stream.ReadLine();
            // read header
            line = stream.ReadLine();
            // find indices of interest
            parts = line.Split();
            for (int i = 0; i < parts.Length && assigned < total; i++)
            {
                if (parts[i] == "time")
                {
                    time = i;
                    assigned++;
                }
                else if (parts[i] == "lineage_id")
                {
                    lineage_id = i;
                    assigned++;
                }
            }

            deathExitList.Clear();

            // gather the exit events first to sort them before usage
            while ((line = stream.ReadLine()) != null)
            {
                parts = line.Split('\t');
                deathExit = new DeathExitContainer();
                deathExit.time = Convert.ToDouble(parts[time]);
                deathExit.lineage = BigInteger.Parse(parts[lineage_id]);
                deathExitList.Add(deathExit);
            }
            stream.Close();
            // sort by time
            deathExitList = deathExitList.OrderBy(o => o.time).ToList();

            // update existing GenealogyInfo objects
            foreach(DeathExitContainer dec in deathExitList)
            {
                // an entry for this cell must exist
                if (data.ContainsKey(dec.lineage) == true)
                {
                    GeneologyInfo entry = data[dec.lineage];

                    // update the existing cell
                    entry.EventType = GeneologyInfo.GI_EXIT;
                    entry.EventTime = dec.time;
                }
                else
                {
                    return null;
                }
            }

            return data;
        }
   }

    public class CompartmentMolpopReporter
    {
        public List<double> listTimes;
        public Dictionary<string, List<double>> dictGraphConcs;
        private Compartment comp;
        private double[] defaultLoc;

        public CompartmentMolpopReporter()
        {
            listTimes = new List<double>();
            dictGraphConcs = new Dictionary<string, List<double>>();
        }

        public void StartCompReporter(Compartment _comp, double[] _defaultLoc, ScenarioBase scenario)
        {
            defaultLoc = (double[])_defaultLoc.Clone();
            comp = _comp;
            dictGraphConcs.Clear();
            listTimes.Clear();

            foreach (ConfigMolecularPopulation configMolPop in ((VatReactionComplexScenario)scenario).AllMols)
            {
                if (configMolPop.report_mp.mp_extended == ExtendedReport.LEAN)
                {
                    if (comp.Populations.ContainsKey(configMolPop.molecule.entity_guid))
                    {
                        dictGraphConcs.Add(configMolPop.molecule.entity_guid, new List<double>());
                    }
                }
            }
        }

        public void AppendReporter(double accumulatedTime)
        {
            listTimes.Add(accumulatedTime);
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                if (dictGraphConcs.ContainsKey(kvp.Key))
                {
                    dictGraphConcs[kvp.Key].Add(comp.Populations[kvp.Key].Conc.Value(defaultLoc));
                }
            }
        }

        public void CloseCompReporter()
        {
            dictGraphConcs.Clear();
            listTimes.Clear();            
        }
    }

    public class VatReactionComplexReporterFiles : SimulationReporterFiles
    {
        public string VatRCReport { get; set; }
        public string ReactionsReport { get; set; }

        public VatReactionComplexReporterFiles()
        {
            clearFileStrings();
        }

        public override void clearFileStrings()
        {
            VatRCReport = "";
            ReactionsReport = "";
        }
    }

    public class VatReactionComplexReporter : ReporterBase
    {
        private VatReactionComplex hSim;
        private StreamWriter vat_conc_file;
        private CompartmentMolpopReporter compMolpopReporter;
        private VatReactionComplexReporterFiles vatRCfiles;
        public bool reportOn;

        public VatReactionComplexReporter(SimulationBase sim)
        {
            if (sim is VatReactionComplex == false)
            {
                throw new InvalidCastException();
            }

            hSim = sim as VatReactionComplex;
            compMolpopReporter = new CompartmentMolpopReporter();
            vatRCfiles = new VatReactionComplexReporterFiles();
            reportOn = false;
        }

        public override void StartReporter(string protocolFileName)
        {
            NeedsFileNameWrite = true;
            vatRCfiles.clearFileStrings();
            createUniqueFolderName(protocolFileName, reportOn == false);

            if (reportOn == false)
            {
                return;
            }

            startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            CloseReporter();
            compMolpopReporter.StartCompReporter(SimulationBase.dataBasket.Environment.Comp, new double[] { 0.0, 0.0, 0.0 }, SimulationBase.ProtocolHandle.scenario);

            ReactionsReport();
        }

        public override void AppendReporter()
        {
            if (reportOn == false)
            {
                return;
            }

            compMolpopReporter.AppendReporter(hSim.AccumulatedTime);
        }

        public override void CloseReporter()
        {
            if (compMolpopReporter.listTimes.Count > 0)
            {
                WriteToFile();
                reportOn = false;
            }

            if (vat_conc_file != null)
            {
                vat_conc_file.Close();
                vat_conc_file = null;
                compMolpopReporter.CloseCompReporter();
            }
        }

        private void WriteToFile()
        {
            string header = "time";
            bool create = false;

            // header
            foreach (ConfigMolecularPopulation configMolPop in ((VatReactionComplexScenario)SimulationBase.ProtocolHandle.scenario).AllMols)
            {
                if (configMolPop.report_mp.mp_extended == ExtendedReport.LEAN)
                {
                    if (SimulationBase.dataBasket.Environment.Comp.Populations.ContainsKey(configMolPop.molecule.entity_guid))
                    {
                        header += "\t" + SimulationBase.ProtocolHandle.entity_repository.molecules_dict[configMolPop.molecule.entity_guid].Name;
                        create = true;
                    }
                }
            }

            // was at least one molecule selected?
            if (create == false)
            {
                return;
            }

            vat_conc_file = createStreamWriter("vatRC_report", "txt");
            vatRCfiles.VatRCReport = fileNameAssembled;
            vat_conc_file.WriteLine("VatRC report from {0} run on {1}.", SimulationBase.ProtocolHandle.experiment_name, startTime);
            vat_conc_file.WriteLine(header);

            // write simulation data
            for (int i = 0; i < compMolpopReporter.listTimes.Count; i++)
            {
                vat_conc_file.Write(compMolpopReporter.listTimes[i]);

                foreach (KeyValuePair<string,List<double>> kvp in compMolpopReporter.dictGraphConcs)
                {
                        // mean concentration of this compartment molecular population
                        vat_conc_file.Write("\t{0:G4}", kvp.Value[i]);
                }
                // terminate line
                vat_conc_file.WriteLine();
            }
        }

        public override void AppendDeathEvent(Cell cell)
        {
            throw new NotImplementedException();
        }

        public override void AppendDivisionEvent(Cell cell, Cell daughter)
        {
            throw new NotImplementedException();
        }

        public override void AppendExitEvent(Cell cell)
        {
            throw new NotImplementedException();
        }

        public override void ReactionsReport()
        {
            if (SimulationBase.ProtocolHandle.scenario.reactionsReport == true)
            {
                StreamWriter writer = createStreamWriter("reactions_report.txt", "txt");

                vatRCfiles.ReactionsReport = fileNameAssembled;
                writer.WriteLine("Reactions from {0} run on {1}.", SimulationBase.ProtocolHandle.experiment_name, startTime);
                writer.WriteLine("rate constant\treaction");
                writer.WriteLine();

                WriteReactionsList(writer, SimulationBase.ProtocolHandle.scenario.environment.comp.Reactions.ToList());
                foreach (ConfigReactionComplex crc in SimulationBase.ProtocolHandle.scenario.environment.comp.reaction_complexes)
                {
                    writer.WriteLine();
                    writer.WriteLine("Reaction Complex: {0}", crc.Name);
                    writer.WriteLine();
                    WriteReactionsList(writer, crc.reactions.ToList());
                }
                writer.WriteLine();

                writer.Close();
            }
        }

        /// <summary>
        /// write the reporter files group to file
        /// </summary>
        /// <param name="hdf5File">the file to write the data to</param>
        public override void WriteReporterFileNamesToHDF5(HDF5FileBase hdf5File)
        {
            // only write once
            if (NeedsFileNameWrite == false)
            {
                return;
            }
            NeedsFileNameWrite = false;

            // create group
            hdf5File.createGroup("ReporterFiles");
            // the vatRC has only single files
            hdf5File.writeString("VatRCReport", stringSafety(vatRCfiles.VatRCReport));
            hdf5File.writeString("ReactionsReport", stringSafety(vatRCfiles.ReactionsReport));
            hdf5File.closeGroup();
        }

        /// <summary>
        /// read the reporter file names group from the file
        /// </summary>
        /// <param name="hdf5File">file to read from</param>
        public override void ReadReporterFileNamesFromHDF5(HDF5FileBase hdf5File)
        {
            string tmp = null;

            // reset file names here
            vatRCfiles.clearFileStrings();
            // parent group
            hdf5File.openGroup("ReporterFiles");
            // the vatRC has only single files
            hdf5File.readString("VatRCReport", ref tmp);
            // a non-existing file got saved as "null" - convert that back to ""
            vatRCfiles.VatRCReport = stringSafety(tmp);
            hdf5File.readString("ReactionsReport", ref tmp);
            vatRCfiles.ReactionsReport = stringSafety(tmp);
            hdf5File.closeGroup();
        }

    }

    public class TransitionEventReporter
    {
        /// <summary>
        /// Reporting on an event type.
        /// Data are written to file at the end of the simulation.
        /// </summary>        
        private StreamWriter writer;

        private List<TransitionEvent> events;

        public TransitionEventReporter(StreamWriter _writer)
        {
            writer = _writer;
            events = new List<TransitionEvent>();
        }

        public void AddEvent(TransitionEvent te)
        {
            events.Add(te);
        }

        public void WriteEvents()
        {
            if (events.Count > 0)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    ((TransitionEvent)events[i]).WriteLine(writer);
                }
            }

            CloseReporter();
        }

        public void CloseReporter()
        {
            if (writer != null)
            {
                writer.Close();
            }
            if (events != null)
            {
                events.Clear();
            }
        }
    }

    /// <summary>
    /// Base class for recording cell transition events
    /// </summary>
    public class TransitionEvent
    {
        public double time;
        public int cell_id;
        BigInteger lineage_id;

        public TransitionEvent(double _time, Cell cell)
        {
            time = _time;
            cell_id = cell.Cell_id;
            lineage_id = cell.Lineage_id;
        }

        public virtual void WriteLine(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}", cell_id, time, lineage_id);
        }
    }

    /// <summary>
    /// Record cell division events
    /// </summary>
    public class DivisionEvent : TransitionEvent
    {
        public int daughter_id;
        public int generation;
        BigInteger mother_lineage_id, daughter1_lineage_id, daugher2_lineage_id;

        public DivisionEvent(double _time, Cell cell, Cell daughter)
            : base(_time, cell)
        {
            daughter_id = daughter.Cell_id;
            generation = cell.generation;
            mother_lineage_id = cell.Lineage_id / 2;
            daughter1_lineage_id = cell.Lineage_id;
            daugher2_lineage_id = daughter.Lineage_id;
        }

        public override void WriteLine(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", cell_id, time, daughter_id, generation, mother_lineage_id, daughter1_lineage_id, daugher2_lineage_id);
        }
    }

    public class CellPopulationDynamicsData : ReporterData
    {
        public enum State { DEATH, DIV, DIFF };
        public List<double> Times { get; private set; }
        private Dictionary<int, List<int>> deathStates;
        private Dictionary<int, List<int>> divStates;
        private Dictionary<int, List<int>> diffStates;
        private Dictionary<double, int> times_dict;


        /// <summary>
        /// constructor creates the data structures to be filled for a population's dynamics plot
        /// </summary>
        /// <param name="pop">the population to be plotted</param>
        public CellPopulationDynamicsData(CellPopulation pop)
        {
            // read out the possible states in this population
            ConfigCell cell = pop.Cell;
            int i;

            Times = new List<double>();
            times_dict = new Dictionary<double, int>();
            
            // death
            if (pop.reportStates.Death == true)
            {
                deathStates = new Dictionary<int, List<int>>();
                for (i = 0; i < cell.death_driver.states.Count; i++)
                {
                    deathStates.Add(i, new List<int>());
                }
            }
            // diff
            if (pop.reportStates.Differentiation == true)
            {
                diffStates = new Dictionary<int, List<int>>();
                for (i = 0; i < cell.diff_scheme.Driver.states.Count; i++)
                {
                    diffStates.Add(i, new List<int>());
                }
            }
            // div
            if (pop.reportStates.Division == true)
            {
                divStates = new Dictionary<int, List<int>>();
                for (i = 0; i < cell.div_scheme.Driver.states.Count; i++ )
                {
                    divStates.Add(i, new List<int>());
                }
            }
        }

        /// <summary>
        /// add a zeroed set of states at a given time
        /// </summary>
        /// <param name="time">time step value</param>
        public void AddSet(double time)
        {
            // only append one set per timestep
            if (times_dict.ContainsKey(time) == true)
            {
                return;
            }

            // add the time
            Times.Add(time);
            // use a dictionary for fast lookup, save the list index as value;
            // it will serve to access the correct state
            times_dict.Add(time, Times.Count - 1);

            // add a counter for each state, set to zero

            //skg added ifs 6/3/15
            if (deathStates != null)
            {
                foreach (List<int> l in deathStates.Values)
                {
                    l.Add(0);
                }
            }
            if (diffStates != null)
            {
                foreach (List<int> l in diffStates.Values)
                {
                    l.Add(0);
                }
            }
            if (divStates != null)
            {
                foreach (List<int> l in divStates.Values)
                {
                    l.Add(0);
                }
            }
        }

        /// <summary>
        /// increment the last entry for the state with the given index
        /// </summary>
        /// <param name="state">enum identifying the state</param>
        /// <param name="index">state index</param>
        /// <param name="time">the current time</param>
        public void IncrementState(State state, int index, double time)
        {
            if (state == State.DEATH)
            {
                deathStates[index][times_dict[time]]++;
            }
            else if (state == State.DIFF)
            {
                diffStates[index][times_dict[time]]++;
            }
            else if (state == State.DIV)
            {
                divStates[index][times_dict[time]]++;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// retrieve the series for a state
        /// </summary>
        /// <param name="state">enum identifying the state</param>
        /// <param name="name">state index</param>
        public List<int> GetState(State state, int index)
        {
            if (state == State.DEATH)
            {
                return deathStates[index];
            }
            else if (state == State.DIFF)
            {
                return diffStates[index];
            }
            else if (state == State.DIV)
            {
                return divStates[index];
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// do a selection sort to make sure the data is sorted by the time
        /// </summary>
        public void Sort()
        {
            int itmp, minloc;
            double dtmp, min;

            for (int i = 0; i < Times.Count - 1; i++)
            {
                // assume min is in starting position
                min = Times[i];
                minloc = i;
                // find the minimum's location
                for (int j = i + 1; j < Times.Count; j++)
                {
                    if (Times[j] < min)
                    {
                        min = Times[j];
                        minloc = j;
                    }
                }
                // swap if needed
                if (minloc != i)
                {
                    // times
                    dtmp = Times[i];
                    Times[i] = Times[minloc];
                    Times[minloc] = dtmp;
                    // states
                    foreach (List<int> list in deathStates.Values)
                    {
                        itmp = list[i];
                        list[i] = list[minloc];
                        list[minloc] = itmp;
                    }
                    foreach (List<int> list in divStates.Values)
                    {
                        itmp = list[i];
                        list[i] = list[minloc];
                        list[minloc] = itmp;
                    }
                    foreach (List<int> list in diffStates.Values)
                    {
                        itmp = list[i];
                        list[i] = list[minloc];
                        list[minloc] = itmp;
                    }
                }
            }
        }
    }

    /// <summary>
    /// class encapsulating the information needed for the founder cells
    /// </summary>
    public class FounderInfo
    {
        public BigInteger Lineage_Id { get; set; }
        public int  Population_Id { get; set; }

        //public BigInteger Lineage_Id;
        //public int Population_Id;

        public FounderInfo(BigInteger lineage_id, int population_id)
        {
            Lineage_Id = lineage_id;
            Population_Id = population_id;
        }
    }

    /// <summary>
    /// class encapsulating the genealogy information for lineage analysis
    /// </summary>
    public class GeneologyInfo
    {
        // lineage id of the mother cell before division
        public BigInteger Lineage_Id;
        // the number of divisions to reach this current cell
        public int Generation;
        // simulation clock time for this event
        public double EventTime;
        // simulation clock time for birth time
        public double BirthTime;
        // constant values defined in this class
        public int EventType;
        // not in use, yet
        public double IgAffinity;
        // not in use, yet
        public double ExpectedMutations;
        // constants
        public static int GI_BIRTH = 0,
                          GI_DIE = 1,
                          GI_EXIT = 2,
                          GI_DIVIDE = 3;

        /// <summary>
        /// constructor with parameters all event types have in common
        /// </summary>
        /// <param name="tBirth">birth time</param>
        /// <param name="lineage_id">lineage id</param>
        /// <param name="generation">generation</param>
        public GeneologyInfo(double tBirth, BigInteger lineage_id, int generation)
        {
            Lineage_Id = lineage_id;
            Generation = generation;
            EventTime = -1;
            EventType = GI_BIRTH;
            BirthTime = tBirth;
            IgAffinity = 0;
        }

        /// <summary>
        /// return the relative event time
        /// </summary>
        /// <returns></returns>
        public double RelativeEventTime()
        {
            return EventTime - BirthTime;
        }

        /// <summary>
        /// return the daughter lineage id
        /// </summary>
        /// <param name="daughter">daughter 0 or 1</param>
        /// <returns>daughter's lineage id</returns>
        public BigInteger Daughter(int daughter)
        {
            if (daughter != 0)
            {
                daughter = 1;
            }
            return Lineage_Id * 2 + daughter;
        }

        /// <summary>
        /// true if the object represents a division
        /// </summary>
        /// <returns>true for division</returns>
        public bool IsDivision()
        {
            return EventType == GI_DIVIDE;
        }
    }

    /// <summary>
    /// save division events for sorting
    /// </summary>
    public class DivisionContainer
    {
        public double time;
        public int generation;
        public BigInteger mother, daughter1, daughter2;

        public DivisionContainer()
        {
        }
    }

    /// <summary>
    /// save death and exit events for sorting
    /// </summary>
    public class DeathExitContainer
    {
        public double time;
        public BigInteger lineage;

        public DeathExitContainer()
        {
        }
    }
}
