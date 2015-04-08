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
	typedef std::unordered_map<int, NtCellPair *> PairMap;

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

                    int hash = pairHash(del->Cell_id, kvp->Value->Cell_id);
					native_collisionManager->removePair(hash);
                    // remove the pair; will only act if the pair exists
                    //if (pairs->erase(hash))
                    //{
                    //    //Console.WriteLine("removal of pair " + del.Index + " " + kvp.Value.Index);
                    //}
                }
            }
	}

	void Nt_CollisionManager::RekeyAllPairsContainingCell(Nt_Cell ^cell, int oldKey)
	{
            if (!native_collisionManager->isEmpty() && cell != nullptr)
            {
                for each (KeyValuePair<int, Nt_Cell^>^ kvp in Nt_CellManager::cellDictionary)
                {
                    // no self-pairing
                    if (oldKey == kvp->Value->Cell_id)
                    {
                        continue;
                    }

                    int hash = pairHash(oldKey, kvp->Value->Cell_id);

                    // remove the pair; will only act if the pair exists
					if (native_collisionManager->itemExists(hash) == true)
					{
						NtCellPair *p = native_collisionManager->getPair(hash);
						native_collisionManager->removePair(hash);
                        // insert with new key
						//std::pair<int, NtCellPair *> p(pairHash(cell->Cell_id, kvp->Value->Cell_id), *pi);
						int key = pairHash(cell->Cell_id, kvp->Value->Cell_id);
						native_collisionManager->addCellPair(key, p);

                        //pairs->insert(make_pair(key, pi->second));
                        //// remove old key
                        //pairs->erase(hash);
                        ////Console.WriteLine("rekeying of pair " + oldKey + " " + kvp.Value.Index);
                    }
                }
            }
	}


	void Nt_CollisionManager::updateGridAndPairs()
	{
            List<Nt_Cell^>^ criticalCells = nullptr;
            List<int>^ removalPairKeys = nullptr;

            array<double>^ gridSizeArr = gridSize;

            // create the pairs dictionary
            if (initialized == false)
            {
                pairKeyMultiplier = multiplier();
                fastMultiplierDecide = Nt_Cell::SafeCell_id;
				native_collisionManager->ClearPairs();
				initialized = true;
            }
            else
            {
                // update the multiplier if needed
                if (Nt_Cell::SafeCell_id > fastMultiplierDecide)
                {
                    int tmp = multiplier();

                    fastMultiplierDecide = Nt_Cell::SafeCell_id;
                    if (tmp > pairKeyMultiplier)
                    {
                        pairKeyMultiplier = tmp;
                    }
                }

				//remove non-critical pairs
				//int num_removed = native_collisionManager->removeNonCriticalPairs();
            }

            array<int>^ idx = tmp_idx;
            // look at all cells to see if they changed in the grid
			Dictionary<int, Nt_Cell^>^ tmp_dict = Nt_CellManager::cellDictionary;
			int nn3 = tmp_dict->Count;
            for each (KeyValuePair<int, Nt_Cell^>^ kvpc in Nt_CellManager::cellDictionary)
            {
				Nt_Cell^ cell = kvpc->Value;
                //double check idx is get updated, does not seem need reference
                findGridIndex(cell->SpatialState->X, idx);

                // if the grid index changed we have to:
                // -put it in its new one
                // -find new pairs
                if (idx[0] != cell->GridIndex[0] || idx[1] != cell->GridIndex[1] || idx[2] != cell->GridIndex[2])
                {
                    // was inserted before? we have to remove it
                    // NOTE: if fdcs were to start moving we would need special case handling for that here
                    // where the whole array of voxels that had an fdc gets cleared
                    //if (legalIndex(cell->GridIndex) == true && grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]] != nullptr)
                    //{
                    //    grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Remove(cell->Cell_id);
                    //}

                    // have the cell remember its position in the grid
					cell->PreviousGridIndex[0] = cell->GridIndex[0];
					cell->PreviousGridIndex[1] = cell->GridIndex[1];
					cell->PreviousGridIndex[2] = cell->GridIndex[2];
                    cell->GridIndex[0] = idx[0];
                    cell->GridIndex[1] = idx[1];
                    cell->GridIndex[2] = idx[2];
                    // insert into grid and determine critical cells
                    if (legalIndex(idx) == true)
                    {
                        if (grid[idx[0], idx[1], idx[2]] == nullptr)
                        {
                            grid[idx[0], idx[1], idx[2]] = gcnew Dictionary<int, Nt_Cell^>();
                        }
                        // use the cell's list index as key
                        //grid[idx[0], idx[1], idx[2]]->Add(cell->Cell_id, cell);

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
				//remove old pairs
				for each (Nt_Cell^ cell in criticalCells)
                {
					if (legalIndex(cell->PreviousGridIndex) == false)continue;
					//if (cell->Cell_id == 0 && cell->GridIndex[2] == 22 && cell->GridIndex[0] == 21 && cell->GridIndex[1] == 20)
					//{
					//	int check_here = true;
					//}
					bool curIndexLegal = legalIndex(cell->GridIndex);
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                array<int>^ test = neighbor(cell->PreviousGridIndex, i, j, k);

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
                                        int hash = pairHash(cell->Cell_id, kvpg->Value->Cell_id);
										if (curIndexLegal == true)
										{
											//if exist and clear-separated - rmeove
											native_collisionManager->removePairOnClearSeparation(hash);
										}
										else 
										{
											//remove if exist
											native_collisionManager->removePair(hash);
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
					if (legalIndex(cell->PreviousGridIndex) == true)
					{
						Dictionary<int, Nt_Cell^>^ cell_dict = grid[cell->PreviousGridIndex[0], cell->PreviousGridIndex[1], cell->PreviousGridIndex[2]];
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
                                array<int>^ test = neighbor(cell->GridIndex, i, j, k);

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

                                        int hash = pairHash(cell->Cell_id, kvpg->Value->Cell_id);

                                        // not already inserted

                                        if (native_collisionManager->itemExists(hash) == false)
                                        {
											Nt_Cell^ cell2 = kvpg->Value;
                                            NtCellPair* pair1 = new NtCellPair(cell->nt_cell, kvpg->Value->nt_cell);
											pair1->set_distance();
											native_collisionManager->addCellPair(hash, pair1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
				native_collisionManager->update_iterator();
            }
	}

}