using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Daphne;
using System.IO;
using Newtonsoft.Json;

namespace testDaphne
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int nSteps;
        private double dt;
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
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            MolDict = MoleculeBuilder.Go(molSpec);

            config = new ReactionsConfigurator();
            config.deserialize("ReacSpecFile1.xml");
            config.TemplReacType(config.content.listOfReactions);

            ////
            //// Scenario: Diffusion dynamics test
            ////      extracellular fluid with CXCL13 and no cells
            //// The diffusion dynamics with zero-flux boundary conditions seem to be working, in the sense that 
            //// a heterogeneous distribution of material tends to a homogeneous distribution and there is no 
            //// significant loss of material as long as the following condition is met:
            ////    (dx^2) >> D*dt, where dx is the StepSize of the grid and D is the diffusion coefficient
            ////

            //// create the simulation
            //sim = new Simulation();

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



        private Simulation JsonLoadScenario()
        {
            Simulation sim = new Simulation();
            Dictionary<int, Cell> cells;
            MolecularPopulation mp;
            Molecule mol;

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
            
            return sim;

        }

        private void JsonSaveScenario(Simulation sim)
        {
             MolecularPopulation mp;
            Molecule mol;

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
                mp = sim.ECS.Populations["CXCL13"];
                jserializer.Serialize(writer, mp);

                //// This works
                // mol = mp.Molecule;
                // jserializer.Serialize(writer, mol);
            }

            MessageBox.Show("Json output to " + "json_output.txt succeeded.");

        }

        private void step(int nSteps, double dt)
        {
            this.dt = dt;
            {
                for (int i = 0; i < nSteps; i++)
                {
                    sim.ECS.Step(dt);

                    sim.CMGR.Step(dt);

                    //foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                    //{
                    //    kvp.Value.Step(dt);
                    //}

                    foreach (KeyValuePair<int,Cell> kvp in sim.Cells)
                    {
                       sim.ECS.Interior.Boundaries[kvp.Value.PlasmaMembrane.Interior.Id].position = kvp.Value.State.X;
                    }
                }
            }
        }

        private void go()
        {
            double T = 100;   // minutes
            double dt = 0.001;
            int nSteps = (int)(T / dt);

            // initialize();
            // step();

            //DiffusionScenario();
            //TestStepperDiffusion(nSteps, dt);

            //LigandReceptorScenario();
            //TestStepperLigandReceptor(nSteps, dt);

            DriverLocomotionScenario();
            TestStepperLocomotion(nSteps, dt);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            go();
        }

        private void TestStepperDiffusion(int nSteps, double dt)
        {
            this.dt = dt;
            {
                double initQ, finalQ, relDiff;
                string output;

                initQ = sim.ECS.Populations["CXCL13"].Integrate();
                //sim.ECS.Populations["CXCL13"].Conc.WriteToFile("CXCL13 initial.txt");

                for (int i = 0; i < nSteps; i++)
                {
                    sim.ECS.Step(dt);
                    sim.CMGR.Step(dt);

                    foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                    {
                        sim.ECS.Interior.Boundaries[kvp.Value.PlasmaMembrane.Interior.Id].position = kvp.Value.State.X;
                    }
                 }

                finalQ = sim.ECS.Populations["CXCL13"].Integrate();
                relDiff = (initQ - finalQ) / initQ;
                output = dt.ToString("E2") + "\t" + sim.ECS.Populations["CXCL13"].Molecule.DiffusionCoefficient.ToString("E2");
                output = output + "\t" + sim.ECS.Interior.StepSize[0].ToString("F4");
                output = output + "\t" + initQ.ToString("F2") + "\t" + finalQ.ToString("F2") + "\t" + relDiff.ToString("E2");
                Console.WriteLine(output);

                //sim.ECS.Populations["CXCL13"].Conc.WriteToFile("CXCL13 final.txt");
            }
        }

        private void TestStepperLigandReceptor(int nSteps, double dt)
        {
            double[] receptorConc = new double[nSteps];
            double[] ligandBoundaryConc = new double[nSteps];
            double[] complexConc = new double[nSteps];

            string output;
            string filename = "LigandReceptorComplex.txt";

            using (StreamWriter writer = File.CreateText(filename))
            {
                for (int i = 0; i < nSteps; i++)
                {
                    receptorConc[i] = sim.Cells[0].PlasmaMembrane.Populations["CXCR5"].Conc[0];
                    complexConc[i] = sim.Cells[0].PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc[0];
                    ligandBoundaryConc[i] = sim.ECS.Populations["CXCL13"].BoundaryConcs[sim.Cells[0].PlasmaMembrane.Interior.Id][0];

                    sim.ECS.Step(dt);
                    sim.CMGR.Step(dt);

                    foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                    {
                        sim.ECS.Interior.Boundaries[kvp.Value.PlasmaMembrane.Interior.Id].position = kvp.Value.State.X;
                        // XHist[i] = sim.CMGR.States[kvp.Value].X;
                    }

                    output = i*dt + "\t" + ligandBoundaryConc[i] + "\t" + receptorConc[i] + "\t" + complexConc[i];
                    //Console.WriteLine(output);
                    writer.WriteLine( output );

                }
            }
        }

        private void TestStepperLocomotion(int nSteps, double dt)
        {
            this.dt = dt;
            {
                double[] receptorConc = new double[nSteps];
                double[] ligandBoundaryConc = new double[nSteps];
                double[] complexConc = new double[nSteps];
                double[] driverConc = new double[nSteps];
                double[][] driverLoc = new double[nSteps][];

                string output;
                string filename = "DriverDynamics.txt";

                using (StreamWriter writer = File.CreateText(filename))
                {
                    for (int i = 0; i < nSteps; i++)
                    {
                        receptorConc[i] = sim.Cells[0].PlasmaMembrane.Populations["CXCR5"].Conc[0];
                        complexConc[i] = sim.Cells[0].PlasmaMembrane.Populations["CXCR5:CXCL13"].Conc[0];
                        driverConc[i] = sim.Cells[0].Cytosol.Populations["driver"].Conc[0];
                        ligandBoundaryConc[i] = sim.ECS.Populations["CXCL13"].BoundaryConcs[sim.Cells[0].PlasmaMembrane.Interior.Id][0];
                        driverLoc[i] =  sim.Cells[0].State.X ;

                        sim.ECS.Step(dt);
                        sim.CMGR.Step(dt);

                        foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
                        {
                            sim.ECS.Interior.Boundaries[kvp.Value.PlasmaMembrane.Interior.Id].position = kvp.Value.State.X;
                            // XHist[i] = sim.CMGR.States[kvp.Value].X;
                        }

                        output = i * dt + "\t" + ligandBoundaryConc[i] + "\t" + receptorConc[i] + "\t" + complexConc[i]
                                    + "\t" + driverConc[i] + "\t" + driverLoc[i][0] + "\t" + driverLoc[i][1] + "\t" + driverLoc[i][2];
                        //Console.WriteLine(output);
                        writer.WriteLine(output);

                    }
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

            // create the simulation
            sim = new Simulation();

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create Extracellular fluid
            // 

            int[] numGridPts = { 21, 21, 21 };
            // int[] numGridPts = { 10, 10, 10 };
            // spatial extent in each dimension
            double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };
            //Compartment XCellSpace = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));
            //sim.ECS = XCellSpace;
            sim.ECS = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));

            //
            // Create Cells
            //

            int numCells = 1;
            double cellRadius = 5.0;
            double[] cellPos = new double[3];
            double[] veloc = new double[3] { 0.0, 0.0, 0.0 };

            // One cell
            cellPos[0] = sim.ECS.Interior.Extents[0] / 4.0;
            cellPos[1] = sim.ECS.Interior.Extents[1] / 2.0;
            cellPos[2] = sim.ECS.Interior.Extents[2] / 2.0;
            sim.AddCell(cellPos, veloc, cellRadius);

            // Add the cell plasma membrane as an embedded boundary manifold in the extracellular compartment
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                TranslEmbedding cellMembraneEmbed = new TranslEmbedding(kvp.Value.PlasmaMembrane.Interior, (BoundedRectangularPrism)sim.ECS.Interior, new int[1] { 0 }, kvp.Value.State.X);
                sim.ECS.Interior.Boundaries.Add(kvp.Value.PlasmaMembrane.Interior.Id, cellMembraneEmbed);
            }

            //
            // Add all molecular populations
            //


            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double maxConc = 2 * (3.0) * (1e-6) * (1e-18) * (6.022e23);

            double[] sigma = { sim.ECS.Interior.Extents[0] / 5.0, 
                               sim.ECS.Interior.Extents[1] / 5.0, 
                               sim.ECS.Interior.Extents[2] / 5.0 };

            double[] center = new double[sim.ECS.Interior.Dim];
            center[0] = sim.ECS.Interior.Extents[0] / 2.0;
            center[1] = sim.ECS.Interior.Extents[1] / 2.0;
            center[2] = sim.ECS.Interior.Extents[2] / 2.0;

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            ScalarField gsf = new DiscreteScalarField(sim.ECS.Interior, new GaussianFieldInitializer(center, sigma, maxConc));

            sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], gsf);
            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            sim.ECS.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], 125.0);
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], 130.0);
            }

            // Add Cytosol molecular populations
            // Start with a non-zero (activated) driver concentration and global gradient
            double[] initGrad = new double[3] { 0, 2.0, 2.0 };
            double initConc = 250;
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.Cytosol.AddMolecularPopulation(MolDict["driver"], initConc, initGrad);
            }

            //
            // Add reactions
            //

            MolecularPopulation receptor, ligand, complex;
            double k1plus = 2.0, k1minus = 1;
            MolecularPopulation driver;
            double  k2plus = 1.0, 
                    k2minus = 10.0,
                    transductionConstant = 1e4,
                    driverTotal = 500;

            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                // add receptor ligand boundary association and dissociation
                // R+L-> C
                // C -> R+L
                receptor = kvp.Value.PlasmaMembrane.Populations["CXCR5"];
                ligand = sim.ECS.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                // QUESTION: Does it matter to which manifold we assign the boundary reactions?
                //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

                // Choose false to have the driver dynamics, but no movement
                kvp.Value.IsMotile = true;

                // Add driver dynamics for locomotion
                driver = kvp.Value.Cytosol.Populations["driver"];
                kvp.Value.Cytosol.reactions.Add(new CatalyzedConservedBoundaryActivation(driver, complex, k2plus, driverTotal));
                kvp.Value.Cytosol.reactions.Add(new BoundaryConservedDeactivation(driver, complex, k2minus));
                kvp.Value.Cytosol.reactions.Add(new DriverDiffusion(driver));

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

            // create the simulation
            sim = new Simulation();

            // NOTE:The order in which compartments and molecular populations are instantiated matters
            // because of the BoundaryConcs and Fluxes in MolecularPopulations
            // Assign all compartments first, then molecular populations

            //
            // Create Extracellular fluid
            // 

            int[] numGridPts = { 21, 21, 21 };
            // int[] numGridPts = { 10, 10, 10 };
            // spatial extent in each dimension
            double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };
            //Compartment XCellSpace = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));
            //sim.ECS = XCellSpace;
            sim.ECS = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));

            //
            // Create Cells
            //

            int numCells = 1;
            double cellRadius = 5.0;
            double[] cellPos = new double[sim.ECS.Interior.Dim];

            // One cell
            cellPos[0] = sim.ECS.Interior.Extents[0] / 3.0;
            cellPos[1] = sim.ECS.Interior.Extents[1] / 3.0;
            cellPos[2] = sim.ECS.Interior.Extents[2] / 3.0;
            sim.AddCell(cellPos, new double[] { 0, 0, 0 }, cellRadius);

            // Add the cell plasma membrane as an embedded boundary manifold in the extracellular compartment
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                TranslEmbedding cellMembraneEmbed = new TranslEmbedding(kvp.Value.PlasmaMembrane.Interior, (BoundedRectangularPrism)sim.ECS.Interior, new int[1] { 0 }, kvp.Value.State.X);
                sim.ECS.Interior.Boundaries.Add(kvp.Value.PlasmaMembrane.Interior.Id, cellMembraneEmbed);
            }

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double maxConc = 2 * (3.0) * (1e-6) * (1e-18) * (6.022e23);

            double[] sigma = { sim.ECS.Interior.Extents[0] / 5.0, 
                               sim.ECS.Interior.Extents[1] / 5.0, 
                               sim.ECS.Interior.Extents[2] / 5.0 };

            double[] center = new double[sim.ECS.Interior.Dim];
            center[0] = sim.ECS.Interior.Extents[0] / 2.0;
            center[1] = sim.ECS.Interior.Extents[1] / 2.0;
            center[2] = sim.ECS.Interior.Extents[2] / 2.0;

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            ScalarField gsf = new DiscreteScalarField(sim.ECS.Interior, new GaussianFieldInitializer(center, sigma, maxConc));

            sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], gsf);
            //sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], 1.0);
            sim.ECS.Populations["CXCL13"].IsDiffusing = false;

            // Add PlasmaMembrane molecular populations
            // Approximately, 20,000 CXCR5 receptors per cell
            foreach (KeyValuePair<int, Cell> kvp in sim.Cells)
            {
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5"], 255.0);
                kvp.Value.PlasmaMembrane.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], 0.0);
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
                ligand = sim.ECS.Populations["CXCL13"];
                complex = kvp.Value.PlasmaMembrane.Populations["CXCR5:CXCL13"];

                // QUESTION: Does it matter to which manifold we assign the boundary reactions?
                //kvp.Value.PlasmaMembrane.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                //kvp.Value.PlasmaMembrane.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));
                // sim.ECS.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.reactions.Add(new TinyBoundaryAssociation(receptor, ligand, complex, k1plus));
                sim.ECS.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, k1minus));

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

            // create the simulation
            sim = new Simulation();

            //
            // Create Extracellular fluid
            // 

            int[] numGridPts = { 21, 21, 21 };

            // spatial extent in each dimension
            double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };

            sim.ECS = new Compartment(new BoundedRectangularPrism(numGridPts, XCellExtent));

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3
            double maxConc = 2 * (3.0) * (1e-6) * (1e-18) * (6.022e23);

            double[] sigma = { sim.ECS.Interior.Extents[0] / 5.0, 
                               sim.ECS.Interior.Extents[1] / 5.0, 
                               sim.ECS.Interior.Extents[2] / 5.0 };

            double[] center = new double[sim.ECS.Interior.Dim];
            center[0] = sim.ECS.Interior.Extents[0] / 2.0;
            center[1] = sim.ECS.Interior.Extents[1] / 2.0;
            center[2] = sim.ECS.Interior.Extents[2] / 2.0;

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            ScalarField gsf = new DiscreteScalarField(sim.ECS.Interior, new GaussianFieldInitializer(center, sigma, maxConc));

            sim.ECS.AddMolecularPopulation(MolDict["CXCL13"], gsf);
        }

    }

}

