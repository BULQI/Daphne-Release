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
		MoleculeKey = _molguid;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = nullptr;
		_molpopConc = NULL;
		allocedItemCount = 0;
		BoundaryConcAndFlux = gcnew Dictionary<int, Nt_MolecluarPopulationBoundary^>();
	}

	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, double _diffusionCoefficient, Nt_Darray^ conc)
	{
		MoleculeKey = _molguid;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = conc;
		allocedItemCount = 0;
		_molpopConc = NULL;
	}

	void Nt_MolecularPopulation::step(double dt)
	{
		throw gcnew Exception("not implemented");
	}

	void Nt_MolecularPopulation::step(Nt_Cytosol^ cytosol, double dt)
	{
		double *_boundaryConc = BoundaryConcAndFlux[0]->ConcPointer;
		double *_boundaryFlux = BoundaryConcAndFlux[0]->FluxPointer;

		int n = ComponentPopulations->Count * 4; //each concentration has 4 items
	
		//handle diffusion - laplacian
		NativeDaphneLibrary::Utility::NtDcopy(n, _molpopConc, 1, _laplacian, 1);
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.0, _laplacian, 4); //set the first element to 0

		double factor1 = (-5.0 /(cytosol->CellRadius *cytosol->CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//Diffusion Flux Term
		double factor2 = -5 * dt /cytosol->CellRadius;

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

	void Nt_MolecularPopulation::step(Nt_PlasmaMembrane^ membrane, double dt)
	{
		int n = ComponentPopulations->Count * 4;

		//handle diffusion flux
		NativeDaphneLibrary::Utility::NtDcopy(n, _molpopConc, 1, _laplacian, 1);
		NativeDaphneLibrary::Utility::NtDscal(n/4, 0.0, _laplacian, 4);
		double factor1 = (-2.0 /(membrane->CellRadius *membrane->CellRadius)) * DiffusionCoefficient * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(n, factor1, _laplacian, 1, _molpopConc, 1);
	}

	void Nt_MolecularPopulation::step(Nt_ECS^ ECS, double dt)
	{
				
		throw gcnew Exception("need work on here");
		NtInterpolatedRectangularPrism *ir_prism = ECS->ir_prism;
		double *sfarray = this->molpopConc->NativePointer;
		
		int item_count = ECS->BoundaryKeys->Count;
		//the boudnary_conc here a double **, the point is to collect the boundar_conc of each cell
		//if we pass the boundar_conc of each molpop, that woudld be equivilent here
		//maybe we shold go that way, in that case, we will need to modify the mehtods.
		//consider the best way (place) for this????
		ir_prism->MultithreadNativeRestrict(sfarray, ECS->Positions, item_count, _boundaryConcPtrs);

		//restrict
		//Dictionary<int, Nt_Darray^>^ BoundaryTransform = this->ECS->BoundaryTransform;
		//NtInterpolatedRectangularPrism *ir_prism = this->ECS->ir_prism;
		//for each(KeyValuePair<int, Nt_Darray^>^ kvp in BoundaryTransform) 
		//{
		//	int key = kvp->Key;
		//	double* pos = kvp->Value->NativePointer;
		//	double *sfarray = this->molpopConc->NativePointer;
		//	double *boundConc = this->boundaryConcs[key]->NativePointer;
		//	ir_prism->NativeRestrict(sfarray, &pos, 1, &boundConc);
		//}

	}

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
			_laplacian = (double *)realloc(_laplacian, alloc_size);
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

	void Nt_MolecularPopulation::AddNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
	{

			if (Compartment == nullptr)
			{
				throw gcnew Exception("parent compartment is null");
			}
			int pop_id = Compartment->GetCellPulationId(boundId);
			if (pop_id == -1)
			{
				throw gcnew Exception("unknown boundary population id");
			}
			if (BoundaryConcAndFlux->ContainsKey(pop_id) == false)
			{
				Nt_MolecluarPopulationBoundary^ boundary = gcnew Nt_MolecluarPopulationBoundary();
				boundary->AddBoundaryConcAndFlux(boundId, conc, flux);
				BoundaryConcAndFlux->Add(pop_id, boundary);
			}
			else 
			{
				Nt_MolecluarPopulationBoundary^ boundary = BoundaryConcAndFlux[pop_id];
				boundary->AddBoundaryConcAndFlux(boundId, conc, flux);
			}
	}

	void Nt_MolecularPopulation::initialize(Nt_Cytosol^ ecs)
	{

		//there is nothing to be done here...

	}

	void Nt_MolecularPopulation::initialize(Nt_PlasmaMembrane^ ecs)
	{

		//nothing to be done here yet.
	}


	void Nt_MolecularPopulation::initialize(Nt_ECS^ ecs)
	{
		
		List<int>^ BoundaryKeys = ecs->BoundaryKeys;

		//this is to ensure that cell transoform and bouanaryConc are in same order
		Dictionary<int, Nt_Darray^>^ boundPtrs = gcnew Dictionary<int, Nt_Darray^>();
		for each (KeyValuePair<int, Nt_MolecluarPopulationBoundary^>^ kvp in BoundaryConcAndFlux)
		{
			for (int i=0; i< kvp->Value->boundIdList->Count; i++)
			{
				int key = kvp->Value->boundIdList[i];
				Nt_Darray^ darray = kvp->Value->boundaryConcs[i];
				boundPtrs->Add(key, darray);
			}
		}
		int n1 = BoundaryKeys->Count;
		int n2 = boundPtrs->Count;
		if (BoundaryKeys->Count != boundPtrs->Count)
		{
			throw gcnew Exception("Boundary count mismatch");
		}
		_boundaryConcPtrs = (double **)realloc(_boundaryConcPtrs, BoundaryKeys->Count * sizeof(double *));
		for (int i=0; i< BoundaryKeys->Count; i++)
		{
			int key = BoundaryKeys[i];
			_boundaryConcPtrs[i] = boundPtrs[key]->NativePointer;
		}
	}

}