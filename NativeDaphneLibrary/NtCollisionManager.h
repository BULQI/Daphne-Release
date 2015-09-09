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

	#pragma warning (disable : 4251)

	class DllExport NtCollisionManager : public NtGrid
	{
	private:

		//note: using vector template here causing a compiler warning C4251
		//but does not affect the running.
		//to remove the warning, use another container
		//or other tricks. we are disabling it for now
		vector<NtCellPair *> FreePairList;

	public:
		static double *GridSize;
		static double GridStep;
		static bool IsToroidal;
		static double Phi1;

		static int max_pair_count;

		unordered_map<long, NtCellPair *> *pairs;

		int ReserveStorageSize;
		NtCellPair *PairArrayStorage;
		int NextFreeIndex;
		//vector<NtCellPair *> FreePairList;


		//thread stuff
		int numPairs;
		int MaxNumThreads;
		HANDLE* jobHandles;
		HANDLE* JobReadyEvents;
		HANDLE* JobFinishedEvents;

		PairInteractArg** pairInteractArgs;

		unsigned long AcitveJobCount;

		NtCollisionManager(double *gsize, double gstep, bool gtoroidal);
		
		~NtCollisionManager()
		{
			delete pairs;

			_aligned_free(PairArrayStorage);

			//stop threads
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

		//do we really have to add, there is nothing to do to add
		//except setting its pairkey? if key = -1, then it is a free one.
		bool addCellPair(long key, NtCellPair *p)
		{
			//if we already have it
			if (pairs->count(key) > 0)return false;
			pairs->insert(make_pair(key, p));
			
			//debug - checking maximum pairs
			int nPairs = NextFreeIndex - (int)FreePairList.size();
			if (nPairs > max_pair_count)
			{
				max_pair_count = nPairs;
				//if (max_pair_count%100 == 0)
				/*{
					fprintf(stdout, "Maximum pair count = %d\n", max_pair_count);
				}*/
			}
			return true;
		}

		NtCellPair *getPair(long key)
		{
			return (*pairs)[key];
		}

		NtCellPair *NewCellPair(long key, NtCell* _a, NtCell* _b);

		bool removePair(long key)
		{
			if (pairs->count(key) == 0)return false;
			NtCellPair *p = (*pairs)[key];
			p->pairKey = -1; //free slot now
			FreePairList.push_back(p);
			pairs->erase(key);
			return true;
		}

		////return if removed.
		//bool removePairOnClearSeparation(long key)
		//{
		//	if (pairs->count(key) == 0)return false;
		//	NtCellPair *pair = getPair(key);
		//	if (!IsToroidal)
		//	{
		//		if (ClearSeperation(pair) == true)
		//		{
		//			removePair(key);
		//			return true;
		//		}
		//	}
		//	else if (ClearSeperationToroidal(pair) == true)
		//	{
		//		removePair(key);
		//		return true;
		//	}
		//	return false;
		//}

		bool isEmpty()
		{
			return getPairCount() == 0;
		}

		void ClearPairs()
		{
			pairs->clear();
		}

		//fill holes in cell pair array
		void balance();

		//reset pair pointers
		NtCellPair* resetPair(long key, NtCell* _a, NtCell* _b);


	};
}

