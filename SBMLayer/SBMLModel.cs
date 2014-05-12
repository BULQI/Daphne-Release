using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using libsbmlcs;
using System.IO;
using Daphne;
using ManifoldRing;

//C# alias to avoid having to fully qualify UNIT_KINDS
using unitType = libsbmlcs.libsbml;
using System.Collections.ObjectModel;


namespace SBMLayer
{

    public class SBMLModel
    {
        //Instance of sbmlDoc which contains a Daphne model
        SBMLDocument sbmlDoc;
        const int SBMLLEVEL = 3;
        const int SBMLVERSION = 1;

        //Instance of a model contained within sbmlDoc
        Model model;

        //Output paths
        string dirPath = string.Empty;
        string fileName = string.Empty;
        string outputPath = string.Empty;

        //Number of errors reported by consistency checkers
        long internal_errors;
        long consistency_errors;

        //Pointer to an SBML reaction object to allow for addition of reactants/products/modifiers without referencing it
        libsbmlcs.Reaction reaction;

        //Unit definition declaration
        UnitDefinition udef;

        //String used for creating output directory
        string appPath = string.Empty;

        //SimConfig object where model info is extracted from
        SimConfigurator configurator;

       /// <summary>
       /// Constructor SBMLDocument object in Level 3 Version 1 format
       /// </summary>
       /// <param name="appPath"></param>
        public SBMLModel(string appPath, SimConfigurator configurator)
        {
            sbmlDoc = new SBMLDocument(SBMLLEVEL, SBMLVERSION);
            // Creates a template SBML model and creates the respective folder if nonexistent
            model = sbmlDoc.createModel();
            this.appPath = appPath;
            this.configurator=configurator;
        }

        /// <summary>
        /// Sets modelId to the scenario name to be exported and the metaId to "Daphne"
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="metaId"></param>
        private void SetModelIds(string mid, string metaId)
        {
            model.setId(CleanIds(mid));
            model.setMetaId(CleanIds(metaId));
        }

        /// <summary>
        /// Add default model units for substance, area, volume, time and extent
        /// </summary>
        private void AddModelUnits()
        {

            // Assign item as the inheritable substance units for species declarations without a unit
            model.setSubstanceUnits("item");

            // Assign minutes as the inheritable substance units for species declarations without a unit
            model.setTimeUnits("minute");

            // Assign item as the extent of the reaction rate equations so all rate equations are measured in susbtance/time units
            model.setExtentUnits("item");

            //build per second unit and make it the default unit of area used by compartments with spatialDims=2
            udef = model.createUnitDefinition();
            Unit second = model.createUnit();
            second.setKind(unitType.UNIT_KIND_SECOND);
            second.setExponent(1);
            second.setScale(0);
            second.setMultiplier(60);
            udef.setId("minute");

            //build micro meter squared unit and make it the default unit of area used by compartments with spatialDims=2
            udef = model.createUnitDefinition();
            Unit mmetre = model.createUnit();
            mmetre.setKind(unitType.UNIT_KIND_METRE);
            mmetre.setExponent(2);
            mmetre.setScale(-6);
            mmetre.setMultiplier(1);
            udef.setId("mmetreSqred");
            model.setAreaUnits(udef.getId());

            //build micro meter cubed unit and make it the default unit of volume used by compartments with spatialDims=3
            udef = model.createUnitDefinition();
            mmetre = model.createUnit();
            mmetre.setKind(unitType.UNIT_KIND_METRE);
            mmetre.setExponent(3);
            mmetre.setScale(-6);
            mmetre.setMultiplier(1);
            udef.setId("mmetreCubed");
            model.setVolumeUnits(udef.getId());
        }

