#pragma once

#include "random.h"

using namespace System;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_NormalDistribution
	{
	public:
		Nt_NormalDistribution();

		void initialize(double _mean, double _variance, int seed);

		bool Sample(int n, double* samples);

		void test();

	private:
		double mean;
		double variance;
		NativeDaphneLibrary::MTRandomNumberGenerator *sampling_source;
		
	};

}

