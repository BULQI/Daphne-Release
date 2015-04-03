#pragma once


#include <errno.h>
#include <unordered_map>

#include "Nt_DArray.h"
#include "Nt_CellPair.h"
#include "Nt_Grid.h"
#include "Utility.h"
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

        // think high and low byte but using integer logic
        int pairHash(int idx1, int idx2)
        {
            return Math::Max(idx1, idx2) * pairKeyMultiplier + Math::Min(idx1, idx2);
        }

        // find a neighbor index of a grid tile; return -1 for illegal index
        array<int>^ neighbor(int* current, int dx, int dy, int dz)
        {

			array<int>^ idx = gcnew array<int>{ current[0], current[1], current[2] }; 

            idx[0] += dx;
            idx[1] += dy;
            idx[2] += dz;

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

        /// <summary>
        /// remove all pairs containing a cell
        /// </summary>
        /// <param name="del">cell to be deleted</param>
        void RemoveAllPairsContainingCell(Nt_Cell^ del);
        

        /// <summary>
        /// rekey pairs containing a cell
        /// </summary>
        /// <param name="cell">the changed cell with the new key</param>
        /// <param name="oldKey">the old cell key</param>
        void RekeyAllPairsContainingCell(Nt_Cell ^cell, int oldKey);
        
        /// <summary>
        /// remove a cell from the grid
        /// </summary>
        /// <param name="del">the cell to be removed</param>
        void RemoveCellFromGrid(Nt_Cell^ del)
        {
            // NOTE: if FDCs start to move, die, divide, we'll have to account for that here
            if (legalIndex(del->GridIndex) == true && grid[del->GridIndex[0], del->GridIndex[1], del->GridIndex[2]] != nullptr)
            {
                grid[del->GridIndex[0], del->GridIndex[1], del->GridIndex[2]]->Remove(del->Cell_id);
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
            if (legalIndex(cell->GridIndex) == true && grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]] != nullptr)
            {
                grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Add(cell->Cell_id, cell);
                grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Remove(oldKey);
            }
        }

	private:
        /// <summary>
        /// multiplier to calculate the pair hash key
        /// </summary>
        /// <returns></returns>
        int multiplier()
        {
            return (int)Math::Pow(10, Math::Round(0.5 + Math::Log10(Nt_Cell::SafeCell_id)));
        }

        /// <summary>
        /// recalculates and updates the distance for existing pairs
        /// </summary>
        //void updateExistingPairs()
        //{
        //    if (pairs != nullptr)
        //    {
        //        for each (KeyValuePair<int, Nt_CellPair^>^ kvp in pairs)
        //        {
        //            // recalculate the distance for pairs
        //            //kvp.Value.distance(gridSize);
        //            kvp->Value->distance();
        //        }
        //    }
        //}

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
			native_collisionManager->pairInteract(dt);
        }

		//unordered_map<int, NtCellPair *> *pairs;

        //Dictionary<int, Nt_CellPair^>^ pairs;
        int pairKeyMultiplier;
		int fastMultiplierDecide;
		array<Dictionary<int, Nt_Cell^>^, 3>^ grid;

        Object^ remove_key_pair_lock;
		array<int>^ tmp_idx;
    };
}