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
        protected string reportFolder, fileName;
        public string AppPath { get; set; } // non uri

        public ReporterBase()
        {
            reportFolder = "";
        }

        public string ReportFolder
        {
            get { return reportFolder; }
            set { reportFolder = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        protected StreamWriter createStreamWriter(string file, string extension)
        {
            int version = 1;
            string rootPath = reportFolder,
                   nameStart,
                   fullPath;

            if (rootPath != "")
            {
                rootPath += @"\";
            }
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
                    if (rootPath != "" && Directory.Exists(rootPath) == false)
                    {
                        Directory.CreateDirectory(rootPath);
                    }
                    return File.CreateText(fullPath);
                }
            } while (true);
        }

        public abstract void StartReporter(SimulationBase sim);
        public abstract void AppendReporter();
        public abstract void CloseReporter();
    }

    public class TissueSimulationReporter : ReporterBase
    {
        private StreamWriter ecm_mean_file;
        private Dictionary<int, StreamWriter> cell_files;
        private TissueSimulation hSim;

        public TissueSimulationReporter()
        {
            cell_files = new Dictionary<int, StreamWriter>();
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
                    if (cp.Cell.div_scheme.Driver.states.Count > 0)
                    {
                        header += "\tDivState";
                        create = true;
                    }
                }
                if (cp.reportStates.Death == true)
                {
                    header += "\tDeathState";
                    create = true;
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

                    if (cp.reportStates.Differentiation == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.Differentiator.CurrentState);
                    }
                    //if (cp.reportStates.Division)
                    //{
                    //    cell_files[cp.cellpopulation_id].Write("\t{0}", c.Divider.CurrentState);
                    //}
                    if (cp.reportStates.Death)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}", c.DeathBehavior.CurrentState);
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
    }

    public class VatReactionComplexReporter : ReporterBase
    {
        private VatReactionComplex hSim;

        public VatReactionComplexReporter()
        {
        }

        public override void StartReporter(SimulationBase sim)
        {
            if (sim is VatReactionComplex == false)
            {
                throw new InvalidCastException();
            }

            hSim = sim as VatReactionComplex;
            hSim.DictGraphConcs.Clear();
            hSim.ListTimes.Clear();

            Compartment comp = SimulationBase.dataBasket.Environment.Comp;

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                hSim.DictGraphConcs.Add(kvp.Key, new List<double>());
            }
        }

        private void appendTimesAndConcs()
        {
            double[] defaultLoc = { 0.0, 0.0, 0.0 };
            Compartment comp = SimulationBase.dataBasket.Environment.Comp;

            hSim.ListTimes.Add(hSim.AccumulatedTime);
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                hSim.DictGraphConcs[kvp.Key].Add(comp.Populations[kvp.Key].Conc.Value(defaultLoc));
            }
        }

        public override void AppendReporter()
        {
            appendTimesAndConcs();
        }

        public override void CloseReporter()
        {
        }
    }
}
