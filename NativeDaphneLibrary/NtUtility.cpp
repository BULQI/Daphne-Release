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
#include "stdafx.h"
#include "NtUtility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

//testing avx
#include <immintrin.h>

namespace NativeDaphneLibrary
{
	
	//int NtUtility::NtDoubleDaxpy(int n, double alpha, double *x, double *y)
	//{
	//	daxpy(n, alpha, x, 1, y, 1);	//y = y + x*alpha
	//	dscal(n, (1-alpha), x, 1);		//x = x - alpa*x = (1-a)*x
	//	return 0;
	//}

	//multiply two scalars and save result in z, inc = 4
	int NtUtility::MomentExpansion_NtMultiplyScalar(int n, double *x, double *y)
	{
#if defined(USE_SSE)
		__m128d s1, s2, v0, v1;
		for (int i=0; i<n; i+= 4)
		{
			s1 = _mm_set1_pd(x[i]);
			s2 = _mm_set1_pd(y[i]);

			v0 = _mm_load_pd(&x[i]);
			v0 = _mm_mul_pd(v0, s2);
			v1 = _mm_set_pd(y[i+1], 0); //(r0 = 0.0, r1=y[i+1])
			v1 = _mm_mul_pd(v1, s1);
			v1 = _mm_add_pd(v0, v1);
			_mm_store_pd(&x[i], v1);

			v0 = _mm_load_pd(&x[i+2]);
			v0 = _mm_mul_pd(v0, s2);
			v1 = _mm_load_pd(&y[i+2]);
			v1 = _mm_mul_pd(v1, s1);
			v1 = _mm_add_pd(v0, v1);
			_mm_store_pd(&x[i+2], v1);
		}
#else

		for (int i=0; i<n; i+= 4)
		{
			double s1 = x[i];
			double s2 = y[i];
			x[i] = x[i] * y[i];
			for (int j = i+1, k = i+4; j< k; j++)
			{
				x[j] = x[j] * s2 + s1 * y[j];
			}
		}
#endif
		return 0;
	}

	//this is used to update chemotactic forces
	//array_length, TransductionConstant, driverConc, _F);
	//x - driverConc, each cell has 4 components, we are using the graident part, i.e. x[1],x[2], x[3]
	//y - is the force part , in the format of y[0], y[1], y[2];  (y[3] is empty for proper alignment
	int NtUtility::cell_apply_chemotactic_force(int n, double tc, double *dconc, double *_F)
	{
		double *fstop = _F + n;
		while (_F < fstop)
		{
			_F[0] += tc * dconc[1];
			_F[1] += tc * dconc[2];
			_F[2] += tc * dconc[3];
			dconc += 4;
			_F += 4;
		}
		return 0;
	}

	//apply chemotatic force when Transduction Constant are different
	//tc - transduciton constant
	//dconc - driver concentration
	int NtUtility::cell_apply_chemotactic_force2(int n, double *tc, double *dconc, double *_F)
	{
		double *fstop = _F + n;
		while (_F != fstop)
		{
			_F[0] += tc[0] * dconc[1];
			_F[1] += tc[0] * dconc[2];
			_F[2] += tc[0] * dconc[2];
			_F += 4;
			tc += 4;
			dconc += 4;
		}
		return 0;
	}

	int NtUtility::cell_apply_boundary_force(int n, double *_X, double *ECSExtentLimit, double radius, double PairPhi1, double *_F)
	{
		double *xstop = _X + n;
		double radius_constant = PairPhi1 / radius;
		double dist;
		while (_X != xstop)
		{
			for (int i=0; i<3; i++, _X++, _F++)
			{
				if (*_X < radius && *_X != 0)
				{
					*_F += (PairPhi1 / (*_X) - radius_constant);
				}
				else if (*_X > ECSExtentLimit[i] && (dist = ECSExtentLimit[i] + radius - *_X) != 0)
				{
					*_F -= (PairPhi1/dist - radius_constant);
				}
			}
			_X++; _F++; //skip 4th element
		}
		return 0;
	}

