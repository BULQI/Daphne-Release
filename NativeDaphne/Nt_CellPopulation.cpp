#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include "NtUtility.h"
#include "Nt_CellPopulation.h"

#include <vcclr.h>

using namespace std;
using namespace System;
using namespace System::Collections::Generic;
using namespace NativeDaphneLibrary;

namespace NativeDaphne
{

	void Nt_CellPopulation::initialize()
	{
		array<double> ^extent = Nt_CellManager::EnvironmentExtent;
		for (int i=0; i< 3; i++)
		{
			ECSExtentLimit[i] = extent[i] - radius;
		}
		Cytosol->initialize();
		PlasmaMembrane->initialize();	
		initialized = true;
	}

	void Nt_CellPopulation::step(double dt)
		{
			if (!initialized)initialize();

			Cytosol->step(dt);

			PlasmaMembrane->step(dt);

			if (!isMotile || ComponentCells->Count == 0)return;

			//handle boudnaryForce - only cells that are close to boudnary needs this.
			if (Nt_CellManager::boundaryForceFlag == true)
			{
				NtUtility::apply_boundary_force(array_length, _X, ECSExtentLimit, radius, Nt_CellManager::PairPhi1, _F);
			}

			//handle chemotaxis
			if (this->isChemotactic)
			{
				double *driverConc = ComponentCells[0]->Driver->ConcPointer;
				if (TransductionConstant != -1)
				{
					NtUtility::daxpy3_skip1(array_length, TransductionConstant, driverConc, _F);
				}
				else 
				{
					NtUtility::dxypz3_skip1(array_length, driverConc, _TransductionConstant, _F);
				}
			}

			//stochastic
			if (this->isStochastic)
			{
				Nt_CellManager::normalDist->Sample(array_length, _random_samples);
				if (Sigma != -1)
				{
					double factor = Sigma /Math::Sqrt(dt);
					daxpy(array_length, factor, _random_samples, 1, _F, 1);
				}
				else 
				{
					double sqrt_dt = Math::Sqrt(dt);
					for (int i=0; i< array_length; i++)
					{
						_F[i] += _random_samples[i] * _Sigma[i]/sqrt_dt;
					}
				}
			
			}
			//handle movement
			daxpy(array_length, dt, _V, 1, _X, 1);

			if (DragCoefficient != -1)
			{
				dscal(array_length, 1.0 - dt*DragCoefficient, _V, 1);
			}
			else 
			{
				for (int i=0; i< array_length; i++)
				{
					_V[i] *= (1.0 - dt * _DragCoefficient[i]);
				}

			}
			daxpy(array_length, dt, _F, 1, _V, 1);

			//reset cell force
			memset(_F, 0, array_length*sizeof(double));

		}
}
