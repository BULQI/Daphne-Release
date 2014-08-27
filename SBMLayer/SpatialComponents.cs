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

        private string xCoord = "x";
        private string yCoord = "y";
        private string zCoord = "z";

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

            InitializeCoordinateSystem();
        }


        /// <summary>
        /// Adds an adjacency relation for 2 given domains
        /// </summary>
        /// <param name="domainOne"></param>
        /// <param name="domainTwo"></param>
        public void AddAdjacency(string domainOne, string domainTwo) {
          AdjacentDomains adjDomains=geometry.createAdjacentDomains();
          adjDomains.setSpatialId(domainOne+"_"+domainTwo);
          adjDomains.setDomain1(domainOne);
          adjDomains.setDomain2(domainTwo);
        }

        /// <summary>
        /// Adds DomainType-Domain-Geometry for background compartment
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="ordinal"></param>
        public Domain AddBackground(libsbmlcs.Compartment comp, int ordinal)
        {
            //Define a DomainType for the background container
            backgroundDomainType = geometry.createDomainType();
            backgroundDomainType.setSpatialDimensions(3);
            backgroundDomainType.setSpatialId("background_domainType");
            
            //Add a CompartmentMapping element to link DomainType with ECS compartment
            SpatialCompartmentPlugin cplug = (SpatialCompartmentPlugin)comp.getPlugin("spatial");
            CompartmentMapping map = cplug.getCompartmentMapping();
            map.setSpatialId("mapping_" + backgroundDomainType.getSpatialId() + "_" + comp.getId());
            map.setCompartment(comp.getId());
            map.setDomainType(backgroundDomainType.getSpatialId());
            map.setUnitSize(1.0);

            //Define a Domain for the given volumetric space
            Domain backgroundDomain = geometry.createDomain();
            backgroundDomain.setDomainType(backgroundDomainType.getSpatialId());
            backgroundDomain.setSpatialId("background_domain");
    
            //Define the shape of the Domain
            AddBackgroundGeometry(ordinal);
            return backgroundDomain;
        }

        /// <summary>
        /// Instantiates a CSGObject to define the background as a cube
        /// </summary>
        /// <param name="ordinal"></param>
        public void AddBackgroundGeometry(int ordinal)
        {
		    CSGObject backgroundObject = csGeometry.createCSGObject();
		    CSGTranslation translation = backgroundObject.createCSGTranslation();
            translation.setSpatialId("translation_background");
            backgroundObject.setOrdinal(ordinal);
		
            // Defines the location of the simulation space
            translation.setTranslateX((xmin+xmax)/2.0);
		    translation.setTranslateY((ymin+ymax)/2.0);
		    translation.setTranslateZ((zmin+zmax)/2.0);
		    CSGScale scale = translation.createCSGScale();
            scale.setSpatialId("scale_background");
		    scale.setScaleX(xmax-xmin);
		    scale.setScaleY(ymax-ymin);
		    scale.setScaleZ(zmax-zmin);

            //Geometrical definition of simulation space as a cube
		    CSGPrimitive cube = scale.createCSGPrimitive();
            cube.setSpatialId("solid_background");
		    cube.setPrimitiveType("SOLID_CUBE");
		    backgroundObject.setDomainType(backgroundDomainType.getSpatialId());
		    backgroundObject.setSpatialId("CSGObject_background");         
        }

        /// <summary>
        /// Instantiates a DomainType for a given cell population
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="ordinal"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public DomainType AddCellModel(libsbmlcs.Compartment comp, Boolean isMembrane) 
        {
            //Instantiate a DomainType for a given cell type
            DomainType domainType = geometry.createDomainType();
            if (isMembrane)
            {
                domainType.setSpatialId("cellType_" + comp.getId());
                domainType.setSpatialDimensions(2);
            }
            else
            {
                domainType.setSpatialId("cellType_" + comp.getId());
                domainType.setSpatialDimensions(3);
            }
     

            //Add a CompartmentMapping element to link DomainType with Cell compartment
            SpatialCompartmentPlugin cplug = (SpatialCompartmentPlugin)comp.getPlugin("spatial");
            CompartmentMapping map = cplug.getCompartmentMapping();
            map.setSpatialId("mapping_" + domainType.getSpatialId() + "_" + comp.getId());
            map.setCompartment(comp.getId());
            map.setDomainType(domainType.getSpatialId());
            map.setUnitSize(1.0);

            return domainType;
        }

        public void ModifySpatialSpecies(libsbmlcs.Species species) {
            SpatialSpeciesRxnPlugin splug = (SpatialSpeciesRxnPlugin)species.getPlugin("spatial");
            splug.setIsSpatial(true);

            SetRequiredElements(species,true);
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
            p.setId(species.getId() + "_BC_"+ domainType.getSpatialId());
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            libsbmlcs.BoundaryCondition symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setBoundaryDomainType(domainType.getSpatialId());
            symbol.setType("Flux");
        }


        public void SetBoxBoundaryCondition(Species species) 
        {
 
            //Add Boundary conditions for each coordinate element in the system (our simulations will always be 3D)
            Parameter p = model.createParameter();
            p.setId(species.getId() + "_BC_Xmin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            libsbmlcs.BoundaryCondition symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Xmin");
            symbol.setType("Flux");

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Xmax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Xmax");
            symbol.setType("Flux");

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Ymin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Ymin");
            symbol.setType("Flux");

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Ymax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Ymax");
            symbol.setType("Flux");

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Zmin");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Zmin");
            symbol.setType("Flux");

            p = model.createParameter();
            p.setId(species.getId() + "_BC_Zmax");
            p.setValue(0.0); //No Flux
            p.setConstant(true);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);

            pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            symbol = pplug.getBoundaryCondition();
            symbol.setVariable(species.getId());
            symbol.setCoordinateBoundary("Zmax");
            symbol.setType("Flux");
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
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SetRequiredElements(p, true);
         
            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            DiffusionCoefficient symbol = pplug.getDiffusionCoefficient();
            symbol.setVariable(species.getId());
        }


        /// <summary>
        /// Adds domains to the given Cell DomainType
        /// </summary>
        /// <param name="domainType"></param>
        /// <param name="comp"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="zCoord"></param>
        public Domain AddCellDomain(DomainType domainType, libsbmlcs.Compartment comp, double xCoord, double yCoord, double zCoord)
        {
            Domain domain = geometry.createDomain();
            domain.setDomainType(domainType.getSpatialId());
            domain.setSpatialId("cell_" + comp.getId());
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
        public void AddCellGeometry(DomainType domainType, int ordinal,  double radius,double xCoord, double yCoord, double zCoord)
        {
            CSGObject csgObject = csGeometry.createCSGObject();
            CSGTranslation translation = csgObject.createCSGTranslation();
            translation.setSpatialId("translation_"+domainType.getSpatialId());
            csgObject.setOrdinal(ordinal);

            // Defines the location of the simulation space
            translation.setTranslateX(xCoord);
            translation.setTranslateY(yCoord);
            translation.setTranslateZ(zCoord);
            CSGScale scale = translation.createCSGScale();
            scale.setSpatialId("scale_" + domainType.getSpatialId());
            scale.setScaleX(radius);
            scale.setScaleY(radius);
            scale.setScaleZ(radius);

            //Geometrical definition of simulation space as a cube
            CSGPrimitive sphere = scale.createCSGPrimitive();
            sphere.setSpatialId("solid_" + domainType.getSpatialId());
            sphere.setPrimitiveType("SOLID_SPHERE");
            csgObject.setDomainType(domainType.getSpatialId());
            csgObject.setSpatialId("CSGObject_" + domainType.getSpatialId()); 
        }

        /// <summary>
        /// Initializes coordinate system and geometryDefinition to be used
        /// </summary>
        private void InitializeCoordinateSystem()
        {
            //Define dimensions of the simulation space
            xmin = 0;
            ymin = 0;
            zmin = 0;
            xmax =  protocol.scenario.environment.extent_x;
            ymax =  protocol.scenario.environment.extent_y;
            zmax =  protocol.scenario.environment.extent_z;

            //Defining a coordinate system
            SpatialModelPlugin plugin = (SpatialModelPlugin)model.getPlugin("spatial");
            geometry = plugin.getGeometry();
            geometry.setCoordinateSystem("Cartesian");
            string coordUnit = AddCoordSystemUnit();

            //Instantiating Coord components of the system
            CoordinateComponent xComp = geometry.createCoordinateComponent();
            xComp.setSpatialId(xCoord);
            xComp.setComponentType("cartesianX");
            xComp.setSbmlUnit(coordUnit);
            xComp.setIndex(0);
            BoundaryMin minX = xComp.createBoundaryMin();
            minX.setSpatialId("Xmin");
            minX.setValue(xmin);
            BoundaryMax maxX = xComp.createBoundaryMax();
            maxX.setSpatialId("Xmax");
            maxX.setValue(xmax);

            CoordinateComponent yComp = geometry.createCoordinateComponent();
            yComp.setSpatialId(yCoord);
            yComp.setComponentType("cartesianY");
            yComp.setSbmlUnit(coordUnit);
            yComp.setIndex(1);
            BoundaryMin minY = yComp.createBoundaryMin();
            minY.setSpatialId("Ymin");
            minY.setValue(ymin);
            BoundaryMax maxY = yComp.createBoundaryMax();
            maxY.setSpatialId("Ymax");
            maxY.setValue(ymax);

            CoordinateComponent zComp = geometry.createCoordinateComponent();
            zComp.setSpatialId(zCoord);
            zComp.setComponentType("cartesianZ");
            zComp.setSbmlUnit(coordUnit);
            zComp.setIndex(2);
            BoundaryMin minZ = zComp.createBoundaryMin();
            minZ.setSpatialId("Zmin");
            minZ.setValue(zmin);
            BoundaryMax maxZ = zComp.createBoundaryMax();
            maxZ.setSpatialId("Zmax");
            maxZ.setValue(zmax);

            //Create spatial params for each coordComp
            CreateParamForSpatialElement(xComp, xComp.getSpatialId());
            CreateParamForSpatialElement(yComp, yComp.getSpatialId());
            CreateParamForSpatialElement(zComp, zComp.getSpatialId());

            csGeometry = geometry.createCSGeometry();
            csGeometry.setSpatialId("CSGeometry");
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
                if (cc.getIndex() == 0)
                {
                    p.setId("x");
                }
                else if (cc.getIndex() == 1)
                {
                    p.setId("y");
                }
                else if (cc.getIndex() == 2)
                {
                    p.setId("z");
                }
            }
            else
            {
                p.setId(spatialId);
            }
            p.setValue(0.0);
            p.setConstant(false);
            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            SpatialSymbolReference symbol = pplug.getSpatialSymbolReference();
            symbol.setSpatialId(spatialId);
            symbol.setType(sBaseElement.getElementName());

            SetRequiredElements(p, false);
        }

        /// <summary>
        /// Sets the CoreHasAlternateMath and MathOverriden required attributes for spatial components
        /// </summary>
        /// <param name="sbase"></param>
        /// <param name="isRequired"></param>
        private void SetRequiredElements(SBase sbase, Boolean isRequired)
        {
            RequiredElementsSBasePlugin req = (RequiredElementsSBasePlugin)sbase.getPlugin("req");
            if (req == null) return;
            req.setCoreHasAlternateMath(isRequired);
            req.setMathOverridden("spatial");
        }

        /// <summary>
        /// Returns the mmetre unit
        /// </summary>
        /// <returns></returns>
        private string AddCoordSystemUnit()
        {
            string unitString = "mmetre";
            if (model.getUnitDefinition(unitString) == null)
            {
                udef = model.createUnitDefinition();
                //build micro meter per min unit
                Unit unit = model.createUnit();
                unit.setKind(unitType.UNIT_KIND_METRE);
                unit.setExponent(1);
                unit.setScale(-6);
                unit.setMultiplier(1);
                udef.setId(unitString);
            }
            return unitString;
        }


    }
}
