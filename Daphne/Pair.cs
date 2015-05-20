//#define ALL_PAIRS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using NativeDaphne;

namespace Daphne
{
    /// <summary>
    /// a pair of cells
    /// </summary>
    public abstract class Pair
    {
        /// <summary>
        /// constructor - takes the two cells that make up the pair
        /// </summary>
        /// <param name="a">cell a</param>
        /// <param name="b">cell b</param>
        public Pair(Cell a, Cell b)
        {
            this.a = a;
            this.b = b;
            dist = 0;
            b_ij = 0;
        }

        /// <summary>
        /// access a cell in the pair
        /// </summary>
        /// <param name="i">0 or 1 for the cell</param>
        /// <returns>pointer to the cell</returns>
        public Cell Cell(int i)
        {
            if (i == 0)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        /// <summary>
        /// calculate the distance for this pair of cells
        /// </summary>
        public virtual void calcDistance(double[] gridSize)
        {
            //Vector tmp = new DenseVector(a.SpatialState.X);
            //tmp = (DenseVector)tmpArr.Subtract(new DenseVector(b.SpatialState.X));
            Nt_Darray a_X = a.SpatialState.X;
            Nt_Darray b_X = b.SpatialState.X;
            double x = a_X[0] - b_X[0];
            double y = a_X[1] - b_X[1];
            double z = a_X[2] - b_X[2];
            // correction for periodic boundary conditions
            if (SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == true)
            {
                double dx = Math.Abs(x),
                       dy = Math.Abs(y),
                       dz = Math.Abs(z);

                if (dx > 0.5 * gridSize[0])
                {
                    x = gridSize[0] - dx;
                }
                if (dy > 0.5 * gridSize[1])
                {
                    y = gridSize[1] - dy;
                }
                if (dz > 0.5 * gridSize[2])
                {
                    z = gridSize[2] - dz;
                }
            }
            //dist = tmpArr.Norm(2);
            dist = Math.Sqrt(x * x + y * y + z * z);
        }

        public int GridIndex_dx
        {
            get
            {
                int dx;
                return ((dx = a.GridIndex[0] - b.GridIndex[0]) >= 0 ? dx : -dx);
            }
        }

        public int GridIndex_dy
        {
            get
            {
                int dy;
                return ((dy = a.GridIndex[1] - b.GridIndex[1]) >= 0 ? dy : -dy);
            }
        }

        public int GridIndex_dz
        {
            get
            {
                int dz;
                return ((dz = a.GridIndex[2] - b.GridIndex[2]) >= 0 ? dz : -dz);
            }
        }


        /// <summary>
        /// tells if this pair is critical, i.e. if it is interacting
        /// </summary>
        /// <returns>true for critical, false otherwise</returns>
        public bool isCriticalPair()
        {
            return b_ij == 1;
        }

        /// <summary>
        /// accessor for the force
        /// </summary>
        public double Force
        {
            get { return force; }
        }

        // accessor for the distance
        public double Distance
        {
            get { return dist; }
        }
        // abstract member functions

        protected abstract void bond();
        public abstract void pairInteract();
#if ALL_PAIRS
        public abstract void pairInteractIntermediateRK(double dt);

        public enum PairVariety { MOTILE, FDC, TCC };
        protected PairVariety variety;
#endif
        protected Cell a, b;
        protected double dist, force;
        protected int b_ij;
        public static double Phi1, Phi2;
    }

    /// <summary>
    ///  a concrete pair of cells
    /// </summary>
    public class CellPair : Pair
    {
        //temp array use to speed up - axin
        private double[] tmp_arr;
        /// <summary>
        /// constructor - takes the two cells that make up the pair
        /// </summary>
        /// <param name="a">cell a</param>
        /// <param name="b">cell b</param>
        public CellPair(Cell a, Cell b)
            : base(a, b)
        {
            tmp_arr = new double[3];
#if ALL_PAIRS
            variety = PairVariety.MOTILE;
#endif
        }

        /// <summary>
        /// calculate the bond variable for a pair
        /// </summary>
        protected override void bond()
        {
            //Console.WriteLine(String.Format("distance pair " + a.CellIndex + " " + b.CellIndex + " = " + dist + ", b = {0:N}", b_ij));
            if (b_ij == 0 && dist <= a.Radius + b.Radius)
            {
                b_ij = 1;
                //Console.WriteLine(String.Format("pair " + a.CellIndex + " " + b.CellIndex + " becomes critical, b = {0:N}", b_ij));
            }
            else if (b_ij == 1 && dist > a.Radius + b.Radius)
            {
                b_ij = 0;
                //Console.WriteLine(String.Format("pair " + a.CellIndex + " " + b.CellIndex + " is no longer critical, b = {0:N}", b_ij));
            }
        }

