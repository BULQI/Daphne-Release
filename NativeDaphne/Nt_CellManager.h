#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_Cell.h"
#include "Nt_CellPopulation.h"
#include "Nt_ScalarField.h"
#include "Nt_DArray.h"

#include "Nt_CollisionManager.h"



using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellManager
	{
	
	public:
		//this is used to sync order of boundary reactions in ECS
		List<int>^ MembraneIdList;

		Nt_CellManager(void)
		{
			cellPopulations = gcnew Dictionary<int, Nt_CellPopulation^>();

			normalDist = gcnew Nt_NormalDistribution();

			EnvironmentExtent = gcnew array<double>(3);

			int numthreads = NativeDaphneLibrary::Utility::acml_getnumthreads();

			NativeDaphneLibrary::Utility::acml_setnumthreads(8);

			CellMebraneMolPopDictionary = gcnew Dictionary<Tuple<int, String^>^, Nt_MolecularPopulation^>();

			MembraneIdList = gcnew List<int>();

			BoundIdToCellPopIdDictionary = gcnew Dictionary<int, int>();



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

		void AddMolecularPopulation(int cellpop_id, int membrane_id, bool isCytosol, Nt_MolecularPopulation^ molpop)
		{
			cellPopulations[cellpop_id]->AddMolecularPopulation(isCytosol, molpop);
			if (isCytosol == false)
			{
				Tuple<int, String^> ^t = gcnew Tuple<int, String^>(membrane_id, molpop->molguid);
				CellMebraneMolPopDictionary->Add(t, molpop);
			}

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
			MembraneIdList->Add(cell->Membrane_id);
			BoundIdToCellPopIdDictionary->Add(cell->Membrane_id, cell->Population_id);
			cellDictionary->Add(cell->Cell_id, cell);
		}

		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>	
		void Clear()
		{
			cellPopulations->Clear();
			cellDictionary->Clear();
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

		//utility method
		//give membrane.interior.Id and molquid, find the Nt_molpop.
		Nt_MolecularPopulation^ findMembraneMolecularPopulation(int membraneId, String^ molguid)
		{
			Tuple<int, String^>^ key = gcnew Tuple<int, String^>(membraneId, molguid);
			if (CellMebraneMolPopDictionary->ContainsKey(key) == true)
			{
				return CellMebraneMolPopDictionary[key];
			}
			throw gcnew Exception("Nt_MolecularPopulation not found for membrane");
		}

		int findCellPopulationId(int bound_id)
		{
			if (BoundIdToCellPopIdDictionary->ContainsKey(bound_id) == true)
			{
				return BoundIdToCellPopIdDictionary[bound_id];
			}
			return -1;
		}

		static Nt_NormalDistribution ^normalDist;

		static array<double> ^EnvironmentExtent;

		//flag indicating if ECS and toroidal = false
		static bool boundaryForceFlag;

		//from Pair.Phil1 name of the parameter?
		static double PairPhi1;

		//contains all current cells, <cell_id, Nt_Cell^>
		static Dictionary<int, Nt_Cell^> ^cellDictionary = gcnew Dictionary<int, Nt_Cell^>();

	private:

		//population_id -> cell population
		Dictionary<int, Nt_CellPopulation^> ^cellPopulations;

		//used in ecs methods
		Dictionary<int, int>^ BoundIdToCellPopIdDictionary;

		//tuple<membrane.interior.Id, molguid> - used to get nt_molpop in ECS
		Dictionary<Tuple<int, String^>^, Nt_MolecularPopulation^>^ CellMebraneMolPopDictionary;

		bool IsEnvironmentInitialzed;

		bool IsDistributionSamplerInitialized;
	};
}

