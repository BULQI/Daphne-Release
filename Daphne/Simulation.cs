using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public class Simulation
    {
        public static DataBasket dataBasket;

        public Simulation()
        {
            cellManager = new CellManager();
            dataBasket = new DataBasket();
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

        private void addCompartmentMolpops(Compartment simComp, ConfigCompartment configComp)
        {
            foreach (ConfigMolecularPopulation cmp in configComp.molpops)
            {
                if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    MolPopGaussian mpgg = (MolPopGaussian)cmp.mpInfo.mp_distribution;

                    double[] initArray = new double[] { mpgg.Center.X, 
                                                        mpgg.Center.Y,
                                                        mpgg.Center.Z,
                                                        mpgg.Sigma.X,
                                                        mpgg.Sigma.Y,
                                                        mpgg.Sigma.Z,
                                                        mpgg.Peak };

                    simComp.AddMolecularPopulation(dataBasket.Molecules[cmp.molecule_guid_ref], "gauss", initArray);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Uniform)
                {
                    MolPopUniform mphl = (MolPopUniform)cmp.mpInfo.mp_distribution;
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

        public void Load(SimConfiguration sc)
        {
            Scenario scenario = sc.scenario;

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

            // cells
            dataBasket.Cells.Clear();

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
                        addCompartmentMolpops(simComp[comp], configComp[comp]);
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
            addCompartmentMolpops(dataBasket.ECS.Space, scenario.environment.ecs);
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
        }

        public CellManager CMGR
        {
            get { return cellManager; }
        }

        private CellManager cellManager;
    }
}
