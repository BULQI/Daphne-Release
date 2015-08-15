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

		static bool isToroidal = false;
		
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
			//this->pairs = native_collisionManager->pairs;
			tmp_idx = gcnew array<int>(3);
			initialized = false;
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
            if (legalIndex(del->gridIndex) == true && grid[del->gridIndex[0], del->gridIndex[1], del->gridIndex[2]] != nullptr)
            {
                grid[del->gridIndex[0], del->gridIndex[1], del->gridIndex[2]]->Remove(del->Cell_id);
            }
        }

        /// <summary>
        /// rekey a cell in the grid
        /// </summary>
        /// <param name="cell">the cell to be rekeyed with its new key</param>
        /// <param name="oldKey">the cell's old key</param>
        void RekeyCellInGrid(Nt_Cell^ cell, int oldKey)
        {
            // NOTE: if FDCs start to move, die, divide, we'll have to account for that here
            if (legalIndex(cell->gridIndex) == true && grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]] != nullptr)
            {
                grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Add(cell->Cell_id, cell);
                grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Remove(oldKey);
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

		array<Dictionary<int, Nt_Cell^>^, 3>^ grid;

        Object^ remove_key_pair_lock;
		array<int>^ tmp_idx;
    };
}