#include "stdafx.h"
#include "NtUtility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

using namespace std;

namespace NativeDaphneLibrary
{
	
	int NtUtility::NtDoubleDaxpy(int n, double alpha, double *x, double *y)
	{
		daxpy(n, alpha, x, 1, y, 1);	//y = y + x*alpha
		dscal(n, (1-alpha), x, 1);		//x = x - alpa*x = (1-a)*x
		return 0;
	}

	//multiply two scalars and save result in z
	int NtUtility::NtMultiplyScalar(int n, int inc, double *x, double *y, double *z)
	{
		for (int i=0; i<n; i+= inc)
		{
			double s1 = x[i];
			double s2 = y[i];
			z[i] = x[i] * y[i];
			for (int j = i+1; j< i+inc; j++)
			{
				z[j] = x[j] * s2 + s1 * y[j];
			}
		}
		return 0;
	}

	int NtUtility::daxpy3_skip1(int n, double alpha, double *x, double *y)
	{
		double *ystop = y + n;
		while (y != ystop)
		{
			x++;
			*y++ += alpha * (*x++);
			*y++ += alpha * (*x++);
			*y++ += alpha * (*x++);
		}
		return 0;
	}

	int NtUtility::dxypz3_skip1(int n, double *x, double *y, double *z)
	{
		double *zstop = z + n;
		while (z != zstop)
		{
			x++;
			*z++ += (*x++) * (*y++);
			*z++ += (*x++) * (*y++);
			*z++ += (*x++) * (*y++);
		}
		return 0;
	}

	int NtUtility::apply_boundary_force(int n, double *_X, double *ECSExtentLimit, double radius, double PairPhi1, double *_F)
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
		}
		return 0;
	}




}