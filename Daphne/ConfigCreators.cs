using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Daphne
{
    public class ConfigCreators
    {
        public static void LoadDefaultGlobalParameters(SimConfiguration sc)
        {
            // molecules
            PredefinedMoleculesCreator(sc);

            // template reactions
            PredefinedReactionTemplatesCreator(sc);

            //code to create reactions
            PredefinedReactionsCreator(sc);

            // cells
            PredefinedCellsCreator(sc);

            //reaction complexes
            PredefinedReactionComplexesCreator(sc);
        }

        public static void CreateAndSerializeLigandReceptorScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Ligand Receptor Scenario";
            sc.experiment_description = "CXCL13 binding to membrane-bound CXCR5. Uniform CXCL13.";
            sc.scenario.time_config.duration = 1;
            sc.scenario.time_config.rendering_interval = 1;
            sc.scenario.time_config.sampling_interval = sc.scenario.time_config.duration/20;

            sc.scenario.environment.extent_x = 300;
            sc.scenario.environment.extent_y = 300;
            sc.scenario.environment.extent_z = 300;
            sc.scenario.environment.extent_min = 5;
            sc.scenario.environment.extent_max = 1000;
            sc.scenario.environment.gridstep_min = 1;
            sc.scenario.environment.gridstep_max = 100;
            sc.scenario.environment.gridstep = 50;


            // Global Paramters
            LoadDefaultGlobalParameters(sc);

            // ECM MOLECULES

            // Choose uniform CXCL13 distribution
            //
            // Fraction of bound receptor = [CXCL13] / ( [CXCL13] + Kd )
            // Binding Affinity Kd = kReverse/kForward 
            //
            // The FASEB Journal vol. 26 no. 12 4841-4854.  doi: 10.1096/fj.12-208876
            // Kd ~ 50.5 nM for CXCL13:CXCR5| = (50.5e-9)*(1e-18)*(6.022e23) = 0.0304 molecule/um^3
            //
            // Arbitrarily, choose CXCL13 concentration that give equilibrium receptor occupancy of 0.5
            // [CXCL13] = Kd = 0.0304 molecule/um^3
            //
            double CXCL13conc = 50.5e-9 * 1e-18 * 6.022e23;
            double[] conc = new double[1] { CXCL13conc };
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule_guid_ref = configMolecule.molecule_guid;
                    configMolPop.mpInfo = new MolPopInfo(configMolecule.Name);
                    configMolPop.Name = configMolecule.Name;
                    configMolPop.mpInfo.mp_dist_name = "Uniform";
                    configMolPop.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mpInfo.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = true;

                    sc.scenario.environment.ecs.molpops.Add(configMolPop);
                }
            }

            // Add cell type
            ConfigCell configCell = findCell("Leukocyte_staticReceptor", sc);
            sc.entity_repository.cells_dict.Add(configCell.cell_guid, configCell);

            // Add cell population
            // Add cell population
            CellPopulation cellPop = new CellPopulation();
            cellPop.cell_guid_ref = configCell.cell_guid;
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { sc.scenario.environment.extent_x, 
                                               sc.scenario.environment.extent_y, 
                                               sc.scenario.environment.extent_z };
            double minDisSquared = 2 * sc.entity_repository.cells_dict[cellPop.cell_guid_ref].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
            cellPop.cellPopDist.CellStates[0] = new CellState(  sc.scenario.environment.extent_x / 2,
                                                                sc.scenario.environment.extent_y / 2,
                                                                sc.scenario.environment.extent_z / 2);
            cellPop.cellpopulation_constrained_to_region = false;
            cellPop.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            sc.scenario.cellpopulations.Add(cellPop);

            // Cell reporting
            cellPop.report_xvf.position = false;
            cellPop.report_xvf.velocity = false;
            cellPop.report_xvf.force = false;
            foreach (ConfigMolecularPopulation cmp in configCell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.LEAN;
            }
            foreach (ConfigMolecularPopulation mpECM in sc.scenario.environment.ecs.molpops)
            {
                ReportECM reportECM = new ReportECM();
                reportECM.molpop_guid_ref = mpECM.molpop_guid;
                reportECM.mp_extended = ExtendedReport.LEAN;       
                cellPop.ecm_probe.Add(reportECM);
                cellPop.ecm_probe_dict.Add(mpECM.molpop_guid, reportECM);                
            }

            sc.reporter_file_name = "lig-rec_test";

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            type = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    sc.scenario.environment.ecs.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CreateAndSerializeDriverLocomotionScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Cell locomotion with driver molecule.";
            sc.experiment_description = "Cell moves in the direction of the CXCL13 linear gradient (right to left) created by Dirichlet BCs. Cytosol molecule A* drives locomotion.";
            sc.scenario.environment.extent_x = 350;
            sc.scenario.environment.extent_y = 350;
            sc.scenario.environment.extent_z = 350;
            sc.scenario.environment.extent_min = 5;
            sc.scenario.environment.extent_max = 1000;
            sc.scenario.environment.gridstep_min = 1;
            sc.scenario.environment.gridstep_max = 100;
            sc.scenario.environment.gridstep = 50;

            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = sc.scenario.time_config.duration/100;
            sc.scenario.time_config.sampling_interval = sc.scenario.time_config.duration/100;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);

            //// Gaussian Distrtibution
            //// Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
            //// box x,y,z_scale parameters are 2*sigma
            //GaussianSpecification gaussSpec = new GaussianSpecification();
            //BoxSpecification box = new BoxSpecification();
            //box.x_trans = sc.scenario.environment.extent_x / 2;
            //box.y_trans = sc.scenario.environment.extent_y / 2;
            //box.z_trans = sc.scenario.environment.extent_z / 2;
            //box.x_scale = sc.scenario.environment.extent_x / 5;
            //box.y_scale = sc.scenario.environment.extent_y / 5;
            //box.z_scale = sc.scenario.environment.extent_z / 5;
            //sc.entity_repository.box_specifications.Add(box);
            //gaussSpec.gaussian_spec_box_guid_ref = box.box_guid;
            //gaussSpec.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            //sc.entity_repository.gaussian_specifications.Add(gaussSpec);

            // ECS

            // Choose uniform CXCL13 distribution
            //
            // Fraction of bound receptor = [CXCL13] / ( [CXCL13] + Kd )
            // Binding Affinity Kd = kReverse/kForward 
            //
            // The FASEB Journal vol. 26 no. 12 4841-4854.  doi: 10.1096/fj.12-208876
            // Kd ~ 50.5 nM for CXCL13:CXCR5| = (50.5e-9)*(1e-18)*(6.022e23) = 0.0304 molecule/um^3
            //
            // Arbitrarily, choose CXCL13 concentration that give equilibrium receptor occupancy of 0.5
            // [CXCL13] = Kd = 0.0304 molecule/um^3
            //
            double CXCL13conc = 50.5e-9 * 1e-18 * 6.022e23;
            double[] conc = new double[1] { CXCL13conc };
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule_guid_ref = configMolecule.molecule_guid;
                    configMolPop.mpInfo = new MolPopInfo(configMolecule.Name);
                    configMolPop.Name = configMolecule.Name;

                    MolPopLinear molpoplin = new MolPopLinear();
                    molpoplin.boundary_face = BoundaryFace.X;
                    molpoplin.dim = 0;
                    molpoplin.x1 = 0;
                    molpoplin.boundaryCondition = new List<BoundaryCondition>(); 
                    BoundaryCondition bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 5 * CXCL13conc);                    
                    molpoplin.boundaryCondition.Add(bc);
                    bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0);
                    molpoplin.boundaryCondition.Add(bc);
                    configMolPop.mpInfo.mp_distribution = molpoplin;
                    configMolPop.mpInfo.mp_dist_name = "Linear";

                    // graphics colors etc
                    configMolPop.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mpInfo.mp_render_blending_weight = 2.0;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = false;

                    sc.scenario.environment.ecs.molpops.Add(configMolPop);
                }
            }

            // Add cell
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell configCell = findCell("Leukocyte_staticReceptor_motile", sc);
            sc.entity_repository.cells_dict.Add(configCell.cell_guid, configCell);

            // Add cell population
            CellPopulation cellPop = new CellPopulation();
            cellPop.cell_guid_ref = configCell.cell_guid;
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { sc.scenario.environment.extent_x, 
                                               sc.scenario.environment.extent_y, 
                                               sc.scenario.environment.extent_z };
            double minDisSquared = 2*sc.entity_repository.cells_dict[cellPop.cell_guid_ref].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
            cellPop.cellPopDist.CellStates[0] = new CellState(  sc.scenario.environment.extent_x,
                                                                sc.scenario.environment.extent_y / 2,
                                                                sc.scenario.environment.extent_z / 2);
            cellPop.cellpopulation_constrained_to_region = false;
            cellPop.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            sc.scenario.cellpopulations.Add(cellPop);
            cellPop.report_xvf.position = true;
            cellPop.report_xvf.velocity = true;
            cellPop.report_xvf.force = true;

            foreach (ConfigMolecularPopulation cmp in configCell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
            }
            foreach (ConfigMolecularPopulation cmp in configCell.cytosol.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
            } 
            foreach (ConfigMolecularPopulation mpECM in sc.scenario.environment.ecs.molpops)
            {
                ReportECM reportECM = new ReportECM();
                reportECM.molpop_guid_ref = mpECM.molpop_guid;
                reportECM.mp_extended = ExtendedReport.COMPLETE;
                cellPop.ecm_probe.Add(reportECM);
                cellPop.ecm_probe_dict.Add(mpECM.molpop_guid, reportECM);
            }

            sc.reporter_file_name = "Loco_test";

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            type = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    sc.scenario.environment.ecs.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }
        }

        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateAndSerializeDiffusionScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Diffusion Scenario";
            sc.experiment_description = "CXCL13 diffusion in the ECM. No cells. Gaussian initial distribution. No flux BCs.";
            sc.scenario.time_config.duration = 10.0;
            sc.scenario.time_config.rendering_interval = 1;
            sc.scenario.time_config.sampling_interval = 1.0;

            sc.scenario.environment.extent_x = 500;
            sc.scenario.environment.extent_y = 500;
            sc.scenario.environment.extent_z = 500;
            sc.scenario.environment.gridstep = 50;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);
            //ChartWindow = ReacComplexChartWindow;

            // Gaussian Distrtibution
            // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
            // box x,y,z_scale parameters are 2*sigma
            GaussianSpecification gaussSpec = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_trans = sc.scenario.environment.extent_x / 2;
            box.y_trans = sc.scenario.environment.extent_y / 2;
            box.z_trans = sc.scenario.environment.extent_z / 2;
            box.x_scale = sc.scenario.environment.extent_x / 2;
            box.y_scale = sc.scenario.environment.extent_y / 4;
            box.z_scale = sc.scenario.environment.extent_z / 5;
            sc.entity_repository.box_specifications.Add(box);
            gaussSpec.gaussian_spec_box_guid_ref = box.box_guid;
            //gg.gaussian_spec_name = "gaussian";
            gaussSpec.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Rotate the box by 45 degrees about the box's y-axis.
            double theta = Math.PI / 4,
                   cos = Math.Cos(theta),
                   sin = Math.Cos(theta);
            double[][] trans_matrix = new double[4][];
            trans_matrix[0] = new double[4] { box.x_scale * cos, 0, box.z_scale * sin, box.x_trans };
            trans_matrix[1] = new double[4] { 0, box.y_scale, 0, box.y_trans };
            trans_matrix[2] = new double[4] { -box.x_scale * sin, 0, box.z_scale * cos, box.z_trans };
            trans_matrix[3] = new double[4] { 0, 0, 0, 1 };
            box.SetMatrix(trans_matrix);
            sc.entity_repository.gaussian_specifications.Add(gaussSpec);

            var query =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation configMolPop = null;
            // ecs
            foreach (ConfigMolecule cm in query)
            {
                configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                configMolPop.molecule_guid_ref = cm.molecule_guid;
                configMolPop.mpInfo = new MolPopInfo(cm.Name);
                configMolPop.Name = cm.Name;
                configMolPop.mpInfo.mp_dist_name = "Gaussian";
                configMolPop.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                configMolPop.mpInfo.mp_render_blending_weight = 2.0;

                MolPopGaussian molPopGaussian = new MolPopGaussian();
                molPopGaussian.peak_concentration = 100;
                molPopGaussian.gaussgrad_gauss_spec_guid_ref = sc.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                configMolPop.mpInfo.mp_distribution = molPopGaussian;

                // Reporting
                configMolPop.report_mp.mp_extended = ExtendedReport.COMPLETE;
                ReportECM r = configMolPop.report_mp as ReportECM;
                r.mean = true;

                sc.reporter_file_name = "Diffusion_test";

                sc.scenario.environment.ecs.molpops.Add(configMolPop);
            }
        }

         /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateAndSerializeBlankScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Blank Scenario";
            sc.experiment_description = "Libraries only.";
            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);

        }

        private static void PredefinedCellsCreator(SimConfiguration sc)
        {
            ConfigCell gc;
            double[] conc;
            //ReactionType[] reacType;
            string[] type;
            ConfigMolecularPopulation gmp;
            ConfigMolecule cm;
            ConfigReaction reac;

            // concentration of CXCR5 on B cells
            // Barroso2012:  13,851 per cell = 44 um^{-2} for cells with 5 um radius
            double cxcr5Conc_5umRadius = 44;

            //
            // Leukocyte_staticReceptor
            // Leukocyte with fixed number of receptor molecules and no locomotion
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            float[,] colors = new float[2,4]{ {0.3f, 0.9f, 0.1f, 0.1f},
                                              {0.3f, 0.2f, 0.9f, 0.1f} };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    //gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(colors[i,0], colors[i,1], colors[i,2], colors[i,3]);
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }
            sc.entity_repository.cells.Add(gc);

            //
            // Leukocyte_staticReceptor_motile
            // Leukocyte with fixed number of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor_motile";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[2] { 250, 0 };
            type = new string[2] { "A", "A*" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            // Reactions in Cytosol
            type = new string[2] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|",
                                          "A* -> A"};
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    gc.cytosol.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 100;

            sc.entity_repository.cells.Add(gc);

            //
            // Leukocyte_dynamicReceptor_motile
            // Leukocyte with dynamic concentration of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_dynamicReceptor_motile";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[5] { 250, 0, 1, 0, 0 };
            type = new string[5] { "A", "A*", "gCXCR5", "CXCR5", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            // Reactions in Cytosol
            type = new string[8] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|",
                                  "A* -> A",
                                  "gCXCR5 -> CXCR5 + gCXCR5",
                                  "CXCR5 -> CXCR5|",
                                  "CXCR5| -> CXCR5",
                                  "CXCR5 ->",
                                  "CXCL13:CXCR5| -> CXCL13:CXCR5", 
                                  "CXCL13:CXCR5 ->" 
                                };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    gc.cytosol.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 1e2;

            sc.entity_repository.cells.Add(gc);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.cells, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedCells.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        private static void PredefinedMoleculesCreator(SimConfiguration sc)
        {
            // Molecular weight in kDa
            // Effective radius in micrometers (um)
            // Diffusion coefficient in um^{2} / min
            ConfigMolecule cm;

            //
            // The following are generally intended as ECS molecules
            //

            // Wang2011, CXCL12:  MWt = 7.96 kDa, D = 4.5e3
            double MWt_CXCL12 = 7.96;
            cm = new ConfigMolecule("CXCL12", MWt_CXCL12, 1.0, 4.5e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // Use CXCL13 values for CXCL13, for now
            cm = new ConfigMolecule("CXCL13", MWt_CXCL12, 1.0, 4.5e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            //
            // Membrane bound
            // 

            // Set the diffusion coefficients very low, for now.
            double membraneDiffCoeff = 1e-7;

            // Marchese2001, CXCR4:  MWt = 43 kDa
            double MWt_CXCR4 = 43;
            cm = new ConfigMolecule("CXCR4|", MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // Use CXCR4 value for CXCR5
            cm = new ConfigMolecule("CXCR5|", MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // Add values for CXCL12 and CXCR4
            cm = new ConfigMolecule("CXCL12:CXCR4|", MWt_CXCL12 + MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);
            
            // Use CXCL12:CXCR4 values, for now
            cm = new ConfigMolecule("CXCL13:CXCR5|", MWt_CXCL12 + MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            //
            // The following are generally intended as cytosol molecules 
            //

            // Bionumbers: diffusion coefficient for cytoplasmic proteins ~ 300 - 900
            // Francis1997: diffusion coefficient for signalling molecules ~ 600 - 6000
            // Use mean of common range 600 - 900
            double cytoDiffCoeff = 750;

            cm = new ConfigMolecule("CXCR5", MWt_CXCR4, 1.0, cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("CXCR4", MWt_CXCR4, 1.0, cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("CXCL13:CXCR5", MWt_CXCR4 + MWt_CXCL12, 1.0, cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("CXCL12:CXCR4", MWt_CXCR4 + MWt_CXCL12, 1.0, cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // These are pseudo cytoplasmic molecules
            //
            // Set diffusion coefficient for A to same as other cytoplasmic proteins
            cm = new ConfigMolecule("A", 1.0, 1.0, cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);
            // Make the diffusion coefficient for A* much less than A
            // A* encompasses the tubulin structure and polarity of the cell 
            // If the diffusion coefficient is too large, the polarity will not be maintained and the cell will not move
            double f = 1e-2;
            cm = new ConfigMolecule("A*", 1.0, 1.0, f * cytoDiffCoeff);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            //
            // Genes
            //

            cm = new ConfigMolecule("gCXCR5", 1.0, 1.0, 1e-7);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("gCXCR4", 1.0, 1.0, 1e-7);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.molecules, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedMolecules.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        //Following function needs to be called only once
        private static void PredefinedReactionTemplatesCreator(SimConfiguration sc)
        {
            //Test code to read in json containing object "PredefinedReactions"
            //string readText = File.ReadAllText("TESTER.TXT");
            //entity_repository.reactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText);
            ConfigReactionTemplate crt;

            //Annihilation
            // a ->
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            //crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Annihilation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
            
            //Association
            // a + b -> c
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Association;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Dimerization
            // 2a -> b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(2);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Dimerization;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Dimer Dissociation
            // b -> 2a
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.DimerDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            //Dissociation
            // c -> a + b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Dissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Transformation
            // a -> b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Transformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // CatalyzedBoundaryActivation
            // a + E -> E + b
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
             crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedBoundaryActivation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            //BoundaryAssociation
            // a + B -> C
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryAssociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            //BoundaryDissociation
            // C -> a + B
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // BoundaryTransportFrom
            // A -> a
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportFrom;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // BoundaryTransportTo
            // a -> A
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportTo;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Autocatalytic Transformation
            // Not a catalyzed reaction in the strict sense needed in the Config class
            // a + e -> 2e 
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.AutocatalyticTransformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Annihilation
            // a + e -> e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            // type
            crt.reac_type = ReactionType.CatalyzedAnnihilation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Association
            // a + b + e -> c + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedAssociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Creation
            // e -> e + a
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedCreation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Dimerization
            // e + 2a -> b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(2);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedDimerization;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Dimer Dissociation
            // e + b -> 2a + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.CatalyzedDimerDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Dissociation
            // c + e -> a + b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // Catalyzed Transformation
            // a + e -> b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedTransformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
            
            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.reaction_templates, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactionTemplates.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        // given a molecule name and location, find its guid
        public static string findMoleculeGuid(string name, MoleculeLocation ml, SimConfiguration sc)
        {
            foreach (ConfigMolecule cm in sc.entity_repository.molecules)
            {
                if (cm.Name == name && cm.molecule_location == ml)
                {
                    return cm.molecule_guid;
                }
            }
            return null;
        }

        // given a reaction template type, find its guid
        public static string findReactionTemplateGuid(ReactionType rt, SimConfiguration sc)
        {
            foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.reaction_template_guid;
                }
            }
            return null;
        }

        // The assumption below is no longer valid.
        //// this only works if we have only one reaction per type of reaction
        //public static string findReactionGuid(ReactionType rt, SimConfiguration sc)
        //{
        //    string template_guid = findReactionTemplateGuid(rt, sc);

        //    if (template_guid != null)
        //    {
        //        foreach (ConfigReaction cr in sc.entity_repository.reactions)
        //        {
        //            if (cr.reaction_template_guid_ref == template_guid)
        //            {
        //                return cr.reaction_guid;
        //            }
        //        }
        //    }
        //    return null;
        //}

        // given a cell type name like BCell, find the ConfigCell object
        public static ConfigCell findCell(string name, SimConfiguration sc)
        {
            foreach (ConfigCell cc in sc.entity_repository.cells)
            {
                if (cc.CellName == name)
                {
                    return cc;
                }
            }
            return null;
        }

        //Following function needs to be called only once
        private static void PredefinedReactionsCreator(SimConfiguration sc)
        {
            // NOTE: Whenever there are two or more reactants (or products or modifiers) in a boundary reaction,
            // the bulk molecules must be added first, then the boundary molecules.

            double kr, kf;

            // These values are waiting for more informed choices:
            //
            // Default degradation rate for ECS proteins  (min)
            // Arbitraritly, assume half life lambda=7 min, then rate constant k=ln(2)/lambda=0.6931/7
            double ecsDefaultDegradRate = 0.1; 
            //
            // Default degradation rate for cytoplasm proteins  (min)
            // Arbitraritly, assume half life lambda=7 min, then rate constant k=ln(2)/lambda=0.6931/7
            double cytoDefaultDegradRate = 0.1;

            ConfigReaction cr = new ConfigReaction();

            // Annihiliation: CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13:CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCR4 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, sc));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL12:CXCR4 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants 
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, sc));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL12 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, sc));
            cr.rate_const = ecsDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = ecsDefaultDegradRate;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            ////////////////////////////////////////////////////////////////////////////////////
            // CXCL13 + CXCR5| <-> CXCL13:CXCR5|
            //
            // Barroso, Munoz, et al.
            // EBI2 regulates CXCL13-mediated responses by heterodimerization with CXCR5
            // The FASEB Journal vol. 26 no. 12 4841-485
            // CXCL13/CXCR5 binding affinity:  KD = 5.05e-8 M  = (50.5e-9)*(1e-18)*(6.022e23) molec/um^3 = 0.0304 molec/um^3
            //
            // KD = k_reverse / k_forward
            // Use k_reverse from CXCL12/CXCR4| binding/unbinding.
            // Vega2011 k_off = 8.24e-3/s = 0.494/min
            // Use same for CXCL13:CXCR5| unbinding k_reverse = 0.494 min^{-1}
            // k_forward = k_reverse / 0.0304 um^3/(molec-min) = 1.625 um^3/(molec-min)
            //
            kr = 0.494;
            kf = 16.25;
            //
            // BoundaryAssociation: CXCL13 + CXCR5| -> CXCL13:CXCR5|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryAssociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, sc));
            cr.rate_const = kf;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // BoundaryDissociation:  CXCL13:CXCR5| ->  CXCR5| + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryDissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, sc));
            cr.rate_const = kr;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //////////////////////////////////////////////////////////////////////////////////
            

            /////////////////////////////////////////////////////////////////////////////
            // CXCL13:CXCR5| + A -> CXCL13:CXCR5| + A*
            // A* -> A
            // CXCL12:CXCR4| + A -> CXCL12:CXCR4| + A*
            //
            // The A* gradient drives the locomotion force.
            // The rate constant has units min^{-1}, similar to the units for off rate for receptor/ligand binding,
            // but this reaction encompasses many processes, including tubulin network growth. Therefore, we expect
            // this pseudo-reaction to occur at a slower rate, and we will choose a rate constant (activation rate) 
            // that is significantly less than simple off rates.
            // 
            // For simple unbinding k_off ~ 0.5 min^{-1} (CXCL12:CXCR4, Vega2011), so arbitrarily, choose a reduction factor 
            double f1 = 1e-2;
            // Then  k_activation ~ 0.5 * f = 5e-3 min^{-1}
            //
            // Choose a slower deactivation (A* -> A) rate   k_deactivation = k_activation / 100;
            double f2 = 1e-1;
            // 
            kf = 0.5 * f1;
            kr = kf * f2;
            //
            // CatalyzedBoundaryActivation: CXCL13:CXCR5| + A -> CXCL13:CXCR5| + A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, sc));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            cr.rate_const = kf;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // Transformation: A* -> A
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            cr.rate_const = kr;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // CatalyzedBoundaryActivation: CXCL12:CXCR4| + A -> CXCL12:CXCR4| + A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, sc));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            cr.rate_const = kf;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            ////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////
            // CXCL12 + CXCR4| -> CXCL12:CXCR4|
            //
            ////// Crump, M. P., Gong, J. H., Loetscher, et al., (1997) EMBO J. 16, 6996–7007.
            ////// CXCL12/CXCR4 binding affinity:  KD = 3.6e-9 M  = (3.6e-9)*(1e-18)*(6.022e23) molec/um^3 = 2.2e-3 molec/um^3
            //////
            ////// KD = k_reverse / k_forward
            ////// Arbitrarily, choose k_reverse = 1/0.1   min^{-1} = 10 min^{-1}
            ////// k_forward = k_reverse / 2.2e-3 um^3/(molec-min) = 455 um^3/(molec-min)
            //////
            ///////////////////////////////////////////////////////////////////////////////////
            // Vega et al., Technical Advance: Surface plasmon resonance-based analysis of CXCL12 binding 
            // using immobilized lentiviral particles. Journal of Leukocyte Biology Vol 90, 1-10, 2011. 
            // 
            // CXCL12/CXCR4 binding affinity:  KD = 2.09e-2 molec/um^3
            // k_reverse (k_off) = 0.494/min
            //
            // KD = k_reverse / k_forward
            // k_forward = k_reverse / KD = 0.494 / 2.09e-2  um^3/(molec-min) = 23.6 um^3/(molec-min)
            //
            kr = 0.494;
            kf = 23.6;
            //
            // BoundaryAssociation: CXCL12 + CXCR4| -> CXCL12:CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryAssociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, sc));
            cr.rate_const = kf;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // BoundaryDissociation:  CXCL12:CXCR4| ->  CXCR4| + CXCL12
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryDissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, sc));
            cr.rate_const = kr;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            ///////////////////////////////////////////////////////////////////////////////////////////////////

            // Internalization of bound receptor
            // 
            // Barroso R, Munoz LM, Barrondo S, et al., EBI2 regulates CXCL13-mediated responses by 
            // heterodimerization with CXCR5. The FASEB Journal Vol. 26, pp 4841-4854, December 2012.
            // Fit of fig. S3b:
            //          k = 0.010 min(-1)
            double k1_CXCL13_CXCR5 = 0.010;
            //
            // BoundaryTransportFrom: CXCL13:CXCR5| -> CXCL13:CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportFrom, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = k1_CXCL13_CXCR5;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // Hesselgesser et al.,  Identification and Characterization of the CXCR4 Chemokine, J Immunol 1998; 160:877-883.
            // Fit of curve on pg 880, SDF-1alpha(CXCL12):CXCR4 internalization:
            //          k = 0.027 min(-1)
            double k1_CXCL12_CXCR4 = 0.027;
            //
            // BoundaryTransportFrom: CXCL12:CXCR4| -> CXCL12:CXCR4
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportFrom, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, sc));
            cr.rate_const = k1_CXCL12_CXCR4;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // Assume that internalization of the unactivated receptor is slower than internalization of bound(activated) receptor
            // Arbitrarily choose a factor of 100
            f2 = 1e-2;
            //
            // BoundaryTransportFrom: CXCR5| -> CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportFrom, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = f2 * k1_CXCL13_CXCR5;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //
            // BoundaryTransportFrom: CXCR4| -> CXCR4
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportFrom, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, sc));
            cr.rate_const = f2 * k1_CXCL12_CXCR4;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            //
            // These next reactions are in need of more informed reaction rates
            //

            // Catalyzed Creation: gCXCR5 -> gCXCR5 + CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedCreation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("gCXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = 10.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Catalyzed Creation: gCXCR4 -> gCXCR4 + CXCR4
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedCreation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("gCXCR4", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, sc));
            cr.rate_const = 10.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryTransportTo: CXCR5 -> CXCR5|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportTo, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, sc));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryTransportTo: CXCR4 -> CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryTransportTo, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, sc));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            ///////////////////////////////////////////////
            // NOTE: These reactions may not be biologically meaningful for bulk molecules, 
            // but are used for reaction complexes
            //
            // Association: CXCR5 + CXCL13 -> CXCL13:CXCR5
            kr = 0.494;
            kf = 16.25;
            //
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Association, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = kf;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            // Dissociation: CXCL13:CXCR5 -> CXCR5 + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Dissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = kr;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            ////////////////////////////////////////////////////

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.reactions, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactions.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        private static void PredefinedReactionComplexesCreator(SimConfiguration sc)
        {
            ConfigReactionComplex crc = new ConfigReactionComplex("Ligand/Receptor");

            //MOLECULES

            double[] conc = new double[3] { 0.0304, 1, 0 };
            string[] type = new string[3] { "CXCL13", "CXCR5", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    configMolPop.molecule_guid_ref = configMolecule.molecule_guid;
                    configMolPop.mpInfo = new MolPopInfo(configMolecule.Name);
                    configMolPop.Name = configMolecule.Name;
                    configMolPop.mpInfo.mp_dist_name = "Uniform";
                    configMolPop.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mpInfo.mp_distribution = hl;
                    crc.molpops.Add(configMolPop);
                }
            }

            //var query =
            //    from mol in sc.entity_repository.molecules
            //    where mol.Name == "CXCR5" || mol.Name == "CXCL13:CXCR5" || mol.Name == "CXCL13"
            //    select mol;

            //ConfigMolecularPopulation cmp = null;
            //foreach (ConfigMolecule cm in query)
            //{
            //    cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            //    cmp.molecule_guid_ref = cm.molecule_guid;
            //    cmp.mpInfo = new MolPopInfo("My " + cm.Name);
            //    cmp.Name = "My " + cm.Name;
            //    cmp.mpInfo.mp_dist_name = "Constant level";
            //    cmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
            //    cmp.mpInfo.mp_render_blending_weight = 2.0;

            //    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
            //    if (cm.Name == "CXCR5")
            //    {
            //        hl.concentration = 1.0;
            //    }
            //    else
            //    {
            //        hl.concentration = 2.0;
            //    }
            //    cmp.mpInfo.mp_distribution = hl;

            //    crc.molpops.Add(cmp);
            //}

            //REACTIONS

            //string guid = findReactionGuid(ReactionType.Association, sc);

            // Reaction strings
            type = new string[2] { "CXCL13:CXCR5 -> CXCL13 + CXCR5",
                                            "CXCL13 + CXCR5 -> CXCL13:CXCR5"}; 
                                            //"CXCR5 ->",
                                            //"CXCL13 ->"};
            
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    ConfigReactionGuidRatePair grp = new ConfigReactionGuidRatePair();
                    grp.Guid = reac.reaction_guid;
                    grp.OriginalRate = reac.rate_const;
                    grp.ReactionComplexRate = reac.rate_const;
                    crc.reactions_guid_ref.Add(reac.reaction_guid);
                    crc.ReactionRates.Add(grp);
                }
            }

            //if (guid != null)
            //{
            //    ConfigReaction reac = findReactionByGuid(guid, sc);
            //    ConfigReactionGuidRatePair grp = new ConfigReactionGuidRatePair();
            //    grp.Guid = guid;
            //    grp.OriginalRate = reac.rate_const;
            //    grp.ReactionComplexRate = reac.rate_const;
            //    crc.reactions_guid_ref.Add(guid);
            //    crc.ReactionRates.Add(grp);
            //}

            //guid = findReactionGuid(ReactionType.Dissociation, sc);
            
            //if (guid != null)
            //{
            //    ConfigReaction reac = findReactionByGuid(guid, sc);
            //    ConfigReactionGuidRatePair grp = new ConfigReactionGuidRatePair();
            //    grp.Guid = guid;
            //    grp.OriginalRate = reac.rate_const;
            //    grp.ReactionComplexRate = reac.rate_const;
            //    crc.reactions_guid_ref.Add(guid);
            //    crc.ReactionRates.Add(grp);
            //}

            //foreach (ConfigReaction cr in sc.entity_repository.reactions)
            //{
            //    foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
            //    {
            //        if (cr.reaction_template_guid_ref == crt.reaction_template_guid && crt.reac_type == ReactionType.Annihilation)
            //        {
            //            crc.reactions_guid_ref.Add(cr.reaction_guid);
            //            ConfigReactionGuidRatePair grp = new ConfigReactionGuidRatePair();
            //            grp.Guid = cr.reaction_guid;
            //            grp.OriginalRate = cr.rate_const;
            //            grp.ReactionComplexRate = cr.rate_const;
            //            crc.ReactionRates.Add(grp);
            //        }
            //    }                
            //}

            sc.entity_repository.reaction_complexes.Add(crc);

        }

        // given a string description of a reaction, return the ConfigReaction that matches
        public static ConfigReaction findReaction(string rt, SimConfiguration sc)
        {
            foreach (ConfigReaction cr in sc.entity_repository.reactions)
            {
                if (cr.TotalReactionString == rt) //cr.reaction_template_guid_ref == template_guid)
                {
                    return cr;
                }
            }
            return null;
        }

        // given a reaction guid, return the ConfigReaction 
        public static ConfigReaction findReactionByGuid(string guid, SimConfiguration sc)
        {
            foreach (ConfigReaction cr in sc.entity_repository.reactions)
            {
                if (cr.reaction_guid == guid) 
                {
                    return cr;
                }
            }
            return null;
        }
    }

}
