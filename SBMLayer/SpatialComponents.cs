using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using libsbmlcs;

//C# alias to avoid having to fully qualify UNIT_KINDS
using unitType = libsbmlcs.libsbml;

namespace SBMLayer
{
    class SpatialComponents
    {
        private double xmin;
        private double ymin;
        private double zmin;
        private double xmax;
        private double ymax;
        private double zmax;

        private const string xCoord = "x";
        private const string yCoord = "y";
        private const string zCoord = "z";
        private const string packageName = "spatial";
        private const string boundaryCondType = "Neumann";
        private const string diffCoefficientType = "isotropic";
        private const string diffUnits = "mmetreSqred_per_minute";
        private const string fluxBoundaryUnits = "item_per_mmetreSqred_per_minute";        
        private const int ordinalBackground = 0;

        private Protocol protocol;
        private Model model;

        //Unit definition declaration
        private UnitDefinition udef;

        private Geometry geometry;

        //Equivalent geometries may be supported (analytic/SF)
        private CSGeometry csGeometry;

        private DomainType backgroundDomainType;

        public SpatialComponents(Protocol protocol, ref libsbmlcs.Model model)
        {
            this.protocol = protocol;
            this.model = model;
            
            AddSpatialUnits();
            InitializeCoordinateSystem();
        }


        /// <summary>
        /// Adds an adjacency relation for 2 given domains
        /// </summary>
        /// <param name="domainOne"></param>
        /// <param name="domainTwo"></param>
        public void AddAdjacency(string domainOne, string domainTwo) {
          AdjacentDomains adjDomains=geometry.createAdjacentDomains();
          adjDomains.setId(domainOne+"_"+domainTwo);
          adjDomains.setDomain1(domainOne);
          adjDomains.setDomain2(domainTwo);
        }

        /// <summary>
        /// Adds DomainType-Domain-Geometry for background compartment
        /// </summary>
        /// <param name="comp"></param>
        public Domain AddBackground(libsbmlcs.Compartment comp)
        {
            //Define a DomainType for the background container
            backgroundDomainType = geometry.createDomainType();
            backgroundDomainType.setSpatialDimensions(3);
            backgroundDomainType.setId("background_domainType");
            
            //Add a CompartmentMapping element to link DomainType with ECS compartment
            SpatialCompartmentPlugin cplug = (SpatialCompartmentPlugin)comp.getPlugin(packageName);
            CompartmentMapping map = cplug.createCompartmentMapping();
            map.setId("mapping_" + backgroundDomainType.getId() + "_" + comp.getId());          
            map.setDomainType(backgroundDomainType.getId());
            map.setUnitSize(1.0);

            //Define a Domain for the given volumetric space
            Domain backgroundDomain = geometry.createDomain();
            backgroundDomain.setDomainType(backgroundDomainType.getId());
            backgroundDomain.setId("background_domain");
    
            //Define the shape of the Domain
            AddBackgroundGeometry();
            return backgroundDomain;
        }

        /// <summary>
        /// Instantiates a CSGObject to define the background as a cube
        /// </summary>
        public void AddBackgroundGeometry()
        {
		    CSGObject backgroundObject = csGeometry.createCsgObject();
            backgroundObject.setOrdinal(ordinalBackground);
		    
            CSGScale scale = backgroundObject.createCsgScale();
            scale.setId("scale_background");
		    scale.setScaleX(xmax-xmin);
		    scale.setScaleY(ymax-ymin);
		    scale.setScaleZ(zmax-zmin);

            //Geometrical definition of simulation space as a cube
            CSGPrimitive cube = scale.createCsgPrimitive();
            cube.setId("solid_background");
		    cube.setPrimitiveType("cube");
		    backgroundObject.setDomainType(backgroundDomainType.getId());
            backgroundObject.setId("CSGObject_background");         
        }

        /// <summary>
        /// Instantiates a DomainType for a given cell population
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public DomainType AddCellModel(libsbmlcs.Compartment comp, Boolean isMembrane) 
        {
            //Instantiate a DomainType for a given cell type
            DomainType domainType = geometry.createDomainType();
            domainType.setId("DomainType_" + comp.getId());

            if (isMembrane)
            { 
                domainType.setSpatialDimensions(2);
            }
            else
            {
                domainType.setSpatialDimensions(3);
            }
     
            //Add a CompartmentMapping element to link DomainType with Cell compartment
            SpatialCompartmentPlugin cplug = (SpatialCompartmentPlugin)comp.getPlugin(packageName);
            CompartmentMapping map = cplug.createCompartmentMapping();
            map.setId("mapping_" + domainType.getId());         
            map.setDomainType(domainType.getId());
            map.setUnitSize(1.0);

            return domainType;
        }

