#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif


namespace NativeDaphneLibrary
{
	class DllExport Utility
	{
	public:

		//same as Daxpy
		static int NtDaxpy(int n, double alpha, double *x, int incx,  double *y, int incy);


		static int NtDscal(int n, double alpha, double *x, int incx);

		/* 
		y = alpha * x + y
		x = -alpha *x + x;
		*/
		static int NtDoubleDaxpy(int n, double alpha, double *x, double *y);

		/*scalar multiply
		  n - total number of elemnts
		  inc - each scalar component contains inc elements
		  result saved in x
		*/
		static int NtMultiplyScalar(int n, int inc, double *x, double *y, double *z);

		static int Utility::NtDcopy(int n, double* x, int incx, double *y, int incy);


		static int Utility::acml_getnumthreads();

		static void Utility::acml_setnumthreads(int numthreads);

		static void Utility::test_performance();


	};
}



