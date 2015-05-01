#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"
#include "Nt_MolecularPopulation.h"

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
			Cytosol = gcnew Nt_Compartment();
			PlasmaMembrane = gcnew Nt_Compartment();
			genes = gcnew List<Nt_Gene ^>();
			initialized = false;
			ntCellDictionory = gcnew Dictionary<int, Nt_Cell^>();
		}

		~Nt_CellPopulation(void){}
				
		/// <summary>
		/// Add an reaction to be handled by the Nt_CellManager instance.
		/// </summary>
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>
		void AddReaction(bool isCytosol,  Nt_Reaction^ reaction)
		{
			if (isCytosol)
			{
				Cytosol->AddReaction(reaction);
			}
			else 
			{
				PlasmaMembrane->AddReaction(reaction);
			}
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
			//??? here we should have a boundary id for the plasmamembrane.
			Cytosol->AddNtBoundary(0, cell->plasmaMembrane->InteriorId, cell->plasmaMembrane);
			if (masterCell == nullptr)
			{
				masterCell = cell->CloneParent();
			}
			else 
			{
				masterCell->AddCell(cell);
			}
			//things to add here...
			//1. add genes
			//2. add molecularPopulation (cytosol & membrane)
			//3. add reacitons both cytosol & mebrane
			for each (KeyValuePair<String^, Nt_Gene^>^ kvp in cell->Genes)
			{
				this->AddGene(kvp->Value);
			}
			this->Cytosol->AddMemberCompartment(cell->cytosol);
			this->PlasmaMembrane->AddMemberCompartment(cell->plasmaMembrane);
			ntCellDictionory->Add(cell->Cell_id, cell);
		}

		void RemoveCell(int cell_id)
		{
			Nt_Cell^ cell = ntCellDictionory[cell_id];
			if (cell == nullptr)
			{
				throw gcnew Exception("Error mid layer RemoveCell");
			}
			masterCell->RemoveCell(cell);
			//at this point, remove the cells data from molpop, reactions etc. etc.
			this->Cytosol->RemoveMemberCompartment(cell->cytosol);
			this->PlasmaMembrane->RemoveMemberCompartment(cell->plasmaMembrane);

			ntCellDictionory->Remove(cell_id);
			//handle other things here
			throw gcnew Exception("lots ot be done here");
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
			Cytosol = gcnew Nt_Compartment();
			PlasmaMembrane = gcnew Nt_Compartment();
		}

		void step(double dt)
		{
			if (!initialized)initialize();
			//check_value();

			Cytosol->step(dt);

			//check_value();

			PlasmaMembrane->step(dt);

			//check_value();

			if (masterCell != nullptr)
			{
				masterCell->step(dt);
			}

			//check_value();
		}

		void initialize()
		{

			//we need to intialize both PlasmaMembrane and Cytosol
			//before going to step, because a boundary reaction
			//may depend on molpops in both compartments.
			Cytosol->initialize();
			//check_value();
			PlasmaMembrane->initialize();	
			//check_value();
			initialized = true;
		}

		//void check_value()
		//{
		//	Nt_MolecularPopulation ^x = this->Cytosol->Populations[2];
		//	double tmp = x->_molpopConc[4];
		//	Console::WriteLine("xxx vlaue = {0}", tmp);
		//}
	private:

		//this will handle the cells force etcs.
		Nt_Cell^ masterCell;

		Nt_Compartment ^Cytosol;

		Nt_Compartment ^PlasmaMembrane;

		List<Nt_Gene^> ^genes;

		//we are not sure if we need this, and how this is going to work.
		Dictionary<int, Nt_Cell^>^ ntCellDictionory;

		bool initialized;
	};
}
