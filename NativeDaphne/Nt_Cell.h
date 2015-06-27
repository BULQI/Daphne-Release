#pragma once

#include <errno.h>
#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_DArray.h"
//#include "NtCellPair.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"

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

		Nt_CellSpatialState()
		{
			_X = _V = _F = NULL;
		}

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
	protected:
		/// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
		bool alive;

		/// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        bool cytokinetic;

		/// <summary>
        /// a flag that signals that the cell is motile
        /// </summary>
        bool isMotile;

		/// <summary>
        /// a flag that signals that the cell responds to chemokine gradients
        /// </summary>
        bool isChemotactic;

        /// <summary>
        /// a flag that signals that the cell is subject to stochastic forces
        /// </summary>
        bool isStochastic;

		/// <summary>
        /// A flag that signals to the cell manager whether the cell is exiting the simulation space.
        /// </summary>
        bool exiting;

		/// <summary>
        /// The radius of the cell
        /// </summary>
        double radius;

		Nt_CellSpatialState ^spatialState;

		Dictionary<String^, Nt_Gene^>^ genes;

		property Nt_Compartment^ BaseCytosol
		{
			Nt_Compartment^ get()
			{
				return cytosol;
			}
			void set(Nt_Compartment^ value)
			{
				cytosol = value;
			}
		}

		property Nt_Compartment^ BasePlasmaMembrane
		{
			Nt_Compartment^ get()
			{
				return plasmaMembrane;
			}
			void set(Nt_Compartment^ value)
			{
				plasmaMembrane = value;
			}
		}

	internal:
		int *gridIndex;
		int* PreviousGridIndex;
		//to be remvoed
		int Membrane_id;

		Nt_Compartment^ cytosol;
		Nt_Compartment^ plasmaMembrane;


	public:
		int Cell_id;
		static int SafeCell_id = 0;
		static double defaultRadius = 5.0;
		property int Population_id;

		//Nt_Compartment^ cytosol;
		//Nt_Compartment^ plasmaMembrane;


		property bool IsMotile
		{
			bool get(){ return isMotile;}
			void set(bool value){ isMotile = value;}
		}

		property bool IsChemotactic
		{
			bool get(){ return isChemotactic;}
			void set(bool value){ isChemotactic = value;}
		}

		property bool IsStochastic
		{
			bool get(){ return isStochastic;}
			void set(bool value){ isStochastic = value;}
		}

		property bool Alive
		{
			bool get(){ return alive;}
			void set(bool value){ alive = value;}
		}

		property bool Cytokinetic
		{
			bool get(){ return cytokinetic;}
			void set(bool value){ cytokinetic = value;}
		}

		property bool Exiting
		{
			bool get(){ return exiting;}
			void set(bool value){ exiting = value;}
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
			}
        }

		property Nt_Iarray^ GridIndex;


		property Dictionary<String^, Nt_Gene^>^ Genes
		{
			Dictionary<String^, Nt_Gene^>^ get()
			{
				return genes;
			}
			void set(Dictionary<String^, Nt_Gene^>^ g)
			{
				genes = g;
			}
		}

		void AddGene(String^ gene_guid, Nt_Gene^ gene)
		{
			genes->Add(gene_guid, gene);
		}


		Nt_MolecularPopulation^ Driver; //point cells->cytosol->puplulaiton[driver-key]
		double TransductionConstant;
		double DragCoefficient;
		double Sigma;

		static int count = 0;

		List<Nt_Cell^>^ ComponentCells;

		//NtCell *nt_cell;

		Nt_Cell(double r)
		{	
			radius = r;
			//cellIds = gcnew List<int>();
			//only need 3, but 4 needed when using ssc2
			GridIndex = gcnew Nt_Iarray(3);
			GridIndex[0] = -1;
			GridIndex[1] = -1;
			GridIndex[2] = -1;
			gridIndex = (int*)malloc(4 *sizeof(int));
			GridIndex->NativePointer = gridIndex;
			PreviousGridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex[0] = -1; //evaludate into invalid
			allocedItemCount = 0;
			//nt_cell = new NtCell(radius, gridIndex);

			_Sigma = NULL;
			_TransductionConstant = NULL;
			_DragCoefficient = NULL;
			_X = NULL;
			_V = NULL;
			_F = NULL;
			_random_samples = NULL;
			_driver_gradient = NULL;

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
				free(gridIndex);
				free(_Sigma);
				free(_TransductionConstant);
				free(_DragCoefficient);
				//delete nt_cell;
			}
		}

		Nt_Cell^ CloneParent()
		{
			Nt_Cell ^cell = gcnew Nt_Cell(radius);
			cell->Population_id = this->Population_id;
			cell->TransductionConstant = this->TransductionConstant;
			cell->DragCoefficient = this->DragCoefficient;
			cell->Sigma = this->Sigma;
			cell->TransductionConstant = this->TransductionConstant;
			cell->DragCoefficient = this->DragCoefficient;

			cell->Alive = this->Alive;
			cell->isMotile = this->isMotile;
			cell->isChemotactic = this->isChemotactic;
			cell->isStochastic = this->isStochastic;
			cell->cytokinetic = this->cytokinetic;
			cell->ComponentCells = gcnew List<Nt_Cell^>();
			cell->AddCell(this);
			return cell;
		}

		void AddCell(Nt_Cell^ cell)
		{
			int itemCount = ComponentCells->Count;
			if (itemCount + 1 > allocedItemCount)
			{
				allocedItemCount = NtUtility::GetAllocSize(itemCount+1, allocedItemCount);
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
					//ComponentCells[i]->nt_cell->X = _X + i * 3;
					ComponentCells[i]->spatialState->V->NativePointer = _V + i * 3;
					ComponentCells[i]->spatialState->F->NativePointer = _F + i * 3;
					//ComponentCells[i]->nt_cell->F = _F + i * 3;

				}

				_Sigma = (double *)realloc(_Sigma, alloc_size * sizeof(double));
				_TransductionConstant = (double *)realloc(_TransductionConstant, alloc_size * sizeof(double));
				_DragCoefficient = (double *)realloc(_DragCoefficient, alloc_size * sizeof(double));
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
			//cell->nt_cell->X = _xptr;
			cell->spatialState->V->NativePointer = _vptr;
			cell->spatialState->F->NativePointer = _fptr;
			//cell->nt_cell->F = _fptr;

			//todo set sigma values etc....
			for (int i= itemCount *3; i < itemCount *3 + 3; i++)
			{
				_Sigma[i] = cell->Sigma;
				_TransductionConstant[i] = cell->TransductionConstant;
				_DragCoefficient[i] = cell->DragCoefficient;
			}

			if (this->Sigma != -1 && cell->Sigma != this->Sigma)
			{
				this->Sigma = -1; //indicate simga is not constant value
			}
			if (this->TransductionConstant != -1 && cell->TransductionConstant != this->TransductionConstant)
			{
				this->TransductionConstant = -1;
			}
			if (this->DragCoefficient != -1 && cell->DragCoefficient != this->DragCoefficient)
			{
				this->DragCoefficient = -1;
			}	

			ComponentCells->Add(cell);
			array_length = ComponentCells->Count * 3;
		}

		void addForce(array<double>^ f)
        {
            spatialState->F[0] += f[0];
            spatialState->F[1] += f[1];
            spatialState->F[2] += f[2];
        }

		void step(double dt);

		int RemoveCell(Nt_Cell ^c)
		{
			//debug
			double *xptr = c->spatialState->X->NativePointer;
			int index = (int)(xptr - _X)/c->spatialState->X->Length;
			if (index < 0 || index >= ComponentCells->Count || ComponentCells[index] != c)
			{
				throw gcnew Exception("Error removing cells - index out of range");
			}

			int last_index = ComponentCells->Count -1;
			Nt_Cell^ last_cell =  ComponentCells[last_index];
			if (c != last_cell)
			{
				//swap cell contents
				c->spatialState->X->MemSwap(last_cell->spatialState->X);
				c->spatialState->V->MemSwap(last_cell->spatialState->V);
				c->spatialState->F->MemSwap(last_cell->spatialState->F);
				ComponentCells[index] = last_cell;
				ComponentCells[last_index] = c;
			}
			//deatch the data's storage
			c->spatialState->X->detach();
			c->spatialState->V->detach();
			c->spatialState->F->detach();

			//cellIds->RemoveAt(last_index);
			ComponentCells->RemoveAt(last_index);
			array_length = ComponentCells->Count * 3;
			return index;
		}

		void RemoveCell(int index)
		{
			if (index < 0 || index >= ComponentCells->Count)
			{
				throw gcnew Exception("Error RemoveCell: index out of range");
			}

			RemoveCell(ComponentCells[index]);
		}

		int GetCellIndex(Nt_Cell^ c)
		{
			double *xptr = c->spatialState->X->NativePointer;
			int index = (int)(xptr - _X)/c->spatialState->X->Length;
			if (index < 0 || index >= ComponentCells->Count || ComponentCells[index] != c)
			{
				throw gcnew Exception("Error GetCellIndex - index out of range");
			}
			return index;
		}

		private:

		double *_driver_gradient; //for chemotaxis
		//double *_driverConc;
		double *_random_samples; //for Stochasitic - random sampling data

		//specialstate arrays
		double *_X;
		double *_V;
		double *_F;

		//used if they are distributions
		double *_Sigma;
		double *_TransductionConstant;
		double *_DragCoefficient;

		int allocedItemCount;
		int array_length;

	};
}