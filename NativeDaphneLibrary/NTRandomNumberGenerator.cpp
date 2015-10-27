
#include "stdafx.h"
#include <process.h>
#include "NtUtility.h"
#include "NTRandomNumberGenerator.h"
#include <stdexcept>
#include <acml.h>

using namespace std;

namespace NativeDaphneLibrary
{


	NtNormalDistribution::NtNormalDistribution()
	{
		buffer = NULL;
		buffer_backup = NULL;
		nextBatchPtr = NULL;
		buffer_size = 10000;
		backup_ready = 0;
		thread_done = 0;
		JobReadyEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	}

	NtNormalDistribution::~NtNormalDistribution()
	{
		if (initialized)
		{
			//stop the thread
			::InterlockedExchange(&thread_done, 1);
			::SetEvent(this->JobReadyEvent);
			WaitForSingleObject(ThreadHandle, INFINITE);
			fprintf(stdout, "Info: Random number generator thread exited.\n");

			free(_mseed);
			free(_state);
			_aligned_free(buffer);
			_aligned_free(buffer_backup);
		}
	}

	//Mersenne Twister needs 624 seed values
	//mLSeed = 624;
	//Mersenne Twister has 633 states
	//mLState = 633;
	bool NtNormalDistribution::Initialize(int seed, double _mean, double _variance)
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

		mean = _mean;
		variance = _variance;
		initialize_buffer();
		initialized = true;
		return info == 0;
	}

	void NtNormalDistribution::initialize_buffer()
	{
		int alloc_size = buffer_size;
		buffer = (double *)_aligned_realloc(buffer, alloc_size * sizeof(double), 32);
		buffer_backup = (double *)_aligned_realloc(buffer_backup, alloc_size * sizeof(double), 32);
		if (buffer == NULL || buffer_backup == NULL)
		{
			throw new exception("Error allocating random number buffer.\n");
		}
		drandgaussian(buffer_size, mean, variance, _state, buffer, &info);
		if (info != 0)
		{
			throw new exception("Error generating random number");
		}
		drandgaussian(buffer_size, mean, variance, _state, buffer_backup, &info);
		if (info != 0)
		{
			throw new exception("Error generating random number");
		}

		backup_ready = 1;
		nextBatchPtr = buffer;
		//start the thread
		unsigned int tid;
		fprintf(stdout, "creating thread, thread_done=%d\n", thread_done);
		ThreadHandle = (HANDLE)_beginthreadex(0, 0, thread_entry, this, 0, &tid);
	}

	bool NtNormalDistribution:: create_reandom_number_backup()
	{
		drandgaussian(buffer_size, mean, variance, _state, buffer_backup, &info);
		if (info != 0)
		{
			throw new exception("Error generating random number");
		}
		return true;
	}

	//generate random number with normal distribution
	//reutrn true if successful, false if failed.
	bool NtNormalDistribution::Sample(int n, double *x)
	{
		/* extern void drandgaussian(int n, double xmu, double var, int *state, double *x, int *info) */
		drandgaussian(n, mean, variance, _state, x, &info);
		return info == 0;
	}

	//note, the currently returned pointer is NOT 32 byte aligned.
	//if needed, can skip some number to ensure 32 byte alignment.
	//get an array pointer containing n random numbers
	double *NtNormalDistribution::GetSample(int n)
	{
		//if requested number is larege, adjust to buffer size to 4 times of the requested number
		if (n * 4 > buffer_size)
		{
			buffer_size = n * 8;
			::InterlockedExchange(&thread_done, 1);
			::SetEvent(this->JobReadyEvent);
			WaitForSingleObject(ThreadHandle, INFINITE);
			::InterlockedExchange(&thread_done, 0);
			initialize_buffer();
		}
		//if we don't have nough numbers in the buffer
		if ( buffer + buffer_size - nextBatchPtr < n)
		{
			//check if backup is ready, if not, wait
			if (backup_ready == 0)
			{
				fprintf(stdout, "random number store not ready.\n");
				while (::InterlockedCompareExchange(&backup_ready, 0, 1) != 1);
				fprintf(stdout, "random number generated ok.\n");
			}

			//now swtch the buffer_buffer and buffer, and request another fill
			double *tmp = buffer;
			buffer = buffer_backup;
			buffer_backup = tmp;
			nextBatchPtr = buffer;
			backup_ready = 0;
			::SetEvent(this->JobReadyEvent);
		}

		double *retval = nextBatchPtr;
		nextBatchPtr+= n;
		return retval;
	}
}