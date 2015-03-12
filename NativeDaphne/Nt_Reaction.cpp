#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>

#include "Nt_Reaction.h"
#include "Utility.h"
#include "Nt_Utility.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{
	Nt_Reaction::Nt_Reaction(Nt_ReactionType type, int cell_id, double rate_const)
	{
		cellIdDictionary = gcnew Dictionary<int, bool>();
		cellIdDictionary->Add(cell_id, true);
		cellIds = gcnew List<int>();
		cellIds->Add(cell_id);
		reactionType = type;
		rateConstant = rate_const;
		initialized = false;
	}

	void Nt_Reaction::AddReaction(Nt_Reaction ^src_rxn)
	{
		throw gcnew NotImplementedException();
	}

	void Nt_Reaction::initialize()
	{}

	void Nt_Reaction::step(double dt)
	{}

	//****************************************
	//implementation of Nt_Transformation
	//****************************************
	Nt_Transformation::Nt_Transformation()
	{
		_reactant = NULL;
		_product = NULL;
	}

	Nt_Transformation::Nt_Transformation(int cell_id, double rate_const) : Nt_Reaction(Nt_ReactionType::Transformation, cell_id, rate_const)
	{
		_reactant = NULL;
		_product = NULL;
	}

	void Nt_Transformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}

	Nt_Reaction^ Nt_Transformation::CloneParent()
	{
		Nt_Transformation^ rxn = gcnew Nt_Transformation(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}
	
	//the data of _reactant and _project will be from the relavant
	//in compartment, consider how do we set that, here we don't allocate 
	//anything unless for some temparyvarailb.e.
	void Nt_Transformation::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transformation ^tmp = dynamic_cast<Nt_Transformation ^>(ComponentReactions[0]);
		array_length *= tmp->reactant->molpopConc->Length;

		_reactant = tmp->reactant->native_pointer();
		_product = tmp->product->native_pointer();
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
		initialized = true;
	}

	void Nt_Transformation::step(double dt)
	{
		if (initialized == false)initialize();
		//note: _reacant is already handled inside.
		NativeDaphneLibrary::Utility::NtDoubleDaxpy(array_length, rateConstant *dt, _reactant, _product);
	}

		
	//************************************
	//implemenation of Nt_Transcription
	//************************************
	Nt_Transcription::Nt_Transcription(int cell_id, double rate_const) : Nt_Reaction(Nt_ReactionType::Transcription, cell_id, rate_const)
	{
		_product = NULL;
	}

	void Nt_Transcription::AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}

	Nt_Reaction^ Nt_Transcription::CloneParent()
	{
		Nt_Transcription^ rxn = gcnew Nt_Transcription(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Transcription::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transcription ^tmp = dynamic_cast<Nt_Transcription ^>(ComponentReactions[0]);
		array_length *= tmp->product->molpopConc->Length;
		_product = tmp->product->native_pointer();
		_activation = tmp->gene->activation_pointer();
		CopyNumber = tmp->gene->CopyNumber;
		if (_product == NULL || _activation == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}

		initialized = true;
	}

	void Nt_Transcription::step(double dt)
	{
		if (initialized == false)initialize();
		//note: transcirption only addes to the first element, not to all 4 elements of _product
		double factor = rateConstant * CopyNumber * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(array_length/4, factor, _activation, 1, _product, 4);
	}

	//**************************************************
	//implementation of Nt_CatalyzedBoundaryActivation
	//**************************************************
	Nt_CatalyzedBoundaryActivation::Nt_CatalyzedBoundaryActivation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::CatalyzedBoundaryActivation, cell_id, rate_const)
	{
		initialized = false;
	}

	void Nt_CatalyzedBoundaryActivation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}


	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent()
	{
		Nt_CatalyzedBoundaryActivation^ rxn = gcnew Nt_CatalyzedBoundaryActivation(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//for debug only
		rxn->bulk = this->bulk;
		rxn->bulkActivated = this->bulkActivated;
		rxn->receptor = this->receptor;

		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedBoundaryActivation::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_CatalyzedBoundaryActivation ^tmp = dynamic_cast<Nt_CatalyzedBoundaryActivation ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;

		if (_intensity != NULL)
		{
			free(_intensity);
		}
		_intensity = (double *)malloc(array_length *sizeof(double));

		Nt_CytosolMolecularPopulation^ bulk_molpop1 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryFlux = bulk_molpop1->boundary_flux_pointer();

		_bulk_BoundaryConc = bulk_molpop1->boundary_conc_pointer(); //tmp->bulk->native_pointer(); //this only works for cytosol, not for ECS


		Nt_CytosolMolecularPopulation^ bulk_molpop2 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulkActivated);
		_bulkActivated_BoundaryFlux = bulk_molpop2->boundary_flux_pointer();

		_receptor = tmp->receptor->native_pointer();

		if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_bulkActivated_BoundaryFlux || !_receptor)
		{
			throw gcnew Exception("reaction component not initialized");
		}
		initialized = true;
	}

	void Nt_CatalyzedBoundaryActivation::step(double dt)
	{
		if (initialized == false)initialize();

		int n = array_length;
		NativeDaphneLibrary::Utility::NtMultiplyScalar(n, 4, _receptor, _bulk_BoundaryConc, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(n, rateConstant, _intensity, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(n, -rateConstant, _intensity, 1, _bulkActivated_BoundaryFlux, 1);
	}
		
	//*************************************************
	//		Nt_Annihilation
	//*************************************************
	Nt_Annihilation::Nt_Annihilation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::Annihilation, cell_id, rate_const)
	{
		initialized = false;
	}

	void Nt_Annihilation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}


	Nt_Reaction^ Nt_Annihilation::CloneParent()
	{
		Nt_Annihilation^ rxn = gcnew Nt_Annihilation(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Annihilation::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Annihilation ^tmp = dynamic_cast<Nt_Annihilation ^>(ComponentReactions[0]);
		array_length *= tmp->reactant->molpopConc->Length;

		_reactant = tmp->reactant->native_pointer();

		if (!_reactant)
		{
			throw gcnew Exception("reaction component not initialized");
		}
		initialized = true;
	}

	void Nt_Annihilation::step(double dt)
	{
		if (initialized == false)initialize();
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0 -rateConstant*dt), _reactant, 1);
	}


	//*************************************
	//    Nt_BoundaryTransportTo
	//*************************************
	Nt_BoundaryTransportTo::Nt_BoundaryTransportTo(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::BoundaryTransportTo, cell_id, rate_const)
	{
		initialized = false;
	}

	void Nt_BoundaryTransportTo:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}

	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent()
	{
		Nt_BoundaryTransportTo^ rxn = gcnew Nt_BoundaryTransportTo(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//this is for debugging, not needed for simulaiton
		rxn->bulk = this->bulk;
		rxn->membrane = this->membrane;

		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryTransportTo::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportTo ^tmp = dynamic_cast<Nt_BoundaryTransportTo ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;

		Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryConc = bulk_molpop->boundary_conc_pointer(); //native_pointer(); //this only works for cytosol, not for ECS
		_bulk_BoundaryFlux = bulk_molpop->boundary_flux_pointer();
		_membraneConc = tmp->membrane->native_pointer();

		if (!_bulk_BoundaryConc)
		{
			throw gcnew Exception("reaction component not initialized");
		}
		initialized = true;
	}

	void Nt_BoundaryTransportTo::step(double dt)
	{
		if (initialized == false)initialize();

		NativeDaphneLibrary::Utility::NtDaxpy(array_length, rateConstant, _bulk_BoundaryConc, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, rateConstant * dt, _bulk_BoundaryConc, 1, _membraneConc, 1);
	}


	//*************************************
	//		Nt_BoundaryTransportFrom
	//*************************************
	Nt_BoundaryTransportFrom::Nt_BoundaryTransportFrom(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::BoundaryTransportFrom, cell_id, rate_const)
	{
		_bulk_BoundaryFlux = NULL;
		_membraneConc = NULL;
		initialized = false;
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellIds[0]);
		initialized = false;
	}

	Nt_Reaction^ Nt_BoundaryTransportFrom::CloneParent()
	{
		Nt_BoundaryTransportFrom^ rxn = gcnew Nt_BoundaryTransportFrom(this->cellIds[0], this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryTransportFrom::initialize()
	{
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportFrom ^tmp = dynamic_cast<Nt_BoundaryTransportFrom ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;
		Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryFlux = bulk_molpop->boundary_flux_pointer();
		_membraneConc = tmp->membrane->native_pointer();

		if (!_bulk_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
		initialized = true;
	}

	void Nt_BoundaryTransportFrom::step(double dt)
	{
		if (initialized == false)initialize();
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -rateConstant, _membraneConc, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-rateConstant * dt), _membraneConc, 1);
	}
}