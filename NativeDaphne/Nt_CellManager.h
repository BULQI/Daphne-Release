#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_Cell.h"
#include "Nt_CellPopulation.h"
#include "Nt_ScalarField.h"
#include "DArray.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellManager
	{
	
	public:

		Nt_CellManager(void)
		{
			cellPopulations = gcnew Dictionary<int, Nt_CellPopulation^>();
			normalDist = gcnew Nt_NormalDistribution();
			EnvironmentExtent = gcnew array<double>(3);
		}

		~Nt_CellManager(void){}

		/// <summary>
		/// Add an reaction to be handled by the Nt_CellManager instance.
		/// </summary
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>
		void AddReaction(int cellpop_id, bool isCytosol, Nt_Reaction^ reaction)
		{

			cellPopulations[cellpop_id]->AddReaction(isCytosol, reaction);
		}

		void AddMolecularPopulation(int cellpop_id, bool isCytosol, Nt_MolecularPopulation^ molpop)
		{
			cellPopulations[cellpop_id]->AddMolecularPopulation(isCytosol, molpop);
		}

		void AddGene(int cellpop_id, Nt_Gene ^gene)
		{
			cellPopulations[cellpop_id]->AddGene(gene);
		}

		void AddCell(Nt_Cell ^cell)
		{
			int cellpop_id = cell->Population_id;
			if (cellPopulations->ContainsKey(cellpop_id) == false)
			{
				Nt_CellPopulation ^cellpop = gcnew Nt_CellPopulation();
				cellPopulations->Add(cellpop_id, cellpop);
			}
			cellPopulations[cellpop_id]->AddCell(cell);
		}

		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>	
		void Clear()
		{
			cellPopulations->Clear();
		}

		void step(double dt)
		{
			for each(KeyValuePair<int, Nt_CellPopulation^>^ item in cellPopulations)
			{
				item->Value->step(dt);
			}
			//for (int i=0; i< cellPopulations->Count; i++)
			//{
			//	cellPopulations[i]->step(dt);
			//}
		}

		void InitializeNormalDistributionSampler(double mean, double variance, int seed)
		{
			//there seems t be a bug in the native initializer...
			//if (IsDistributionSamplerInitialized == true)return;
			normalDist = gcnew Nt_NormalDistribution();
			normalDist->initialize(mean, variance, seed);
			IsDistributionSamplerInitialized = true;
		}

		void SetEnvironmentExtents(double extent0, double extent1, double extent2, bool do_boundary_force, double pair_phi1 )
		{
			EnvironmentExtent[0] = extent0;
			EnvironmentExtent[1] = extent1;
			EnvironmentExtent[2] = extent2;
			boundaryForceFlag = do_boundary_force;
			PairPhi1 = pair_phi1;
			IsEnvironmentInitialzed = true;
		}

		bool IsInitialized()
		{
			return IsEnvironmentInitialzed && IsDistributionSamplerInitialized;
		}


		static Nt_NormalDistribution ^normalDist;

		static array<double> ^EnvironmentExtent;

		//flag indicating if ECS and toroidal = false
		static bool boundaryForceFlag;

		//from Pair.Phil1 name of the parameter?
		static double PairPhi1;

	private:

		//need for quick access???
		//Dictionary<int, Nt_Cell^> ^cellIdDictionary;

		//population_id -> cell population
		Dictionary<int, Nt_CellPopulation^> ^cellPopulations;

		bool IsEnvironmentInitialzed;

		bool IsDistributionSamplerInitialized;
	};
}

