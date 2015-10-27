#pragma once


//#include <errno.h>
//#include <unordered_map>

#include "Nt_DArray.h"
#include "Nt_CellPair.h"
#include "Nt_Grid.h"
#include "NtUtility.h"
#include "NtCollisionManager.h"

using namespace std;
using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CollisionManager: Nt_Grid
	{
	public:
		
		//signal if any cell changed there voxel position
		static bool CellGridIndexChanged;

		static bool isToroidal = false;

		//this signals that some 
		static bool cellStateAddressChanged = false;
		
		bool initialized;
		NtCollisionManager *native_collisionManager;

		/// <summary>
        /// constructor
        /// </summary>
        /// <param name="gridSize">3D grid size in microns</param>
        /// <param name="gridStep">voxel width in microns</param>
        Nt_CollisionManager(array<double>^ gridSize, double gridStep, bool _isEcsToroidal) : Nt_Grid(gridSize, gridStep)
        {
            grid = gcnew array<Dictionary<int, Nt_Cell^>^, 3>(gridPts[0], gridPts[1], gridPts[2]);
			remove_key_pair_lock = gcnew Object();
			isToroidal = _isEcsToroidal;
			
			pin_ptr<double> gs_ptr = &gridSize[0];
			native_collisionManager = new NtCollisionManager(gs_ptr, gridStep, _isEcsToroidal);
			tmp_idx = gcnew array<int>(3);
			initialized = false;
			CellGridIndexChanged = false;
        }

        void Step(double dt)
        {
            update(dt);
        }

		void set_parameter_Ph1(double p)
		{
			NtCollisionManager::Phi1 = p;
		}

	private:

		// high and low int
        long pairKey(int idx1, int idx2)
        {
            long max = idx1 > idx2 ? idx1 : idx2,
                 min = max == idx1 ? idx2 : idx1,
                 // half a long, meaning an int, in bits
                 key;
            Byte halfLongLength = sizeof(long) * 4;

            key = max;
            key <<= halfLongLength;
            key |= min;
            return key;
        }

        // find a neighbor index of a grid tile; return -1 for illegal index
        array<int>^ neighbor(int* current, int dx, int dy, int dz)
        {

			//using tmp rather allocate
			//array<int>^ idx = gcnew array<int>{ current[0], current[1], current[2] }; 

			array<int>^ idx = tmp_idx;
			idx[0] = current[0] + dx;
			idx[1] = current[1] + dy;
            idx[2] = current[2] + dz;

            if (legalIndex(idx) == false)
            {
                // correction for periodic boundary conditions
                if (isToroidal)
                {
                    if (idx[0] < 0 || idx[0] >= gridPts[0])
                    {
                        idx[0] %= gridPts[0];
                        if (idx[0] < 0)
                        {
                            idx[0] += gridPts[0];
                        }
                    }
                    if (idx[1] < 0 || idx[1] >= gridPts[1])
                    {
                        idx[1] %= gridPts[1];
                        if (idx[1] < 0)
                        {
                            idx[1] += gridPts[1];
                        }
                    }
                    if (idx[2] < 0 || idx[2] >= gridPts[2])
                    {
                        idx[2] %= gridPts[2];
                        if (idx[2] < 0)
                        {
                            idx[2] += gridPts[2];
                        }
                    }
                }
                else
                {
                    idx[0] = idx[1] = idx[2] = -1;
                }
            }
            return idx;
        }


		// find a neighbor index of a grid tile; return -1 for illegal index
        array<int>^ neighbor(long long longIndex, int dx, int dy, int dz)
        {

			//using tmp rather allocate
			//array<int>^ idx = gcnew array<int>{ current[0], current[1], current[2] }; 
			IndexStr indexStr(longIndex);
			array<int>^ idx = tmp_idx;
			idx[0] = indexStr.index[0] + dx;
			idx[1] = indexStr.index[1] + dy;
            idx[2] = indexStr.index[2] + dz;

            if (legalIndex(idx) == false)
            {
                // correction for periodic boundary conditions
                if (isToroidal)
                {
                    if (idx[0] < 0 || idx[0] >= gridPts[0])
                    {
                        idx[0] %= gridPts[0];
                        if (idx[0] < 0)
                        {
                            idx[0] += gridPts[0];
                        }
                    }
                    if (idx[1] < 0 || idx[1] >= gridPts[1])
                    {
                        idx[1] %= gridPts[1];
                        if (idx[1] < 0)
                        {
                            idx[1] += gridPts[1];
                        }
                    }
                    if (idx[2] < 0 || idx[2] >= gridPts[2])
                    {
                        idx[2] %= gridPts[2];
                        if (idx[2] < 0)
                        {
                            idx[2] += gridPts[2];
                        }
                    }
                }
                else
                {
                    idx[0] = idx[1] = idx[2] = -1;
                }
            }
            return idx;
        }


	public:
        /// <summary>
        /// remove all pairs containing a cell
        /// </summary>
        /// <param name="del">cell to be deleted</param>
        void RemoveAllPairsContainingCell(Nt_Cell^ del);
                
        /// <summary>
        /// remove a cell from the grid
        /// </summary>
        /// <param name="del">the cell to be removed</param>
        void RemoveCellFromGrid(Nt_Cell^ del)
        {
            // NOTE: if FDCs start to move, die, divide, we'll have to account for that here
			//		 
			//		 the gridIndex may or may not be the actuall location of where the cell is, 
			//		 since we are now updating gridIndex when spatialState->X changes.
			//		 but the value in LongGridIndex is only updated when the cell is placed in 
			//		 collisionManager and thus have the right location of the cell - AH
			bool found = false;
			if (del->LongGridIndex != -1)
			{
				IndexStr idx(del->LongGridIndex);
				if (grid[idx.index[0], idx.index[1], idx.index[2]] != nullptr)
				{
					found = grid[idx.index[0], idx.index[1], idx.index[2]]->Remove(del->Cell_id);
				}

				if (!found)
				{
					fprintf(stdout, "Warning: cell to be removed not found in grid. Cell_id= %d\n", del->Cell_id);
				}
			}
        }

		double GetBurnInMuValue(double integratorStep)
		{
			return native_collisionManager->getBurnInMuValue(integratorStep);
		}

	private:

        /// <summary>
        /// this version of the function avoids excessive loops
        /// </summary>
        void updateGridAndPairs();
        
        /// <summary>
        /// update and apply the grid state, pairs, and forces
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        void update(double dt)
        {
            // update cell locations in the grid tiles and update pairs
            updateGridAndPairs();
            // handle all pairs and find the forces
			//native_collisionManager->pairInteract(dt);
			native_collisionManager->MultiThreadPairInteract(dt);
        }


		//given two cell voxel indices, check if clearly separated
		bool clearSeparation(int *a, int *b)
		{
			if (!isToroidal)
			{
				int d1 = a[0] - b[0];
				if (d1 > 1 || d1 < -1)return true;
				d1 = a[1] - b[1];
				if (d1 > 1 || d1 < -1)return true;
				d1 = a[2] - b[2];
				if (d1 > 1 || d1 < -1)return true;
				return false;
			}
			else 
			{
				int d1 = a[0] > b[0] ? a[0] - b[0] : b[0] - a[0];
				if (d1 > 0.5 * gridPts[0])
				{
					d1 = gridPts[0] - d1;
				}
				if (d1 >1) return true; 

				d1 = a[1] > b[1] ? a[1] - b[1] : b[1] - a[1];
				if (d1 > 0.5 * gridPts[1])
				{
					d1 = gridPts[1] - d1;
				}
				if (d1 > 1) return true; 

				d1 = a[2] > b[2] ? a[2] - b[2] : b[2] - a[2];
				if (d1 > 0.5 * gridPts[2])
				{
					d1 = gridPts[2] - d1;
				}
				return d1 > 1;
			}
		}

		array<Dictionary<int, Nt_Cell^>^, 3>^ grid;

        Object^ remove_key_pair_lock;
		array<int>^ tmp_idx;
    };
}