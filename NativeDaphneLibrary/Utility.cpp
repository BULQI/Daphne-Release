

#include "stdafx.h"
#include "Utility.h"
#include <stdexcept>
#include <acml.h>

using namespace std;

namespace NativeDaphneLibrary
{
	
	
	int Utility::NtDaxpy(int n, double alpha, double *x, int incx, double *y, int incy)
	{
		daxpy(n, alpha, x, incx, y, incy);
		return 0;
		
	}
	
	int Utility::NtDscal(int n, double alpha, double *x, int incx)
	{
		dscal(n, alpha, x, incx);
		return 0;
	}

	int Utility::NtDoubleDaxpy(int n, double alpha, double *x, double *y)
	{
		daxpy(n, alpha, x, 1, y, 1); //y = y + x*alpha
		dscal(n, (1-alpha), x, 1); //x = x - alpa*x = (1-a)*x
		return 0;
	}

	int Utility::NtDcopy(int n, double* x, int incx, double *y, int incy)
	{
		dcopy(n, x, incx, y, incy);
		return 0;
	}

	//multiply two scalars and save result in z
	int Utility::NtMultiplyScalar(int n, int inc, double *x, double *y, double *z)
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





}