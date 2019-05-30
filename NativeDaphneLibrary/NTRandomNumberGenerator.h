#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>

namespace NativeDaphneLibrary
{

	//this class generates random number with normal distrubtuion 
	//using Mersenne Twister generator from the acml library
	class DllExport NtNormalDistribution
	{
	public:

		NtNormalDistribution();

		~NtNormalDistribution();

		bool Initialize(int seed, double _mean, double _variance);
		
		//generate random number with normal distribution
		//reutrn true if successful, false if failed.
		bool Sample(int n, double *x);

		//get a pointer to the buffer with n randome numbers.
		double* GetSample(int n);

		
		//thread stuff
		HANDLE JobReadyEvent;


		volatile unsigned long backup_ready;

		//for stop the thread
		volatile unsigned long thread_done;
				
		//create backup store for reandom numbers
		bool create_reandom_number_backup();

	private:

		void initialize_buffer();

		static unsigned __stdcall thread_entry(void *data)
		{
			NtNormalDistribution *owner = (NtNormalDistribution *)data;
			while (true)
			{
				WaitForSingleObject(owner->JobReadyEvent, INFINITE);
				if (owner->thread_done == 1)return 0; //thead is done
				owner->create_reandom_number_backup();
				//indicate the backup store is ready to use
				::InterlockedExchange(&owner->backup_ready, 1);
			}
		}

		int *_mseed;
		int *_state;
		int info;
		bool initialized;

		double mean;
		double variance;

		//parameters used to generate random number
		//using another thread.
		double *buffer;
		double *buffer_backup;
		double *nextBatchPtr;
		int buffer_size;
		HANDLE ThreadHandle;
	};
}