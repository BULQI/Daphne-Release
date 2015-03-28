#include "stdafx.h"
#include <acml.h>
#include <stdlib.h>
#include "NtInterpolatedRectangularPrism.h"
#include <stdexcept>

using namespace std;

namespace NativeDaphneLibrary
{

	NtInterpolatedRectangularPrism::NtInterpolatedRectangularPrism()
	{}


	NtInterpolatedRectangularPrism::NtInterpolatedRectangularPrism( 
		int* extents, double step_size, bool is_toroidal)
	{
		NodesPerSide0 = extents[0];
		NodesPerSide1 = extents[1];
		NodesPerSide2 = extents[2];
		NPS01 = NodesPerSide0 * NodesPerSide1;
		NodesPerSide0m1 = NodesPerSide0 -1; //-1
		NodesPerSide1m1 = NodesPerSide1 -1;
		NodesPerSide2m1 = NodesPerSide2 -1;
		StepSize = step_size;


		//data for restrict - precompute local matrix
		isToroidal = is_toroidal;
		int n = NodesPerSide0 * NodesPerSide1 * NodesPerSide2;
		localMatrixArray = (NtIndexMatrix **)malloc(n * sizeof(NtIndexMatrix*));
		for (int i= 0; i< n; i++)
		{
			localMatrixArray[i] = new NtIndexMatrix();
			initialize_index_matrix(i, localMatrixArray[i]);
		}

		//thread related setup
		MaxNumThreads = acmlgetnumthreads()-4; 
		if (MaxNumThreads <= 0)MaxNumThreads = 1;
		jobHandles = (HANDLE *)malloc(MaxNumThreads * sizeof(HANDLE));
		JobReadyEvents = (HANDLE *)malloc(MaxNumThreads * sizeof(HANDLE));
		//JobFinishedEvents = (HANDLE *)malloc(MaxNumThreads * sizeof(HANDLE));
		JobFinishedSignal = CreateEvent(NULL, FALSE, FALSE, NULL);

		EcsArgs = (EcsRestrictArg **)malloc(MaxNumThreads * sizeof(EcsRestrictArg*));
		for (int i=0; i< MaxNumThreads; i++)
		{
			unsigned int tid;
			EcsArgs[i] = new EcsRestrictArg();
			EcsArgs[i]->owner = this;
			EcsArgs[i]->threadId = i;
			JobReadyEvents[i] = CreateEvent(NULL, FALSE, FALSE, NULL);
			jobHandles[i] = (HANDLE)_beginthreadex(0, 0, &RestrictThreadEntry, EcsArgs[i], 0, &tid);
		}



		////********************************
		////data for restririct
		////index inc
		//indexInc[0] = 0;
		//indexInc[1] = 1;
		//indexInc[2] = NodesPerSide0;
		//indexInc[3] = NodesPerSide0 + 1;
		//indexInc[4] = NodesPerSide0 * NodesPerSide1;
		//indexInc[5] = NodesPerSide0 * NodesPerSide1 + 1;
		//indexInc[6] = NodesPerSide0 + NodesPerSide0 * NodesPerSide1;
		//indexInc[7] = NodesPerSide0 + NodesPerSide0 * NodesPerSide1 + 1;

		//int num_restrict_node = 1000;
		//NodeIndex = Delta = Omdelta = D1Array = NULL;
	}



	NtInterpolatedRectangularPrism::~NtInterpolatedRectangularPrism()
	{
		//terminate thread
		for (int i=0; i<MaxNumThreads; i++)
		{
			EcsRestrictArg *arg = EcsArgs[i];
			arg->n = -1;
			::SetEvent(JobReadyEvents[i]);
		}

		//todo: free allocated memory
	}

