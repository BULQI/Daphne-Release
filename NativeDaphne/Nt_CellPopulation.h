#pragma once

#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Cell.h"
#include "Nt_CellManager.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellPopulation
	{

	private:

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
        /// The radius of the cell
        /// </summary>
        double radius;

		void AddGene(Nt_Gene ^gene)
		{
			for (int i=0; i< genes->Count; i++)
			{
				if (genes[i]->Name == gene->Name)
				{
					genes[i]->AddGene(gene);
					return;
				}
			}
			Nt_Gene^ container = gene->CloneParent();
			genes->Add(container);
		}

	
	public:
		int PopulationId;

		/// <summary>
		/// create a new instance of Nt_CellManger
		/// </summary>
		/// <returns>none</returns>
		Nt_CellPopulation(void)
		{
			ComponentCells = gcnew List<Nt_Cell^>();
			Cytosol = gcnew Nt_Cytosol(Nt_Cell::defaultRadius, Nt_ManifoldType::TinyBallCollection);
			PlasmaMembrane = gcnew Nt_PlasmaMembrane(Nt_Cell::defaultRadius, Nt_ManifoldType::TinySphereCollection);
			genes = gcnew List<Nt_Gene ^>();
			initialized = false;
			deadCells = gcnew Dictionary<int, Nt_Cell^>();
			ntCellDictionary = gcnew Dictionary<int, Nt_Cell^>();

			_Sigma = NULL;
			_TransductionConstant = NULL;
			_DragCoefficient = NULL;
			_X = NULL;
			_V = NULL;
			_F = NULL;
			_random_samples = NULL;

			ECSExtentLimit = (double *)malloc(3 *sizeof(double));
		}

		~Nt_CellPopulation(void)
		{
			this->!Nt_CellPopulation();
		}

		!Nt_CellPopulation(void)
		{
			if (allocedItemCount > 0)
			{
				free(_random_samples);
//				free(_driver_gradient);
				free(_X);
				free(_V);
				free(_F);
				free(_Sigma);
				free(_TransductionConstant);
				free(_DragCoefficient);
			}
			free(ECSExtentLimit);
		}
				
		void AddMolecularPopulation(bool isCytosol, Nt_MolecularPopulation^ molpop)
		{
			if (isCytosol)
			{
				Cytosol->AddMolecularPopulation(molpop);
			}
			else 
			{
				PlasmaMembrane->AddMolecularPopulation(molpop);
			}
		}

		void AddCell(Nt_Cell ^cell)
		{
			if (ComponentCells->Count == 0)
			{
				this->PopulationId = cell->Population_id;
				this->TransductionConstant = cell->TransductionConstant;
				this->DragCoefficient = cell->DragCoefficient;
				this->Sigma = cell->Sigma;

				this->isMotile = cell->IsMotile;
				this->isChemotactic = cell->IsChemotactic;
				this->isStochastic = cell->IsStochastic;

				this->radius = cell->Radius;
				Cytosol->CellRadius = cell->Radius;
				PlasmaMembrane->CellRadius = cell->Radius;
			}

			//add cell data
			{
				int itemCount = ComponentCells->Count;
				if (itemCount + 1 > allocedItemCount)
				{
					allocedItemCount = NtUtility::GetAllocSize(itemCount+1, allocedItemCount);
					int alloc_size = allocedItemCount * 3 * sizeof(double);
					_random_samples = (double *)realloc(_random_samples, alloc_size);
//					_driver_gradient = (double *)realloc(_driver_gradient, alloc_size);
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
						ComponentCells[i]->SpatialState->X->NativePointer = _X + i * 3;
						//ComponentCells[i]->nt_cell->X = _X + i * 3;
						ComponentCells[i]->SpatialState->V->NativePointer = _V + i * 3;
						ComponentCells[i]->SpatialState->F->NativePointer = _F + i * 3;
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
					_xptr[i] = cell->SpatialState->X[i];
					_vptr[i] = cell->SpatialState->V[i];
					_fptr[i] = cell->SpatialState->F[i];
				}
				cell->SpatialState->X->NativePointer = _xptr;
				//cell->nt_cell->X = _xptr;
				cell->SpatialState->V->NativePointer = _vptr;
				cell->SpatialState->F->NativePointer = _fptr;
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


			//here we should have a boundary id for the plasmamembrane.
			Cytosol->AddNtBoundary(0, cell->plasmaMembrane->InteriorId, cell->plasmaMembrane);
			//if (masterCell == nullptr)
			//{
			//	masterCell = cell->CloneParent();
			//}
			//else 
			//{
			//	masterCell->AddCell(cell);
			//}
			for each (KeyValuePair<String^, Nt_Gene^>^ kvp in cell->Genes)
			{
				this->AddGene(kvp->Value);
			}
			//add molecular population before add reactions.
			this->Cytosol->AddMolecularPopulation(cell->cytosol->NtPopulations);
			this->PlasmaMembrane->AddMolecularPopulation(cell->plasmaMembrane->NtPopulations);

			this->Cytosol->AddCompartmentReactions(cell->cytosol);
			this->PlasmaMembrane->AddCompartmentReactions(cell->plasmaMembrane);
			ntCellDictionary->Add(cell->Cell_id, cell);
		}

		void RemoveCell(int cell_id, bool completeRemoval)
		{
			Nt_Cell^ cell = ntCellDictionary[cell_id];
			if (cell == nullptr)
			{
				throw gcnew Exception("Error mid layer RemoveCell");
			}
			if (completeRemoval == true)
			{
				if (deadCells->ContainsKey(cell_id) == true)
				{
					deadCells->Remove(cell_id);
					fprintf(stderr, "Dead cell id=%d removed from system.\n", cell_id);
					return;
				}
				fprintf(stderr, "Removing cell id=%d current cell Count = %d\n", cell_id, ntCellDictionary->Count);
			}
			else
			{
				//only called for dead cell
				fprintf(stderr, "Cell id=%d is marked as dead\n", cell_id);
			}
			
			int index = this->GetCellIndex(cell);

			//remove cell spaticalState etc.
			this->RemoveCell(cell);

			//at this point, remove the cells data from molpop, reactions etc.

			this->Cytosol->RemoveMemberCompartment(index);

			this->PlasmaMembrane->RemoveMemberCompartment(index);

			//remove genes
			for (int i=0; i< genes->Count; i++)
			{
				genes[i]->RemoveGene(index);
			}
			//remove membrane boundary from cytosol and update boundaryId index in the cytosol collection
			Cytosol->RemoveNtBoundary(cell->plasmaMembrane->InteriorId);

			if (completeRemoval == false)
			{
				deadCells->Add(cell->Cell_id, cell);
				return;
			}
			ntCellDictionary->Remove(cell_id);
		}

		//remove cells spatialStates, sigma, transdcutionConsant and Dragcoefficient from array
		int RemoveCell(Nt_Cell ^c)
		{
			//debug
			double *xptr = c->SpatialState->X->NativePointer;
			int index = (int)(xptr - _X)/c->SpatialState->X->Length;
			if (index < 0 || index >= ComponentCells->Count || ComponentCells[index] != c)
			{
				throw gcnew Exception("Error removing cells - index out of range");
			}

			int last_index = ComponentCells->Count -1;
			Nt_Cell^ last_cell =  ComponentCells[last_index];
			if (c != last_cell)
			{
				//swap cell contents
				c->SpatialState->X->MemSwap(last_cell->SpatialState->X);
				c->SpatialState->V->MemSwap(last_cell->SpatialState->V);
				c->SpatialState->F->MemSwap(last_cell->SpatialState->F);
				ComponentCells[index] = last_cell;
				ComponentCells[last_index] = c;

				_Sigma[index] = _Sigma[last_index];
				_TransductionConstant[index] = _TransductionConstant[last_index];
				_DragCoefficient[index] = _DragCoefficient[last_index];
			}
			//deatch the data's storage
			c->SpatialState->X->detach();
			c->SpatialState->V->detach();
			c->SpatialState->F->detach();

			//cellIds->RemoveAt(last_index);
			ComponentCells->RemoveAt(last_index);
			array_length = ComponentCells->Count * 3;
			return index;
		}

		int GetCellIndex(Nt_Cell^ c)
		{
			double *xptr = c->SpatialState->X->NativePointer;
			int index = (int)(xptr - _X)/c->SpatialState->X->Length;
			if (index < 0 || index >= ComponentCells->Count || ComponentCells[index] != c)
			{
				throw gcnew Exception("Error GetCellIndex - index out of range");
			}
			return index;
		}


		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <returns>void</returns>	
		void Clear()
		{
			initialized = false;
			//cell special states information
			//todo check disopose of these objects
			Cytosol = gcnew Nt_Cytosol(Nt_Cell::defaultRadius);
			PlasmaMembrane = gcnew Nt_PlasmaMembrane(Nt_Cell::defaultRadius);
			ComponentCells->Clear();
			deadCells->Clear();
			ntCellDictionary->Clear();
		}

		void step(double dt);
		
		void initialize();

	private:


		List<Nt_Cell^>^ ComponentCells;

		//double *_driver_gradient; //for chemotaxis
		double *_random_samples; //for Stochasitic - random sampling data

		//specialstate arrays
		double *_X;
		double *_V;
		double *_F;

		//used if all cells have identical value, otherwise = -1.0.
		double TransductionConstant;
		double DragCoefficient;
		double Sigma;

		//used if they are distributions
		double *_Sigma;
		double *_TransductionConstant;
		double *_DragCoefficient;

		double *ECSExtentLimit;

		int allocedItemCount;
		int array_length;



		//this will handle the cells force etcs.
		//Nt_Cell^ masterCell;

		//we may want to move the stochastic death method
		//to the middle layer.
		Dictionary<int, Nt_Cell^>^ deadCells;

		Nt_Cytosol ^Cytosol;

		Nt_PlasmaMembrane ^PlasmaMembrane;

		List<Nt_Gene^> ^genes;

		//this exists so that when we are given an Cell_ID, we can get the cell object
		Dictionary<int, Nt_Cell^>^ ntCellDictionary;

		bool initialized;
	};
}
