#pragma once

#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Cell.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellPopulation
	{

	private:
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
			masterCell = nullptr;
			Cytosol = gcnew Nt_Cytosol(Nt_Cell::defaultRadius, Nt_ManifoldType::TinyBallCollection);
			PlasmaMembrane = gcnew Nt_PlasmaMembrane(Nt_Cell::defaultRadius, Nt_ManifoldType::TinySphereCollection);
			genes = gcnew List<Nt_Gene ^>();
			initialized = false;
			deadCells = gcnew Dictionary<int, Nt_Cell^>();
			ntCellDictionary = gcnew Dictionary<int, Nt_Cell^>();
		}

		~Nt_CellPopulation(void){}
				
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
			if (ntCellDictionary->Count == 0)
			{
				Cytosol->CellRadius = cell->Radius;
				PlasmaMembrane->CellRadius = cell->Radius;
			}

			//here we should have a boundary id for the plasmamembrane.
			Cytosol->AddNtBoundary(0, cell->plasmaMembrane->InteriorId, cell->plasmaMembrane);
			if (masterCell == nullptr)
			{
				masterCell = cell->CloneParent();
			}
			else 
			{
				masterCell->AddCell(cell);
			}
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
			
			int index = masterCell->GetCellIndex(cell);

			//return the index of the cell in array
			masterCell->RemoveCell(cell);
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


		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <returns>void</returns>	
		void Clear()
		{
			initialized = false;
			//cell special states information
			//todo check disopose of these objects
			masterCell = nullptr;
			Cytosol = gcnew Nt_Cytosol(Nt_Cell::defaultRadius);
			PlasmaMembrane = gcnew Nt_PlasmaMembrane(Nt_Cell::defaultRadius);
			deadCells->Clear();
			ntCellDictionary->Clear();
		}

		void step(double dt)
		{
			if (!initialized)initialize();

			Cytosol->step(dt);

			PlasmaMembrane->step(dt);

			if (masterCell != nullptr && masterCell->IsMotile)
			{
				masterCell->step(dt);
			}
		}

		void initialize()
		{
			Cytosol->initialize();
			PlasmaMembrane->initialize();	
			initialized = true;
		}

	private:

		//this will handle the cells force etcs.
		Nt_Cell^ masterCell;

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
