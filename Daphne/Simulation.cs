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

        public void Load(SimConfiguration sc)
        {
            Scenario scenario = sc.scenario;

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(scenario));

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            dataBasket.Cells.Clear();

            double[] extent = new double[] { dataBasket.ECS.Space.Interior.Extent(0), 
                                             dataBasket.ECS.Space.Interior.Extent(1), 
                                             dataBasket.ECS.Space.Interior.Extent(2) };

            // ADD CELLS            
            double[] cellPos = new double[dataBasket.ECS.Space.Interior.Dim];

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                double transductionConstant = 1e4;

                for (int i = 0; i < cp.number; i++)
                {
                    Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cp.CellType.CellRadius));

                    cellPos[0] = extent[0] / 4.0;
                    cellPos[1] = extent[1] / 2.0;
                    cellPos[2] = extent[2] / 2.0;
                    cell.setState(cellPos, new double[] { 0, 0, 0 });
                    cell.IsMotile = false;

                    //foreach (GuiMolecularPopulation gmp in cp.CellMolPops)
                    foreach (ConfigMolecularPopulation cmp in cp.CellType.CellMolPops)
                    {
                        if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                        {
                            MolPopGaussianGradient mpgg = (MolPopGaussianGradient)cmp.mpInfo.mp_distribution;
                            double maxConc = mpgg.peak_concentration;  //2 * 3.0 * 1e-6 * 1e-18 * 6.022e23;
                            double[] sigma = { extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0 }, center = new double[dataBasket.ECS.Space.Interior.Dim];

                            center[0] = extent[0] / 2.0;
                            center[1] = extent[1] / 2.0;
                            center[2] = extent[2] / 2.0;

                            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
                            Molecule mol = new Molecule(cmp.Molecule.Name, cmp.Molecule.MolecularWeight, cmp.Molecule.EffectiveRadius, cmp.Molecule.DiffusionCoefficient);

                            //if (gmp.InMembrane)
                            if (cmp.Location == MolPopPosition.Membrane)
                            {
                                //cell.PlasmaMembrane.AddMolecularPopulation(mol, new GaussianFieldInitializer(center, sigma, maxConc));
                                if (mol.Name == "CXCR5")
                                {
                                    cell.PlasmaMembrane.AddMolecularPopulation(mol, "const", new double[] { 125.0 });
                                }
                                else if (mol.Name == "CXCR5:CXCL13")
                                {
                                    cell.PlasmaMembrane.AddMolecularPopulation(mol, "const", new double[] { 130.0 });
                                }
                                else
                                {
                                    cell.PlasmaMembrane.AddMolecularPopulation(mol, "const", new double[] { 0.0 });
                                }
                            }
                            else
                            {
                                cell.Cytosol.AddMolecularPopulation(mol, "const", new double[] { 250.0 });
                            }
                        }

                    }

                    //CELL REACTIONS
                    //if (cp.CellReactions != null)
                    if (cp.CellType.CellReactions != null)
                    {
                        //foreach (ConfigReaction grt in cp.CellReactions)
                        foreach (ConfigReaction grt in cp.CellType.CellReactions)
                        {
                            if (grt.ReacType == ReactionType.Association)
                            {
                                double k1plus = 2.0;
                                cell.Cytosol.Reactions.Add(new Association(cell.Cytosol.Populations[grt.listOfReactants[0].species],
                                                                            cell.Cytosol.Populations[grt.listOfReactants[1].species],
                                                                            cell.Cytosol.Populations[grt.listOfProducts[0].species],
                                                                            k1plus));
                            }
                            else if (grt.ReacType == ReactionType.Dissociation)
                            {
                                double k1minus = 1;
                                cell.Cytosol.Reactions.Add(new Association(cell.Cytosol.Populations[grt.listOfReactants[0].species],
                                                                            cell.Cytosol.Populations[grt.listOfProducts[0].species],
                                                                            cell.Cytosol.Populations[grt.listOfProducts[1].species],
                                                                            k1minus));
                            }
                            else if (grt.ReacType == ReactionType.Dimerization)
                            {
                                cell.Cytosol.Reactions.Add(new Dimerization(cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.DimerDissociation)
                            {
                                cell.Cytosol.Reactions.Add(new DimerDissociation(cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.Transformation)
                            {
                                cell.Cytosol.Reactions.Add(new Transformation(cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.AutocatalyticTransformation)
                            {
                                cell.Cytosol.Reactions.Add(new AutocatalyticTransformation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedAnnihilation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedAssociation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedAssociation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfReactants[1].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedAnnihilation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedDimerization)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedDimerization(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedDimerDissociation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedDimerDissociation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedDissociation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedDissociation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], cell.Cytosol.Populations[grt.listOfProducts[1].species], grt.RateConst));
                            }
                            else if (grt.ReacType == ReactionType.CatalyzedTransformation)
                            {
                                cell.Cytosol.Reactions.Add(new CatalyzedTransformation(cell.Cytosol.Populations[grt.listOfModifiers[0].species], cell.Cytosol.Populations[grt.listOfReactants[0].species], cell.Cytosol.Populations[grt.listOfProducts[0].species], grt.RateConst));
                            }
                        }
                    }

                    if (cell.Cytosol.Populations.ContainsKey("driver"))
                    {
                        MolecularPopulation driver = driver = cell.Cytosol.Populations["driver"];
                        cell.Locomotor = new Locomotor(driver, transductionConstant);
                    }

                    AddCell(cell);

                }
            }

            // ADD ECS MOLECULAR POPULATIONS

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3

            foreach (ConfigMolecularPopulation gmp in scenario.MolPops)
            {
                if (gmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    MolPopGaussianGradient mpgg = (MolPopGaussianGradient)gmp.mpInfo.mp_distribution;
                    double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                        extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0,
                                                        mpgg.peak_concentration };  // 2 * 3.0 * 1e-6 * 1e-18 * 6.022e23

                    // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
                    Molecule mol = new Molecule(gmp.Molecule.Name, gmp.Molecule.MolecularWeight, gmp.Molecule.EffectiveRadius, gmp.Molecule.DiffusionCoefficient);
                    dataBasket.ECS.Space.AddMolecularPopulation(mol, "gauss", initArray);
                    dataBasket.ECS.Space.Populations[mol.Name].IsDiffusing = false;
                }
            }

            // ADD ECS REACTIONS
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                MolecularPopulation receptor, ligand, complex;
                double k1plus = 2.0, k1minus = 1;

                foreach (ConfigReaction grt in scenario.Reactions)
                {
                    if (grt.ReacType == ReactionType.BoundaryAssociation || grt.ReacType == ReactionType.BoundaryDissociation)
                    {
                        GuiBoundaryReactionTemplate gbrt = (GuiBoundaryReactionTemplate)grt;

                        receptor = kvp.Value.PlasmaMembrane.Populations[gbrt.receptor.species];
                        ligand = dataBasket.ECS.Space.Populations[gbrt.ligand.species];
                        complex = kvp.Value.PlasmaMembrane.Populations[gbrt.complex.species];

                        if (grt.ReacType == ReactionType.BoundaryAssociation)
                        {
                            dataBasket.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                        }
                        else
                        {
                            dataBasket.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                        }
                    }
                    else if (grt.ReacType == ReactionType.Association)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new Association(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                           dataBasket.ECS.Space.Populations[grt.listOfReactants[1].species],
                                                                           dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                           k1plus));
                    }
                    else if (grt.ReacType == ReactionType.Dissociation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new Association(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                           dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                           dataBasket.ECS.Space.Populations[grt.listOfProducts[1].species],
                                                                           k1minus));
                    }
                    else if (grt.ReacType == ReactionType.Annihilation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new Annihilation(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                            grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.Dimerization)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new Dimerization(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                            dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                            grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.DimerDissociation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new DimerDissociation(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                 dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                 grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.Transformation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new Transformation(dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                              dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                              grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.AutocatalyticTransformation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new AutocatalyticTransformation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                           dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                           grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedAnnihilation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                     grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAssociation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedAssociation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                    dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                    dataBasket.ECS.Space.Populations[grt.listOfReactants[1].species],
                                                                                    dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                    grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedAnnihilation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                     grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDimerization)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedDimerization(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                     grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDimerDissociation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedDimerDissociation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                          dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                          dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                          grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDissociation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedDissociation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                     dataBasket.ECS.Space.Populations[grt.listOfProducts[1].species],
                                                                                     grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedTransformation)
                    {
                        dataBasket.ECS.Space.Reactions.Add(new CatalyzedTransformation(dataBasket.ECS.Space.Populations[grt.listOfModifiers[0].species],
                                                                                       dataBasket.ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                                       dataBasket.ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                                       grt.RateConst));
                    }

                }
            }

        }

        public CellManager CMGR
        {
            get { return cellManager; }
        }

        private CellManager cellManager;
    }
}
