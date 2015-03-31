#pragma once

#include <errno.h>
#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Grid
	{
	public:
		Nt_Grid(array<double>^ _gridSize, double _gridStep)
		{
			if (_gridSize->Length != 3)
			{
				throw gcnew Exception("Grid must be three-dimensional.");
            }

			this->gridStep = _gridStep;
            this->gridSize = _gridSize;
            // volumes
            volume = gridSize[0] * gridSize[1] * gridSize[2];
            volumeVoxel = gridStep * gridStep * gridStep;
            // multi-dimensional array
            gridPts = gcnew array<int>{ (int)Math::Ceiling(gridSize[0] / gridStep),
                                  (int)Math::Ceiling(gridSize[1] / gridStep),
                                  (int)Math::Ceiling(gridSize[2] / gridStep) };
        }

		/// <summary>
        /// retrieve the grid size
        /// </summary>
        property array<double>^ GridSize
        {
			array<double>^ get()
			{
				return gridSize;
			}
        }

        /// <summary>
        /// retrieve the grid step
        /// </summary>
        property double GridStep
        {
			double get()
			{
				return gridStep;
			}
        }

        /// <summary>
        /// retrieve the grid units
        /// </summary>
        property array<int>^ GridPts
        {
			array<int>^ get()
			{
				return gridPts;
			}
        }

        /// <summary>
        /// based on a position, find a linear index in the grid
        /// </summary>
        /// <param name="pos">position to test</param>
        /// <returns>tuple with indices; negative for out of bounds</returns>
        void findGridIndex(Nt_Darray^ pos, array<int>^ idx)
        {
            //save one allocation
			double *pos_ptr = pos->NativePointer;
            for (int i = 0; i < pos->Length; i++)
            {
                idx[i] = (int)(pos_ptr[i] / gridStep);
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
        bool legalIndex(array<int>^ idx)
        {
            return idx[0] >= 0 && idx[0] < gridPts[0] && idx[1] >= 0 && idx[1] < gridPts[1] && idx[2] >= 0 && idx[2] < gridPts[2];
        }

		//default voxel = false;
		double Volume()
        {
			return volume;
        }

        double Volume(bool voxel)
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

		protected:
        /// <summary>
        /// width of a voxel in microns
        /// </summary>
        double gridStep;
        /// <summary>
        /// grid extents in microns
        /// </summary>
        array<double>^ gridSize;
        /// <summary>
        /// number of voxels in each dimension
        /// </summary>
        array<int>^ gridPts;
		private:
		double volume, volumeVoxel;
    };
}