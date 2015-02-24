
#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>

#include "Nt_CellManager.h"
#include "Utility.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{

	//implementation of Nt_Reaction class
	Nt_Reaction::Nt_Reaction(){}

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

	//implementation of Nt_Transformation
	Nt_Transformation::Nt_Transformation()
	{
		//create the collections
		reactant = gcnew List<array<double>^>();
		product = gcnew List<array<double>^>();
	}

	Nt_Transformation::Nt_Transformation(int cell_id, double rate_const) : Nt_Reaction(Nt_ReactionType::Transformation, cell_id, rate_const)
	{
		//create the collections
		reactant = gcnew List<array<double>^>();
		product = gcnew List<array<double>^>();
		_reactant = NULL;
		_product = NULL;
	}

	void Nt_Transformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_Transformation ^rxn = dynamic_cast<Nt_Transformation ^>(src_rxn);
		reactant->Add(rxn->reactant[0]);
		product->Add(rxn->product[0]);
		initialized = false;
	}
	
	void Nt_Transformation::initialize()
	{
		if (_reactant != NULL)
		{
			free(_reactant);
			free(_product);
		}
		int numItems = reactant->Count;
		_reactant = (double *)malloc(numItems * 4 * sizeof(double));
		_product = (double *)malloc(numItems * 4 * sizeof(double));
		initialized = true;
	}

	void Nt_Transformation::step(double dt)
	{
		if (initialized == false)initialize();

		int n = 0;
		for (int i=0; i< reactant->Count; i++)
		{
			array<double> ^item_r = reactant[i];
			array<double> ^item_p = product[i];
			for (int j = 0; j< item_r->Length; j++, n++)
			{
				_reactant[n] = item_r[j];
				_product[n] = item_p[j];
			}
		}

		//intensity = reactant * rateConstnat *dt
		//reactant -= intensity
		//project  += intensity.
		NativeDaphneLibrary::Utility::NtDoubleDaxpy(n, rateConstant *dt, _reactant, _product);
		n = 0;
		for (int i=0; i< reactant->Count; i++)
		{
			array<double> ^item_r = reactant[i];
			array<double> ^item_p = product[i];
			for (int j = 0; j< item_r->Length; j++, n++)
			{
				item_r[j] = _reactant[n];
				item_p[j] = _product[n];
			}
		}
		
		//testing duplicate
		for (int i=1; i< reactant->Count; i++)
		{
			if (reactant[i] == reactant[0] || product[i] == product[0])
			{
				int same = 1;
			}
		}		

	}


		
	//************************************
	//implemenation of Nt_Transcription
	//************************************
	Nt_Transcription::Nt_Transcription(int cell_id, double rate_const) : Nt_Reaction(Nt_ReactionType::Transcription, cell_id, rate_const)
	{
		CopyNumber = gcnew List<int>();
		ActivationLevel = gcnew List<array<double>^>();
		product = gcnew List<array<double>^>();
		activationLevelSave = gcnew List<double>();
	}

	void Nt_Transcription::AddReaction(Nt_Reaction ^src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_Transcription ^rxn = dynamic_cast<Nt_Transcription ^>(src_rxn);
		CopyNumber->Add(rxn->CopyNumber[0]);
		ActivationLevel->Add(rxn->ActivationLevel[0]);
		product->Add(rxn->product[0]);
		initialized = false;
	}


	void Nt_Transcription::initialize()
	{
		if (initialized==true)
		{
			free(_product);
		}
		int numItems = product->Count * 4;
		_product = (double *)malloc(numItems * sizeof(double));
		for (int i=0; i< ActivationLevel->Count; i++)
		{
			activationLevelSave->Add(ActivationLevel[i][0]);
		}

		initialized = true;
	}

	void Nt_Transcription::step(double dt)
	{
		if (initialized == false)initialize();

		//we don't know how/if this  helps with native code yet.
		for (int i=0; i< product->Count; i++)
		{
			array<double> ^item_p = product[i];
			double inc = rateConstant * CopyNumber[i] * ActivationLevel[i][0] * dt;
			item_p[0] += inc;
		}
	}

	//**************************************************
	//implementation of Nt_CatalyzedBoundaryActivation
	//**************************************************
	Nt_CatalyzedBoundaryActivation::Nt_CatalyzedBoundaryActivation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::CatalyzedBoundaryActivation, cell_id, rate_const)
	{
		receptor = gcnew List<array<double>^>();
		bulkBoundaryConc = gcnew List<array<double>^>();
		bulkBoundaryFluxes = gcnew List<array<double>^>();
		bulkActivatedBoundaryFluxes = gcnew List<array<double>^>();
	}

	void Nt_CatalyzedBoundaryActivation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_CatalyzedBoundaryActivation ^rxn = dynamic_cast<Nt_CatalyzedBoundaryActivation ^>(src_rxn);
		receptor->Add(rxn->receptor[0]);
		bulkBoundaryConc->Add(rxn->bulkBoundaryConc[0]);
		bulkBoundaryFluxes->Add(rxn->bulkBoundaryFluxes[0]);
		bulkActivatedBoundaryFluxes->Add(rxn->bulkActivatedBoundaryFluxes[0]);
		initialized = false;
	}

	void Nt_CatalyzedBoundaryActivation::initialize()
	{
		if (initialized == true)
		{
			free(_receptor);
			free(_bulkBoundaryConc);
			free(_bulkBoundaryFluxes);
			free(_bulkActivatedBoundaryFluxes);
		}
		int numItems = receptor->Count * 4;
		_receptor = (double *)malloc(numItems * sizeof(double));
		_bulkBoundaryConc = (double *)malloc(numItems * sizeof(double));
		_bulkBoundaryFluxes = (double *)malloc(numItems * sizeof(double));
		_bulkActivatedBoundaryFluxes = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_CatalyzedBoundaryActivation::step(double dt)
	{
		if (initialized == false)initialize();

		int n = 0;
		for (int i=0; i< bulkBoundaryFluxes->Count; i++)
		{
			array<double> ^item_b = bulkBoundaryFluxes[i];
			array<double> ^item_a = bulkActivatedBoundaryFluxes[i];
			array<double> ^item_r = receptor[i];
			array<double> ^item_c = bulkBoundaryConc[i];
			for (int j = 0; j< item_r->Length; j++, n++)
			{
				_bulkBoundaryFluxes[n] = item_b[j];
				_bulkActivatedBoundaryFluxes[n] = item_a[j];
				_receptor[n] = item_r[j];
				_bulkBoundaryConc[n] = item_c[j];
			}
		}

		//scalara multiply - result is in _receptor 
		//NtDscal(n, 1.0 -rateConstant*dt, _reactant, 1);

		NativeDaphneLibrary::Utility::NtMultiplyScalar(n, 4, _receptor, _bulkBoundaryConc);
		//NativeDaphneLibrary::Utility::NtDscal(n, rateConstant, _receptor, 1);

		NativeDaphneLibrary::Utility::NtDaxpy(n, rateConstant, _receptor, 1, _bulkBoundaryFluxes, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(n, -rateConstant, _receptor, 1, _bulkActivatedBoundaryFluxes, 1);
		
		n = 0;
		for (int i=0; i< bulkBoundaryFluxes->Count; i++)
		{
			array<double> ^item_b = bulkBoundaryFluxes[i];
			array<double> ^item_a = bulkActivatedBoundaryFluxes[i];
			for (int j = 0; j< item_b->Length; j++, n++)
			{
				item_b[j] = _bulkBoundaryFluxes[n];
				item_a[j] = _bulkActivatedBoundaryFluxes[n];
				//item_b[j] += _receptor[n];
				//item_a[j] -= _receptor[n];

			}
		}

		//testing identical items....
		//for (int i=1; i< bulkBoundaryFluxes->Count; i++)
		//{
		//	if (bulkBoundaryFluxes[i] == bulkBoundaryFluxes[0] ||
		//		bulkActivatedBoundaryFluxes[i] == bulkActivatedBoundaryFluxes[0] ||
		//		receptor[i] == receptor[0] || 
		//		bulkBoundaryConc[i] == bulkBoundaryConc[0]
		//		)
		//	{
		//		int same = 1;
		//	}
		//}

	}


	//void Nt_CatalyzedBoundaryActivation::step_work(double dt)
	//{
	//	if (initialized == false)initialize();

	//	int n = 0;
	//	for (int i=0; i< bulkBoundaryFluxes->Count; i++)
	//	{
	//		//array<double> ^item_b = bulkBoundaryFluxes[i];
	//		//array<double> ^item_a = bulkActivatedBoundaryFluxes[i];
	//		array<double> ^item_r = receptor[i];
	//		array<double> ^item_c = bulkBoundaryConc[i];
	//		for (int j = 0; j< item_r->Length; j++, n++)
	//		{
	//			//_bulkBoundaryFluxes[n] = item_b[j];
	//			//_bulkActivatedBoundaryFluxes[n] = item_a[j];
	//			_receptor[n] = item_r[j];
	//			_bulkBoundaryConc[n] = item_c[j];
	//		}
	//	}

	//	//scalara multiply - result is in _receptor 
	//	//NtDscal(n, 1.0 -rateConstant*dt, _reactant, 1);

	//	NativeDaphneLibrary::Utility::NtMultiplyScalar(n, 4, _receptor, _bulkBoundaryConc);
	//	NativeDaphneLibrary::Utility::NtDscal(n, rateConstant, _receptor, 1);

	//	//NativeDaphneLibrary::Utility::NtDaxpy(n, rateConstant, _receptor, 1, _bulkBoundaryFluxes, 1);
	//	//NativeDaphneLibrary::Utility::NtDaxpy(n, -rateConstant, _receptor, 1, _bulkActivatedBoundaryFluxes, 1);
	//	
	//	n = 0;
	//	for (int i=0; i< bulkBoundaryFluxes->Count; i++)
	//	{
	//		array<double> ^item_b = bulkBoundaryFluxes[i];
	//		array<double> ^item_a = bulkActivatedBoundaryFluxes[i];
	//		for (int j = 0; j< item_b->Length; j++, n++)
	//		{
	//			//item_b[j] = _bulkBoundaryFluxes[n];
	//			//item_a[j] = _bulkActivatedBoundaryFluxes[n];
	//			item_b[j] += _receptor[n];
	//			item_a[j] -= _receptor[n];

	//		}
	//	}

	//	//testing identical items....
	//	for (int i=1; i< bulkBoundaryFluxes->Count; i++)
	//	{
	//		if (bulkBoundaryFluxes[i] == bulkBoundaryFluxes[0] ||
	//			bulkActivatedBoundaryFluxes[i] == bulkActivatedBoundaryFluxes[0])
	//		{
	//			int same = 1;
	//		}
	//	}

	//}




	//*************************************************
	//		Nt_Annihilation
	//*************************************************
	Nt_Annihilation::Nt_Annihilation(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::Annihilation, cell_id, rate_const)
	{
		reactant = gcnew List<array<double>^>();
	}

	void Nt_Annihilation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_Annihilation ^rxn = dynamic_cast<Nt_Annihilation ^>(src_rxn);
		reactant->Add(rxn->reactant[0]);
		initialized = false;
	}

	void Nt_Annihilation::initialize()
	{
		if (initialized == true)
		{
			free(_reactant);
		}
		int numItems = reactant->Count * 4;
		_reactant = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_Annihilation::step(double dt)
	{
		if (initialized == false)initialize();

		int n = 0;
		for (int i=0; i< reactant->Count; i++)
		{
			array<double> ^item_r = reactant[i];
			for (int j = 0; j< item_r->Length; j++, n++)
			{
				_reactant[n] = item_r[j];
			}
		}
		NativeDaphneLibrary::Utility::NtDscal(n, (1.0 -rateConstant*dt), _reactant, 1);
		n = 0;
		for (int i=0; i< reactant->Count; i++)
		{
			array<double> ^item_r = reactant[i];
			for (int j = 0; j< item_r->Length; j++, n++)
			{
				item_r[j] = _reactant[n];
			}
		}
	}


	//*************************************
	//    Nt_BoundaryTransportTo
	//*************************************
	Nt_BoundaryTransportTo::Nt_BoundaryTransportTo(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::BoundaryTransportTo, cell_id, rate_const)
	{
		BulkBoundaryConc = gcnew List<array<double>^>();
		BulkBoundaryFluxes = gcnew List<array<double>^>();
		MembraneConc = gcnew List<array<double>^>();
	}

	void Nt_BoundaryTransportTo:: AddReaction(Nt_Reaction ^src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_BoundaryTransportTo ^rxn = dynamic_cast<Nt_BoundaryTransportTo ^>(src_rxn);

		BulkBoundaryConc->Add(rxn->BulkBoundaryConc[0]);
		BulkBoundaryFluxes->Add(rxn->BulkBoundaryFluxes[0]);
		MembraneConc->Add(rxn->MembraneConc[0]);
		initialized = false;
	}

	void Nt_BoundaryTransportTo::initialize()
	{
		if (initialized == true)
		{
			free(_bulkBoundaryConc);
			free(_bulkBoundaryFluxes);
			free(_membraneConc);
		}
		int numItems = BulkBoundaryConc->Count * 4;
		_bulkBoundaryConc = (double *)malloc(numItems * sizeof(double));
		_bulkBoundaryFluxes = (double *)malloc(numItems * sizeof(double));
		_membraneConc = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_BoundaryTransportTo::step(double dt)
	{
		if (initialized == false)initialize();

		int n = 0;
		for (int i=0; i< BulkBoundaryConc->Count; i++)
		{
			array<double> ^item_b = BulkBoundaryConc[i];
			array<double> ^item_f = BulkBoundaryFluxes[i];
			array<double> ^item_m = MembraneConc[i];
			for (int j = 0; j< item_b->Length; j++, n++)
			{
				_bulkBoundaryConc[n] = item_b[j];
				_bulkBoundaryFluxes[n] = item_f[j];
				_membraneConc[n] = item_m[j];
			}
		}
		NativeDaphneLibrary::Utility::NtDaxpy(n, rateConstant, _bulkBoundaryConc, 1, _bulkBoundaryFluxes, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(n, rateConstant * dt, _bulkBoundaryConc, 1, _membraneConc, 1);
		
		n = 0;
		for (int i=0; i< BulkBoundaryConc->Count; i++)
		{
			array<double> ^item_f = BulkBoundaryFluxes[i];
			array<double> ^item_m = MembraneConc[i];
			for (int j = 0; j< item_f->Length; j++, n++)
			{
				item_f[j] = _bulkBoundaryFluxes[n];
				item_m[j] = _membraneConc[n];
			}
		}
	}


	//*************************************
	//		Nt_BoundaryTransportFrom
	//*************************************
	Nt_BoundaryTransportFrom::Nt_BoundaryTransportFrom(int cell_id, double rate_const) :Nt_Reaction(Nt_ReactionType::BoundaryTransportFrom, cell_id, rate_const)
	{
		BulkBoundaryFluxes = gcnew List<array<double>^>();
		MembraneConc = gcnew List<array<double>^>();
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		cellIds->Add(src_rxn->cellIds[0]);
		Nt_BoundaryTransportFrom ^rxn = dynamic_cast<Nt_BoundaryTransportFrom ^>(src_rxn);

		BulkBoundaryFluxes->Add(rxn->BulkBoundaryFluxes[0]);
		MembraneConc->Add(rxn->MembraneConc[0]);
		initialized = false;
	}

	void Nt_BoundaryTransportFrom::initialize()
	{
		if (initialized == true)
		{
			free(_bulkBoundaryFluxes);
			free(_membraneConc);
		}
		int numItems = BulkBoundaryFluxes->Count * 4;
		_bulkBoundaryFluxes = (double *)malloc(numItems * sizeof(double));
		_membraneConc = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_BoundaryTransportFrom::step(double dt)
	{
		if (initialized == false)initialize();

		int n = 0;
		for (int i=0; i< BulkBoundaryFluxes->Count; i++)
		{
			array<double> ^item_f = BulkBoundaryFluxes[i];
			array<double> ^item_m = MembraneConc[i];
			for (int j = 0; j< item_f->Length; j++, n++)
			{
				_bulkBoundaryFluxes[n] = item_f[j];
				_membraneConc[n] = item_m[j];
			}
		}
		NativeDaphneLibrary::Utility::NtDaxpy(n, -rateConstant, _membraneConc, 1, _bulkBoundaryFluxes, 1);
		NativeDaphneLibrary::Utility::NtDscal(n, (1.0-rateConstant * dt), _membraneConc, 1);
		
		n = 0;
		for (int i=0; i< BulkBoundaryFluxes->Count; i++)
		{
			array<double> ^item_f = BulkBoundaryFluxes[i];
			array<double> ^item_m = MembraneConc[i];
			for (int j = 0; j< item_f->Length; j++, n++)
			{
				item_f[j] = _bulkBoundaryFluxes[n];
				item_m[j] = _membraneConc[n];
			}
		}
	}


	///////////////////////////////////
	//Nt_MolecularPopulation
	///////////////////////////////////
	Nt_MolecularPopulation::Nt_MolecularPopulation(double _diffusionCoefficient, array<double> ^conc)
	{
		molpopConc = gcnew List<array<double>^>();
		molpopConc->Add(conc);
		DiffusionCoefficient = _diffusionCoefficient;
	}

	void Nt_MolecularPopulation::step(double dt)
	{}

	void Nt_MolecularPopulation::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{}


	///////////////////////////////////
	//Nt_CytosolMolecularPopulation
	///////////////////////////////////
	Nt_CytosolMolecularPopulation::Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, double _diffusionCoefficient, 
		array<double> ^conc, array<double> ^bflux, array<double> ^bconc) : Nt_MolecularPopulation(_diffusionCoefficient, conc)
	{
		boundaryFluxes = gcnew List<array<double>^>();
		boundaryConc = gcnew List<array<double>^>();
		cellIds = gcnew List<int>();

		boundaryFluxes->Add(bflux);
		boundaryConc->Add(bconc);
		cellIds->Add(_cellId);

		CellRadius = _cellRadius;
		initialized = false;
	}

	void Nt_CytosolMolecularPopulation::initialize()
	{
		if (initialized == true)
		{
			free(_laplacian);
			free(_molpopConc);
			free(_boundaryFluxes);
		}
		int numItems = molpopConc->Count * 4;
		_molpopConc = (double *)malloc(numItems * sizeof(double));
		_laplacian = (double *)malloc(numItems * sizeof(double));
		_boundaryFluxes = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_CytosolMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{
		Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(src);
		cellIds->Add(molpop->cellIds[0]);
		molpopConc->Add(molpop->molpopConc[0]);
		boundaryFluxes->Add(molpop->boundaryFluxes[0]);
		boundaryConc->Add(molpop->boundaryConc[0]);
		initialized = false;
	}

	void Nt_CytosolMolecularPopulation::step(double dt)
	{

		if (initialized == false)initialize();
		int n = 0;
		for (int i=0; i< molpopConc->Count; i++)
		{
			array<double> ^item_c = molpopConc[i];
			array<double> ^item_f = boundaryFluxes[i];
			_laplacian[n] = 0;
			_molpopConc[n] = item_c[0]; 
			_boundaryFluxes[n] = item_f[0] * 3/5; /*so that we can multple all array item by 5 */
			n++;
			for (int j = 1; j< item_c->Length; j++, n++)
			{
				_laplacian[n] = item_c[j];
				_molpopConc[n] = item_c[j];
				_boundaryFluxes[n] = item_f[j];
			}
		}

		//handle diffusion...
		double factor1 = (-5.0 /(CellRadius *CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//handle diffusion flux
		double factor2 = -5 * dt /CellRadius;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor2, _boundaryFluxes, 1, _molpopConc, 1);

		//set value back and update boundary
		n = 0;
		for (int i=0; i< molpopConc->Count; i++)
		{
			array<double> ^item_c = molpopConc[i];
			array<double> ^item_f = boundaryFluxes[i];
			array<double> ^item_b = boundaryConc[i];
			for (int j = 0; j< item_c->Length; j++, n++)
			{
				item_c[j] = _molpopConc[n]; /* copy concentration back */
				item_b[j] = _molpopConc[n]; /* update boundary */
				item_f[j] = 0;				/* reset flux */
			}
		}
	}

	//////////////////////////////////////////
	// Nt_MembraneMolecularPopulation
	//////////////////////////////////////////
	//Nt_MembraneMolecularPopulation
	Nt_MembraneMolecularPopulation::Nt_MembraneMolecularPopulation(int _cellId, double _cellRadius, double _diffusionCoefficient, 
		array<double> ^conc) : Nt_MolecularPopulation(_diffusionCoefficient, conc)
	{
		cellIds = gcnew List<int>();
		cellIds->Add(_cellId);

		CellRadius = _cellRadius;
		initialized = false;
	}

	void Nt_MembraneMolecularPopulation::initialize()
	{
		if (initialized == true)
		{
			free(_laplacian);
			free(_molpopConc);
		}
		int numItems = molpopConc->Count * 4;
		_molpopConc = (double *)malloc(numItems * sizeof(double));
		_laplacian = (double *)malloc(numItems * sizeof(double));
		initialized = true;
	}

	void Nt_MembraneMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{

		Nt_MembraneMolecularPopulation ^molpop = dynamic_cast<Nt_MembraneMolecularPopulation^>(src);
		cellIds->Add(molpop->cellIds[0]);
		molpopConc->Add(molpop->molpopConc[0]);
		initialized = false;
	}

	void Nt_MembraneMolecularPopulation::step(double dt)
	{

		if (initialized == false)initialize();
		int n = 0;
		for (int i=0; i< molpopConc->Count; i++)
		{
			array<double> ^item_c = molpopConc[i];
			_laplacian[n] = 0;
			_molpopConc[n] = item_c[0]; 
			n++;
			for (int j = 1; j< item_c->Length; j++, n++)
			{
				_laplacian[n] = item_c[j];
				_molpopConc[n] = item_c[j];
			}
		}

		//handle diffusion...
		double factor1 = (-2.0 /(CellRadius *CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//set value back and update boundary
		n = 0;
		for (int i=0; i< molpopConc->Count; i++)
		{
			array<double> ^item_c = molpopConc[i];
			for (int j = 0; j< item_c->Length; j++, n++)
			{
				item_c[j] = _molpopConc[n]; /* copy concentration back */
			}
		}
	}

	void CellStateCollection::step(double dt)
		{
			if (Initialized == false)
			{
				initialize();
			}
			//handle boudnaryFource first --- since most cells won't have it.
			if (Nt_CellManager::boundaryForceFlag == true)
			{
				for (int i=0; i< XList->Count; i++)				
				{
					double radius = RadiusList[i];
					array<double> ^X = XList[i];
					double dist = 0;
					double x1 = Nt_CellManager::PairPhi1;
					array<double> ^x2 = Nt_CellManager::EnvironmentExtent;
					for (int j = 0; j<3; j++)
					{
						if (X[j] < radius && X[j] != 0)
						{
							FList[i][j] += Nt_CellManager::PairPhi1 *(1.0/X[j] - 1.0/radius);
						}
						else if ( (dist = Nt_CellManager::EnvironmentExtent[j] - X[j]) < radius && dist != 0)
						{
							FList[i][j] -= Nt_CellManager::PairPhi1 *(1.0/dist - 1.0/radius);
						}
					}
				}
			}

			//load state data
			int n = 0;
			for (int i=0; i<FList->Count; i++)
			{
				array<double> ^item_x = XList[i];
				array<double> ^item_v = VList[i];
				array<double> ^item_f = FList[i];
				for (int j = 0; j< item_f->Length; j++, n++)
				{
					_X[n] = item_x[j];
					_V[n] = item_v[j];
					_F[n] = item_f[j];
				}
			}

			//handle chemotaxis
			n = 0;
			bool force_current = false;
			if (IsTransductionConstIdentical == true)
			{
				force_current = true;
				for (int i=0; i < cellIds->Count; i++)
				{
					array<double> ^item_d = DriverConcList[i];
					for (int j=0; j< item_d->Length-1; j++, n++)
					{
						_driver_gradient[n] = item_d[j+1];
					}
				}
				double transductionConstant = TransductionConstList[0];
				NativeDaphneLibrary::Utility::NtDaxpy(n, transductionConstant, _driver_gradient, 1, _F, 1);
			}
			else 
			{
				for (int i=0; i < cellIds->Count; i++)
				{
					array<double> ^item_d = DriverConcList[i];
					for (int j=0; j< item_d->Length-1; j++, n++)
					{
						_driver_gradient[n] = item_d[j+1] * TransductionConstList[i];
					}
				}
				NativeDaphneLibrary::Utility::NtDaxpy(n, 1.0, _driver_gradient, 1, _F, 1);
			}

			//handle Stochastic -- this well create the given number of samples at once...
			/*n = FList->Count * 3;
			for (int i=0; i< n; i++)_samples[i] = 0.0;*/
		    Nt_CellManager::normalDist->Sample(n, _samples);
			if (IsSigmaIdentical == true)
			{
				double factor = SigmaList[0] /Math::Sqrt(dt);
				NativeDaphneLibrary::Utility::NtDaxpy(n, factor, _samples, 1, _F, 1);
			}
			else 
			{
				n = 0;
				double sqrt_dt = Math::Sqrt(dt);
				for (int i=0; i< FList->Count; i++)
				{
					double factor = SigmaList[i] /sqrt_dt;
					double Sigma = SigmaList[i];
					for (int j=0; j< 3; j++, n++)
					{
						_samples[n] *= factor;
					}
				}
				NativeDaphneLibrary::Utility::NtDaxpy(n, 1.0, _samples, 1, _F, 1);
			}

			for (int i=0; i<n; i++)
			{
				if (_X[i] + _V[i] * dt > 260.001)
				{
					int overflow = 1;
				}
			}
			//handle movement
			NativeDaphneLibrary::Utility::NtDaxpy(n, dt, _V, 1, _X, 1);
			
			if (IsDragCoefficientIdentical)
			{
				double DragCoefficient = DragCoefficientList[0];
				NativeDaphneLibrary::Utility::NtDscal(n, 1.0 - dt*DragCoefficient, _V, 1);
			}
			else 
			{
				throw gcnew Exception("not implemented");
			}
			NativeDaphneLibrary::Utility::NtDaxpy(n, dt, _F, 1, _V, 1);

			//copy value back;
			n = 0;
			for (int i=0; i< FList->Count; i++)
			{
				array<double> ^item_x = XList[i];
				array<double> ^item_v = VList[i];
				array<double> ^item_f = FList[i];
				for (int j = 0; j< item_x->Length; j++, n++)
				{
					item_x[j] = _X[n];
					item_v[j] = _V[n];
					item_f[j] = _F[n];
				}
			}
		}

	//////////////////////////////////////////////////////////////
	//implementatation of Nt_CellManager
	//////////////////////////////////////////////////////////////
 
	Nt_CellManager::Nt_CellManager()
	{
		reactionList = gcnew List<Nt_Reaction^>();
		molpopList = gcnew List<Nt_MolecularPopulation^>();
		cellStates = gcnew CellStateCollection();

		normalDist = gcnew Nt_NormalDistribution();
		EnvironmentExtent = gcnew array<double>(3);
	}

	void Nt_CellManager::AddReaction(Nt_Reaction^ rxn)
	{
		for (int i=0; i< reactionList->Count; i++)
		{
			Nt_Reaction^ src = reactionList[i];
			if (src->reactionType != rxn->reactionType || src->rateConstant != rxn->rateConstant)continue;
			int cid = rxn->cellIds[0];
			if (src->cellIdDictionary->ContainsKey(cid))continue;
			src->cellIdDictionary->Add(cid, true);
			src->AddReaction(rxn);
			return;
		}
		reactionList->Add(rxn);
	}

	void Nt_CellManager::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{
		for (int i=0; i< molpopList->Count; i++)
		{
			Nt_MolecularPopulation^ src = molpopList[i];
			if (src->GetType() != molpop->GetType() || src->DiffusionCoefficient != molpop->DiffusionCoefficient)continue; 
			if (dynamic_cast<Nt_CytosolMolecularPopulation ^>(molpop) != nullptr)
			{
				Nt_CytosolMolecularPopulation ^a = dynamic_cast<Nt_CytosolMolecularPopulation^>(molpop);
				Nt_CytosolMolecularPopulation ^b = dynamic_cast<Nt_CytosolMolecularPopulation^>(src);
				if ( a->CellRadius != b->CellRadius)continue;
			}
			else if (dynamic_cast<Nt_MembraneMolecularPopulation^>(molpop) != nullptr)
			{
				Nt_MembraneMolecularPopulation ^a = dynamic_cast<Nt_MembraneMolecularPopulation^>(molpop);
				Nt_MembraneMolecularPopulation ^b = dynamic_cast<Nt_MembraneMolecularPopulation^>(src);
				if ( a->CellRadius != b->CellRadius)continue;
			}
			src->AddMolecularPopulation(molpop);
			return;
		}
		molpopList->Add(molpop);
	}


	Nt_CellManager::~Nt_CellManager()
	{
	}

	void Nt_CellManager::step(double dt)
	{
		for (int i=0; i< reactionList->Count; i++)
		{
			reactionList[i]->step(dt);
		}
		for (int i=0; i< molpopList->Count; i++)
		{
			molpopList[i]->step(dt);
		}

		cellStates->step(dt);
	}

	void Nt_CellManager::Clear()
	{
		reactionList->Clear();
		molpopList->Clear();
		cellStates = gcnew CellStateCollection();
	}


}


