#include "stdafx.h"
#include "Utility.h"
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

		MaxSeparation2 = MaxSeparation * MaxSeparation;
		remove_flag = false;
		b_ij = 0;
		distance = 0;

		MaxSepArr = (int *)malloc(4 *sizeof(int));
		MaxSepArr[0] = MaxSepArr[1] = MaxSepArr[3] = MaxSeparation;
	}

	void NtCellPair::set_distance()
	{
		double *a_X = a->X;
		double *b_X = b->X;

        double x = a_X[0] - b_X[0];
        double y = a_X[1] - b_X[1];
        double z = a_X[2] - b_X[2];
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

		if (dx > NtCollisionManager::GridSize[0] * 0.5) x = NtCollisionManager::GridSize[0] - dx;
		if (dy > NtCollisionManager::GridSize[1] * 0.5) y = NtCollisionManager::GridSize[1] - dy;
		if (dz > NtCollisionManager::GridSize[2] * 0.5) z = NtCollisionManager::GridSize[2] - dz;

        distance = sqrt(x * x + y * y + z * z);
	}
}