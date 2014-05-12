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
            // create the simulation
            sim = new Simulation();
        }

        private void step(int nSteps, double dt)
        {
            for (int i = 0; i < nSteps; i++)
            {
                sim.Step(dt);
            }
        }

        private void go()
        {
            double T = 50;   // minutes
            double dt = 0.001;
            int nSteps = (int)(T / dt);

            initialize();

            // Run through scenarios to make sure nothing crashes after changes
            //for (int n=1; n<=6; n++)
            int n = 3;
            {
                switch (n)
                {
                    case 1:
                        // ECM: single molecular population, diffusing with zero flux at the natural boundary
                        // Cells: none
                        // Reactions: none
                        Console.WriteLine("\n DiffusionScenario \n");
                        DiffusionScenario();
                        TestStepperDiffusion(nSteps, dt);
                        break;

                    case 2:
                        // ECM: single lingand molecular population, diffusing with zero flux (natural boundary conditions).
                        // Cells: one, receptor and complex molecular populations
                        // Reactions: boundary association and dissociation
                        Console.WriteLine("\n LigandReceptorScenario \n");
                        LigandReceptorScenario( 2.0, 1.0);
                        TestStepperLigandReceptor(nSteps, dt, 2.0, 1.0);
                        break;

                    case 3:
                        // Console.WriteLine("\n DriverLocomotionScenario \n");
                        DriverLocomotionScenario();
                        TestStepperLocomotion(nSteps, dt);
                        break;

                    case 4:
                        // Displays ECM natural boundary coordinates
                        // Created to visualize the ECM coordinate system wrt faces 
                        Console.WriteLine("\n ECM_NaturalBoundaries_Test \n");
                        ECM_NaturalBoundaries_Test();
                        break;


                    case 5:
                        // ECM: single molecular population, diffusing with applied flux at the natural boundary
                        // Cells: none
                        // Reactions: none
                        Console.WriteLine("\n FluxNaturalBoundaryConditions \n");
                        FluxNaturalBoundaryConditions();
                        FluxNaturalBC_Stepper(nSteps, dt);
                        break;

                    case 6:
                        // Cells: none
                        // Reactions: none
                        Console.WriteLine("\n DirichletBoundaryConditions \n");
                        DirichletBoundaryConditions();
                        Dirichlet_Stepper(nSteps, dt);
                        break;
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            go();
        }
        
        private void TestStepperDiffusion(int nSteps, double dt)
        {
            double initQ, finalQ, relDiff;
            string output;

            initQ = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            for (int i = 0; i < nSteps; i++)
            {
                sim.Step(dt);
                // Console.WriteLine(i);
            }

            finalQ = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            relDiff = (initQ - finalQ) / initQ;
            output = dt.ToString("E2") + "\t" + Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
            output = output + "\t" + Simulation.dataBasket.ECS.Space.Interior.StepSize().ToString("F4");
            output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
            Console.WriteLine(output);

            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");
        }

        private void TestStepperLigandReceptor(int nSteps, double dt, double k1plus, double k1minus)
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
                    sim.Step(dt);

                    // ecs boundary; convert cell position to the membrane's coordinate system
                    driverLoc = Simulation.dataBasket.ECS.Space.BoundaryTransforms[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].toLocal(Simulation.dataBasket.Cells.First().Value.State.X);
                    ligandBoundaryConc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].BoundaryConcs[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].Value(driverLoc);

                    // membrane; already in membrane's coordinate system
                    receptorConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5"].Conc.Value(driverLoc);
                    complexConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc.Value(driverLoc);

                    output = i * dt + "\t" + ligandBoundaryConc + "\t" + receptorConc + "\t" + complexConc;
                    //Console.WriteLine(output);
                    writer.WriteLine( output );
                }
            }

            driverLoc = Simulation.dataBasket.ECS.Space.BoundaryTransforms[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].toLocal(Simulation.dataBasket.Cells.First().Value.State.X);
            complexConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc.Value(driverLoc);
            receptorConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5"].Conc.Value(driverLoc);
            ligandBoundaryConc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].BoundaryConcs[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].Value(driverLoc);
            double pred = ligandBoundaryConc / (ligandBoundaryConc + (k1minus / k1plus));
            double actual = complexConc / (complexConc + receptorConc);
            double relError = (pred - actual) / pred;
            string result;

            if (relError < 1e-3)
            {
                result = "GOOD";
            }
            else
            {
                result = "BAD";
            }
            Console.WriteLine("Relative error in R-L complex concentration = " + relError + "\t" + result);
            
        }
        
        private void TestStepperLocomotion(int nSteps, double dt)
        {
            double receptorConc,
                   ligandBoundaryConc,
                   complexConc,
                   driverConc,
                   driverAConc;
            double[] driverLoc, convDriverLoc;

            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");
            string output;
            string filename = "DriverDynamics.txt";
            nSteps = (int)(20/ dt);

            using (StreamWriter writer = File.CreateText(filename))
            {
                for (int i = 0; i < nSteps; i++)
                {
                    sim.Step(dt);

                    // ecs boundary; convert cell position to the membrane's coordinate system
                    driverLoc = Simulation.dataBasket.Cells.First().Value.State.X;
                    convDriverLoc = Simulation.dataBasket.ECS.Space.BoundaryTransforms[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].toLocal(driverLoc);
                    ligandBoundaryConc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].BoundaryConcs[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].Value(convDriverLoc);

                    // membrane; already in membrane's coordinate system
                    receptorConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5"].Conc.Value(convDriverLoc);
                    complexConc = Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc.Value(convDriverLoc);

                    // cytosol; convert from the membrane's to the cytosol's system
                    convDriverLoc = Simulation.dataBasket.Cells.First().Value.Cytosol.BoundaryTransforms[Simulation.dataBasket.Cells.First().Value.PlasmaMembrane.Interior.Id].toContaining(convDriverLoc);
                    driverConc = Simulation.dataBasket.Cells.First().Value.Cytosol.Populations["driver"].Conc.Value(convDriverLoc);
                    driverAConc = Simulation.dataBasket.Cells.First().Value.Cytosol.Populations["driverA"].Conc.Value(convDriverLoc);

                    output = i * dt + "\t" + ligandBoundaryConc + "\t" + receptorConc + "\t" + complexConc + "\t" +
                             driverConc + "\t" + driverAConc + "\t" + driverLoc[0] + "\t" + driverLoc[1] + "\t" + driverLoc[2];
                    //Console.WriteLine(output);
                    writer.WriteLine(output);
                }
            }

            // Check results

            InterpolatedNodes inm = (InterpolatedNodes)Simulation.dataBasket.ECS.Space.Interior;
            ScalarField conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc;
            MolecularPopulation mp = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
            double value, min, max, slope;
            double stepSize = inm.StepSize();
            int numNodes_x, numNodes_y, numNodes_z, m;

            numNodes_x = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0);
            numNodes_y = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1);
            numNodes_z = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2);
            min = mp.NaturalBoundaryConcs[Simulation.dataBasket.ECS.Sides["right"]].Value(new double[3] { 0.0, 0.0, 0.0 });
            max = mp.NaturalBoundaryConcs[Simulation.dataBasket.ECS.Sides["left"]].Value(new double[3] { 0.0, 0.0, 0.0 });
            slope = (max - min) / inm.Extent(0);

            // CXCL13 concentration profile

            // along midline of xy-plane at z=midpoint
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x + ((int)numNodes_z / 2) * numNodes_x * numNodes_y;
                value = conc.Value(inm.linearIndexToLocal(m));
                Console.WriteLine(i + " " +  value);
            }

            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");

       }
          

        // 
        // Scenarios
        //

        //private void DriverLocomotionScenario()
        //{
        //    // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
        //    // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
        //    // NOTE: Make the diffusion coefficient for CXCL13 small so the CXCL13 gradient doesn't dissipate too fast,
        //    // otherwise add Dirchlet boundary conditions. Diffusion coefficient value of zero causes NaN values.
        //    string molSpec = "CXCR5\t1.0\t0.0\t0.0\nCXCL13\t\t\t1.0\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t1.0e3\ndriverA\t\t\t1.0\nCXCL12\t7.96\t\t6.0e3\n";
        //    MolDict = MoleculeBuilder.Go(molSpec);

        //    config = new ReactionsConfigurator();
        //    config.deserialize("ReacSpecFile1.xml");
        //    config.TemplReacType(config.content.listOfReactions);

        //    //
        //    // Scenario: Ligand:Receptor dynamics without ligand diffusion and driver:complex dynamics
        //    //      extracellular fluid with CXCL13 
        //    //      one cell with CXCR5 and CXCR5:CXCL13 surface molecules and driver molecules in the cytosol
        //    // The CXCL13 distribution is centered in the ECM.
        //    // The cell position is shifted negatively on the x-axis by one quarter of the length of the cube 
        //    // from the center of the CXCL13 distribution.
        //    //

        //    // NOTE:The order in which compartments and molecular populations are instantiated matters
        //    // because of the BoundaryConcs and Fluxes in MolecularPopulations
        //    // Assign all compartments first, then molecular populations

        //    //
        //    // Create Extracellular fluid
        //    // 

        //    // executes the ninject bindings; call this after the config is initialized with valid values
        //    SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

        //    Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

        //    // Create Cells
        //    //
        //    double[] cellPos = new double[3],
        //             veloc = new double[3] { 0.0, 0.0, 0.0 },
        //             extent = new double[] { Simulation.dataBasket.ECS.Space.Interior.Extent(0), 
        //                                     Simulation.dataBasket.ECS.Space.Interior.Extent(1), 
        //                                     Simulation.dataBasket.ECS.Space.Interior.Extent(2) };
        //    double cellRadius = 5.0;

        //    // One cell
        //    Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cellRadius));

        //    cellPos[0] = extent[0] / 4.0;
        //    cellPos[1] = extent[1] / 4.0;
        //    cellPos[2] = extent[2] / 4.0;
        //    cell.setState(cellPos, veloc);
        //    sim.AddCell(cell);

        //    //
        //    // Add all molecular populations
        //    //


        //    // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
        //    // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
        //    // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
        //    double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
        //                                        extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
        //                                        2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };
        //    // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
        //    Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
        //    //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
        //    //Simulation.dataBasket.ECS.Space.Populations["CXCL13"].IsDiffusing = false;

        //    // Add PlasmaMembrane molecular populations
        //    // Approximately, 20,000 CXCR5 receptors per cell
        //    foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
        //    {
        //        kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], "const", new double[] { 255.0 });
        //        kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], "const", new double[] { 0.0 });
        //    }

        //    // Add Cytosol molecular populations
        //    foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
        //    {
        //        kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driver"], "const", new double[] { 250.0 });
        //        kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driverA"], "const", new double[] { 0.0 });
        //    }

        //    //
        //    // Add reactions
        //    //

        //    MolecularPopulation receptor, ligand, complex;
        //    double k1plus = 2.0, k1minus = 1;
        //    MolecularPopulation driver, driverA;
        //    double  k2plus = 1.0, 
        //            k2minus = 10.0,
        //            transductionConstant = 1e4;

        //    foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
        //    {
        //        // add receptor ligand boundary association and dissociation
        //        // R+L-> C
        //        // C -> R+L
        //        receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
        //        ligand = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
        //        complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

        //        // QUESTION: Does it matter to which manifold we assign the boundary reactions?
        //        //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
        //        //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
        //        // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
        //        Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
        //        Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

        //        // Choose false to have the driver dynamics, but no movement
        //        kvp.Value.IsMotile = true;

        //        // Add driver dynamics for locomotion
        //        driver = kvp.Value.Cytosol.Populations["driver"];
        //        driverA = kvp.Value.Cytosol.Populations["driverA"];
        //        kvp.Value.Cytosol.Reactions.Add(new CatalyzedBoundaryActivation(driver, driverA, complex, k2plus));
        //        kvp.Value.Cytosol.Reactions.Add(new Transformation(driverA, driver, k2minus));

        //        kvp.Value.Locomotor = new Locomotor(driver, transductionConstant);
        //    }

        //}

        private void DriverLocomotionScenario()
        {
            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            string molSpec = "CXCR5\t1.0\t0.0\t0.0\nCXCL13\t\t\t6.0e2\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t1.0e3\ndriverA\t\t\t1.0\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Cell locomotion with cytosol driver molecules
            //      extracellular fluid with CXCL13 and one cell 
            //      PlasmaMembrane CXCR5 and CXCR5:CXCL13 surface molecules 
            //      Cytosol driver and activated driver molecules
            //      Dirichlet boundary conditions on the CXCL13 distribution 
            //        with maximum on the left face (yz-plane at x=0) and 
            //        minimum on the right face (yz-plane at x=xmax) of the ECM.
            //      The CXCL13 initial distribution is uniform.
            //      The initial position of the cell is at the middle of the ECM.
            // 
            // As currently configured, the cell moves with positive vx=vy=vz. 
            // Not sure why vy and vz are not zero.
            // Not sure why vx is not negative.
            //

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));
            
            // Create Cells
            //
            double[] state = new double[SpatialState.Dim],
                     extent = new double[] { Simulation.dataBasket.ECS.Space.Interior.Extent(0), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(1), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(2) };
            double cellRadius = 5.0;

            // One cell
            Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cellRadius));

            // set location; keep remaining state variables equal to zero
            state[0] = extent[0] / 2.0;
            state[1] = extent[1] / 2.0;
            state[2] = extent[2] / 2.0;
            cell.setState(state);
            sim.AddCell(cell);

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double midConc = 2 * 3.0 * 1e-6 * 1e-18 * 6.022e23;
            double leftConc = 2 * midConc; 
            double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                    extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                    midConc };
            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);

            //// Add a ligand MolecularPopulation whose concentration (molecules/um^3) is linear in x

            ////Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "const", new double[] { midConc });
            //Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "linear", new double[] { leftConc, 0.0, 0.0, extent[0], 0.0 });

            Manifold m;
            ScalarField sf;

            int n = Simulation.dataBasket.ECS.Sides["right"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Dirichlet = true;

            // m is the manifold corresponding to the right face
            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n].M;

            // Specify the value of the CXCL13 concentration to be enforced at the right face
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { 0.0 });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n] = sf;
            // Enforce that concentration to start
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.DirichletBC(sf, Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n]);

            n = Simulation.dataBasket.ECS.Sides["left"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Dirichlet = true;

            // m is the manifold corresponding to the left face
            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n].M;

            // Specify the value of the CXCL13 concentration to be enforce at the left face
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { leftConc });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n] = sf;
            // Enforce that concentration to start
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.DirichletBC(sf, Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n]);


            //// Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            //// Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            //// 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            //double[] initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
            //                                    extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
            //                                    2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };
            //// Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            //Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            ////sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            //Simulation.dataBasket.ECS.Space.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], "const", new double[] { 255.0 });
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], "const", new double[] { 0.0 });
            }

            // Add Cytosol molecular populations
            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driver"], "const", new double[] { 250.0 });
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driverA"], "const", new double[] { 0.0 });
            }

            //
            // Add reactions
            //

            MolecularPopulation receptor, ligand, complex;
            double k1plus = 2.0, k1minus = 1;
            MolecularPopulation driver, driverA;
            double k2plus = 10.0,
                    k2minus = 1.0,
                    transductionConstant = -1e6;

            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                // add receptor ligand boundary association and dissociation
                // R+L-> C
                // C -> R+L
                receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
                ligand = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

                // Choose false to have the driver dynamics, but no movement
                kvp.Value.IsMotile = true;

                // Add driver dynamics for locomotion
                driver = kvp.Value.Cytosol.Populations["driver"];
                driverA = kvp.Value.Cytosol.Populations["driverA"];
                kvp.Value.Cytosol.Reactions.Add(new CatalyzedBoundaryActivation(driver, driverA, complex, k2plus));
                kvp.Value.Cytosol.Reactions.Add(new Transformation(driverA, driver, k2minus));

                kvp.Value.Locomotor = new Locomotor(driver, transductionConstant);
            }
        }

        private void LigandReceptorScenario(double k1plus, double k1minus)
        {
            FakeConfig.gridStep = 50;
            FakeConfig.numGridPts = new int[] { 21, 21, 21 };

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

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            // Create Cells
            //
            double[] state = new double[SpatialState.Dim],
                     extent = new double[] { Simulation.dataBasket.ECS.Space.Interior.Extent(0), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(1), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(2) };
            double cellRadius = 5.0;

            // One cell
            Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cellRadius));

            // set position; keep remaining state variable equal to zero
            state[0] = extent[0] / 3.0;
            state[1] = extent[1] / 3.0;
            state[2] = extent[2] / 3.0;
            cell.setState(state);
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
            Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], "const", new double[] { 255.0 });
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], "const", new double[] { 0.0 });
            }

            //
            // Add reactions
            //

            MolecularPopulation receptor, ligand, complex;
            // double k1plus = 2.0, k1minus = 1;

            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                // add receptor ligand boundary association and dissociation
                // R+L-> C
                // C -> R+L
                receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
                ligand = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                // QUESTION: Does it matter to which manifold we assign the boundary reactions?
                //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                Simulation.dataBasket.ECS.Space.Reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

                kvp.Value.IsMotile = false;

            }
        }

        private void DiffusionScenario()
        {
            FakeConfig.gridStep = 50;
            FakeConfig.numGridPts = new int[] { 21, 21, 21 };

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

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double[] extent = new double[] { Simulation.dataBasket.ECS.Space.Interior.Extent(0), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(1), 
                                             Simulation.dataBasket.ECS.Space.Interior.Extent(2) },
                     initArray = new double[] { extent[0] / 2.0, extent[1] / 2.0, extent[2] / 2.0,
                                                extent[0] / 5.0, extent[1] / 5.0, extent[2] / 5.0,
                                                2 * 3.0 * 1e-6 * 1e-18 * 6.022e23 };

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "gauss", initArray);
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].IsDiffusing = true;
        }

        public void ECM_NaturalBoundaries_Test()
        {
            FakeConfig.numGridPts = new int[] { 3, 4, 5 };
            FakeConfig.gridStep = 2;

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            InterpolatedNodes m;
            double[] x = new double[3];
            double[] X = new double[3];
            int[] indices = new int[2];

            Console.WriteLine("ECS extents: " + Simulation.dataBasket.ECS.Space.Interior.Extent(0)
                                               + ", " + Simulation.dataBasket.ECS.Space.Interior.Extent(1)
                                               + ", " + Simulation.dataBasket.ECS.Space.Interior.Extent(2));

            foreach (KeyValuePair<string, int> kvp in Simulation.dataBasket.ECS.Sides)
            {
                Console.WriteLine(kvp.Key);
                m = (InterpolatedNodes)Simulation.dataBasket.ECS.Space.NaturalBoundaries[kvp.Value];

                for (int i = 0; i < m.ArraySize; i++)
                {
                    indices = m.linearIndexToIndexArray(i);
                    x = m.linearIndexToLocal(i);
                    X = Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[kvp.Value].toContaining(x); 

                    Console.WriteLine("\t" + i 
                                        + "\t local indices: " + indices[0] + ", " + indices[1] 
                                        + "\t local coordinates: " + x[0] + ", " + x[1]
                                        + "\t global coordinates: " + X[0] + ", " + X[1] + ", " + X[2]);
                }
            }

        }

        public void FluxNaturalBoundaryConditions()
        {
            FakeConfig.gridStep = 50;
            FakeConfig.numGridPts = new int[] { 21, 21, 21 };

            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            //
            // Increase diffusion coefficient for CXCL13 by factor of 10 to speedup testing.
            //
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e4\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Diffusion dynamics test with flux boundary conditions
            //      extracellular fluid with CXCL13 diffusing and no cells
            //      specify the fluxes at the right and left faces of ECM

            //
            // Create Extracellular fluid
            // 

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            //
            // Add all molecular populations
            //

            ScalarField sf;
            Manifold m;
            InterpolatedNodes inm = (InterpolatedNodes)Simulation.dataBasket.ECS.Space.Interior;

            double flux, min, max, slope;
            double stepSize = inm.StepSize();
            int numNodes_x, numNodes_y, numNodes_z;

            numNodes_x = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0);
            numNodes_y = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1);
            numNodes_z = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2);
            min = 5.0;
            max = 10.0;
            slope = (max - min) / inm.Extent(0);

            // Add CXCL13 uniform concentration
            Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "const", new double[] { (max + min) / 2.0 });

            MolecularPopulation mp = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
            flux = 1e6 * slope * mp.Molecule.DiffusionCoefficient;


            int n = Simulation.dataBasket.ECS.Sides["right"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Neumann = true;

            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { flux });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = sf;

            n = Simulation.dataBasket.ECS.Sides["left"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Neumann = true;

            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n].M;
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { -flux });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryFluxes[n] = sf;

        }

        private void FluxNaturalBC_Stepper(int nSteps, double dt)
        {
            double initQ, finalQ, relDiff;
            string output;

            initQ = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            nSteps = 50000;
            for (int i = 0; i < nSteps; i++)
            {
                sim.Step(dt);
                // Console.WriteLine(i);
            }

            finalQ = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.Integrate();
            relDiff = (initQ - finalQ) / initQ;
            output = dt.ToString("E2") + "\t" + Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
            output = output + "\t" + Simulation.dataBasket.ECS.Space.Interior.StepSize().ToString("F4");
            output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
            Console.WriteLine(output);

            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");

            // Check results

            InterpolatedNodes inm = (InterpolatedNodes)Simulation.dataBasket.ECS.Space.Interior;
            ScalarField conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc;
            MolecularPopulation mp = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
            double value, theor_value, min, max, slope, diff = 0;
            double stepSize = inm.StepSize();
            string result;
            int numNodes_x, numNodes_y, numNodes_z, m;

            numNodes_x = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0);
            numNodes_y = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1);
            numNodes_z = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2);
            min = conc.Value(inm.indexArrayToLocal(new int[3]{numNodes_x-1, numNodes_y/2, numNodes_z/2}));
            max = conc.Value(inm.indexArrayToLocal(new int[3]{0, numNodes_y/2, numNodes_z/2}));
            slope = (max - min) / inm.Extent(0);

            // Sum deviation from expected linear distribution at three locations

            // along midline of xy-plane 
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff);
            }

            // along midline of xy-plane at z=midpoint
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x + ((int)numNodes_z / 2) * numNodes_x * numNodes_y;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff);
            }

            // along midline of xy-plane at z=midpoint
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x + (numNodes_z - 2) * numNodes_x * numNodes_y;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff);
            }

            if (diff < 1e-3)
            {
                result = "GOOD";
            }
            else
            {
                result = "BAD";
            }

            Console.WriteLine("Sum of deviation from linear profile at three locations: " + diff + "\t Result: " + result);

        }


        public void DirichletBoundaryConditions()
        {
            FakeConfig.gridStep = 50;
            FakeConfig.numGridPts = new int[] { 21, 21, 21 };

            // Units: [length] = um, [time] = min, [MolWt] = kDa, [DiffCoeff] = um^2/min
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            //
            // Increase diffusion coefficient for CXCL13 by factor of 100 to speedup testing.
            //
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e5\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            //
            // Scenario: Diffusion dynamics test with Dirichlet boundary conditions
            //      Extracellular fluid with CXCL13 diffusing and no cells
            //      Specify CXCL13 concentration at the right and left faces of ECM

            //
            // Create Extracellular fluid
            // 

            //FakeConfig.numGridPts = new int[] { 3, 4, 5 };
            //FakeConfig.gridStep = 2;

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(null));

            Simulation.dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>(new ConstructorArgument("kernel", SimulationModule.kernel));

            //
            // Add all molecular populations
            //

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            Simulation.dataBasket.ECS.Space.AddMolecularPopulation(MolDict["CXCL13"], "const", new double[] { 5.0 });

            //
            // Add Dirichlet boundary conditions to the ECM
            // Unless otherwise specified, the ECM default boundary conditions are zero-flux
            //

            Manifold m;
            ScalarField sf;

            int n = Simulation.dataBasket.ECS.Sides["right"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Dirichlet = true;

            // m is the manifold corresponding to the right face
            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n].M;

            // Specify the value of the CXCL13 concentration to be enforced at the right face
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { 0.0 });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n] = sf;
            // Enforce that concentration to start
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.DirichletBC(sf, Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n]);

            n = Simulation.dataBasket.ECS.Sides["left"];
            Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n].Dirichlet = true;

            // m is the manifold corresponding to the left face
            m = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n].M;

            // Specify the value of the CXCL13 concentration to be enforce at the left face
            sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", m));
            sf.Initialize("const", new double[] { 10.0 });
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].NaturalBoundaryConcs[n] = sf;
            // Enforce that concentration to start
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.DirichletBC(sf, Simulation.dataBasket.ECS.Space.NaturalBoundaryTransforms[n]);


            // For debugging
            //double[] x = new double[3]; ;
            //double value;
            //InterpolatedNodes inm = (InterpolatedNodes)sim.ECS.Space.Interior;
            // double[] pos = (double[])sim.ECS.Space.NaturalBoundaryTransforms[n].Translation;

            //for (int i = 0; i < sim.ECS.Space.Interior.ArraySize; i++)
            //{
            //    //x[0] = (double)(inm.linearIndexToLocal(i)[0]);
            //    //x[1] = (double)(inm.linearIndexToLocal(i)[1]);
            //    //x[2] = (double)(inm.linearIndexToLocal(i)[2]);
            //    x = (double[])(inm.linearIndexToLocal(i));
            //    value = sim.ECS.Space.Populations["CXCL13"].Conc.Value(x);
            //    // Console.WriteLine(i + "\t(" + x[0] + ", " + x[1] + ", " + x[2] + ")\t " + value);
            //    if (value != 0.0)
            //    {
            //        Console.WriteLine(i + "\t(" + x[0] + ", " + x[1] + ", " + x[2] + ")\t " + value);
            //    }
            //}

        }

        private void Dirichlet_Stepper(int nSteps, double dt)
        {
            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

            nSteps = 5000;
            for (int i = 0; i < nSteps; i++)
            {
                sim.Step(dt);
                // Console.WriteLine(i);
            }

            Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");

            // Check results

            InterpolatedNodes inm = (InterpolatedNodes)Simulation.dataBasket.ECS.Space.Interior;
            ScalarField conc = Simulation.dataBasket.ECS.Space.Populations["CXCL13"].Conc;
            MolecularPopulation mp = Simulation.dataBasket.ECS.Space.Populations["CXCL13"];
            double value, theor_value, min, max, slope, diff = 0;
            double stepSize = inm.StepSize();
            string result;
            int numNodes_x, numNodes_y, numNodes_z, m;

            numNodes_x = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(0);
            numNodes_y = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(1);
            numNodes_z = Simulation.dataBasket.ECS.Space.Interior.NodesPerSide(2);
            min = mp.NaturalBoundaryConcs[Simulation.dataBasket.ECS.Sides["right"]].Value(new double[3] { 0.0, 0.0, 0.0 });
            max = mp.NaturalBoundaryConcs[Simulation.dataBasket.ECS.Sides["left"]].Value(new double[3] { 0.0, 0.0, 0.0 });
            slope = (max - min) / inm.Extent(0);

            // Sum deviation from expected linear distribution at three locations

            // along midline of xy-plane 
            for (int i = 0; i < numNodes_x; i++ )
            {
                m = i + ((int)numNodes_y/2)*numNodes_x;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff); 
            }

            // along midline of xy-plane at z=midpoint
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x + ((int)numNodes_z / 2) * numNodes_x * numNodes_y;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff); 
            }

            // along midline of xy-plane at z=midpoint
            for (int i = 0; i < numNodes_x; i++)
            {
                m = i + ((int)numNodes_y / 2) * numNodes_x + (numNodes_z - 2) * numNodes_x * numNodes_y;
                value = conc.Value(inm.linearIndexToLocal(m));
                theor_value = max - slope * i * stepSize;
                diff = diff + Math.Abs((double)(theor_value - value));
                Console.WriteLine(i + ": " + theor_value + "\t" + value + "\t" + diff);
            }

            if (diff < 1e-3)
            {
                result = "GOOD";
            }
            else
            {
                result = "BAD";
            }

            Console.WriteLine("Sum of differences along gradient direction at three locations: " + diff + "\t Result: " + result);

         }
    }

}

