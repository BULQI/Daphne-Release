﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NativeDaphne;
using MathNet.Numerics.Random;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Gene = NativeDaphne.Nt_Gene;

namespace Daphne
{
    public class CellManager : Nt_CellManager, IDynamic
    {
        private Dictionary<int, double[]> deadDict = null;
        private int[] tempDeadKeys = null;
        public DistributedParameter Phagocytosis;

        public CellManager()
        {
            deadDict = new Dictionary<int, double[]>();
        }

        public static int iteration_count = 0;

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            iteration_count++;
            //Debug.WriteLine("*******************interation = {0} *********************", iteration_count);
            //testing cytosol reaction using native methods.

            //steps through cell populations - a candidate for paralization later.
            foreach (KeyValuePair<int, CellsPopulation> kvp in SimulationBase.dataBasket.Populations)
            {
                //currently all reactions are handled in population level.
                kvp.Value.step(dt);
            }

            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                // cell takes a step
                if (kvp.Value.Alive == true)
                {
                    kvp.Value.Step(dt);
                }

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

            base.SetEnvironmentExtents(extend1, extend2, extend3, boundary_force_flag, Pair.Phi1);
            int seed = SimulationBase.ProtocolHandle.sim_params.globalRandomSeed;
            base.InitializeNormalDistributionSampler(0.0, 1.0, seed);
        }

    }
}
