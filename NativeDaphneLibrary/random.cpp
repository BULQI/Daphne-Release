
#include "stdafx.h"
#include "Utility.h"
#include "random.h"
#include <stdexcept>
#include <acml.h>

using namespace std;

namespace NativeDaphneLibrary
{


	MTRandomNumberGenerator::MTRandomNumberGenerator(){}

	MTRandomNumberGenerator::~MTRandomNumberGenerator()
	{
		if (initialized)
		{
			free(_mseed);
			free(_state);
		}
	}

	//Mersenne Twister needs 624 seed values
	//mLSeed = 624;
	//Mersenne Twister has 633 states
	//mLState = 633;

	bool MTRandomNumberGenerator::Initialize(int seed)
	{

		int lseeds = 624;
		int lstate = 633;
		fprintf(stderr, "in intialize random number generator\n");
		_mseed = (int *)malloc(624 * sizeof(int));
		//mimicking the way math.net numberics to generate the seeds
		//hopefully they will geneate same sequence.
		//see https://github.com/mathnet/mathnet-numerics/blob/master/src/Numerics/Random/MersenneTwister.cs
		//unforturnately we cannot do that, because ACML expects positive integer seeds

		srand(seed);
		for (int i = 0; i < 624; i++)
		{
			_mseed[i] = rand() + 1;
		}

		_state = (int *)malloc(633 * sizeof(int));

		/* Initialize the base generator */
		drandinitialize(3, 0, _mseed, &lseeds, _state, &lstate, &info);
		initialized = true;
		return info == 0;
	}

	//generate random number with normal distribution
	//reutrn true if successful, false if failed.
	bool MTRandomNumberGenerator::Sample(int n, double mean, double variance, double *x)
	{
		/* extern void drandgaussian(int n, double xmu, double var, int *state, double *x, int *info) */
		drandgaussian(n, mean, variance, _state, x, &info);
		return info == 0;
	}

}