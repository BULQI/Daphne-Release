using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NativeDaphne;
using MathNet.Numerics.Random;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Daphne
{
    public class CellManager : IDynamic
    {
        private Dictionary<int, double[]> deadDict = null;
        private int[] tempDeadKeys = null;
        public DistributedParameter Phagocytosis;

        public static Nt_CellManager nt_cellManager;

        private List<Thread> threadList;
        private List<CellStepThreadArg> threadArgs;
        private int numWorkerThreads;
        private int numWorkingThreads;
        private int numActiveThreads;
        private int NumItemPerThread;

        //private object jobDoneEvent;
        public bool threadDataInitialized;

        class CellStepThreadArg
        {
            public double dt = 0;
            public List<Cell> CellList = new List<Cell>();

            public int threadId;
            public readonly object JobReadySignal = new object();
            public int blockFlag = 1;
        }

        public CellManager()
        {
            deadDict = new Dictionary<int, double[]>();

            nt_cellManager = new Nt_CellManager();
            //creating threads
            threadList = new List<Thread>();
            threadArgs = new List<CellStepThreadArg>();
            //these are the number threads created and available to use.
            numWorkerThreads = Environment.ProcessorCount - 4; //half if 8
            //jobDoneEvent = new object();
            for (int i = 0; i < numWorkerThreads; i++)
            {
                Thread th = new Thread(this.run_cell_step);
                th.IsBackground = true;
                CellStepThreadArg cta = new CellStepThreadArg();
                cta.threadId = i;
                threadArgs.Add(cta);
                th.Start(cta);
                threadList.Add(th);
            }
            //extra list for current thread
            threadArgs.Add(new CellStepThreadArg());
            threadDataInitialized = false;
            numWorkingThreads = 0;
            NumItemPerThread = 0;
        }

        public void initializeThreadData(double dt)
        {
            int numCells = SimulationBase.dataBasket.Cells.Count;
            numWorkingThreads = numWorkerThreads;
            NumItemPerThread = numCells / (numWorkerThreads + 2);
            if (NumItemPerThread < 20)
            {
                NumItemPerThread = 20;
                numWorkingThreads = numCells / 20 - 2;
                if (numWorkingThreads < 0) numWorkingThreads = 0;
            }

            List<AutoResetEvent> activeEventList = new List<AutoResetEvent>();
            //adding cells.
            using (var cell_enumerator = SimulationBase.dataBasket.Cells.GetEnumerator())
            {
                for (int i = 0; i < numWorkingThreads; i++)
                {
                    CellStepThreadArg arg = threadArgs[i];
                    arg.dt = dt;
                    List<Cell> cell_list = arg.CellList;
                    cell_list.Clear();
                    while (cell_list.Count < NumItemPerThread)
                    {
                        if (!cell_enumerator.MoveNext()) break;
                        cell_list.Add(cell_enumerator.Current.Value);
                    }
                }
                CellStepThreadArg last_arg = threadArgs[numWorkingThreads];
                while (cell_enumerator.MoveNext())
                {
                    last_arg.CellList.Add(cell_enumerator.Current.Value);
                }
            }
            threadDataInitialized = true;
        }

        public static int iteration_count = 0;

        void run_cell_step(object threadContext)
        {
            CellStepThreadArg arg = threadContext as CellStepThreadArg;
            Object JobReadySignal = arg.JobReadySignal;
            List<Cell> tasklist = arg.CellList;
            while (true)
            {
                lock (JobReadySignal)
                {
                    while (arg.blockFlag == 1)
                    {
                        Monitor.Wait(JobReadySignal);
                    }
                    Interlocked.Exchange(ref arg.blockFlag, 1);
                }
                for (int i = 0; i < tasklist.Count; i++)
                {
                    tasklist[i].Step(arg.dt);
                }
                if (Interlocked.Decrement(ref numActiveThreads) == 0)
                {
                    //lock (jobDoneEvent)
                    //{
                    //    Monitor.Pulse(jobDoneEvent);
                    //}
                }
            }
        }

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            iteration_count++;
            //Debug.WriteLine("*******************interation = {0} *********************", iteration_count);
            //testing cytosol reaction using native methods.
            bool use_native = true;
            if (use_native)
            {
                nt_cellManager.step(dt);
            }

            if (threadDataInitialized == false)
            {
                initializeThreadData(dt);
            }

            //start job
            Interlocked.Exchange(ref numActiveThreads, numWorkingThreads);
            for (int i = 0; i < numWorkingThreads; i++)
            {
                CellStepThreadArg arg = threadArgs[i];
                lock (arg.JobReadySignal)
                {
                    Interlocked.Exchange(ref arg.blockFlag, 0);
                    Monitor.Pulse(arg.JobReadySignal);
                }
            }

            List<Cell> cellList = threadArgs[numWorkingThreads].CellList;
            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].Step(dt);
            }

            //wait for finish
            if (numWorkingThreads > 0)
            {
                while (Interlocked.CompareExchange(ref numActiveThreads, 1, 0) != 0) ;
            }

            //wait for finish

            //lock (jobDoneEvent)
            //{
            //    if (numActiveThreads != 0)
            //    {
            //        Monitor.Wait(jobDoneEvent);
            //    }
            //}

            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                // cell takes a step
                //if (kvp.Value.Alive == true)
                //{
                //    kvp.Value.Step(dt);
                //}

                // still alive and motile
                //if (kvp.Value.Alive == true && kvp.Value.IsMotile == true && kvp.Value.Exiting == false)
                //{
                    //if (!use_native)
                    //{
                    //    if (kvp.Value.IsChemotactic)
                    //    {
                    //        // For TinySphere cytosol, the force is determined by the gradient of the driver molecule at position (0,0,0).
                    //        // add the chemotactic force (accumulate it into the force variable)
                    //        kvp.Value.addForce(kvp.Value.Force(new double[3] { 0.0, 0.0, 0.0 }));
                    //    }
                    //    // apply the boundary force
                    //    kvp.Value.BoundaryForce();
                    //    // apply stochastic force
                    //    if (kvp.Value.IsStochastic)
                    //    {
                    //        kvp.Value.addForce(kvp.Value.StochLocomotor.Force(dt));
                    //    }

                    //    // A simple implementation of movement. For testing.
                    //    for (int i = 0; i < kvp.Value.SpatialState.X.Length; i++)
                    //    {
                    //        kvp.Value.SpatialState.X[i] += kvp.Value.SpatialState.V[i] * dt;
                    //        kvp.Value.SpatialState.V[i] += (-kvp.Value.DragCoefficient * kvp.Value.SpatialState.V[i] + kvp.Value.SpatialState.F[i]) * dt;
                    //    }
                    //}
                    // enforce boundary condition
                    //kvp.Value.EnforceBC();
                //}

                ////this is after the step is done
                if (iteration_count < 0) //> 0 && iteration_count % 10000 == 0) //> 0 && kvp.Value.Cell_id == 40)
                {
                    Debug.WriteLine("\n----membrane----");
                    foreach (var item in kvp.Value.PlasmaMembrane.Populations)
                    {
                        var tmp = item.Value.Conc;
                        Debug.WriteLine("it={0}\tconc[0]= {1}\tconc[1]= {2}\tconc[2]={3}\t{4}",
                            iteration_count, tmp.array[0], tmp.array[1], tmp.array[2], item.Value.Molecule.Name);
                    }

                    Debug.WriteLine("----Cytosol----");
                    foreach (var item in kvp.Value.Cytosol.Populations)
                    {
                        var tmp = item.Value.Conc;
                        Debug.WriteLine("it={0}\tconc[0]= {1}\tconc[1]= {2}\tconc[2]={3}\t{4}",
                            iteration_count, tmp.array[0], tmp.array[1], tmp.array[2], item.Value.Molecule.Name);
                    }

                }

                // if the cell  moved out of bounds schedule its removal
                if (kvp.Value.Exiting == true)
                {
                    if (removalList == null)
                    {
                        removalList = new List<int>();
                    }
                    removalList.Add(kvp.Value.Cell_id);
                }

                // if the cell died schedule its (stochastic) removal
                if (kvp.Value.Alive == false)
                {
                    if (!deadDict.ContainsKey(kvp.Value.Cell_id))
                    {
                        // start clock at 0 and sample the distribution for the time of removal
                        deadDict.Add(kvp.Value.Cell_id, new double[] { 0.0, Phagocytosis.Sample() });
                    }
                }

                // cell division
                if (kvp.Value.Cytokinetic == true)
                {
                    // divide the cell, return daughter
                    Cell c = kvp.Value.Divide();

                    if (daughterList == null)
                    {
                        daughterList = new List<Cell>();
                    }
                    daughterList.Add(c);

                    SimulationBase.dataBasket.DivisionEvent(kvp.Value.Cell_id, kvp.Value.Population_id, c.Cell_id);
                }
            }

            // process removal list
            if (removalList != null)
            {
                foreach (int key in removalList)
                {
                    SimulationBase.dataBasket.ExitEvent(key);
                    SimulationBase.dataBasket.RemoveCell(key);
                    //nt_cellManager.RemoveCell(key);
                }
            }

            // process death list
            if (deadDict != null)
            {
                tempDeadKeys = deadDict.Keys.ToArray<int>();
                foreach (int key in tempDeadKeys)
                {
                    // increment elapsed time since death
                    double[] d = deadDict[key];
                    d[0] += dt;

                    if (d[0] >= d[1])
                    {
                        SimulationBase.dataBasket.DeathEvent(key);
                        SimulationBase.dataBasket.RemoveCell(key);
                        deadDict.Remove(key);
                    }
                }
            }

            // process daughter list
            if (daughterList != null)
            {
                foreach (Cell c in daughterList)
                {
                    // add the cell
                    SimulationBase.AddCell(c);
                    // add the cell's membrane to the ecs boundary
                    if (SimulationBase.dataBasket.Environment is ECSEnvironment)
                    {
                        ((ECSEnvironment)SimulationBase.dataBasket.Environment).AddBoundaryManifold(c.PlasmaMembrane.Interior);
                    }
                }
            }
        }

        /// <summary>
        /// zero all cell forces
        /// </summary>/
        public void ResetCellForces()
        {
            foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
            {
                c.resetForce();
            }
        }


        internal void InitializeNtCellManger()
        {
            double extend1 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0);
            double extend2 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1);
            double extend3 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2);
            bool boundary_force_flag = SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == false;

            nt_cellManager.SetEnvironmentExtents(extend1, extend2, extend3, boundary_force_flag, Pair.Phi1);
            int seed = SimulationBase.ProtocolHandle.sim_params.globalRandomSeed;
            nt_cellManager.InitializeNormalDistributionSampler(0.0, 1.0, seed);
        }

        /// <summary>
        /// add cell's reaction to nt_CellManager.
        /// </summary>
        /// <param name="c"></param>
        internal void AddNtCell(Cell c)
        {

            Nt_Cell ntc = c.ntCell;
            ntc.Population_id = c.Population_id;
            ntc.Membrane_id = c.PlasmaMembrane.Interior.Id;
            ntc.isMotile = c.IsMotile;
            ntc.IsChemotactic = c.IsChemotactic;
            ntc.IsStochastic = c.IsStochastic;
            ntc.cytokinetic = c.Cytokinetic;
            ntc.TransductionConstant = c.Locomotor.TransductionConstant;
            ntc.DragCoefficient = c.DragCoefficient;
            ntc.Sigma = (c.StochLocomotor != null) ? c.StochLocomotor.Sigma : 0;
            nt_cellManager.AddCell(ntc);

            //genes
            Dictionary<string, Nt_Gene> gene_dict = new Dictionary<string, Nt_Gene>();
            foreach (var item in c.Genes)
            {
                Gene gene = item.Value;
                Nt_Gene nt_gene = new Nt_Gene(c.Cell_id, gene.Name, gene.CopyNumber, gene.ActivationLevel);
                gene.nt_gene = nt_gene;
                gene_dict.Add(gene.Name, nt_gene);
                nt_cellManager.AddGene(c.Population_id, nt_gene);
            }

            Dictionary<string, Nt_MolecularPopulation> cytosol_nt_mp = new Dictionary<string, Nt_MolecularPopulation>();
            foreach (var item in c.Cytosol.Populations)
            {
                MolecularPopulation mp = item.Value;
                double diffCoeff = mp.IsDiffusing ? mp.Molecule.DiffusionCoefficient : 0.0;

                
                Nt_Darray bflux = null;
                if (mp.BoundaryFluxes.Count > 0)
                {
                    //cytosol should always have a boundary of membrane?
                    var tmp = mp.BoundaryFluxes.First().Value;
                    bflux = tmp.array;
                }

                Nt_Darray bconc = null;
                if (mp.BoundaryConcs.Count > 0)
                {
                    var tmp = mp.BoundaryConcs.First().Value;
                    bconc = tmp.array;
                }
                var cmp = new Nt_CytosolMolecularPopulation(c.Cell_id, c.Radius, item.Key, diffCoeff, mp.Conc.array, bflux, bconc);
                cmp.Name = item.Value.Molecule.Name; //exist for debugging
                mp.nt_instance = cmp;
                nt_cellManager.AddMolecularPopulation(c.Population_id, -1, true, cmp);
                cytosol_nt_mp.Add(item.Key, cmp);
            }
            if (c.Locomotor != null)
            {
                string driver_key = c.Locomotor.Driver.MoleculeKey;
                ntc.Driver = cytosol_nt_mp[driver_key];
            }

            Dictionary<string, Nt_MolecularPopulation> membrane_nt_mp = new Dictionary<string, Nt_MolecularPopulation>();
            foreach (var item in c.PlasmaMembrane.Populations)
            {
                MolecularPopulation mp = item.Value;
                double diffCoeff = mp.IsDiffusing ? mp.Molecule.DiffusionCoefficient : 0.0;

                var cmp = new Nt_MembraneMolecularPopulation(c.Cell_id, c.Radius, item.Key, diffCoeff, mp.Conc.array);
                cmp.Name = item.Value.Molecule.Name;
                mp.nt_instance = cmp;
                nt_cellManager.AddMolecularPopulation(c.Population_id, c.PlasmaMembrane.Interior.Id, false, cmp);
                membrane_nt_mp.Add(item.Key, cmp);
            }

            //add reactions
            for (int i = 0; i< c.Cytosol.BulkReactions.Count; i++)
            {
                Reaction r = c.Cytosol.BulkReactions[i];
                if (r is Transformation)
                {
                    Transformation tr = r as Transformation;
                    Nt_Transformation nt_rxn = new Nt_Transformation(c.Cell_id, tr.RateConstant);
                    nt_rxn.isBulkReaction = true;
                    nt_rxn.reaction_index = i;
                    //checking...
                    string reactant_guid = tr.reactant.MoleculeKey;
                    if (cytosol_nt_mp.ContainsKey(reactant_guid) == false)
                    {
                        throw new Exception("reactnat molpop not found");
                    }
                    nt_rxn.reactant = cytosol_nt_mp[reactant_guid];
                    nt_rxn.product = cytosol_nt_mp[tr.product.MoleculeKey];
                    nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                }
                else if (r is Transcription)
                {
                    Transcription tr = r as Transcription;
                    Nt_Transcription nt_rxn = new Nt_Transcription(c.Cell_id, tr.RateConstant);
                    nt_rxn.isBulkReaction = true;
                    nt_rxn.reaction_index = i;
                    nt_rxn.gene = gene_dict[tr.gene.Name];
                    nt_rxn.product = cytosol_nt_mp[tr.product.MoleculeKey];
                    nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                }
                else if (r is Annihilation)
                {
                    Annihilation ah = r as Annihilation;
                    Nt_Annihilation nt_rxn = new Nt_Annihilation(c.Cell_id, ah.RateConstant);
                    nt_rxn.isBulkReaction = true;
                    nt_rxn.reaction_index = i;
                    nt_rxn.reactant = cytosol_nt_mp[ah.reactant.MoleculeKey];
                    nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            foreach (List<Reaction> rlist in c.Cytosol.BoundaryReactions.Values)
            {

                for (int i = 0; i< rlist.Count; i++)
                {
                    Reaction r = rlist[i];
                    if (r is CatalyzedBoundaryActivation)
                    {
                        CatalyzedBoundaryActivation cba = r as CatalyzedBoundaryActivation;
                        Nt_CatalyzedBoundaryActivation nt_rxn = new Nt_CatalyzedBoundaryActivation(c.Cell_id, cba.RateConstant);
                        nt_rxn.isBulkReaction = false;
                        nt_rxn.reaction_index = i;
                        nt_rxn.bulk = cytosol_nt_mp[cba.bulk.MoleculeKey];
                        nt_rxn.bulkActivated = cytosol_nt_mp[cba.bulkActivated.MoleculeKey];
                        nt_rxn.receptor = membrane_nt_mp[cba.receptor.MoleculeKey];
                        nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                    }
                    else if (r is BoundaryTransportTo)
                    {
                        BoundaryTransportTo btt = r as BoundaryTransportTo;
                        Nt_BoundaryTransportTo nt_rxn = new Nt_BoundaryTransportTo(c.Cell_id, btt.RateConstant);
                        nt_rxn.isBulkReaction = false;
                        nt_rxn.reaction_index = i;
                        nt_rxn.bulk = cytosol_nt_mp[btt.bulk.MoleculeKey];
                        nt_rxn.membrane = membrane_nt_mp[btt.membrane.MoleculeKey];
                        nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                    }
                    else if (r is BoundaryTransportFrom)
                    {
                        BoundaryTransportFrom btf = r as BoundaryTransportFrom;
                        Nt_BoundaryTransportFrom nt_rxn = new Nt_BoundaryTransportFrom(c.Cell_id, btf.RateConstant);
                        nt_rxn.isBulkReaction = false;
                        nt_rxn.reaction_index = i;
                        nt_rxn.bulk = cytosol_nt_mp[btf.bulk.MoleculeKey];
                        nt_rxn.membrane = membrane_nt_mp[btf.membrane.MoleculeKey];
                        nt_cellManager.AddReaction(c.Population_id, true, nt_rxn);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
