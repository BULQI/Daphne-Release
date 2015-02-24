using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NativeDaphne;
using MathNet.Numerics.Random;

namespace Daphne
{
    public class CellManager : IDynamic
    {
        private Dictionary<int, double[]> deadDict = null;
        private int[] tempDeadKeys = null;
        public DistributedParameter Phagocytosis;

        public static Nt_CellManager nt_cellManager;

       

        public CellManager()
        {
            deadDict = new Dictionary<int, double[]>();
            nt_cellManager = new Nt_CellManager();
            //double extend1 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0);
            //double extend2 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1);
            //double extend3 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2);
            //bool boundary_force_flag = SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == false;

            //nt_cellManager.SetEnvironmentExtents(extend1, extend2, extend3, boundary_force_flag, Pair.Phi1);
            //int seed = SimulationBase.ProtocolHandle.sim_params.globalRandomSeed;
            //nt_cellManager.InitializeNormalDistributionSampler(0.0, 1.0, seed);
        }

        public static int iteration_count = 0;

        public void Step(double dt)
        {
            List<int> removalList = null;
            List<Cell> daughterList = null;

            iteration_count++;
            //testing cytosol reaction using native methods.
            bool use_native = true;
            if (use_native)
            {
                nt_cellManager.step(dt);
            }

            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                // cell takes a step
                if (kvp.Value.Alive == true)
                {
                    kvp.Value.Step(dt);
                }

                // still alive and motile
                if (kvp.Value.Alive == true && kvp.Value.IsMotile == true && kvp.Value.Exiting == false)
                {
                    //if (kvp.Value.IsChemotactic)
                    //{
                    //    // For TinySphere cytosol, the force is determined by the gradient of the driver molecule at position (0,0,0).
                    //    // add the chemotactic force (accumulate it into the force variable)
                    //    kvp.Value.addForce(kvp.Value.Force(new double[3] { 0.0, 0.0, 0.0 }));
                    //}
                    //// apply the boundary force
                    //kvp.Value.BoundaryForce();
                    //// apply stochastic force
                    //if (kvp.Value.IsStochastic)
                    //{
                    //    //kvp.Value.addForce(kvp.Value.StochLocomotor.Force(dt));
                    //    kvp.Value.addForce(kvp.Value.StochLocomotor.Force(dt));
                    //}

                    //// A simple implementation of movement. For testing.
                    //for (int i = 0; i < kvp.Value.SpatialState.X.Length; i++)
                    //{
                    //    kvp.Value.SpatialState.X[i] += kvp.Value.SpatialState.V[i] * dt;
                    //    kvp.Value.SpatialState.V[i] += (-kvp.Value.DragCoefficient * kvp.Value.SpatialState.V[i] + kvp.Value.SpatialState.F[i]) * dt;
                    //}

                    // enforce boundary condition
                    kvp.Value.EnforceBC();
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
                        deadDict.Add(kvp.Value.Cell_id, new double[] {0.0, Phagocytosis.Sample()});
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
                }
            }

