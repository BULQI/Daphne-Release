using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    /// <summary>
    /// The locomotor class generates a force from the gradient of a molecular population.
    /// </summary>
    public class Locomotor
    {
        //driver is the molecular population that provide locomotor with a gradient
        public MolecularPopulation Driver { get; set; }

        /// <summary>
        /// The constant of proportionality that convert concentration gradient into a force.
        /// </summary>
        public double TransductionConstant { get; set; }
        /// <summary>
        /// Re-use this array for calculating the force
        /// </summary>
        public double[] force = {0, 0, 0};

        public Locomotor(MolecularPopulation _driver, double constant)
        {
            Driver = _driver;
            TransductionConstant = constant;
         }

        public double[] Force(double[] position)
        {
            force = Driver.Conc.Gradient(position);
            for (int i = 0; i < force.Length; i++)
            {
                force[i] *= TransductionConstant;
            }

            return force;
        }
    }

    public class StochLocomotor
    {
        /// <summary>
        /// Sigma / sqrt(dt) = standard deviation of stochastic force
        /// </summary>
        public double Sigma;
        /// <summary>
        /// Re-use this array for calculating the force
        /// </summary>
        public double[] force = { 0, 0, 0 };

        public StochLocomotor(double sigma)
        {
            Sigma = sigma;
        }

        public double[] Force(double dt)
        {
            double tmp = Sigma / Math.Sqrt(dt);
            for (int i = 0; i < force.Length; i++)
            {
                //force[i] = Sigma * Rand.NormalDist.Sample() / Math.Sqrt(dt);
                force[i] = Rand.NormalDist.Sample() * tmp;
            }

            return force;
        }
    }
}
