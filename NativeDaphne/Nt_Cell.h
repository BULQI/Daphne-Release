#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellSpatialState
	{
	public:
		Nt_Darray^ X;
		Nt_Darray^ V;				
		Nt_Darray^ F;

		static int SingleDim = 3;
		static int Dim = 9;

		Nt_CellSpatialState(Nt_Darray^ x, Nt_Darray^ v, Nt_Darray^ f)
		{
			X = x;
			V = v;
			F = f;
			_X = _V = _F = NULL;
		}

	public private:
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
			this->!Nt_Cell();
		}

		!Nt_Cell()
		{
			if (allocedItemCount > 0)
			{
				free(_random_samples);
				free(_driver_gradient);
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
			cell->cellIds = gcnew List<int>();
			cell->cellIds->Add(this->Cell_id);

			cell->AddCell(this);
			return cell;
		}

		void AddCell(Nt_Cell^ cell)
		{
			int itemCount = ComponentCells->Count;
			if (itemCount + 1 > allocedItemCount)
			{
				allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
				int alloc_size = allocedItemCount * 3 * sizeof(double);
				_random_samples = (double *)realloc(_random_samples, alloc_size);
				_driver_gradient = (double *)realloc(_driver_gradient, alloc_size);

				_X = (double *)realloc(_X, alloc_size);
				_V = (double *)realloc(_V, alloc_size);
				_F = (double *)realloc(_F, alloc_size);
				if (_X == NULL || _V == NULL || _F == NULL)
				{
					throw gcnew Exception("Error realloc memory");
				}
				//reassign memory address
				for (int i=0; i< itemCount; i++)
				{
					ComponentCells[i]->spatialState->X->NativePointer = _X + i * 3 * sizeof(double);
					ComponentCells[i]->spatialState->V->NativePointer = _V + i * 3 * sizeof(double);
					ComponentCells[i]->spatialState->F->NativePointer = _F + i * 3 * sizeof(double);
				}
			}
			//copy new values
			double *_xptr = _X + itemCount * 3 * sizeof(double);
			double *_vptr = _V + itemCount * 3 * sizeof(double);
			double *_fptr = _F + itemCount * 3 * sizeof(double);
			for (int i=0; i<3; i++)
			{
				_xptr[i] = cell->spatialState->X[i];
				_vptr[i] = cell->spatialState->V[i];
				_fptr[i] = cell->spatialState->F[i];
			}
			cell->spatialState->X->NativePointer = _xptr;
			cell->spatialState->V->NativePointer = _vptr;
			cell->spatialState->F->NativePointer = _fptr;
			ComponentCells->Add(cell);
			cellIds->Add(cell->Cell_id);
			array_length = ComponentCells->Count * 3;
		}

		void step(double dt);

		void RemoveCell(int cell_id)
		{
			//we may want to use linkedlist for fast removal
			int index = cellIds->IndexOf(cell_id);
			if (index == -1)return;

			ComponentCells[index]->spatialState->X->reallocate();
			ComponentCells[index]->spatialState->V->reallocate();
			ComponentCells[index]->spatialState->F->reallocate();

			int move_count = ComponentCells->Count - index -1;
			if (move_count > 0)
			{
				double *src = _X + (index+1)*3*sizeof(double);
				double *dst = src - 3 * sizeof(double);
				memmove(dst, src, move_count * 3 * sizeof(double));

				src = _V + (index+1)*3*sizeof(double);
				dst = src - 3 * sizeof(double);
				memmove(dst, src, move_count * 3 * sizeof(double));

				src = _F + (index+1)*3*sizeof(double);
				dst = src - 3 * sizeof(double);
				memmove(dst, src, move_count * 3 * sizeof(double));

				for (int i= index+1; i<ComponentCells->Count; i++)
				{
					ComponentCells[i]->spatialState->X->NativePointer -= (3 * sizeof(double));
					ComponentCells[i]->spatialState->V->NativePointer -= (3 * sizeof(double));
					ComponentCells[i]->spatialState->F->NativePointer -= (3 * sizeof(double));
				}
			}
			cellIds->RemoveAt(index);
			ComponentCells->RemoveAt(index);
			array_length = ComponentCells->Count * 3;
		}

	private:
		double *_driver_gradient; //for chemotaxis
		//double *_driverConc;
		double *_random_samples; //for Stochasitic - random sampling data

		//specialstate arrays
		double *_X;
		double *_V;
		double *_F;

		int allocedItemCount;
		int array_length;

	};
}