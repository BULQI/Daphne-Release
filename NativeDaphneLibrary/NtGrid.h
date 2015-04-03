#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif
#define ITERATOR_DEBUG_LEVEL 0

#include <stdlib.h>
#include <unordered_map>
#include <xmmintrin.h>
#include <math.h>
#include "Utility.h"
#include "NtCellPair.h"


using namespace std;

namespace NativeDaphneLibrary
{

	class DllExport NtGrid
	{
	protected:
		double gridStep;
        /// <summary>
        /// grid extents in microns
        /// </summary>
        double* gridSize;
        /// <summary>
        /// number of voxels in each dimension
        /// </summary>
        int* gridPts;

		bool isToroidal;


	private:
		double volume, volumeVoxel;

	public:
		NtGrid(double * _gridSize, double _gridStep, bool _isToroidal)
		{
			gridSize = (double *)malloc(3 * sizeof(double));
			for (int i=0; i< 3; i++)
			{
				gridSize[i] = _gridSize[i];
			}
			this->gridStep = _gridStep;
            this->gridSize = _gridSize;
			isToroidal = _isToroidal;
            // volumes
            volume = gridSize[0] * gridSize[1] * gridSize[2];
            volumeVoxel = gridStep * gridStep * gridStep;
            // multi-dimensional array
			gridPts = (int *)malloc(3 * sizeof(int));
			gridPts[0] = (int)ceil(gridSize[0] / gridStep);
			gridPts[1] = (int)ceil(gridSize[1] / gridStep);
			gridPts[2] = (int)ceil(gridSize[2] / gridStep);
        }

		~NtGrid()
		{
		}

		//void linearIndexToIndexArray(int lin, int* idx)
  //      {

		//	idx[2] = lin/(gridPts[0] * gridPts[1]);
		//	lin %= (gridPts[0] * gridPts[1]);
		//	idx[1] = lin / gridPts[0];
		//	idx[0] = lin % gridPts[0];
  //      }

		//int IndexArrayToLinearIndex(int *idx)
		//{
		//	return idx[0] + idx[1] * gridPts[0] + idx[2] * (gridPts[0] * gridPts[1]);
		//}


        /// <summary>
        /// based on a position, find a linear index in the grid
        /// </summary>
        /// <param name="pos">position to test</param>
        /// <returns>tuple with indices; negative for out of bounds</returns>
        void findGridIndex(double* pos_ptr, int *idx)
        {
            for (int i = 0; i < 3; i++)
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
        bool legalIndex(int *idx)
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

		bool ClearSeperation(NtCellPair * pair)
		{
			if (pair->b_ij == 0)
			{
				int *a = pair->a->gridIndex;
				int *b = pair->b->gridIndex;
				int maxSep = pair->MaxSeparation;
				int d1 = a[0] - b[0];
				if (d1 > maxSep || d1 < -maxSep) return true;
				d1 = a[1] - b[1];
				if (d1 > maxSep || d1 < - maxSep) return true;
				d1 = a[2] - b[2];
				if (d1 > maxSep || d1 < -maxSep) return true;
			}
			return (!pair->a->isLegalIndex || !pair->b->isLegalIndex);
		}

		bool ClearSeparaitonToroidal(NtCellPair *pair)
		{
			if (pair->b_ij == 0 && ClearSeperationToroidal(pair->a->gridIndex, pair->b->gridIndex, pair->MaxSeparation) == true)
			{
				return true;
			}
			return (!pair->a->isLegalIndex || !pair->b->isLegalIndex);
		}

		bool ClearSeperation(int *a, int *b, int maxSep)
		{
			int d1 = a[0] - b[0];
			if (d1 > maxSep || d1 < -maxSep)return true;
			d1 = a[1] - b[1];
			if (d1 > maxSep || d1 < - maxSep)return true;
			d1 = a[2] - b[2];
			if (d1 > maxSep || d1 < -maxSep)return true;
			return false;
		}

		bool ClearSeperationToroidal(int *a, int *b, int maxSep)
		{

				int d1 = a[0] - b[0];
				if (d1 > 0.5 * gridPts[0])
				{
					d1 = gridPts[0] - d1;
				}
				if (d1 > maxSep || d1 < -maxSep) return true; 

				d1 = a[1] - b[1];
				if (d1 > 0.5 * gridPts[1])
				{
					d1 = gridPts[1] - d1;
				}
				if (d1 > maxSep || d1 < -maxSep) return true; 

				d1 = a[2] - b[2];
				if (d1 > 0.5 * gridPts[2])
				{
					d1 = gridPts[2] - d1;
				}
				return d1 > maxSep || d1 < maxSep;
		}

    };
}