            // process death list
            if (deadDict != null)
            {
                tempDeadKeys = deadDict.Keys.ToArray<int>();
                foreach(int key in tempDeadKeys)
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
        internal void AddCell(Cell c)
        {
            //if (nt_cellManager.IsInitialized() == false)
            //{
            //    double extend1 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0);
            //    double extend2 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1);
            //    double extend3 = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2);
            //    bool boundary_force_flag = SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == false;

            //    nt_cellManager.SetEnvironmentExtents(extend1, extend2, extend3, boundary_force_flag, Pair.Phi1);
            //    int seed = SimulationBase.ProtocolHandle.sim_params.globalRandomSeed;
            //    nt_cellManager.InitializeNormalDistributionSampler(0.0, 1.0, seed);
            //}

            foreach (Reaction r in c.Cytosol.BulkReactions)
            {
                if (r is Transformation)
                {
                    Transformation tr = r as Transformation;
                    Nt_Transformation nt_rxn = new Nt_Transformation(c.Cell_id, tr.RateConstant);
                    nt_rxn.reactant.Add(tr.reactant.Conc.array);
                    nt_rxn.product.Add(tr.product.Conc.array);
                    nt_cellManager.AddReaction(nt_rxn);
                }
                else if (r is Transcription)
                {
                    Transcription tr = r as Transcription;
                    Nt_Transcription nt_rxn = new Nt_Transcription(c.Cell_id, tr.RateConstant);
                    nt_rxn.CopyNumber.Add(tr.gene.CopyNumber);
                    nt_rxn.ActivationLevel.Add(tr.gene._activationLevel);
                    nt_rxn.product.Add(tr.product.Conc.array);
                    nt_cellManager.AddReaction(nt_rxn);
                }
                else if (r is Annihilation)
                {
                    Annihilation ah = r as Annihilation;
                    Nt_Annihilation nt_rxn = new Nt_Annihilation(c.Cell_id, ah.RateConstant);
                    nt_rxn.reactant.Add(ah.reactant.Conc.array);
                    nt_cellManager.AddReaction(nt_rxn);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            foreach (List<Reaction> rlist in c.Cytosol.BoundaryReactions.Values)
            {
                foreach (Reaction r in rlist)
                {
                    if (r is CatalyzedBoundaryActivation)
                    {
                        CatalyzedBoundaryActivation cba = r as CatalyzedBoundaryActivation;
                        Nt_CatalyzedBoundaryActivation nt_rxn = new Nt_CatalyzedBoundaryActivation(c.Cell_id, cba.RateConstant);
                        nt_rxn.receptor.Add(cba.receptor.Conc.array);
                        int boundary_id = cba.receptor.Man.Id;
                        nt_rxn.bulkBoundaryConc.Add(cba.bulk.BoundaryConcs[boundary_id].array);
                        nt_rxn.bulkBoundaryFluxes.Add(cba.bulk.BoundaryFluxes[boundary_id].array);
                        nt_rxn.bulkActivatedBoundaryFluxes.Add(cba.bulkActivated.BoundaryFluxes[boundary_id].array);
                        nt_cellManager.AddReaction(nt_rxn);
                    }
                    else if (r is BoundaryTransportTo)
                    {
                        BoundaryTransportTo btt = r as BoundaryTransportTo;
                        Nt_BoundaryTransportTo nt_rxn = new Nt_BoundaryTransportTo(c.Cell_id, btt.RateConstant);
                        int boundary_id = btt.membrane.Man.Id;
                        nt_rxn.BulkBoundaryConc.Add(btt.bulk.BoundaryConcs[boundary_id].array);
                        nt_rxn.BulkBoundaryFluxes.Add(btt.bulk.BoundaryFluxes[boundary_id].array);
                        nt_rxn.MembraneConc.Add(btt.membrane.Conc.array);
                        nt_cellManager.AddReaction(nt_rxn);
                    }
                    else if (r is BoundaryTransportFrom)
                    {
                        BoundaryTransportFrom btf = r as BoundaryTransportFrom;
                        Nt_BoundaryTransportFrom nt_rxn = new Nt_BoundaryTransportFrom(c.Cell_id, btf.RateConstant);
                        int boundary_id = btf.membrane.Man.Id;
                        nt_rxn.BulkBoundaryFluxes.Add(btf.bulk.BoundaryFluxes[boundary_id].array);
                        nt_rxn.MembraneConc.Add(btf.membrane.Conc.array);
                        nt_cellManager.AddReaction(nt_rxn);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            foreach (var item in c.Cytosol.Populations)
            {
                MolecularPopulation mp = item.Value;
                double diffCoeff = mp.IsDiffusing ? mp.Molecule.DiffusionCoefficient : 0.0;
                double[] bflux = null;
                if (mp.BoundaryFluxes.Count > 0)
                {
                    //cytosol should always have a boundary of memrane?
                    bflux = mp.BoundaryFluxes.First().Value.array;
                }
                else
                {
                    bflux = new double[4];
                }
                double[] bconc = null;
                if (mp.BoundaryConcs.Count > 0)
                {
                    bconc = mp.BoundaryConcs.First().Value.array;
                }
                else
                {
                    bconc = new double[4];
                }
               
                var cmp = new Nt_CytosolMolecularPopulation(c.Cell_id, c.Radius, diffCoeff, mp.Conc.array, bflux, bconc);
                nt_cellManager.AddMolecularPopulation(cmp);
            }

            foreach (var item in c.PlasmaMembrane.Populations)
            {
                MolecularPopulation mp = item.Value;
                double diffCoeff = mp.IsDiffusing ? mp.Molecule.DiffusionCoefficient : 0.0;
                var cmp = new Nt_MembraneMolecularPopulation(c.Cell_id, c.Radius, diffCoeff, mp.Conc.array);
                nt_cellManager.AddMolecularPopulation(cmp);
            }

            if (c.IsMotile && c.Alive && !c.Exiting)
            {
                Nt_Cell ntc = new Nt_Cell(c.Cell_id, c.Radius, c.SpatialState.X, c.SpatialState.V, c.SpatialState.F);

                ntc.isMotile = c.IsMotile;
                ntc.isChemotactic = c.IsChemotactic;
                ntc.isStochastic = c.IsStochastic;
                ntc.cytokinetic = c.Cytokinetic;

                ntc.TransductionConsant = c.Locomotor.TransductionConstant;
                ntc.DragCoefficient = c.DragCoefficient;
                if (c.StochLocomotor != null)
                {
                    ntc.Sigma = c.StochLocomotor.Sigma;
                }
                else ntc.Sigma = 0;
                if (c.Locomotor != null && c.Locomotor.Driver != null)
                {
                    ntc.driverConc = c.Locomotor.Driver.Conc.array;
                }
                else
                {
                    ntc.driverConc = new double[4];
                }

                nt_cellManager.AddCell(ntc);
            }
        }
    }
}
