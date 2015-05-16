#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>
#include <stdio.h>
#include <xmmintrin.h>

namespace NativeDaphneLibrary
{
	class DllExport NtUtility
	{
	public:

		/******************************** 
		 * y = alpha * x + y
		 * x = -alpha *x + x;
		 *********************************/
		static int NtDoubleDaxpy(int n, double alpha, double *x, double *y);

		/*scalar multiply
		  n - total number of elemnts
		  inc - each scalar component contains inc elements
		  result saved in x
		*/
		static int NtMultiplyScalar(int n, int inc, double *x, double *y, double *z);


		//compute memory size to be allocted, given data size
		//the memory doubles when not enough
		static int GetAllocSize(int n, int curSize)
		{
			int size = curSize == 0 ? 1: curSize;
			while (n > size)size *= 2;
			return size;
		}

		static void AddDoubleArray(double *a, double *b, int length)
		{
			double *s = a + length;
			while (a < s)
			{
				*a += *b;
				a++;
				b++;
			}
		}


	};
}



