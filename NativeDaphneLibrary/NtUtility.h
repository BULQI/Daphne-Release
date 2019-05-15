/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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



