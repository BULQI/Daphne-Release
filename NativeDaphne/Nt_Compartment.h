#pragma once

#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Reaction.h"
#include "NtInterpolatedRectangularPrism.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	public enum class Nt_ManifoldType {TinyBall, TinySphere, InterpolatedRectangularPrism, TinyBallCollection, TinySphereCollection};

	public ref class Nt_Compartment
    {
	protected:
		bool initialized;

	internal:

		void AddBulkReaction(List<Nt_Reaction^>^ rxns)
		{
			//not initialized yet, initalize
			if (NtBulkReactions->Count == 0)
			{
				for (int i=0; i< rxns->Count; i++)
				{
					NtBulkReactions->Add(rxns[i]->CloneParent());
				}
			}
			else 
			{
				if (rxns->Count != NtBulkReactions->Count)
				{
					throw gcnew Exception("reaction count error");
				}
				for (int i=0; i< NtBulkReactions->Count; i++)
				{
					NtBulkReactions[i]->AddReaction(rxns[i]);
				}
			}
		}

		// gmk: terminology
		//with default populaiton id = 0, like cytosol and plasmambrane boundaries
		void AddBoundaryReaction(List<Nt_Reaction^>^ rxns)
		{
			for (int i=0; i< rxns->Count; i++)
			{
				Nt_Reaction^ rxn = rxns[i];
				if (rxn->IsCollection())
				{
					if (rxn->ComponentReactions->Count != 1)
					{
						//this is used add cell's boundary reaction
						//it should only contain one component
						throw gcnew Exception("reaction count error");
					}
					AddBoundaryReaction(0, rxn->ComponentReactions[0], i);
				}
				else
				{
					AddBoundaryReaction(0, rxns[i], i);
				}

			}
		}

		void AddMolecularPopulation(Nt_MolecularPopulation ^molpop)
        {
			for (int i= 0; i< NtPopulations->Count; i++)
			{
				if (NtPopulations[i]->MoleculeKey == molpop->MoleculeKey)
				{
					NtPopulations[i]->AddMolecularPopulation(molpop);
					//debug
					Nt_MolecularPopulation^ item = NtPopulations[i];
					double *conc = item->ConcPointer;
					double x = conc[0];

					Nt_MolecularPopulation^ child = item->ComponentPopulations[0];
					double *child_c = child->ConcPointer;
					double y = child_c[0];
					return;
				}
			}
			NtPopulations->Add(molpop->CloneParent(this));
        }

		void AddMolecularPopulation(List<Nt_MolecularPopulation^>^ molpop_list)
        {
			for (int i=0; i< molpop_list->Count; i++)
			{
				this->AddMolecularPopulation(molpop_list[i]);
			}
        }

		void AddCompartmentReactions(Nt_Compartment^ comp)
		{
			this->AddBulkReaction(comp->NtBulkReactions);
			if (comp->NtBoundaryReactions->Count > 0)
			{
				//default popid = 0
				this->AddBoundaryReaction(comp->NtBoundaryReactions[0]->ReactionList);
			}
		}


		//remove molpop and reactions for a cell with the given index.
		void RemoveMemberCompartmentMolpop(int index)
		{
			for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->RemoveMolecularPopulation(index);
			}
		}

		void RemoveMemberCompartmentReactions(int index)
		{
			//remove reactions
			for (int i=0; i< NtBulkReactions->Count; i++)
			{
				NtBulkReactions[i]->RemoveReaction(index);
			}

			if (NtBoundaryReactions->Count > 0)
			{
				if (this->manifoldType != Nt_ManifoldType::TinyBallCollection)
				{
					throw gcnew Exception("Error RemoveMemberCompartment: wrong compartment");
				}
				//the key to NtBoundaryReaction is cellpopulation_id, which is valid for ECS
				//but for cytosol collection, the boundary is membrane collection, there is
				//no concept of populationId, we thus asseumed 0 as the key
				//maybe we should move this into the Cytosol compartment
				///and make this a non-dictionary.
				Nt_ReactionSet^ rxn_set = NtBoundaryReactions[0];
				rxn_set->RemoveReactions(index);
			}
		}


		//void RemoveMemberCompartment(int index)
		//{
		//	//remove molpop and reactions for a cell with the given index.
		//	for (int i=0; i< NtPopulations->Count; i++)
		//	{
		//		NtPopulations[i]->RemoveMolecularPopulation(index);
		//	}

		//	//remove reactions
		//	for (int i=0; i< NtBulkReactions->Count; i++)
		//	{
		//		NtBulkReactions[i]->RemoveReaction(index);
		//	}

		//	if (NtBoundaryReactions->Count > 0)
		//	{
		//		if (this->manifoldType != Nt_ManifoldType::TinyBallCollection)
		//		{
		//			throw gcnew Exception("Error RemoveMemberCompartment: wrong compartment");
		//		}
		//		Nt_ReactionSet^ rxn_set = NtBoundaryReactions[0];
		//		rxn_set->RemoveReactions(index);
		//	}
		//}

		// gmk: Pulation, Put in NT_ECS with virtual and override?
		//given boundaryId, return cellpopulationId
		int GetCellPulationId(int boundary_id)
		{
			//debug 
			//if (BoundaryToCellpopMap != nullptr && boundary_id == 0)
			//{
			//	for each (KeyValuePair<int, int>^ item in BoundaryToCellpopMap)
			//	{
			//		int key = item->Key;
			//		int val = item->Value;
			//	}
			//}

			if (BoundaryToCellpopMap != nullptr && BoundaryToCellpopMap->ContainsKey(boundary_id) == true)
			{
				return BoundaryToCellpopMap[boundary_id];
			}
			return -1;
		}

		// gmk: Belongs elsewhere? Put in NT_ECS with virtual and override?
		//given boundaryId, return index of the given boundary in the given population
		int GetCellPopulationIndex(int boundary_id)
		{

			if (BoundaryIndexMap != nullptr && BoundaryIndexMap->ContainsKey(boundary_id) == true)
			{
				return BoundaryIndexMap[boundary_id];
			}
			return -1;
		}

	public:
		Nt_ManifoldType manifoldType;
		List<Nt_MolecularPopulation ^> ^NtPopulations;
        List<Nt_Reaction^> ^NtBulkReactions;

		// gmk: key=0 means plasma membrane/cytosol?
		//key - populaiton id; vlaue - boudnaryReactions
		Dictionary<int, Nt_ReactionSet^>^ NtBoundaryReactions;

		//one major difference between Nt_Compartment and upper level compartment
		//is that in Nt_Compartment, the compartment is orgnanized by cell populaiton
		//and within each cell pupulation, the "compartments" is ordered.
		//with insertion is easy, remove can be difficult, to help this
		//we create another dicitonary, which will "remember" the positon of the compartment
		//of the array. the next two dictionary helps to handle this
        Dictionary<int, List<Nt_Compartment^>^> ^NtBoundaries;

		//given boundaryId, return the cellpopulation map if applicable
		//if not in this dictionary, then assume 0
		Dictionary<int, int>^ BoundaryToCellpopMap;

		//map boundaryId to its array index in NtBoundaries.
		Dictionary<int, int>^ BoundaryIndexMap;

		//this is the id of the interior in the upper level.
		int InteriorId;

		Nt_Compartment()
        {
            NtPopulations = gcnew List<Nt_MolecularPopulation^>();
            NtBulkReactions = gcnew List<Nt_Reaction^>();
			NtBoundaryReactions = gcnew Dictionary<int, Nt_ReactionSet^>();
            NtBoundaries = gcnew Dictionary<int, List<Nt_Compartment^>^>();
			BoundaryToCellpopMap = gcnew Dictionary<int, int>();
			BoundaryIndexMap = gcnew Dictionary<int, int>();
			initialized = false;
			InteriorId = -1;

        }

		Nt_Compartment(Nt_ManifoldType _manifold_type)
        {
            manifoldType = _manifold_type;
            NtPopulations = gcnew List<Nt_MolecularPopulation^>();
            NtBulkReactions = gcnew List<Nt_Reaction^>();
			NtBoundaryReactions = gcnew Dictionary<int, Nt_ReactionSet^>();
            NtBoundaries = gcnew Dictionary<int, List<Nt_Compartment^>^>();
			BoundaryToCellpopMap = gcnew Dictionary<int, int>();
			BoundaryIndexMap = gcnew Dictionary<int, int>();
			initialized = false;
			InteriorId = -1;
        }

		~Nt_Compartment()
		{
			this->!Nt_Compartment();
		}

		!Nt_Compartment()
		{
		}

		void AddNtBoundary(int population_id, int boundary_id, Nt_Compartment^ c)
		{
			if (NtBoundaries->ContainsKey(population_id) == false)
			{
				List<Nt_Compartment^>^ comp_list = gcnew List<Nt_Compartment^>();
				comp_list->Add(c);
				BoundaryIndexMap->Add(boundary_id, 0);
				NtBoundaries->Add(population_id, comp_list);
			}
			else 
			{
				List<Nt_Compartment^>^ comp_list = NtBoundaries[population_id];
				BoundaryIndexMap->Add(boundary_id, comp_list->Count);
				comp_list->Add(c);
			}
			BoundaryToCellpopMap->Add(boundary_id, population_id);
		}

		void RemoveNtBoundary(int boundary_id)
		{
			if (BoundaryToCellpopMap->ContainsKey(boundary_id) == false)
			{
				throw gcnew Exception("RemoveNtBoudnary: id not found");
			}
			int population_id = BoundaryToCellpopMap[boundary_id];
			int boundary_index = BoundaryIndexMap[boundary_id];

			List<Nt_Compartment^>^ src = NtBoundaries[population_id];
			if (boundary_index != src->Count-1)//if not last one.
			{
				//swap with last
				Nt_Compartment^ last = src[src->Count -1];
				Nt_Compartment^ curr = src[boundary_index];
				BoundaryIndexMap[last->InteriorId] = boundary_index;
				BoundaryIndexMap[boundary_id] = src->Count-1;
				src[boundary_index] = last;
				src[src->Count-1] = curr;
			}
			src->RemoveAt(src->Count-1);
		}

		virtual void AddBoundaryTransform(int key, Transform^ pos)
		{
			//this should only be callsed for ECS
			throw gcnew Exception("NotImplementedException");
		}

		virtual void RemoveBoundaryTransform(int key)
		{
			//this should only be callsed for ECS
			throw gcnew Exception("NotImplementedException");
		}

        /// <summary>
        /// Carries out the dynamics in-place for its molecular populations over time interval dt.
        /// </summary>
        /// <param name="dt">The time interval.</param>
        virtual void step(double dt)
        {
			//throw gcnew Exception("NotImplementedException");

			if (!initialized)initialize();

            for (int i=0; i< NtBulkReactions->Count; i++)
			{
				NtBulkReactions[i]->Step(dt);
			}

			for each (KeyValuePair<int, Nt_ReactionSet^>^ kvp in NtBoundaryReactions)
			{
				 List<Nt_Reaction^>^ ReactionList = kvp->Value->ReactionList;
				 for (int i= 0; i< ReactionList->Count; i++)
				 {
					 ReactionList[i]->Step(dt);
				 }
			}

            for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->step(dt);
			}
        }

		//add boundary reaction, here key is the manifold id of the boundary.
		//for a given boundary, there is a list of reactions, here 
		//we neeed to form arrays for same reaction, and we are assuming
		//that the "same" reaction will have same index in the list of reactions
		//for that boundary. if not, we will need some kind of key
		//to identify the reaction.
		void AddBoundaryReaction(int key, Nt_Reaction^ rxn, int _index)
		{
			//note if the key is in the CellToCellPopMap, dictionary, then it is cells.
			int population_id = 0;
			if (this->BoundaryToCellpopMap->ContainsKey(key) == true)
			{
				population_id = this->BoundaryToCellpopMap[key];
			}

			if (NtBoundaryReactions->ContainsKey(population_id) == false)
			{
				Nt_ReactionSet^ dst_set = gcnew Nt_ReactionSet(population_id);
				dst_set->AddReaction(rxn, _index);
				NtBoundaryReactions->Add(population_id, dst_set);
			}
			else 
			{
				NtBoundaryReactions[population_id]->AddReaction(rxn, _index);
			}
		}

		//remove all boundary reaction with the given key, bundaryID;
		void RemoveBoundaryReaction(int key)
		{

			if (this->BoundaryToCellpopMap->ContainsKey(key) == false)
			{
				throw gcnew Exception("Error removing boundary reaction: key not found");
			}

			if (NtBoundaryReactions->Count == 0)
				return;

			int population_id = BoundaryToCellpopMap[key];
			int reaction_index = BoundaryIndexMap[key];

			Nt_ReactionSet^ rset = NtBoundaryReactions[population_id];
			rset->RemoveReactions(reaction_index);
		}

		virtual void AddBulkReaction(Nt_Reaction ^rxn)
		{
			NtBulkReactions->Add(rxn);
		}

		virtual void UpdateBoundary()
		{
			throw gcnew Exception("NotImplementedException");
		}

		//set up native data structure for molecular population and reactions.
		void initialize()
		{
			initialized = true;
		}
    };

	public ref class Nt_Cytosol : Nt_Compartment
	{
	public:
		double CellRadius;

		Transform^ BoundaryTransform;
		
		Nt_Cytosol(double r) : Nt_Compartment(Nt_ManifoldType::TinyBall)
		{
			CellRadius = r;
			BoundaryTransform = gcnew Transform(false);
		}

		Nt_Cytosol(double r, Nt_ManifoldType mtype) : Nt_Compartment(mtype)
		{
			CellRadius = r;
			BoundaryTransform = gcnew Transform(false);
		}

		~Nt_Cytosol(){}

		virtual void step(double dt) override
        {
			if (initialized == false)initialize();

			//debug 
			int componentCount = -1;

			for (int i=0; i< NtBulkReactions->Count; i++)
			{
				//debug
				//int com_count = NtBulkReactions[i]->ComponentCount();
				//if (componentCount == -1)
				//{
				//	componentCount = com_count;
				//}
				//else if (componentCount != com_count)
				//{
				//	throw gcnew Exception("Wrong number of reactions");
				//}

				NtBulkReactions[i]->Step(dt);
			}

			for each (KeyValuePair<int, Nt_ReactionSet^>^ kvp in NtBoundaryReactions)
			{
				 List<Nt_Reaction^>^ ReactionList = kvp->Value->ReactionList;
				 for (int i= 0; i< ReactionList->Count; i++)
				 {
					//debug
					//int com_count = ReactionList[i]->ComponentCount();
					//if (componentCount == -1)
					//{
					//	componentCount = com_count;
					//}
					//else if (componentCount != com_count)
					//{
					//	throw gcnew Exception("Wrong number of reactions");
					//}

					 ReactionList[i]->Step(dt);
				 }
			}

			//for now, this is doing update ecs/membrane boundary
			for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->step(this, dt);
			}
		}

	};


	//Nt_Cytosol and Nt_PlasmaMembrane is very similar
	public ref class Nt_PlasmaMembrane : Nt_Compartment
	{
	public:
		double CellRadius;

		Nt_PlasmaMembrane(double r) : Nt_Compartment(Nt_ManifoldType::TinySphere)
		{
			CellRadius = r;
		}

		Nt_PlasmaMembrane(double r, Nt_ManifoldType type) : Nt_Compartment(type)
		{
			CellRadius = r;
		}

		~Nt_PlasmaMembrane(){}


		virtual void step(double dt) override
        {
			if (initialized == false)initialize();

			for (int i=0; i< NtBulkReactions->Count; i++)
			{
				NtBulkReactions[i]->Step(dt);
			}

			for each (KeyValuePair<int, Nt_ReactionSet^>^ kvp in NtBoundaryReactions)
			{
				 List<Nt_Reaction^>^ ReactionList = kvp->Value->ReactionList;
				 for (int i= 0; i< ReactionList->Count; i++)
				 {
					 ReactionList[i]->Step(dt);
				 }
			}

			//for now, this is doing update ecs/membrane boundary
			//disabled for reactions only
			for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->step(this, dt);
			}
		}
	};



	public ref class Nt_ECS : Nt_Compartment
	{
	public:
		int* NodesPerSide;
		double StepSize;
		bool IsToroidal;
		NtInterpolatedRectangularPrism *ir_prism;
		bool initialized;

		//here boundary reactions need to be organized diffrently than in cytosol
		//here we need to explicitely organize them by cell population id.
		Dictionary<int, Nt_ReactionSet^>^ boundaryReactions;

		//interior->id, transformation
		Dictionary<int, Transform^>^ BoundaryTransforms;
		double **Positions;

		List<int>^ BoundaryKeys; //to keep sync with boundary in molpop

		Nt_ECS(array<int> ^extents, double step_size, bool toroidal) : Nt_Compartment(Nt_ManifoldType::InterpolatedRectangularPrism)
		{
			NodesPerSide = (int *)malloc(3 * sizeof(int));
			NodesPerSide[0] = extents[0];
			NodesPerSide[1] = extents[1];
			NodesPerSide[2] = extents[2];
			StepSize = step_size;
			IsToroidal = toroidal;
			BoundaryTransforms = gcnew Dictionary<int, Transform^>();
			ir_prism = new NtInterpolatedRectangularPrism(NodesPerSide, StepSize, IsToroidal);
			initialized = false;
			Positions = NULL;
			BoundaryKeys = gcnew List<int>();
			boundaryReactions = gcnew Dictionary<int, Nt_ReactionSet^>();
		}

		~Nt_ECS()
		{
			this->!Nt_ECS();
		}

		!Nt_ECS()
		{
			delete ir_prism;
		}

		//here key is membrane's interor id
		virtual void AddBoundaryTransform(int key, Transform^ t) override
		{
			BoundaryTransforms->Add(key, t);
			initialized = false;
		}

		virtual void RemoveBoundaryTransform(int key) override
		{
			BoundaryTransforms->Remove(key);
			initialized = false;
		}

		virtual void AddBulkReaction(Nt_Reaction ^rxn) override
		{
			NtBulkReactions->Add(rxn->CloneParent());
		}


		void initialize()
		{
			BoundaryKeys->Clear();
			int items_count = BoundaryTransforms->Count;
			Positions = (double **)realloc(Positions, items_count * sizeof(double *));
			int n = 0;
			for each (KeyValuePair<int, Transform^>^ kvp in BoundaryTransforms)
			{
				BoundaryKeys->Add(kvp->Key);
				Positions[n++] = kvp->Value->Translation->NativePointer;
			}
			for (int i=0; i< NtPopulations->Count; i++)
			{
				Nt_MolecularPopulation^ pop = NtPopulations[i];
				pop->initialize(this);
			}

			initialized = true;
		}


		//ecm step
		virtual void step(double dt) override
        {
			if (initialized == false)initialize();

			//debug
			int componentCount = -1;

			for (int i=0; i< NtBulkReactions->Count; i++)
			{

				//debug
				int com_count = NtBulkReactions[i]->ComponentCount();
				if (componentCount == -1)
				{
					componentCount = com_count;
				}
				else if (componentCount != com_count)
				{
					throw gcnew Exception("Wrong number of reactions");
				}

				NtBulkReactions[i]->Step(dt);
			}

			for each (KeyValuePair<int, Nt_ReactionSet^>^ kvp in NtBoundaryReactions)
			{
				 List<Nt_Reaction^>^ ReactionList = kvp->Value->ReactionList;

				 //debug
				 componentCount = -1;

				 for (int i= 0; i< ReactionList->Count; i++)
				 {

					//debug
					int com_count = ReactionList[i]->ComponentCount();
					if (componentCount == -1)
					{
						componentCount = com_count;
					}
					else if (componentCount != com_count)
					{
						throw gcnew Exception("Wrong number of reactions");
					}

					 ReactionList[i]->Step(dt);
				 }
			}

			//for now, this is doing update ecs/membrane boundary
			//this is disabled for handling reactions ONLY
			for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->step(this, dt);
			}
		}

		virtual void UpdateBoundary() override
		{
			for (int i=0; i< NtPopulations->Count; i++)
			{
				NtPopulations[i]->UpdateBoundary(this);
			}
		}
		
	};
	
}