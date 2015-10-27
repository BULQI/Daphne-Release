#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>
#include <stdio.h>
#include <xmmintrin.h>
#include <emmintrin.h>

namespace NativeDaphneLibrary
{
	class DllExport NtUtility
	{
	public:

		/******************************** 
		 * y = alpha * x + y
		 * x = -alpha *x + x;
		 *********************************/
		//static int NtDoubleDaxpy(int n, double alpha, double *x, double *y);

		/*scalar multiply
		  n - total number of elemnts
		  inc - each scalar component contains inc elements
		  result saved in x
		*/
		static int MomentExpansion_NtMultiplyScalar(int n, double *x, double *y);



		//for same transduction constnat
		static int cell_apply_chemotactic_force(int n, double tc, double *dconc, double *_F);

		//for different transdcution constant
		static int cell_apply_chemotactic_force2(int n, double *tc, double *dconc, double *_F);
		

		static int cell_apply_boundary_force(int n, double *_x, double *ECSExtentLimit, double radius, double PairPhi1, double *_F);

		static int TinyBall_laplacian(int n, double alpha, double *sf, double *laplacian);

		static int TinyBall_DiffusionFluxTerm(int n, double alpha, double *flux, double *dst);

		//compute memory size to be allocted, given data size
		//the memory doubles when not enough
		static int GetAllocSize(int n, int curSize)
		{
			int size = curSize == 0 ? 1: curSize;
			while (n > size)size *= 2;
			return size;
		}

		static int mem_zero_d(double*dst, int count);

		static int mem_copy_d(double *dst, double *src, int count);

#define USE_SSE
		static void AddDoubleArray(double *a, double *b, int length)
		{
#if defined(USE_SSE)
			int n = (length >> 1)<<1;
			for (int i=0; i< n; i+= 2)
			{
				__m128d v0 = _mm_load_pd(&a[i]);
				__m128d v1 = _mm_load_pd(&b[i]);
				__m128d c = _mm_add_pd(v0, v1);
				_mm_store_pd(&a[i], c);
			}
			if (n < length)
			{
				a[n] += b[n];
			}

#else
			double *s = a + length;
			while (a < s)
			{
				*a += *b;
				a++;
				b++;
			}
#endif
		}


	};
}



