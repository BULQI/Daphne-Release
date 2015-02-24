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
	class DllExport MTRandomNumberGenerator
	{
	public:

		MTRandomNumberGenerator();

		~MTRandomNumberGenerator();

		bool Initialize(int seed);
		
		//generate random number with normal distribution
		//reutrn true if successful, false if failed.
		bool Sample(int n, double mean, double variance, double *x);

	private:

		int *_mseed;
		int *_state;
		int info;
		bool initialized;
	};
}