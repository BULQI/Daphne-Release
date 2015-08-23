#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>

#include "Nt_CollisionManager.h"
#include "Nt_CellManager.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{
	//typedef std::unordered_map<int, NtCellPair *> PairMap;

	void Nt_CollisionManager::RemoveAllPairsContainingCell(Nt_Cell^ del)
	{
		if (!native_collisionManager->isEmpty() && del != nullptr)
		{
			for each (KeyValuePair<int, Nt_Cell^>^ kvp in Nt_CellManager::cellDictionary)
			{
				// no self-pairing
				if (del->Cell_id == kvp->Value->Cell_id)
				{
					continue;
				}

				long key = pairKey(del->Cell_id, kvp->Value->Cell_id);
				native_collisionManager->removePair(key); 
			}
			native_collisionManager->balance();
		}
	}

	void Nt_CollisionManager::updateGridAndPairs()
	{
            List<Nt_Cell^>^ criticalCells = nullptr;
            List<int>^ removalPairKeys = nullptr;

			// look at all cells to see if they changed in the grid
			Dictionary<int, Nt_Cell^>::ValueCollection^ cellColl = Nt_CellManager::cellDictionary->Values;
			if (cellStateAddressChanged == true)
			{
				//update pair pointer.
				//remove pairs from previous grid index
				for each (Nt_Cell^ cell in cellColl)
                {
					if (cell->nt_cell->X == cell->SpatialState->X->NativePointer) continue;
					cell->nt_cell->X = cell->SpatialState->X->NativePointer;
					cell->nt_cell->F = cell->SpatialState->F->NativePointer;
					cell->nt_cell->gridIndex = cell->GridIndex->NativePointer;
					//check if index legal
					if (cell->LongGridIndex == -1)continue;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                array<int>^ test = neighbor(cell->LongGridIndex, i, j, k);

                                // don't go outside the grid
                                if (legalIndex(test) == true && grid[test[0], test[1], test[2]] != nullptr)
                                {
                                    // add all pairs in the test grid location
                                    for each (KeyValuePair<int, Nt_Cell^>^ kvpg in grid[test[0], test[1], test[2]])
                                    {
                                        // do not allow self-collisions
                                        if (cell == kvpg->Value)
                                        {
                                            continue;
                                        }
                                        long key = pairKey(cell->Cell_id, kvpg->Value->Cell_id);
										native_collisionManager->resetPair(key, cell->nt_cell, kvpg->Value->nt_cell);
                                    }
                                }
                            }
                        }
                    }
                }
				cellStateAddressChanged = false;
			}

            // look at all cells to see if they changed in the grid
            for each (Nt_Cell^ cell in cellColl)
            {

				if (cell->gridIndex[3] == 0)continue;

				//long long newIndex = findGridIndex(cell->nt_cell->X);

                // if the grid index changed we have to:
                // -put it in its new one
                // -find new pairs
				cell->PrevLongGridIndex = cell->LongGridIndex;
				IndexStr idx(cell->gridIndex);
				long long newIndex = idx.Value();
				cell->LongGridIndex = newIndex;
			
				// insert into grid and determine critical cells
				if (newIndex != -1 || cell->PrevLongGridIndex != -1)
				{
					if (newIndex != -1 && grid[idx.index[0], idx.index[1], idx.index[2]] == nullptr)
					{
						grid[idx.index[0], idx.index[1], idx.index[2]] = gcnew Dictionary<int, Nt_Cell^>();
					}
					// schedule to find any new pairs
					if (criticalCells == nullptr)
					{
						criticalCells = gcnew List<Nt_Cell^>();
					}
					criticalCells->Add(cell);
				}
            }

            if (criticalCells != nullptr)
            {
				//remove pairs from previous gridindex if clearSeparated
				for each (Nt_Cell^ cell in criticalCells)
                {
					//check if previous index legal
					if (cell->PrevLongGridIndex == -1)continue;
					int *index1 = cell->nt_cell->gridIndex;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                array<int>^ test = neighbor(cell->PrevLongGridIndex, i, j, k);

                                // don't go outside the grid
                                if (legalIndex(test) == true && grid[test[0], test[1], test[2]] != nullptr)
                                {
                                    // add all pairs in the test grid location
                                    for each (KeyValuePair<int, Nt_Cell^>^ kvpg in grid[test[0], test[1], test[2]])
                                    {
                                        // do not allow self-collisions
                                        if (cell == kvpg->Value)
                                        {
                                            continue;
                                        }
                                        long key = pairKey(cell->Cell_id, kvpg->Value->Cell_id);
										//may try to avoid this if two voxels are still neighbours after the move.
										//so we don't go through 3*3*3 neighbours?
										if (cell->LongGridIndex == -1 || clearSeparation(index1, kvpg->Value->nt_cell->gridIndex) == true)
										{
											//remove if exist
											native_collisionManager->removePair(key);
										}
                                    }
                                }
                            }
                        }
                    }
                }

				//move the cells to new slots.
				for each (Nt_Cell^ cell in criticalCells)
                {
					if (cell->PrevLongGridIndex != -1)
					{
						IndexStr idx(cell->PrevLongGridIndex);
						Dictionary<int, Nt_Cell^>^ cell_dict = grid[idx.index[0], idx.index[1], idx.index[2]];
						if (cell_dict != nullptr)
						{
							cell_dict->Remove(cell->Cell_id);
						}
					}
					if (cell->LongGridIndex != -1)
					{			
						grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Add(cell->Cell_id, cell);
					}
				}

				// now find the new pairs
                for each (Nt_Cell^ cell in criticalCells)
                {
					if (cell->LongGridIndex == -1)continue;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                array<int>^ test = neighbor(cell->GridIndex->NativePointer, i, j, k);

                                // don't go outside the grid
                                if (legalIndex(test) == true && grid[test[0], test[1], test[2]] != nullptr)
                                {
                                    // add all pairs in the test grid location
                                    for each (KeyValuePair<int, Nt_Cell^>^ kvpg in grid[test[0], test[1], test[2]])
                                    {
                                        // do not allow self-collisions
                                        if (cell == kvpg->Value)
                                        {
                                            continue;
                                        }

                                        long key = pairKey(cell->Cell_id, kvpg->Value->Cell_id);

                                        // not already inserted
                                        if (native_collisionManager->itemExists(key) == false)
                                        {
											Nt_Cell^ cell2 = kvpg->Value;
											NtCellPair* pair1 = native_collisionManager->NewCellPair(key, cell->nt_cell, kvpg->Value->nt_cell);
                                            //NtCellPair* pair1 = new NtCellPair(cell->nt_cell, kvpg->Value->nt_cell);
											pair1->set_distance();
											native_collisionManager->addCellPair(key, pair1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
				//fix pair array if necessary
				native_collisionManager->balance();
            }
	}

}