        /// <summary>
        /// choose the normal to point from a to b: a feels a negative force, b positive
        /// </summary>
        public override void pairInteract()
        {
            bond();

            force = 0.0;
            if (b_ij != 0)
            {
                if (dist > 0)
                {
                    force = Phi1 * (1.0 / dist - 1.0 / (a.Radius + b.Radius));
                }

                if (force != 0.0)
                {
                    //DenseVector normal = new DenseVector(b.SpatialState.X);

                    //normal -= a.SpatialState.X;
                    //normal = (DenseVector)normal.Normalize(2.0);

                    ////Console.WriteLine(String.Format("Distance: {2:N}, Force: {0:N}, B: {1:N}", force, b_ij, dist));

                    //// F_a = -F_b
                    ////a.addForce(normal * -force);
                    ////b.addForce(normal * force);

                    //a.addForce(normal.Multiply(-force).ToArray());
                    //b.addForce(normal.Multiply(force).ToArray());

                    //performance tuning
                    var b_X = b.SpatialState.X;
                    var a_X = a.SpatialState.X;
                    double dx = b_X[0] - a_X[0];
                    double dy = b_X[1] - a_X[1];
                    double dz = b_X[2] - a_X[2];

                    double tmplen = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    dx = dx * force / tmplen;
                    dy = dy * force / tmplen;
                    dz = dz * force / tmplen;
                    tmp_arr[0] = -dx;
                    tmp_arr[1] = -dy;
                    tmp_arr[2] = -dz;
                    a.addForce(tmp_arr);
                    tmp_arr[0] = dx;
                    tmp_arr[1] = dy;
                    tmp_arr[2] = dz;
                    b.addForce(tmp_arr);
                }
            }
        }

 
#if ALL_PAIRS
        /// <summary>
        /// apply the pair force but do not change b_ij
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteractIntermediateRK(double dt)
        {
            int saveB = b_ij;

            pairInteract(dt);
            b_ij = saveB;
        }

        public static double DeltaM;
#endif
    }
#if ALL_PAIRS
    /// <summary>
    ///  a pair of motile cells
    /// </summary>
    public class MotilePair : Pair
    {
        /// <summary>
        /// constructor - takes the two cells that make up the pair
        /// </summary>
        /// <param name="a">cell a</param>
        /// <param name="b">cell b</param>
        public MotilePair(BaseCell a, BaseCell b) : base(a, b)
        {
            variety = PairVariety.MOTILE;
        }

        /// <summary>
        /// calculate the bond variable for a pair
        /// </summary>
        protected override void bond()
        {
            //Console.WriteLine(String.Format("distance pair " + a.CellIndex + " " + b.CellIndex + " = " + dist + ", b = {0:N}", b_ij));
            if (b_ij == 0 && dist <= a.Radius + b.Radius)
            {
                b_ij = 1;
                //Console.WriteLine(String.Format("pair " + a.CellIndex + " " + b.CellIndex + " becomes critical, b = {0:N}", b_ij));
            }
            else if (b_ij == 1 && dist > 2 * DeltaM - a.Radius - b.Radius)
            {
                b_ij = 0;
                //Console.WriteLine(String.Format("pair " + a.CellIndex + " " + b.CellIndex + " is no longer critical, b = {0:N}", b_ij));
            }
        }

        /// <summary>
        /// choose the normal to point from a to b: a feels a negative force, b positive
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteract(double dt)
        {
            bond();

            if (b_ij != 0)
            {
                double force = 0.0;

                if (dist < a.Radius + b.Radius)
                {
                    if (dist > 0)
                    {
                        force = Phi1 * (1.0 / dist - 1.0 / (a.Radius + b.Radius));
                    }
                }
                else
                {
                    force = Phi2 * (Math.Pow(dist - DeltaM, 2.0) - Math.Pow(a.Radius + b.Radius - DeltaM, 2.0));
                }

                if (force != 0.0)
                {
                    Vector normal = new Vector(b.LM.StateV(Locomotor.StatesV.POS));

                    normal -= a.LM.StateV(Locomotor.StatesV.POS);
                    normal = normal.Normalize();

                    //Console.WriteLine(String.Format("Distance: {2:N}, Force: {0:N}, B: {1:N}", force, b_ij, dist));

                    // F_a = -F_b
                    a.LM.addForce(normal * -force);
                    b.LM.addForce(normal * force);
                }
            }
        }

