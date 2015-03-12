#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellSpatialState
	{
	public:
		array<double>^ X;
		array<double>^ V;				
		array<double>^ F;

		static int SingleDim = 3;
		static int Dim = 9;

		Nt_CellSpatialState(array<double>^ x, array<double>^ v, array<double>^ f)
		{
			X = x;
			V=  v;
			F = f;
			_X = _V = _F = NULL;
		}

		void updateManaged()
		{
			for (int i=0; i< X->Length; i++)
			{
				X[i] = _X[i];
				V[i] = _V[i];
				F[i] = _F[i];
			}
		}

		void updateUnmanaged()
		{
			if (!_X || !_V || !_F) return; //??
			for (int i=0; i< X->Length; i++)
			{
				_X[i] = X[i];
				_V[i] = V[i];
				_F[i] = F[i];
			}
		}

		void updateManagedX()
		{
			if (_X == NULL)return; //not initialized yet.
			X[0] = _X[0]; X[1] = _X[1]; X[2] = _X[2];
		}

		void updateUnmanagedX()
		{
			if (!_X) return;
			_X[0] = X[0]; _X[1] = X[1]; _X[2] = X[2];
		}

		void updateManagedV()
		{
			if (!_V)return;
			V[0] = _V[0]; V[1] = _V[1]; V[2] = _V[2];
		}

		void updateUnmanagedV()
		{
			if (!_V) return;
			_V[0] = V[0]; _V[1] = V[1]; _V[2] = V[2];
		}

		void updateManagedF()
		{
			if (!_F)return;
			F[0] = _F[0]; F[1] = _F[1]; F[2] = _F[2];
		}

		void updateUnmanagedF()
		{
			if (!_F) return;
			_F[0] = F[0]; _F[1] = F[1]; _F[2] = F[2];
		}

	//private:
		double *_X;
		double *_V;
		double *_F;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Cell
	{
	//these are all set as public for testing, change
	//later when necessary.
	public:
		int Cell_id;
		int Population_id;
		double radius;

		bool alive;
		bool exiting;
		bool isMotile;
		Nt_MolecularPopulation^ Driver; //point cells->cytosol->puplulaiton[driver-key]
		double TransductionConstant;
		double DragCoefficient;
		double Sigma;
		bool IsChemotactic;
		bool IsStochastic;
		bool cytokinetic;
		Nt_CellSpatialState ^spatialState;


		static int count = 0;


		List<int> ^cellIds;

		List<Nt_Cell^>^ ComponentCells;

		Nt_Cell()
		{
			cellIds = gcnew List<int>();
			allocedItemCount = 0;
		}

		Nt_Cell(int cid, double r)
		{
			Cell_id = cid;
			radius = r;
			cellIds = gcnew List<int>();
			allocedItemCount = 0;
		}

		~Nt_Cell()
		{
			//this->!Nt_Cell();
		}

		!Nt_Cell()
		{
			if (allocedItemCount > 0)
			{
				free(_random_samples);
				free(_X);
				free(_V);
				free(_F);
			}
		}

		Nt_Cell^ CloneParent()
		{
			Nt_Cell ^cell = gcnew Nt_Cell(-1, radius);
			cell->Population_id = this->Population_id;
			cell->TransductionConstant = this->TransductionConstant;
			cell->DragCoefficient = this->DragCoefficient;
			cell->Sigma = this->Sigma;
			cell->TransductionConstant;
			cell->DragCoefficient = this->DragCoefficient;

			cell->IsChemotactic = this->IsChemotactic;
			cell->IsStochastic = this->IsStochastic;
			cell->cytokinetic = this->cytokinetic;
			cell->ComponentCells = gcnew List<Nt_Cell^>();
			cell->ComponentCells->Add(this);
			cell->cellIds = gcnew List<int>();
			cell->cellIds->Add(this->Cell_id);
			return cell;
		}

		void initialize()
		{
			int itemCount = ComponentCells->Count;
			if (itemCount > allocedItemCount)
			{
				free(_random_samples);
				free(_driver_gradient);
				free(_X);
				free(_V);
				free(_F);

				allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
				int alloc_size = allocedItemCount * 3 * sizeof(double);
				_random_samples = (double *)malloc(alloc_size);
				_driver_gradient = (double *)malloc(alloc_size);
				_X = (double *)malloc(alloc_size);
				_V = (double *)malloc(alloc_size);
				_F = (double *)malloc(alloc_size);
				array_length = itemCount * 3;
			}

			int n = 0;
			for (int i=0; i< ComponentCells->Count; i++)
			{
				Nt_CellSpatialState^ state = ComponentCells[i]->spatialState;
				array<double>^ x = state->X;
				array<double>^ v = state->V;
				array<double>^ f = state->F;
				state->_X = _X + n;
				state->_V = _V + n;
				state->_F = _F + n;
				for (int j = 0; j< 3; j++, n++)
				{
					_X[n] = x[j];
					_V[n] = v[j];
					_F[n] = f[j];
				}

			}
			initialized = true;
		}

		void step(double dt);

		void AddCell(Nt_Cell ^cell)
		{
			ComponentCells->Add(cell);
			initialized = false;
		}

		void RemoveCell(int cell_id)
		{
			//we may want to use linkedlist for fast removal
			int index = cellIds->IndexOf(cell_id);
			if (index == -1)return;
			cellIds->RemoveAt(index);
			ComponentCells->RemoveAt(index);
			initialized = false;
		}

	private:
		double *_driver_gradient; //for chemotaxis
		//double *_driverConc;
		double *_random_samples; //for Stochasitic - random sampling data

		//specialstate arrays
		double *_X;
		double *_V;
		double *_F;

		bool initialized;
		int allocedItemCount;
		int array_length;

	};
}