	void NtInterpolatedRectangularPrism::initialize_laplacian(int* index_operator, 
		double _coef1, double _coef2)
	{
		int alloc_size = NodesPerSide0 * NodesPerSide1 * 6;
		//the index corresponding to i in the laplacian.array[i]
		lpindex = (int *)malloc(alloc_size * sizeof(int));
		//this corresponding to the index in operator[i][j].index
		sfindex = (int *)malloc(alloc_size * sizeof(int));

		for (int i = 0; i < alloc_size; i++)
		{
			int val = index_operator[i];
			lpindex[i] = val>>16;
			sfindex[i] = (val << 16)>>16;
		}
		coef1 = _coef1;
		coef2 = _coef2;
		_tmparr = (double *)malloc((NodesPerSide0 * NodesPerSide1 * (NodesPerSide2 + 1)) *sizeof(double));
	}
	

	//precompute localmatrixes
	void NtInterpolatedRectangularPrism::initialize_index_matrix(int index, NtIndexMatrix *lm)
	{
		int idxarr[3];
		int NPS01 = NodesPerSide0 * NodesPerSide1;
		idxarr[2] = index/NPS01;
		idxarr[1] = (index%NPS01)/NodesPerSide1;
		idxarr[0] = (index%NPS01)%NodesPerSide1;

		if (idxarr[0] == NodesPerSide0 - 1)
		{
			idxarr[0]--;
		}
		if (idxarr[1] == NodesPerSide1 - 1)
		{
			idxarr[1]--;
		}
		if (idxarr[2] == NodesPerSide2 - 1)
		{
			idxarr[2]--;
		}

		int base_index = index;
		int dyindex;

		int bound_flag = 0;
		if (idxarr[0] == 0) bound_flag |= XLEFT;
		else if (idxarr[0] + 1 == NodesPerSide0 - 1)bound_flag |= XRIGHT;
		if (idxarr[1] == 0) bound_flag |= YLEFT;
		else if (idxarr[1] + 1 == NodesPerSide1 - 1)bound_flag |= YRIGHT;
		if (idxarr[2] == 0)bound_flag |= ZLEFT;
		else if (idxarr[2] + 1 == NodesPerSide2 - 1)bound_flag |= ZRIGHT;
		lm->boundFlag = bound_flag;

		int n, n1, n2, n0;
		n = n0 = n1 = n2 = 0;
		int *lm_index0 = lm->indexArray0;
		int *lm_index1 = lm->indexArray1;
		int *lm_index2 = lm->indexArray2;
		int *lm_index3 = lm->indexArray3;

		int node_index = 0;
		for (int di = 0; di < 2; di++)
		{
			for (int dj = 0; dj < 2; dj++)
			{
				for (int dk = 0; dk < 2; dk++)
				{
					node_index = base_index + di  + dj * NodesPerSide0 + dk * NPS01;
					lm_index0[n0++] = node_index;

					// 0th element:
					if (idxarr[0] + di == NodesPerSide0 - 1) //right side ==> di == 1, rightBound == true.
					{
						if (isToroidal)
						{
							lm_index1[n++] = (1) + (idxarr[1] + dj) * NodesPerSide0 + (idxarr[2] + dk) * NPS01;
							lm_index1[n++] = node_index -1;
						}
						else
						{
							lm_index1[n++] = node_index;
							lm_index1[n++] = node_index-1;
							lm_index1[n++] = node_index-2;
						}
					}
					else if (idxarr[0] + di == 0) //left bound di == 0 && LeftBound == true
					{
						if (isToroidal)
						{
							lm_index1[n++] = node_index + 1;
							lm_index1[n++] =  (NodesPerSide0 - 2) + (idxarr[1] + dj) * NodesPerSide0 + (idxarr[2] + dk) * NPS01;
							//lm_index1[n] = -1;
						}
						else
						{
							lm_index1[n++] = node_index;
							lm_index1[n++] = node_index + 1;
							lm_index1[n++] = node_index + 2;
                        }
					}
					else
					{
						lm_index1[n++] = node_index + 1;
						lm_index1[n++] = node_index - 1;
						//lm_index1[n] = -1;
					}  

					// 1st element:
					if (idxarr[1] + dj == NodesPerSide1 - 1)
					{
						if (isToroidal)
						{
							lm_index2[n1++] = (idxarr[0] + di) + (1) * NodesPerSide0 + (idxarr[2] + dk) * NPS01;
							lm_index2[n1++] = node_index - NodesPerSide0;
						}
						else
                        {
							lm_index2[n1++] = node_index;
							lm_index2[n1++] = node_index - NodesPerSide0;
							lm_index2[n1++] = node_index - NodesPerSide0 * 2;
                        }
					}
                    else if (idxarr[1] + dj == 0)
					{
						if (isToroidal)
						{
							lm_index2[n1++] = node_index - NodesPerSide0 * 2;
							lm_index2[n++] = (idxarr[0] + di) + (NodesPerSide1 - 2) * NodesPerSide0 + (idxarr[2] + dk) * NPS01;
						}
						else
						{
							lm_index2[n1++] = node_index;
							lm_index2[n1++] = node_index + NodesPerSide0;
							lm_index2[n1++] = node_index + NodesPerSide0 * 2;
						}
					}
					else
					{
						lm_index2[n1++] = node_index + NodesPerSide0;
						lm_index2[n1++] = node_index - NodesPerSide0;
					}

					//2nd element
					if (idxarr[2] + dk == NodesPerSide2 - 1)
                    {
						if (isToroidal)
                        {
							lm_index3[n2++] = (idxarr[0] + di) + (idxarr[1] + dj) * NodesPerSide0 + (1) * NPS01;
							lm_index3[n2++] = node_index - NPS01; 
                        }
                        else
                        {
							lm_index3[n2++] = node_index; // 3, -4, 1
							lm_index3[n2++] = node_index - NPS01;
							lm_index3[n2++] = node_index - NPS01 * 2;
                        }
					}
                    else if (idxarr[2] + dk == 0)
                    {
						if (isToroidal)
                        {
							lm_index3[n2++] = node_index + NPS01;
							lm_index3[n2++] = (idxarr[0] + di) + (idxarr[1] + dj) * NodesPerSide0 + (NodesPerSide2 - 2) * NPS01;
                        }
                        else
                        {
							lm_index3[n2++] = node_index; // -3, 4, -1
							lm_index3[n2++] = node_index + NPS01;
							lm_index3[n2++] = node_index + NPS01 * 2;
						}
					}
                    else
                    {
						lm_index3[n2++] = node_index + NPS01;
						lm_index3[n2++] = node_index - NPS01;
					}        
				}
			}
		}
		if (n != 16 && n != 20 || n1 != 16 && n1 != 20 || n2 != 16 && n2 != 20)
		{
			fprintf(stderr, "Erroring initializing local matrix");
			exit(1);
		}
	}

