﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra;

namespace DaphneGui
{
    /// <summary>
    /// generic implementation of a grid
    /// </summary>
    public class Grid
    {
        /// <summary>
        /// generic grid constructor
        /// </summary>
        /// <param name="gridSize">vector holding the size of the grid in each dimension</param>
        /// <param name="gridStep">extent of each grid cell (uniform in all directions)</param>
        public Grid(Vector gridSize, double gridStep)
        {
            this.gridStep = gridStep;
            this.gridSize = gridSize.Clone();
            // volumes
            volume = gridSize[0] * gridSize[1] * gridSize[2];
            volumeVoxel = gridStep * gridStep * gridStep;
            // multi-dimensional array
            gridDim = new int[] { (int)Math.Ceiling(gridSize[0] / gridStep),
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
        /// retrieve the grid dimension
        /// </summary>
        public int[] GridDim
        {
            get { return gridDim; }
        }

        /// <summary>
        /// based on a position, find a linear index in the grid
        /// </summary>
        /// <param name="pos">position to test</param>
        /// <returns>tuple with indices; negative for out of bounds</returns>
        public int[] findGridIndex(Vector pos)
        {
            double[] tmp = new double[pos.Length];

            for (int i = 0; i < pos.Length; i++)
            {
                // tmp[0] goes along x, tmp[1] along y
                tmp[i] = pos[i] / gridStep;

                // for now return -1 for out of bounds
                if (tmp[i] < 0 || (int)tmp[i] > gridDim[i] - 1)
                {
                    return new int[] { -1, -1, -1 };
                }
            }

            return new int[] { (int)tmp[0], (int)tmp[1], (int)tmp[2] };
        }

        /// <summary>
        /// test an index tuple regaring whether it specifies legal indices
        /// </summary>
        /// <param name="idx">tuple to test</param>
        /// <returns>true or false</returns>
        public bool legalIndex(int[] idx)
        {
            return idx[0] >= 0 && idx[0] < gridDim[0] && idx[1] >= 0 && idx[1] < gridDim[1] && idx[2] >= 0 && idx[2] < gridDim[2];
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
        protected int[] gridDim;
        private double volume, volumeVoxel;
    }
}