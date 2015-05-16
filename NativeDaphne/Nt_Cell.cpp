#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>
#include <acml.h>

#include "NtUtility.h"
#include "Nt_Cell.h"
#include "Nt_CellManager.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{
	void Nt_Cell::step(double dt)
	{
		//handle boudnaryForce - only cells that are close to boudnary needs this.
		if (Nt_CellManager::boundaryForceFlag == true)
		{
			array<double> ^extent = Nt_CellManager::EnvironmentExtent;
			double radius_inverse = 1.0/radius;
			double PairPhi1 = Nt_CellManager::PairPhi1;
			double dist = 0;
			for (int i=0; i< array_length; i++)
			{
				//check left boundary
				if (_X[i] < radius)
				{
					if (_X[i] == 0)continue;
					_F[i] += PairPhi1 *(1.0/_X[i] - radius_inverse); //1.0/radius
				}
				//check right boundary
				else if ( (dist= extent[i%3] - _X[i]) < radius)
				{
					//normal would be (-1) for the right bound!
					_F[i] -= PairPhi1 *(1.0/dist - radius_inverse);
				}
			}
		}

		//handle chemotaxis
		if (this->isChemotactic)
		{
			int n = 0;
			double *gradient = _driver_gradient;
			for (int i=0; i < ComponentCells->Count; i++)
			{
				//copy gradient (the last 3 elements) of DriverConc
				double *src = ComponentCells[i]->Driver->NativePointer + 1;
				*gradient++ = *src++;
				*gradient++ = *src++;
				*gradient++ = *src++;
			}
			daxpy(array_length, TransductionConstant, _driver_gradient, 1, _F, 1);
		}

		//stochastic
		if (this->isStochastic)
		{
			Nt_CellManager::normalDist->Sample(array_length, _random_samples);
			double factor = Sigma /Math::Sqrt(dt);
			daxpy(array_length, factor, _random_samples, 1, _F, 1);
			
		}
		//handle movement
		daxpy(array_length, dt, _V, 1, _X, 1);
		dscal(array_length, 1.0 - dt*DragCoefficient, _V, 1);
		daxpy(array_length, dt, _F, 1, _V, 1);

		//reset cell force
		memset(_F, 0, array_length*sizeof(double));
	}
}