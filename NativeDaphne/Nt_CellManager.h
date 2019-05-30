/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#pragma once

#include "NtUtility.h"
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
			EnvironmentExtent = gcnew array<double>(3);
			normalDist = NULL;
		}

		~Nt_CellManager(void)
		{
			this->!Nt_CellManager();
		}


		!Nt_CellManager()
		{
			if (normalDist != NULL)
			{
				delete normalDist;
				normalDist = NULL;
			}

		}
		void Clear()
		{
			cellDictionary->Clear();
			if (normalDist != NULL)
			{
				delete normalDist;
				normalDist = NULL;
				IsDistributionSamplerInitialized = true;
			}

		}

		void InitializeNormalDistributionSampler(double mean, double variance, int seed)
		{
			normalDist = new NativeDaphneLibrary::NtNormalDistribution();
			normalDist->Initialize(seed, mean, variance);
			IsDistributionSamplerInitialized = true;
		}

		void SetEnvironmentExtents(double extent0, double extent1, double extent2, bool ecs_flag, bool toroidal_flag, double pair_phi1 )
		{
			EnvironmentExtent[0] = extent0;
			EnvironmentExtent[1] = extent1;
			EnvironmentExtent[2] = extent2;

			ECS_flag = ecs_flag;
			ECS_IsToroidal = toroidal_flag;
			boundaryForceFlag = ECS_flag && !ECS_IsToroidal;
			PairPhi1 = pair_phi1;
			IsEnvironmentInitialzed = true;
		}

		bool IsInitialized()
		{
			return IsEnvironmentInitialzed && IsDistributionSamplerInitialized;
		}

		static NtNormalDistribution* normalDist;

		static array<double> ^EnvironmentExtent;

		static bool ECS_flag;

		static bool ECS_IsToroidal;

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

