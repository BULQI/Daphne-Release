using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public class CellManager
    {
        public CellManager()
        {
        }

        public void Step(double dt)
        {
            foreach (KeyValuePair<int, Cell> kvp in Sim.Cells)
            {
                kvp.Value.Step(dt);

                if (kvp.Value.IsMotile == true)
                {
                    double[] force = kvp.Value.Force(dt, kvp.Value.State.X);

                    // A simple implementation of movement. For testing.
                    for (int i = 0; i < kvp.Value.State.X.Length; i++)
                    {
                        kvp.Value.State.X[i] += kvp.Value.State.V[i] * dt;
                        kvp.Value.State.V[i] += -1.0 * kvp.Value.State.V[i] + force[i] * dt;
                    }
                }
            }
        }

        public void WriteStates(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                int n = 0;
                foreach (KeyValuePair<int, Cell> kvp in Sim.Cells)
                {
                    writer.Write(n + "\t"
                        + kvp.Value.State.X[0] + "\t" + kvp.Value.State.X[1] + "\t" + kvp.Value.State.X[2] + "\t"
                        + kvp.Value.State.V[0] + "\t" + kvp.Value.State.V[1] + "\t" + kvp.Value.State.V[2]
                        + "\n");

                    n++;
                }
            }
        }

        public Simulation Sim { get; set; }
    }

    public class Simulation
    {
        public Simulation()
        {
            cellManager = new CellManager();
            cellManager.Sim = this;
            cells = new Dictionary<int, Cell>();
        }

        public void AddCell(Cell c)
        {
            cells.Add(c.Index, c);
            // add the cell membrane to the ecs
            if (extracellularSpace == null)
            {
                throw new Exception("Need to create the ECS before adding cells.");
            }

            // no cell rotation currently
            Transform t = new Transform(false);

            extracellularSpace.Space.Boundaries.Add(c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);
            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.State.X);
            extracellularSpace.Space.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        public void Load(SimConfiguration sc)
        {
            Scenario scenario = sc.scenario;

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(scenario));

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            Cells.Clear();

            double[] extent = new double[] { ECS.Space.Interior.Extent(0), 
                                             ECS.Space.Interior.Extent(1), 
                                             ECS.Space.Interior.Extent(2) };

            // ADD CELLS            
            double[] cellPos = new double[ECS.Space.Interior.Dim];

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
                            double[] sigma = { extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0 }, center = new double[ECS.Space.Interior.Dim];

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
                    ECS.Space.AddMolecularPopulation(mol, "gauss", initArray);
                    ECS.Space.Populations[mol.Name].IsDiffusing = false;
                }
            }

            // ADD ECS REACTIONS
            foreach (KeyValuePair<int, Cell> kvp in Cells)
            {
                MolecularPopulation receptor, ligand, complex;
                double k1plus = 2.0, k1minus = 1;

                foreach (ConfigReaction grt in scenario.Reactions)
                {
                    if (grt.ReacType == ReactionType.BoundaryAssociation || grt.ReacType == ReactionType.BoundaryDissociation)
                    {
                        GuiBoundaryReactionTemplate gbrt = (GuiBoundaryReactionTemplate)grt;

                        receptor = kvp.Value.PlasmaMembrane.Populations[gbrt.receptor.species];
                        ligand = ECS.Space.Populations[gbrt.ligand.species];
                        complex = kvp.Value.PlasmaMembrane.Populations[gbrt.complex.species];

                        if (grt.ReacType == ReactionType.BoundaryAssociation)
                        {
                            ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                        }
                        else
                        {
                            ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                        }
                    }
                    else if (grt.ReacType == ReactionType.Association)
                    {
                        ECS.Space.Reactions.Add(new Association(ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                    ECS.Space.Populations[grt.listOfReactants[1].species],
                                                                    ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                    k1plus));
                    }
                    else if (grt.ReacType == ReactionType.Dissociation)
                    {
                        ECS.Space.Reactions.Add(new Association(ECS.Space.Populations[grt.listOfReactants[0].species],
                                                                    ECS.Space.Populations[grt.listOfProducts[0].species],
                                                                    ECS.Space.Populations[grt.listOfProducts[1].species],
                                                                    k1minus));
                    }
                    else if (grt.ReacType == ReactionType.Annihilation)
                    {
                        ECS.Space.Reactions.Add(new Annihilation(ECS.Space.Populations[grt.listOfReactants[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.Dimerization)
                    {
                        ECS.Space.Reactions.Add(new Dimerization(ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.DimerDissociation)
                    {
                        ECS.Space.Reactions.Add(new DimerDissociation(ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.Transformation)
                    {
                        ECS.Space.Reactions.Add(new Transformation(ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.AutocatalyticTransformation)
                    {
                        ECS.Space.Reactions.Add(new AutocatalyticTransformation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedAnnihilation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAssociation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedAssociation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfReactants[1].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedAnnihilation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedAnnihilation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDimerization)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedDimerization(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDimerDissociation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedDimerDissociation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedDissociation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedDissociation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], ECS.Space.Populations[grt.listOfProducts[1].species], grt.RateConst));
                    }
                    else if (grt.ReacType == ReactionType.CatalyzedTransformation)
                    {
                        ECS.Space.Reactions.Add(new CatalyzedTransformation(ECS.Space.Populations[grt.listOfModifiers[0].species], ECS.Space.Populations[grt.listOfReactants[0].species], ECS.Space.Populations[grt.listOfProducts[0].species], grt.RateConst));
                    }

                }
            }

        }

        public ExtraCellularSpace ECS
        {
            get { return extracellularSpace; }
            set { extracellularSpace = value; }
        }

        public CellManager CMGR
        {
            get { return cellManager; }
        }

        public Dictionary<int, Cell> Cells
        {
            get { return cells; }
            set { cells = value; }
        }

        private ExtraCellularSpace extracellularSpace;
        private CellManager cellManager;
        private Dictionary<int, Cell> cells;
    }
}