        public void ModifySpatialSpecies(libsbmlcs.Species species) {
            SpatialSpeciesPlugin splug = (SpatialSpeciesPlugin)species.getPlugin(packageName);
            splug.setIsSpatial(true);
       }
        /// <summary>
        /// Returns the units to be used for params defining a boundary conditions
        /// </summary>
        /// <returns></returns>
        private string getBoundaryCondUnits()
        {
            return model.getUnitDefinition(fluxBoundaryUnits).getId();
        }

        /// <summary>
        /// Adds Boundary condition for DomainType that encloses a given species
        /// </summary>
        /// <param name="species"></param>
        /// <param name="domainType"></param>
        public void SetDomainTypeBoundaryCondition(Species species, DomainType domainType)
        {
            //Add Boundary condition for DomainType that encloses a given species
            Parameter p = model.createParameter();
            p.setId(species.getId() + "_BC_"+ domainType.getId());
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            libsbmlcs.BoundaryCondition symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setBoundaryDomainType(domainType.getId());
            symbol.setType(boundaryCondType);
        }

        /// <summary>
        /// Adds boundary condition for the ECS 
        /// </summary>
        /// <param name="species"></param>
        public void SetBoxBoundaryCondition(Species species) 
        {
 
            //Add Boundary conditions for each coordinate element in the system (our simulations will always be 3D)
            Parameter p = model.createParameter();
            p.setId(species.getId() + "_BC_Xmin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            libsbmlcs.BoundaryCondition symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Xmin");
            symbol.setType(boundaryCondType);

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Xmax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Xmax");
            symbol.setType(boundaryCondType);

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Ymin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Ymin");
            symbol.setType(boundaryCondType);

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Ymax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Ymax");
            symbol.setType(boundaryCondType);

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Zmin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Zmin");
            symbol.setType(boundaryCondType);

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Zmax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            p.setUnits(getBoundaryCondUnits());

            pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            symbol = pplug.createBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Zmax");
            symbol.setType(boundaryCondType);
        }

        /// <summary>
        /// Returns the units to be used for params defining a diffusion coefficient
        /// </summary>
        /// <returns></returns>
        private string getDiffCoefficientUnits() {
            return model.getUnitDefinition(diffUnits).getId();
        }

        /// <summary>
        /// Adds diffusion coefficients for molecular populations
        /// </summary>
        /// <param name="species"></param>
        /// <param name="coeff"></param>
        public void AddDiffusionCoefficient(Species species, double coeff) 
        {
            Parameter p = model.createParameter();
            p.setId(species.getId() + "_diff");
            p.setValue(coeff);
            p.setConstant(true);
            p.setUnits(getDiffCoefficientUnits());

            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            DiffusionCoefficient symbol = pplug.createDiffusionCoefficient();
            symbol.setVariable(species.getId());
            symbol.setType(diffCoefficientType);
        }


        /// <summary>
        /// Adds domains to the given Cell DomainType
        /// </summary>
        /// <param name="domainType"></param>
        /// <param name="comp"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="zCoord"></param>
        public Domain AddCellDomain(DomainType domainType, libsbmlcs.Compartment comp, double xCoord, double yCoord, double zCoord, int cellNumber)
        {
            Domain domain = geometry.createDomain();
            domain.setDomainType(domainType.getId());
            domain.setId("cell_" +cellNumber+"_"+ comp.getId());
            InteriorPoint interiorPoint = domain.createInteriorPoint();
            interiorPoint.setCoord1(xCoord);
            interiorPoint.setCoord2(yCoord);
            interiorPoint.setCoord3(zCoord);
            return domain;
         }

        /// <summary>
        /// Creates a volumetric container for a cell
        /// </summary>
        /// <param name="domainType"></param>
        public void AddCellGeometry(DomainType domainType, int ordinal,  double radius)
        {
            CSGObject csgObject = csGeometry.createCsgObject();
            csgObject.setOrdinal(ordinal);            
            CSGScale scale = csgObject.createCsgScale();
            scale.setId("scale_" + domainType.getId());
            scale.setScaleX(radius);
            scale.setScaleY(radius);
            scale.setScaleZ(radius);

            //Geometrical definition of simulation space as a cube
            CSGPrimitive sphere = scale.createCsgPrimitive();
            sphere.setId("solid_" + domainType.getId());
            sphere.setPrimitiveType("sphere");
            csgObject.setDomainType(domainType.getId());
            csgObject.setId("CSGObject_" + domainType.getId()); 
        }

