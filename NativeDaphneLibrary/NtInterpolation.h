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
#include <stdlib.h>
#include <acml.h>

/*this class creates basic structure needed for the middle layer
  to call unmanaged method. for now, it is for computing laplacian() in ECS.
  only methods needed for now is created */

#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

namespace NativeDaphneLibrary
{

	class DllExport NtNodeInterpolator
	{

	protected:
		bool toroidal;

	public:
		virtual int Laplacian(double *sfarray, double *retval, int n) = 0;
	};



	class DllExport NtTrilinear3D : NtNodeInterpolator
	{
	private:
			int nodePerSide0;
			int nodePerSide1;
			int nodePerSide2;
			double stepSize;

			//data for laplacian; lpindex - index not following rule, see note in laplacian()
			//sfindex - the target index to use. lpStartIndex, the start index for lpindex for each i(1..7)
			int *lpindex, *sfindex;
			int lpStartIndex[8];

			double *_tmparr;

			double laplacianCoefficient1;
			double laplacianCoefficient2;
	public:
		NtTrilinear3D(int* side_lens, double step_size, bool _toroidal)
		{
			nodePerSide0 = side_lens[0];
			nodePerSide1 = side_lens[1];
			nodePerSide2 = side_lens[2];
			stepSize = step_size;
			toroidal = _toroidal;

			//data for laplacian
			int alloc_size = nodePerSide0 * nodePerSide1 * nodePerSide2 * 6;
			//the index corresponding to i in the laplacian.array[i]
			lpindex = (int *)malloc(alloc_size * sizeof(int));
			//this corresponding to the index in operator[i][j].index
			sfindex = (int *)malloc(alloc_size * sizeof(int));


			laplacianCoefficient2 = 1.0 / (stepSize * stepSize);
            laplacianCoefficient1 = -2.0 * laplacianCoefficient2 * 3;

			int msize = nodePerSide0 * nodePerSide1 * nodePerSide2;
			int step2 = nodePerSide0 * nodePerSide1;
			int shifts[7] = {0, 1, -1, nodePerSide0, -nodePerSide0, step2, -step2};
			int ** lp_operator = get_laplacian_index_array();

			int nn = 0;
			lpStartIndex[0] = 0;
			for (int j = 1; j<7; j++)
			{
				lpStartIndex[j] = nn;
				for (int i=0; i < msize; i++)
				{
					int k = i + shifts[j];
					int target_index = lp_operator[i][j];
					if (k == target_index)continue;
					lpindex[nn] = i;
					sfindex[nn] = target_index;
					nn++;
				}
			}
			lpStartIndex[7] = nn;

			for (int i=0; i< msize; i++)
			{
				free(lp_operator[i]);
			}
			free(lp_operator);
			lp_operator = NULL;

			//temporary array
			_tmparr = (double *)malloc((nodePerSide0 * nodePerSide1 * (nodePerSide2 + 1)) *sizeof(double));
		}

		~NtTrilinear3D()
		{
			if (lpindex != NULL)
			{
				free(lpindex);
				free(sfindex);
				lpindex = sfindex = NULL;
			}
			if (_tmparr != NULL)
			{
				free(_tmparr);
				_tmparr = NULL;
			}
		}


		//this compute the indices of the laplacianOperator, except that
		//only keeping the indices not the coefficient, since the cofficients are same
		//for all nodes.
		int **get_laplacian_index_array()
		{
			int msize = nodePerSide0 * nodePerSide1 * nodePerSide2;
			int **laplacianOperator = (int **)malloc(msize * sizeof(int *));
			for (int i=0; i< msize; i++)
			{
				laplacianOperator[i] = (int *)malloc(7 * sizeof(int));
			}

			int n = 0;
            int idxplus, idxminus;
            int N01 = nodePerSide0 * nodePerSide1;
            for (int k = 0; k < nodePerSide2; k++)
            {
                for (int j = 0; j < nodePerSide1; j++)
                {
                    for (int i = 0; i < nodePerSide0; i++)
                    {
                        laplacianOperator[n][0] = i + j * nodePerSide0 + k * N01;

                        if (i == 0)
                        {
                            idxplus = (i + 1) + j * nodePerSide0 + k * N01;
                            idxminus = toroidal ? (nodePerSide0 - 2) + j * nodePerSide0 + k * N01 : idxplus;
                        }
                        else if (i == nodePerSide0 - 1)
                        {
                            idxminus = (i - 1) + j * nodePerSide0 + k * N01;
                            idxplus = toroidal ? 1 + j * nodePerSide0 + k * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = (i + 1) + j * nodePerSide0 + k * N01;
                            idxminus = (i - 1) + j * nodePerSide0 + k * N01;
                        }

                        // (i+1), j, k
                        laplacianOperator[n][1] = idxplus;

                        // (i-1), j, k
                        laplacianOperator[n][2] = idxminus;

                        if (j == 0)
                        {
                            idxplus = i + (j + 1) * nodePerSide0 + k * N01;
                            idxminus = toroidal ? i + (nodePerSide1 - 2) * nodePerSide0 + k * N01 : idxplus;
                        }
                        else if (j == nodePerSide1 - 1)
                        {
                            idxminus = i + (j - 1) * nodePerSide0 + k * N01;
                            idxplus = toroidal ? i + 1 * nodePerSide0 + k * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = i + (j + 1) * nodePerSide0 + k * N01;
                            idxminus = i + (j - 1) * nodePerSide0 + k * N01;
                        }

                        // i, (j+1), k
                        laplacianOperator[n][3] = idxplus;

                        // i, (j-1), k
                        laplacianOperator[n][4] = idxminus;

                        if (k == 0)
                        {
                            idxplus = i + j * nodePerSide0 + (k + 1) * N01;
                            idxminus = toroidal ? i + j * nodePerSide0 + (nodePerSide1 - 2) * N01 : idxplus;
                        }
                        else if (k == nodePerSide2- 1)
                        {
                            idxminus = i + j * nodePerSide0 + (k - 1) * N01;
                            idxplus = toroidal ? i + j * nodePerSide0 + 1 * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = i + j * nodePerSide0 + (k + 1) * N01;
                            idxminus = i + j * nodePerSide0 + (k - 1) * N01;
                        }
                        // i, j, (k+1)
                        laplacianOperator[n][5] = idxplus;
                        // i, j, (k-1)
                        laplacianOperator[n][6] = idxminus;

                        n++;
                    }
                }
            }
            return laplacianOperator;
		}

