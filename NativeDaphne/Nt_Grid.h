#pragma once

#include <errno.h>
#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	typedef struct LongIndex
	{
		short index[4];

		LongIndex(){}

		LongIndex(long long value)
		{			
			Set(value);
		}

		LongIndex(int *src)
		{
			index[0] = src[0];
			index[1] = src[1];
			index[2] = src[2];
			index[3] = src[3];
		}

		void Set(long long value)
		{
			if (value < 0)
			{
				index[0] = -1;
				index[1] = -1;
				index[2] = -1;
				index[3] = -1;
				return;
			}
			index[0] = value >>48;
			index[1] = (value <<16) >> 48;
			index[2] = (value <<32) >> 48;
			index[3] = (value <<48) >> 48;
		}

		long long Value()
		{
			return  ((unsigned long long)index[0] <<48) | ((unsigned long long)index[1] <<32) | (unsigned long long)index[2] << 16 | index[3];
		}

	}IndexStr;



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
			static_GridStepInverse = 1.0/_gridStep;

            this->gridSize = _gridSize;

            // volumes
            volume = gridSize[0] * gridSize[1] * gridSize[2];
            volumeVoxel = gridStep * gridStep * gridStep;
            // multi-dimensional array
            gridPts = gcnew array<int>{ (int)Math::Ceiling(gridSize[0] / gridStep),
                                  (int)Math::Ceiling(gridSize[1] / gridStep),
                                  (int)Math::Ceiling(gridSize[2] / gridStep) };
			static_GridPts = gridPts;
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

		//compute the index and store the value in a long long integer
		//return -1 if out of range.
		long long findGridIndex(double *pos_ptr)
		{
			int index = (int)(pos_ptr[0] * static_GridStepInverse);
			if ( (unsigned)index > (unsigned)gridPts[0] - 1) return -1;
			long long retval = (unsigned long long)index <<48;
			index = (int)(pos_ptr[1] * static_GridStepInverse);
			if ( (unsigned)index > (unsigned)gridPts[1] - 1) return -1;
			retval |= (unsigned long long)index << 32;
			index = (int)(pos_ptr[2] * static_GridStepInverse);
			if ( (unsigned)index > (unsigned)gridPts[2] - 1) return -1;
			retval |= (unsigned long long)index << 16;
			return retval;
		}

        /// <summary>
        /// test an index tuple regaring whether it specifies legal indices
        /// </summary>
        /// <param name="idx">tuple to test</param>
        /// <returns>true or false</returns>
        bool legalIndex(int *idx)
        {
            return (unsigned)idx[0] < (unsigned)gridPts[0] && (unsigned)idx[1] < (unsigned)gridPts[1] && (unsigned)idx[2] < (unsigned)gridPts[2];;
        }

		bool legalIndex(array<int>^ idx)
		{
			return (unsigned)idx[0] < (unsigned)gridPts[0] && (unsigned)idx[1] < (unsigned)gridPts[1] && (unsigned)idx[2] < (unsigned)gridPts[2];
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


		static double static_GridStepInverse;

		static array<int>^ static_GridPts;

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