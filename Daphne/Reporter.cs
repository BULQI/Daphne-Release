using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

using ManifoldRing;
using System.Globalization;

namespace Daphne
{
    public abstract class ReporterBase
    {
        protected DateTime startTime;
        protected string fileName;
        public string AppPath { get; set; } // non uri

        public ReporterBase()
        {
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        protected StreamWriter createStreamWriter(string file, string extension)
        {
            int version = 1;
            string nameStart,
                   fullPath;

            if (fileName == "")
            {
                nameStart = startTime.Month + "." + startTime.Day + "." + startTime.Year + "_" + startTime.Hour + "h" + startTime.Minute + "m" + startTime.Second + "s_";
            }
            else
            {
                nameStart = fileName + "_";
            }

            fullPath = AppPath + nameStart + file + "." + extension;

            do
            {
                if (File.Exists(fullPath) == true)
                {
                    fullPath = AppPath + nameStart + "_" + file + "(" + version + ")." + extension;
                    version++;
                }
                else
                {
                    if (AppPath != "" && Directory.Exists(AppPath) == false)
                    {
                        Directory.CreateDirectory(AppPath);
                    }
                    return File.CreateText(fullPath);
                }
            } while (true);
        }

        public abstract void StartReporter(SimulationBase sim);
        public abstract void AppendReporter();
        public abstract void CloseReporter();

        public abstract void AppendDeathEvent(int cell_id, int cellpop_id);
        public abstract void AppendDivisionEvent(int cell_id, int cellpop_id, int daughter_id);
        public abstract void AppendExitEvent(int cell_id, int cellpop_id);
    }

    public class TissueSimulationReporter : ReporterBase
    {
        private StreamWriter ecm_mean_file;
        private Dictionary<int, StreamWriter> cell_files;
        private TissueSimulation hSim;
        private Dictionary<int, TransitionEventReporter> deathEvents;
        private Dictionary<int, TransitionEventReporter> divisionEvents;
        private Dictionary<int, TransitionEventReporter> exitEvents;

        public TissueSimulationReporter()
        {
            cell_files = new Dictionary<int, StreamWriter>();
            deathEvents = new Dictionary<int, TransitionEventReporter>();
            divisionEvents = new Dictionary<int, TransitionEventReporter>();
            exitEvents = new Dictionary<int, TransitionEventReporter>();
        }

        public override void StartReporter(SimulationBase sim)
        {
            if (sim is TissueSimulation == false)
            {
                throw new InvalidCastException();
            }

            hSim = sim as TissueSimulation;
            startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            CloseReporter();
            startECM();
            startCells();
            startEvents();
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
                string header = "cell_id\ttime";
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
                    // cell_id time
                    cell_files[cp.cellpopulation_id].Write("{0}\t{1}", c.Cell_id, hSim.AccumulatedTime);

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
                    writer.WriteLine("Cell {0} death events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine("cell_id\ttime");
                    deathEvents.Add(cp.cellpopulation_id, new TransitionEventReporter(writer));
                }
                if (cp.reportStates.Division == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_divisionEvents", "txt");
                    writer.WriteLine("Cell {0} division events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);                
                    writer.WriteLine("cell_id\ttime\tdaughter_id");
                    divisionEvents.Add(cp.cellpopulation_id, new TransitionEventReporter(writer));
                }
                if (cp.reportStates.Exit == true)
                {
                    StreamWriter writer = createStreamWriter("cell_type" + cp.cellpopulation_id + "_exitEvents", "txt");
                    writer.WriteLine("Cell {0} exit events from {1} run on {2}.", cp.Cell.CellName, SimulationBase.ProtocolHandle.experiment_name, startTime);
                    writer.WriteLine("cell_id\ttime");
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

        public override void AppendDeathEvent(int cell_id, int cellpop_id)
        {
            if (deathEvents != null)
            {
                if (deathEvents.ContainsKey(cellpop_id))
                {
                    deathEvents[cellpop_id].AddEvent(new TransitionEvent(hSim.AccumulatedTime, cell_id));
                }
            }
        }

        public override void AppendDivisionEvent(int cell_id, int cellpop_id, int daughter_id)
        {
            if (divisionEvents != null)
            {
                if (divisionEvents.ContainsKey(cellpop_id))
                {
                    divisionEvents[cellpop_id].AddEvent(new DivisionEvent(hSim.AccumulatedTime, cell_id, daughter_id));
                }
            }
        }

        public override void AppendExitEvent(int cell_id, int cellpop_id)
        {
            if (exitEvents != null)
            {
                if (exitEvents.ContainsKey(cellpop_id))
                {
                    exitEvents[cellpop_id].AddEvent(new TransitionEvent(hSim.AccumulatedTime, cell_id));
                }
            }
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

    public class VatReactionComplexReporter : ReporterBase
    {
        private VatReactionComplex hSim;
        private StreamWriter vat_conc_file;
        private CompartmentMolpopReporter compMolpopReporter;
        public bool reportOn;

        public VatReactionComplexReporter()
        {
            compMolpopReporter = new CompartmentMolpopReporter();
            reportOn = false;
        }

        public override void StartReporter(SimulationBase sim)
        {
            if (reportOn == false)
            {
                return;
            }

            if (sim is VatReactionComplex == false)
            {
                throw new InvalidCastException();
            }

            hSim = sim as VatReactionComplex;

            startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            CloseReporter();
            compMolpopReporter.StartCompReporter(SimulationBase.dataBasket.Environment.Comp,
                                        new double[] { 0.0, 0.0, 0.0 },
                                        SimulationBase.ProtocolHandle.scenario);
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

        public override void AppendDeathEvent(int cell_id, int cellpop_id)
        {
            throw new NotImplementedException();
        }

        public override void AppendDivisionEvent(int mother_id, int cellpop_id, int daughter_id)
        {
            throw new NotImplementedException();
        }

        public override void AppendExitEvent(int cell_id, int cellpop_id)
        {
            throw new NotImplementedException();
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

        public TransitionEvent(double _time, int _cell_id)
        {
            time = _time;
            cell_id = _cell_id;
        }

        public virtual void WriteLine(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}", this.cell_id, this.time);
        }
    }

    /// <summary>
    /// Record cell division events
    /// </summary>
    public class DivisionEvent : TransitionEvent
    {
        public int daughter_id;

        public DivisionEvent(double _time, int _cell_id, int _daughter_id)
            : base(_time, _cell_id)
        {
            daughter_id = _daughter_id;
        }

        public override void WriteLine(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}", this.cell_id, this.time, this.daughter_id);
        }
    }
}
