#include "stdafx.h"
#include "NtUtility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "NtCellPair.h"
#include "NtCollisionManager.h"

namespace NativeDaphneLibrary
{

	NtCellPair::NtCellPair(long key, NtCell* _a, NtCell* _b)
	{
		X1 = _a->X;
		F1 = _a->F;
		X2 = _b->X;
		F2 = _b->F;
		sumRadius = _a->radius + _b->radius;
		sumRadius2 = sumRadius * sumRadius;
		distance = 0;
		pairKey = key;
	}

	void NtCellPair::set_distance()
	{
        double x = X1[0] - X2[0];
        double y = X1[1] - X2[1];
        double z = X1[2] - X2[2];
		double tmp = x * x + y * y + z * z;
		//when the distance is large than squre, no use to do sqrt.
		distance = tmp > sumRadius2 ? sumRadius2 : sqrt(tmp);
	}

	void NtCellPair::set_distance_toroidal()
	{
		double x = X1[0] - X2[0];
        double y = X1[1] - X2[1];
        double z = X1[2] - X2[2];

		double dx = x > 0 ? x : -x;
		double dy = y > 0 ? y : -y;
		double dz = z > 0 ? z : -z;

		double *gsize = NtCollisionManager::GridSize;
		if (dx > gsize[0] * 0.5) x = gsize[0] - dx;
		if (dy > gsize[1] * 0.5) y = gsize[1] - dy;
		if (dz > gsize[2] * 0.5) z = gsize[2] - dz;
		
		double tmp = x * x + y * y + z * z;
        distance = tmp > sumRadius2 ? sumRadius2 : sqrt(tmp);
	}
}