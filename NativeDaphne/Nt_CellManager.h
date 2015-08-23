#pragma once

#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_Cell.h"
#include "Nt_CellPopulation.h"
#include "Nt_DArray.h"

#include "Nt_CollisionManager.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	ref class Nt_CellPopulation;

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellManager
	{
	
	public:

		Nt_CellManager(void)
		{
			//for gloable access
			cellPopulations = gcnew Dictionary<int, Nt_CellPopulation^>();


			//for reandom number generation
			normalDist = gcnew Nt_NormalDistribution();

			EnvironmentExtent = gcnew array<double>(3);
		}

		~Nt_CellManager(void){}

		void Clear()
		{
			cellDictionary->Clear();
		}

		void InitializeNormalDistributionSampler(double mean, double variance, int seed)
		{
			normalDist = gcnew Nt_NormalDistribution();
			normalDist->initialize(mean, variance, seed);
			IsDistributionSamplerInitialized = true;
		}

		void SetEnvironmentExtents(double extent0, double extent1, double extent2, bool do_boundary_force, double pair_phi1 )
		{
			EnvironmentExtent[0] = extent0;
			EnvironmentExtent[1] = extent1;
			EnvironmentExtent[2] = extent2;
			boundaryForceFlag = do_boundary_force;
			PairPhi1 = pair_phi1;
			IsEnvironmentInitialzed = true;
		}

		bool IsInitialized()
		{
			return IsEnvironmentInitialzed && IsDistributionSamplerInitialized;
		}

		static Nt_NormalDistribution ^normalDist;

		static array<double> ^EnvironmentExtent;

		//flag indicating if ECS and toroidal = false
		static bool boundaryForceFlag;

		//from Pair.Phil1 name of the parameter?
		static double PairPhi1;

		//this is for globle access in the middle layer.
		//contains all current cells, <cell_id, Nt_Cell^>
		static Dictionary<int, Nt_Cell^> ^cellDictionary = gcnew Dictionary<int, Nt_Cell^>();

		static array<Nt_Cell^>^ cellArray;

	private:

		//population_id -> cell population
		Dictionary<int, Nt_CellPopulation^> ^cellPopulations;

		bool IsEnvironmentInitialzed;

		bool IsDistributionSamplerInitialized;
	};
}

