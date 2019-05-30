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


