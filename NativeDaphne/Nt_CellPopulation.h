#pragma once

#include "NtUtility.h"
#include "NTRandomNumberGenerator.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Cell.h"
#include "Nt_CellManager.h"
#include "Nt_CollisionManager.h"

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
		/// create a new instance of Nt_CellManager
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
			_GridIndex = NULL;
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
				_aligned_free(_random_samples);
				_aligned_free(_X);
				_aligned_free(_V);
				_aligned_free(_F);
				_aligned_free(_GridIndex);
				_aligned_free(_Sigma);
				_aligned_free(_TransductionConstant);
				_aligned_free(_DragCoefficient);
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
			//inform collision manager that the cell is entering the system
			cell->gridIndex[3] = 0;
			cell->updateGridIndex();
			if (cell->gridIndex[3] != 0)
			{
				Nt_CollisionManager::CellGridIndexChanged = true;
			}

			if (cell->Alive == false)
			{
				//it is possible to add dead cell, when loading from saved experment
				cell->nt_cell->X = cell->SpatialState->X->NativePointer;
				cell->nt_cell->F = cell->SpatialState->F->NativePointer;
				deadCells->Add(cell->Cell_id, cell);
				return;
			}
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
					int alloc_size = allocedItemCount * 4 * sizeof(double);
					_random_samples = (double *)_aligned_realloc(_random_samples, alloc_size, 32);
					_X = (double *)_aligned_realloc(_X, alloc_size, 32);
					_V = (double *)_aligned_realloc(_V, alloc_size, 32);
					_F = (double *)_aligned_realloc(_F, alloc_size, 32);
					_GridIndex = (int *)_aligned_realloc(_GridIndex, allocedItemCount * 4 * sizeof(int), 16);
					if (_X == NULL || _V == NULL || _F == NULL || _GridIndex == NULL)
					{
						throw gcnew Exception("Error realloc memory");
					}
					//reassign memory address
					for (int i=0; i< itemCount; i++)
					{
						ComponentCells[i]->SpatialState->X->NativePointer = _X + i * 4;
						ComponentCells[i]->SpatialState->V->NativePointer = _V + i * 4;
						ComponentCells[i]->SpatialState->F->NativePointer = _F + i * 4;
						ComponentCells[i]->GridIndex->NativePointer = _GridIndex + i * 4;
						ComponentCells[i]->gridIndex = _GridIndex + i * 4;
					}

					_Sigma = (double *)_aligned_realloc(_Sigma, alloc_size * sizeof(double), 32);
					_TransductionConstant = (double *)_aligned_realloc(_TransductionConstant, alloc_size * sizeof(double), 32);
					_DragCoefficient = (double *)_aligned_realloc(_DragCoefficient, alloc_size * sizeof(double), 32);
					Nt_CollisionManager::cellStateAddressChanged = true;
				}
				//copy new values
				double *_xptr = _X + itemCount * 4;
				double *_vptr = _V + itemCount * 4;
				double *_fptr = _F + itemCount * 4;
				int *_gridptr = _GridIndex + itemCount * 4;
				for (int i=0; i<3; i++)
				{
					_xptr[i] = cell->SpatialState->X[i];
					_vptr[i] = cell->SpatialState->V[i];
					_fptr[i] = cell->SpatialState->F[i];
					_gridptr[i] = cell->GridIndex[i];
				}
				_gridptr[3] = cell->GridIndex[3]; //signal index changed.

				cell->SpatialState->X->NativePointer = _xptr;
				cell->SpatialState->V->NativePointer = _vptr;
				cell->SpatialState->F->NativePointer = _fptr;
				cell->GridIndex->NativePointer = _gridptr;
				cell->gridIndex = _gridptr;

				if (Nt_CollisionManager::cellStateAddressChanged == false)
				{
					//if memory is not reallocated, there is no cell pair containing this cell yet
					//so it is safe to set these pointers without the need to updae pair pointer dataq
					cell->nt_cell->X = _xptr;
					cell->nt_cell->F = _fptr;
					cell->nt_cell->gridIndex = _gridptr;
				}

				for (int i= itemCount *4; i < itemCount *4 + 4; i++)
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
				array_length = ComponentCells->Count * 4;
			}


			//here we should have a boundary id for the plasmamembrane.
			Cytosol->AddNtBoundary(0, cell->plasmaMembrane->InteriorId, cell->plasmaMembrane);

			for each (KeyValuePair<String^, Nt_Gene^>^ kvp in cell->Genes)
			{
				this->AddGene(kvp->Value);
			}
			//add molecular population before adding reactions.
			//otherwise the reaction won't have correct conc. pointer and length
			this->Cytosol->AddMolecularPopulation(cell->cytosol->NtPopulations);
			this->PlasmaMembrane->AddMolecularPopulation(cell->plasmaMembrane->NtPopulations);

			int boundary_id = cell->plasmaMembrane->InteriorId;
			this->Cytosol->AddCompartmentReactions(boundary_id, cell->cytosol);
			this->PlasmaMembrane->AddCompartmentReactions(-1, cell->plasmaMembrane);
			ntCellDictionary->Add(cell->Cell_id, cell);
		}
		

		//remove cell
		void RemoveCell(int cell_id)
		{
			if (ntCellDictionary->ContainsKey(cell_id) == false)
			{
				//AH - for debug
				if (deadCells->ContainsKey(cell_id) == true)
				{
					deadCells->Remove(cell_id);
#if defined (_DEBUG)
					fprintf(stdout, "Dead cell id=%d removed from system.\n", cell_id);
#endif
				}
				else 
				{
					throw gcnew Exception("Error RemoveCell: cell_id does not exist");
				}
				return;
			}
			
			Nt_Cell^ cell = ntCellDictionary[cell_id];
			//remove chemistry etc.
			int index = this->GetCellIndex(cell);

			//remove cell spaticalState etc.
			this->RemoveCellStates(cell);

			//remove cell chemistry
			this->Cytosol->RemoveMemberCompartmentMolpop(index);
			this->PlasmaMembrane->RemoveMemberCompartmentMolpop(index);
			//remove genes
			for (int i=0; i< genes->Count; i++)
			{
				genes[i]->RemoveGene(index);
			}

			this->Cytosol->RemoveMemberCompartmentReactions(index);
			this->PlasmaMembrane->RemoveMemberCompartmentReactions(index);

			//remove membrane boundary from cytosol and update boundaryId index in the cytosol collection
			Cytosol->RemoveNtBoundary(cell->plasmaMembrane->InteriorId);

			//AH - for debug
			if (cell->Alive == false)
			{
				deadCells->Add(cell->Cell_id, cell);

#if defined (_DEBUG)
				fprintf(stdout, "Cell id=%d is marked as dead\n", cell_id);
#endif
			}

			ntCellDictionary->Remove(cell_id);	
		}

		//remove cells spatialStates, sigma, transdcutionConsant and Dragcoefficient from array
		//note, the cell by itself still has its valid spatialstates etc., just not as part
		//of the cellpopulation.
		int RemoveCellStates(Nt_Cell ^c)
		{
			//debug
			double *xptr = c->SpatialState->X->NativePointer;
			int index = (int)(xptr - _X)/4;
			if ( (unsigned)index >= (unsigned)ComponentCells->Count || ComponentCells[index] != c)
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
				c->GridIndex->MemSwap(last_cell->GridIndex);
				last_cell->gridIndex = last_cell->GridIndex->NativePointer;
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
			c->GridIndex->detach();
			c->gridIndex = c->GridIndex->NativePointer;

			//cellIds->RemoveAt(last_index);
			ComponentCells->RemoveAt(last_index);
			array_length = ComponentCells->Count * 4;
			Nt_CollisionManager::cellStateAddressChanged = true;
			return index;
		}

		int GetCellIndex(Nt_Cell^ c)
		{
			double *xptr = c->SpatialState->X->NativePointer;
			int index = (int)(xptr - _X)/4;
			if (index < 0 || index >= ComponentCells->Count || ComponentCells[index] != c)
			{
				throw gcnew Exception("Error GetCellIndex - index out of range");
			}
			return index;
		}

		void resetForce()
		{
			if (_F != NULL)
			{
				memset(_F, 0, array_length*sizeof(double));
			}
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
		int *_GridIndex;

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
