#include "stdafx.h"
#include "NtUtility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <unordered_map>
#include <xmmintrin.h>
#include <emmintrin.h>

#include "NtCollisionManager.h"

using namespace std;
typedef list<int> LISTINT;

namespace NativeDaphneLibrary
{
	double *NtCollisionManager::GridSize = NULL;
	double NtCollisionManager::GridStep = 0;
	bool NtCollisionManager::IsToroidal = false;
	double NtCollisionManager::Phi1 = 0;
	int NtCollisionManager::max_pair_count = 0;

	typedef std::unordered_map<int, NtCellPair *> PairMap;


	NtCollisionManager::NtCollisionManager(double *gsize, double gstep, bool gtoroidal) : NtGrid(gsize, gstep, gtoroidal)
	{
			GridStep = gstep;
			IsToroidal = gtoroidal;
			GridSize = gridSize;

			pairs = new unordered_map<long, NtCellPair *>();
			max_pair_count = 0;


			//handling storage
			ReserveStorageSize = 1000000;
			PairArrayStorage = (NtCellPair *)_aligned_malloc(ReserveStorageSize * sizeof(NtCellPair), 64);
			NextFreeIndex = 0;
						
			//thread stuff
			MaxNumThreads = acmlgetnumthreads()-2; 
			if (MaxNumThreads <= 0)MaxNumThreads = 1;

			jobHandles = (HANDLE *)malloc(MaxNumThreads * sizeof(HANDLE));
			JobReadyEvents = (HANDLE *)malloc(MaxNumThreads * sizeof(HANDLE));

			pairInteractArgs = (PairInteractArg **)malloc(MaxNumThreads * sizeof(PairInteractArg*));
			for (int i=0; i< MaxNumThreads; i++)
			{
				unsigned int tid;
				pairInteractArgs[i] = new PairInteractArg();
				pairInteractArgs[i]->owner = this;
				pairInteractArgs[i]->threadId = i;
				pairInteractArgs[i]->n = 0;
				JobReadyEvents[i] = CreateEvent(NULL, FALSE, FALSE, NULL);
				jobHandles[i] = (HANDLE)_beginthreadex(0, 0, &PairInteractThreadEntry, pairInteractArgs[i], 0, &tid);
			}

	}

	void NtCollisionManager::pairInteract(double dt)
	{

		if (IsToroidal)
		{
			pairInteractToroidal(dt);
			return;
		}
		pairInteractEx(0, numPairs, dt);
	}

	void NtCollisionManager::pairInteractToroidal(double dt)
	{
		NtCellPair *pairArray = PairArrayStorage;
		for (int i=0; i<numPairs; i++)
		{
			NtCellPair &pair = pairArray[i];
			pair.set_distance_toroidal();
			if (pair.distance == 0 || pair.distance >= pair.sumRadius)continue;

			//divide by pair->distance is for normalize 
			double force = Phi1 * (1.0/pair.distance - 1.0/pair.sumRadius)/pair.distance;

			double *a_X = pair.X1;
			double *b_X = pair.X2;
			 
			double dx = (b_X[0] - a_X[0]) * force;
            double dy = (b_X[1] - a_X[1]) * force;
            double dz = (b_X[2] - a_X[2]) * force;

			double *a_F = pair.F1;
			double *b_F = pair.F2;
			a_F[0] -= dx;
			a_F[1] -= dy;
			a_F[2] -= dz;

			b_F[0] += dx;
			b_F[1] += dy;
			b_F[2] += dz;
		}
	}

	//non-toroidal only
	void NtCollisionManager::pairInteractEx(int start_index, int n, double dt)
	{
		double sum_squares = 0;
		double dx, dy, dz;

		NtCellPair *pairArray = PairArrayStorage;
		for (int i=start_index, end=start_index+n; i < end; ++i)
		{
			//testing
			//_mm_prefetch((char *)pairArray[i+1].X1, _MM_HINT_T0);
			//_mm_prefetch((char *)pairArray[i+1].X2, _MM_HINT_T0);

			NtCellPair &pair = pairArray[i];

			double *a_X = pair.X1;
			double *b_X = pair.X2;

			dx = b_X[0] - a_X[0];
			dy = b_X[1] - a_X[1];
			dz = b_X[2] - a_X[2];
			sum_squares = dx * dx + dy * dy + dz * dz;

			if (sum_squares > pair.sumRadius2 || sum_squares == 0)continue;

			double dist_inverse = 1.0/sqrt(sum_squares);
			double force = Phi1 * (dist_inverse - 1.0 /pair.sumRadius) * dist_inverse; 

			dx *= force;
            dy *= force;
            dz *= force;

			double *a_F = pair.F1;
			double *b_F = pair.F2;
			a_F[0] -= dx;
			a_F[1] -= dy;
			a_F[2] -= dz;

			b_F[0] += dx;
			b_F[1] += dy;
			b_F[2] += dz;
		}
	}

