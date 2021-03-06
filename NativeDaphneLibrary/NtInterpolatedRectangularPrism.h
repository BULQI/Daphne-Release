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
#include <process.h>


namespace NativeDaphneLibrary
{
	//this class stores precomputed gradientMatrix information
	//indexArray stores 20 x 3 indexes for the 3 gradient matrix components
	typedef struct DllExport NtIndexMatrix
	{
		int boundFlag;
		int indexArray[60]; 
	}NtIndexMatrixStr;

	class NtInterpolatedRectangularPrism;

	class DllExport EcsRestrictArg
	{
	public:
		NtInterpolatedRectangularPrism *owner;
		double *sfarray;
		double** position; 
		int n; 
		double **_output;
		int threadId;
	};

	class DllExport NtInterpolatedRectangularPrism
	{
		
		static const int XLEFT = 1;
		static const int XRIGHT = 2;
		static const int XBOUND = XLEFT + XRIGHT;
		static const int YLEFT = 4;
		static const int YRIGHT = 8;
		static const int YBOUND = YLEFT + YRIGHT;
		static const int ZLEFT = 16;
		static const int ZRIGHT = 32;
		static const int ZBOUND = ZLEFT + ZRIGHT;

		int NodesPerSide0;
		int NodesPerSide1;
		int NodesPerSide2;
		int NodesPerSide0m1; //-1
		int NodesPerSide1m1;
		int NodesPerSide2m1;
		double StepSize;
		double gradientFactor;


		//data for laplacian
		double coef1;
		double coef2;
		int *lpindex;
		int *sfindex;
		int indexInc[8];

		double *_tmparr;

		//for laplacianv0 only
		int *lpindex_inbound;
		int *sfindex_inbound;
		int inbound_length;

		//data for restrict
		//precomputed localmatrix informaiton
		NtIndexMatrix *localMatrixArray;
		bool isToroidal;
		int NPS01;  //NodesPerSide0 * NodesPerSide1;
		//more precomputed values.
		int sf_prefetch_list[12];
		int array_index_shifts[56];



		int num_restrict_node;
		double *NodeIndex;	//for pos/sepsize in x, y z.
		double *Delta;		//for dx dy dz
		double *Omdelta;	//for 1-dx, 1-dy, 1-dz
		double *D1Array;	//constant 1.0
	
		//thread stuff
		int MaxNumThreads;
		HANDLE* jobHandles;
		HANDLE* JobReadyEvents;
		HANDLE* JobFinishedEvents;

		EcsRestrictArg** EcsArgs;

		unsigned long AcitveJobCount;
		HANDLE JobFinishedSignal;



	public:

		NtInterpolatedRectangularPrism();

		NtInterpolatedRectangularPrism(int* extents, double step_size, bool is_toroidal);

		~NtInterpolatedRectangularPrism();

		void initialize_index_matrix(int index);

		void initialize_laplacian(int* index_operator, double _coef1, double _coef2);

		int Laplacian(double *sfarray, double *retval, int n);

		int NativeRestrict(double *sfarray, double** pos, int n, double **output);

		int NtInterpolatedRectangularPrism::thread_restrict( void* arg);

		int MultithreadNativeRestrict(double *sfarray, double** position, int n, double **_output);
	
		//for testing access
		int TestAddition(int a, int b);

	private:
		static unsigned __stdcall RestrictThreadEntry(void* pUserData) 
		{
			EcsRestrictArg *arg = (EcsRestrictArg *)pUserData;
			int tid = arg->threadId;
			NtInterpolatedRectangularPrism *owner = arg->owner;

			while (true)
			{
				WaitForSingleObject(owner->JobReadyEvents[tid], INFINITE); 
				if (arg->n == -1) //signal to end thread
				{
					_endthread();
				}
				arg->owner->NativeRestrict(arg->sfarray, arg->position, arg->n, arg->_output);
				if (::InterlockedDecrement(&owner->AcitveJobCount) == 0)
				{
					//SetEvent(owner->JobFinishedSignal);
				}
			}
		}

	};
}