	//computer laplacian for tinyball
	//n - total array length (4 * number of cells)
	//alpha - -5.0/(radius * radius)
	//sf - input
	//laplacian - output
	int NtUtility:: TinyBall_laplacian(int n, double alpha, double *sf, double *laplacian)
	{
#if defined(USE_SSE)
		__m128d a0 = _mm_set_pd(alpha, 0); //v1=0; v2 = alpha
		__m128d a1 = _mm_set1_pd(alpha);   //v1=alpha; v2=alpha
		__m128d v1;
		for (int i=0; i<n; i+=2)
		{
			v1 = _mm_load_pd(&sf[i]);
			v1 = _mm_mul_pd(v1, a0);
			_mm_store_pd(&laplacian[i], v1);
			i+= 2;
			v1 = _mm_load_pd(&sf[i]);
			v1 = _mm_mul_pd(v1, a1);
			_mm_store_pd(&laplacian[i], v1);
		}
#else

		memcpy(laplacian, sf, n * sizeof(double));
		for (int i=0; i<n; i+=4)laplacian[i] = 0;
		dscal(n, alpha, laplacian, 1);
#endif

		return 0;
	}


	//compute flux term for tinyball, alpha = -dt/raidus
	int NtUtility::TinyBall_DiffusionFluxTerm(int n, double alpha, double *flux, double *dst)
	{
#if defined(USE_SSE)
		__m128d a0 = _mm_set_pd(alpha * 5.0, alpha * 3.0); //v1=3.0 * alpha; v2 = alpha *5.0
		__m128d a1 = _mm_set1_pd(alpha * 5.0);   //v1=alpha*5; v2=alpha * 5
		__m128d v1, v2;
		for (int i=0; i<n; i+=4)
		{
			v1 = _mm_load_pd(&flux[i]);
			v1 = _mm_mul_pd(v1, a0);
			v2 = _mm_load_pd(&dst[i]);
			v2 = _mm_add_pd(v1, v2);
			_mm_store_pd(&dst[i], v2);
			v1 = _mm_load_pd(&flux[i+2]);
			v1 = _mm_mul_pd(v1, a1);
			v2 = _mm_load_pd(&dst[i+2]);
			v2 = _mm_add_pd(v1, v2);
			_mm_store_pd(&dst[i+2], v2);
		}

#else
		double f3 = alpha * 3;
		double f5 = alpha * 5;
		for (int i=0; i<n; i+= 4)
		{
			dst[i] += flux[i] * f3;
			dst[i+1] += flux[i+1] * f5;
			dst[i+2] += flux[i+2] * f5;
			dst[i+3] += flux[i+3] * f5;
		}
#endif

		return 0;
	}


	//fast memset, 
	//if using SSE, dst memory has to be 32 byte aligned
	//and count is multiple of 4
	int NtUtility::mem_zero_d(double*dst, int count)
	{	
#if defined(USE_SSE)
		__m128d a0 = _mm_set1_pd(0.0); 
		for (int i=0; i<count; i+=4)
		{
			_mm_store_pd(&dst[i], a0);
			_mm_store_pd(&dst[i+2], a0);
		}
#else
		memset(dst, 0, count * sizeof(double));
#endif
		return 0;
	}

	//fast memcpy
	//if using SSE, dst memory has to be 32 byte aligned
	//and count is multiple of 4
	int NtUtility::mem_copy_d(double*dst, double *src, int count)
	{
#if defined(USE_SSE)
		__m128d a0; 
		for (int i=0; i<count; i+=4)
		{
			a0 = _mm_load_pd(&src[i]);
			_mm_store_pd(&dst[i], a0);
			a0 = _mm_load_pd(&src[i+2]);
			_mm_store_pd(&dst[i+2], a0);
		}
#else
		memcpy(dst, src, count *sizeof(double));
#endif
		return 0;
	}

}