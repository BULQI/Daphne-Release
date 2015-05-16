#pragma once


#include "NtUtility.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Gene.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Reaction
	{
	public:
		property double RateConstant;

		//indicate if the reaction should/should not perform step()
		property bool Steppable;

		virtual void Step(double dt);

	internal:
		
		Nt_Reaction(){}

		Nt_Reaction(double rate_const);

		List<Nt_Reaction ^> ^ComponentReactions;

		virtual void AddReaction(Nt_Reaction ^ rxn);

		virtual void RemoveReaction(int index);

		//clone this reaction and return reaction servers as placeholder
		virtual Nt_Reaction ^CloneParent()
		{
			Nt_Reaction^ rxn = gcnew Nt_Reaction(this->RateConstant);
			return rxn;
		}	

		virtual Nt_Reaction ^CloneParent(int population_id)
		{
			throw gcnew Exception("NotImplementedException");
		}

		bool IsCollection()
		{
			return ComponentReactions != nullptr;
		}

		int ComponentCount()
		{
			return ComponentReactions == nullptr ? -1 : ComponentReactions->Count;
		}
	};


	public ref class Nt_ReactionSet
	{
	public:
		int populationId;
		List<Nt_Reaction^>^ ReactionList;

		Nt_ReactionSet()
		{
			populationId = 0;
			ReactionList = gcnew List<Nt_Reaction^>();
		}

		Nt_ReactionSet(int pid)
		{
			populationId = pid;
			ReactionList = gcnew List<Nt_Reaction^>();
		}

		void AddReaction(Nt_Reaction ^rxn, int index)
		{
			if (index >= ReactionList->Count)
			{
				ReactionList->Add(rxn->CloneParent(populationId));
			}
			else 
			{
				ReactionList[index]->AddReaction(rxn);
			}
		}

		//remove all reactions in the set at index, which corresponds to a boundary
		void RemoveReactions(int index)
		{
			for (int i=0; i<ReactionList->Count; i++)
			{
				ReactionList[i]->RemoveReaction(index);
			}
		}

	};

	//Fundamental reactions

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Annihilation : public Nt_Reaction
	{
	internal:
		void varify_pointers(){}
	public:

		Nt_Annihilation(double rate_const);

		Nt_Annihilation(Nt_MolecularPopulation^ reactant, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;
		virtual void Step(double dt) override;

		Nt_MolecularPopulation^ Reactant;
	private:
		double *_reactant;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Association: public Nt_Reaction
	{

	public:
		Nt_Association(double rate_const);
		
		Nt_Association(Nt_MolecularPopulation^ _reactant1, Nt_MolecularPopulation^ _reactant2, Nt_MolecularPopulation^ _product, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant1;
		Nt_MolecularPopulation ^Reactant2;
		Nt_MolecularPopulation ^Product;

	private:
		double *_reactant1;
		double *_reactant2;
		double *_product;
		double *_intensity;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Dimerization: public Nt_Reaction
	{

	public:
		Nt_Dimerization(double _RateConst);

		Nt_Dimerization(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product;

	private:
		double *_reactant;
		double *_product;
		int array_length; //the "active" data length of the array
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_DimerDissociation: public Nt_Reaction
	{

	public:
		Nt_DimerDissociation(double _RateConst);

		Nt_DimerDissociation(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product, double _RateConst);


		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product;

	private:
		double *_reactant;
		double *_product;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Dissociation: public Nt_Reaction
	{

	public:
		Nt_Dissociation(double _RateConst);

		Nt_Dissociation(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product1, Nt_MolecularPopulation^ _product2, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product1;
		Nt_MolecularPopulation ^Product2;

	private:
		double *_reactant;
		double *_product1;
		double *_product2;
		int array_length;
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transformation: public Nt_Reaction
	{

	public:
		Nt_Transformation(double rate_const);

		Nt_Transformation(Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product;

	private:
		double *_reactant;
		double *_product;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedAnnihilation: public Nt_Reaction
	{

	public:
		Nt_CatalyzedAnnihilation(double _RateConst);

		Nt_CatalyzedAnnihilation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Catalyst;

	private:
		double *_reactant;
		double *_catalyst;
		double *_intensity;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedAssociation: public Nt_Reaction
	{

	public:
		Nt_CatalyzedAssociation(double RateConst);

		Nt_CatalyzedAssociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant1, Nt_MolecularPopulation^ reactant2, Nt_MolecularPopulation^ product, double RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Reactant1;
		Nt_MolecularPopulation ^Reactant2;
		Nt_MolecularPopulation ^Product;

	private:
		double *_catalyst;
		double *_reactant1;
		double *_reactant2;
		double *_product;
		double *_intensity;
		double *_intensity2;
		int array_length; //the "active" data length of the array
		
	};


	//to be implemented
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedCreation: public Nt_Reaction
	{

	public:

		Nt_CatalyzedCreation(double _RateConst);

		Nt_CatalyzedCreation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ _product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Product;

	private:
		double *_catalyst;
		double *_product;
		int array_length;
		
	};


	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedDimerization: public Nt_Reaction
	{

	public:
		Nt_CatalyzedDimerization(double _RateConst);

		Nt_CatalyzedDimerization(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Product;

	private:
		double *_reactant;
		double *_catalyst;
		double *_product;
		double *_intensity;
		int array_length; //the "active" data length of the array
		
	};


	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedDimerDissociation: public Nt_Reaction
	{

	public:
		Nt_CatalyzedDimerDissociation(double _RateConst);

		Nt_CatalyzedDimerDissociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product;

	private:
		double *_catalyst;
		double *_reactant;
		double *_product;
		double *_intensity;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedDissociation: public Nt_Reaction
	{

	public:
		Nt_CatalyzedDissociation(double _RateConst);

		Nt_CatalyzedDissociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product1, Nt_MolecularPopulation^ product2, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Reactant;		
		Nt_MolecularPopulation ^Product1;
		Nt_MolecularPopulation ^Product2;

	private:
		double *_catalyst;
		double *_reactant;
		double *_product1;
		double *_product2;
		double *_intensity;
		int array_length; //the "active" data length of the array		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedTransformation: public Nt_Reaction
	{

	public:
		Nt_CatalyzedTransformation(double _RateConst);

		Nt_CatalyzedTransformation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Catalyst;
		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Product;

	private:
		double *_catalyst;
		double *_reactant;
		double *_product;
		double *_intensity;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_AutocatalyticTransformation: public Nt_Reaction
	{

	public:
		Nt_AutocatalyticTransformation(double _RateConst);

		Nt_AutocatalyticTransformation(Nt_MolecularPopulation^ reactant1, Nt_MolecularPopulation^ reactant2, Nt_MolecularPopulation^ product, double _RateConst);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;


		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Reactant;
		Nt_MolecularPopulation ^Catalyst;

	private:
		double *_reactant;
		double *_catalyst;
		double *_intensity;
		int array_length; //the "active" data length of the array
		
	};


	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transcription: public Nt_Reaction
	{
	public:
		Nt_Transcription(double rate_const);

		Nt_Transcription(Nt_Gene^ gene, Nt_MolecularPopulation^ product, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void Step(double dt) override;

		Nt_Gene ^Gene;
		Nt_MolecularPopulation^ Product;
	private:
		double *_product;
		double *_activation;
		int array_length;
		int CopyNumber;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedBoundaryActivation : public Nt_Reaction
	{
	public:
		Nt_CatalyzedBoundaryActivation(double rate_const);

		Nt_CatalyzedBoundaryActivation(Nt_MolecularPopulation^ bulk, Nt_MolecularPopulation^ bulkActivated, Nt_MolecularPopulation^ receptor, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual Nt_Reaction^ CloneParent(int boundary_id) override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation^ Bulk;
		Nt_MolecularPopulation^ BulkActivated;
		Nt_MolecularPopulation^ Receptor;
		int boundaryId;

	private:
		double *_bulk_BoundaryConc;
		double *_bulk_BoundaryFlux;
		double *_bulkActivated_BoundaryFlux;
		double *_receptor;
		double *_intensity;
		int array_length;
	};


	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryTransportTo : public Nt_Reaction
	{
	public:
		Nt_BoundaryTransportTo(double rate_const);

		Nt_BoundaryTransportTo(Nt_MolecularPopulation^ bulk, Nt_MolecularPopulation^ membrane, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent() override;
		virtual Nt_Reaction^ CloneParent(int boundary_id) override;

		virtual void Step(double dt) override;
	
		Nt_MolecularPopulation^ Bulk;
		Nt_MolecularPopulation^ Membrane;
		int boundaryId;
	private:
		double *_bulk_BoundaryConc;
		double *_bulk_BoundaryFlux;
		double *_membraneConc;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryTransportFrom : public Nt_Reaction
	{
	public:

		Nt_BoundaryTransportFrom(double rate_const);

		Nt_BoundaryTransportFrom(Nt_MolecularPopulation^ membrane, Nt_MolecularPopulation^ bulk, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent(int boundary_id) override;
		virtual void Step(double dt) override;

		Nt_MolecularPopulation^ Bulk;
		Nt_MolecularPopulation^ Membrane;
		int boundaryId;
	private:
		double *_bulk_BoundaryFlux;
		double *_membraneConc;
		int array_length;

	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryAssociation : public Nt_Reaction
	{

	public:
		Nt_BoundaryAssociation(double rate_const);

		Nt_BoundaryAssociation(Nt_MolecularPopulation ^receptor, Nt_MolecularPopulation ^ligand, Nt_MolecularPopulation ^complex, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;


		virtual Nt_Reaction^ CloneParent() override
		{
			throw gcnew Exception("BoundaryAssociaiton.CloneParent needs populationId");
		}

		virtual Nt_Reaction^ CloneParent(int population_id) override;

		virtual void Step(double dt) override;

		//void initialize();

		Nt_MolecularPopulation ^Receptor;
		Nt_MolecularPopulation ^Ligand;
		Nt_MolecularPopulation ^Complex;
		int boundaryId;

	private:
		double *_ligand_BoundaryConc; //from bulk
		double *_ligand_BoundaryFlux; //from bulk

		double* _receptor;
		double* _complex;
		double *_intensity;
		int array_length; //the "active" data length of the array
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryDissociation : public Nt_Reaction
	{
	public:
		Nt_BoundaryDissociation(double rate_const);

		Nt_BoundaryDissociation(Nt_MolecularPopulation ^receptor, Nt_MolecularPopulation ^ligand, Nt_MolecularPopulation ^complex, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void RemoveReaction(int index) override;

		virtual Nt_Reaction^ CloneParent(int pop_id) override;

		virtual void Step(double dt) override;

		Nt_MolecularPopulation ^Receptor;
		Nt_MolecularPopulation ^Ligand;
		Nt_MolecularPopulation ^Complex;
		int boundaryId; //manifold->Id

	private:
		double *_ligand_BoundaryConc; //from bulk
		double *_ligand_BoundaryFlux; //from bulk

		double* _receptor;
		double* _complex;
		double *_intensity;
		int array_length;
	};

}
	
	