	//Compute mu for burn in step
	//see Tom's burn in algorithm in Simulation.cs(line 1173) for detail
	double NtCollisionManager::getBurnInMuValue(double integratorStep)
	{
		double f_max = 0;
		NtCellPair *pmax = NULL;

		NtCellPair *pairArray = PairArrayStorage;
		for (int i=0; i<numPairs; i++)
		{
			NtCellPair &pair = pairArray[i];
			if (IsToroidal)
			{
				pair.set_distance_toroidal();
			}
			else 
			{
				pair.set_distance();
			}
			if (pair.distance == 0 || pair.distance >= pair.sumRadius)continue;
			double force = Phi1 * (1.0/pair.distance - 1.0/pair.sumRadius);
			if (force > f_max)
			{
				f_max = force;
				pmax = &pair;
			}
		}
		double mu = 0;
		if (pmax != NULL)
		{
			 // find mu such that it allows maximally 10% of (r1 + r2) movement for the pair with f_max
			mu = 0.1 * pmax->sumRadius/(f_max * integratorStep);
		}
		return mu;
	}

	int NtCollisionManager::MultiThreadPairInteract(double dt)
	{

		int n = numPairs;
		int numThreads = MaxNumThreads;
		int NumItemsPerThread = n /(numThreads + 2);
		if (NumItemsPerThread < 100)
		{
			NumItemsPerThread = 100;
			numThreads = n/100 - 2;
			if (numThreads < 0)numThreads = 0;
		}

		//fprintf(stderr, "+++++ ready to run pair interact ++++ \n");
		//temparily disable thread
		//numThreads = 0;
		//start job
		::InterlockedExchange(&AcitveJobCount, numThreads);
		int n0, nn;
		n0 = nn = n - NumItemsPerThread * numThreads;
		for (int i=0; i< numThreads; i++)
		{
			PairInteractArg *arg = pairInteractArgs[i];
			arg->start_index = nn;
			arg->n = NumItemsPerThread;
			nn += NumItemsPerThread;
			::SetEvent(JobReadyEvents[i]);
		}

		pairInteractEx(0, n0, dt);
		//wait for job finish
		if (numThreads > 0)
		{
			while (::InterlockedCompareExchange(&AcitveJobCount, 1, 0) != 0);
		}
		return 0;
	}

	NtCellPair* NtCollisionManager::NewCellPair(long key, NtCell* _a, NtCell* _b)
	{
		NtCellPair* cellpair = NULL;
		unsigned count = FreePairList.size();
		if (count > 0)
		{
			cellpair = FreePairList[count-1];
			FreePairList.pop_back();
		}
		else 
		{
			if (NextFreeIndex >= ReserveStorageSize)
			{
				return NULL; //or allocate more?
			}

			cellpair = &PairArrayStorage[NextFreeIndex];
			NextFreeIndex++;
		}
		return new (cellpair) NtCellPair(key, _a, _b);
	}

	void NtCollisionManager::balance()
	{
		
		unsigned count = FreePairList.size();
		if (count == 0)return;

		NtCellPair *pairArray = PairArrayStorage;
		for (int i=0; i< count; i++)
		{
			NtCellPair* cellpair = FreePairList[i];
			int index = (int)(cellpair - pairArray);
			int last_used_index = NextFreeIndex-1;
			//make sure the last slot is not free one.
			while (last_used_index >= 0 && pairArray[last_used_index].pairKey == -1)
			{
				last_used_index--;
				NextFreeIndex--;
			}
			//now index cannpt be equal to (NextFreeIndex-1) because that would have been 
			//shinked in the above step, so we only need to deal with the ones that
			//is less than NextFreeIndex-1, anyting beger would have been handled also!
			if (index < NextFreeIndex-1)
			{
				cellpair->copy(&pairArray[NextFreeIndex-1]);
				if (pairs->count(cellpair->pairKey) > 0)
				{
					(*pairs)[cellpair->pairKey] = cellpair;
				}
				NextFreeIndex--;
			}
		}
		numPairs = NextFreeIndex;
		//if (numPairs != (int)pairs->size())
		//{
		//	int error = 1;
		//}

		//for (int i=0; i<numPairs; i++)
		//{
		//	if (itemExists(pairArray[i].pairKey) == false)
		//	{
		//		int error = 2;
		//	}
		//}
		FreePairList.clear();
	}

	NtCellPair* NtCollisionManager::resetPair(long key, NtCell* _a, NtCell* _b)
	{
		NtCellPair *pair = (*pairs)[key];
		if (pair == NULL)
		{
			throw new exception("Error: pair with key not found!");
		}
		return new (pair) NtCellPair(key, _a, _b);
	}
}

