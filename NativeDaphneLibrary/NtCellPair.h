#pragma once

#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif

#include <stdlib.h>
//#include <xmmintrin.h>
#include <emmintrin.h>
#include <smmintrin.h>


namespace NativeDaphneLibrary
{

	class DllExport NtCell
	{
	public:
		double radius;
		//bool isLegalIndex;
		int *gridIndex;
		double *X;
		double *F;
		NtCell(double _r, int *g)
		{
			radius = _r;
			gridIndex = g;
			//isLegalIndex = true;
		}
	};

	class DllExport NtCellPair
	{
	public:
		NtCell *a;
		NtCell *b;
		int MaxSeparation;
		int MaxSeparation2;
		double sumRadius;
		double sumRadius2; 
		double sumRadiusInverse;

		int b_ij;
		double distance;
		bool remove_flag;
		static int int4buf[4];
		int *MaxSepArr;

		NtCellPair(NtCell* _a, NtCell* _b);

		~NtCellPair()
		{
			//delete a;
			//delete b;
		}

		void set_distance();

		void set_distance_toroidal();

	};
}








