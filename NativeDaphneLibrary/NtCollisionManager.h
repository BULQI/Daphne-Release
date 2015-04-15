#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>
#include <unordered_map>
#include <xmmintrin.h>
#include "NtGrid.h"
#include "NtCellPair.h"

using namespace std;

namespace NativeDaphneLibrary
{

	class DllExport NtCollisionManager : public NtGrid
	{
	public:
		static double *GridSize;
		static double GridStep;
		static bool IsToroidal;
		static double Phi1;

		static int max_pair_count;

		unordered_map<int, NtCellPair *> *pairs;
		//int *nodes_to_remove;

		NtCollisionManager(double *gsize, double gstep, bool gtoroidal) : NtGrid(gsize, gstep, gtoroidal)
		{

			GridSize = gsize;
			GridStep = gstep;
			IsToroidal = gtoroidal;
			GridSize = gridSize;

			pairs = new unordered_map<int, NtCellPair *>();
			//hard coded for now.
			//nodes_to_remove = (int *)malloc(50000*sizeof(int));
			max_pair_count = 0;
		}

		~NtCollisionManager()
		{
			if (GridSize != NULL)
			{
				//free(GridSize);
				GridSize = NULL;
			}
			//delete pairs;
			//free(nodes_to_remove);
		}

		//void updateExistingPairs();

		void pairInteract(double dt);

		void pairInteractToroidal(double dt);

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
	};
}

