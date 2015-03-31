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

	void Nt_CollisionManager::RemoveAllPairsContainingCell(Nt_Cell^ del)
	{
            if (pairs != nullptr && pairs->Count > 0 && del != nullptr)
            {
                for each (KeyValuePair<int, Nt_Cell^>^ kvp in Nt_CellManager::cellDictionary)
                {
                    // no self-pairing
                    if (del->Cell_id == kvp->Value->Cell_id)
                    {
                        continue;
                    }

                    int hash = pairHash(del->Cell_id, kvp->Value->Cell_id);

                    // remove the pair; will only act if the pair exists
                    if (pairs->Remove(hash))
                    {
                        //Console.WriteLine("removal of pair " + del.Index + " " + kvp.Value.Index);
                    }
                }
            }
	}

	void Nt_CollisionManager::RekeyAllPairsContainingCell(Nt_Cell ^cell, int oldKey)
	{
            if (pairs != nullptr && pairs->Count > 0 && cell != nullptr)
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
                    if (pairs->ContainsKey(hash) == true)
                    {
                        // insert with new key
                        pairs->Add(pairHash(cell->Cell_id, kvp->Value->Cell_id), pairs[hash]);
                        // remove old key
                        pairs->Remove(hash);
                        //Console.WriteLine("rekeying of pair " + oldKey + " " + kvp.Value.Index);
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
            if (pairs == nullptr)
            {
                pairs = gcnew Dictionary<int, Nt_CellPair^>();
                pairKeyMultiplier = multiplier();
                fastMultiplierDecide = Nt_Cell::SafeCell_id;
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

                for each (KeyValuePair<int, Nt_CellPair^>^ kvp in pairs)
                {
					Nt_CellPair^ p = kvp->Value;
                    if (p->isCriticalPair() == false && clearSeparation(p) == true || legalIndex(p->Cell[0]->GridIndex) == false || legalIndex(p->Cell[1]->GridIndex) == false)
                    {
                        if (removalPairKeys == nullptr)
                        {
                            removalPairKeys = gcnew List<int>();
                        }
                        removalPairKeys->Add(kvp->Key);
                    }
                    else
                    {
                        // recalculate the distance for pairs that stay
                        //kvp.Value.distance(gridSize);
                        p->distance();
                    }
                }

                if (removalPairKeys != nullptr)
                {
                    for each (int key in removalPairKeys)
                    {
                        pairs->Remove(key);
                    }
                }
            }

            array<int>^ idx = gcnew array<int>(3);
            // look at all cells to see if they changed in the grid
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
                    if (legalIndex(cell->GridIndex) == true && grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]] != nullptr)
                    {
                        grid[cell->GridIndex[0], cell->GridIndex[1], cell->GridIndex[2]]->Remove(cell->Cell_id);
                    }

                    // have the cell remember its position in the grid
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
                        grid[idx[0], idx[1], idx[2]]->Add(cell->Cell_id, cell);

                        // schedule to find any new pairs
                        if (criticalCells == nullptr)
                        {
                            criticalCells = gcnew List<Nt_Cell^>();
                        }
                        criticalCells->Add(cell);
                    }
                }
            }

            // now find the new pairs
            if (criticalCells != nullptr)
            {
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
                                        if (pairs->ContainsKey(hash) == false)
                                        {
                                            // create the pair
                                            Nt_CellPair^ p = gcnew Nt_CellPair(cell, kvpg->Value);

                                            // calculate the distance
                                            //p.distance(gridSize);
                                            p->distance();
                                            // insert the pair
                                            pairs->Add(hash, p);
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