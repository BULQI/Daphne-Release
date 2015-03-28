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
	//boundFlag, will tell if it is left bound, rightbound? for
	//each component (x, y, z)
	//the indexArray will be 3 * 20 - maximum indexes for each element is 20
	class DllExport NtIndexMatrix
	{
	public:
		int *indexArray0; 
		int *indexArray1; //for gradient
		int *indexArray2;
		int *indexArray3;
		int boundFlag;

		NtIndexMatrix()
		{
			indexArray0 = (int *)malloc(8 * sizeof(int));
			indexArray1 = (int *)malloc(20 * sizeof(int));
			indexArray2 = (int *)malloc(20 * sizeof(int));
			indexArray3 = (int *)malloc(20 * sizeof(int));
			if (!indexArray0 || !indexArray1 || !indexArray2 || !indexArray3)
			{
				fprintf(stderr, "out of memory in NtLocalMatrix()");
				exit(1);
			}
			boundFlag = 0;
		}

		~NtIndexMatrix()
		{
			free(indexArray0);
			free(indexArray1);
			free(indexArray2);
			free(indexArray3);
		}
	};

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
		NtIndexMatrix **localMatrixArray;
		bool isToroidal;
		int NPS01;  //NodesPerSide0 * NodesPerSide1;

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

		void initialize_index_matrix(int index, NtIndexMatrix *lm);

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





