#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>

#include "NtUtility.h"
#include <vcclr.h>
#include "Nt_NormalDist.h"

namespace NativeDaphne
{



	Nt_NormalDistribution::Nt_NormalDistribution(){}


	void Nt_NormalDistribution::initialize(double m, double v, int seed)
	{
		mean = m;
		variance = v;
		sampling_source = new NativeDaphneLibrary::NTRandomNumberGenerator();
		bool init_ok = sampling_source->Initialize(seed);
	}

	bool Nt_NormalDistribution::Sample(int n, double *sample)
	{
		bool status = sampling_source->Sample(n, mean, variance, sample);
		return status;
	}

	void Nt_NormalDistribution::test()
	{
		double *tmp = (double *)malloc(1000 *sizeof(double));
		bool status = sampling_source->Sample(1000, 0, 1.0, tmp);
		for (int i=0; i< 1000; i++)
		{
			double val = tmp[i];
			Console::WriteLine(val);
		}
	}

}
