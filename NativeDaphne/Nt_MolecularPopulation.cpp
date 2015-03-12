#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>

#include "Nt_MolecularPopulation.h"
#include "Utility.h"
#include "Nt_Utility.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{

	///////////////////////////////////
	//Nt_MolecularPopulation
	///////////////////////////////////
	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, double _diffusionCoefficient)
	{
		molguid = _molguid;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = nullptr;
		allocedItemCount = 0;
		initialized = false;
	}

	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, double _diffusionCoefficient, Nt_ScalarField^ conc)
	{
		molguid = _molguid;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = conc;
		allocedItemCount = 0;
	}

	void Nt_MolecularPopulation::step(double dt)
	{}

	void Nt_MolecularPopulation::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{
		ComponentPopulations->Add(molpop);
	}

	void Nt_MolecularPopulation::initialize()
	{
		int itemCount = ComponentPopulations->Count;
		if (itemCount > allocedItemCount)
		{
			if (allocedItemCount > 0)
			{
				free(_molpopConc);
			}

			allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
			int allocSize = allocedItemCount * 4 * sizeof(double);
			_molpopConc = (double *)malloc(allocSize);

			//update unamanged pointers for components
			double *mp_ptr = _molpopConc;
			for (int i=0; i< itemCount; i++, mp_ptr+=4)
			{
				Nt_MolecularPopulation ^molpop = ComponentPopulations[i];
				molpop->_molpopConc = mp_ptr;
			}
		}

		int n = 0;
		for (int i=0; i< ComponentPopulations->Count; i++)
		{
			array<double> ^item_c = ComponentPopulations[i]->molpopConc->darray;
			for (int j = 0; j< item_c->Length; j++, n++)
			{
				_molpopConc[n] = item_c[j];
			}
		}
		initialized = true;
	}


	///////////////////////////////////
	//Nt_CytosolMolecularPopulation
	///////////////////////////////////
	Nt_CytosolMolecularPopulation::Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, String ^_molguid, double _diffusionCoefficient, 
		Nt_ScalarField ^conc, Nt_ScalarField^ bflux, Nt_ScalarField^ bconc) : Nt_MolecularPopulation(_molguid, _diffusionCoefficient, conc)
	{
		boundaryFlux = bflux;
		boundaryConc = bconc;

		cellIds = gcnew List<int>();
		cellIds->Add(_cellId);
		CellRadius = _cellRadius;

		initialized = false;
	}

	void Nt_CytosolMolecularPopulation::initialize()
	{
		int itemCount = ComponentPopulations->Count;
		if (itemCount > allocedItemCount)
		{
			if (allocedItemCount > 0)
			{
				free(_laplacian);
				free(_molpopConc);
				free(_boundaryFlux);
				free(_boundaryConc);
			}

			allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
			int allocSize = allocedItemCount * 4 * sizeof(double);
			_molpopConc = (double *)malloc(allocSize);
			_laplacian = (double *)malloc(allocSize); //will initialize in step()
			_boundaryFlux = (double *)malloc(allocSize); //set to 0 after eveyr step
			_boundaryConc = (double *)malloc(allocSize);
			//update unamanged pointers for components
			double *mp_ptr = _molpopConc;
			double *bconc_ptr = _boundaryConc;
			double *bflux_ptr = _boundaryFlux;
			for (int i=0; i< itemCount; i++, mp_ptr+=4, bflux_ptr += 4, bconc_ptr+=4)
			{
				Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(ComponentPopulations[i]);
				molpop->_molpopConc = mp_ptr;
				molpop->molpopConc->_array = mp_ptr;

				molpop->_boundaryConc = bconc_ptr;

				molpop->boundaryFlux->_array = bflux_ptr;
				molpop->_boundaryFlux = bflux_ptr;
			}
		}

		int n = 0;
		for (int i=0; i< ComponentPopulations->Count; i++)
		{
			array<double> ^item_c = ComponentPopulations[i]->molpopConc->darray;
			for (int j = 0; j< item_c->Length; j++, n++)
			{
				_molpopConc[n] = item_c[j];
			}
		}
		memset(_boundaryFlux, 0, n*sizeof(double));
		memset(_boundaryConc, 0, n*sizeof(double));
		initialized = true;
	}

	//copy date to managed array.
	void Nt_CytosolMolecularPopulation::updateManaged()
	{

		if (this->molpopConc != nullptr)
		{
			double *dptr = _molpopConc;
			double *fluxptr = _boundaryFlux;
			molpopConc->pull();
			boundaryConc->pull();
			boundaryFlux->pull();
			/*for (int i=0; i< molpopConc->Length; i++)
			{
				molpopConc[i] = *dptr;
				boundaryConc[i] = *dptr++;
				boundaryFlux[i] = *fluxptr++;
			}*/
		}
		else 
		{
			//update collection
			//here flux may not need to be updated
			double *dptr = _molpopConc;
			double *fluxptr = _boundaryFlux;
			for (int i=0; i< ComponentPopulations->Count; i++)
			{
				Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(ComponentPopulations[i]);
				molpop->molpopConc->pull();
				molpop->boundaryConc->pull();
				molpop->boundaryFlux->pull();
			}
		}
	}

	void Nt_CytosolMolecularPopulation::updateUnmanaged(array<double> ^conc)
	{
		Nt_MolecularPopulation::updateUnmanaged(conc);
		//to do: study how to how about boundary and flux
	}


	void Nt_CytosolMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{
		ComponentPopulations->Add(src);
		Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(src);
		cellIds->Add(molpop->cellIds[0]);
		if (initialized == true)
		{
			updateManaged(); //copy back to managed and will call back when initialized again.
							 //to do: optimize - memcopy array and add new ones.
			initialized = false;
		}
	}

	void Nt_CytosolMolecularPopulation::step(double dt)
	{

		if (initialized == false)initialize();
		int n = ComponentPopulations->Count * 4; //each concentration has 4 items
	
		//handle diffusion - laplacian
		NativeDaphneLibrary::Utility::NtDcopy(n, _molpopConc, 1, _laplacian, 1);
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.0, _laplacian, 4); //set the first element to 0

		double factor1 = (-5.0 /(CellRadius *CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//Diffusion Flux Term
		double factor2 = -5 * dt /CellRadius;

		//if (this->Name == "A*")
		//{
		//	Console::WriteLine("_boundarFlux[0] = {0} _molpopConc[0] = {1}", _boundaryFlux[0], _molpopConc[0]);
		//}
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.6, _boundaryFlux, 4); //set the first element to 3/5, so that we can time 5 for all elements
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor2, _boundaryFlux, 1, _molpopConc, 1);

	
		//clear flux
		memset(_boundaryFlux, 0, n*sizeof(double));

		//update membrane boundary
		memcpy(_boundaryConc, _molpopConc, n *sizeof(double));
	}

	//////////////////////////////////////////
	// Nt_MembraneMolecularPopulation
	//////////////////////////////////////////
	//Nt_MembraneMolecularPopulation
	Nt_MembraneMolecularPopulation::Nt_MembraneMolecularPopulation(int _cellId, double _cellRadius, String ^_molguid, double _diffusionCoefficient, 
		Nt_ScalarField^ conc) : Nt_MolecularPopulation(_molguid, _diffusionCoefficient, conc)
	{
		cellIds = gcnew List<int>();
		cellIds->Add(_cellId);

		CellRadius = _cellRadius;
		initialized = false;
	}

	void Nt_MembraneMolecularPopulation::initialize()
	{

		int itemCount = ComponentPopulations->Count;
		if (itemCount > allocedItemCount)
		{
			if (allocedItemCount > 0)
			{
				free(_laplacian);
				free(_molpopConc);
			}

			allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
			int allocSize = allocedItemCount * 4 * sizeof(double);
			_molpopConc = (double *)malloc(allocSize);
			_laplacian = (double *)malloc(allocSize);

			//update components
			double *mp_ptr = _molpopConc;
			for (int i=0; i< itemCount; i++, mp_ptr+=4)
			{
				Nt_MolecularPopulation ^molpop = ComponentPopulations[i];
				molpop->nativePointer = mp_ptr;
			}
		}

		int n = 0;
		for (int i=0; i< ComponentPopulations->Count; i++)
		{
			array<double> ^item_c = ComponentPopulations[i]->molpopConc->darray;
			for (int j = 0; j< item_c->Length; j++, n++)
			{
				_molpopConc[n] = item_c[j];
			}
		}
		initialized = true;
	}

	void Nt_MembraneMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{
		ComponentPopulations->Add(src);
		Nt_MembraneMolecularPopulation ^molpop = dynamic_cast<Nt_MembraneMolecularPopulation^>(src);
		cellIds->Add(molpop->cellIds[0]);
		initialized = false;
	}

	void Nt_MembraneMolecularPopulation::step(double dt)
	{

		if (initialized == false)initialize();
		int n = ComponentPopulations->Count * 4;

		//handle diffusion flux
		NativeDaphneLibrary::Utility::NtDcopy(n, _molpopConc, 1, _laplacian, 1);
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.0, _laplacian, 4);
		double factor1 = (-2.0 /(CellRadius *CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//Nt_MolecularPopulation::updateManaged();
	}

	void Nt_MembraneMolecularPopulation::updateManaged()
	{
		Nt_MolecularPopulation::updateManaged();
	}

	void Nt_MembraneMolecularPopulation::updateUnmanaged(array<double> ^conc)
	{
		Nt_MolecularPopulation::updateUnmanaged(conc);
	}
}