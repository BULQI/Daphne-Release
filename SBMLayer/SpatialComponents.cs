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

        private SimConfigurator configurator;
        private Model model;

        //Unit definition declaration
        private UnitDefinition udef;

        private Geometry geometry;

        //Equivalent geometries may be supported (analytic/SF)
        private CSGeometry csGeometry;

        public SpatialComponents(Daphne.SimConfigurator configurator, ref libsbmlcs.Model model)
        {
            this.configurator = configurator;
            this.model = model;

            InitializeCoordinateSystem();
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
            xmax = configurator.SimConfig.scenario.environment.extent_x;
            ymax = configurator.SimConfig.scenario.environment.extent_y;
            zmax = configurator.SimConfig.scenario.environment.extent_z;

            //Defining a coordinate system
            SpatialModelPlugin plugin = (SpatialModelPlugin)model.getPlugin("spatial");
            geometry = plugin.getGeometry();
            geometry.setCoordinateSystem("Cartesian");
            string coordUnit = AddCoordSystemUnit();

            //Instantiating Coord components of the system
            CoordinateComponent xComp = geometry.createCoordinateComponent();
            xComp.setSpatialId("x");
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
            yComp.setSpatialId("y");
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
            zComp.setSpatialId("z");
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

            // since p is a parameter from 'spatial' package, need to set the
            // requiredElements attributes on parameter 
            SpatialParameterPlugin pplug = (SpatialParameterPlugin)p.getPlugin("spatial");
            SpatialSymbolReference symbol = pplug.getSpatialSymbolReference();
            symbol.setSpatialId(spatialId);
            symbol.setType(sBaseElement.getElementName());

            RequiredElementsSBasePlugin req = (RequiredElementsSBasePlugin)p.getPlugin("req");
            req.setCoreHasAlternateMath(true);
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
