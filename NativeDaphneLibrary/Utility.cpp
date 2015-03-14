

#include "stdafx.h"
#include "Utility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>



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


		test_performance();
		//stress test
		//for (int i=0; i< 10000000; i++)
		//{
		//	daxpy(n, alpha, x, 1, y, 1); //y = y + x*alpha
		//	dscal(n, (1-alpha), x, 1); //x = x - alpa*x = (1-a)*x
		//}
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

	//testing performance
	void Utility::test_performance()
	{

		double * x = (double *)malloc(1000000 * sizeof(double));

		double * y = (double *)malloc(1000000 * sizeof(double));

		  double * eig0 = (double *)malloc(1000000 * sizeof(double));

		  double * eig1 = (double *)malloc(1000000 * sizeof(double));

		  double * eigw = (double *)malloc(1000 * sizeof(double));

		  double * chol = (double *)malloc(1000000 * sizeof(double));	

 

  clock_t t0,t1;

  int info;

  int i;

 

  // generate a random matrix

  for(i = 0; i<1000000; ++i){

    x[i] = rand() / (double) RAND_MAX;

  }




  // compute y = xx^T so that y is symmetric positive definite

  dgemm('N','T',1000,1000,1000,1,x,1000,x,1000,0,y,1000);

 

  // make a copy of y for cholesky and eigen decompositions

  for(i = 0; i<1000000; ++i){

    chol[i] = y[i];

    eig0[i] = y[i];

    eig1[i] = y[i];

  }

 

  // first eigenvalue test

  t0 = clock();

  dsyev('V','U',1000,eig0,1000,eigw,&info);

  t1 = clock();

  printf("Eigen decomposition time: %d\n", (t1-t0)/1000);

 

  // cholesky

  dpotrf('U',1000,chol,1000,&info);

 

  // second eigenvalue test, after cholesky

  t0 = clock();

  dsyev('V','U',1000,eig1,1000,eigw,&info);

  t1 = clock();

  printf("Eigen decomposition time: %d\n", (t1-t0)/1000);

}







	int Utility::acml_getnumthreads(void)
	{
		return acmlgetnumthreads();
	}

	
	void Utility::acml_setnumthreads(int numthreads)
	{
		acmlsetnumthreads(numthreads);
	}
}