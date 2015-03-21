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
	
	public:

		/// <summary>
		/// create a new instance of Nt_CellManger
		/// </summary>
		/// <returns>none</returns>
		Nt_CellPopulation(void)
		{
			MotileCells = nullptr;
			Cytosol = gcnew Nt_Cytosol();
			PlasmaMembrane = gcnew Nt_PlasmaMembrane();
			genes = gcnew List<Nt_Gene ^>();

			initialized = false;
		}

		~Nt_CellPopulation(void){}

		/// <summary>
		/// Add an reaction to be handled by the Nt_CellManager instance.
		/// </summary
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

		void AddCell(Nt_Cell ^cell)
		{
			if (cell->isMotile)
			{
				if (MotileCells == nullptr)
				{
					MotileCells = cell->CloneParent();
				}
				else
				{
					MotileCells->AddCell(cell);
				}
			}
		}

		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>	
		void Clear()
		{
			initialized = false;
			//cell special states information
			MotileCells = nullptr;
			Cytosol = gcnew Nt_Cytosol();
			PlasmaMembrane = gcnew Nt_PlasmaMembrane();
		}

		void step(double dt)
		{
			if (!initialized)initialize();
			//check_value();

			Cytosol->step(dt);

			//check_value();

			PlasmaMembrane->step(dt);

			//check_value();

			if (MotileCells != nullptr)
			{
				MotileCells->step(dt);
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

		//all cells goes to the dictionary
		//only motile cess go to the collection???
		//consider how to handle exiting/non motile cells
		Nt_Cell^ MotileCells;

		Nt_Cytosol ^Cytosol;

		Nt_PlasmaMembrane ^PlasmaMembrane;

		List<Nt_Gene^> ^genes;

		//need this???
		Dictionary<int, Nt_Cell^> ^cellIdDictionary;

		bool initialized;
	};
}
