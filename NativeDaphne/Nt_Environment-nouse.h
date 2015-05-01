#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Environment
	{
	public:
		Nt_Environment(void)
		{
			
			normalDist = gcnew Nt_NormalDistribution();
			EnvironmentExtent = gcnew array<double>(3);
		}

		~Nt_Environment(void){}
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

	private:
		bool IsEnvironmentInitialzed;

		bool IsDistributionSamplerInitialized;
	};
}

