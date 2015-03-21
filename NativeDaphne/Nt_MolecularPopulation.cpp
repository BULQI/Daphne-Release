#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>

#include "Nt_MolecularPopulation.h"
#include "Utility.h"
#include "Nt_Utility.h"
#include "Nt_Compartment.h"

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
		_molpopConc = NULL;
		allocedItemCount = 0;
	}

	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, double _diffusionCoefficient, Nt_Darray^ conc)
	{
		molguid = _molguid;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = conc;
		allocedItemCount = 0;
		_molpopConc = NULL;
	}

	void Nt_MolecularPopulation::step(double dt)
	{}

	void Nt_MolecularPopulation::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{
		int itemCount = ComponentPopulations->Count;
		int itemLength = molpop->molpopConc->Length;

		if (itemCount + 1 > allocedItemCount)
		{
			allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
			int alloc_size = allocedItemCount * itemLength * sizeof(double);
			_molpopConc = (double *)realloc(_molpopConc, alloc_size);
			if (_molpopConc == NULL)
			{
				throw gcnew Exception("Error realloc memory");
			}
			//reassign memory address
			for (int i=0; i< itemCount; i++)
			{
					ComponentPopulations[i]->NativePointer = _molpopConc + i * itemLength;
			}
		}
		//copy new values
		double *_cptr = _molpopConc + itemCount * itemLength;
		for (int i=0; i<itemLength; i++)
		{
			_cptr[i] = molpop->molpopConc[i];
		}
		molpop->molpopConc->NativePointer = _cptr;
			
		ComponentPopulations->Add(molpop);
		array_length = ComponentPopulations->Count * itemLength;
	}

	
	///////////////////////////////////
	//Nt_CytosolMolecularPopulation
	///////////////////////////////////
	Nt_CytosolMolecularPopulation::Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, String ^_molguid, double _diffusionCoefficient, 
		Nt_Darray ^conc, Nt_Darray^ bflux, Nt_Darray^ bconc) : Nt_MolecularPopulation(_molguid, _diffusionCoefficient, conc)
	{
		boundaryFlux = bflux;
		boundaryConc = bconc;
		cellId = _cellId;
		CellRadius = _cellRadius;
		_boundaryConc = NULL;
		_boundaryFlux = NULL; 
		_laplacian = NULL;
	}

	//void Nt_CytosolMolecularPopulation::initialize()
	//{
	//	int itemCount = ComponentPopulations->Count;
	//	if (itemCount > allocedItemCount)
	//	{
	//		if (allocedItemCount > 0)
	//		{
	//			free(_laplacian);
	//			free(_molpopConc);
	//			free(_boundaryFlux);
	//			free(_boundaryConc);
	//		}

	//		allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
	//		int allocSize = allocedItemCount * 4 * sizeof(double);
	//		_molpopConc = (double *)malloc(allocSize);
	//		_laplacian = (double *)malloc(allocSize); //will initialize in step()
	//		_boundaryFlux = (double *)malloc(allocSize); //set to 0 after eveyr step
	//		_boundaryConc = (double *)malloc(allocSize);
	//		//update unamanged pointers for components
	//		double *mp_ptr = _molpopConc;
	//		double *bconc_ptr = _boundaryConc;
	//		double *bflux_ptr = _boundaryFlux;
	//		for (int i=0; i< itemCount; i++, mp_ptr+=4, bflux_ptr += 4, bconc_ptr+=4)
	//		{
	//			Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(ComponentPopulations[i]);
	//			molpop->_molpopConc = mp_ptr;
	//			molpop->molpopConc->_array = mp_ptr;

	//			molpop->_boundaryConc = bconc_ptr;

	//			molpop->boundaryFlux->_array = bflux_ptr;
	//			molpop->_boundaryFlux = bflux_ptr;
	//		}
	//	}

	//	int n = 0;
	//	for (int i=0; i< ComponentPopulations->Count; i++)
	//	{
	//		array<double> ^item_c = ComponentPopulations[i]->molpopConc->darray;
	//		for (int j = 0; j< item_c->Length; j++, n++)
	//		{
	//			_molpopConc[n] = item_c[j];
	//		}
	//	}
	//	memset(_boundaryFlux, 0, n*sizeof(double));
	//	memset(_boundaryConc, 0, n*sizeof(double));
	//	initialized = true;
	//}

	void Nt_CytosolMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{
		Nt_CytosolMolecularPopulation ^molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(src);

		int itemCount = ComponentPopulations->Count;
		int itemLength = molpop->molpopConc->Length;

		if (itemCount + 1 > allocedItemCount)
		{
			allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
			int alloc_size = allocedItemCount * itemLength * sizeof(double);
			_molpopConc = (double *)realloc(_molpopConc, alloc_size);
			_boundaryConc = (double *)realloc(_boundaryConc, alloc_size);
			_boundaryFlux = (double *)realloc(_boundaryFlux, alloc_size);
			_laplacian = (double *)realloc(_laplacian, alloc_size);

			if (_molpopConc == NULL || _boundaryConc == NULL || _boundaryFlux == NULL || _laplacian == NULL )
			{
				throw gcnew Exception("Error realloc memory");
			}
			//reassign memory address
			for (int i=0; i< ComponentPopulations->Count; i++)
			{
				Nt_CytosolMolecularPopulation ^mp = dynamic_cast<Nt_CytosolMolecularPopulation ^>(ComponentPopulations[i]);
				mp->NativePointer = _molpopConc + i * itemLength;
				mp->BoundaryFluxPointer = _boundaryFlux + i * itemLength;
				mp->BoundaryConcPointer = _boundaryConc + i * itemLength;
			}
		}
		//copy new values
		double *_cptr = _molpopConc + itemCount * itemLength;
		double *_fptr = _boundaryFlux + itemCount * itemLength;
		double *_bptr = _boundaryConc + itemCount * itemLength;
		for (int i=0; i<itemLength; i++)
		{
			_cptr[i] = molpop->molpopConc[i];
			_fptr[i] = molpop->boundaryFlux[i];
			_bptr[i] = molpop->boundaryConc[i];
		}
		molpop->molpopConc->NativePointer = _cptr;
		molpop->BoundaryFluxPointer = _fptr;
		molpop->BoundaryConcPointer = _bptr;
			
		ComponentPopulations->Add(molpop);
		cellIds->Add(molpop->cellId);
		array_length = ComponentPopulations->Count * itemLength;
		
	}

	void Nt_CytosolMolecularPopulation::step(double dt)
	{
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
		Nt_Darray^ conc) : Nt_MolecularPopulation(_molguid, _diffusionCoefficient, conc)
	{
		cellId = _cellId;
		CellRadius = _cellRadius;
	}

	//void Nt_MembraneMolecularPopulation::initialize()
	//{

	//	int itemCount = ComponentPopulations->Count;
	//	if (itemCount > allocedItemCount)
	//	{
	//		if (allocedItemCount > 0)
	//		{
	//			free(_laplacian);
	//			free(_molpopConc);
	//		}

	//		allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
	//		int allocSize = allocedItemCount * 4 * sizeof(double);
	//		_molpopConc = (double *)malloc(allocSize);
	//		_laplacian = (double *)malloc(allocSize);

	//		//update components
	//		double *mp_ptr = _molpopConc;
	//		for (int i=0; i< itemCount; i++, mp_ptr+=4)
	//		{
	//			Nt_MolecularPopulation ^molpop = ComponentPopulations[i];
	//			molpop->nativePointer = mp_ptr;
	//		}
	//	}

	//	int n = 0;
	//	for (int i=0; i< ComponentPopulations->Count; i++)
	//	{
	//		array<double> ^item_c = ComponentPopulations[i]->molpopConc->darray;
	//		for (int j = 0; j< item_c->Length; j++, n++)
	//		{
	//			_molpopConc[n] = item_c[j];
	//		}
	//	}
	//	initialized = true;
	//}

	void Nt_MembraneMolecularPopulation:: AddMolecularPopulation(Nt_MolecularPopulation ^src)
	{
		Nt_MembraneMolecularPopulation ^molpop = dynamic_cast<Nt_MembraneMolecularPopulation^>(src);

		int itemCount = ComponentPopulations->Count;
		int itemLength = molpop->molpopConc->Length;

		if (itemCount + 1 > allocedItemCount)
		{
			allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
			int alloc_size = allocedItemCount * itemLength * sizeof(double);
			_molpopConc = (double *)realloc(_molpopConc, alloc_size);
			_laplacian = (double *)realloc(_laplacian, alloc_size);

			if (_molpopConc == NULL || _laplacian == NULL )
			{
				throw gcnew Exception("Error realloc memory");
			}
			//reassign memory address
			for (int i=0; i< itemCount; i++)
			{
				ComponentPopulations[i]->NativePointer = _molpopConc + i * itemLength;
			}
		}
		//copy new values
		double *_cptr = _molpopConc + itemCount * itemLength;
		
		for (int i=0; i<itemLength; i++)
		{
			_cptr[i] = molpop->molpopConc[i];
			
		}
		molpop->molpopConc->NativePointer = _cptr;

		ComponentPopulations->Add(molpop);
		cellIds->Add(molpop->cellId);
		array_length = ComponentPopulations->Count * itemLength;

	}

	void Nt_MembraneMolecularPopulation::step(double dt)
	{
		int n = ComponentPopulations->Count * 4;

		//handle diffusion flux
		NativeDaphneLibrary::Utility::NtDcopy(n, _molpopConc, 1, _laplacian, 1);
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.0, _laplacian, 4);
		double factor1 = (-2.0 /(CellRadius *CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);
	}

	Nt_ECSMolecularPopulation::Nt_ECSMolecularPopulation(String ^_molguid, double _diffusionCoefficient, 
		Nt_Darray^ conc) : Nt_MolecularPopulation(_molguid, _diffusionCoefficient, conc)
	{
		boundaryFluxes = gcnew Dictionary<int, Nt_Darray^>();
        boundaryConcs = gcnew Dictionary<int, Nt_Darray^>();
	}

	void Nt_ECSMolecularPopulation::step(double dt)
	{

		//restrict
		Dictionary<int, Nt_Darray^>^ BoundaryTransform = this->ECS->BoundaryTransform;
		NtInterpolatedRectangularPrism *ir_prism = this->ECS->ir_prism;
		//int n = BoundaryTransform->Count * 4;
		//double *output = (double *)malloc(n *sizeof(double));
		for each(KeyValuePair<int, Nt_Darray^>^ kvp in BoundaryTransform) 
		{
			int key = kvp->Key;
			double* pos = kvp->Value->NativePointer;
			double *sfarray = this->molpopConc->NativePointer;
			double *boundConc = this->boundaryConcs[key]->NativePointer;
			ir_prism->NativeRestrict(sfarray, pos, 1, boundConc);
		}
	}

}