        /// <summary>
        /// apply the pair force but do not change b_ij
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteractIntermediateRK(double dt)
        {
            int saveB = b_ij;

            pairInteract(dt);
            b_ij = saveB;
        }

        public static double DeltaM;
    }

    public class FDCPair : Pair
    {
        /// <summary>
        /// constructor - takes the two cells that make up the pair
        /// </summary>
        /// <param name="a">cell a</param>
        /// <param name="b">cell b</param>
        public FDCPair(BaseCell a, BaseCell b) : base(a, b)
        {
            variety = PairVariety.FDC;
        }

        /// <summary>
        /// return the pair's fdc
        /// </summary>
        /// <returns>pointer to the fdc</returns>
        public FDC FDC()
        {
            if (a.isBaseType(CellBaseTypeLabel.FDC) == true)
            {
                return (FDC)a;
            }
            else
            {
                return (FDC)b;
            }
        }

        /// <summary>
        /// return the pair's b-cell
        /// </summary>
        /// <returns>pointer to the b-cell</returns>
        public BCell B()
        {
            if (a.isBaseType(CellBaseTypeLabel.BCell) == true)
            {
                return (BCell)a;
            }
            else
            {
                return (BCell)b;
            }
        }

        /// <summary>
        /// calculate the distance for B-FDC: traditional distance before the synapse forms, B-cell tracking after
        /// </summary>
        public override void distance(double[] gridSize)
        {
            if (isCriticalPair() == false)
            {
                base.distance(gridSize);
            }
            else
            {
                Vector tmp = (double[])B().LM.StateV(Locomotor.StatesV.POS).Clone();

                tmp -= bStartingPos;
                dist = tmp.Norm();
            }
        }

