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
            }
	}

	void Nt_CollisionManager::updateGridAndPairs()
	{
            List<Nt_Cell^>^ criticalCells = nullptr;
            List<int>^ removalPairKeys = nullptr;

            // look at all cells to see if they changed in the grid
			Dictionary<int, Nt_Cell^>::ValueCollection^ cellColl = Nt_CellManager::cellDictionary->Values;
            for each (Nt_Cell^ cell in cellColl)
            {
				long long newIndex = findGridIndex(cell->nt_cell->X);

				//if (cell->nt_cell->X != cell->SpatialState->X->NativePointer)
				//{
				//	int wrong = 1;
				//}
                // if the grid index changed we have to:
                // -put it in its new one
                // -find new pairs
                if (newIndex != cell->LongGridIndex)
                {

					findGridIndex(cell->SpatialState->X, tmp_idx);
					IndexStr idx(newIndex);
                    cell->GridIndex[0] = idx.index[0];
                    cell->GridIndex[1] = idx.index[1];
                    cell->GridIndex[2] = idx.index[2];

					//verify....
					//findGridIndex(cell->SpatialState->X, tmp_idx);
					//{
					//	if (tmp_idx[0] != cell->GridIndex[0] || tmp_idx[1] != cell->GridIndex[1] ||
					//		tmp_idx[2] != cell->GridIndex[2])
					//	{
					//		int wrong = 1;
					//	}
					//}

					cell->PrevLongGridIndex = cell->LongGridIndex;
					cell->LongGridIndex = newIndex;
                    // insert into grid and determine critical cells
                    if (newIndex != -1)
                    {
                        if (grid[idx.index[0], idx.index[1], idx.index[2]] == nullptr)
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
            }

            if (criticalCells != nullptr)
            {
				//remove pairs from previous grid index
				for each (Nt_Cell^ cell in criticalCells)
                {
					//check if previous index legal
					if (cell->PrevLongGridIndex == -1)continue;
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
										if (cell->LongGridIndex != -1) //leagal index
										{
											//if exist and clear-separated - rmeove
											native_collisionManager->removePairOnClearSeparation(key);
										}
										else 
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
					grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Add(cell->Cell_id, cell);
				}

				// now find the new pairs
                for each (Nt_Cell^ cell in criticalCells)
                {
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
											NtCellPair* pair1 = native_collisionManager->NewCellPair(cell->nt_cell, kvpg->Value->nt_cell);
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
            }
	}

}