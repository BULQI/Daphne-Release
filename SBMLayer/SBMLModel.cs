using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using libsbmlcs;
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
        private SBMLDocument sbmlDoc;
        private const int SBMLLEVEL = 3;
        private const int SBMLVERSION = 1;
        private const int SPATIALPKGVERSION = 1;
        private const string spatialPackageName = "spatial";

        //Handles spatial components of current SBML model
        private SpatialComponents spatialComponents;
        private SpatialModelPlugin spatialModelPlugin;
        private Geometry geometry;
        private CSGeometry csgGeometry;

        //Instance of a model contained within sbmlDoc
        private Model model;

        //Namespace used for SBML model annotations (unique URI rather than an actual web address)
        private string annotNamespace = "http://www.daphneURI.com";
        private string annotprefix = "daphne";

        //Output paths
        private string dirPath = string.Empty;
        private string fileName = string.Empty;
        private string inputLogFile = string.Empty;
        private string outputLogFile = string.Empty;

        //Number of errors reported by consistency checkers
        private long internal_errors;
        private long consistency_errors;

        //Pointer to an SBML reaction object to allow for addition of reactants/products/modifiers without referencing it
        private libsbmlcs.Reaction currentGlobReaction;

        //Unit definition declaration
        private UnitDefinition udef;

        //Indicates whether Protocol file contains a ReactionComplex vs. Spatial simulation
        private Boolean reactionComplexFlag;

        //String used for creating output directory
        private string appPath = string.Empty;

        //Protocol object where model info is extracted from
        private Protocol protocol;

        private Boolean isSpatialModel;

        private TissueScenario scenario;
        private ConfigECSEnvironment envHandle;

        /// <summary>
        /// Sets Paths for SBMLDocument - Level 3 Version 1 format
        /// </summary>
        /// <param name="appPath"></param>
        /// <param name="protocol"></param>
        public SBMLModel(string appPath, Protocol protocol)
        {
            // Creates a template SBML model
            this.appPath = appPath;
            this.protocol = protocol;
            
            // for now, restrict to the tissue scenario
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }
            scenario = (TissueScenario)protocol.scenario;
            envHandle = (ConfigECSEnvironment)scenario.environment;
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
        private void AddModelUnits(Boolean isReactionComplex = false)
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

            //build micro meter cubed unit and make it the default unit of volume used by compartments with spatialDims=3
            Unit mmetre;
            udef = model.createUnitDefinition();
            mmetre = model.createUnit();
            mmetre.setKind(unitType.UNIT_KIND_METRE);
            mmetre.setExponent(3);
            mmetre.setScale(-6);
            mmetre.setMultiplier(1);
            udef.setId("mmetreCubed");
            model.setVolumeUnits(udef.getId());

            if (!isReactionComplex)
            {
                //build micro meter squared unit and make it the default unit of area used by compartments with spatialDims=2
                udef = model.createUnitDefinition();
                mmetre = model.createUnit();
                mmetre.setKind(unitType.UNIT_KIND_METRE);
                mmetre.setExponent(2);
                mmetre.setScale(-6);
                mmetre.setMultiplier(1);
                udef.setId("mmetreSqred");
                model.setAreaUnits(udef.getId());
            }
        }

        /// <summary>
        /// Adds the appropriate units for the indicated reaction type to the enclosing SBML model
        /// </summary>
        /// <param name="reactionType"></param>
        /// <returns>String corresponding to specific unitId</returns>
        private string AddReactionSpecificUnits(string reactionType)
        {

            if (reactionType.Equals("BoundaryDissociation") || reactionType.Equals("Annihilation") || reactionType.Equals("Dissociation")
                || reactionType.Equals("DimerDissociation") || reactionType.Equals("CatalyzedCreation") || reactionType.Equals("Transformation")||reactionType.Equals("Transcription"))
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
        private libsbmlcs.Compartment AddCompartment(string cid, string name, bool constant, double size, int dims, string units = "")
        {
            if (model.getCompartment(CleanIds(cid)) == null)
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
            return model.getCompartment(CleanIds(cid));
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
        private Species AddSpecies(string sid, string name, string compartment, bool boundary, bool constant, bool hasonlySubstanceUnits, double initialConcentration, string units = "")
        {
            if (model.getSpecies(CleanIds(sid)) == null)
            {
                Species species = model.createSpecies();
                species.setId(CleanIds(sid));
                species.setName(name);
                species.setCompartment(CleanIds(compartment));
                species.setBoundaryCondition(boundary);
                species.setConstant(constant);
                species.setHasOnlySubstanceUnits(hasonlySubstanceUnits);
                species.setInitialConcentration(initialConcentration);
                if (!units.Equals(string.Empty))
                {
                    species.setUnits(units);
                }

                if (isSpatialModel)
                {
                    spatialComponents.ModifySpatialSpecies(species);
                }
            }
      
            return model.getSpecies(CleanIds(sid));
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
            if (model.getReaction(CleanIds(rid))==null)
	        {
                currentGlobReaction = null; //Resets the instace to reaction
                currentGlobReaction = model.createReaction();
                currentGlobReaction.setId(CleanIds(rid));
                currentGlobReaction.setName(name);
                currentGlobReaction.setReversible(reversible);
                currentGlobReaction.setFast(fast);
                if (!compartment.Equals(string.Empty))
                {
                    currentGlobReaction.setCompartment(CleanIds(compartment));
                }
                if (isSpatialModel)
                {
                    SpatialReactionPlugin splug = (SpatialReactionPlugin)currentGlobReaction.getPlugin(spatialPackageName);
                    splug.setIsLocal(false);
                }
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
            ModifierSpeciesReference modSpeciesRef = currentGlobReaction.createModifier();
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
            KineticLaw kinetic = currentGlobReaction.createKineticLaw();
            string rateEquation = string.Empty;
            if (reactionType.Equals("BoundaryDissociation") || reactionType.Equals("BoundaryTransportFrom") || reactionType.Equals("BoundaryTransportTo")
                || reactionType.Equals("Annihilation") || reactionType.Equals("Dissociation") || reactionType.Equals("DimerDissociation")
                || reactionType.Equals("CatalyzedCreation") || reactionType.Equals("Transformation") || reactionType.Equals("Transcription"))
            {
                rateEquation = rateConstant + "*" + CleanIds(compartment) + "*" + CleanIds(reactants[0]);
            }
            else if (reactionType.Equals("BoundaryAssociation") || reactionType.Equals("CatalyzedBoundaryActivation") || reactionType.Equals("Association")
                || reactionType.Equals("AutocatalyticTransformation") || reactionType.Equals("CatalyzedAnnihilation") || reactionType.Equals("CatalyzedDimerDissociation")
                || reactionType.Equals("CatalyzedTransformation") || reactionType.Equals("CatalyzedDimerization") || reactionType.Equals("CatalyzedDissociation")
                || reactionType.Equals("Dimerization"))
            {
                rateEquation = rateConstant + "*" + CleanIds(compartment) + "*" + CleanIds(reactants[0]) + "*" + CleanIds(reactants[1]);
            }

            //Transform string kinetic law into MathML representation and add to kinetic law
            ASTNode root = libsbml.parseFormula(rateEquation);
            kinetic.setMath(root);
        }

        /// <summary>
        /// Ensures consistency and adherence to SBML specifications
        /// </summary>
        /// <param name="exportFlag"></param>
        /// <param name="logFileName"></param>
        private void CheckModelConsistency(bool exportFlag, string logFileName)
        {
            //reports on fundamental syntactic and structural errors that violate the XML Schema for SBML
            internal_errors = sbmlDoc.checkInternalConsistency();

            //performs more elaborate model verifications and also validation according to SBML validation rules 
            consistency_errors = sbmlDoc.checkConsistency();

            //prints errors in a file or a message of success
            ReportErrors((internal_errors + consistency_errors), logFileName);


            if ((internal_errors + consistency_errors) == 0)
            {                
                if (exportFlag)
                {
                    File.AppendAllText(logFileName, "The model was correctly encoded into SBML");

                }
                else
                {
                    File.AppendAllText(logFileName, "The model was correctly read into Daphne for simulation");
                }
            }
        }


        /// <summary>
        ///  Writes a log file reporting internal and cosistency errors in the SBML model
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="text"></param>
        /// <param name="outputPath"></param>
        private void ReportErrors(long errors, string outputPath)
        {
            //prints errors to file
            SBMLError error;
            string message = DateTime.UtcNow.ToLocalTime() + Environment.NewLine; //append to beginning

            for (int i = 0; i < errors; i++)
            {
                error = sbmlDoc.getError(i);
                message += error.getSeverityAsString() + " : " + error.getMessage() + Environment.NewLine;
            }
            File.WriteAllText(outputPath, message);
        }

        /// <summary>
        /// Writes out current SBMLDocument to file
        /// </summary>
        /// <returns>flag indicating success</returns>
        private bool WriteSBMLModel()
        {
            SBMLWriter writer = new SBMLWriter();
            writer.writeSBML(sbmlDoc, dirPath + fileName + ".xml");
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
            id = id.Replace(".", "");
            id = id.Replace("/", "");
            id = Regex.Replace(id, @"\s+|%|:", "_");
            id = Regex.Replace(id, @"\|", "_Membrane");
            id = Regex.Replace(id, @"\*", "_Activated");
            id = Regex.Replace(id, @"[<>!@#%()?-]", "");
            
            return id;
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
            inputLogFile = dirPath + fileName + "_inputLog.txt";
            outputLogFile = dirPath + fileName + "_outputLog.txt";
        }

        /// <summary>
        /// Calculates the concentration of species when its distribution is Gaussian
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        private double CalculateGaussianConcentration(BoxSpecification spec, ConfigMolecularPopulation confMolPop)
        {

            return ((Math.Pow(2 * Math.PI, 3 / 2)) * (((MolPopGaussian)confMolPop.mp_distribution).peak_concentration)) / (spec.x_scale * spec.y_scale * spec.z_scale);
        }

        /// <summary>
        /// Adds simulation specific parameters as annotations
        /// </summary>
        private void SetModelAnnotation(Boolean isReactionComplex)
        {
            XMLAttributes attr = new XMLAttributes();
            if (!isReactionComplex)
            {
                //Simulation setup params
                attr.add("description", protocol.experiment_description, annotNamespace, annotprefix);
                attr.add("duration", Convert.ToString(protocol.scenario.time_config.duration), annotNamespace, annotprefix);
                attr.add("rendering_interval", Convert.ToString(protocol.scenario.time_config.rendering_interval), annotNamespace, annotprefix);
                attr.add("sampling_interval", Convert.ToString(protocol.scenario.time_config.sampling_interval), annotNamespace, annotprefix);
                attr.add("integrator_step", Convert.ToString(protocol.scenario.time_config.integrator_step), annotNamespace, annotprefix);
                attr.add("repetitions", Convert.ToString(protocol.experiment_reps), annotNamespace, annotprefix);
                attr.add("grid_size", Convert.ToString(envHandle.gridstep), annotNamespace, annotprefix);

                //Spatial Geometry params
                attr.add("toroidal", Convert.ToString(envHandle.toroidal), annotNamespace, annotprefix);
                if (!isSpatialModel)
                {
                    attr.add("extent_x", Convert.ToString(envHandle.extent_x), annotNamespace, annotprefix);
                    attr.add("extent_y", Convert.ToString(envHandle.extent_y), annotNamespace, annotprefix);
                    attr.add("extent_z", Convert.ToString(envHandle.extent_z), annotNamespace, annotprefix);
                }
                //Cell Death params
                attr.add("death_constant", Convert.ToString(protocol.sim_params.deathConstant), annotNamespace, annotprefix);
                attr.add("death_order", Convert.ToString(protocol.sim_params.deathOrder), annotNamespace, annotprefix);

                //Cell Interaction params
                attr.add("ph1", Convert.ToString(protocol.sim_params.phi1), annotNamespace, annotprefix);
                attr.add("ph2", Convert.ToString(protocol.sim_params.phi2), annotNamespace, annotprefix);
            }
            else
            {
                //ReactionComplex ID
                attr.add("reaction_complex", "true", annotNamespace, annotprefix);
            }

            XMLNamespaces names = new XMLNamespaces();
            names.add(annotNamespace, annotprefix);
            XMLNode Childnode = new XMLNode(new XMLTriple("daphnemodel", annotNamespace, annotprefix), attr, names);
            model.setAnnotation(Childnode);
        }

        /// <summary>
        /// Adds annotations to compartments (cell type they belong to, drag coeff, transd const, locomotor mol)
        /// </summary>
        /// <param name="cellPop"></param>
        private void SetCompartmentAnnotation(CellPopulation cellPop, libsbmlcs.Compartment compartment, ConfigCell configCell = null)
        {
            //double dragCoeff=0, double transductionConst=0, string locomotor=""
            XMLAttributes attr = new XMLAttributes();
            attr.add("cellPop", Convert.ToString(cellPop.cellpopulation_name), annotNamespace, annotprefix);

            //Only add when compartment has 3 dimensions, i.e., when it's the cytosol compartment
            if (compartment.getSpatialDimensions() == 3)
            {
                attr.add("dragCoeff", Convert.ToString(cellPop.Cell.DragCoefficient), annotNamespace, annotprefix);
                attr.add("transdConst", Convert.ToString(cellPop.Cell.TransductionConstant), annotNamespace, annotprefix);
                attr.add("stochForce", Convert.ToString(cellPop.Cell.Sigma), annotNamespace, annotprefix);
                if (!isSpatialModel) { attr.add("cell_radius", Convert.ToString(cellPop.Cell.CellRadius), annotNamespace, annotprefix); }
                attr.add("number", Convert.ToString(cellPop.number), annotNamespace, annotprefix);

                if (!configCell.locomotor_mol_guid_ref.Equals(string.Empty))
                {
                    attr.add("locomotor", CleanIds(protocol.entity_repository.molecules_dict[configCell.locomotor_mol_guid_ref].Name) + "_" + compartment.getId(), annotNamespace, annotprefix);
                }
            }
            XMLNamespaces names = new XMLNamespaces();
            names.add(annotNamespace, annotprefix);
            XMLNode Childnode = new XMLNode(new XMLTriple("daphnecomps", annotNamespace, annotprefix), attr, names);
            compartment.setAnnotation(Childnode);
        }

        /// <summary>
        /// Appends to SBML species elements annotations on diffusion coeff, molecular weigths and molPop distributions
        /// </summary>
        /// <param name="diffCoeff"></param>
        /// <param name="molWeigth"></param>
        private void SetSpeciesAnnotation(ConfigMolecularPopulation confMolPop, Species species)
        {
            //Species specific params
            XMLAttributes attr = new XMLAttributes();
            if (!isSpatialModel) { attr.add("diff_coeff", Convert.ToString(confMolPop.molecule.DiffusionCoefficient), annotNamespace, annotprefix); }
            attr.add("mol_weight", Convert.ToString(confMolPop.molecule.MolecularWeight), annotNamespace, annotprefix);
            attr.add("mol_radius", Convert.ToString(confMolPop.molecule.EffectiveRadius), annotNamespace, annotprefix);
            attr.add("complex", Convert.ToString(confMolPop.molecule.Name.Contains(":") ? true : false), annotNamespace, annotprefix); //If name of species has a :, it is stored as a complex

            //Distribution of species
            if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
            {
                attr.add("distribution", "Gaussian", annotNamespace, annotprefix);
                attr.add("peak_conc", Convert.ToString(((MolPopGaussian)confMolPop.mp_distribution).peak_concentration), annotNamespace, annotprefix);

                BoxSpecification box = ((MolPopGaussian)confMolPop.mp_distribution).gauss_spec.box_spec;

                attr.add("x_trans", Convert.ToString(box.x_trans), annotNamespace, annotprefix);
                attr.add("x_halfscale", Convert.ToString(box.half_x_scale), annotNamespace, annotprefix);
                attr.add("y_trans", Convert.ToString(box.y_trans), annotNamespace, annotprefix);
                attr.add("y_halfscale", Convert.ToString(box.half_y_scale), annotNamespace, annotprefix);
                attr.add("z_trans", Convert.ToString(box.z_trans), annotNamespace, annotprefix);
                attr.add("z_halfscale", Convert.ToString(box.half_z_scale), annotNamespace, annotprefix);
            }
            else if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
            {
                attr.add("distribution", "Linear", annotNamespace, annotprefix);

                List<Daphne.BoundaryCondition> boundary = ((MolPopLinear)confMolPop.mp_distribution).boundaryCondition;
                attr.add("boundary_type", Convert.ToString((int)boundary[0].boundaryType), annotNamespace, annotprefix);
                attr.add("boundary_start", Convert.ToString((int)boundary[0].boundary), annotNamespace, annotprefix);
                attr.add("boundary_start_conc", Convert.ToString(boundary[0].concVal), annotNamespace, annotprefix);
                attr.add("boundary_end", Convert.ToString((int)boundary[1].boundary), annotNamespace, annotprefix);
                attr.add("boundary_end_conc", Convert.ToString(boundary[1].concVal), annotNamespace, annotprefix);
            }

            XMLNamespaces names = new XMLNamespaces();
            names.add(annotNamespace, annotprefix);
            XMLNode Childnode = new XMLNode(new XMLTriple("daphnespecies", annotNamespace, annotprefix), attr, names);
            species.setAnnotation(Childnode);
        }

        /// <summary>
        /// Appends to SBML species elements annotations
        /// </summary>
        private void SetGeneAnnotation(ConfigGene confGenePop, Species species)
        {
            //Species specific params
            XMLAttributes attr = new XMLAttributes();
            attr.add("copy_num", Convert.ToString(confGenePop.CopyNumber), annotNamespace, annotprefix);
           
            XMLNamespaces names = new XMLNamespaces();
            names.add(annotNamespace, annotprefix);
            XMLNode Childnode = new XMLNode(new XMLTriple("daphnespecies", annotNamespace, annotprefix), attr, names);
            species.setAnnotation(Childnode);
        }
        /// <summary>
        /// Appends Daphne specific reaction annotation to SBML reactions
        /// </summary>
        /// <param name="type"></param>
        private void SetReactionAnnotation(ReactionType type)
        {
            XMLAttributes attr = new XMLAttributes();
            attr.add("react_type", Convert.ToString((int)type), annotNamespace, annotprefix);

        }

        /// <summary>
        /// Initializes SBMLDoc and adds Namespaces of Spatial and Req SBML packages
        /// </summary>
        private void SetSBMLNameSpaces()
        {
            if (isSpatialModel)
            {
                SBMLNamespaces sbmlns = new SBMLNamespaces(SBMLLEVEL, SBMLVERSION, spatialPackageName, SPATIALPKGVERSION);
                sbmlDoc = new SBMLDocument(sbmlns);

                /*Declare our model uses other packages by referencing their XMLnamespaceURI
                 * Spatial Package
                 * It allows us to encode modeling spatial information such as spatially localized reactions, non-homogenously
                 * distributed species, and cellular geometries.*/
                sbmlDoc.enablePackage(SpatialExtension.getXmlnsL3V1V1(), spatialPackageName, true);
                sbmlDoc.setPackageRequired(spatialPackageName, true);
            }
            else
            {
                sbmlDoc = new SBMLDocument(SBMLLEVEL, SBMLVERSION);
            }
        }

        /// <summary>
        /// Exports the current simulation model in the protocol in SBML format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ConvertDaphneToSBML(int isSpatial)
        {
            //Sets flag to know which type of SBML we are building:1 for Core, 2 for Spatial
            if (isSpatial == 1)
            {
                isSpatialModel = false;
            }
            else
            {
                isSpatialModel = true;
            }

            //Initializes SBMLDoc and adda spatial package namespaces to model if necessary
            SetSBMLNameSpaces();

            model = sbmlDoc.createModel();

            //Configure spatial model constraints
            if (isSpatialModel)
            {
                spatialComponents = new SpatialComponents(protocol, ref model);
            }
          
            SetPaths(Uri.UnescapeDataString(new Uri(System.IO.Path.GetDirectoryName(appPath) + @"\").LocalPath), System.IO.Path.GetFileNameWithoutExtension(appPath));

            //Populates model ids in SBML template
            SetModelIds(protocol.experiment_name, "Daphne");

            //adds simulation specific parameters as annotations
            SetModelAnnotation(false);

            //Defines model-wide units for volume, substance, time and extents(used by compartments/entities)
            AddModelUnits();

            //adds ECS compartment
            double ecsVol = (envHandle.extent_x) * (envHandle.extent_y) * (envHandle.extent_z);
            libsbmlcs.Compartment backgComp = AddCompartment("ECS", "ECS", true, ecsVol, envHandle.NumGridPts.Length, "");
            Domain backgDomain=null;
            if (isSpatialModel)
            {
                backgDomain= spatialComponents.AddBackground(backgComp);
            }
            //adds species in the ECS
            BoxSpecification spec;
            string confMolPopName = string.Empty;
            Species species = null;
            foreach (ConfigMolecularPopulation confMolPop in protocol.scenario.environment.comp.molpops)
            {
                //We need to create SBML species with the type of the molecular population and not with the name as the latter is user defined.
                //confMolPopName = protocol.entity_repository.molecules_dict[confMolPop.molecule_guid_ref].Name;
                confMolPopName = confMolPop.Name;
                if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    spec = ((MolPopGaussian)confMolPop.mp_distribution).gauss_spec.box_spec;
                    species = AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, CalculateGaussianConcentration(spec, confMolPop), "");
                }
                else if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    species = AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((MolPopHomogeneousLevel)confMolPop.mp_distribution).concentration, "");
                }
                else if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
                {
                    species = AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((MolPopExplicit)confMolPop.mp_distribution).conc.Average(), "");
                }
                else if (confMolPop.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    if ((((MolPopLinear)confMolPop.mp_distribution).boundaryCondition != null) && (((MolPopLinear)confMolPop.mp_distribution).boundaryCondition.Count == 2))
                    {
                        species = AddSpecies(confMolPopName + "_ECS", confMolPopName, "ECS", false, false, false, ((((MolPopLinear)confMolPop.mp_distribution).boundaryCondition[0].concVal) + (((MolPopLinear)confMolPop.mp_distribution).boundaryCondition[1].concVal)) / 2, "");
                    }
                    else
                    {
                        //MessageBox.Show("Model could not be saved into SBML as linear gradient values could not be found", "Linear Gradient Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                }
                SetSpeciesAnnotation(confMolPop, species);

                if (isSpatialModel)
                {
                    spatialComponents.AddDiffusionCoefficient(species, protocol.entity_repository.molecules_dict[confMolPop.molecule.entity_guid].DiffusionCoefficient);
                    //All boundary conditions are no flux if they don't specify a linear distribution- all others are not supported by SBML spatial
                    spatialComponents.SetBoxBoundaryCondition(species);
                } 
            }

            //adds reactions in the ECS
            foreach (ConfigReaction cr in protocol.scenario.environment.comp.Reactions)
            {
                AddSBMLReactions(cr, cr.rate_const, protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, "ECS", string.Empty);

                //Add reaction annotation
                SetReactionAnnotation(protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type);
            }

            //Add a membrane compartment and a cytosol compartment for each cell type
            string cellMembraneId, cytosolId = string.Empty;
            TinySphere sphere;
            TinyBall ball;
            ConfigCompartment[] configComp;
            ConfigCell configCell = new ConfigCell();
            libsbmlcs.Compartment compartment;
            int ordinal = 1;
            Domain membraneDomain=null, cytoDomain=null;
            DomainType membraneDomainType=null, cytoDomainType=null;
            foreach (CellPopulation cellPop in scenario.cellpopulations)
            {
                //Name added to compartment includes cell type
                //Membrane
                configCell = protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid];
                cellMembraneId = string.Concat("Membrane_", configCell.CellName);
                double radius = cellPop.Cell.CellRadius;
                sphere = new TinySphere();
                sphere.Initialize(new double[] { radius });
                compartment = AddCompartment(cellMembraneId, cellMembraneId, true, sphere.Area(), sphere.Dim, "");
                SetCompartmentAnnotation(cellPop, compartment);
                string [] membraneNames=new string[cellPop.number];
                //Adds Domains and DomainTypes of every cell membrane
                if (isSpatialModel)
                {
                    membraneDomainType = spatialComponents.AddCellModel(compartment, true);
                    for (int z = 0; z < cellPop.number; z++)
                    {
                        CellState positions = cellPop.CellStates[z];
                        membraneDomain = spatialComponents.AddCellDomain(membraneDomainType, compartment, positions.X+radius, positions.Y, positions.Z, z);
                        spatialComponents.AddAdjacency(backgDomain.getId(), membraneDomain.getId());
                        membraneNames[z] = membraneDomain.getId();
                    }
                }

                //Only add a cytosol compartment if there are any molecular populations in the cytosol 
                //if (configComp[0].molpops.Count > 0)
                //{
                cytosolId = string.Concat("Cytosol_", configCell.CellName);
                ball = new TinyBall();
                ball.Initialize(new double[] { radius });
                compartment = AddCompartment(cytosolId, cytosolId, true, ball.Volume(), ball.Dim, "");
                SetCompartmentAnnotation(cellPop, compartment, configCell);

                //Adds DomainTypes and Domains for every cytosol
                if (isSpatialModel)
                {
                    //Only one csgObject for each cell type
                    cytoDomainType = spatialComponents.AddCellModel(compartment, false);
                    spatialComponents.AddCellGeometry(cytoDomainType, ordinal, radius);
                    for (int z = 0; z < cellPop.number; z++)
                    {
                        CellState positions = cellPop.CellStates[z];
                        cytoDomain = spatialComponents.AddCellDomain(cytoDomainType, compartment, positions.X, positions.Y, positions.Z, z);
                        spatialComponents.AddAdjacency(membraneNames[z], cytoDomain.getId());
                    }
                    ordinal = ordinal + 1;
                }

                //Setup necessary to extract molecular populations from each compartment (cytosol/membrane)
                configComp = new ConfigCompartment[2];
                configComp[0] = configCell.cytosol;
                configComp[1] = configCell.membrane;

                //ConfigGene configGene;
                //}

                for (int comp = 0; comp < 2; comp++)
                {
                    //0 cytosol, 1 membrane (both assume uniform distribution of molecular populations
                    foreach (ConfigMolecularPopulation cmp in configComp[comp].molpops)
                    {
                        //We need to create SBML species with the type of the molecular population and not with the name as the latter is user defined.
                        confMolPopName = protocol.entity_repository.molecules_dict[cmp.molecule.entity_guid].Name;
                        if (comp == 0)
                        {
                            //Add cytosol molecular species
                            species = AddSpecies(confMolPopName + "_" + cytosolId, confMolPopName, cytosolId, false, false, false, ((MolPopHomogeneousLevel)cmp.mp_distribution).concentration, "");
                            SetSpeciesAnnotation(cmp, species);
                            
                            //SetRequiredElements(species);

                            // protocol.entity_repository.cells[0].locomotor_mol_guid_ref
                        }
                        else
                        {
                            //Add membrane molecular species
                            species = AddSpecies(confMolPopName.TrimEnd('|') + "_" + cellMembraneId, confMolPopName, cellMembraneId, false, false, false, ((MolPopHomogeneousLevel)cmp.mp_distribution).concentration, "");
                            SetSpeciesAnnotation(cmp, species);

                        }

                        if (isSpatialModel)
	                    {
                            spatialComponents.AddDiffusionCoefficient(species, protocol.entity_repository.molecules_dict[cmp.molecule.entity_guid].DiffusionCoefficient);
                            //All boundary conditions are no flux as this is the only one that SBML spatial allows us to indicate 
                            spatialComponents.SetDomainTypeBoundaryCondition(species,comp==0?cytoDomainType:membraneDomainType); 
	                    }
                    }
                }

                //add genes
                ////for (int i = 0; i < configCell.genes_guid_ref.Count; i++)
                ////{
                ////    configGene= protocol.entity_repository.genes_dict[configCell.genes_guid_ref[i]];
                ////    confMolPopName = configGene.Name;
                ////    species = AddSpecies(confMolPopName + "_" + cytosolId, confMolPopName, cytosolId, false, false, false,configGene.ActivationLevel, "");
                ////    SetGeneAnnotation(configGene, species);

                ////    //No diffusion coefficients or Boundary Conditions are needed here
                ////}
                foreach (ConfigGene configGene in configCell.genes)
                {
                    confMolPopName = configGene.Name;
                    species = AddSpecies(confMolPopName + "_" + cytosolId, confMolPopName, cytosolId, false, false, false, configGene.ActivationLevel, "");
                    SetGeneAnnotation(configGene, species);

                    //No diffusion coefficients or Boundary Conditions are needed here
                }

                //Add reactions belonging to current cellPopulation and specify which compartment they take place in
                foreach (ConfigReaction cr in configCell.cytosol.Reactions)
                {
                    cytosolId = string.Concat("Cytosol_", configCell.CellName);
                    AddSBMLReactions(cr, cr.rate_const, protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, cytosolId, cellPop.Cell.entity_guid);
                }

            }

            //Check for model consistency and serialize to file (provide output stream for log)
            CheckModelConsistency(true, outputLogFile);
            WriteSBMLModel();
        }


        /// <summary>
        /// Exports the current reaction complex in SBML format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ConvertReactionComplexToSBML(ConfigReactionComplex crc)
        {
            //Initializes SBMLDoc and adda spatial package namespaces to model if necessary
            SetSBMLNameSpaces();

            model = sbmlDoc.createModel();

            string experimentName = CleanIds(crc.Name);
            SetPaths(Uri.UnescapeDataString(new Uri(System.IO.Path.GetDirectoryName(appPath) + @"\").LocalPath), System.IO.Path.GetFileNameWithoutExtension(appPath));

            //Populates model ids in SBML template
            SetModelIds(experimentName, "Daphne");

            //Add model annotations
            SetModelAnnotation(true);

            //Defines model-wide units for substance, time and extents
            AddModelUnits(true);

            //Adds cytosol compartment
            string compName = "RComplex";
            AddCompartment(compName, compName, true, 1.0, 3);

            //Adds molecular populations
            foreach (ConfigMolecularPopulation confMolPop in crc.molpops)
            {
                AddSpecies(confMolPop.Name + "_" + compName, confMolPop.Name, compName, false, false, false, ((MolPopHomogeneousLevel)confMolPop.mp_distribution).concentration, "");
            }

            // Adds reactions
            foreach (ConfigReaction cr in crc.reactions)
            {
                AddSBMLReactions(cr, cr.rate_const, protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type, compName, string.Empty);
            }

            // Check for model consistency and serialize to file (provide output stream for log)
            CheckModelConsistency(true, outputLogFile);
            WriteSBMLModel();
        }

        /// <summary>
        /// Tests whether the given molecule is present in the given compartment for a given cellPopulation
        /// </summary>
        /// <param name="cellPop_guid_ref"></param>
        /// <param name="molecule"></param>
        /// <param name="compartment"></param>
        /// <returns></returns>
        private bool ExistMolecule(string cellId, string molecule, string compartment, bool isGene=false)
        {
            ObservableCollection<ConfigMolecularPopulation> configComp=null;
            //ObservableCollection<string> configGenes=null;
            ObservableCollection<ConfigGene> cGenes = null;

            if (!compartment.ToLower().Contains("complex") && isGene)
            {
                //configGenes = protocol.entity_repository.cells_dict[cellId].genes_guid_ref;
                cGenes = protocol.entity_repository.cells_dict[cellId].genes;
            }
            else if (compartment.ToLower().Contains("membrane"))
            {
                configComp = protocol.entity_repository.cells_dict[cellId].membrane.molpops;
            }
            else if (compartment.ToLower().Contains("cytosol"))
            {
                configComp = protocol.entity_repository.cells_dict[cellId].cytosol.molpops;
            }
            else if (compartment.Equals("ECS"))
            {
                configComp = protocol.scenario.environment.comp.molpops;
            }
            else if (compartment.ToLower().Contains("complex"))//Reaction Complex
            {
                foreach (ConfigMolecule mol in protocol.entity_repository.molecules)
                {
                    if (mol.Name.Equals(molecule))
                    {
                        return true;
                    }
                }
                foreach (ConfigGene gen in protocol.entity_repository.genes)
                {
                    if (gen.Name.Equals(molecule))
                    {
                        return true;
                    }
                }
            }

            if (configComp!=null)
            {
                foreach (ConfigMolecularPopulation cmp in configComp)
                {
                    if (protocol.entity_repository.molecules_dict[cmp.molecule.entity_guid].Name.Equals(molecule))
                    {
                        return true;
                    }
                }  
            }
            ////else if (configGenes != null)
            ////{
            ////    foreach (string cng in configGenes)
            ////    {
            ////        if (protocol.entity_repository.genes_dict[cng].Name.Equals(molecule))
            ////        {
            ////            return true;
            ////        }
            ////    }
            ////}
            else if (cGenes != null)
            {
                foreach (ConfigGene cg in cGenes)
                {
                    if (cg.Name.Equals(molecule))
                    {
                        return true;
                    }
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
            if(protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            string receptor, ligand, complex, reactant, product, membrane, bulk, bulkActivated, modifier, rid, gene;
            TissueScenario scenario = (TissueScenario)protocol.scenario;

            if (type == ReactionType.BoundaryAssociation)
            {
                receptor = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                ligand = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                complex = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in scenario.cellpopulations)
                    {
                        AddBoundaryAssociation(cellPop.Cell.entity_guid, receptor, ligand, complex, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryAssociation(cellId, receptor, ligand, complex, compartment, rateConstant);
                }
            }
            else if (type == ReactionType.BoundaryDissociation)
            {
                receptor = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;
                ligand = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                complex = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in scenario.cellpopulations)
                    {
                        AddBoundaryDissociation(cellPop.Cell.entity_guid, receptor, ligand, complex, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryDissociation(cellId, receptor, ligand, complex, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.BoundaryTransportFrom)
            {
                membrane = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in scenario.cellpopulations)
                    {
                        AddBoundaryTransportFromReaction(cellPop.Cell.entity_guid, bulk, membrane, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryTransportFromReaction(cellId, bulk, membrane, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.BoundaryTransportTo)
            {
                bulk = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                membrane = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in scenario.cellpopulations)
                    {
                        AddBoundaryTransportToReaction(cellPop.Cell.entity_guid, bulk, membrane, compartment, rateConstant);
                    }
                }
                else
                {
                    AddBoundaryTransportToReaction(cellId, bulk, membrane, compartment, rateConstant);
                }

            }
            else if (type == ReactionType.CatalyzedBoundaryActivation)
            {
                bulk = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulkActivated = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;

                if (compartment.Equals("ECS"))
                {
                    foreach (CellPopulation cellPop in scenario.cellpopulations)
                    {
                        AddCatalyzedBoundaryActivation(cellPop.Cell.entity_guid, bulk, bulkActivated, modifier, compartment, rateConstant);
                    }
                }
                else
                {
                    AddCatalyzedBoundaryActivation(cellId, bulk, bulkActivated, modifier, compartment, rateConstant);
                }
            }
            else if (type == ReactionType.Annihilation)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Annihilation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Annhil", "k_Annhil", rateConstant, true, AddReactionSpecificUnits("Annihilation"));
                    AddKineticLaw("Annihilation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Annhil");

                }

            }
            else if (type == ReactionType.Association)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "Association" + "_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(bulk + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Assoc", "k_Assoc", rateConstant, true, AddReactionSpecificUnits("Association"));
                    AddKineticLaw("Association", new string[] { (reactant + "_" + compartment), (bulk + "_" + compartment) }, compartment, "k_Assoc");
                }
            }
            else if (type == ReactionType.Dissociation)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                bulk = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "Dissociation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);
                    AddReactProd(bulk + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Dissoc", "k_Dissoc", rateConstant, true, AddReactionSpecificUnits("Dissociation"));
                    AddKineticLaw("Dissociation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Dissoc");
                }
            }
            else if (type == ReactionType.DimerDissociation)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "DimerDissociation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_DimerDissoc", "k_DimerDissoc", rateConstant, true, AddReactionSpecificUnits("DimerDissociation"));
                    AddKineticLaw("DimerDissociation", new string[] { (reactant + "_" + compartment) }, compartment, "k_DimerDissoc");
                }
            }
            else if (type == ReactionType.AutocatalyticTransformation)
            {
                bulk = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment))
                {
                    rid = "AutocatalyticTransformation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(bulk + "_" + compartment, true, true, 1);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(bulk + "_" + compartment, true, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_AutoTrans", "k_AutoTrans", rateConstant, true, AddReactionSpecificUnits("AutocatalyticTransformation"));
                    AddKineticLaw("AutocatalyticTransformation", new string[] { (reactant + "_" + compartment), (bulk + "_" + compartment) }, compartment, "k_AutoTrans");
                }
            }
            else if (type == ReactionType.CatalyzedAnnihilation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, modifier, compartment))
                {
                    rid = "CatalyzedAnnihilation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatAnnih", "k_CatAnnih", rateConstant, true, AddReactionSpecificUnits("CatalyzedAnnihilation"));
                    AddKineticLaw("CatalyzedAnnihilation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatAnnih");
                }
            }
            else if (type == ReactionType.CatalyzedAssociation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                bulk = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[1]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, bulk, compartment) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "CatalyzedAssociation" + "_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(bulk + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatAssoc", "k_CatAssoc", rateConstant, true, AddReactionSpecificUnits("CatalyzedAssociation"));
                    AddKineticLaw("CatalyzedAssociation", new string[] { (bulk + "_" + compartment), (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatAssoc");
                }
            }
            else if (type == ReactionType.CatalyzedCreation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment))
                {
                    rid = "CatalyzedCreation" + "_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatCreat", "k_CatCreat", rateConstant, true, AddReactionSpecificUnits("CatalyzedCreation"));
                    AddKineticLaw("CatalyzedCreation", new string[] { (modifier + "_" + compartment) }, compartment, "k_CatCreat");
                }
            }
            else if (type == ReactionType.CatalyzedDimerDissociation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedDimerDissociation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 2);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_DimDissoc", "k_DimDissoc", rateConstant, true, AddReactionSpecificUnits("CatalyzedDimerDissociation"));
                    AddKineticLaw("CatalyzedDimerDissociation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_DimDissoc");
                }
            }
            else if (type == ReactionType.CatalyzedTransformation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedTransformation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatTrans", "k_CatTrans", rateConstant, true, AddReactionSpecificUnits("CatalyzedTransformation"));
                    AddKineticLaw("CatalyzedTransformation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatTrans");
                }
            }
            else if (type == ReactionType.CatalyzedDimerization)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "CatalyzedDimerization" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 2);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatDimer", "k_CatDimer", rateConstant, true, AddReactionSpecificUnits("CatalyzedDimerization"));
                    AddKineticLaw("CatalyzedDimerization", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatDimer");
                }
            }
            else if (type == ReactionType.CatalyzedDissociation)
            {
                modifier = protocol.entity_repository.molecules_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
                bulk = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[1]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, modifier, compartment) & ExistMolecule(cellId, reactant, compartment) & ExistMolecule(cellId, bulk, compartment))
                {
                    rid = "CatalyzedDissociation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(modifier + "_" + compartment);
                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);
                    AddReactProd(bulk + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_CatDissoc", "k_CatDissoc", rateConstant, true, AddReactionSpecificUnits("CatalyzedDissociation"));
                    AddKineticLaw("CatalyzedDissociation", new string[] { (reactant + "_" + compartment), (modifier + "_" + compartment) }, compartment, "k_CatDissoc");
                }
            }
            else if (type == ReactionType.Dimerization)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Dimerization" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 2);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Dimer", "k_Dimer", rateConstant, true, AddReactionSpecificUnits("Dimerization"));
                    AddKineticLaw("Dimerization", new string[] { (reactant + "_" + compartment), (reactant + "_" + compartment) }, compartment, "k_Dimer");
                }
            }
            else if (type == ReactionType.Transformation)
            {
                reactant = protocol.entity_repository.molecules_dict[cr.reactants_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;

                if (ExistMolecule(cellId, product, compartment) & ExistMolecule(cellId, reactant, compartment))
                {
                    rid = "Transformation" + "_" + reactant + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddReactProd(reactant + "_" + compartment, true, true, 1);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Trans", "k_Trans", rateConstant, true, AddReactionSpecificUnits("Transformation"));
                    AddKineticLaw("Transformation", new string[] { (reactant + "_" + compartment) }, compartment, "k_Trans");
                }
            }
            else if (type==ReactionType.Transcription)
            {
                gene = protocol.entity_repository.genes_dict[cr.modifiers_molecule_guid_ref[0]].Name;
                product = protocol.entity_repository.molecules_dict[cr.products_molecule_guid_ref[0]].Name;
               
                if (ExistMolecule(cellId, gene, compartment,true) & ExistMolecule(cellId, product, compartment))
                {
                    rid = "Transcription" + "_" + product + "_" + compartment;
                    AddReaction(rid, rid, false, compartment, false);

                    AddModifier(gene + "_" + compartment);
                    AddReactProd(product + "_" + compartment, true, false, 1);

                    //create reaction rate coefficient units and add reaction rate as model parameter
                    AddParameter("k_Transcrip", "k_Transcrip", rateConstant, true, AddReactionSpecificUnits("Transcription"));
                    AddKineticLaw("Transcription", new string[] { (gene + "_" + compartment) }, compartment, "k_Transcrip");
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
            string cellName = protocol.entity_repository.cells_dict[cellIdentifier].CellName;
            ////need to check whether reactant is on membrane (associate with proper reactant on each cell) 
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, membrane, "membrane"))
            {
                membrane = membrane.Contains("|") ? membrane : membrane + "_" + "Membrane";
                string rid = "BoundaryTransportFrom" + "_" + membrane + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(membrane + "_" + cellName, true, true, 1);
                AddReactProd(bulk + "_" + compartment, true, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BTransFrom", "k_BTransFrom", rateConstant, true, AddReactionSpecificUnits("BoundaryTransportFrom"));
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
            string cellName = protocol.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether product is on the membrane
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, membrane, "membrane"))
            {
                membrane = membrane.Contains("|") ? membrane : membrane + "_" + "Membrane";
                string rid = "BoundaryTransportTo" + "_" + bulk + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(bulk + "_" + compartment, true, true, 1);
                AddReactProd(membrane + "_" + cellName, true, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BTransTo", "k_BTransTo", rateConstant, true, AddReactionSpecificUnits("BoundaryTransportTo"));
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
            string cellName = protocol.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether modifier(receptor) is on the membrane
            if (ExistMolecule(cellIdentifier, bulk, compartment) & ExistMolecule(cellIdentifier, bulkActivated, compartment) & ExistMolecule(cellIdentifier, modifier, "membrane"))
            {
                modifier = modifier.Contains("|") ? modifier : modifier + "_" + "Membrane";

                string rid = "CatalyzedBoundaryActivation" + "_" + modifier + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);

                AddReactProd(bulk + "_" + compartment, true, true, 1);
                AddReactProd(bulkActivated + "_" + compartment, true, false, 1);
                AddModifier(modifier + "_" + cellName);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_CatBActiv", "k_CatBActiv", rateConstant, true, AddReactionSpecificUnits("CatalyzedBoundaryActivation"));
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
            string cellName = protocol.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether reactant 1 and product 1 are on the membrane (associate with proper reactant on each cell) 
            if (ExistMolecule(cellIdentifier, ligand, compartment) & ExistMolecule(cellIdentifier, receptor, "membrane") & ExistMolecule(cellIdentifier, complex, "membrane"))
            {
                complex = complex.Contains("|") ? complex : complex + "_" + "Membrane";
                receptor = complex.Contains("|") ? receptor : receptor + "_" + "Membrane";

                string rid = "BoundaryDissociation" + "_" + complex + "_" + cellName;
                AddReaction(rid, rid, false, compartment, false);
                AddReactProd(complex + "_" + cellName, true, true, 1);
                AddReactProd(receptor + "_" + cellName, true, false, 1);
                AddReactProd(ligand + "_" + compartment, true, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BDissoc", "k_BDissoc", rateConstant, true, AddReactionSpecificUnits("BoundaryDissociation"));
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
            string cellName = protocol.entity_repository.cells_dict[cellIdentifier].CellName;

            ////need to check whether reactant 1 and product 1 are on the membrane (associate with proper reactant on each cell) and whether ligand is on compartment
            if (ExistMolecule(cellIdentifier, ligand, compartment) & ExistMolecule(cellIdentifier, receptor, "membrane") & ExistMolecule(cellIdentifier, complex, "membrane"))
            {
                complex = complex.Contains("|") ? complex : complex + "_" + "Membrane";
                receptor = complex.Contains("|") ? receptor : receptor + "_" + "Membrane";

                string rid = "BoundaryAssociation" + "_" + complex + "_" + cellName;
                //add reaction components 
                AddReaction(rid, rid, false, compartment, false);
                AddReactProd(receptor + "_" + cellName, true, true, 1);
                AddReactProd(ligand + "_" + compartment, true, true, 1);
                AddReactProd(complex + "_" + cellName, true, false, 1);

                //create reaction rate coefficient units and add reaction rate as model parameter
                AddParameter("k_BAssoc", "k_BAssoc", rateConstant, true, AddReactionSpecificUnits("BoundaryAssociation"));
                AddKineticLaw("BoundaryAssociation", new string[] { (ligand + "_" + compartment), (receptor + "_" + cellName) }, ("Membrane" + "_" + cellName), "k_BAssoc");
            }
        }

        /// <summary>
        /// Imports an SBML model for simulation from a given file
        /// </summary>
        /// <param name="filename"> </param>
        public Protocol ReadSBMLFile()
        {
            //Stores directory path and file name into appropriate variables
            SetPaths(Uri.UnescapeDataString(new Uri(System.IO.Path.GetDirectoryName(appPath)+ @"\").LocalPath), System.IO.Path.GetFileNameWithoutExtension(appPath));
            //Reads and performs basic consistency checks upon reading the content
            sbmlDoc = libsbml.readSBML(dirPath + fileName + ".xml");

            // Checking consistency errors is done differently when reading from a file
            CheckModelConsistency(false, inputLogFile);

            //Attempt to convert upwards imported SBMLdocument to supported Level and Version of SBML
            if (sbmlDoc.getNumErrors() > 0 && (sbmlDoc.getNumErrors() != sbmlDoc.getErrorLog().getNumFailsWithSeverity(1)))
            {
                MessageBox.Show("File could not be read into Daphne. Please select another one", "Error reading SBML file");
                return null;
            }
            if (!ConvertToL3V1(ref sbmlDoc))
            {
                MessageBox.Show("Conversion to SBML L3V1 failed. Either libSBML does not (yet)" + Environment.NewLine
                                   + "have the ability to convert this model or (automatic)" + Environment.NewLine
                                   + "conversion is not possible in this case." + Environment.NewLine, "Error converting SBML file to L3V1");
                return null;
            }

            //Obtain SBML model
            model = sbmlDoc.getModel();

            //Determines whether input model uses the Spatial package
            isSpatialModel=model.isPackageEnabled(spatialPackageName);
            if (isSpatialModel)
            {
                spatialModelPlugin = (SpatialModelPlugin)model.getPlugin(spatialPackageName);
                geometry = spatialModelPlugin.getGeometry();                
                //Assumes there only active geometry definition is CsG
                 GeometryDefinition b= geometry.getListOfGeometryDefinitions().get(0);
                csgGeometry=(CSGeometry)spatialModelPlugin.getElementBySId(b.getId());
                //TODO HOW DO WE EXACTRACT A CSG Geometry
                string a = csgGeometry.getId();
            }
            //Extracts all model components from the read SBML model and populates the Sim Configuration object
            PopulateProtocol();
            return protocol;
        }

        /// <summary>
        /// Parses model annotation and stores it in SimConfi object
        /// </summary>
        private void GetModelAnnotation()
        {
            if (protocol.scenario.environment is ConfigECSEnvironment == false)
            {
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            //If there is a Daphne annotation
            if (model.isSetAnnotation())
            {
                XMLNode annotation = model.getAnnotation();
                annotation = annotation.getChild("daphnemodel");
                //Model description
                XMLAttributes attributes = annotation.getAttributes();
                protocol.experiment_description = attributes.getValue(attributes.getIndex("description"));
                protocol.scenario.time_config.duration = Convert.ToDouble(attributes.getValue(attributes.getIndex("duration")));
                protocol.scenario.time_config.rendering_interval = Convert.ToDouble(attributes.getValue(attributes.getIndex("rendering_interval")));
                protocol.scenario.time_config.sampling_interval = Convert.ToDouble(attributes.getValue(attributes.getIndex("sampling_interval")));
                protocol.scenario.time_config.integrator_step = Convert.ToDouble(attributes.getValue(attributes.getIndex("integrator_step")));
                protocol.experiment_reps = Convert.ToInt32(attributes.getValue(attributes.getIndex("repetitions")));

                envHandle.gridstep = Convert.ToDouble(attributes.getValue(attributes.getIndex("grid_size")));
                envHandle.toroidal = Convert.ToBoolean(attributes.getValue(attributes.getIndex("toroidal")));

                if (!isSpatialModel)
                {
                    envHandle.extent_x = Convert.ToInt32(attributes.getValue(attributes.getIndex("extent_x")));
                    envHandle.extent_y = Convert.ToInt32(attributes.getValue(attributes.getIndex("extent_y")));
                    envHandle.extent_z = Convert.ToInt32(attributes.getValue(attributes.getIndex("extent_z")));
                }
                protocol.sim_params.deathConstant = Convert.ToDouble(attributes.getValue(attributes.getIndex("death_constant")));
                protocol.sim_params.deathOrder = Convert.ToInt32(attributes.getValue(attributes.getIndex("death_order")));
                protocol.sim_params.phi1 = Convert.ToDouble(attributes.getValue(attributes.getIndex("ph1")));
                protocol.sim_params.phi2 = Convert.ToDouble(attributes.getValue(attributes.getIndex("ph2")));
            }
            else
            {
                //Defaults in case a model doesn't have additional annotations (i.e., a model that isn't ours)
                protocol.experiment_description = "Simulation Model";
                protocol.scenario.time_config.duration = 100;
                protocol.scenario.time_config.rendering_interval = 1;
                protocol.scenario.time_config.sampling_interval = 1;
                envHandle.gridstep = 50;
                //Replace with spatial package constructs at some point
                envHandle.extent_x = 200;
                envHandle.extent_y = 200;
                envHandle.extent_z = 200;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cytosol"></param>
        /// <returns></returns>
        private double[] GetCellAttributes(libsbmlcs.Compartment cytosol)
        {
            XMLNode annotation;
            XMLAttributes attributes;
            annotation = cytosol.getAnnotation();
            annotation = annotation.getChild("daphnecomps");
            attributes = annotation.getAttributes();

            return new double[] { Convert.ToDouble(attributes.getValue(attributes.getIndex("dragCoeff"))), Convert.ToDouble(attributes.getValue(attributes.getIndex("transdConst"))), Convert.ToDouble(attributes.getValue(attributes.getIndex("number"))), Convert.ToDouble(attributes.getValue(attributes.getIndex("stochForce"))), Convert.ToDouble(attributes.getValue(attributes.getIndex("cell_radius"))) };
        }

        /// <summary>
        /// Retrieves cell membrane and cytoplasm compartments for each cell type 
        /// </summary>
        /// <param name="compartments"></param>
        /// <returns></returns>
        private Dictionary<string, List<libsbmlcs.Compartment>> GetCellTypes(ListOfCompartments compartments)
        {
            XMLNode annotation;
            XMLAttributes attributes;
            libsbmlcs.Compartment temporaryComp;
            string cellType = string.Empty;
            Dictionary<string, List<libsbmlcs.Compartment>> outputDictionary = new Dictionary<string, List<libsbmlcs.Compartment>>();
            List<libsbmlcs.Compartment> tempList;
            for (int i = 0; i < compartments.size(); i++)
            {
                temporaryComp = compartments.get(i);
                annotation = temporaryComp.getAnnotation();
                annotation = annotation.getChild("daphnecomps");
                attributes = annotation.getAttributes();
                cellType = attributes.getValue(attributes.getIndex("cellPop"));

                if (outputDictionary.ContainsKey(cellType))
                {
                    outputDictionary.TryGetValue(cellType, out tempList);
                    tempList.Add(temporaryComp);
                }
                else
                {
                    outputDictionary.Add(attributes.getValue(attributes.getIndex("cellPop")), new List<libsbmlcs.Compartment>() { temporaryComp });
                }

            }
            return outputDictionary;
        }

        /// <summary>
        /// Tests whether the provided species molecule drives molecular movement 
        /// </summary>
        /// <param name="locomotor"></param>
        /// <returns></returns>
        private Boolean isLocomotor(Species locomotor, libsbmlcs.Compartment cytosol)
        {
            XMLNode annotation = cytosol.getAnnotation();
            annotation = annotation.getChild("daphnecomps");
            XMLAttributes attributes = annotation.getAttributes();
            if (locomotor.getId().Equals(attributes.getValue(attributes.getIndex("locomotor"))))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the incoming SBML file corresponds to a reactionComplex a model
        /// </summary>
        /// <returns></returns>
        private Boolean IsReactionComplex()
        {
            //If there is a Daphne annotation
            if (model.isSetAnnotation())
            {
                XMLNode annotation = model.getAnnotation();
                annotation = annotation.getChild("daphnemodel");
                //Model description
                XMLAttributes attributes = annotation.getAttributes();
                if (attributes.getIndex("reaction_complex") == -1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests whether a given species is a gene or a molecular population by examining the annotation
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        private Boolean TestSpecies(Species spec) {
         //If there is a Daphne annotation
            if (spec.isSetAnnotation())
            {
                XMLNode annotation = spec.getAnnotation();
                annotation = annotation.getChild("daphnespecies");
                //Model description
                XMLAttributes attributes = annotation.getAttributes();
                if (attributes.getIndex("copy_num") != -1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Reconstructs and returns a ConfigGene from an SBML species
        /// </summary>
        /// <param name="specGene"></param>
        /// <returns></returns>
        private ConfigGene PrepareGenes(Species specGene) 
        {
            int copy_num = GetCopyNum(specGene);
            ConfigGene cg= new ConfigGene(specGene.getId(), copy_num, specGene.getInitialConcentration());
            protocol.entity_repository.genes.Add(cg);
            protocol.entity_repository.genes_dict.Add(cg.entity_guid, cg);
            return cg;
        }

        /// <summary>
        /// Returns copy number variation for a given gene
        /// </summary>
        /// <param name="specGene"></param>
        /// <returns></returns>
        private int GetCopyNum(Species specGene)
        {
            XMLNode annotation = specGene.getAnnotation();
            annotation = annotation.getChild("daphnespecies");
            //Model description
            XMLAttributes attributes = annotation.getAttributes();
            return Convert.ToInt32(attributes.getValue("copy_num"));
        }

        public Boolean ContainsReactionComplex() {
            return reactionComplexFlag;
        }

        /// <summary>
        /// Extracts the size of the simulation container from Spatial components
        /// </summary>
        private void SetBoxExtents() {
            ListOfCSGObjects csgObjects = csgGeometry.getListOfCsgObjects();
            CSGScale csgScale;
            for (int i = 0; i < csgObjects.size(); i++)
			{
                if (csgObjects.get(i).getOrdinal()==0)
	            {
                    csgScale = (CSGScale)csgObjects.get(i).getCsgNode();
                    envHandle.extent_x= (Int32)csgScale.getScaleX();
                    envHandle.extent_y = (Int32)csgScale.getScaleY();
                    envHandle.extent_z = (Int32)csgScale.getScaleZ();
	            }     
			}

        }

        /// <summary>
        /// Populates Protocol object with SBML model 
        /// </summary>
        /// <param name="sc"></param>
        private void PopulateProtocol()
        {

            //Set up predefined molecules, reactionTemplates, reactions, cells and reaction complexes
            ProtocolCreators.LoadDefaultGlobalParameters(protocol);

            //Extract compartments from SBML model
            ListOfCompartments compartments = model.getListOfCompartments();
            libsbmlcs.Compartment tempComp = null;

            //Extract species from SBML model
            ListOfSpecies speciesList = model.getListOfSpecies();
            libsbmlcs.Species tempSpecies = null;

            //If # of compartments==1, then build as a reaction complex. If not, build as a spatial simulation scenario.
            if (IsReactionComplex())
            {
                ConfigReactionComplex crc = new ConfigReactionComplex(model.getId());
                //Add all molecular populations
                Species crcSpec;

                for (int i = 0; i < speciesList.size(); i++)
                {
                    crcSpec = speciesList.get(i);
                 
                    if(TestSpecies(crcSpec))
                    {
                        // protocol.rc_scenario.environment.ecs.molpops.Add(PreparePopulation(speciesList.get(i), MoleculeLocation.Bulk, true));
                        crc.molpops.Add(PreparePopulation(crcSpec, MoleculeLocation.Bulk, true));
                    }
                }

                libsbmlcs.Reaction sbmlReaction;
                ConfigReaction complexConfigReaction;

                for (int i = 0; i < model.getListOfReactions().size(); i++)
                {
                    sbmlReaction = model.getReaction(i);

                    //Add reactions to reactionComplex
                    if (sbmlReaction.isSetKineticLaw())
                    {
                        complexConfigReaction = new ConfigReaction();

                        buildConfigReaction(sbmlReaction, ref complexConfigReaction);
                        complexConfigReaction.GetTotalReactionString(protocol.entity_repository);
                        protocol.entity_repository.reactions.Add(complexConfigReaction);
                        protocol.entity_repository.reactions_dict.Add(complexConfigReaction.entity_guid, complexConfigReaction);

                        if (complexConfigReaction != null)
                        {
                            crc.reactions.Add(complexConfigReaction);
                        }
                    }
                }
                // add the reaction complex to repository collection
                protocol.entity_repository.reaction_complexes.Add(crc);
                reactionComplexFlag = true;
            }
            else
            {
                if(protocol.scenario.environment is ConfigECSEnvironment == false)
                {
                    throw new InvalidCastException();
                }

                ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

                //Populate experiment
                protocol.experiment_name = model.getId();
                protocol.reporter_file_name = protocol.experiment_name;

                //Parse additional simulation elem these params as a custom annotation
                GetModelAnnotation();

                if (isSpatialModel) { SetBoxExtents(); }

                /****Populate Protocol with ECS molPops****/
                double CompSize = 0;
                string largestCompID = string.Empty;
                //Populate Protocol

                //WHAT IF SIZE DOESN'T EXIST
                //Obtain the ID of the 3D compartment with largest size (ECS)
                for (int i = 0; i < compartments.size(); i++)
                {
                    tempComp = compartments.get(i);
                    if (tempComp.getSize() > CompSize && tempComp.getSpatialDimensions() == 3)
                    {
                        CompSize = tempComp.getSize();
                        largestCompID = tempComp.getId();
                    }
                }

                //Add all ECS molecular populations
                for (int i = 0; i < speciesList.size(); i++)
                {
                    tempSpecies = speciesList.get(i);
                    if (tempSpecies.getCompartment().Equals(largestCompID))
                    {
                        protocol.scenario.environment.comp.molpops.Add(PreparePopulation(tempSpecies, MoleculeLocation.Bulk, true));
                    }
                }

                /****Populate Protocol with CellPops****/

                //Excludes ECS compartment from collection          
                compartments.remove(largestCompID);

                //Obtain annotations on compartments to find how many cell types there are
                Dictionary<string, List<libsbmlcs.Compartment>> cellTypes = GetCellTypes(compartments);

                //Add Cell
                ConfigCell gc = new ConfigCell();
                libsbmlcs.Compartment membrane = null;
                libsbmlcs.Compartment cytosol = null;
                double[] compartAttributes = null;

                //for reactions
                libsbmlcs.Reaction ecsReaction = null, cellReaction = null;
                ConfigReaction cr = null;

                foreach (KeyValuePair<string, List<libsbmlcs.Compartment>> cellTypeComp in cellTypes)
                {
                    gc = new ConfigCell();
                    gc.CellName = cellTypeComp.Key; //May end up in conflicts because it tries to create a cell that already exists by default.

                    foreach (libsbmlcs.Compartment comp in cellTypeComp.Value)
                    {
                        if (comp.getSpatialDimensions() == 2)
                        {
                            membrane = comp;

                            //Pull all membrane species
                            for (int i = 0; i < speciesList.size(); i++)
                            {
                                tempSpecies = speciesList.get(i);
                                if (tempSpecies.getCompartment().Equals(membrane.getId()))
                                {
                                    gc.membrane.molpops.Add(PreparePopulation(tempSpecies, MoleculeLocation.Boundary, false));
                                }
                            }

                        }
                        else if (comp.getSpatialDimensions() == 3)
                        {
                            cytosol = comp;
                            compartAttributes = GetCellAttributes(cytosol);
                            gc.DragCoefficient.ConstValue = compartAttributes[0];
                            gc.TransductionConstant.ConstValue = compartAttributes[1];
                            gc.Sigma.ConstValue = compartAttributes[3];
                            //Pull all cytosol species
                            for (int i = 0; i < speciesList.size(); i++)
                            {
                                tempSpecies = speciesList.get(i);
                                if (tempSpecies.getCompartment().Equals(cytosol.getId()))
                                {
                                    if (TestSpecies(tempSpecies))
                                    {
                                        gc.cytosol.molpops.Add(PreparePopulation(tempSpecies, MoleculeLocation.Bulk, false));
                                        if (isLocomotor(tempSpecies, cytosol))
                                        {
                                            gc.locomotor_mol_guid_ref = ProtocolCreators.findMoleculeGuid(tempSpecies.getId(), MoleculeLocation.Bulk, protocol);
                                        }
                                    }
                                    else {
                                        //gc.genes_guid_ref.Add(PrepareGenes(tempSpecies).entity_guid);
                                        gc.genes.Add(PrepareGenes(tempSpecies));
                                    }
                                }
                            }
                        }
                    }

                    ////Take into account the case when one of these is missing!
                    double membraneRadius = CalculateCellRadius(membrane.getSize(), true);
                    double cytoRadius = CalculateCellRadius(cytosol.getSize(), false);

                    if (membraneRadius == cytoRadius)
                    {
                        gc.CellRadius = cytoRadius; //standard in our simulation 5.0    
                    }
                    else
                    {
                        gc.CellRadius = Math.Max(membraneRadius, cytoRadius);
                    }

                    //Add cell to entity repository
                    protocol.entity_repository.cells.Add(gc);

                    /****Add cytosol-specific reactions - between membrane and cytosol and cytosol-cytosol or cytosol-nucleus****/
                    for (int i = 0; i < model.getListOfReactions().size(); i++)
                    {
                        cellReaction = model.getReaction(i);

                        //Intra-cellular reactions (this will need to change as we dynamically figure out which reactions belong in what compartment)
                        if (cellReaction.isSetCompartment() && cellReaction.getCompartment().Equals(cytosol.getId()) && cellReaction.isSetKineticLaw())
                        {
                            cr = new ConfigReaction();

                            buildConfigReaction(cellReaction, ref cr);
                            cr.GetTotalReactionString(protocol.entity_repository);
                            //Add the reaction to repository collection
                            protocol.entity_repository.reactions.Add(cr);
                            gc.cytosol.Reactions.Add(cr.Clone(true));
                        }
                    }

          
                    //Add cell population
                    CellPopulation cellPop = new CellPopulation();
                    cellPop.Cell = gc.Clone(true);
                    cellPop.Cell.entity_guid = gc.entity_guid;
                    cellPop.cellpopulation_name = gc.CellName;
                    cellPop.number = (int)compartAttributes[2];

                    double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
                    double minDisSquared = 2 * gc.CellRadius;
                    minDisSquared *= minDisSquared;
                    cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
                    // Don't start the cell on a lattice point, until gradient interpolation method improves.
                    cellPop.cellPopDist.Initialize();
                    cellPop.CellStates[0] = new CellState(envHandle.extent_x - 2 * gc.CellRadius - envHandle.gridstep / 2,
                                                          envHandle.extent_y / 2 - envHandle.gridstep / 2,
                                                          envHandle.extent_z / 2 - envHandle.gridstep / 2);

                    //cellPop.cellpopulation_color = Syste/m.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);

                    if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
                    {
                        throw new InvalidCastException();
                    }

                    //Add cell population to scenario
                    ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);
                    cellPop.report_xvf.position = true;
                    cellPop.report_xvf.velocity = true;
                    cellPop.report_xvf.force = true;

                    /****Reporting parameters****/
                    foreach (ConfigMolecularPopulation cmp in gc.membrane.molpops)
                    {
                        // Mean only
                        cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
                    }
                    foreach (ConfigMolecularPopulation cmp in gc.cytosol.molpops)
                    {
                        // Mean only
                        cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
                    }
                    foreach (ConfigMolecularPopulation mpECM in protocol.scenario.environment.comp.molpops)
                    {
                        ReportECM reportECM = new ReportECM();
                        reportECM.molpop_guid_ref = mpECM.molpop_guid;
                        reportECM.mp_extended = ExtendedReport.COMPLETE;
                        cellPop.ecm_probe.Add(reportECM);
                        cellPop.ecm_probe_dict.Add(mpECM.molpop_guid, reportECM);

                    }
                }

                /****Add external reactions - in ECS ****/

                for (int i = 0; i < model.getListOfReactions().size(); i++)
                {
                    ecsReaction = model.getReaction(i);

                    //ECS reactions
                    if (ecsReaction.isSetCompartment() && ecsReaction.getCompartment().Equals(largestCompID) && ecsReaction.isSetKineticLaw())
                    {
                        cr = new ConfigReaction();

                        buildConfigReaction(ecsReaction, ref cr);
                        cr.GetTotalReactionString(protocol.entity_repository);
                        //Add the reaction to repository collection
                        protocol.entity_repository.reactions.Add(cr);
                        protocol.scenario.environment.comp.Reactions.Add(cr.Clone(true));
                    }
                }
            }
        }

        /// <summary>
        /// Populates a ConfigReaction instance from an SBML reaction element
        /// </summary>
        /// <param name="ecsReaction"></param>
        /// <param name="cr"></param>
        private void buildConfigReaction(libsbmlcs.Reaction ecsReaction, ref ConfigReaction cr)
        {
            libsbmlcs.SpeciesReference spRef;
            libsbmlcs.ModifierSpeciesReference modRef;
            ConfigReactionTemplate crt;

            Dictionary<string, int> inputReactants = new Dictionary<string, int>();
            Dictionary<string, int> inputProducts = new Dictionary<string, int>();
            Dictionary<string, int> inputModifiers = new Dictionary<string, int>();

            //extract rate constant by checking local and global params
            cr.rate_const = getRateConstant(ecsReaction);

            for (int j = 0; j < ecsReaction.getNumReactants(); j++)
            {
                spRef = ecsReaction.getReactant(j);
                inputReactants.Add(spRef.getSpecies(), (int)spRef.getStoichiometry());
            }

            for (int j = 0; j < ecsReaction.getNumProducts(); j++)
            {
                spRef = ecsReaction.getProduct(j);
                inputProducts.Add(spRef.getSpecies(), (int)spRef.getStoichiometry());
            }

            for (int j = 0; j < ecsReaction.getNumModifiers(); j++)
            {
                modRef = ecsReaction.getModifier(j);
                //no definition of stoichiometry for modifier species, hence 0
                inputModifiers.Add(modRef.getSpecies(), 1);
            }

            cr.reaction_template_guid_ref = protocol.IdentifyReactionType(inputReactants, inputProducts, inputModifiers);
            if (cr.reaction_template_guid_ref == null)
            {
                string msg = string.Format("Unsupported reaction");
                MessageBox.Show(msg);
                return;
            }
            crt = protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref];

            // Don't have to add stoichiometry information since the reaction template knows it based on reaction type
            // For each list of reactants, products, and modifiers, add bulk then boundary molecules.

            // Bulk Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.reactants_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.reactants_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.reactants_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.reactants_molecule_guid_ref.Add(guid);
                }
            }

            // Bulk Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.products_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.products_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.products_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.products_molecule_guid_ref.Add(guid);
                }
            }

            // Bulk modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string guid;
                if (crt.reac_type==ReactionType.Transcription)
                {
                    guid = protocol.findGeneGuidByName(kvp.Key);
                    if (!cr.modifiers_molecule_guid_ref.Contains(guid))
                    {
                         cr.modifiers_molecule_guid_ref.Add(guid);
                    }
                }
                else
                {
                    guid = protocol.findMoleculeGuidByName(kvp.Key);
                    if (!cr.modifiers_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                    {
                        cr.modifiers_molecule_guid_ref.Add(guid);
                    }
                }
            }
            // Boundary modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string guid;
                if (crt.reac_type == ReactionType.Transcription)
                {
                    guid = protocol.findGeneGuidByName(kvp.Key);
                    if (!cr.modifiers_molecule_guid_ref.Contains(guid))
                    {
                        cr.modifiers_molecule_guid_ref.Add(guid);
                    }
                }
                else
                {
                    guid = protocol.findMoleculeGuidByName(kvp.Key);
                    if (!cr.modifiers_molecule_guid_ref.Contains(guid) && protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                    {
                        cr.modifiers_molecule_guid_ref.Add(guid);
                    }
                }
            }
        }

      
        /// <summary>
        /// Obtains reaction rate constant from Kinetic Law element
        /// </summary>
        /// <param name="ecsReaction"></param>
        /// <returns></returns>
        private double getRateConstant(libsbmlcs.Reaction ecsReaction)
        {
            KineticLaw kinLaw = ecsReaction.getKineticLaw();
            string reactionRateEq = libsbml.formulaToString(kinLaw.getMath());
            List<string> tokensRateEq = reactionRateEq.Split(new char[] { '*' }).ToList();
            double value = 0;

            //Remove trailing and leading empty spaces in each token
            for (int i = 0; i < tokensRateEq.Count; i++)
            {
                tokensRateEq[i] = tokensRateEq[i].Trim();
            }

            Parameter param;
            LocalParameter localParam;
            //To get rate constant first check local (scope)
            for (int i = 0; i < kinLaw.getNumLocalParameters(); i++)
            {
                localParam = kinLaw.getLocalParameter(i);
                if (tokensRateEq.Contains(localParam.getId()))
                {
                    value = localParam.getValue();
                }
            }

            //then examine global params    
            //To get rate constant first check local (scope)
            for (int i = 0; i < model.getNumParameters(); i++)
            {
                param = model.getParameter(i);
                if (tokensRateEq.Contains(param.getId()))
                {
                    value = param.getValue();
                }
            }

            return value;
        }


        /// <summary>
        /// Calculates the radius of a cell from the area or volume provided
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isMembrane"></param>
        /// <returns></returns>
        private double CalculateCellRadius(double size, bool isMembrane)
        {
            if (isMembrane)
            {
                return Math.Sqrt(size / (4 * Math.PI));
            }
            else
            {
                //Cytosol
                return Math.Pow((3 * size) / (4 * Math.PI), (1.0 / 3.0));
            }
        }


        /// <summary>
        /// Obtain molecular weight from custom annotation for given species - Defaults from Wang2011 et al.
        /// </summary>
        /// <param name="species"></param>
        private double[] GetSpeciesAnnotation(Species species, MoleculeLocation location, Boolean inECS)
        {
            // Diffusion coefficient in um^{2} / min
            double diffusion = 0;
            //Molecular weight in kDa
            double weight = 0;
            double radius = 0;
            bool complex = false;
            if (species.isSetAnnotation())
            {
                XMLNode annotation = species.getAnnotation();
                annotation = annotation.getChild("daphnespecies");
                //Species description
                XMLAttributes attributes = annotation.getAttributes();
                diffusion = Convert.ToDouble(attributes.getValue(attributes.getIndex("diff_coeff")));
                weight = Convert.ToDouble(attributes.getValue(attributes.getIndex("mol_weight")));
                complex = Convert.ToBoolean(attributes.getValue(attributes.getIndex("complex")));
                radius = Convert.ToDouble(attributes.getValue(attributes.getIndex("mol_radius")));
            }
            else
            {
                //In ECS
                if (location == MoleculeLocation.Bulk && inECS)
                {
                    weight = 7.96;
                    diffusion = 4.5e3;
                }//In Boundary
                else if (location == MoleculeLocation.Boundary && !inECS)
                {
                    if (complex)
                    {
                        weight = 43 + 7.96;
                    }
                    else
                    {
                        weight = 43;
                    }
                    diffusion = 1e-7;
                }//In Cytoplasm (FINISH THIS)
                else if (location == MoleculeLocation.Bulk && !inECS)
                {
                    if (complex)
                    {//Complexes are only for known complexes, hence the 43
                        weight = 43 + 7.96;
                    }
                    else
                    {
                        //Mols like A and A*
                        weight = 1;
                    }
                    diffusion = 750;
                }
            }
            return new double[] { weight, diffusion, radius };
        }

        /// <summary>
        /// Sets the distribution of a ConfigMolecularPopulation
        /// </summary>
        /// <param name="species"></param>
        /// <param name="configMolPop"></param>
        /// <returns></returns>
        private void SetSpeciesDistribution(Species species, ref ConfigMolecularPopulation configMolPop)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)protocol.scenario;

            Boolean isDistributionSet = false;

            if (species.isSetAnnotation())
            {
                XMLNode annotation = species.getAnnotation();
                annotation = annotation.getChild("daphnespecies");
                //Species description
                XMLAttributes attributes = annotation.getAttributes();
                string distributionType = Convert.ToString(attributes.getValue(attributes.getIndex("distribution")));
                if (distributionType.Equals("Gaussian"))
                {
                    double peakConc = Convert.ToDouble(attributes.getValue(attributes.getIndex("peak_conc")));

                    // Gaussian Distrtibution
                    // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
                    // box x,y,z_scale parameters are 2*sigma
                    GaussianSpecification gaussSpec = new GaussianSpecification();
                    BoxSpecification box = new BoxSpecification();

                    box.x_trans = Convert.ToDouble(attributes.getValue(attributes.getIndex("x_trans")));
                    box.y_trans = Convert.ToDouble(attributes.getValue(attributes.getIndex("y_trans")));
                    box.z_trans = Convert.ToDouble(attributes.getValue(attributes.getIndex("z_trans")));
                    box.half_x_scale = Convert.ToDouble(attributes.getValue(attributes.getIndex("x_halfscale")));
                    box.half_y_scale = Convert.ToDouble(attributes.getValue(attributes.getIndex("y_halfscale")));
                    box.half_z_scale= Convert.ToDouble(attributes.getValue(attributes.getIndex("z_halfscale")));

                    gaussSpec.box_spec = box;
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

                    MolPopGaussian molPopGaussian = new MolPopGaussian();
                    molPopGaussian.peak_concentration = peakConc;

                    //double check this is the correct GUIid for this box
                    molPopGaussian.gauss_spec = gaussSpec;
                    configMolPop.mp_distribution = molPopGaussian;

                    isDistributionSet = true;
                }
                else if (distributionType.Equals("Linear"))
                {
                    //Fetches the boundary type, axes and boundary concentrations
                    MolBoundaryType boundaryType = (MolBoundaryType)Convert.ToInt32(attributes.getValue(attributes.getIndex("boundary_type")));
                    Daphne.Boundary boundaryStart = (Daphne.Boundary)Convert.ToInt32(attributes.getValue(attributes.getIndex("boundary_start")));
                    double boundarySConc = Convert.ToDouble(attributes.getValue(attributes.getIndex("boundary_start_conc")));
                    Daphne.Boundary boundaryEnd = (Daphne.Boundary)Convert.ToInt32(attributes.getValue(attributes.getIndex("boundary_end")));
                    double boundaryEndConc = Convert.ToDouble(attributes.getValue(attributes.getIndex("boundary_end_conc")));

                    MolPopLinear molpoplin = new MolPopLinear();
                    //configMolPop.mp_dist_name = "Linear";

                    if (boundaryStart == Daphne.Boundary.left)
                    {
                        molpoplin.boundary_face = BoundaryFace.X;
                    }
                    else if (boundaryStart == Daphne.Boundary.bottom)
                    {
                        molpoplin.boundary_face = BoundaryFace.Y;
                    }
                    else
                    { //back
                        molpoplin.boundary_face = BoundaryFace.Z;
                    }

                    molpoplin.dim = 0;
                    molpoplin.x1 = 0;
                    molpoplin.boundaryCondition = new List<Daphne.BoundaryCondition>();
                    Daphne.BoundaryCondition bc = new Daphne.BoundaryCondition(boundaryType, boundaryStart, boundarySConc);
                    molpoplin.boundaryCondition.Add(bc);
                    bc = new Daphne.BoundaryCondition(boundaryType, boundaryEnd, boundaryEndConc);
                    molpoplin.boundaryCondition.Add(bc);
                    configMolPop.mp_distribution = molpoplin;

                    isDistributionSet = true;
                }
            }
            //If distirbution not set after reading annotation
            if (!isDistributionSet)
            {
                MolPopHomogeneousLevel uniformDistribution;
                uniformDistribution = new MolPopHomogeneousLevel();
                uniformDistribution.concentration = species.getInitialConcentration();
                configMolPop.mp_distribution = uniformDistribution;
            }
        }

        /// <summary>
        /// Creates a new ConfigMolecule and returns the associated CofigMolecularPopulation
        /// </summary>
        /// <param name="tempSpecies"></param>
        /// <param name="sc"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private ConfigMolecularPopulation PreparePopulation(Species tempSpecies, MoleculeLocation location, Boolean inECS)
        {

            //Create new molecule, add such molecule to the entity repository and continue.
            //Obtain diffusion coefficients (below included) for ECS molecules from spatial diffusion params
            double[] speciesAnnot = GetSpeciesAnnotation(tempSpecies, location, inECS);
            ConfigMolecule configMolecule = new ConfigMolecule(tempSpecies.getId(), speciesAnnot[0], speciesAnnot[2], speciesAnnot[1]);
            if (location == MoleculeLocation.Bulk)
            {
                configMolecule.molecule_location = MoleculeLocation.Bulk;
            }
            else
            {
                configMolecule.molecule_location = MoleculeLocation.Boundary;
            }

            protocol.entity_repository.molecules.Add(configMolecule);
            protocol.entity_repository.molecules_dict.Add(configMolecule.entity_guid, configMolecule);

            ConfigMolecularPopulation configMolPop;

            if (inECS)
            {
                configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
            }
            else
            {
                configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);
            }

            configMolPop.molecule = configMolecule.Clone(null);
            configMolPop.Name = configMolecule.Name;

            //Retrieve distribution of ECS molecules from custom annotation
            SetSpeciesDistribution(tempSpecies, ref configMolPop);

            return configMolPop;
        }

        /// <summary>
        /// Converts upwards imported SBMLdocument to supported Level and Version of SBML (L3V1)
        /// </summary>
        /// <param name="document"></param>
        private bool ConvertToL3V1(ref SBMLDocument document)
        {
            int dlLvel = (int)sbmlDoc.getLevel();
            int dVersion = (int)sbmlDoc.getVersion();
            bool success = true;
            long errors = 0;
            //Convert input file upwards to L3V1
            if (dlLvel < SBMLLEVEL || dVersion < SBMLVERSION)
            {
                success = sbmlDoc.setLevelAndVersion(SBMLLEVEL, SBMLVERSION);
                errors = document.getNumErrors();
                if (!success || errors > 0)
                {
                    ReportErrors(errors, inputLogFile);
                    File.AppendAllText(inputLogFile, "Conversion to SBML L3V1 failed");
                    success = false;
                }
                if (success && errors == 0)
                {
                    File.AppendAllText(inputLogFile, "Input file was succesfully converted to SBML L3V1");
                }
            }
            return success;
        }
    }
}

