#pragma once

#include <errno.h>
#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_DArray.h"
#include "NtCellPair.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

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

	internal:
		double *_X;
		double *_V;
		double *_F;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Cell
	{
	private:
		Nt_CellSpatialState ^spatialState;
	public:
		//how to get this updated??
		static int SafeCell_id = 0;

		int Cell_id;
		int Population_id;
		int Membrane_id;
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
		int* GridIndex;
		int* PreviousGridIndex;


		static int count = 0;


		List<int> ^cellIds;

		List<Nt_Cell^>^ ComponentCells;

		NtCell *nt_cell;

		Nt_Cell()
		{
			cellIds = gcnew List<int>();
			//only need 3, but 4 needed when using ssc2
			GridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex[0] = -1; //evaludate into invalid
			allocedItemCount = 0;
			nt_cell = new NtCell(radius, GridIndex);
		}

		Nt_Cell(int cid, double r)
		{
			Cell_id = cid;
			radius = r;
			cellIds = gcnew List<int>();
			GridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex[0] = -1;
			PreviousGridIndex[3] = 0; //indicate invalid
			allocedItemCount = 0;
			nt_cell = new NtCell(radius, GridIndex);
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
				free(GridIndex);
				delete nt_cell;
			}
		}

		property double Radius
		{
			double get()
			{
				return radius;
			}
		}

		property Nt_CellSpatialState^ SpatialState
        {
			Nt_CellSpatialState^ get()
			{
				return spatialState;
				
			}

			void set(Nt_CellSpatialState^ value)
			{
				spatialState = value;
				nt_cell->X = spatialState->X->NativePointer;
				nt_cell->F = spatialState->F->NativePointer;
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
					int tt = errno;
					throw gcnew Exception("Error realloc memory");
				}
				//reassign memory address
				for (int i=0; i< itemCount; i++)
				{
					ComponentCells[i]->spatialState->X->NativePointer = _X + i * 3;
					ComponentCells[i]->nt_cell->X = _X + i * 3;
					ComponentCells[i]->spatialState->V->NativePointer = _V + i * 3;
					ComponentCells[i]->spatialState->F->NativePointer = _F + i * 3;
					ComponentCells[i]->nt_cell->F = _F + i * 3;

				}
			}
			//copy new values
			double *_xptr = _X + itemCount * 3;
			double *_vptr = _V + itemCount * 3;
			double *_fptr = _F + itemCount * 3;
			for (int i=0; i<3; i++)
			{
				_xptr[i] = cell->spatialState->X[i];
				_vptr[i] = cell->spatialState->V[i];
				_fptr[i] = cell->spatialState->F[i];
			}
			cell->spatialState->X->NativePointer = _xptr;
			cell->nt_cell->X = _xptr;
			cell->spatialState->V->NativePointer = _vptr;
			cell->spatialState->F->NativePointer = _fptr;
			cell->nt_cell->F = _fptr;
			ComponentCells->Add(cell);
			cellIds->Add(cell->Cell_id);
			array_length = ComponentCells->Count * 3;
		}

		void addForce(array<double>^ f)
        {
            spatialState->F[0] += f[0];
            spatialState->F[1] += f[1];
            spatialState->F[2] += f[2];
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