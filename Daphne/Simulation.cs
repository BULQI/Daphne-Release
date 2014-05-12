using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
//using System.Windows.Controls;

using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public class Simulation : IDynamic
    {
        /// <summary>
        /// constants used to set the run status
        /// </summary>
        public static byte RUNSTAT_OFF = 0,
                           RUNSTAT_RUN = 1,
                           RUNSTAT_PAUSE = 2,
                           RUNSTAT_ABORT = 3,
                           RUNSTAT_FINISHED = 4;
        /// <summary>
        /// simulation actions
        /// </summary>
        public static byte SIMFLAG_RENDER = 1 << 0,
                           SIMFLAG_SAMPLE = 1 << 1,
                           SIMFLAG_ALL    = 0xFF;

        public static DataBasket dataBasket;

        public byte RunStatus { get; set; }
        private byte simFlags;
        private double accumulatedTime, duration, renderStep, sampleStep;
        private int renderCount, sampleCount;
        private const double integratorStep = 0.001;

        public Simulation()
        {
            cellManager = new CellManager();
            dataBasket = new DataBasket();
            reset();
        }

        private void clearFlag(byte flag)
        {
            simFlags &= (byte)~flag;
        }

        public bool CheckFlag(byte flag)
        {
            return (simFlags & flag) != 0;
        }

        private void setFlag(byte flag)
        {
            simFlags |= flag;
        }

        public void reset()
        {
            RunStatus = RUNSTAT_OFF;
            accumulatedTime = 0.0;
            renderCount = sampleCount = 0;
        }

        public void restart()
        {
            reset();
            RunStatus = RUNSTAT_RUN;
        }

        public void AddCell(Cell c)
        {
            dataBasket.AddCell(c);
            // add the cell membrane to the ecs
            if (dataBasket.ECS == null)
            {
                throw new Exception("Need to create the ECS before adding cells.");
            }

            // no cell rotation currently
            Transform t = new Transform(false);

            dataBasket.ECS.Space.Boundaries.Add(c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);
            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.State.X);
            dataBasket.ECS.Space.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        private void addCompartmentMolpops(Compartment simComp, ConfigCompartment configComp, SimConfiguration sc)
        {
            foreach (ConfigMolecularPopulation cmp in configComp.molpops)
            {
                if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    MolPopGaussianGradient mpgg = (MolPopGaussianGradient)cmp.mpInfo.mp_distribution;

                    // find the box_spec associated with this gaussian
                    int box_id = -1;

                    for (int j = 0; j < sc.entity_repository.box_specifications.Count; j++)
                    {
                        if (sc.entity_repository.box_specifications[j].box_guid == mpgg.gaussgrad_gauss_spec_guid_ref)
                        {
                            box_id = j;
                            break;
                        }
                    }
                    if (box_id == -1)
                    {
                        // Should never reach here... pop up notice
                        MessageBoxResult tmp = MessageBox.Show("Problem: Box spec for that gaussian spec can't be found...");
                        return;
                    }

                    double[] initArray = new double[] { sc.entity_repository.box_specifications[box_id].x_trans, 
                                                        sc.entity_repository.box_specifications[box_id].y_trans,
                                                        sc.entity_repository.box_specifications[box_id].z_trans,
                                                        sc.entity_repository.box_specifications[box_id].x_scale,
                                                        sc.entity_repository.box_specifications[box_id].y_scale,
                                                        sc.entity_repository.box_specifications[box_id].z_scale,
                                                        mpgg.peak_concentration };


                    simComp.AddMolecularPopulation(dataBasket.Molecules[cmp.molecule_guid_ref], "gauss", initArray);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    MolPopHomogeneousLevel mphl = (MolPopHomogeneousLevel)cmp.mpInfo.mp_distribution;

                    simComp.AddMolecularPopulation(dataBasket.Molecules[cmp.molecule_guid_ref], "const", new double[] { mphl.concentration });
                }
                else
                {
                    throw new Exception("Molecular population distribution type not implemented.");
                }
            }
        }

        private void addCompartmentReactions(Compartment comp, Compartment boundary, ConfigCompartment configComp, EntityRepository er)
        {
            foreach (string guid in configComp.reactions_guid_ref)
            {
                ConfigReaction cr = er.reactions_dict[guid];

                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryAssociation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }

                    MolecularPopulation r1, r2, p;

                    // reactant 1
                    if (er.molecules_dict[cr.reactants_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        r1 = comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        r1 = boundary.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    // reactant 2
                    if (er.molecules_dict[cr.reactants_molecule_guid_ref[1]].molecule_location == MoleculeLocation.Bulk)
                    {
                        r2 = comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name];
                    }
                    else
                    {
                        r2 = boundary.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name];
                    }
                    // product
                    if (er.molecules_dict[cr.products_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        p = comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        p = boundary.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    comp.Reactions.Add(new BoundaryAssociation(r1, r2, p, cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryDissociation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }

                    MolecularPopulation p1, p2, r;

                    // product 1
                    if (er.molecules_dict[cr.products_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        p1 = comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        p1 = boundary.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    // product 2
                    if (er.molecules_dict[cr.products_molecule_guid_ref[1]].molecule_location == MoleculeLocation.Bulk)
                    {
                        p2 = comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name];
                    }
                    else
                    {
                        p2 = boundary.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name];
                    }
                    // reactant
                    if (er.molecules_dict[cr.reactants_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        r = comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        r = boundary.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    comp.Reactions.Add(new BoundaryDissociation(p1, p2, r, cr.rate_const));
                }
                else if(er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedBoundaryActivation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }

                    MolecularPopulation receptor = null, bulk = null, bulkActivated = null;

                    // find bulk, bulkActivated, and the receptor
                    // test reactant 1
                    if (er.molecules_dict[cr.reactants_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        bulk = comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        receptor = boundary.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name];
                    }
                    // test reactant 2
                    if (er.molecules_dict[cr.reactants_molecule_guid_ref[1]].molecule_location == MoleculeLocation.Bulk)
                    {
                        bulk = comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name];
                    }
                    else
                    {
                        receptor = boundary.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name];
                    }
                    // test product 1
                    if (er.molecules_dict[cr.products_molecule_guid_ref[0]].molecule_location == MoleculeLocation.Bulk)
                    {
                        bulkActivated = comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    else
                    {
                        receptor = boundary.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name];
                    }
                    // test product 2
                    if (er.molecules_dict[cr.products_molecule_guid_ref[1]].molecule_location == MoleculeLocation.Bulk)
                    {
                        bulkActivated = comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name];
                    }
                    else
                    {
                        receptor = boundary.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name];
                    }
                    // if any is null, throw an exception
                    if (bulk == null || bulkActivated == null || receptor == null)
                    {
                        throw new Exception("At least one argument of the CatalyzedBoundaryActivation constructor is null.");
                    }
                    comp.Reactions.Add(new CatalyzedBoundaryActivation(bulk, bulkActivated, receptor, cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Association)
                {
                    comp.Reactions.Add(new Association(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                       comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name],
                                                       comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dissociation)
                {
                    comp.Reactions.Add(new Association(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                       comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                       comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name],
                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Annihilation)
                {
                    comp.Reactions.Add(new Annihilation(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                        cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dimerization)
                {
                    comp.Reactions.Add(new Dimerization(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                        comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                        cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.DimerDissociation)
                {
                    comp.Reactions.Add(new DimerDissociation(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                             comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                             cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transformation)
                {
                    comp.Reactions.Add(new Transformation(comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                          comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                          cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.AutocatalyticTransformation)
                {
                    comp.Reactions.Add(new AutocatalyticTransformation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                       comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAnnihilation)
                {
                    comp.Reactions.Add(new CatalyzedAnnihilation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAssociation)
                {
                    comp.Reactions.Add(new CatalyzedAssociation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name],
                                                                comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAnnihilation)
                {
                    comp.Reactions.Add(new CatalyzedAnnihilation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedCreation)
                {
                    comp.Reactions.Add(new CatalyzedCreation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                             comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                             cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerization)
                {
                    comp.Reactions.Add(new CatalyzedDimerization(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerDissociation)
                {
                    comp.Reactions.Add(new CatalyzedDimerDissociation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                      comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                      comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                      cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDissociation)
                {
                    comp.Reactions.Add(new CatalyzedDissociation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                 comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[1]].Name],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedTransformation)
                {
                    comp.Reactions.Add(new CatalyzedTransformation(comp.Populations[er.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name],
                                                                   comp.Populations[er.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name],
                                                                   comp.Populations[er.molecules_dict[cr.products_molecule_guid_ref[0]].Name],
                                                                   cr.rate_const));
                }
            }
        }

        public bool Load(SimConfiguration sc, bool completeReset)
        {
            Scenario scenario = sc.scenario;

            duration = scenario.time_config.duration;
            sampleStep = scenario.time_config.sampling_interval;
            renderStep = scenario.time_config.rendering_interval;
            // make sure the simulation does not start to run immediately
            RunStatus = RUNSTAT_OFF;

            // exit if no reset required
            if (completeReset == false)
            {
                return true;
            }

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(scenario));

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            // molecules
            dataBasket.Molecules.Clear();
            foreach (ConfigMolecule cm in sc.entity_repository.molecules)
            {
                Molecule mol = SimulationModule.kernel.Get<Molecule>(new ConstructorArgument("name", cm.Name),
                                                                     new ConstructorArgument("mw", cm.MolecularWeight),
                                                                     new ConstructorArgument("effRad", cm.EffectiveRadius),
                                                                     new ConstructorArgument("diffCoeff", cm.DiffusionCoefficient));

                dataBasket.Molecules.Add(cm.molecule_guid, mol);
            }

            // set up the collision manager
            MathNet.Numerics.LinearAlgebra.Vector box = new MathNet.Numerics.LinearAlgebra.Vector(3);
            
            box[0] = sc.scenario.environment.extent_x;
            box[1] = sc.scenario.environment.extent_y;
            box[2] = sc.scenario.environment.extent_z;
            collisionManager = SimulationModule.kernel.Get<CollisionManager>(new ConstructorArgument("gridSize", box), new ConstructorArgument("gridStep", 2 * Cell.defaultRadius));

            // cells
            dataBasket.Cells.Clear();
            Cell.SafeCellIndex = 0;

            double[] extent = new double[] { dataBasket.ECS.Space.Interior.Extent(0), 
                                             dataBasket.ECS.Space.Interior.Extent(1), 
                                             dataBasket.ECS.Space.Interior.Extent(2) };

            // ADD CELLS            
            double[] cellPos = new double[dataBasket.ECS.Space.Interior.Dim];
            // convenience arrays to save code length
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            Compartment[] simComp = new Compartment[2];

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                configComp[0] = sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol;
                configComp[1] = sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane;
                
                for (int i = 0; i < cp.number; i++)
                {
                    Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", sc.entity_repository.cells_dict[cp.cell_guid_ref].CellRadius));

                    cellPos[0] = cp.cell_locations[i].X;
                    cellPos[1] = cp.cell_locations[i].Y;
                    cellPos[2] = cp.cell_locations[i].Z;
                    cell.setState(cellPos, new double[] { 0, 0, 0 });

                    simComp[0] = cell.Cytosol;
                    simComp[1] = cell.PlasmaMembrane;

                    for (int comp = 0; comp < 2; comp++)
                    {
                        addCompartmentMolpops(simComp[comp], configComp[comp], sc);
                    }

                    //CELL REACTIONS
                    // cytosol; has boundary
                    addCompartmentReactions(cell.Cytosol, cell.PlasmaMembrane, sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol, sc.entity_repository);
                    // membrane; no boundary
                    addCompartmentReactions(cell.PlasmaMembrane, null, sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane, sc.entity_repository);

                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref != "")
                    {
                        MolecularPopulation driver = cell.Cytosol.Populations[sc.entity_repository.molecules_dict[sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref].Name];

                        cell.Locomotor = new Locomotor(driver, sc.entity_repository.cells_dict[cp.cell_guid_ref].TransductionConstant);
                        cell.IsMotile = true;
                    }
                    else
                    {
                        cell.IsMotile = false;
                    }

                    AddCell(cell);
                }
            }

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3

            // ADD ECS MOLECULAR POPULATIONS
            addCompartmentMolpops(dataBasket.ECS.Space, scenario.environment.ecs, sc);
            // set non-diffusing
            foreach (MolecularPopulation mp in dataBasket.ECS.Space.Populations.Values)
            {
                mp.IsDiffusing = false;
            }

            // ADD ECS REACTIONS
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                addCompartmentReactions(dataBasket.ECS.Space, kvp.Value.PlasmaMembrane, scenario.environment.ecs, sc.entity_repository);
            }

            return true;
        }

        public void RunForward()
        {
            if (RunStatus == RUNSTAT_RUN)
            {
                // render and sample the initial state
                if (renderCount == 0 && sampleCount == 0)
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    renderCount++;
                    sampleCount++;
                    return;
                }
                // clear all flags
                clearFlag(SIMFLAG_ALL);
                
                // find the maximum allowable step (time until the next event occurs)
                double dt;

                // render to happen next
                if (renderCount * renderStep < sampleCount * sampleStep)
                {
                    setFlag(SIMFLAG_RENDER);
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                }
                // sample to happen next
                else if (renderCount * renderStep > sampleCount * sampleStep)
                {
                    setFlag(SIMFLAG_SAMPLE);
                    dt = sampleCount * sampleStep - accumulatedTime;
                    sampleCount++;
                }
                // both to happen simultaneously
                else
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                    sampleCount++;
                }
                // take the step
                Step(dt);

                // render and sample the final state upon completion
                if (RunStatus == RUNSTAT_FINISHED)
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                }
            }
        }

        public void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);
                dataBasket.ECS.Space.Step(localStep);
                collisionManager.Step(localStep);
                cellManager.Step(localStep);
                t += localStep;
            }
            accumulatedTime += dt;
            if (accumulatedTime >= duration)
            {
                RunStatus = RUNSTAT_FINISHED;
            }
        }

        /// <summary>
        /// calculate and return the progress of the simulation
        /// </summary>
        /// <returns>integer indicating the percent of progress</returns>
        public int GetProgressPercent()
        {
            int percent = (accumulatedTime == 0) ? 0 : (int)(100 * accumulatedTime / duration);

            if (RunStatus == RUNSTAT_RUN)
            {
                if (percent >= 100)
                {
                    percent = 99;
                }
            }
            else if (RunStatus == RUNSTAT_FINISHED)
            {
                if (percent > 0)
                {
                    percent = 100;
                }
            }
            else if (percent > 100)
            {
                percent = 100;
            }

            return percent;
        }

        private CellManager cellManager;
        private CollisionManager collisionManager;
    }
}
