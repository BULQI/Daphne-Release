﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public class Simulation : EntityModelBase, IDynamic
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

        private byte runStatus;
        private byte simFlags;
        private double accumulatedTime, duration, renderStep, sampleStep;
        private int renderCount, sampleCount;
        private const double integratorStep = 0.001;


        public Simulation()
        {
            cellManager = new CellManager();
            dataBasket = new DataBasket(this);
            reset();
        }

        /// <summary>
        /// accessor for the simulation's run status
        /// </summary>
        public byte RunStatus
        {
            get { return runStatus; }
            set
            {
                runStatus = value;
                OnPropertyChanged("RunStatus");
            }
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
            lock (this)
            {
                reset();
                RunStatus = RUNSTAT_RUN;
            }
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
                    MolPopGaussian mpgg = (MolPopGaussian)cmp.mpInfo.mp_distribution;

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

                    // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
                    // box x,y,z_scale parameters are 2*sigma
                    double[] initArray = new double[] { sc.entity_repository.box_specifications[box_id].x_trans, 
                                                        sc.entity_repository.box_specifications[box_id].y_trans,
                                                        sc.entity_repository.box_specifications[box_id].z_trans,
                                                        sc.entity_repository.box_specifications[box_id].x_scale / 2,
                                                        sc.entity_repository.box_specifications[box_id].y_scale / 2,
                                                        sc.entity_repository.box_specifications[box_id].z_scale / 2,
                                                        mpgg.peak_concentration };


                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "gauss", initArray);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    MolPopHomogeneousLevel mphl = (MolPopHomogeneousLevel)cmp.mpInfo.mp_distribution;

                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "const", new double[] { mphl.concentration });
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
                {
                	MolPopExplicit mpc = (MolPopExplicit)cmp.mpInfo.mp_distribution;
                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "explicit", mpc.conc);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    MolPopLinear mpl = cmp.mpInfo.mp_distribution as MolPopLinear;

                    double c1 = mpl.boundaryCondition[0].concVal;
                    double c2 = mpl.boundaryCondition[1].concVal;
                    double x2;
                    switch (mpl.dim)
                    {
                        case 0:
                            x2 = sc.scenario.environment.extent_x;
                            break;
                        case 1:
                            x2 = sc.scenario.environment.extent_y;
                            break;
                        case 2:
                            x2 = sc.scenario.environment.extent_z;
                            break;
                        default:
                            x2 = sc.scenario.environment.extent_x; 
                            break;
                    }
                         
                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "linear", new double[] {       
                                c1, 
                                c2,
                                mpl.x1, 
                                x2, 
                                mpl.dim});
                }

                else
                {
                    throw new Exception("Molecular population distribution type not implemented.");
                }
            }
        }

        private void addCompartmentReactions(Compartment comp, Compartment boundary, ConfigCompartment configComp, EntityRepository er)
        {
            List<string> reac_guids = new List<string>();

            foreach (string rguid in configComp.reactions_guid_ref)
            {
                reac_guids.Add(rguid);
            }

            foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            {
                ConfigReactionComplex crc = er.reaction_complexes_dict[rcguid];
                foreach (string rguid in crc.reactions_guid_ref)
                {
                    if (reac_guids.Contains(rguid) == false)
                    {
                        reac_guids.Add(rguid);
                    }
                }
            }

            // NOTES: 
            // ConfigCreator.PredefinedReactionsCreator() and SimConfigToolWindow.btnSave_Click() 
            // add bulk molecules then boundary molecules to the reactant, product, and modifier lists.
            // 
            // When comp is ECS then ECS boundary reactions may not apply to some cell types. 
            // The continue statements below catch these cases.

            foreach( string guid in reac_guids)
            {
                ConfigReaction cr = er.reactions_dict[guid];           

                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryAssociation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }
                    if ( !boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[1]) || !boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]) )
                    {
                        continue;
                    }
                    comp.Reactions.Add(new BoundaryAssociation( boundary.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryDissociation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[0]) || !boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[1]))
                    {
                        continue;
                    }
                    comp.Reactions.Add(new BoundaryDissociation(boundary.Populations[cr.products_molecule_guid_ref[1]],
                                                                comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                boundary.Populations[cr.reactants_molecule_guid_ref[0]], cr.rate_const));
                }
                else if(er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedBoundaryActivation)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }
                    if ( !boundary.Populations.ContainsKey(cr.modifiers_molecule_guid_ref[0]) )
                    {
                        continue;
                    }
                    comp.Reactions.Add(new CatalyzedBoundaryActivation( comp.Populations[cr.reactants_molecule_guid_ref[0]], 
                                                                        comp.Populations[cr.products_molecule_guid_ref[0]], 
                                                                        boundary.Populations[cr.modifiers_molecule_guid_ref[0]], cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportTo)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }
                    if (!boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]) )
                    {
                        continue;
                    }
                    comp.Reactions.Add(new BoundaryTransportTo( comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportFrom)
                {
                    if (boundary == null)
                    {
                        throw new Exception("Can't have a null boundary in a boundary reaction.");
                    }
                    if ( !boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[0]) )
                    {
                        continue;
                    }
                    comp.Reactions.Add(new BoundaryTransportFrom( boundary.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                  comp.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Association)
                {
                    comp.Reactions.Add(new Association(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                       comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                       comp.Populations[cr.products_molecule_guid_ref[0]],
                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dissociation)
                {
                    comp.Reactions.Add(new Dissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                       comp.Populations[cr.products_molecule_guid_ref[0]],
                                                       comp.Populations[cr.products_molecule_guid_ref[1]],
                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Annihilation)
                {
                    comp.Reactions.Add(new Annihilation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                        cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dimerization)
                {
                    comp.Reactions.Add(new Dimerization(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                        comp.Populations[cr.products_molecule_guid_ref[0]],
                                                        cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.DimerDissociation)
                {
                    comp.Reactions.Add(new DimerDissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                             comp.Populations[cr.products_molecule_guid_ref[0]],
                                                             cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transformation)
                {
                    comp.Reactions.Add(new Transformation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                          comp.Populations[cr.products_molecule_guid_ref[0]],
                                                          cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.AutocatalyticTransformation)
                {
                    comp.Reactions.Add(new AutocatalyticTransformation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                       comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                       cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAnnihilation)
                {
                    comp.Reactions.Add(new CatalyzedAnnihilation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAssociation)
                {
                    comp.Reactions.Add(new CatalyzedAssociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedCreation)
                {
                    comp.Reactions.Add(new CatalyzedCreation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                             comp.Populations[cr.products_molecule_guid_ref[0]],
                                                             cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerization)
                {
                    comp.Reactions.Add(new CatalyzedDimerization(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerDissociation)
                {
                    comp.Reactions.Add(new CatalyzedDimerDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                      comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                      comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                      cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDissociation)
                {
                    comp.Reactions.Add(new CatalyzedDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[1]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedTransformation)
                {
                    comp.Reactions.Add(new CatalyzedTransformation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                   comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                   comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                   cr.rate_const));
                }
            }
        }

        public bool Load(SimConfiguration sc, bool completeReset, bool is_reaction_complex=false)
        {
            Scenario scenario = sc.scenario;

            if (is_reaction_complex == true)
                scenario = sc.rc_scenario;

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

            // create a factory container: shared factories reside here; not all instances of a class
            // need their own factory
            SimulationModule.kernel.Get<FactoryContainer>();

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            // clear the databasket dictionaries
            dataBasket.Clear();

            // molecules
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
            double[] extent = new double[] { dataBasket.ECS.Space.Interior.Extent(0), 
                                             dataBasket.ECS.Space.Interior.Extent(1), 
                                             dataBasket.ECS.Space.Interior.Extent(2) };

            // ADD CELLS            
            double[] state = new double[SpatialState.Dim];
            // convenience arrays to save code length
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            Compartment[] simComp = new Compartment[2];

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                // if this is a new cell population, add it
                dataBasket.AddPopulation(cp.cellpopulation_id);

                configComp[0] = sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol;
                configComp[1] = sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane;
                
                for (int i = 0; i < cp.number; i++)
                {
                    Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", sc.entity_repository.cells_dict[cp.cell_guid_ref].CellRadius));

                    // cell population id
                    cell.Population_id = cp.cellpopulation_id;
                    cell.setState(cp.cellPopDist.CellStates[i].ConfigState);

                    simComp[0] = cell.Cytosol;
                    simComp[1] = cell.PlasmaMembrane;

                    //modify molpop information before setting
                    for (int comp = 0; comp < 2; comp++)
                    {
                        foreach (ConfigMolecularPopulation cmp in configComp[comp].molpops)
                        {
                            //config_comp's distriubution changed. may need to keep 
                            //it for not customized cell later(?)

                            // if (!cp.cell_list[i].configMolPop.ContainsKey(cmp.molecule_guid_ref)) continue;
                            if (!cp.cellPopDist.CellStates[i].configMolPop.ContainsKey(cmp.molecule_guid_ref)) continue;

                            MolPopExplicit mp_explicit = new MolPopExplicit();
                            // mp_explicit.conc = cp.cell_list[i].configMolPop[cmp.molecule_guid_ref];
                            mp_explicit.conc = cp.cellPopDist.CellStates[i].configMolPop[cmp.molecule_guid_ref];
                            cmp.mpInfo.mp_distribution = mp_explicit;                              
                        }
                        addCompartmentMolpops(simComp[comp], configComp[comp], sc);
                    }

                    //CELL REACTIONS
                    // cytosol; has boundary
                    addCompartmentReactions(cell.Cytosol, cell.PlasmaMembrane, sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol, sc.entity_repository);
                    // membrane; no boundary
                    addCompartmentReactions(cell.PlasmaMembrane, null, sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane, sc.entity_repository);

                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref != "" && sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref != null)
                    {
                        MolecularPopulation driver = cell.Cytosol.Populations[sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref];

                        cell.Locomotor = new Locomotor(driver, sc.entity_repository.cells_dict[cp.cell_guid_ref].TransductionConstant);
                        cell.IsMotile = true;
                        cell.DragCoefficient = sc.entity_repository.cells_dict[cp.cell_guid_ref].DragCoefficient;
                    }
                    else
                    {
                        cell.IsMotile = false;
                    }

                    AddCell(cell);
                }
            }

            // ADD ECS MOLECULAR POPULATIONS
            addCompartmentMolpops(dataBasket.ECS.Space, scenario.environment.ecs, sc);
            // NOTE: This boolean isn't used anywhere. Do we envision a need for it?
            // Default: set diffusing
            foreach (MolecularPopulation mp in dataBasket.ECS.Space.Populations.Values)
            {
                mp.IsDiffusing = true;
            }

            // ECS molpops boundary conditions
            foreach (ConfigMolecularPopulation cmp in scenario.environment.ecs.molpops)
            {
                if (cmp.mpInfo.mp_distribution.GetType() == typeof(MolPopLinear)) {
                    MolPopLinear mpl = cmp.mpInfo.mp_distribution as MolPopLinear;
                    foreach (BoundaryCondition bc in mpl.boundaryCondition)
                    //foreach (BoundaryCondition bc in cmp.boundaryCondition)
                    {
                        int face = Simulation.dataBasket.ECS.Sides[bc.boundary.ToString()];

                        if (!dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].boundaryCondition.ContainsKey(face))
                        {
                            dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].boundaryCondition.Add(face, bc.boundaryType);

                            if (bc.boundaryType == MolBoundaryType.Dirichlet)
                            {
                                dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].NaturalBoundaryConcs[face].Initialize("const", new double[] { bc.concVal });
                            }
                            else
                            {
                                dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].NaturalBoundaryFluxes[face].Initialize("const", new double[] { bc.concVal });
                            }
                        }
                }
                }
            }

            // ADD ECS REACTIONS
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                addCompartmentReactions(dataBasket.ECS.Space, kvp.Value.PlasmaMembrane, scenario.environment.ecs, sc.entity_repository);
            }

            // general parameters
            Pair.Phi1 = sc.sim_params.phi1;
            Pair.Phi2 = sc.sim_params.phi2;

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
                // zero all cell forces; needs to happen first
                cellManager.ResetCellForces();
                // handle collisions
                if (collisionManager != null)
                {
                    collisionManager.Step(localStep);
                }
                // cell force reset happens in cell manager at the end of the cell update
                cellManager.Step(localStep);
                t += localStep;
            }
            accumulatedTime += dt;
            if (accumulatedTime >= duration)
            {
                RunStatus = RUNSTAT_FINISHED;
            }
        }

        public double AccumulatedTime
        {
            get { return accumulatedTime; }
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

        public CollisionManager CollisionManager
        {
            get { return CollisionManager; }
        }

        private CellManager cellManager;
        private CollisionManager collisionManager;
    }
}
