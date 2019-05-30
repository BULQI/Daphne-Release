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
#include "NtUtility.h"
#include "Nt_CellPopulation.h"
#include "Nt_Grid.h"

#include <vcclr.h>

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
			double *random_samples = Nt_CellManager::normalDist->GetSample(array_length);
			//Nt_CellManager::normalDist->Sample(array_length, _random_samples);
			//to do remove random samples..
			if (Sigma != -1)
			{
				double factor = Sigma /Math::Sqrt(dt);
				daxpy(array_length, factor, random_samples, 1, _F, 1);
				_F[3] = 0;
			}
			else 
			{
				double sqrt_dt = Math::Sqrt(dt);
				for (int i=0; i< array_length; i++)
				{
					_F[i] += random_samples[i] * _Sigma[i]/sqrt_dt;
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
		//memset(_F, 0, array_length*sizeof(double));

		//apply boundary condition - BoundaryBC
		array<double>^ extents = Nt_CellManager::EnvironmentExtent;
		if (Nt_CellManager::ECS_flag == true && Nt_CellManager::ECS_IsToroidal == true)
		{
			
			for (int i=0; i< array_length; i+= 4)
			{
				if (_X[i] < 0.0)
				{
					_X[i] = extents[0] - Nt_Cell::SafetySlab;
				}
				else if (_X[i] > extents[0])
				{
					_X[i] = 0.0;
				}
				
				if (_X[i+1] < 0.0)
				{
					_X[i+1] = extents[1] - Nt_Cell::SafetySlab;
				}
				else if (_X[i+1] > extents[1])
				{
					_X[i+1] = 0.0;
				}
			
				if (_X[i+2] < 0.0)
				{
					_X[i+2] = extents[2] - Nt_Cell::SafetySlab;
				}
				else if (_X[i+2] > extents[2])
				{
					_X[i+2] = 0.0;
				}
			}
		}
		else 
		{
			for (int i=0; i< array_length; i+= 4)
			{
				if (_X[i] < 0.0 || _X[i] > extents[0] || _X[i+1] < 0.0 || _X[i+1] > extents[1] || _X[i+2] < 0.0 || _X[i+2] > extents[2])
				{
					ComponentCells[i/4]->Exiting = true;
					
				}
			}
		}

		//computing grid index.
		double gridStepInverse = Nt_Grid::static_GridStepInverse;
		array<int> ^gridPts = Nt_Grid::static_GridPts;
		bool cellGridChanged = true;
		for (int i=0; i< array_length; i+= 4)
		{
			int x = (int)(_X[i] * gridStepInverse);
			int y = (int)(_X[i+1] * gridStepInverse);
			int z = (int)(_X[i+2] * gridStepInverse);
			if ( (unsigned)x < (unsigned)gridPts[0] && (unsigned)y < (unsigned)gridPts[1] && (unsigned)z < (unsigned)gridPts[2])
			{
				//fourth elements signal the index has changed.
				if (x != _GridIndex[i] || y != _GridIndex[i+1] || z != _GridIndex[i+2])
				{
					_GridIndex[i] = x;
					_GridIndex[i+1] = y;
					_GridIndex[i+2] = z;
					_GridIndex[i+3] = 1;
					cellGridChanged = true;
				}
			}
			else if (_GridIndex[i] != -1) 
			{
				_GridIndex[i] = -1;
				_GridIndex[i+1] = -1;
				_GridIndex[i+2] = -1;
				_GridIndex[i+3] = 1;
				cellGridChanged = true;
			}
		}

		if (cellGridChanged == true)
		{
			Nt_CollisionManager::CellGridIndexChanged = true;
		}

	}
}
