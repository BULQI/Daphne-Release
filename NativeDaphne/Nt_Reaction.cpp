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
using namespace NativeDaphneLibrary;

namespace NativeDaphne
{
	Nt_Reaction::Nt_Reaction(Nt_ReactionType type, int cell_id, double rate_const)
	{
		/*cellIdDictionary = gcnew Dictionary<int, bool>();
		cellIdDictionary->Add(cell_id, true);
		cellIds = gcnew List<int>();
		cellIds->Add(cell_id);*/
		cellId = cell_id;
		reactionType = type;
		rateConstant = rate_const;
	}

	void Nt_Reaction::AddReaction(Nt_Reaction ^src_rxn)
	{
		throw gcnew NotImplementedException();
	}

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
		cellIds->Add(src_rxn->cellId);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transformation ^tmp = dynamic_cast<Nt_Transformation ^>(ComponentReactions[0]);
		array_length *= tmp->reactant->molpopConc->Length;

		_reactant = tmp->reactant->NativePointer;
		_product = tmp->product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Transformation::CloneParent()
	{
		Nt_Transformation^ rxn = gcnew Nt_Transformation(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		rxn->AddReaction(this);
		return rxn;
	}
	
	//the data of _reactant and _project will be from the relavant
	//in compartment, consider how do we set that, here we don't allocate 
	//anything unless for some temparyvarailb.e.
	/*void Nt_Transformation::initialize()
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
	}*/

	void Nt_Transformation::step(double dt)
	{
		
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
		cellIds->Add(src_rxn->cellId);
		
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transcription ^tmp = dynamic_cast<Nt_Transcription ^>(ComponentReactions[0]);
		array_length *= tmp->product->molpopConc->Length;
		_product = tmp->product->NativePointer;
		_activation = tmp->gene->activation_pointer();
		CopyNumber = tmp->gene->CopyNumber;
		if (_product == NULL || _activation == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Transcription::CloneParent()
	{
		Nt_Transcription^ rxn = gcnew Nt_Transcription(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		rxn->AddReaction(this);
		return rxn;
	}

	/*void Nt_Transcription::initialize()
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
	}*/

	void Nt_Transcription::step(double dt)
	{
		//note: transcirption only addes to the first element, not to all 4 elements of _product
		double factor = rateConstant * CopyNumber * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(array_length/4, factor, _activation, 1, _product, 4);
	}

	//**************************************************
	//implementation of Nt_CatalyzedBoundaryActivation
	//**************************************************
	Nt_CatalyzedBoundaryActivation::Nt_CatalyzedBoundaryActivation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::CatalyzedBoundaryActivation, cell_id, rate_const)
	{

	}

	void Nt_CatalyzedBoundaryActivation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);

		array_length = ComponentReactions->Count;

		Nt_CatalyzedBoundaryActivation ^tmp = dynamic_cast<Nt_CatalyzedBoundaryActivation ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		Nt_CytosolMolecularPopulation^ bulk_molpop1 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryFlux = bulk_molpop1->BoundaryFluxPointer;
		_bulk_BoundaryConc = bulk_molpop1->BoundaryConcPointer; //tmp->bulk->native_pointer(); //this only works for cytosol, not for ECS


		Nt_CytosolMolecularPopulation^ bulk_molpop2 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulkActivated);
		_bulkActivated_BoundaryFlux = bulk_molpop2->BoundaryFluxPointer;

		_receptor = tmp->receptor->NativePointer;

		if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_bulkActivated_BoundaryFlux || !_receptor)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent()
	{
		Nt_CatalyzedBoundaryActivation^ rxn = gcnew Nt_CatalyzedBoundaryActivation(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		//for debug only
		rxn->bulk = this->bulk;
		rxn->bulkActivated = this->bulkActivated;
		rxn->receptor = this->receptor;

		rxn->AddReaction(this);
		return rxn;
	}

	//void Nt_CatalyzedBoundaryActivation::initialize()
	//{
	//	array_length = ComponentReactions->Count;
	//	if (array_length == 0)return;

	//	Nt_CatalyzedBoundaryActivation ^tmp = dynamic_cast<Nt_CatalyzedBoundaryActivation ^>(ComponentReactions[0]);
	//	array_length *= tmp->bulk->molpopConc->Length;

	//	if (_intensity != NULL)
	//	{
	//		free(_intensity);
	//	}
	//	_intensity = (double *)malloc(array_length *sizeof(double));

	//	Nt_CytosolMolecularPopulation^ bulk_molpop1 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
	//	_bulk_BoundaryFlux = bulk_molpop1->boundary_flux_pointer();

	//	_bulk_BoundaryConc = bulk_molpop1->boundary_conc_pointer(); //tmp->bulk->native_pointer(); //this only works for cytosol, not for ECS


	//	Nt_CytosolMolecularPopulation^ bulk_molpop2 = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulkActivated);
	//	_bulkActivated_BoundaryFlux = bulk_molpop2->boundary_flux_pointer();

	//	_receptor = tmp->receptor->native_pointer();

	//	if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_bulkActivated_BoundaryFlux || !_receptor)
	//	{
	//		throw gcnew Exception("reaction component not initialized");
	//	}
	//	initialized = true;
	//}

	void Nt_CatalyzedBoundaryActivation::step(double dt)
	{
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
	}

	void Nt_Annihilation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Annihilation ^tmp = dynamic_cast<Nt_Annihilation ^>(ComponentReactions[0]);
		array_length *= tmp->reactant->molpopConc->Length;

		_reactant = tmp->reactant->NativePointer;

		if (!_reactant)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_Annihilation::CloneParent()
	{
		Nt_Annihilation^ rxn = gcnew Nt_Annihilation(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		rxn->AddReaction(this);
		return rxn;
	}

	/*void Nt_Annihilation::initialize()
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
	}*/

	void Nt_Annihilation::step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0 -rateConstant*dt), _reactant, 1);
	}


	//*************************************
	//    Nt_BoundaryTransportTo
	//*************************************
	Nt_BoundaryTransportTo::Nt_BoundaryTransportTo(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::BoundaryTransportTo, cell_id, rate_const)
	{
	}

	void Nt_BoundaryTransportTo:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportTo ^tmp = dynamic_cast<Nt_BoundaryTransportTo ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;

		Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryConc = bulk_molpop->BoundaryConcPointer; //native_pointer(); //this only works for cytosol, not for ECS
		_bulk_BoundaryFlux = bulk_molpop->BoundaryFluxPointer;
		_membraneConc = tmp->membrane->NativePointer;

		if (!_bulk_BoundaryConc)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent()
	{
		Nt_BoundaryTransportTo^ rxn = gcnew Nt_BoundaryTransportTo(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		//this is for debugging, not needed for simulaiton
		rxn->bulk = this->bulk;
		rxn->membrane = this->membrane;

		rxn->AddReaction(this);
		return rxn;
	}

	//void Nt_BoundaryTransportTo::initialize()
	//{
	//	array_length = ComponentReactions->Count;
	//	if (array_length == 0)return;

	//	Nt_BoundaryTransportTo ^tmp = dynamic_cast<Nt_BoundaryTransportTo ^>(ComponentReactions[0]);
	//	array_length *= tmp->bulk->molpopConc->Length;

	//	Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
	//	_bulk_BoundaryConc = bulk_molpop->boundary_conc_pointer(); //native_pointer(); //this only works for cytosol, not for ECS
	//	_bulk_BoundaryFlux = bulk_molpop->boundary_flux_pointer();
	//	_membraneConc = tmp->membrane->native_pointer();

	//	if (!_bulk_BoundaryConc)
	//	{
	//		throw gcnew Exception("reaction component not initialized");
	//	}
	//	initialized = true;
	//}

	void Nt_BoundaryTransportTo::step(double dt)
	{
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
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);

		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportFrom ^tmp = dynamic_cast<Nt_BoundaryTransportFrom ^>(ComponentReactions[0]);
		array_length *= tmp->bulk->molpopConc->Length;
		Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
		_bulk_BoundaryFlux = bulk_molpop->BoundaryFluxPointer;
		_membraneConc = tmp->membrane->NativePointer;

		if (!_bulk_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}

	}

	Nt_Reaction^ Nt_BoundaryTransportFrom::CloneParent()
	{
		Nt_BoundaryTransportFrom^ rxn = gcnew Nt_BoundaryTransportFrom(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		rxn->AddReaction(this);
		return rxn;
	}

	//void Nt_BoundaryTransportFrom::initialize()
	//{
	//	array_length = ComponentReactions->Count;
	//	if (array_length == 0)return;

	//	Nt_BoundaryTransportFrom ^tmp = dynamic_cast<Nt_BoundaryTransportFrom ^>(ComponentReactions[0]);
	//	array_length *= tmp->bulk->molpopConc->Length;
	//	Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
	//	_bulk_BoundaryFlux = bulk_molpop->boundary_flux_pointer();
	//	_membraneConc = tmp->membrane->native_pointer();

	//	if (!_bulk_BoundaryFlux)
	//	{
	//		throw gcnew Exception("reaction component not initialized");
	//	}
	//	initialized = true;
	//}

	void Nt_BoundaryTransportFrom::step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -rateConstant, _membraneConc, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-rateConstant * dt), _membraneConc, 1);
	}

	//*************************************
	// BoundarAssociation
	//*************************************
	Nt_BoundaryAssociation::Nt_BoundaryAssociation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::CatalyzedBoundaryActivation, cell_id, rate_const)
	{
		_intensity = NULL;
	}

	void Nt_BoundaryAssociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);

		//do initialize
		array_length = ComponentReactions->Count;
		Nt_BoundaryAssociation ^tmp = dynamic_cast<Nt_BoundaryAssociation ^>(ComponentReactions[0]);
		array_length *= tmp->receptor->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = tmp->receptor->molpopConc->NativePointer;	//membrane
		_complex = tmp->complex->NativePointer;					//membrane

		//set boundary info from ecs.
		Nt_ECSMolecularPopulation^ ligand_molpop = dynamic_cast<Nt_ECSMolecularPopulation^>(tmp->ligand);
		if (ligand_molpop != nullptr)
		{	
			_ligand_BoundaryConc = ligand_molpop->cellBoundaries[boundaryId]->ConcPointer;
			_ligand_BoundaryFlux = ligand_molpop->cellBoundaries[boundaryId]->FluxPointer;
		}

		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	//void Nt_BoundaryAssociation :: initialize()
	//{
	//	array_length = ComponentReactions->Count;
	//	Nt_BoundaryAssociation ^tmp = dynamic_cast<Nt_BoundaryAssociation ^>(ComponentReactions[0]);
	//	array_length *= tmp->receptor->molpopConc->Length;

	//	_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
	//	_receptor = tmp->receptor->molpopConc->NativePointer;	//membrane
	//	_complex = tmp->complex->NativePointer;					//membrane

	//	//set boundary info from ecs.
	//	Nt_ECSMolecularPopulation^ ligand_molpop = dynamic_cast<Nt_ECSMolecularPopulation^>(tmp->ligand);
	//	if (ligand_molpop != nullptr)
	//	{	
	//		_ligand_BoundaryConc = ligand_molpop->cellBoundaries[boundaryId]->ConcPointer;
	//		_ligand_BoundaryFlux = ligand_molpop->cellBoundaries[boundaryId]->FluxPointer;
	//	}

	//	if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
	//	{
	//		throw gcnew Exception("reaction component not initialized");
	//	}
	//}

	Nt_Reaction^ Nt_BoundaryAssociation::CloneParent()
	{
		Nt_BoundaryAssociation^ rxn = gcnew Nt_BoundaryAssociation(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		//for debug only
		rxn->complex = this->complex;
		rxn->ligand = this->ligand;
		rxn->receptor = this->receptor;
		rxn->boundaryId = this->boundaryId;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryAssociation::step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _receptor, _ligand_BoundaryConc, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, rateConstant, _intensity, 1, _ligand_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*rateConstant, _intensity, 1, _receptor, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*rateConstant, _intensity, 1, _complex, 1);
	}

	//*************************************
	// BoundarDissociation
	//*************************************
	Nt_BoundaryDissociation::Nt_BoundaryDissociation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::CatalyzedBoundaryActivation, cell_id, rate_const)
	{
		_ligand_BoundaryConc = NULL;  //from bulk
		_ligand_BoundaryFlux = NULL; //from bulk
		_receptor = NULL;
		_complex = NULL;
		_intensity = NULL;
	}

	void Nt_BoundaryDissociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		cellIds->Add(src_rxn->cellId);

		//do initialize
		array_length = ComponentReactions->Count;
		Nt_BoundaryDissociation ^tmp = dynamic_cast<Nt_BoundaryDissociation ^>(ComponentReactions[0]);
		array_length *= tmp->receptor->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = tmp->receptor->molpopConc->NativePointer;	//membrane
		_complex = tmp->complex->NativePointer;					//membrane

		//set boundary info from ecs.
		Nt_ECSMolecularPopulation^ ligand_molpop = dynamic_cast<Nt_ECSMolecularPopulation^>(tmp->ligand);
		if (ligand_molpop != nullptr)
		{	
			_ligand_BoundaryConc = ligand_molpop->cellBoundaries[boundaryId]->ConcPointer;
			_ligand_BoundaryFlux = ligand_molpop->cellBoundaries[boundaryId]->FluxPointer;
		}

		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_BoundaryDissociation::CloneParent()
	{
		Nt_BoundaryDissociation^ rxn = gcnew Nt_BoundaryDissociation(this->cellId, this->rateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->cellIds = gcnew List<int>();
		//for debug only
		rxn->complex = this->complex;
		rxn->ligand = this->ligand;
		rxn->receptor = this->receptor;
		rxn->boundaryId = this->boundaryId;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryDissociation::step(double dt)
	{
		Utility::NtDaxpy(array_length, -rateConstant, _complex, 1, _ligand_BoundaryFlux, 1); //ligand.BoundaryFluxes -=  rateConsant * Complex
		Utility::NtDaxpy(array_length, rateConstant * dt, _complex, 1, _receptor, 1);		//receptor += rateConsant * Complex * dt
		Utility::NtDaxpy(array_length, -rateConstant * dt, _complex, 1, _complex, 1);
	}
}