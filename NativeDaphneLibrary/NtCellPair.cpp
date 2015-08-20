#include "stdafx.h"
#include "NtUtility.h"
#include <stdexcept>
#include <acml.h>

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "NtCellPair.h"
#include "NtCollisionManager.h"

using namespace std;

namespace NativeDaphneLibrary
{
	int NtCellPair::int4buf[4];

	NtCellPair::NtCellPair(NtCell* _a, NtCell* _b)
	{
		a = _a;
		b = _b;
		sumRadius = a->radius + b->radius;
		sumRadius2 = sumRadius * sumRadius;
		sumRadiusInverse = 1.0/sumRadius;

		double tmp = sumRadius/NtCollisionManager::GridStep;
		MaxSeparation = (int)tmp;
		if (tmp > MaxSeparation)MaxSeparation++;
		b_ij = 0;
		distance = 0;

		//MaxSepArr = (int *)malloc(4 *sizeof(int));
		//MaxSepArr[0] = MaxSepArr[1] = MaxSepArr[3] = MaxSeparation;
	}

	void NtCellPair::set_distance()
	{
		double *a_X = a->X;
		double *b_X = b->X;

        double x = a_X[0] - b_X[0];
        double y = a_X[1] - b_X[1];
        double z = a_X[2] - b_X[2];
		double tmp = x * x + y * y + z * z;
		//when the distance is large than squre, no use to do sqrt.
		if (tmp > sumRadius2)
		{
			distance = tmp;
			return;
		}
        distance = sqrt(x * x + y * y + z * z);
	}

	void NtCellPair::set_distance_toroidal()
	{
		double *a_X = a->X;
		double *b_X = b->X;

        double x = a_X[0] - b_X[0];
        double y = a_X[1] - b_X[1];
        double z = a_X[2] - b_X[2];

		double dx = x > 0 ? x : -x;
		double dy = y > 0 ? y : -y;
		double dz = z > 0 ? z : -z;

		double *gsize = NtCollisionManager::GridSize;
		if (dx > gsize[0] * 0.5) x = gsize[0] - dx;
		if (dy > gsize[1] * 0.5) y = gsize[1] - dy;
		if (dz > gsize[2] * 0.5) z = gsize[2] - dz;

        distance = sqrt(x * x + y * y + z * z);
	}
}