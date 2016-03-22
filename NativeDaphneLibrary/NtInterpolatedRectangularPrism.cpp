#include "stdafx.h"
#include <acml.h>
#include <stdlib.h>
#include "NtInterpolatedRectangularPrism.h"
#include <stdexcept>
#include <xmmintrin.h>


#include "NtInterpolation.h"

//#define DO_PREFETCH

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
		gradientFactor = 1.0 /(2 * step_size);



		//data for restrict - precompute local matrix
		isToroidal = is_toroidal;
		int n = NodesPerSide0 * NodesPerSide1 * NodesPerSide2;
		localMatrixArray = (NtIndexMatrix *)malloc(n * sizeof(NtIndexMatrix));
		for (int i= 0; i< n; i++)
		{
			initialize_index_matrix(i);
		}

		sf_prefetch_list[0] = -NPS01;
		sf_prefetch_list[1] = NodesPerSide0 - NPS01;
		sf_prefetch_list[2] = - NodesPerSide0;
		sf_prefetch_list[3] = -1;
		sf_prefetch_list[4] = NodesPerSide0 - 1;
		sf_prefetch_list[5] = NodesPerSide0 * 2;
		sf_prefetch_list[6] = -NodesPerSide0 + NPS01;
		sf_prefetch_list[7] = NPS01 -1;
		sf_prefetch_list[8] = NodesPerSide0 + NPS01-1;
		sf_prefetch_list[9] = NodesPerSide0 + NodesPerSide0 + NPS01;
		sf_prefetch_list[10] = NPS01 * 2;
		sf_prefetch_list[11] = NodesPerSide0 + NPS01 * 2;

		int *array_index0 = array_index_shifts;
		int *array_index1 = array_index_shifts + 8;
		int *array_index2 = array_index_shifts + 24;
		int *array_index3 = array_index_shifts + 40;
		n  = 0;
		int k = 0;
		for (int di = 0; di<2; di++)
		{
			for (int dj = 0; dj <2; dj++)
			{
				for (int dk = 0; dk< 2; dk++)
				{
					//zero th element  for interpolationMatrix
					array_index0[k] =  di + dj * NodesPerSide0 + dk * NPS01;
					k++;
					//first element
					array_index1[n]   = (di + 1) + dj * NodesPerSide0 + dk * NPS01;
                    array_index1[n+1] = (di - 1) + dj * NodesPerSide0 + dk * NPS01;
					//second element
					array_index2[n]   = di + (dj + 1) * NodesPerSide0 + dk * NPS01;
					array_index2[n+1] = di + (dj - 1) * NodesPerSide0 + dk * NPS01;
					//third element
					array_index3[n]     = di + dj * NodesPerSide0 + (dk + 1) * NPS01;
                    array_index3[n + 1] = di + dj * NodesPerSide0 + (dk - 1) * NPS01;
					n+= 2;
				}
			}
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

		if (localMatrixArray != NULL)
		{
			free(localMatrixArray);
		}

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
	void NtInterpolatedRectangularPrism::initialize_index_matrix(int index)
	{
		int idxarr[3];
		int NPS01 = NodesPerSide0 * NodesPerSide1;
		idxarr[2] = index/NPS01;
		idxarr[1] = (index%NPS01)/NodesPerSide0;
		idxarr[0] = (index%NPS01)%NodesPerSide0;

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

		int bound_flag = 0;
		if (idxarr[0] == 0) bound_flag |= XLEFT;
		else if (idxarr[0] + 1 == NodesPerSide0 - 1)bound_flag |= XRIGHT;
		if (idxarr[1] == 0) bound_flag |= YLEFT;
		else if (idxarr[1] + 1 == NodesPerSide1 - 1)bound_flag |= YRIGHT;
		if (idxarr[2] == 0)bound_flag |= ZLEFT;
		else if (idxarr[2] + 1 == NodesPerSide2 - 1)bound_flag |= ZRIGHT;

		NtIndexMatrix *lm = &localMatrixArray[index];
		lm->boundFlag = bound_flag;
		
		//if internal node, we don't compute neighbours
		if (bound_flag == 0)return;

		int *lm_index1 = lm->indexArray;
		int *lm_index2 = lm->indexArray + 20;
		int *lm_index3 = lm->indexArray + 40;

		////if its a internal node, then we make the memory contigougs for the index
		//if (bound_flag == 0)
		//{
		//	lm_index2 = lm->indexArray + 16;
		//	lm_index3 = lm->indexArray + 32;
		//}

		int n, n1, n2;
		n = n1 = n2 = 0;

		int node_index = 0;
		for (int di = 0; di < 2; di++)
		{
			for (int dj = 0; dj < 2; dj++)
			{
				for (int dk = 0; dk < 2; dk++)
				{
					node_index = base_index + di  + dj * NodesPerSide0 + dk * NPS01;

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
		
		if (bound_flag == 0 && (n != 16 || n1 !=16 || n2 != 16))
		{
			fprintf(stderr, "Erroring initializing local matrix");
			exit(1);
		}
		else if (n != 16 && n != 20 || n1 != 16 && n1 != 20 || n2 != 16 && n2 != 20)
		{
			fprintf(stderr, "Erroring initializing local matrix");
			exit(1);
		}
	}

	//not yet implemented
	int NtInterpolatedRectangularPrism::Laplacian(double *sfarray, double *retval, int n)
	{ 

		dscal(n, 0.0, retval, 1);
		dscal(n, 0.0, _tmparr, 1);

		int step2 = NodesPerSide0 * NodesPerSide1;
		int shifts[7] = {0, 1, -1, NodesPerSide0, -NodesPerSide0, step2, -step2};
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
			int *sindex = lpindex + (i-1)*step2;
			int *pindex = sindex + step2;
			int *dindex = sfindex + (i-1)*step2;

			while (sindex != pindex)
			{
				_tmparr[*sindex++] = sfarray[*dindex++];
			}
			daxpy(n, 1.0, _tmparr, 1, retval, 1);
		}
		dscal(n, coef2, retval, 1);
		daxpy(n, coef1, sfarray, 1, retval, 1);
		return 0;
	}




	int NtInterpolatedRectangularPrism::NativeRestrict(double *sfarray, double** position, int n, double **_output)
	{
		
		double dx, dy, dz, ddx, ddy, ddz, tmpval;
		double *pos;
		double *sfarray_tmp;
		double StepSizeInverse = 1.0/StepSize;
		int nps0m1 = NodesPerSide0 - 1;
		int nps1m1 = NodesPerSide1 - 1;
		int nps2m1 = NodesPerSide2 - 1;
		int ishift[8];
		int ishift1[16];
		int ishift2[16];
		int ishift3[16];
		for (int i=0; i<8; i++)
		{
			ishift[i] = array_index_shifts[i];
		}
		for (int i=0; i< 16; i++)
		{
			ishift1[i] = array_index_shifts[i+8];
			ishift2[i] = array_index_shifts[i+24];
			ishift3[i] = array_index_shifts[i+40];
		}

#if defined(DO_PREFETCH)
		int pflist[12];
		for (int i=0; i<12; i++)
		{
			pflist[i] = sf_prefetch_list[i];
		}
#endif
	
		for (int p= 0; p < n; p++)
		{

#if defined(DO_PREFETCH)
			if (p < n-1)
			{
				pos =  position[p+1];
				int pre_index = (int)(pos[0] * StepSizeInverse) + (int)(pos[1] * StepSizeInverse) * NodesPerSide0 + (int)(pos[2] * StepSizeInverse)* NPS01;
				sfarray_tmp = sfarray + pre_index;
				for (int x = 0; x < 12; x++)
				{
					_mm_prefetch((const char *)(sfarray_tmp + pflist[x]), _MM_HINT_T0);
				}
				_mm_prefetch((const char *)(localMatrixArray + pre_index), _MM_HINT_T0);
			}
#endif
			pos = position[p];
			double *output = _output[p];

			tmpval = pos[0] * StepSizeInverse;
			//tmpval = pos_next;
			//pos_next = p < n-1 ? position[p+1][0] : 0; //prefetching the next position
			int idx = (int)tmpval;
			if (idx == nps0m1)idx--;
			dx = tmpval - idx;
			ddx = 1 - dx;

			tmpval = pos[1] * StepSizeInverse;
			int idy = (int)tmpval;
			if (idy == nps1m1)idy--;
			dy = tmpval - idy;
			ddy = 1 - dy;

			tmpval = pos[2] * StepSizeInverse;
			int idz = (int)tmpval;
			if (idz == nps2m1)idz--;
			dz = tmpval - idz;
			ddz = 1 - dz;

			int index = idx + idy * NodesPerSide0 + idz * NPS01;

			double coeffs[8];
			coeffs[0] = ddx * ddy * ddz;	//0 0 0
			coeffs[1] = ddx * ddy * dz;		//0 0 1
			coeffs[2] = ddx * dy * ddz;		//0 1 0
			coeffs[3] = ddx * dy * dz;		//0 1 1
			coeffs[4] = dx * ddy * ddz;		//1 0 0
			coeffs[5] = dx * ddy * dz;		//1 0 1
			coeffs[6] = dx * dy * ddz;		//1 1 0
			coeffs[7] = dx * dy * dz;		//1 1 1

			//0th element - interpolation
			sfarray_tmp = sfarray + index;
							
			double sumval = sfarray_tmp[ishift[0]] * coeffs[0];
			sumval += sfarray_tmp[ishift[1]] * coeffs[1];
			sumval += sfarray_tmp[ishift[2]] * coeffs[2];
			sumval += sfarray_tmp[ishift[3]] * coeffs[3];
			sumval += sfarray_tmp[ishift[4]] * coeffs[4];
			sumval += sfarray_tmp[ishift[5]] * coeffs[5];
			sumval += sfarray_tmp[ishift[6]] * coeffs[6];
			sumval += sfarray_tmp[ishift[7]] * coeffs[7];
			output[0] = sumval;

			//1th element
			NtIndexMatrix *lm = localMatrixArray + index;
			int boundFlag = lm->boundFlag;
			//handle non-boundary nodes.
			if (boundFlag == 0)
			{
				sumval = (sfarray_tmp[ishift1[0]] - sfarray_tmp[ishift1[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift1[2]] - sfarray_tmp[ishift1[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift1[4]] - sfarray_tmp[ishift1[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift1[6]] - sfarray_tmp[ishift1[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift1[8]] - sfarray_tmp[ishift1[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift1[10]] - sfarray_tmp[ishift1[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift1[12]] - sfarray_tmp[ishift1[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift1[14]] - sfarray_tmp[ishift1[15]]) * coeffs[7];
				output[1] = sumval * gradientFactor;

				sumval = (sfarray_tmp[ishift2[0]] - sfarray_tmp[ishift2[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift2[2]] - sfarray_tmp[ishift2[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift2[4]] - sfarray_tmp[ishift2[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift2[6]] - sfarray_tmp[ishift2[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift2[8]] - sfarray_tmp[ishift2[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift2[10]] - sfarray_tmp[ishift2[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift2[12]] - sfarray_tmp[ishift2[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift2[14]] - sfarray_tmp[ishift2[15]]) * coeffs[7];
				output[2] = sumval * gradientFactor;

				sumval = (sfarray_tmp[ishift3[0]] - sfarray_tmp[ishift3[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift3[2]] - sfarray_tmp[ishift3[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift3[4]] - sfarray_tmp[ishift3[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift3[6]] - sfarray_tmp[ishift3[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift3[8]] - sfarray_tmp[ishift3[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift3[10]] - sfarray_tmp[ishift3[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift3[12]] - sfarray_tmp[ishift3[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift3[14]] - sfarray_tmp[ishift3[15]]) * coeffs[7];
				output[3] = sumval * gradientFactor;

				continue;
			}

			//for boundary node
			int *index_ptr = lm->indexArray;
			if ((boundFlag & XBOUND) == 0)
			{
				sumval = (sfarray_tmp[ishift1[0]] - sfarray_tmp[ishift1[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift1[2]] - sfarray_tmp[ishift1[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift1[4]] - sfarray_tmp[ishift1[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift1[6]] - sfarray_tmp[ishift1[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift1[8]] - sfarray_tmp[ishift1[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift1[10]] - sfarray_tmp[ishift1[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift1[12]] - sfarray_tmp[ishift1[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift1[14]] - sfarray_tmp[ishift1[15]]) * coeffs[7];
			}
			else if ((boundFlag & XLEFT) != 0)
			{
				sumval = (-sfarray[index_ptr[0]] * 3 + sfarray[index_ptr[1]] * 4  - sfarray[index_ptr[2]]) * coeffs[0];
				sumval += (-sfarray[index_ptr[3]] * 3 + sfarray[index_ptr[4]] * 4  - sfarray[index_ptr[5]]) * coeffs[1];
				sumval += (-sfarray[index_ptr[6]] * 3 + sfarray[index_ptr[7]] * 4  - sfarray[index_ptr[8]]) * coeffs[2];
				sumval += (-sfarray[index_ptr[9]] * 3 + sfarray[index_ptr[10]] * 4  - sfarray[index_ptr[11]]) * coeffs[3];
				sumval += (sfarray[index_ptr[12]] - sfarray[index_ptr[13]]) * coeffs[4];
				sumval += (sfarray[index_ptr[14]] - sfarray[index_ptr[15]]) * coeffs[5];
				sumval += (sfarray[index_ptr[16]] - sfarray[index_ptr[17]]) * coeffs[6];
				sumval += (sfarray[index_ptr[18]] - sfarray[index_ptr[19]]) * coeffs[7];
			} 
			else //right bound
			{
				sumval = (sfarray[index_ptr[0]] - sfarray[index_ptr[1]]) * coeffs[0];
				sumval += (sfarray[index_ptr[2]] - sfarray[index_ptr[3]]) * coeffs[1];
				sumval += (sfarray[index_ptr[4]] - sfarray[index_ptr[5]]) * coeffs[2];
				sumval += (sfarray[index_ptr[6]] - sfarray[index_ptr[7]]) * coeffs[3];
				sumval += (sfarray[index_ptr[8]] * 3 - sfarray[index_ptr[9]] * 4  + sfarray[index_ptr[10]]) * coeffs[4];
				sumval += (sfarray[index_ptr[11]] * 3 - sfarray[index_ptr[12]] * 4  + sfarray[index_ptr[13]]) * coeffs[5];
				sumval += (sfarray[index_ptr[14]] * 3 - sfarray[index_ptr[15]] * 4  + sfarray[index_ptr[16]]) * coeffs[6];
				sumval += (sfarray[index_ptr[17]] * 3 - sfarray[index_ptr[18]] * 4  + sfarray[index_ptr[19]]) * coeffs[7];
			}
			output[1] = sumval * gradientFactor;

			//2th element
			index_ptr += 20;
			if ((boundFlag & YBOUND) == 0)
			{
				sumval = (sfarray_tmp[ishift2[0]] - sfarray_tmp[ishift2[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift2[2]] - sfarray_tmp[ishift2[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift2[4]] - sfarray_tmp[ishift2[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift2[6]] - sfarray_tmp[ishift2[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift2[8]] - sfarray_tmp[ishift2[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift2[10]] - sfarray_tmp[ishift2[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift2[12]] - sfarray_tmp[ishift2[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift2[14]] - sfarray_tmp[ishift2[15]]) * coeffs[7];
			}
			else if ((boundFlag & YLEFT) != 0) //3 3 2 2 3 3 2 2
			{
				//20 elements
				sumval = (-sfarray[index_ptr[0]] * 3 + sfarray[index_ptr[1]] * 4  - sfarray[index_ptr[2]]) * coeffs[0];
				sumval += (-sfarray[index_ptr[3]] * 3 + sfarray[index_ptr[4]] * 4  - sfarray[index_ptr[5]]) * coeffs[1];
				sumval += (sfarray[index_ptr[6]] - sfarray[index_ptr[7]]) * coeffs[2];
				sumval += (sfarray[index_ptr[8]] - sfarray[index_ptr[9]]) * coeffs[3];
				sumval += (-sfarray[index_ptr[10]] * 3 + sfarray[index_ptr[11]] * 4  - sfarray[index_ptr[12]]) * coeffs[4];
				sumval += (-sfarray[index_ptr[13]] * 3 + sfarray[index_ptr[14]] * 4  - sfarray[index_ptr[15]]) * coeffs[5];
				sumval += (sfarray[index_ptr[16]] - sfarray[index_ptr[17]]) * coeffs[6];
				sumval += (sfarray[index_ptr[18]] - sfarray[index_ptr[19]]) * coeffs[7];
			} 
			else //right bound: 2 2 3 3 2 2 3 3
			{
				sumval = (sfarray[index_ptr[0]] - sfarray[index_ptr[1]]) * coeffs[0];
				sumval += (sfarray[index_ptr[2]] - sfarray[index_ptr[3]]) * coeffs[1];
				sumval += (sfarray[index_ptr[4]] * 3 - sfarray[index_ptr[5]] * 4  + sfarray[index_ptr[6]]) * coeffs[2];
				sumval += (sfarray[index_ptr[7]] * 3 - sfarray[index_ptr[8]] * 4  + sfarray[index_ptr[9]]) * coeffs[3];
				sumval += (sfarray[index_ptr[10]] - sfarray[index_ptr[11]]) * coeffs[4];
				sumval += (sfarray[index_ptr[12]] - sfarray[index_ptr[13]]) * coeffs[5];
				sumval += (sfarray[index_ptr[14]] * 3 - sfarray[index_ptr[15]] * 4  + sfarray[index_ptr[16]]) * coeffs[6];
				sumval += (sfarray[index_ptr[17]] * 3 - sfarray[index_ptr[18]] * 4  + sfarray[index_ptr[19]]) * coeffs[7];
			}
			output[2] = sumval * gradientFactor;

			//3th element
			index_ptr += 20;
			if ((boundFlag & ZBOUND) == 0)
			{
				sumval = (sfarray_tmp[ishift3[0]] - sfarray_tmp[ishift3[1]]) * coeffs[0];
				sumval += (sfarray_tmp[ishift3[2]] - sfarray_tmp[ishift3[3]]) * coeffs[1];
				sumval += (sfarray_tmp[ishift3[4]] - sfarray_tmp[ishift3[5]]) * coeffs[2];
				sumval += (sfarray_tmp[ishift3[6]] - sfarray_tmp[ishift3[7]]) * coeffs[3];
				sumval += (sfarray_tmp[ishift3[8]] - sfarray_tmp[ishift3[9]]) * coeffs[4];
				sumval += (sfarray_tmp[ishift3[10]] - sfarray_tmp[ishift3[11]]) * coeffs[5];
				sumval += (sfarray_tmp[ishift3[12]] - sfarray_tmp[ishift3[13]]) * coeffs[6];
				sumval += (sfarray_tmp[ishift3[14]] - sfarray_tmp[ishift3[15]]) * coeffs[7];
			}
			else if ((boundFlag & ZLEFT) != 0) //3 2 3 2 3 2 3 2
			{
				sumval = (-sfarray[index_ptr[0]] * 3 + sfarray[index_ptr[1]] * 4  - sfarray[index_ptr[2]]) * coeffs[0];
				sumval += (sfarray[index_ptr[3]] - sfarray[index_ptr[4]]) * coeffs[1];
				sumval += (-sfarray[index_ptr[5]] * 3 + sfarray[index_ptr[6]] * 4  - sfarray[index_ptr[7]]) * coeffs[2];
				sumval += (sfarray[index_ptr[8]] - sfarray[index_ptr[9]]) * coeffs[3];
				sumval += (-sfarray[index_ptr[10]] * 3 + sfarray[index_ptr[11]] * 4  - sfarray[index_ptr[12]]) * coeffs[4];
				sumval += (sfarray[index_ptr[13]] - sfarray[index_ptr[14]]) * coeffs[5];
				sumval += (-sfarray[index_ptr[15]] * 3 + sfarray[index_ptr[16]] * 4  - sfarray[index_ptr[17]]) * coeffs[6];
				sumval += (sfarray[index_ptr[18]] - sfarray[index_ptr[19]]) * coeffs[7];
			} 
			else //right bound: 2 3 2 3 2 3 2 3
			{
				sumval = (sfarray[index_ptr[0]] - sfarray[index_ptr[1]]) * coeffs[0];
				sumval += (sfarray[index_ptr[2]] * 3 - sfarray[index_ptr[3]] * 4  + sfarray[index_ptr[4]]) * coeffs[1];
				sumval += (sfarray[index_ptr[5]] - sfarray[index_ptr[6]]) * coeffs[2];
				sumval += (sfarray[index_ptr[7]] * 3 - sfarray[index_ptr[8]] * 4  + sfarray[index_ptr[9]]) * coeffs[3];
				sumval += (sfarray[index_ptr[10]] - sfarray[index_ptr[11]]) * coeffs[4];
				sumval += (sfarray[index_ptr[12]] * 3 - sfarray[index_ptr[13]] * 4  + sfarray[index_ptr[14]]) * coeffs[5];
				sumval += (sfarray[index_ptr[15]] - sfarray[index_ptr[16]]) * coeffs[6];
				sumval += (sfarray[index_ptr[17]] * 3 - sfarray[index_ptr[18]] * 4  + sfarray[index_ptr[19]]) * coeffs[7];
			}
			output[3] = sumval * gradientFactor;
		}
		return 0;
	}


	int NtInterpolatedRectangularPrism::MultithreadNativeRestrict(double *sfarray, double** position, int n, double **_output)
	{

		int numThreads = MaxNumThreads; //total cores -4
		int NumItemsPerThread = n /(numThreads + 2);
		if (NumItemsPerThread < 20)
		{
			NumItemsPerThread = 20;
			numThreads = n/20 - 2;
			if (numThreads < 0)numThreads = 0;
		}

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
		}
		return 0;

	}


	int NtInterpolatedRectangularPrism::TestAddition(int a, int b)
	{
		return a+b;
	}

}



