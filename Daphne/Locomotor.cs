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
        private MolecularPopulation driver;

        /// <summary>
        /// The constant of proportionality that convert concentration gradient into a force.
        /// </summary>
        public double TransductionConstant;

        public Locomotor(MolecularPopulation _driver, double constant)
        {
            driver = _driver;
            TransductionConstant = constant;
        }

        public double[] Force(double[] position)
        {
            double[] force = new double[3];

            force = driver.Conc.Gradient(position);
            for (int i = 0; i < force.Length; i++)
            {
                force[i] *= TransductionConstant;
            }

            return force;
        }
    }
}