        /// <summary>
        /// Adds the appropriate units for the indicated reaction type to the enclosing SBML model
        /// </summary>
        /// <param name="reactionType"></param>
        /// <returns>String corresponding to specific unitId</returns>
        private string AddReactionSpecificUnits(string reactionType)
        {

            if (reactionType.Equals("BoundaryDissociation") || reactionType.Equals("Annihilation") || reactionType.Equals("Dissociation")
                || reactionType.Equals("DimerDissociation") || reactionType.Equals("CatalyzedCreation") || reactionType.Equals("Transformation"))
            {
                if (model.getUnitDefinition("per_minute") != null)
                {
                    return "per_minute";
                }
                else
                {
                    udef = model.createUnitDefinition();
                    //build per min unit
                    Unit unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_SECOND);
                    unit.setExponent(-1);
                    unit.setScale(0);
                    unit.setMultiplier(60);
                    udef.setId("per_minute");
                    return "per_minute";
                }
            }
            else if (reactionType.Equals("BoundaryTransportTo") || reactionType.Equals("BoundaryTransportFrom"))
            {
                if (model.getUnitDefinition("mmetre_per_minute") != null)
                {
                    return "mmetre_per_minute";
                }
                else
                {
                    udef = model.createUnitDefinition();
                    //build micro meter per min unit
                    Unit unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_METRE);
                    unit.setExponent(1);
                    unit.setScale(-6);
                    unit.setMultiplier(1);

                    unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_SECOND);
                    unit.setExponent(-1);
                    unit.setScale(0);
                    unit.setMultiplier(60);
                    udef.setId("mmetre_per_minute");
                    return "mmetre_per_minute";
                }
            }
            else if (reactionType.Equals("CatalyzedBoundaryActivation"))
            {
                if (model.getUnitDefinition("mmetreSqred_per_item_per_minute") != null)
                {
                    return "mmetreSqred_per_item_per_minute";
                }
                else
                {
                    udef = model.createUnitDefinition();
                    //build micro meter sqrd per item per minute unit
                    Unit unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_METRE);
                    unit.setExponent(2);
                    unit.setScale(-6);
                    unit.setMultiplier(1);

                    unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_ITEM);
                    unit.setExponent(-1);
                    unit.setScale(1);
                    unit.setMultiplier(1);

                    unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_SECOND);
                    unit.setExponent(-1);
                    unit.setScale(0);
                    unit.setMultiplier(60);
                    udef.setId("mmetreSqred_per_item_per_minute");
                    return "mmetreSqred_per_item_per_minute";
                }
            }
            else if (reactionType.Equals("BoundaryAssociation") || reactionType.Equals("Association") || reactionType.Equals("AutocatalyticTransformation")
                 || reactionType.Equals("CatalyzedAnnihilation") || reactionType.Equals("CatalyzedDimerDissociation") || reactionType.Equals("CatalyzedTransformation")
                || reactionType.Equals("CatalyzedDimerization") || reactionType.Equals("CatalyzedDissociation") || reactionType.Equals("Dimerization"))
            {

                if (model.getUnitDefinition("mmetreCubed_per_item_per_minute") != null)
                {
                    return "mmetreCubed_per_item_per_minute";
                }
                else
                {
                    udef = model.createUnitDefinition();
                    //build micro meter cubed per item per minute unit
                    Unit unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_METRE);
                    unit.setExponent(3);
                    unit.setScale(-6);
                    unit.setMultiplier(1);

                    unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_ITEM);
                    unit.setExponent(-1);
                    unit.setScale(1);
                    unit.setMultiplier(1);

                    unit = model.createUnit();
                    unit.setKind(unitType.UNIT_KIND_SECOND);
                    unit.setExponent(-1);
                    unit.setScale(0);
                    unit.setMultiplier(60);
                    udef.setId("mmetreCubed_per_item_per_minute");
                    return "mmetreCubed_per_item_per_minute";
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Adds a compartment to the current SBML model
        /// </summary>
        /// <param name="compId"></param>
        /// <param name="compName"></param>
        /// <param name="constant"></param>
        /// <param name="outside"></param>
        /// <param name="size"></param>
        /// <param name="units"></param>
        /// <param name="dims"></param>
        /// <param name="volume"></param>
        private void AddCompartment(string cid, string name, bool constant, double size, int dims, string units = "")
        {
            libsbmlcs.Compartment comp = model.createCompartment();
            comp.setId(CleanIds(cid));
            comp.setName(name);
            comp.setConstant(constant);

            if (!units.Equals(string.Empty))
            {
                comp.setUnits(units);
            }
            comp.setSize(size);
            comp.setSpatialDimensions(dims);

        }

        /// <summary>
        /// Adds a species to the current SBML model
        /// </summary>
        /// <param name="speciesId"></param>
        /// <param name="speciesName"></param>
        /// <param name="speciesComp"></param>
        /// <param name="boundary"></param>
        /// <param name="constant"></param>
        /// <param name="hasonlySubstanceUnits"></param>
        /// <param name="initialAmount"></param>
        private void AddSpecies(string sid, string name, string compartment, bool boundary, bool constant, bool hasonlySubstanceUnits, double initialConcentration, string units = "")
        {
            Species species = model.createSpecies();
            species.setId(CleanIds(sid));
            species.setName(name);
            species.setCompartment(compartment);
            species.setBoundaryCondition(boundary);
            species.setConstant(constant);
            species.setHasOnlySubstanceUnits(hasonlySubstanceUnits);
            species.setInitialConcentration(initialConcentration);
            if (!units.Equals(string.Empty))
            {
                species.setUnits(units);
            }
        }

        /// <summary>
        /// Adds a parameter to the current SBML model
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="constant">Determines whether the param is a param (true) or a variable (false)</param>
        private void AddParameter(string pid, string name, double value, bool constant = true, string units = "")
        {
            if (model.getParameter(CleanIds(pid)) == null)
            {
                Parameter param = model.createParameter();
                param.setId(CleanIds(pid));
                param.setName(name);
                param.setValue(value);
                param.setConstant(constant);
                if (!units.Equals(string.Empty))
                {
                    param.setUnits(units);
                }
            }
        }

        /// <summary>
        /// Adds a reaction to the current SBML model
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="name"></param>
        /// <param name="fast"></param>
        /// <param name="compartment"></param>
        /// <param name="reversible"></param>
        private void AddReaction(string rid, string name, bool fast, string compartment = "", bool reversible = false)
        {
            reaction = null; //Resets the instace to reaction
            reaction = model.createReaction();
            reaction.setId(CleanIds(rid));
            reaction.setName(name);
            reaction.setReversible(reversible);
            reaction.setFast(fast);
            if (!compartment.Equals(string.Empty))
            {
                reaction.setCompartment(compartment);
            }
        }

        /// <summary>
        /// Adds a reactant/product to the instance of an SBML reaction object stored in "reaction"
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="stoich"></param>
        /// <param name="constant"></param>
        /// <param name="flag"></param>
        private void AddReactProd(string sid, bool constant, bool isReactant, double stoich = 0)
        {
            libsbmlcs.SpeciesReference speciesRef = (isReactant ? model.createReactant() : model.createProduct());
            speciesRef.setSpecies(CleanIds(sid));
            if (stoich > 0) { speciesRef.setStoichiometry(stoich); }
            speciesRef.setConstant(constant);
        }

        /// <summary>
        /// Adds a modifier to the current SBML reaction instance
        /// </summary>
        /// <param name="sid"></param>
        private void AddModifier(string sid)
        {
            ModifierSpeciesReference modSpeciesRef = reaction.createModifier();
            modSpeciesRef.setSpecies(CleanIds(sid));
        }

        /// <summary>
        /// Adds a kinetic Law component to the current SBML reaction instance. Corrections for volume can be incorported by having explicit compartment terms or by including them into the rate constant
        /// </summary>
        /// <param name="reactionType"></param>
        /// <param name="reactants"></param>
        /// <param name="rateConstant"></param>
        private void AddKineticLaw(string reactionType, string[] reactants, string compartment, string rateConstant)
        {
            KineticLaw kinetic = reaction.createKineticLaw();
            string rateEquation = string.Empty;
            if (reactionType.Equals("BoundaryDissociation") || reactionType.Equals("BoundaryTransportFrom") || reactionType.Equals("BoundaryTransportTo")
                || reactionType.Equals("Annihilation") || reactionType.Equals("Dissociation") || reactionType.Equals("DimerDissociation")
                || reactionType.Equals("CatalyzedCreation") || reactionType.Equals("Transformation"))
            {
                rateEquation = rateConstant + "*" + compartment + "*" + CleanIds(reactants[0]);
            }
            else if (reactionType.Equals("BoundaryAssociation") || reactionType.Equals("CatalyzedBoundaryActivation") || reactionType.Equals("Association")
                || reactionType.Equals("AutocatalyticTransformation") || reactionType.Equals("CatalyzedAnnihilation") || reactionType.Equals("CatalyzedDimerDissociation")
                || reactionType.Equals("CatalyzedTransformation") || reactionType.Equals("CatalyzedDimerization") || reactionType.Equals("CatalyzedDissociation")
                || reactionType.Equals("Dimerization"))
            {
                rateEquation = rateConstant + "*" + compartment + "*" + CleanIds(reactants[0]) + "*" + CleanIds(reactants[1]);
            }

            //Transform string kinetic law into MathML representation and add to kinetic law
            ASTNode root = libsbml.parseFormula(rateEquation);
            kinetic.setMath(root);
        }

        /// <summary>
        /// As model attributes are added individually, these checks ensure consistency and adherence to SBML specifications when combined
        /// </summary>
        private void CheckModelConsistency()
        {
            //reports on fundamental syntactic and structural errors that violate the XML Schema for SBML
            internal_errors = sbmlDoc.checkInternalConsistency();

            //performs more elaborate model verifications and also validation according to SBML validation rules 
            consistency_errors = sbmlDoc.checkConsistency();

            //prints errors in a file or a message of success
            ReportErrors(internal_errors + consistency_errors);
        }

        /// <summary>
        /// Writes a log file reporting internal and cosistency errors in the SBML model
        /// </summary>
        /// <returns></returns>
        private void ReportErrors(long errors)
        {
            //prints errors to file
            SBMLError error;
            string message = DateTime.UtcNow.ToLocalTime() + Environment.NewLine;

            if (errors > 0)
            {
                for (int i = 0; i < errors; i++)
                {
                    error = sbmlDoc.getError(i);
                    message += error.getSeverityAsString() + " : " + error.getMessage() + Environment.NewLine;
                }
            }
            else
            {
                message = DateTime.UtcNow.ToLocalTime() + Environment.NewLine;
                //message = DateTime.UtcNow;
                message += "The model " + fileName + " was correctly encoded into SBML";
            }


            using (System.IO.StreamWriter file = new System.IO.StreamWriter(dirPath + fileName + "_log.txt"))
            {
                file.Write(message);
            }
        }

        /// <summary>
        /// Writes out current SBMLDocument to file
        /// </summary>
        /// <returns>flag indicating success</returns>
        private bool WriteSBMLModel()
        {
            SBMLWriter writer = new SBMLWriter();
            writer.writeSBML(sbmlDoc, outputPath);
            return true;
        }

        /// <summary>
        /// Makes sure that strings used as ids of SBML elements comply to the language specification for SId type
        /// letter ::= 'a'..'z','A'..'Z'
        /// digit ::= '0'..'9'
        /// idChar ::= letter | digit | ' '
        /// SId ::= ( letter | ' ' ) idChar*
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string CleanIds(string id)
        {
            id = id.Trim();
            id = id.TrimEnd('.');
            id = Regex.Replace((id), @"\s+|%|:", "_");
            id = Regex.Replace((id), @"\|", "_Membrane");
            id = Regex.Replace((id), @"\*", "_Activated");
            return id;
        }

        /// <summary>
        /// Structures user output directory for SBML and log files to be saved
        /// </summary>
        /// <param name="path"></param>
        private string SetUpDirectory(string path)
        {
            string SBML_folder = new Uri(path).LocalPath;
            if (!Directory.Exists(SBML_folder)) { Directory.CreateDirectory(SBML_folder); }
            return path;
        }

        /// <summary>
        /// Copies directory file and output paths 
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="fileName"></param>
        private void SetPaths(string dirPath, string fileName)
        {
            this.dirPath = dirPath;
            this.fileName = fileName;
            this.outputPath = dirPath + fileName + ".xml";
        }

        /// <summary>
        /// Calculates the concentration of species when its distribution is Gaussian
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        private double CalculateGaussianConcentration(BoxSpecification spec, ConfigMolecularPopulation confMolPop)
        {

            return ((Math.Pow(2 * Math.PI, 3 / 2)) * (((MolPopGaussian)confMolPop.mpInfo.mp_distribution).peak_concentration)) / (spec.x_scale * spec.y_scale * spec.z_scale);
        }

        /// <summary>
        /// Exports the current simulation model in the configurator into SBML format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ConvertToSBML()
        {
            SetPaths(Uri.UnescapeDataString(new Uri(SetUpDirectory(appPath + @"\SBML\")).LocalPath), configurator.SimConfig.experiment_name.Replace(".", string.Empty));

            //Populates model ids in SBML template
            SetModelIds(configurator.SimConfig.experiment_name, "Daphne");

            //Defines model-wide units for volume, substance, time and extents(used by compartments/entities)
            AddModelUnits();

            //adds ECS compartment
            double ecsVol = (configurator.SimConfig.scenario.environment.extent_x) * (configurator.SimConfig.scenario.environment.extent_y) * (configurator.SimConfig.scenario.environment.extent_z);
            AddCompartment("ECS", "ECS", true, ecsVol, configurator.SimConfig.scenario.environment.NumGridPts.Length, "");

            //adds species in the ECS
            BoxSpecification spec;
            string confMolPopName = string.Empty;
            foreach (ConfigMolecularPopulation confMolPop in configurator.SimConfig.scenario.environment.ecs.molpops)
            {
                //We need to create SBML species with the type of the molecular population and not with the name as the latter is user defined.
                confMolPopName = configurator.SimConfig.entity_repository.molecules_dict[confMolPop.molecule_guid_ref].Name;
                if (confMolPop.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    spec = configurator.SimConfig.box_guid_box_dict[(configurator.SimConfig.entity_repository.gauss_guid_gauss_dict[((MolPopGaussian)confMolPop.mpInfo.mp_distribution).gaussgrad_gauss_spec_guid_ref]).gaussian_spec_box_guid_ref];
                    AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, CalculateGaussianConcentration(spec, confMolPop), "");
                }
                else if (confMolPop.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((MolPopHomogeneousLevel)confMolPop.mpInfo.mp_distribution).concentration, "");
                }
                else if (confMolPop.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
                {
                    AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((MolPopExplicit)confMolPop.mpInfo.mp_distribution).conc.Average(), "");
                }
                else if (confMolPop.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    if ((((MolPopLinear)confMolPop.mpInfo.mp_distribution).boundaryCondition != null) && (((MolPopLinear)confMolPop.mpInfo.mp_distribution).boundaryCondition.Count == 2))
                    {
                        AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((((MolPopLinear)confMolPop.mpInfo.mp_distribution).boundaryCondition[0].concVal) + (((MolPopLinear)confMolPop.mpInfo.mp_distribution).boundaryCondition[1].concVal)) / 2, "");
                    }
                    else
                    {
                        //MessageBox.Show("Model could not be saved into SBML as linear gradient values could not be found", "Linear Gradient Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                }
            }

            //adds reactions in the ECS
            ConfigReaction cr;
            foreach (string rguid in configurator.SimConfig.scenario.environment.ecs.reactions_guid_ref)
            {
                cr = configurator.SimConfig.entity_repository.reactions_dict[rguid];
                AddSBMLReactions(cr, cr.rate_const, configurator.SimConfig.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, "ECS", string.Empty);
            }

            //Add a membrane compartment and a cytosol compartment for each cell type
            string cellMembraneId, cytosolId = string.Empty;
            TinySphere sphere;
            TinyBall ball;
            foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
            {
                //Name added to compartment includes cell type
                //Membrane
                cellMembraneId = string.Concat("Membrane_", configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellName);
                double radius = configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellRadius;
                sphere = new TinySphere();
                sphere.Initialize(new double[] { radius });
                AddCompartment(cellMembraneId, cellMembraneId, true, sphere.Area(), sphere.Dim, "");

                //Setup necessary to extract molecular populations from each compartment (cytosol/membrane)
                ConfigCompartment[] configComp = new ConfigCompartment[2];
                configComp[0] = configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].cytosol;
                configComp[1] = configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].membrane;

                //Only add a cytosol compartment if there are any molecular populations in the cytosol 
                if (configComp[0].molpops.Count > 0)
                {
                    cytosolId = string.Concat("Cytosol_", configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellName);
                    ball = new TinyBall();
                    ball.Initialize(new double[] { radius });
                    AddCompartment(cytosolId, cytosolId, true, ball.Volume(), ball.Dim, "");
                }

                for (int comp = 0; comp < 2; comp++)
                {
                    //0 cytosol, 1 membrane (both assume uniform distribution of molecular populations
                    foreach (ConfigMolecularPopulation cmp in configComp[comp].molpops)
                    {
                        //We need to create SBML species with the type of the molecular population and not with the name as the latter is user defined.
                        confMolPopName = configurator.SimConfig.entity_repository.molecules_dict[cmp.molecule_guid_ref].Name;
                        if (comp == 0)
                        {
                            //Add cytosol molecular species
                            AddSpecies(confMolPopName + "_" + cytosolId, confMolPopName, cytosolId, false, false, false, ((MolPopHomogeneousLevel)cmp.mpInfo.mp_distribution).concentration, "");
                        }
                        else
                        {
                            //Add membrane molecular species
                            AddSpecies(confMolPopName.TrimEnd('|') + "_" + cellMembraneId, confMolPopName, cellMembraneId, false, false, false, ((MolPopHomogeneousLevel)cmp.mpInfo.mp_distribution).concentration, "");
                        }
                    }
                }
            }

            //Add reactions belonging to all cellPopulations and specify which compartment they take place in
            foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
            {

                foreach (string reaction in configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].membrane.reactions_guid_ref)
                {
                    cr = configurator.SimConfig.entity_repository.reactions_dict[reaction];
                    cellMembraneId = string.Concat("Membrane_", configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellName);
                    AddSBMLReactions(cr, cr.rate_const, configurator.SimConfig.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, cellMembraneId, cellPop.cell_guid_ref);
                }

                foreach (string reaction in configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].cytosol.reactions_guid_ref)
                {
                    cr = configurator.SimConfig.entity_repository.reactions_dict[reaction];
                    cytosolId = string.Concat("Cytosol_", configurator.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellName);
                    AddSBMLReactions(cr, cr.rate_const, configurator.SimConfig.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, cytosolId, cellPop.cell_guid_ref);
                }
            }

            //Check for model consistency and serialize to file (provide output stream for log)
            CheckModelConsistency();

            //Ask user where to save it. Imitate what happens in saveScenarioUsingDialog()
            WriteSBMLModel();

        }

        /// <summary>
        /// Tests whether the given molecule is present in the given compartment for a given cellPopulation
        /// </summary>
        /// <param name="cellPop_guid_ref"></param>
        /// <param name="molecule"></param>
        /// <param name="compartment"></param>
        /// <returns></returns>
        private bool ExistMolecule(string cellId, string molecule, string compartment)
        {
            ObservableCollection<ConfigMolecularPopulation> configComp = new ObservableCollection<ConfigMolecularPopulation>();
            if (compartment.ToLower().Contains("membrane"))
            {
                configComp = configurator.SimConfig.entity_repository.cells_dict[cellId].membrane.molpops;
            }
            else if (compartment.ToLower().Contains("cytosol"))
            {
                configComp = configurator.SimConfig.entity_repository.cells_dict[cellId].cytosol.molpops;
            }
            else if (compartment.Equals("ECS"))
            {
                configComp = configurator.SimConfig.scenario.environment.ecs.molpops;
            }

            foreach (ConfigMolecularPopulation cmp in configComp)
            {
                if (configurator.SimConfig.entity_repository.molecules_dict[cmp.molecule_guid_ref].Name.Equals(molecule))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///  Parses reaction object prior to encoding members into SBML
        /// </summary>
        /// <param name="reaction"></param>
        /// <param name="type"></param>
        /// <param name="compartment"></param>
        private void AddSBMLReactions(ConfigReaction cr, double rateConstant, ReactionType type, string compartment, string cellId)
        {
            string receptor, ligand, complex, reactant, product, membrane, bulk, bulkActivated, modifier, rid;

            if (type == ReactionType.BoundaryAssociation)
            {
                receptor = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                ligand = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                complex = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
                    {
                        AddBoundaryAssociation(cellPop.cell_guid_ref, receptor, ligand, complex, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryAssociation(cellId, receptor, ligand, complex, compartment, rateConstant);
                }
            }
            else if (type == ReactionType.BoundaryDissociation)
            {
                receptor = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;
                ligand = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                complex = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
                    {
                        AddBoundaryDissociation(cellPop.cell_guid_ref, receptor, ligand, complex, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryDissociation(cellId, receptor, ligand, complex, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.BoundaryTransportFrom)
            {
                membrane = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
                    {
                        AddBoundaryTransportFromReaction(cellPop.cell_guid_ref, bulk, membrane, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryTransportFromReaction(cellId, bulk, membrane, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.BoundaryTransportTo)
            {
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                membrane = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
                    {
                        AddBoundaryTransportToReaction(cellPop.cell_guid_ref, bulk, membrane, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryTransportToReaction(cellId, bulk, membrane, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.CatalyzedBoundaryActivation)
            {
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulkActivated = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in configurator.SimConfig.scenario.cellpopulations)
                    {
                        AddCatalyzedBoundaryActivation(cellPop.cell_guid_ref, bulk, bulkActivated, modifier, compartment, rateConstant);
                    }
                }
                else
                {
                    AddCatalyzedBoundaryActivation(cellId, bulk, bulkActivated, modifier, compartment, rateConstant);
                }
            }
            else if (type == ReactionType.Annihilation)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Annihilation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Annhil", "k_Annhil", rateConstant, false, AddReactionSpecificUnits("Annihilation"));
                    AddKineticLaw("Annihilation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Annhil");
                }

            }
            else if (type == ReactionType.Association)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "Association_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(bulk + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Assoc", "k_Assoc", rateConstant, false, AddReactionSpecificUnits("Association"));
                    AddKineticLaw("Association", new string[] { (reactant + "_" + compartment), (bulk + "_" + compartment) }, compartment, "k_Assoc");
                }
            }
            else if (type == ReactionType.Dissociation)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "Dissociation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);
                    AddReactProd(bulk + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Dissoc", "k_Dissoc", rateConstant, false,AddReactionSpecificUnits("Dissociation"));
                    AddKineticLaw("Dissociation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Dissoc");
                }
            }
            else if (type == ReactionType.DimerDissociation)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "DimerDissociation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_DimerDissoc", "k_DimerDissoc", rateConstant, false, AddReactionSpecificUnits("DimerDissociation"));
                    AddKineticLaw("DimerDissociation", new string[] { (reactant + "_" + compartment) }, compartment, "k_DimerDissoc");
                }
            }
            else if (type == ReactionType.AutocatalyticTransformation)
            {
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment))
                {
                    rid = "AutocatalyticTransformation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(bulk + "_" + compartment, false, true, 1);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(bulk + "_" + compartment, false, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_AutoTrans", "k_AutoTrans", rateConstant, false, AddReactionSpecificUnits("AutocatalyticTransformation"));
                    AddKineticLaw("AutocatalyticTransformation", new string[] { (reactant + "_" + compartment), (bulk + "_" + compartment) }, compartment, "k_AutoTrans");
                }
            }
            else if (type == ReactionType.CatalyzedAnnihilation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, modifier, compartment))
                {
                    rid = "CatalyzedAnnihilation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatAnnih", "k_CatAnnih", rateConstant, false, AddReactionSpecificUnits("CatalyzedAnnihilation"));
                    AddKineticLaw("CatalyzedAnnihilation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatAnnih");
                }
            }
            else if (type == ReactionType.CatalyzedAssociation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "CatalyzedAssociation_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(bulk + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatAssoc", "k_CatAssoc", rateConstant, false, AddReactionSpecificUnits("CatalyzedAssociation"));
                    AddKineticLaw("CatalyzedAssociation", new string[] { (bulk + "_" + compartment), (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatAssoc");
                }
            }
            else if (type == ReactionType.CatalyzedCreation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment))
                {
                    rid = "CatalyzedCreation_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatCreat", "k_CatCreat", rateConstant, false, AddReactionSpecificUnits("CatalyzedCreation"));
                    AddKineticLaw("CatalyzedCreation", new string[] { (modifier + "_" + compartment) }, compartment, "k_CatCreat");
                }
            }
            else if (type == ReactionType.CatalyzedDimerDissociation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedDimerDissociation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_DimDissoc", "k_DimDissoc", rateConstant, false, AddReactionSpecificUnits("CatalyzedDimerDissociation"));
                    AddKineticLaw("CatalyzedDimerDissociation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_DimDissoc");
                }
            }
            else if (type == ReactionType.CatalyzedTransformation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedTransformation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatTrans", "k_CatTrans", rateConstant, false, AddReactionSpecificUnits("CatalyzedTransformation"));
                    AddKineticLaw("CatalyzedTransformation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatTrans");
                }
            }
            else if (type == ReactionType.CatalyzedDimerization)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedDimerization_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 2);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatDimer", "k_CatDimer", rateConstant, false, AddReactionSpecificUnits("CatalyzedDimerization"));
                    AddKineticLaw("CatalyzedDimerization", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatDimer");
                }
            }
            else if (type == ReactionType.CatalyzedDissociation)
            {
                modifier = configurator.SimConfig.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                bulk = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment))
                {
                    rid = "CatalyzedDissociation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);
                    AddReactProd(bulk + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatDissoc", "k_CatDissoc", rateConstant, false, AddReactionSpecificUnits("CatalyzedDissociation"));
                    AddKineticLaw("CatalyzedDissociation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatDissoc");
                }
            }
            else if (type == ReactionType.Dimerization)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Dimerization_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 2);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Dimer", "k_Dimer", rateConstant, false, AddReactionSpecificUnits("Dimerization"));
                    AddKineticLaw("Dimerization", new string[] { (reactant + "_" + compartment), (reactant + "_" + compartment) }, compartment, "k_Dimer");
                }
            }
            else if (type == ReactionType.Transformation)
            {
                reactant = configurator.SimConfig.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = configurator.SimConfig.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Transformation_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, false, true, 1);
                    AddReactProd(product + "_" + compartment, false, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Trans", "k_Trans", rateConstant, false, AddReactionSpecificUnits("Transformation"));
                    AddKineticLaw("Transformation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Trans");
                }
            }
        }

        /// <summary>
        /// Method that adds Boundary Transport From Reactions
        /// </summary>
        /// <param name="encodedSBML"></param>
        /// <param name="cellIdentifier"></param>
        /// <param name="bulk"></param>
        /// <param name="membrane"></param>
        /// <param name="compartment"></param>
        /// <param name="rateConstant"></param>
        private void AddBoundaryTransportFromReaction(string cellIdentifier, string bulk, string membrane, string compartment, double rateConstant)
        {
            string cellName = configurator.SimConfig.entity_repository.cells_dict[cellIdentifier].CellName;
            ////need to check whether reactant is on membrane (associate with proper reactant on each cell) 
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, membrane, "membrane"))
            {
                string rid = "BoundaryTransportFrom_" + membrane + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(membrane + "_" + cellName, false, true, 1);
                AddReactProd(bulk + "_" + compartment, false, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BTransFrom", "k_BTransFrom", rateConstant, false, AddReactionSpecificUnits("BoundaryTransportFrom"));
                AddKineticLaw("BoundaryTransportFrom", new string[] { (bulk + "_" + compartment) }, ("Membrane" + "_" + cellName), "k_BTransFrom");
            }

        }

        /// <summary>
        /// Method that adds Boundary Transport To Reactions
        /// </summary>
        /// <param name="encodedSBML"></param>
        /// <param name="cellIdentifier"></param>
        /// <param name="bulk"></param>
        /// <param name="membrane"></param>
        /// <param name="compartment"></param>
        /// <param name="rateConstant"></param>
        private void AddBoundaryTransportToReaction(string cellIdentifier, string bulk, string membrane, string compartment, double rateConstant)
        {
            string cellName = configurator.SimConfig.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether product is on the membrane
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, membrane, "membrane"))
            {
                string rid = "BoundaryTransportTo_" + bulk + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(bulk + "_" + compartment, false, true, 1);
                AddReactProd(membrane + "_" + cellName, false, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BTransTo", "k_BTransTo", rateConstant, false, AddReactionSpecificUnits("BoundaryTransportTo"));
                AddKineticLaw("BoundaryTransportTo", new string[] { (bulk + "_" + compartment) }, ("Membrane" + "_" + cellName), "k_BTransTo");
            }
        }

        /// <summary>
        ///  Method that adds Catalized Boundary Activation Reactions
        /// </summary>
        /// <param name="encodedSBML"></param>
        /// <param name="cellIdentifier"></param>
        /// <param name="bulk"></param>
        /// <param name="bulkActivated"></param>
        /// <param name="modifier"></param>
        /// <param name="compartment"></param>
        /// <param name="rateConstant"></param>
        private void AddCatalyzedBoundaryActivation(string cellIdentifier, string bulk, string bulkActivated, string modifier, string compartment, double rateConstant)
        {
            string cellName = configurator.SimConfig.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether modifier(receptor) is on the membrane
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, bulkActivated, compartment) & ExistMolecule(cellIdentifier, modifier, "membrane"))
            {
                string rid = "CatalyzedBoundaryActivation_" + modifier + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(bulk + "_" + compartment, false, true, 1);
                AddReactProd(bulkActivated + "_" + compartment, false, false, 1);
                AddModifier(modifier + "_" + cellName);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_CatBActiv", "k_CatBActiv", rateConstant, false, AddReactionSpecificUnits("CatalyzedBoundaryActivation"));
                AddKineticLaw("CatalyzedBoundaryActivation", new string[] { (bulk + "_" + compartment), (modifier + "_" + cellName) }, compartment, "k_CatBActiv");
            }
        }

        /// <summary>
        /// Method that adds Boundary Dissociation Reactions
        /// </summary>
        /// <param name="encodedSBML"></param>
        /// <param name="cellIdentifier"></param>
        /// <param name="receptor"></param>
        /// <param name="ligand"></param>
        /// <param name="complex"></param>
        /// <param name="compartment"></param>
        /// <param name="rateConstant"></param>
        private void AddBoundaryDissociation(string cellIdentifier, string receptor, string ligand, string complex, string compartment, double rateConstant)
        {
            string cellName = configurator.SimConfig.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether reactant 1 and product 1 are on the membrane (associate with proper reactant on each cell) 
            if (ExistMolecule(cellIdentifier, ligand, compartment) & ExistMolecule(cellIdentifier, receptor, "membrane") & ExistMolecule(cellIdentifier, complex, "membrane"))
            {
                string rid = "BoundaryDissociation_" + complex + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);
                AddReactProd(complex + "_" + cellName, false, true, 1);
                AddReactProd(receptor + "_" + cellName, false, false, 1);
                AddReactProd(ligand + "_" + compartment, false, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BDissoc", "k_BDissoc", rateConstant, false, AddReactionSpecificUnits("BoundaryDissociation"));
                AddKineticLaw("BoundaryDissociation", new string[] { (complex + "_" + cellName) }, ("Membrane" + "_" + cellName), "k_BDissoc");
            }
        }

        /// <summary>
        /// Method that adds Boundary Association Reactions
        /// </summary>
        /// <param name="encodedSBML"></param>
        /// <param name="cellIdentifier"></param>
        /// <param name="receptor"></param>
        /// <param name="ligand"></param>
        /// <param name="complex"></param>
        /// <param name="compartment"></param>
        /// <param name="rateConstant"></param>
        private void AddBoundaryAssociation(string cellIdentifier, string receptor, string ligand, string complex, string compartment, double rateConstant)
        {
            string cellName = configurator.SimConfig.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether reactant 1 and product 1 are on the membrane (associate with proper reactant on each cell) and whether ligand is on compartment
            if (ExistMolecule(cellIdentifier, ligand, compartment) & ExistMolecule(cellIdentifier, receptor, "membrane") & ExistMolecule(cellIdentifier, complex, "membrane"))
            {
                string rid = "BoundaryAssociation_" + complex + "_" + cellName;
                //add reaction components 
                AddReaction(rid, rid, false, compartment, false);
                AddReactProd(receptor + "_" + cellName, false, true, 1);
                AddReactProd(ligand + "_" + compartment, false, true, 1);
                AddReactProd(complex + "_" + cellName, false, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BAssoc", "k_BAssoc", rateConstant, false, AddReactionSpecificUnits("BoundaryAssociation"));
                AddKineticLaw("BoundaryAssociation", new string[] { (ligand + "_" + compartment), (receptor + "_" + cellName) }, ("Membrane" + "_" + cellName), "k_BAssoc");
            }

        }
    }
}
