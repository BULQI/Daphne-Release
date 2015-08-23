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

	//do we need to keep gridindex?
	//we are already handling clear index
	//already in updategridindex, so a pair is already always clear separated!
	//that saves 8 byte.
	class DllExport NtCellPair
	{
	public:
		double *X1;			//8
		double *X2;			//8
		double *F1;			//8
		double *F2;			//8
		double sumRadius;	//8
		double sumRadius2;  //8
		double distance;	//8
		long   pairKey;		//4 byte - the key of the pair
		int	   padding;		//padding total to 64 bytes;

		NtCellPair(long key, NtCell* _a, NtCell* _b);

		void copy(NtCellPair *src)
		{
			X1 = src->X1;
			X2 = src->X2;
			F1 = src->F1;
			F2 = src->F2;
			sumRadius = src->sumRadius;
			sumRadius2 = src->sumRadius2;
			distance = src->distance;
			pairKey = src->pairKey;
		}

		~NtCellPair()
		{
		}

		void set_distance();

		void set_distance_toroidal();

		//get the cell id, index 0/1
		//is is coupled with how the pair key is made
		long get_cell_id(int index)
		{
			if (index == 0)
			{
				return (pairKey >>16);
			}
			else 
			{
				return ((pairKey <<16)>>16);
			}
		}
	};

	//class DllExport NtCellPair
	//{
	//public:
	//	NtCell *a;
	//	NtCell *b;
	//	int index;
	//	int MaxSeparation;
	//	int b_ij;
	//	double sumRadius;
	//	double sumRadius2; 
	//	double sumRadiusInverse;
	//	double distance;

	//	static int int4buf[4];

	//	NtCellPair(NtCell* _a, NtCell* _b);

	//	~NtCellPair()
	//	{
	//	}

	//	void set_distance();

	//	void set_distance_toroidal();

	//};
}


