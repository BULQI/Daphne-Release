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

using MathNet.Numerics.LinearAlgebra.Double;
using NativeDaphne;

namespace Daphne
{
    /// <summary>
    /// generic implementation of a grid
    /// </summary>
    public class Grid
    {
        /// <summary>
        /// generic grid constructor
        /// </summary>
        /// <param name="gridSize">vector holding the size of the grid in each direction</param>
        /// <param name="gridStep">extent of each grid cell (uniform in all directions)</param>
        public Grid(Vector gridSize, double gridStep)
        {
            if (gridSize.Count != 3)
            {
                throw new Exception("Grid must be three-dimensional.");
            }

            this.gridStep = gridStep;
            this.gridSize = (DenseVector)gridSize.Clone();
            // volumes
            volume = gridSize[0] * gridSize[1] * gridSize[2];
            volumeVoxel = gridStep * gridStep * gridStep;
            // multi-dimensional array
            gridPts = new int[] { (int)Math.Ceiling(gridSize[0] / gridStep),
                                  (int)Math.Ceiling(gridSize[1] / gridStep),
                                  (int)Math.Ceiling(gridSize[2] / gridStep) };
        }

        /// <summary>
        /// retrieve the grid size
        /// </summary>
        public Vector GridSize
        {
            get { return gridSize; }
        }

        /// <summary>
        /// retrieve the grid step
        /// </summary>
        public double GridStep
        {
            get { return gridStep; }
        }

        /// <summary>
        /// retrieve the grid units
        /// </summary>
        public int[] GridPts
        {
            get { return gridPts; }
        }

        /// <summary>
        /// based on a position, find a linear index in the grid
        /// </summary>
        /// <param name="pos">position to test</param>
        /// <returns>tuple with indices; negative for out of bounds</returns>
        public void findGridIndex(Nt_Darray pos, ref int[] idx)
        {
            //save one allocation
            for (int i = 0; i < pos.Length; i++)
            {
                idx[i] = (int)(pos[i] / gridStep);
                if (idx[i] < 0 || idx[i] > gridPts[i] - 1)
                {
                    idx[0] = idx[1] = idx[2] = -1;
                    return;
                }
            }
        }

        public void findGridIndex(Nt_Darray pos, ref Nt_Iarray idx)
        {
            //save one allocation
            for (int i = 0; i < pos.Length; i++)
            {
                idx[i] = (int)(pos[i] / gridStep);
                if (idx[i] < 0 || idx[i] > gridPts[i] - 1)
                {
                    idx[0] = idx[1] = idx[2] = -1;
                    return;
                }
            }
        }

        /// <summary>
        /// test an index tuple regaring whether it specifies legal indices
        /// </summary>
        /// <param name="idx">tuple to test</param>
        /// <returns>true or false</returns>
        public bool legalIndex(Nt_Iarray idx)
        {
            return idx[0] >= 0 && idx[0] < gridPts[0] && idx[1] >= 0 && idx[1] < gridPts[1] && idx[2] >= 0 && idx[2] < gridPts[2];
        }


        public bool legalIndex(int[] idx)
        {
            return idx[0] >= 0 && idx[0] < gridPts[0] && idx[1] >= 0 && idx[1] < gridPts[1] && idx[2] >= 0 && idx[2] < gridPts[2];
        }

        public double Volume(bool voxel = false)
        {
            if (voxel == false)
            {
                return volume;
            }
            else
            {
                return volumeVoxel;
            }
        }

        /// <summary>
        /// width of a voxel in microns
        /// </summary>
        protected double gridStep;
        /// <summary>
        /// grid extents in microns
        /// </summary>
        protected Vector gridSize;
        /// <summary>
        /// number of voxels in each dimension
        /// </summary>
        protected int[] gridPts;
        private double volume, volumeVoxel;
    }
}