        private double effectiveSurfaceArea()
        {
            double h = a.Radius + b.Radius;

            if(dist < h)
            {
                return Alpha * (1.0 - dist / h);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// calculate the bond variable for this pair: will determine when the synapse breaks
        /// </summary>
        protected override void bond()
        {
            if (b_ij == 1 && dist > DeltaF)
            {
                b_ij = 0;
            }
        }

        /// <summary>
        /// interaction for FDC-B
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteract(double dt)
        {
            // initial contact formation, create synapse if the condition is met
            if (b_ij == 0)
            {
                if (synapse == null && FDC().AvailableArea > FDC().SynapseArea && Kappa * effectiveSurfaceArea() * dt > Utilities.SystemRandom.Sample())
                {
                    {
                        b_ij = 1;
                        bStartingPos = (double[])B().LM.StateV(Locomotor.StatesV.POS).Clone();
                        dist = 0;
                        synapse = new FDCSynapse(this);
                        FDC().AvailableArea -= FDC().SynapseArea;
                        if (Properties.Settings.Default.skipDataBaseWrites == false)
                        {
#if WRITE_SUMMARY_FILE_ON_FLY
                            if(Properties.Settings.Default.writeCellsummaries==true)
                                Simulation.synapseSummary.WriteLine("Synapse formation, BCell {0} FDC {1}", B().CellIndex, FDC().CellIndex);
#endif
                            MainWindow.Sim.Dw.addSynapse(MainWindow.SOP.Protocol.experiment_db_id, B().CellIndex, -1, FDC().CellIndex,-0.1);
                        }
                    }
                }
            }
            else
            {
                // update the bond variable
                bond();
            }

            if (synapse != null)
            {
                synapse.Step(dt);
                if (b_ij == 0)
                {
                    if (Properties.Settings.Default.skipDataBaseWrites == false)
                    {
#if WRITE_SUMMARY_FILE_ON_FLY
                        if(Properties.Settings.Default.writeCellsummaries==true)
                            Simulation.synapseSummary.WriteLine("Synapse end, BCell {0} FDC {1}, age {2}", B().CellIndex, FDC().CellIndex, synapse.Age);
#endif
                        MainWindow.Sim.Dw.addSynapse(MainWindow.SOP.Protocol.experiment_db_id, B().CellIndex, -1, FDC().CellIndex, synapse.Age );
                    }
                    synapse = null;
                    FDC().AvailableArea += FDC().SynapseArea;
                }
                // adhesion
                else if (dist > 0)
                {
                    double force = 0.0;

                    force = (Phi2 + Beta * B().BCRBound) * dist * (dist - DeltaF);

                    if (force != 0.0)
                    {
                        // normal towards the b-cell's position
                        Vector normal = new Vector(B().LM.StateV(Locomotor.StatesV.POS));

                        normal -= bStartingPos;
                        normal = normal.Normalize();

                        // only the b-cell feels adhesion
                        B().LM.addForce(normal * force);
                    }
                }
            }
        }

        /// <summary>
        /// apply the pair force but do not change b_ij
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteractIntermediateRK(double dt)
        {
            // NOTE: needed
        }

        private Synapse synapse;
        private Vector bStartingPos;
        public static double Alpha, Beta, Kappa, DeltaF;
    }

    public class TCentrocytePair : MotilePair
    {
        /// <summary>
        /// constructor - takes the two cells that make up the pair
        /// </summary>
        /// <param name="a">cell a</param>
        /// <param name="b">cell b</param>
        public TCentrocytePair(BaseCell a, BaseCell b) : base(a, b)
        {
            variety = PairVariety.TCC;
        }

        /// <summary>
        /// return the pair's T cell
        /// </summary>
        /// <returns>pointer to the T cell</returns>
        public TCell T()
        {
            if (a.isBaseType(CellBaseTypeLabel.TCell) == true)
            {
                return (TCell)a;
            }
            else
            {
                return (TCell)b;
            }
        }

        /// <summary>
        /// return the pair's b-cell
        /// </summary>
        /// <returns>pointer to the b-cell</returns>
        public BCell B()
        {
            if (a.isBaseType(CellBaseTypeLabel.BCell) == true)
            {
                return (BCell)a;
            }
            else
            {
                return (BCell)b;
            }
        }

        /// <summary>
        /// interaction for T-B pair
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteract(double dt)
        {
            bond();

            if (b_ij > 0)
            {
                if (synapse == null)
                {
                    synapse = new TCentrocyteSynapse(this);
                    if (Properties.Settings.Default.skipDataBaseWrites == false)
                    {
#if WRITE_SUMMARY_FILE_ON_FLY
                        if(Properties.Settings.Default.writeCellsummaries==true)
                            Simulation.synapseSummary.WriteLine("TCentrocyte Synapse formation, BCell {0} TCell {1}", B().CellIndex, T().CellIndex);
#endif
                        MainWindow.Sim.Dw.addSynapse(MainWindow.SOP.Protocol.experiment_db_id, B().CellIndex, T().CellIndex , -1, -0.1);
                    }
                }

                // compute force
                double force = 0.0;

                if (dist < a.Radius + b.Radius)
                {
                    if (dist > 0)
                    {
                        force = Phi1 * (1.0 / dist - 1.0 / (a.Radius + b.Radius));
                    }
                }
                else
                {
                    force = Phi2 * (Math.Pow(dist - DeltaT, 2.0) - Math.Pow(a.Radius + b.Radius - DeltaT, 2.0));
                }
            
                if (force != 0.0)
                {
                    Vector normal = new Vector(b.LM.StateV(Locomotor.StatesV.POS));

                    normal -= a.LM.StateV(Locomotor.StatesV.POS);
                    normal = normal.Normalize();

                    // F_a = -F_b
                    a.LM.addForce(normal * -force);
                    b.LM.addForce(normal * force);
                }
            }

            if (synapse != null)
            {
                synapse.Step(dt);
                if (b_ij == 0)
                {
                    // TEMP_SUMMARY
                    if (Properties.Settings.Default.skipDataBaseWrites == false)
                    {
#if WRITE_SUMMARY_FILE_ON_FLY
                        if(Properties.Settings.Default.writeCellsummaries==true)
                            Simulation.synapseSummary.WriteLine("TB Synapse end, BCell {0} TCell {1}, age {2}", B().CellIndex, T().CellIndex, synapse.Age);
#endif
                        MainWindow.Sim.Dw.addSynapse(MainWindow.SOP.Protocol.experiment_db_id, B().CellIndex, T().CellIndex,-1,  synapse.Age);
                    }
                    synapse = null;
                }
            }
        }

        /// <summary>
        /// apply the pair force but do not change b_ij
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        public override void pairInteractIntermediateRK(double dt)
        {
            // NOTE: needed
        }

        private Synapse synapse;
        //private Vector bStartingPos;
        public static double DeltaT;
    }
#endif
}
