/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
