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
#include <malloc.h>
#include <cmath>
#include <acml.h>

#include "NtUtility.h"
#include "Nt_Cell.h"
#include "Nt_CellManager.h"
#include "Nt_CollisionManager.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{
	void Nt_Cell::updateGridIndex()
	{
		double gridStepInverse = Nt_Grid::static_GridStepInverse;
		array<int> ^gridPts = Nt_Grid::static_GridPts;

		double *X = this->SpatialState->X->NativePointer;
		int x = (int)(X[0] * gridStepInverse);
		int y = (int)(X[1] * gridStepInverse);
		int z = (int)(X[2] * gridStepInverse);
		bool cellGridChanged = false;
		if ( (unsigned)x < (unsigned)gridPts[0] && (unsigned)y < (unsigned)gridPts[1] && (unsigned)z < (unsigned)gridPts[2])
		{
			//fourth elements signal the index has changed.
			if (x != GridIndex[0] || y != GridIndex[1] || z != GridIndex[2])
			{
				GridIndex[0] = x;
				GridIndex[1] = y;
				GridIndex[2] = z;
				GridIndex[3] = 1;
				cellGridChanged = true;
			}
		}
		else if (GridIndex[0] != -1) 
		{
			GridIndex[0] = -1;
			GridIndex[1] = -1;
			GridIndex[2] = -1;
			GridIndex[3] = 1;
			cellGridChanged = true;
		}
		if (cellGridChanged == true)
		{
			Nt_CollisionManager::CellGridIndexChanged = true;
		}
	}



}