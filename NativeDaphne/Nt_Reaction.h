#pragma once


#include "Utility.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_Gene.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	public enum class Nt_ReactionType {Annihilation, Association, AutocatalyticTransformation, BoundaryAssociation,
		BoundaryDissociation, BoundaryTransportFrom, BoundaryTransportTo, CatalyzedAnnihilation, CatalyzedAssociation,
		CatalyzedBoundaryActivation, CatalyzedCreation, CatalyzedDimerDissociation, CatalyzedDimerization,
		CatalyzedDissociation, CatalyzedTransformation, DimerDissociation, Dimerization, Dissociation, GeneralizedReaction,
		Transcription, Transformation};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Reaction
	{
	public:
	
		Nt_ReactionType reactionType;

		Nt_Reaction(){}

		Nt_Reaction(Nt_ReactionType type, int cell_id, double rate_const);

		Dictionary<int, bool> ^cellIdDictionary;

		int cellId;
		double rateConstant;

		//stores the list of component reactions.
		List<Nt_Reaction ^> ^ComponentReactions;
		List<int>^ cellIds;


		//since we don't have a key for reactions
		//this is the index of the reaction in List<Reaction>
		int reaction_index;
		bool isBulkReaction;


	    virtual void AddReaction(Nt_Reaction ^ rxn);

		virtual void step(double dt);

		//clone this reaction and return reaction servers as placeholder
		virtual Nt_Reaction ^CloneParent()
		{
			Nt_Reaction^ rxn = gcnew Nt_Reaction(this->reactionType, this->cellIds[0], this->rateConstant);
			return rxn;
		}
	protected:	
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transformation: public Nt_Reaction
	{

	public:
		Nt_Transformation();

		Nt_Transformation(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void step(double dt) override;

		Nt_MolecularPopulation ^reactant;
		Nt_MolecularPopulation ^product;

	private:
		double *_reactant;
		double *_product;
		int array_length; //the "active" data length of the array
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transcription: public Nt_Reaction
	{
	public:
		Nt_Transcription(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void step(double dt) override;

		Nt_Gene ^gene;
		Nt_MolecularPopulation^ product;
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
		Nt_CatalyzedBoundaryActivation(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual Nt_Reaction^ CloneParent() override;

		virtual void step(double dt) override;

		Nt_MolecularPopulation^ bulk;
		Nt_MolecularPopulation^ bulkActivated;
		Nt_MolecularPopulation^ receptor;

	private:
		double *_bulk_BoundaryConc;
		double *_bulk_BoundaryFlux;
		double *_bulkActivated_BoundaryFlux;
		double *_receptor;
		double *_intensity;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Annihilation : public Nt_Reaction
	{
	public:
		Nt_Annihilation(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual Nt_Reaction^ CloneParent() override;
		virtual void step(double dt) override;

		Nt_MolecularPopulation^ reactant;
	private:
		double *_reactant;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryTransportTo : public Nt_Reaction
	{
	public:
		Nt_BoundaryTransportTo(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual Nt_Reaction^ CloneParent() override;

		virtual void step(double dt) override;
	
		Nt_MolecularPopulation^ bulk;
		Nt_MolecularPopulation^ membrane;
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
		Nt_BoundaryTransportFrom(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual Nt_Reaction^ CloneParent() override;

		virtual void step(double dt) override;

		Nt_MolecularPopulation^ bulk;
		Nt_MolecularPopulation^ membrane;
	private:
		double *_bulk_BoundaryFlux;
		double *_membraneConc;
		int array_length;

	};
}
	
	