        /// <summary>
        /// Initializes coordinate system and geometryDefinition to be used
        /// </summary>
        private void InitializeCoordinateSystem()
        {
            if (protocol.scenario.environment is ConfigECSEnvironment == false)
            {
                // for now
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            //Define dimensions of the simulation space
            xmin = 0;
            ymin = 0;
            zmin = 0;
            xmax = envHandle.extent_x;
            ymax = envHandle.extent_y;
            zmax = envHandle.extent_z;

            //Defining a coordinate system
            SpatialModelPlugin plugin = (SpatialModelPlugin)model.getPlugin(packageName);
            geometry = plugin.createGeometry();
            geometry.setCoordinateSystem("cartesian");
            geometry.setId("Geometry");            
            string coordUnit = model.getLengthUnits();

            //Instantiating Coord components of the system
            CoordinateComponent xComp = geometry.createCoordinateComponent();
            xComp.setId(xCoord);
            xComp.setType("cartesianX");
            xComp.setUnit(coordUnit);
            libsbmlcs.Boundary minX = xComp.createBoundaryMin();
            minX.setId("Xmin");
            minX.setValue(xmin);
            libsbmlcs.Boundary maxX = xComp.createBoundaryMax();
            maxX.setId("Xmax");
            maxX.setValue(xmax);

            CoordinateComponent yComp = geometry.createCoordinateComponent();
            yComp.setId(yCoord);
            yComp.setType("cartesianY");
            yComp.setUnit(coordUnit);
            libsbmlcs.Boundary minY = yComp.createBoundaryMin();
            minY.setId("Ymin");
            minY.setValue(ymin);
            libsbmlcs.Boundary maxY = yComp.createBoundaryMax();
            maxY.setId("Ymax");
            maxY.setValue(ymax);

            CoordinateComponent zComp = geometry.createCoordinateComponent();
            zComp.setId(zCoord);
            zComp.setType("cartesianZ");
            zComp.setUnit(coordUnit);
            libsbmlcs.Boundary minZ = zComp.createBoundaryMin();
            minZ.setId("Zmin");
            minZ.setValue(zmin);
            libsbmlcs.Boundary maxZ = zComp.createBoundaryMax();
            maxZ.setId("Zmax");
            maxZ.setValue(zmax);

            //Create spatial params for each coordComp
            CreateParamForSpatialElement(xComp, xComp.getId());
            CreateParamForSpatialElement(yComp, yComp.getId());
            CreateParamForSpatialElement(zComp, zComp.getId());

            csGeometry = geometry.createCsGeometry();
            csGeometry.setId("CSGeometry");
            csGeometry.setIsActive(true);
        }


        /// <summary>
        /// Adds spatial parameters to model
        /// </summary>
        /// <param name="sBaseElement"></param>
        /// <param name="spatialId"></param>
        private void CreateParamForSpatialElement(SBase sBaseElement, String spatialId)
        {
            Parameter p = model.createParameter();
            if (sBaseElement is CoordinateComponent)
            {
                CoordinateComponent cc = (CoordinateComponent)sBaseElement;
                // coordComponent with index = 1 represents X-axis, hence set param id as 'x'
                if (cc.getType()==1)
                {
                    p.setId("x");
                }
                else if (cc.getType()==2)
                {
                    p.setId("y");
                }
                else if (cc.getType()==3)
                {
                    p.setId("z");
                }
                p.setUnits(model.getLengthUnits());
            }
            else
            {
                p.setId(spatialId);
                p.setValue(0.0);
            }
            p.setConstant(false);
            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin(packageName);
            SpatialSymbolReference symbol = pplug.createSpatialSymbolReference();
            symbol.setSpatialRef(spatialId);
            }

        /// <summary>
        /// Sets the mmetre unit as unit of length and units used for model params
        /// </summary>        
        private void AddSpatialUnits()
        {
            Unit unit;
            string unitString = "mmetre";
            if (model.getUnitDefinition(unitString) == null)
            {
                udef = model.createUnitDefinition();
                //build micro meter per min unit
                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_METRE);
                unit.setExponent(1);
                unit.setScale(-6);
                unit.setMultiplier(1);
                udef.setId(unitString);
                model.setLengthUnits(unitString);
            }

            //Units used for params storing diffusion coefficients must be length^2 per time 
            if (model.getUnitDefinition(diffUnits) == null)
            {
                //build micro meter sqrd unit
                udef = model.createUnitDefinition();
                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_METRE);
                unit.setExponent(2);
                unit.setScale(-6);
                unit.setMultiplier(1);

                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_SECOND);
                unit.setExponent(-1);
                unit.setScale(0);
                unit.setMultiplier(60);
                udef.setId(diffUnits);
            }
            //Units used for params storing flux boundary condition values must be conc*length/time
            if (model.getUnitDefinition(fluxBoundaryUnits) == null) {
                //build micro meter sqrd unit
                udef = model.createUnitDefinition();
                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_ITEM);
                unit.setExponent(1);
                unit.setScale(1);
                unit.setMultiplier(1);

                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_METRE);
                unit.setExponent(-2);
                unit.setScale(-6);
                unit.setMultiplier(1);

                unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_SECOND);
                unit.setExponent(-1);
                unit.setScale(0);
                unit.setMultiplier(60);
                udef.setId(fluxBoundaryUnits);
            }
        }
    }
}
