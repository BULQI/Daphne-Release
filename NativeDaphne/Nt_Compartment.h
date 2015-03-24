#pragma once

#include "Utility.h"
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
	public enum class Nt_ManifoldType {TinyBall, TinySphere, InterpolatedRectangularPrism};

	public ref class Nt_Compartment
    {
	private:
		bool initialized;
	public:

		List<Nt_MolecularPopulation ^> ^Populations;
        List<Nt_Reaction^> ^BulkReactions;
		List<Nt_Reaction^> ^BoundaryReactions;

		//?? this is not being used.
        Dictionary<int, Nt_Compartment ^> ^Boundaries;

        Nt_Compartment(Nt_ManifoldType _manifold_type)
        {
            //manifoldType = _manifold_type;
            Populations = gcnew List<Nt_MolecularPopulation^>();
            BulkReactions = gcnew List<Nt_Reaction^>();
            BoundaryReactions = gcnew List<Nt_Reaction^>();
            Boundaries = gcnew Dictionary<int, Nt_Compartment^>();
			initialized = false;
        }

        void AddMolecularPopulation(Nt_MolecularPopulation ^molpop)
        {
			for (int i= 0; i< Populations->Count; i++)
			{
				if (Populations[i]->molguid == molpop->molguid)
				{
					Populations[i]->AddMolecularPopulation(molpop);
					return;
				}
			}
			Populations->Add(molpop->CloneParent());
        }

        /// <summary>
        /// Carries out the dynamics in-place for its molecular populations over time interval dt.
        /// </summary>
        /// <param name="dt">The time interval.</param>
        void step(double dt)
        {
			if (!initialized)initialize();

            for (int i=0; i< BulkReactions->Count; i++)
			{
				BulkReactions[i]->step(dt);
			}

			for (int i=0; i< BoundaryReactions->Count; i++)
			{
				BoundaryReactions[i]->step(dt);
			}

            for (int i=0; i< Populations->Count; i++)
			{
				Populations[i]->step(dt);
			}
        }

		void AddReaction(Nt_Reaction ^rxn)
		{
			int index = rxn->reaction_index;
			List<Nt_Reaction^> ^reactions = rxn->isBulkReaction ? BulkReactions : BoundaryReactions;
			if (index >= reactions->Count)
			{
				reactions->Add(rxn->CloneParent());
			}
			else 
			{
				reactions[index]->AddReaction(rxn);
			}
		}

		//set up native data structure for molecular population and reactions.
		void initialize()
		{
			initialized = true;
		}
    };


	//the distinction here between Nt_Cytosol and Nt_Plasmamembrane does not seem to be necessary
	//consider to remove
	public ref class Nt_Cytosol : Nt_Compartment
	{
	public:
		Nt_Cytosol() : Nt_Compartment(Nt_ManifoldType::TinyBall)
		{
		}

		~Nt_Cytosol(){}

	};

	public ref class Nt_PlasmaMembrane : Nt_Compartment
	{
	public:
		Nt_PlasmaMembrane() : Nt_Compartment(Nt_ManifoldType::TinySphere)
		{}

		~Nt_PlasmaMembrane(){}

	};

	public ref class Nt_ReactionSet
	{
		List<Nt_Reaction^>^ ReactionList;

		Nt_ReactionSet()
		{
			ReactionList = gcnew List<Nt_Reaction^>();
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

		//<cell_population_id, List<interior->id, transformation>>
		Dictionary<int, Nt_Darray^>^ BoundaryTransforms;
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
			BoundaryTransforms = gcnew Dictionary<int, Nt_Darray^>();
			ir_prism = new NtInterpolatedRectangularPrism(NodesPerSide, StepSize, IsToroidal);
			initialized = false;
			Positions = NULL;
			BoundaryKeys = gcnew List<int>();
			boundaryReactions = gcnew Dictionary<int, Nt_ReactionSet^>();
		}

		~Nt_ECS(){}

		void AddMolecularPopulation(Nt_MolecularPopulation ^molpop)
        {
			Nt_ECSMolecularPopulation^ mp = dynamic_cast<Nt_ECSMolecularPopulation ^>(molpop);
			mp->ECS = this;
			Populations->Add(molpop);
        }

		//here key is membrane's interor id
		void AddBoundaryTransform(int key, Nt_Darray^ pos)
		{
			BoundaryTransforms->Add(key, pos);
			initialized = false;
		}

		void initialize()
		{
			BoundaryKeys->Clear();
			int items_count = BoundaryTransforms->Count;
			Positions = (double **)realloc(Positions, items_count * sizeof(double *));
			int n = 0;
			for each (KeyValuePair<int, Nt_Darray^>^ kvp in BoundaryTransforms)
			{
				BoundaryKeys->Add(kvp->Key);
				Positions[n++] = kvp->Value->NativePointer;
			}
			for (int i=0; i< Populations->Count; i++)
			{
				Nt_ECSMolecularPopulation^ pop = dynamic_cast<Nt_ECSMolecularPopulation^>(Populations[i]);
				pop->initialize();
			}

			initialized = true;
		}

		void step(double dt)
        {
			if (initialized == false)initialize();
			for (int i=0; i< Populations->Count; i++)
			{
				Populations[i]->step(dt);
			}
		}

	};

}