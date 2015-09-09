using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NativeDaphne;
using Gene = NativeDaphne.Nt_Gene;
using System.Diagnostics;
using System.Numerics;

namespace Daphne
{
    public class CellManager : Nt_CellManager, IDynamic
    {
        private Dictionary<int, double[]> deadDict = null;
        public Dictionary<int, double[]> DeadDict
        {
            get
            {
                return deadDict;
            }

            set
            {
                deadDict = value;
            }
        }
        public DistributedParameter Phagocytosis;

        Dictionary<BigInteger, int> item_to_keep;

        public CellManager()
        {
            deadDict = new Dictionary<int, double[]>();
        }

        /// <summary>
        /// have all cells take a step forward according to the burn-in update x += mu * f * dt
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="mu"></param>
        public void Burn_inStep(double dt, double mu)
        {
            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                for (int i = 0; i < kvp.Value.SpatialState.X.Length; i++)
                {
                    kvp.Value.SpatialState.X[i] += mu * kvp.Value.SpatialState.F[i] * dt;
                }
                kvp.Value.updateGridIndex();
            }
        }

        //for debug
        public static int iteration_count = 0;

        public void Step(double dt)
        {

            List<int> removalList = null;
            List<Cell> daughterList = null;

            iteration_count++;

            //steps through cell populations - it is ONLY handling reactions for now.
            foreach (CellsPopulation cellpop in SimulationBase.dataBasket.Populations.Values)
            {
                //currently all reactions are handled in population level.
                cellpop.step(dt);
            }

            foreach (Cell cell in SimulationBase.dataBasket.Cells.Values)
            {
                // cell takes a step - only handling cells trans
                if (cell.Alive == true)
                {
                    cell.Step(dt);
                }

                // for debugging
                if (false)
                {
                    Debug.WriteLine("===============cell id = {0}=================", cell.Cell_id);
                    Debug.WriteLine("----membrane----");
                    foreach (var item in cell.PlasmaMembrane.Populations)
                    {
                        var tmp = item.Value.Conc;
                        Debug.WriteLine("it={0}\tconc[0]= {1}\tconc[1]= {2}\tconc[2]={3}\t{4}",
                            iteration_count, tmp.darray[0], tmp.darray[1], tmp.darray[2], item.Value.Molecule.Name);
                    }

                    Debug.WriteLine("----Cytosol----");
                    foreach (var item in cell.Cytosol.Populations)
                    {
                        var tmp = item.Value.Conc;
                        Debug.WriteLine("it={0}\tconc[0]= {1}\tconc[1]= {2}\tconc[2]={3}\t{4}",
                            iteration_count, tmp.darray[0], tmp.darray[1], tmp.darray[2], item.Value.Molecule.Name);
                    }

                    Debug.WriteLine("---location---");
                    Debug.WriteLine("positon = {0} {1} {2}", cell.SpatialState.X[0], cell.SpatialState.X[1], cell.SpatialState.X[2]);

                }

                // if the cell  moved out of bounds schedule its removal
                if (cell.Exiting == true)
                {
                    if (removalList == null)
                    {
                        removalList = new List<int>();
                    }
                    removalList.Add(cell.Cell_id);
                }

                // if the cell died schedule its (stochastic) removal
                if (cell.Alive == false)
                {
                    if (!deadDict.ContainsKey(cell.Cell_id))
                    {
                        // start clock at 0 and sample the distribution for the time of removal
                        deadDict.Add(cell.Cell_id, new double[] { 0.0, Phagocytosis.Sample() });
                        //remove the cell's chemistry and all its associated boundaries
                        SimulationBase.dataBasket.RemoveCell(cell.Cell_id, false);
                    }
                }

                // cell division
                if (cell.Cytokinetic == true)
                {
                    // divide the cell, return daughter
                    Cell c = cell.Divide();
                    if (daughterList == null)
                    {
                        daughterList = new List<Cell>();
                    }
                    daughterList.Add(c);

                    SimulationBase.dataBasket.DivisionEvent(cell, c);
                }
            }

            // process removal list
            if (removalList != null)
            {
                foreach (int key in removalList)
                {
                    SimulationBase.dataBasket.ExitEvent(key);
                    SimulationBase.dataBasket.RemoveCell(key);
                }
            }

            // process death list
            if (deadDict != null)
            {
                foreach (int key in deadDict.Keys.ToArray<int>())
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
                    // cell's membrane was added to the ecs in Cell.Divide()
                    if (SimulationBase.dataBasket.Environment is ECSEnvironment)
                    {
                        ((ECSEnvironment)SimulationBase.dataBasket.Environment).AddBoundaryManifold(c.PlasmaMembrane.Interior);
                        // add ECS boundary reactions, where applicable
                        List<ConfigReaction> reacs = SimulationBase.ProtocolHandle.GetReactions(SimulationBase.ProtocolHandle.scenario.environment.comp, true);
                        SimulationBase.AddCompartmentBoundaryReactions(SimulationBase.dataBasket.Environment.Comp, c.PlasmaMembrane, SimulationBase.ProtocolHandle.entity_repository, reacs, null);
                    }

                }
            }
        }

        /// <summary>
        /// zero all cell forces
        /// </summary>
        public void ResetCellForces()
        {
            foreach (CellsPopulation cellpop in SimulationBase.dataBasket.Populations.Values)
            {
                cellpop.resetForce();
            }
        }


        internal void InitializeNtCellManager()
        {
            double extend1 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0);
            double extend2 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1);
            double extend3 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2);

            bool ECS_flag = SimulationBase.dataBasket.Environment is ECSEnvironment;
            bool toroidal_flag = false;
            if (SimulationBase.dataBasket.Environment is ECSEnvironment)
            {
                toroidal_flag = ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal;
            }

            //bool boundary_force_flag = SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == false;

            base.SetEnvironmentExtents(extend1, extend2, extend3, ECS_flag, toroidal_flag, Pair.Phi1);
            int seed = SimulationBase.ProtocolHandle.sim_params.globalRandomSeed;
            base.InitializeNormalDistributionSampler(0.0, 1.0, seed);
        }
    }
}