		/***************************************************************************************************
		 * this method take account the following fact.
		 * 1. consider node index x, the laplacian involves the node itself and 6 other nodes aroudn it.
		 *    the 7 nodes have indexes shifted relative the node x as 
		 *    int shifts[7] = {0, 1, -1, nodePerSide0, -nodePerSide0, nodePerSide0 * nodePerSide1, -nodePerSide0 * NodePerSide1};
		 *    double val = 0;
		 *	  for (i = 0; i< 7; i++)
		 *	  {
		 *		    val += sf[x+shift] * coeff[i];
		 *	  }
		 * 2. the coeffs are same for all nodes. In addition, coeff[1] = coeff[2] = coeff[3]...coeff[6]
		 *
		 * 3. from 1 & 2, assume the source scalarfeild sf is size of n, we can compute the values for all nodes as follows
		 *                     [s0 s1 s2 ..............................sn-1]     * coef[0]   <== for i = 0; shift = 0;
		 *
		 *                         [s0 s1 s2 ..............................sn-1]	         <== for i = 1 shift = 1;
		 *					[s0 s1 s2 ..............................sn-1]                    <== for i = 2 shift = -1	             
		 *									[s0 s1 s2 ..............................sn-1]	 <== for i = 3 shift = nodePerSide0 
		 *			[s0 s1 s2 ..............................sn-1]							 <== for i = 4 shift = -NodePerSide0;	
		 *										[s0 s1 s2 ..............................sn-1]<== for i = 5	
		 *    [s0 s1 s2 ..............................sn-1]									 <== for i = 6								
		 *    
		 *    since i = 1..6 have same coeff, so we add up the 6 arrays, and then multiply coeff[1]
		 *  4. The above scheme ignored boundary values for i=1..6. for each i, we precompute the boundary nodes
		 *     and modify the array before they are added together.
		 *
		 *  notice that, arrays i=1..6 are not aligned with array i=0; the extra portion will be trucated off
		 *	the missing part are boundary conditions, that will be filled up according to item 4.
		 ***************************************************************************************************/

		virtual int Laplacian(double *sfarray, double *retval, int n) override
		{
			dscal(n, 0.0, retval, 1);
			dscal(n, 0.0, _tmparr, 1);

			int step2 = nodePerSide0 * nodePerSide1;
			int shifts[7] = {0, 1, -1, nodePerSide0, -nodePerSide0, step2, -step2};
			for (int i= 1; i< 7; i++)
			{
				int shift = shifts[i]; //src shift relative to dst
				if (shift > 0)
				{
					dcopy(n-shift, sfarray + shift, 1, _tmparr, 1);
				}
				else 
				{
					dcopy(n, sfarray, 1, _tmparr-shift, 1);
				}
				
				//modify slot not folowing rule.
				int *sindex = lpindex + lpStartIndex[i];
				int *pindex = lpindex + lpStartIndex[i+1]; //stop
				int *dindex = sfindex + lpStartIndex[i];

				while (sindex != pindex)
				{
					_tmparr[*sindex++] = sfarray[*dindex++];
				}
				daxpy(n, 1.0, _tmparr, 1, retval, 1);
			}
			dscal(n, laplacianCoefficient2, retval, 1);
			daxpy(n, laplacianCoefficient1, sfarray, 1, retval, 1);
			return 0;
		}
	};

}
