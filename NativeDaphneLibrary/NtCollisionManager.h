#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>
#include <unordered_map>
#include <xmmintrin.h>
#include <process.h>
#include "NtGrid.h"
#include "NtCellPair.h"

using namespace std;

namespace NativeDaphneLibrary
{

	typedef std::unordered_map<int, NtCellPair *> PairMap;

	class NtCollisionManager;
	class DllExport PairInteractArg
	{
	public:
		NtCollisionManager *owner;
		int start_index;
		int n; //number of items
		double dt; 
		int threadId;
	};

	class DllExport NtCollisionManager : public NtGrid
	{
	public:
		static double *GridSize;
		static double GridStep;
		static bool IsToroidal;
		static double Phi1;

		static int max_pair_count;

		unordered_map<long, NtCellPair *> *pairs;

		//testing thread stuff
		NtCellPair **pairArray;
		int numPairs;
		int MaxNumThreads;
		HANDLE* jobHandles;
		HANDLE* JobReadyEvents;
		HANDLE* JobFinishedEvents;

		PairInteractArg** pairInteractArgs;
		//unsigned long AcitveJobCount;
		volatile long AcitveJobCount;


		NtCollisionManager(double *gsize, double gstep, bool gtoroidal);
		
		~NtCollisionManager()
		{
			if (GridSize != NULL)
			{
				//free(GridSize);
				GridSize = NULL;
			}

			delete pairs;

			//step threads
			for (int i=0; i<MaxNumThreads; i++)
			{
				PairInteractArg *arg = pairInteractArgs[i];
				arg->n = -1;
				::SetEvent(JobReadyEvents[i]);
			}
		}

		static unsigned __stdcall PairInteractThreadEntry(void* pUserData) 
		{
			PairInteractArg *arg = (PairInteractArg *)pUserData;
			int tid = arg->threadId;
			NtCollisionManager *owner = arg->owner;

			while (true)
			{
				WaitForSingleObject(owner->JobReadyEvents[tid], INFINITE); 
				if (arg->n == -1) //signal to end thread
				{
					_endthread();
				}
				arg->owner->pairInteractEx(arg->start_index, arg->n, arg->dt);
				//fprintf(stderr, "pari_interact thread %d run once\n", arg->threadId);
				::InterlockedDecrement(&owner->AcitveJobCount);
			}
		}


		//void updateExistingPairs();

		void pairInteract(double dt);

		void pairInteractToroidal(double dt);

		//this is implemented for non-toroidal for testing...
		void pairInteractEx(int start_index, int num_items, double dt);

		double NtCollisionManager::getBurnInMuValue(double dt);

		int MultiThreadPairInteract(double dt);

		int getPairCount()
		{
			return (int)pairs->size();
		}

		bool itemExists(long key)
		{
			return (pairs->count(key) > 0);
		}

		bool addCellPair(long key, NtCellPair *p)
		{
			//if we already have it
			if (pairs->count(key) > 0)return false;
			pairs->insert(make_pair(key, p));

			p->index = numPairs;
			pairArray[numPairs] = p; 
			numPairs++;
			if (numPairs >= 1000000)
			{
				throw std::runtime_error("Error in AddCellPair: allocated array size full");
			}

			if (numPairs > max_pair_count)
			{
				max_pair_count = numPairs;
				if (max_pair_count%100 == 0)
				{
					//fprintf(stdout, "Maximum pair count = %d\n", max_pair_count);
				}
			}
			return true;
		}

		NtCellPair *getPair(long key)
		{
			return (*pairs)[key];
		}

		NtCellPair *NewCellPair(NtCell* _a, NtCell* _b);

		void removePairFromArray(NtCellPair *p)
		{
			int index = p->index;
			if (index < 0 || index >= numPairs)
			{
				throw std::invalid_argument("Error in RemovePairFromArray: index out of range");
			}
			pairArray[index] = pairArray[numPairs-1];
			pairArray[index]->index = index;
			p->index = -1;
			numPairs--;
		}

		bool removePair(long key)
		{
			if (pairs->count(key) == 0)return false;
			NtCellPair *p = (*pairs)[key];
			pairs->erase(key);
			removePairFromArray(p);
			_aligned_free(p);
			return true;
		}

		//return if removed.
		bool removePairOnClearSeparation(long key)
		{
			if (pairs->count(key) == 0)return false;
			NtCellPair *pair = getPair(key);
			if (!IsToroidal)
			{
				if (ClearSeperation_nobond(pair) == true)
				{
					removePair(key);
					return true;
				}
			}
			else if (ClearSeperationToroidal(pair->a->gridIndex, pair->b->gridIndex, pair->MaxSeparation) == true)
			{
				removePair(key);
				return true;
			}
			return false;
		}

		bool isEmpty()
		{
			return numPairs == 0;
		}

		void ClearPairs()
		{
			pairs->clear();
			numPairs = 0;
		}

		

	};
}

