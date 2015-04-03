#include "stdafx.h"
#include "Utility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <unordered_map>

#include "NtCollisionManager.h"

using namespace std;
typedef list<int> LISTINT;

namespace NativeDaphneLibrary
{
	double *NtCollisionManager::GridSize = NULL;
	double NtCollisionManager::GridStep = 0;
	bool NtCollisionManager::IsToroidal = false;
	double NtCollisionManager::Phi1 = 0;
	int NtCollisionManager::max_removal = 0;

	typedef std::unordered_map<int, NtCellPair *> PairMap;


//	void NtCollisionManager::updateExistingPairs()
//	{
//		if (pairs == NULL)return;
//		if (IsToroidal)
//		{
//			updateExistingPairs_toroidal();
//			return;
//		}
//
//#if UseSSE2
//		double dst_arr[3];
//		__m128 m1, m2, m3, m4;
//		__m128* pSrc1;
//		__m128* pSrc2;
//		__m128* pDst = (__m128*)dst_arr;
//#endif
//
//		for (PairMap::iterator it = pairs->begin(); it != pairs->end(); ++it)
//		{
//			NtCellPair *pair = it->second;
//
//			double *a_X = pair->a->X;
//			double *b_X = pair->b->X;
//
//            double x = a_X[0] - b_X[0];
//            double y = a_X[1] - b_X[1];
//            double z = a_X[2] - b_X[2];
//            pair->distance = sqrt(x * x + y * y + z * z);
//
//#if UseSSE2
//			pSrc1 = (__m128*)(pair->a->X);
//			pSrc2 = (__m128*)(pair->b->X);
//			m1 = _mm_sub_ss(*pSrc1, *pSrc2);
//			*pDst = _mm_mul_ps(m1, m1);
//			pSrc1++;
//			pSrc2++;
//			pDst++;
//			m1 = _mm_sub_ss(*pSrc1, *pSrc2);
//			*pDst = _mm_mul_ps(m1, m1);
//			pair->distance2 = dst_arr[0] + dst_arr[1] + dst_arr[2];
//#endif
//		}
//	}
//
//	void NtCollisionManager::updateExistingPairs_toroidal()
//	{
//		if (pairs == NULL)return;
//
//#if UseSSE2
//		double dst_arr[3];
//		__m128 m1, m2, m3, m4;
//		__m128* pSrc1;
//		__m128* pSrc2;
//		__m128* pDst = (__m128*)dst_arr;
//#endif
//
//		double half_gridSize0 = GridSize[0] * 0.5;
//		double half_gridSize1 = GridSize[1] * 0.5;
//		double half_gridSize2 = GridSize[2] * 0.5;
//
//		for (PairMap::iterator it = pairs->begin(); it != pairs->end(); ++it)
//		{
//			NtCellPair *pair = it->second;
//
//			double *a_X = pair->a->X;
//			double *b_X = pair->b->X;
//
//            double x = a_X[0] - b_X[0];
//            double y = a_X[1] - b_X[1];
//            double z = a_X[2] - b_X[2];
//
//			double dx = x > 0 ? x : -x;
//			double dy = y > 0 ? y : -y;
//			double dz = z > 0 ? z : -z;
//
//			if (dx > half_gridSize0) x = GridSize[0] - dx;
//			if (dy > half_gridSize1) y = GridSize[1] - dy;
//			if (dz > half_gridSize2) z = GridSize[2] - dz;
//            pair->distance = sqrt(x * x + y * y + z * z);
//		}
//	}

	void NtCollisionManager::pairInteract(double dt)
	{
		for (PairMap::iterator it = pairs->begin(), end= pairs->end(); it != end; ++it)
		{
			NtCellPair *pair = it->second;
			int b_ij = pair->b_ij = pair->distance > pair->sumRadius ? 0 : 1;
			if (b_ij == 0 || pair->distance == 0 || pair->distance == pair->sumRadius)continue;

			//divide by pair->distance is for normalize 
			double force = Phi1 * (1.0/pair->distance - 1.0/pair->sumRadius)/pair->distance;

			double *a_X = pair->a->X;
			double *b_X = pair->b->X;
			 
			double dx = (b_X[0] - a_X[0]) * force;
            double dy = (b_X[1] - a_X[1]) * force;
            double dz = (b_X[2] - a_X[2]) * force;

			double *a_F = pair->a->F;
			double *b_F = pair->b->F;
			a_F[0] -= dx;
			a_F[1] -= dy;
			a_F[2] -= dz;

			b_F[0] += dx;
			b_F[1] += dy;
			b_F[2] += dz;
		}
	}

	//this check if the pair is to be removed, if not, compute distance
	int NtCollisionManager::removeNonCriticalPairs()
	{
		int n = 0;
		if (!IsToroidal)
		{
			for (PairMap::iterator it = pairs->begin(), end= pairs->end(); it != end; ++it)
			{
				NtCellPair *pair = it->second;
				if (!ClearSeperation(pair) )
				{
					pair->set_distance();
				}
				else 
				{
					nodes_to_remove[n] = it->first;
					n++;
				}
			}
		}
		else 
		{
			for (PairMap::iterator it = pairs->begin(), end= pairs->end(); it != end; ++it)
			{
				NtCellPair *pair = it->second;
				if ( !ClearSeparaitonToroidal(pair) )
				{
					pair->set_distance_toroidal();
				}
				else 
				{
					nodes_to_remove[n] = it->first;
					n++;
				}
			}
		}

		if (n > max_removal)
		{
			max_removal = n;
			fprintf(stderr, "maximum removal = %d currnt map item count = %d\n", n, pairs->size());
		}

		for (int i=0; i< n; i++)
		{
			pairs->erase(nodes_to_remove[i]);
		}
		return n;
	}


}

