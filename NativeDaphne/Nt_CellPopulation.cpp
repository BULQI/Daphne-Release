#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include "NtUtility.h"
#include "Nt_CellPopulation.h"
#include "Nt_Grid.h"

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
			NtUtility::cell_apply_boundary_force(array_length, _X, ECSExtentLimit, radius, Nt_CellManager::PairPhi1, _F);
		}

		//handle chemotaxis
		if (this->isChemotactic)
		{
			double *driverConc = ComponentCells[0]->Driver->ConcPointer;
			if (TransductionConstant != -1) //TransductionConstant all same
			{
				NtUtility::cell_apply_chemotactic_force(array_length, TransductionConstant, driverConc, _F);
			}
			else //TransductionConstant all different
			{
				NtUtility::cell_apply_chemotactic_force2(array_length, driverConc, _TransductionConstant, _F);
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
				_F[3] = 0;
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

		//compute gridIndex
		double gridStepInverse = Nt_Grid::static_GridStepInverse;
		array<int> ^gridPts = Nt_Grid::static_GridPts;
		for (int i=0; i< array_length; i+= 4)
		{
			int x = (int)_X[i] * gridStepInverse;
			int y = (int)_X[i+1] * gridStepInverse;
			int z = (int)_X[i+2] * gridStepInverse;
			if ( (unsigned)x < (unsigned)gridPts[0] && (unsigned)y < (unsigned)gridPts[1] || (unsigned)z >(unsigned)gridPts[2])
			{
				//fourth elements signal the index has changed.
				_GridIndex[i+3] = (x == _GridIndex[i] && y == _GridIndex[i+1] && z == _GridIndex[i+2]) ? 0 : 1;
				_GridIndex[i] = x;
				_GridIndex[i+1] = y;
				_GridIndex[i+2] = z;
			}
			else if (_GridIndex[i] != -1) 
			{
				_GridIndex[i] = -1;
				_GridIndex[i+1] = -1;
				_GridIndex[i+2] = -1;
				_GridIndex[i+3] = -1;
			}
		}

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
