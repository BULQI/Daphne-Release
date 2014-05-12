using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

using ManifoldRing;

namespace Daphne
{
    public class Reporter
    {
        private StreamWriter ecm_mean_file;
        private Dictionary<int, StreamWriter> cell_files;
        private DateTime startTime;
        private string reportFolder;

        public Reporter()
        {
            cell_files = new Dictionary<int, StreamWriter>();
            reportFolder = "";
        }

        public string ReportFolder
        {
            get { return reportFolder; }
            set { reportFolder = value; }
        }

        public void StartReporter(SimConfiguration sc)
        {
            startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            CloseReporter();
            startECM(sc);
            startCells(sc);
        }

        public void AppendReporter(SimConfiguration sc, Simulation sim)
        {
            appendECM(sc, sim);
            appendCells(sc, sim);
        }

        public void CloseReporter()
        {
            closeECM();
            closeCells();
        }

        private StreamWriter createStreamWriter(string file, string extension)
        {
            int version = 1;
            string rootPath = reportFolder,
                   timeStamp,
                   fullPath;
            
            if(rootPath != "")
            {
                rootPath += @"\";
            }
            timeStamp = startTime.Month + "." + startTime.Day + "." + startTime.Year + "_" + startTime.Hour + "h" + startTime.Minute + "m" + startTime.Second + "s_";
            fullPath = rootPath + timeStamp + file + "." + extension;

            do
            {
                if (File.Exists(fullPath) == true)
                {
                    fullPath = rootPath + timeStamp + "_" + file + "(" + version + ")." + extension;
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

        private void startECM(SimConfiguration sc)
        {
            string header = "time";
            bool create = false;

            // mean
            foreach (ConfigMolecularPopulation c in sc.scenario.environment.ecs.molpops)
            {
                if (((ReportECM)c.report_mp).mean == true)
                {
                    header += "\t" + sc.entity_repository.molecules_dict[c.molecule_guid_ref].Name;
                    create = true;
                }
            }
            // was at least one molecule selected?
            if(create == true)
            {
                ecm_mean_file = createStreamWriter("ecm_mean_report", "txt");
                ecm_mean_file.WriteLine("ECM mean report from {0} run on {1}.", sc.experiment_name, startTime);
                ecm_mean_file.WriteLine(header);
            }
        }

        private void appendECM(SimConfiguration sc, Simulation sim)
        {
            // mean
            if (ecm_mean_file != null)
            {
                // simulation time
                ecm_mean_file.Write(sim.AccumulatedTime);
                foreach (ConfigMolecularPopulation c in sc.scenario.environment.ecs.molpops)
                {
                    if (((ReportECM)c.report_mp).mean == true)
                    {
                        // mean concentration of this ecm molecular population
                        ecm_mean_file.Write("\t{0}", Simulation.dataBasket.ECS.Space.Populations[c.molecule_guid_ref].Conc.MeanValue());
                    }
                }
                // terminate line
                ecm_mean_file.WriteLine();
            }

            // extended
            foreach (ConfigMolecularPopulation c in sc.scenario.environment.ecs.molpops)
            {
                if (c.report_mp.mp_extended > ExtendedReport.NONE)
                {
                    string name = sc.entity_repository.molecules_dict[c.molecule_guid_ref].Name;
                    StreamWriter writer = createStreamWriter("ecm_" + name + "_report_step" + sim.AccumulatedTime, "txt");
                    string header = "x\ty\tz\tconc\tgradient_x\tgradient_y\tgradient_z";

                    writer.WriteLine("ECM {0} report at {1}min from {2} run on {3}.", name, sim.AccumulatedTime, sc.experiment_name, startTime);
                    writer.WriteLine(header);

                    InterpolatedRectangularPrism prism = (InterpolatedRectangularPrism)Simulation.dataBasket.ECS.Space.Interior;
                    MolecularPopulation mp = Simulation.dataBasket.ECS.Space.Populations[c.molecule_guid_ref];

                    for (int i = 0; i < prism.ArraySize; i++)
                    {
                        double[] pos = prism.linearIndexToLocal(i);

                        writer.Write("{0}\t{1}\t{2}\t{3}", pos[0], pos[1], pos[2], mp.Conc.Value(pos));

                        // gradient
                        if (c.report_mp.mp_extended == ExtendedReport.COMPLETE)
                        {
                            double[] grad = mp.Conc.Gradient(pos);

                            writer.Write("\t{0}\t{1}\t{2}", grad[0], grad[1], grad[2]);
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
            }
        }

        private void startCells(SimConfiguration sc)
        {
            // create a file stream for each cell population
            foreach (CellPopulation cp in sc.scenario.cellpopulations)
            {
                string header = "time\tcell_id";
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

                // cell molpop concentrations
                for (int i = 0; i < 2; i++)
                {
                    // 0: cytosol, 1: membrane
                    ConfigCompartment comp = (i == 0) ? sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol : sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane;

                    foreach (ConfigMolecularPopulation mp in comp.molpops)
                    {
                        string name = sc.entity_repository.molecules_dict[mp.molecule_guid_ref].Name;

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
                foreach (ConfigMolecularPopulation mp in sc.scenario.environment.ecs.molpops)
                {
                    string name = sc.entity_repository.molecules_dict[mp.molecule_guid_ref].Name;

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

                    writer.WriteLine("Cell {0} report from {1} run on {2}.", sc.entity_repository.cells_dict[cp.cell_guid_ref].CellName, sc.experiment_name, startTime);
                    writer.WriteLine(header);
                    cell_files.Add(cp.cellpopulation_id, writer);
                }
            }
        }

        private void appendCells(SimConfiguration sc, Simulation sim)
        {
            // create a file stream for each cell population
            foreach (CellPopulation cp in sc.scenario.cellpopulations)
            {
                if (cell_files.ContainsKey(cp.cellpopulation_id) == false)
                {
                    continue;
                }

                foreach (Cell c in Simulation.dataBasket.Populations[cp.cellpopulation_id].Values)
                {
                    // time cell_id
                    cell_files[cp.cellpopulation_id].Write("{0}\t{1}", sim.AccumulatedTime, c.Cell_id);

                    if (cp.report_xvf.position == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}\t{1}\t{2}", c.State.X[0], c.State.X[1], c.State.X[2]);
                    }
                    if (cp.report_xvf.velocity == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}\t{1}\t{2}", c.State.V[0], c.State.V[1], c.State.V[2]);
                    }
                    if (cp.report_xvf.force == true)
                    {
                        cell_files[cp.cellpopulation_id].Write("\t{0}\t{1}\t{2}", c.State.F[0], c.State.F[1], c.State.F[2]);
                    }

                    // cell molpop concentrations
                    for (int i = 0; i < 2; i++)
                    {
                        // 0: cytosol, 1: membrane
                        ConfigCompartment configComp = (i == 0) ? sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol : sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane;
                        Compartment comp = (i == 0) ? c.Cytosol : c.PlasmaMembrane;
                        double[] pos = new double[] { 0, 0, 0 };

                        foreach (ConfigMolecularPopulation cmp in configComp.molpops)
                        {
                            MolecularPopulation mp = comp.Populations[cmp.molecule_guid_ref];

                            // concentration
                            if (cmp.report_mp.mp_extended > ExtendedReport.NONE)
                            {
                                cell_files[cp.cellpopulation_id].Write("\t{0}", mp.Conc.MeanValue());

                                // gradient
                                if (cmp.report_mp.mp_extended == ExtendedReport.COMPLETE)
                                {
                                    double[] grad = mp.Conc.Gradient(pos);

                                    cell_files[cp.cellpopulation_id].Write("\t{0}\t{1}\t{2}", grad[0], grad[1], grad[2]);
                                }
                            }
                        }
                    }

                    // ecm probe concentrations
                    foreach (ConfigMolecularPopulation mp in sc.scenario.environment.ecs.molpops)
                    {
                        string name = sc.entity_repository.molecules_dict[mp.molecule_guid_ref].Name;

                        if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended > ExtendedReport.NONE)
                        {
                            cell_files[cp.cellpopulation_id].Write("\t{0}", Simulation.dataBasket.ECS.Space.Populations[mp.molecule_guid_ref].Conc.Value(c.State.X));

                            // gradient
                            if (cp.ecm_probe_dict[mp.molpop_guid].mp_extended == ExtendedReport.COMPLETE)
                            {
                                double[] grad = Simulation.dataBasket.ECS.Space.Populations[mp.molecule_guid_ref].Conc.Gradient(c.State.X);

                                cell_files[cp.cellpopulation_id].Write("\t{0}\t{1}\t{2}", grad[0], grad[1], grad[2]);
                            }
                        }
                    }
                }
                // terminate the line
                cell_files[cp.cellpopulation_id].WriteLine();
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
}
