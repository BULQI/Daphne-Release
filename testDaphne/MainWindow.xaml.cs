using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using MathNet.Numerics.LinearAlgebra;

using Ninject;
using Ninject.Parameters;

using Daphne;
using ManifoldRing;
using Newtonsoft.Json;

namespace testDaphne
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private int nSteps;
        private ReactionsConfigurator config;

        private Dictionary<string, Molecule> MolDict;
        private static Simulation sim;

        public static Simulation Sim
        {
            get { return MainWindow.sim; }
            set { MainWindow.sim = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void initialize()
        {
            // For testing json serialization of scenario
            // Simulation sim = JsonLoadScenario();

            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            //MolDict = MoleculeBuilder.Go(molSpec);

            //config = new ReactionsConfigurator();
            //config.deserialize("ReacSpecFile1.xml");
            //config.TemplReacType(config.content.listOfReactions);

            ////
            //// Scenario: Diffusion dynamics test
            ////      extracellular fluid with CXCL13 and no cells
            //// The diffusion dynamics with zero-flux boundary conditions seem to be working, in the sense that 
            //// a heterogeneous distribution of material tends to a homogeneous distribution and there is no 
            //// significant loss of material as long as the following condition is met:
            ////    (dx^2) >> D*dt, where dx is the StepSize of the grid and D is the diffusion coefficient
            ////

            // create the simulation
            sim = new Simulation();

            //// NOTE:The order in which compartments and molecular populations are instantiated matters
            //// because of the BoundaryConcs and Fluxes in MolecularPopulations
            //// Assign all compartments first, then molecular populations

            ////
            //// Create Extracellular fluid
            //// 

            //int[] numGridPts = { 21, 21, 21};
            //// int[] numGridPts = { 10, 10, 10 };
            //// spatial extent in each dimension
            //double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };
            ////Compartment XCellSpace = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));
            ////sim.ECS = XCellSpace;
            //sim.ECS = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));
 
            ////
            //// Create Cells
            ////

            //int numCells = 1;
            //double cellRadius = 5.0;
            //double[] cellPos = new double[sim.ECS.Interior.Dim];

            //// One cell
            //cellPos[0] = sim.ECS.Interior.Extents[0] / 3.0;
            //cellPos[1] = sim.ECS.Interior.Extents[1] / 3.0;
            //cellPos[2] = sim.ECS.Interior.Extents[2] / 3.0;
            //sim.AddCell(cellPos, new double[] { 0, 0, 0 }, cellRadius);

            ////// Cells on a diagonal
            ////numCells = 3;
            ////for (int i = 0; i < numCells; i++)
            ////{
            ////    cellPos[0] = (i + 1) * XCellSpace.Interior.Extents[0] / (numCells + 1);
            ////    cellPos[1] = (i + 1) * XCellSpace.Interior.Extents[1] / (numCells + 1);
            ////    cellPos[2] = (i + 1) * XCellSpace.Interior.Extents[2] / (numCells + 1);

            ////    // create the cell with its Cytoplasm and PlasmaMembrane compartments
            ////    sim.AddCell(cellPos, new double[] { 0, 0, 0 }, cellRadius);
            ////}

            //// Add the cell plasma membrane as an embedded boundary manifold in the extracellular compartment
            //foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            //{
            //    TranslEmbedding cellMembraneEmbed = new TranslEmbedding(kvp.Value.PlasmaMembrane.Interior, (BoundedRectangularPrism)sim.ECS.Interior, new int[1] { 0 }, sim.CMGR.States[kvp.Value].X);
            //    sim.ECS.Interior.Boundaries.Add(kvp.Value.PlasmaMembrane.Interior, cellMembraneEmbed);
            //}

            ////
            //// Add all molecular populations
            ////

            //// Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            //GaussianScalarField gsf = new GaussianScalarField(sim.ECS.Interior);
            
            //// Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            //// Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            //// 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            //double maxConc = 2* (3.0) * (1e-6) * (1e-18) * (6.022e23);

            //double[] sigma = { sim.ECS.Interior.Extents[0] / 5.0, 
            //                   sim.ECS.Interior.Extents[1] / 5.0, 
            //                   sim.ECS.Interior.Extents[2] / 5.0 };

            //double[] center = new double[sim.ECS.Interior.Dim];
            //center[0] = sim.ECS.Interior.Extents[0] / 2.0;
            //center[1] = sim.ECS.Interior.Extents[1] / 2.0;
            //center[2] = sim.ECS.Interior.Extents[2] / 2.0;

            //gsf.Initialize(center, sigma, maxConc);

            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], gsf);
            ////sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            //sim.ECS.Populations["CXCL13"].IsDiffusing = false;

            //// Add PlasmaMembrane molecular populations
            //// Approximately, 20,000 CXCR5 receptors per cell
            //foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            //{
            //    kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], 255.0);
            //    kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], 0.0);
            //}

            ////// Add Cytosol molecular populations
            ////foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            ////{
            ////    kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driver"], 0);
            ////}

            ////
            //// Add reactions
            ////

            //MolecularPopulation receptor, ligand, complex;
            //double k1plus = 2.0, k1minus = 0.1;

            ////MolecularPopulation driver;
            ////double driverTotal = 255.0;
            ////double k2plus = 1, k2minus = 0.1;
            ////double transductionConstant = 1000.0;

            //foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            //{
            //    // add receptor ligand boundary association and dissociation
            //    // R+L-> C
            //    // C -> R+L
            //    receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
            //    ligand = sim.ECS.Populations["CXCL13"];
            //    complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

            //    // QUESTION: Does it matter to which manifold we assign the boundary reactions?
            //    //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
            //    //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
            //    sim.ECS.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
            //    sim.ECS.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

            //    kvp.Value.IsMotile = false;

            //    //// Add driver dynamics for locomotion
            //    //driver = kvp.Value.Cytosol.Populations["driver"];
            //    //kvp.Value.Cytosol.reactions.Add(new CatalyzedConservedBoundaryActivation(driver, complex, k2plus, driverTotal));
            //    //kvp.Value.Cytosol.reactions.Add(new BoundaryConservedDeactivation(driver, complex, k2minus));
            //    //kvp.Value.Cytosol.reactions.Add(new DriverDiffusion(driver));

            //    //kvp.Value.Locomotor = new Locomotor(driver, transductionConstant);

            //}

            //// For testing json serialization of scenarios
            //// JsonSaveScenario(sim);
        }



        private void JsonLoadScenario()
        {
            //Dictionary<int, Cell> cells;
            MolecularPopulation mp;
            //Molecule mol;

            string readText = File.ReadAllText("json_output.txt");

            // The whole enchilada
            // Error - Unable to find a constructor to use for type Daphne.MolecularPopulation. ...
            //sim = JsonConvert.DeserializeObject<Simulation>(readText);

            // same error as above - Unable to find a constructor to use for type Daphne.MolecularPopulation. ...
            //cells = JsonConvert.DeserializeObject<Dictionary<int, Cell>>(readText);
            //sim.Cells = cells;

            // Unable to find a constructor to use for type Daphne.MolecularPopulation. 
            // A class should either have a default constructor, one constructor with arguments or a constructor 
            // marked with the JsonConstructor attribute. Path 'Molecule', line 1, position 12.
            // Solution: changed Molecule to a class - not sure if this was necessary, added "empty" constructor
            // for MolecularPopulation
            // Next error:  Could not create an instance of type Daphne.DiscretizedManifold. 
            // Type is an interface or abstract class and cannot be instantiated. Path 'Man.ArraySize', line 1, position 120.
            mp = JsonConvert.DeserializeObject<MolecularPopulation>(readText);

            //// This works
            //mol = JsonConvert.DeserializeObject<Molecule>(readText);
        }

        private void JsonSaveScenario(Simulation sim)
        {
            MolecularPopulation mp;
            //Molecule mol;

            JsonSerializer jserializer = new JsonSerializer();

            using (StreamWriter sw = new StreamWriter("json_output.txt"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                jserializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                jserializer.TypeNameHandling = TypeNameHandling.All;

                // These don't work
                //jserializer.Serialize(writer, sim);
                //jserializer.Serialize(writer, sim.Cells);
                //jserializer.Serialize(writer, sim.ECS);

                // These don't work
                mp = sim.ECS.Space.Populations["CXCL13"];
                jserializer.Serialize(writer, mp);

                //// This works
                // mol = mp.Molecule;
                // jserializer.Serialize(writer, mol);
            }

            MessageBox.Show("Json output to " + "json_output.txt succeeded.");
        }

        private void step(int nSteps, double dt)
        {
            for (int i = 0; i < nSteps; i++)
            {
                sim.ECS.Space.Step(dt);
                sim.CMGR.Step(dt);
            }
        }

        private void go()
        {
            double T = 50;   // minutes
            double dt = 0.001;
            int nSteps = (int)(T / dt);

            initialize();
            // step();

            //// ECM: single molecular population, diffusing with zero flux at the natural boundary
            //// Cells: none
            //// Reactions: none
            //DiffusionScenario();
            //TestStepperDiffusion(nSteps, dt);

            //// ECM: single lingand molecular population, diffusing with zero flux (natural boundary conditions).
            //// Cells: one, receptor and complex molecular populations
            //// Reactions: boundary association and dissociation
            //LigandReceptorScenario();
            //TestStepperLigandReceptor(nSteps, dt);

            //// Not implemented yet
            //DriverLocomotionScenario();
            //TestStepperLocomotion(nSteps, dt);

            //// Displays ECM natural boundary coordinates
            //ECM_NaturalBoundaries_Test();

            // ECM: single molecular population, diffusing with applied flux at the natural boundary
            // Cells: none
            // Reactions: none
            FluxNaturalBoundaryConditions();
            FluxNaturalBC_Stepper(nSteps, dt);

            //// Not implemented yet
            //DirichletBoundaryConditions();
            //Dirichlet_Stepper(nSteps, dt);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            go();
        }
        
        private void TestStepperDiffusion(int nSteps, double dt)
        {
            double initQ, finalQ, relDiff;
            string output;

            initQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            for (int i = 0; i < nSteps; i++)
            {
                sim.ECS.Space.Step(dt);
                sim.CMGR.Step(dt);
                // Console.WriteLine(i);
            }

            finalQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            relDiff = (initQ - finalQ) / initQ;
            output = dt.ToString("E2") + "\t" + sim.ECS.Space.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
            output = output + "\t" + sim.ECS.Space.Interior.StepSize().ToString("F4");
            output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
            Console.WriteLine(output);

            sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");
        }

        private void TestStepperLigandReceptor(int nSteps, double dt)
        {
            double receptorConc,
                   ligandBoundaryConc,
                   complexConc;
            double[] driverLoc;

            string output;
            string filename = "LigandReceptorComplex.txt";

            using (StreamWriter writer = File.CreateText(filename))
            {
                for (int i = 0; i < nSteps; i++)
                {
                    sim.ECS.Space.Step(dt);
                    sim.CMGR.Step(dt);

                    // ecs boundary; convert cell position to the membrane's coordinate system
                    driverLoc = sim.ECS.Space.BoundaryTransforms[sim.Cells.First().Value.PlasmaMembrane.Interior.Id].toLocal(sim.Cells.First().Value.State.X);
                    ligandBoundaryConc = sim.ECS.Space.Populations["CXCL13"].BoundaryConcs[sim.Cells.First().Value.PlasmaMembrane.Interior.Id].Value(driverLoc);

                    // membrane; already in membrane's coordinate system
                    receptorConc = sim.Cells.First().Value.PlasmaMembrane.Populations["CXCR5"].Conc.Value(driverLoc);
                    complexConc = sim.Cells.First().Value.PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc.Value(driverLoc);

                    output = i * dt + "\t" + ligandBoundaryConc + "\t" + receptorConc + "\t" + complexConc;
                    //Console.WriteLine(output);
                    writer.WriteLine( output );
                }
            }
        }
        
        private void TestStepperLocomotion(int nSteps, double dt)
        {
            double receptorConc,
                   ligandBoundaryConc,
                   complexConc,
                   driverConc;
            double[] driverLoc, convDriverLoc;

            string output;
            string filename = "DriverDynamics.txt";

            using (StreamWriter writer = File.CreateText(filename))
            {
                for (int i = 0; i < nSteps; i++)
                {
                    sim.ECS.Space.Step(dt);
                    sim.CMGR.Step(dt);

                    // ecs boundary; convert cell position to the membrane's coordinate system
                    driverLoc = sim.Cells.First().Value.State.X;
                    convDriverLoc = sim.ECS.Space.BoundaryTransforms[sim.Cells.First().Value.PlasmaMembrane.Interior.Id].toLocal(driverLoc);
                    ligandBoundaryConc = sim.ECS.Space.Populations["CXCL13"].BoundaryConcs[sim.Cells.First().Value.PlasmaMembrane.Interior.Id].Value(convDriverLoc);

                    // membrane; already in membrane's coordinate system
                    receptorConc = sim.Cells.First().Value.PlasmaMembrane.Populations["CXCR5"].Conc.Value(convDriverLoc);
                    complexConc = sim.Cells.First().Value.PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc.Value(convDriverLoc);

                    // cytosol; convert from the membrane's to the cytosol's system
                    convDriverLoc = sim.Cells.First().Value.Cytosol.BoundaryTransforms[sim.Cells.First().Value.PlasmaMembrane.Interior.Id].toContaining(convDriverLoc);
                    driverConc = sim.Cells.First().Value.Cytosol.Populations["driver"].Conc.Value(convDriverLoc);

                    output = i * dt + "\t" + ligandBoundaryConc + "\t" + receptorConc + "\t" + complexConc + "\t" +
                             driverConc + "\t" + driverLoc[0] + "\t" + driverLoc[1] + "\t" + driverLoc[2];
                    //Console.WriteLine(output);
                    writer.WriteLine(output);
                }
            }
        }
          

        // 
        // Scenarios
        //

        private void DriverLocomotionScenario()
        {
            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Ligand:Receptor dynamics without ligand diffusion and driver:complex dynamics
            //      extracellular fluid with CXCL13 
            //      one cell with CXCR5 and CXCR5:CXCL13 surface molecules and driver molecules in the cytosol
            // The CXCL13 distribution is centered in the ECM.
            // The cell position is shifted negatively on the x-axis by one quarter of the length of the cube 
            // from the center of the CXCL13 distribution.
            //

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            sim.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            // Create Cells
            //
            double[] cellPos = new double[3],
                     veloc = new double[3] { 0.0, 0.0, 0.0 },
                     extent = new double[] { sim.ECS.Space.Interior.Extent(0), 
                                             sim.ECS.Space.Interior.Extent(1), 
                                             sim.ECS.Space.Interior.Extent(2) };

            // One cell
            Cell cell = SimulationModule.kernel.Get<Cell>();

            cellPos[0] = extent[0] / 4.0;
            cellPos[1] = extent[1] / 2.0;
            cellPos[2] = extent[2] / 2.0;
            cell.setState(cellPos, veloc);
            sim.AddCell(cell);

            //
            // Add all molecular populations
            //


            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0,
                                                2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };
            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            sim.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            sim.ECS.Space.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], "const", new double[] { 125.0 });
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], "const", new double[] { 130.0 });
            }

            // Add Cytosol molecular populations
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driver"], "const", new double[] { 250.0 });
            }

            //
            // Add reactions
            //

            MolecularPopulation receptor, ligand, complex;
            double k1plus = 2.0, k1minus = 1;
            MolecularPopulation driver;
            double  //k2plus = 1.0, 
                    //k2minus = 10.0,
                    //driverTotal = 500,
                    transductionConstant = 1e4;

            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                // add receptor ligand boundary association and dissociation
                // R+L-> C
                // C -> R+L
                receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
                ligand = sim.ECS.Space.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                // QUESTION: Does it matter to which manifold we assign the boundary reactions?
                //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

                // Choose false to have the driver dynamics, but no movement
                kvp.Value.IsMotile = true;

                // Add driver dynamics for locomotion
                driver = kvp.Value.Cytosol.Populations["driver"];
                //kvp.Value.Cytosol.Reactions.Add(new CatalyzedConservedBoundaryActivation(driver, complex, k2plus, driverTotal));
                //kvp.Value.Cytosol.Reactions.Add(new BoundaryConservedDeactivation(driver, complex, k2minus));
                //kvp.Value.Cytosol.Reactions.Add(new DriverDiffusion(driver));

                kvp.Value.Locomotor = new Locomotor(driver, transductionConstant);
            }

        }
        
        private void LigandReceptorScenario()
        {
            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Ligand:Receptor dynamics without ligand diffusion or driver dynamics
            //      extracellular fluid with CXCL13 
            //      one cell with CXCR5 and CXCR5:CXCL13 surface molecules
            //

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            sim.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            // Create Cells
            //
            double[] cellPos = new double[sim.ECS.Space.Interior.Dim],
                     extent = new double[] { sim.ECS.Space.Interior.Extent(0), 
                                             sim.ECS.Space.Interior.Extent(1), 
                                             sim.ECS.Space.Interior.Extent(2) };

            // One cell
            Cell cell = SimulationModule.kernel.Get<Cell>();

            cellPos[0] = extent[0] / 3.0;
            cellPos[1] = extent[1] / 3.0;
            cellPos[2] = extent[2] / 3.0;
            cell.setState(cellPos, new double[] { 0, 0, 0 });
            sim.AddCell(cell);

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0,
                                                2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            sim.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            sim.ECS.Space.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], "const", new double[] { 255.0 });
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], "const", new double[] { 0.0 });
            }

            //
            // Add reactions
            //

            MolecularPopulation receptor, ligand, complex;
            double k1plus = 2.0, k1minus = 1;

            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                // add receptor ligand boundary association and dissociation
                // R+L-> C
                // C -> R+L
                receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
                ligand = sim.ECS.Space.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                // QUESTION: Does it matter to which manifold we assign the boundary reactions?
                //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

                kvp.Value.IsMotile = false;

            }
        }

        private void DiffusionScenario()
        {
            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Diffusion dynamics test
            //      extracellular fluid with CXCL13 and no cells
            // The diffusion dynamics with zero-flux boundary conditions seem to be working, in the sense that 
            // a heterogeneous distribution of material tends to a homogeneous distribution and there is no 
            // significant loss of material as long as the following condition is met:
            //    (dx^2) >> D*dt, where dx is the StepSize of the grid and D is the diffusion coefficient
            //

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            sim.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double[] extent = new double[] { sim.ECS.Space.Interior.Extent(0), 
                                             sim.ECS.Space.Interior.Extent(1), 
                                             sim.ECS.Space.Interior.Extent(2) },
                     initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0,
                                                2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            sim.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            sim.ECS.Space.Populations["CXCL13"].IsDiffusing = true;

        }

        public void ECM_NaturalBoundaries_Test()
        {
            FakeConfig.numGridPts = new int[] { 3, 4, 5 };
            FakeConfig.gridStep = 2;

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            sim.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            InterpolatedNodes m;
            double[] x = new double[3];
            double[] X = new double[3];
            int[] indices = new int[2];

            foreach (KeyValuePair<string, int> kvp in sim.ECS.Sides)
            {
                Console.WriteLine(kvp.Key);
                m = (InterpolatedNodes)sim.ECS.Space.NaturalBoundaries[kvp.Value];

                for (int i = 0; i < m.ArraySize; i++)
                {
                    indices = m.linearIndexToIndexArray(i);
                    x = m.linearIndexToLocal(i);
                    X = sim.ECS.Space.NaturalBoundaryTransforms[kvp.Value].toContaining(x); 

                    Console.WriteLine("\t" + i + "\t" + indices[0] 
                                        + ", " + indices[1] + "\t" + x[0] + ", " + x[1]
                                        + "\t" + X[0] + ", " + X[1] + ", " + X[2]);
                }
            }

        }

        public void FluxNaturalBoundaryConditions()
        {
            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Diffusion dynamics test
            //      extracellular fluid with CXCL13 and no cells
            // CXCL13 diffuses
            // Input flux at right face of ECM
            // Equal output flux at left face.

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            sim.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            //
            // Add all molecular populations
            //

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            sim.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "const", new double[] { 10.0 });
            //sim.ECS.Space.Populations["CXCL13"].IsDiffusing = true;

            Manifold m;
            ScalarField sf;
            int n = sim.ECS.Sides["right"];

            m = sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { 100.0 });
            sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = sf;
            // A hack
            sim.ECS.Space.NaturalBoundaryTransforms[n].IsFluxing = true;

            n = sim.ECS.Sides["left"];
            m = sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { -100.0 });
            sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = sf;
            sim.ECS.Space.NaturalBoundaryTransforms[n].IsFluxing = true;

        }

        private void FluxNaturalBC_Stepper(int nSteps, double dt)
        {
            double initQ, finalQ, relDiff;
            string output;

            initQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            for (int i = 0; i < nSteps; i++)
            {
                sim.ECS.Space.Step(dt);
                sim.CMGR.Step(dt);
                // Console.WriteLine(i);
            }

            finalQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            relDiff = (initQ - finalQ) / initQ;
            output = dt.ToString("E2") + "\t" + sim.ECS.Space.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
            output = output + "\t" + sim.ECS.Space.Interior.StepSize().ToString("F4");
            output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
            Console.WriteLine(output);

            sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");
        }


        public void DirichletBoundaryConditions()
        {
            //// Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            //// Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            //MolDict = MoleculeBuilder.Go(molSpec);

            //config = new ReactionsConfigurator();
            //config.deserialize("ReacSpecFile1.xml");
            //config.TemplReacType(config.content.listOfReactions);

            ////
            //// Scenario: Diffusion dynamics test
            ////      extracellular fluid with CXCL13 and no cells
            //// CXCL13 diffuses
            //// Fix CXCL13 at right and left faces of ECM to maintain a concentration gradient

            ////
            //// Create Extracellular fluid
            //// 

            ////int[] numGridPts = { 31, 21, 11 };
            //int[] numGridPts = { 21, 21, 21 };
            //double gridStep = 50;

            //sim.CreateECS(new InterpolatedRectangularPrism(numGridPts, gridStep));

            ////
            //// Add all molecular populations
            ////

            //double[] extent = new double[] { sim.ECS.Space.Interior.Extent(0), 
            //                                 sim.ECS.Space.Interior.Extent(1), 
            //                                 sim.ECS.Space.Interior.Extent(2) },
            //         sigma = { extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0 },
            //         center = new double[sim.ECS.Space.Interior.Dim];

            //center[0] = extent[0] / 2.0;
            //center[1] = extent[1] / 2.0;
            //center[2] = extent[2] / 2.0;

            //// Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            //sim.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], new ConstFieldInitializer(10.0));
            ////sim.ECS.Space.Populations["CXCL13"].IsDiffusing = true;

            //Manifold m;

            //int n = sim.ECS.Sides["right"];
            //ConstFieldInitializer cfi = new ConstFieldInitializer(100.0);
            //m = sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            //sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = new ScalarField(m, cfi);
            //// A hack
            //sim.ECS.Space.NaturalBoundaryTransforms[n].IsFluxing = true;

            //n = sim.ECS.Sides["left"];
            //cfi = new ConstFieldInitializer(-100.0);
            //m = sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            //sim.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = new ScalarField(m, cfi);
            //sim.ECS.Space.NaturalBoundaryTransforms[n].IsFluxing = true;

        }

        private void Dirichlet_Stepper(int nSteps, double dt)
        {
            //double initQ, finalQ, relDiff;
            //string output;

            //initQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            //sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            //for (int i = 0; i < nSteps; i++)
            //{
            //    sim.ECS.Space.Step(dt);
            //    sim.CMGR.Step(dt);
            //    // Console.WriteLine(i);
            //}

            //finalQ = sim.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            //relDiff = (initQ - finalQ) / initQ;
            //output = dt.ToString("E2") + "\t" + sim.ECS.Space.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
            //output = output + "\t" + sim.ECS.Space.Interior.StepSize().ToString("F4");
            //output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
            //Console.WriteLine(output);

            //sim.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");
        }



    }

}

