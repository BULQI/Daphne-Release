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
		unordered_map<int, NtCellPair *> *pairs;

		NtCellPair **pairArray;
		//testing thread stuff
		//thread stuff
		int MaxNumThreads;
		HANDLE* jobHandles;
		HANDLE* JobReadyEvents;
		HANDLE* JobFinishedEvents;

		PairInteractArg** pairInteractArgs;
		unsigned long AcitveJobCount;


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
			
			//WaitForMultipleObjects(MaxNumThreads, jobHandles, true, INFINITE);
			//for (int i=0; i <MaxNumThreads; i++)
			//{
			//	CloseHandle(jobHandles[i]);
			//}

			//close events too.

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

		//this is implemented for non-toroidal for testing...
		void pairInteractEx(int start_index, int num_items, double dt);

		void pairInteractToroidal(double dt);

		int MultiThreadPairInteract(double dt);

		//this check if a pair needs to ber removed.
		//int removeNonCriticalPairs();

		int getPairCount()
		{
			return pairs->size();
		}

		bool itemExists(int key)
		{
			return (pairs->count(key) > 0);
		}

		bool addCellPair(int key, NtCellPair *p)
		{
			if (pairs->count(key) > 0)return false;
			pairs->insert(make_pair(key, p));
			return true;
		}

		NtCellPair *getPair(int key)
		{
			return (*pairs)[key];
		}

		bool removePair(int key)
		{
			if (pairs->count(key) == 0)return false;
			pairs->erase(key);
			return true;
		}

		int iterator_test1()
		{
			double max = 0;
			int n = pairs->size();
			for (int i=0; i< 100; i++)
			{
				double max = 0;
				for (int j = 0; j< n; j++)
				{
					max += pairArray[j]->distance;
				}
			}
			return max;
		}

		int iterator_test2()
		{
			for (int i=0; i< 10; i++)
			{
				double max = 0;
				for (PairMap::iterator it = pairs->begin(), end = pairs->end(); it != end; ++it)
				{
					max += it->second->distance;
				}
			}
			return 1;
		}

		//return if removed.
		bool removePairOnClearSeparation(int key)
		{
			if (pairs->count(key) == 0)return false;
			NtCellPair *pair = getPair(key);
			if (!IsToroidal)
			{
				if (ClearSeperation_nobond(pair) == true)
				{
					pairs->erase(key);
					return true;
				}
			}
			else if (ClearSeperationToroidal(pair->a->gridIndex, pair->b->gridIndex, pair->MaxSeparation) == true)
			{
				pairs->erase(key);
				return true;
			}
			return false;
		}

		bool isEmpty()
		{
			return pairs->size() == 0;
		}

		void ClearPairs()
		{
			pairs->clear();
		}

		//this update the pairArray after insertion/deletion of entry.
		void update_iterator()
		{
			int n= 0;
			for (PairMap::iterator it = pairs->begin(), end = pairs->end(); it != end; ++it)
			{
				pairArray[n] = it->second;
				n++;
			}
		}

	};
}