	int NtInterpolatedRectangularPrism::Laplacian(double *, double *, int){ return 0;}

	///each position * is for one cell
	int NtInterpolatedRectangularPrism::NativeRestrict(double *sfarray, double** position, int n, double **_output)
	{
		
		double node_vals[20];
		double node_coeffs[20];
		double dx, dy, dz, ddx, ddy, ddz;
		for (int p= 0; p < n; p++)
		{
			double *pos = position[p];
			double *output = _output[p];
			double idxval = pos[0]/StepSize;
			int idx = (int)idxval;
			if (idx == NodesPerSide0m1)idx--;
			dx = idxval - idx;
			ddx = 1 - dx;

			double idyval = pos[1]/StepSize;
			int idy = (int)idyval;
			if (idy == NodesPerSide1m1)idy--;
			dy = idyval - idy;
			ddy = 1 - dy;

			double idzval = pos[2]/StepSize;
			int idz = (int)idzval;
			if (idz == NodesPerSide2m1)idz--;
			dz = idzval - idz;
			ddz = 1 - dz;

			int index = idx + idy * NodesPerSide0 + idz * NPS01;
			
			double coeffs[8];
			//coeffs[0] = (1-delta[0])*(1-delta[1])*(1-delta[2]); //0 0 0
			//coeffs[1] = (1-delta[0])*(1-delta[1])*delta[2];		//0 0 1
			//coeffs[2] = (1-delta[0])*delta[1]*(1-delta[2]);		//0 1 0
			//coeffs[3] = (1-delta[0])*delta[1]*delta[2];			//0 1 1
			//coeffs[4] = delta[0]*(1-delta[1])*(1-delta[2]);		//1 0 0
			//coeffs[5] = delta[0]*(1-delta[1])*delta[2];			//1 0 1
			//coeffs[6] = delta[0]*delta[1]*(1-delta[2]);			//1 1 0
			//coeffs[7] = delta[0]*delta[1]*delta[2];				//1 1 1

			coeffs[0] = ddx * ddy * ddz;	//0 0 0
			coeffs[1] = ddx * ddy * dz;		//0 0 1
			coeffs[2] = ddx * dy * ddz;		//0 1 0
			coeffs[3] = ddx * dy * dz;		//0 1 1
			coeffs[4] = dx * ddy * ddz;		//1 0 0
			coeffs[5] = dx * ddy * dz;		//1 0 1
			coeffs[6] = dx * dy * ddz;		//1 1 0
			coeffs[7] = dx * dy * dz;		//1 1 1

			NtIndexMatrix *lm = localMatrixArray[index];
			int boundFlag = lm->boundFlag;

			//0th element
			int *indexArray = lm->indexArray0;
			//for (int i= 0; i< 8; i++)
			//{
			//	node_vals[i] = sfarray[indexArray[i]];
			//}
			double *node_ptr = node_vals;
			double *ptr_stop = node_ptr + 8;
			int *index_ptr = indexArray;
			while (node_ptr != ptr_stop)
			{
				*node_ptr++ = sfarray[*index_ptr++];
				*node_ptr++ = sfarray[*index_ptr++];
				*node_ptr++ = sfarray[*index_ptr++];
				*node_ptr++ = sfarray[*index_ptr++];
			}

			output[0] = ddot(8, node_vals, 1, coeffs, 1);


			//1th element
			indexArray = lm->indexArray1;
			int nn = 0;
			//fprintf(stderr, "in restricting...\n");
			if ((boundFlag & XBOUND) == 0)
			{
				//for (int i = 0; i< 8; i++)
				//{
				//	node_vals[nn] = sfarray[indexArray[nn]];
				//	node_coeffs[nn++] = coeffs[i];					
				//	node_vals[nn] = sfarray[indexArray[nn]];
				//	node_coeffs[nn++] = -coeffs[i];
				//}

				node_ptr = node_vals;
				ptr_stop = node_ptr + 16;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				for (int i = 0; i< 8; i++)
				{
					node_coeffs[nn++] = coeffs[i];					
					node_coeffs[nn++] = -coeffs[i];
				}
			}
			else if ((boundFlag & XLEFT) != 0)
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}

				int nn = 0;
				for (int i=0; i< 4; i++)
				{
					node_coeffs[nn++] = coeffs[i] * (-3);
					node_coeffs[nn++] = coeffs[i] * (4);
					node_coeffs[nn++] = coeffs[i] * (-1);
				}
				for (int i=4; i< 8; i++)
				{
					node_coeffs[nn++] = coeffs[i];
					node_coeffs[nn++] = -coeffs[i];
				}
			} 
			else //right bound
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				for (int i=0; i< 4; i++)
				{
					node_coeffs[nn++] = coeffs[i];
					node_coeffs[nn++] = -coeffs[i];
				}
				for (int i=4; i< 8; i++)
				{
					node_coeffs[nn++] = coeffs[i] * (3);
					node_coeffs[nn++] = coeffs[i] * (-4);
					node_coeffs[nn++] = coeffs[i];
				}
			}
			output[1] = ddot(nn, node_vals, 1, node_coeffs, 1);
			output[1] /= (2 * StepSize);

			//2th element
			indexArray = lm->indexArray2;
			nn = 0;
			if ((boundFlag & YBOUND) == 0)
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 16;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				for (int i = 0; i< 8; i++)
				{
					node_coeffs[nn++] = coeffs[i];					
					node_coeffs[nn++] = -coeffs[i];
				}
			}
			else if ((boundFlag & YLEFT) != 0) //3 3 2 2 3 3 2 2
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				node_ptr = node_coeffs;
				*node_ptr++ = coeffs[0] * (-3);
				*node_ptr++ = coeffs[0] * (4);
				*node_ptr++ = coeffs[0] * (-1);
				*node_ptr++ = coeffs[1] * (-3);
				*node_ptr++ = coeffs[1] * (4);
				*node_ptr++ = coeffs[1] * (-1);
				*node_ptr++ = coeffs[2];
				*node_ptr++ = -coeffs[2];
				*node_ptr++ = coeffs[3];
				*node_ptr++ = -coeffs[3];
				*node_ptr++ = coeffs[4] * (-3);
				*node_ptr++ = coeffs[4] * (4);
				*node_ptr++ = coeffs[4] * (-1);
				*node_ptr++ = coeffs[5] * (-3);
				*node_ptr++ = coeffs[5] * (4);
				*node_ptr++ = coeffs[5] * (-1);
				*node_ptr++ = coeffs[6];
				*node_ptr++ = -coeffs[6];
				*node_ptr++ = coeffs[7];
				*node_ptr++ = -coeffs[7];
				nn = 20;
			} 
			else //right bound: 2 2 3 3 2 2 3 3
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				node_ptr = node_coeffs;
				*node_ptr++ = coeffs[0];
				*node_ptr++ = -coeffs[0];
				*node_ptr++ = coeffs[1];
				*node_ptr++ = -coeffs[1];
				*node_ptr++ = coeffs[2] * (3);
				*node_ptr++ = coeffs[2] * (-4);
				*node_ptr++ = coeffs[2] * (1);
				*node_ptr++ = coeffs[3] * (3);
				*node_ptr++ = coeffs[3] * (-4);
				*node_ptr++ = coeffs[3] * (1);
				*node_ptr++ = coeffs[4];
				*node_ptr++ = -coeffs[4];
				*node_ptr++ = coeffs[5];
				*node_ptr++ = -coeffs[5];
				*node_ptr++ = coeffs[6] * (3);
				*node_ptr++ = coeffs[6] * (-4);
				*node_ptr++ = coeffs[6] * (1);
				*node_ptr++ = coeffs[7] * (3);
				*node_ptr++ = coeffs[7] * (-4);
				*node_ptr++ = coeffs[7] * (1);
				nn= 20;
			}
			output[2] = ddot(nn, node_vals, 1, node_coeffs, 1);
			output[2] /= (2 * StepSize);

			//3th element
			indexArray = lm->indexArray3;
			nn = 0;
			if ((boundFlag & ZBOUND) == 0)
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 16;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				for (int i = 0; i< 8; i++)
				{
					node_coeffs[nn++] = coeffs[i];					
					node_coeffs[nn++] = -coeffs[i];
				}
			}
			else if ((boundFlag & ZLEFT) != 0) //3 2 3 2 3 2 3 2
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}
				node_ptr = node_coeffs;
				*node_ptr++ = coeffs[0] * (-3);
				*node_ptr++ = coeffs[0] * (4);
				*node_ptr++ = coeffs[0] * (-1);
				*node_ptr++ = coeffs[1];
				*node_ptr++ = -coeffs[1];
				*node_ptr++ = coeffs[2] * (-3);
				*node_ptr++ = coeffs[2] * (4);
				*node_ptr++ = coeffs[2] * (-1);
				*node_ptr++ = coeffs[3];
				*node_ptr++ = -coeffs[3];
				*node_ptr++ = coeffs[4] * (-3);
				*node_ptr++ = coeffs[4] * (4);
				*node_ptr++ = coeffs[4] * (-1);
				*node_ptr++ = coeffs[5];
				*node_ptr++ = -coeffs[5];
				*node_ptr++ = coeffs[6] * (-3);
				*node_ptr++ = coeffs[6] * (4);
				*node_ptr++ = coeffs[6] * (-1);
				*node_ptr++ = coeffs[7];
				*node_ptr++ = -coeffs[7];
				nn = 20;
			} 
			else //right bound: 2 3 2 3 2 3 2 3
			{
				node_ptr = node_vals;
				ptr_stop = node_ptr + 20;
				index_ptr = indexArray;
				while (node_ptr != ptr_stop)
				{
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
					*node_ptr++ = sfarray[*index_ptr++];
				}

				node_ptr = node_coeffs;
				*node_ptr++ = coeffs[0];
				*node_ptr++ = -coeffs[0];
				*node_ptr++ = coeffs[1] * (3);
				*node_ptr++ = coeffs[1] * (-4);
				*node_ptr++ = coeffs[1] * (1);
				*node_ptr++ = coeffs[2];
				*node_ptr++ = -coeffs[2];
				*node_ptr++ = coeffs[3] * (3);
				*node_ptr++ = coeffs[3] * (-4);
				*node_ptr++ = coeffs[3] * (1);

				*node_ptr++ = coeffs[4];
				*node_ptr++ = -coeffs[4];
				*node_ptr++ = coeffs[5] * (3);
				*node_ptr++ = coeffs[5] * (-4);
				*node_ptr++ = coeffs[5] * (1);
				*node_ptr++ = coeffs[6];
				*node_ptr++ = -coeffs[6];
				*node_ptr++ = coeffs[7] * (3);
				*node_ptr++ = coeffs[7] * (-4);
				*node_ptr++ = coeffs[7] * (1);
				
			}
			output[3] = ddot(nn, node_vals, 1, node_coeffs, 1);	
			output[3] /= (2 * StepSize);
		}
		return 0;
	}

	int NtInterpolatedRectangularPrism::MultithreadNativeRestrict(double *sfarray, double** position, int n, double **_output)
	{
		DWORD retVal;
		int numThreads = MaxNumThreads; //total cores -4
		int NumItemsPerThread = n /(numThreads + 2);
		if (NumItemsPerThread < 20)
		{
			NumItemsPerThread = 20;
			numThreads = n/20 - 2;
			if (numThreads < 0)numThreads = 0;
		}

		//start job
		::InterlockedExchange(&AcitveJobCount, numThreads);
		int n0, nn;
		n0 = nn = n - NumItemsPerThread * numThreads;
		for (int i=0; i< numThreads; i++)
		{
			EcsRestrictArg *arg = EcsArgs[i];
			arg->sfarray = sfarray;
			arg->position = position + nn;
			arg->_output = _output + nn;
			arg->n = NumItemsPerThread;
			nn += NumItemsPerThread;
			::SetEvent(JobReadyEvents[i]);
		}

		NativeRestrict(sfarray, position, n0, _output);
		//wait for job finish
		if (numThreads > 0)
		{
			while (::InterlockedCompareExchange(&AcitveJobCount, 1, 0) != 0);

			//long xx = 0;
			//if ( (xx = ::InterlockedCompareExchange(&AcitveJobCount, 1, 0)) != 0)
			//{
			//	fprintf(stderr, "waiting for %d out of %d\n", xx, numThreads);
			//	WaitForSingleObject(this->JobFinishedSignal, INFINITE);
			//}
			//else fprintf(stderr, "goood..no wait\n");
		}
		return 0;

	}


	int NtInterpolatedRectangularPrism::TestAddition(int a, int b)
	{
		return a+b;
	}

}



