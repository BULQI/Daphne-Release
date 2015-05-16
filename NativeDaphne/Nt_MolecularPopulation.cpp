#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <acml.h>

#include "Nt_MolecularPopulation.h"
#include "NtUtility.h"
#include "Nt_Compartment.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;

namespace NativeDaphne
{

	///////////////////////////////////
	//Nt_MolecularPopulation
	///////////////////////////////////
	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, String^ _name, double _diffusionCoefficient)
	{
		MoleculeKey = _molguid;
		Name = _name;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = nullptr;
		_molpopConc = NULL;
		allocedItemCount = 0;
		BoundaryConcAndFlux = gcnew Dictionary<int, Nt_MolecluarPopulationBoundary^>();
		parent = nullptr;
	}

	Nt_MolecularPopulation::Nt_MolecularPopulation(String ^_molguid, String^ _name, double _diffusionCoefficient, Nt_Darray^ conc)
	{
		MoleculeKey = _molguid;
		Name = _name;
		DiffusionCoefficient = _diffusionCoefficient;
		molpopConc = conc;
		allocedItemCount = 0;
		_molpopConc = NULL;
		parent = nullptr;
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
		dcopy(n, _molpopConc, 1, _laplacian, 1);
		dscal(n/4, 0.0, _laplacian, 4); //set the first element to 0

		double factor1 = (-5.0 /(cytosol->CellRadius *cytosol->CellRadius)) * DiffusionCoefficient * dt;
		daxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		//Diffusion Flux Term
		double factor2 = -5 * dt /cytosol->CellRadius;

		//if (this->Name == "A*")
		//{
		//	Console::WriteLine("_boundarFlux[0] = {0} _molpopConc[0] = {1}", _boundaryFlux[0], _molpopConc[0]);
		//}
		dscal(n/4, 0.6, _boundaryFlux, 4); //set the first element to 3/5, so that we can time 5 for all elements
		daxpy(n, factor2, _boundaryFlux, 1, _molpopConc, 1);

	
		//clear flux
		memset(_boundaryFlux, 0, n*sizeof(double));

		//update membrane boundary
		memcpy(_boundaryConc, _molpopConc, n *sizeof(double));
		
	}

	void Nt_MolecularPopulation::step(Nt_PlasmaMembrane^ membrane, double dt)
	{
		int n = ComponentPopulations->Count * 4;

		//handle diffusion flux
		dcopy(n, _molpopConc, 1, _laplacian, 1);
		dscal(n/4, 0.0, _laplacian, 4);
		double factor1 = (-2.0 /(membrane->CellRadius *membrane->CellRadius)) * DiffusionCoefficient * dt;
		daxpy(n, factor1, _laplacian, 1, _molpopConc, 1);
	}

	void Nt_MolecularPopulation::step(Nt_ECS^ ECS, double dt)
	{
				
		throw gcnew Exception("removed from implementation for reactions only");
		/*NtInterpolatedRectangularPrism *ir_prism = ECS->ir_prism;
		double *sfarray = this->molpopConc->NativePointer;
		int item_count = ECS->BoundaryKeys->Count;
		ir_prism->MultithreadNativeRestrict(sfarray, ECS->Positions, item_count, _boundaryConcPtrs);*/
	}

	void Nt_MolecularPopulation::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{
		int itemCount = ComponentPopulations->Count;
		int itemLength = molpop->molpopConc->Length;

		if (itemCount + 1 > allocedItemCount)
		{
			allocedItemCount = NtUtility::GetAllocSize(itemCount+1, allocedItemCount);
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
			
		molpop->parent = this;
		ComponentPopulations->Add(molpop);
		array_length = ComponentPopulations->Count * itemLength;

		//merge boundaries, this should only be needed for cells.
		if (molpop->BoundaryConcAndFlux->Count > 0)
		{
			if (molpop->BoundaryConcAndFlux->Count != 1)
			{
				throw gcnew Exception("unexpected number of boundaries");
			}
			
			//this means that the cells BoundaryConcAndFlux would be empty...
			//do we want this, or we keep it there, but not as an grouped component????
			//any complication?
			for each (KeyValuePair<int, Nt_MolecluarPopulationBoundary^>^kvp in molpop->BoundaryConcAndFlux)
			{
				Nt_MolecluarPopulationBoundary^ boundary = kvp->Value;
				if (boundary->IsContainer() == true)
				{
					throw gcnew Exception("cytosol should only have one boundary");
				}
				this->AddNtBoundaryFluxConc(boundary);
			}
		}
	}


	void Nt_MolecularPopulation::RemoveMolecularPopulation(int index)
	{
		int itemCount = ComponentPopulations->Count;
		if (index < 0 || index >= itemCount)
		{
			throw gcnew Exception("Error RemoveMolecularPopulation: index out of range");
		}

		Nt_MolecularPopulation^ target = ComponentPopulations[index];
		Nt_MolecularPopulation^ last_molpop = ComponentPopulations[itemCount-1];
		if (target != last_molpop)
		{
			//swap the content
			target->molpopConc->MemSwap(last_molpop->molpopConc);
			target->molpopConc->detach();
			ComponentPopulations[index] = last_molpop;
		}
		target->parent = nullptr;
		ComponentPopulations->RemoveAt(itemCount-1);

		//here we are dealing with cytosol/plasma mebrane, so we 
		//have maximum one boundary
		if (target->BoundaryConcAndFlux->Count > 0)
		{
			for each (KeyValuePair<int, Nt_MolecluarPopulationBoundary^>^kvp in target->BoundaryConcAndFlux)
			{
				Nt_MolecluarPopulationBoundary^ boundary = kvp->Value;
				if (boundary->IsContainer() == true)
				{
					throw gcnew Exception("cytosol should only have one boundary");
				}
				this->RemoveNtBoundaryFluxConc(boundary->BoundaryId);
			}
		}
	}

	//add - add to a collection
	void Nt_MolecularPopulation::AddNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
	{

			if (Compartment == nullptr)
			{
				throw gcnew Exception("parent compartment is null");
			}
			if (Compartment->manifoldType == Nt_ManifoldType::TinyBall)
			{
				SetNtBoundaryFluxConc(boundId, conc, flux);
				return;
			}
			
			int pop_id = Compartment->GetCellPulationId(boundId);
			if (pop_id == -1)
			{
				throw gcnew Exception("unknown boundary population id");
			}
			if (BoundaryConcAndFlux->ContainsKey(pop_id) == false)
			{
				Nt_MolecluarPopulationBoundary^ boundary = gcnew Nt_MolecluarPopulationBoundary();
				boundary->AddBoundaryConcAndFlux(gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux));
				BoundaryConcAndFlux->Add(pop_id, boundary);
			}
			else 
			{
				BoundaryConcAndFlux[pop_id]->AddBoundaryConcAndFlux(gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux));
			}
	}

	//set boundary and flux - for cytosol which has only one boundary
	void Nt_MolecularPopulation::SetNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
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
			Nt_MolecluarPopulationBoundary^ boundary = gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux);
			if (BoundaryConcAndFlux->ContainsKey(pop_id) == false)
			{
				BoundaryConcAndFlux->Add(pop_id, boundary);
			}
			else 
			{
				BoundaryConcAndFlux[pop_id] = boundary;
			}
	}

	void Nt_MolecularPopulation::AddNtBoundaryFluxConc(Nt_MolecluarPopulationBoundary^ boundary)
	{

		AddNtBoundaryFluxConc(boundary->BoundaryId, boundary->Conc, boundary->Flux);
	}

	void Nt_MolecularPopulation::RemoveNtBoundaryFluxConc(int boundary_id)
	{
		if (Compartment == nullptr)
		{
			throw gcnew Exception("parent compartment is null");
		}
		int pop_id = Compartment->GetCellPulationId(boundary_id);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}

		if (BoundaryConcAndFlux->ContainsKey(pop_id) == false)
		{
			throw gcnew Exception("Error RemoveNtBoundayrFluxCon - no poulation id found");
		}

		Nt_MolecluarPopulationBoundary^ boundary = BoundaryConcAndFlux[pop_id];

		if (boundary->IsContainer() == false)
		{
			throw gcnew Exception("Error RemoveNtBoundayrFluxCon - not collection");
		}
		int index = Compartment->GetCellPopulationIndex(boundary_id);
				
		boundary->RemoveBoundaryConcAndFlux(index);
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
		Dictionary<int, Nt_Darray^>^ boundaryConcDict = gcnew Dictionary<int, Nt_Darray^>();
		for each (KeyValuePair<int, Nt_MolecluarPopulationBoundary^>^ kvp in BoundaryConcAndFlux)
		{
			Nt_MolecluarPopulationBoundary^ boundary = kvp->Value;
			if (boundary->IsContainer() == false)
			{
				boundaryConcDict->Add(boundary->BoundaryId, boundary->Conc);
			}
			else 
			{
				List<Nt_MolecluarPopulationBoundary^>^ Component = boundary->Component;
				for (int i=0; i< Component->Count; i++)
				{
					boundaryConcDict->Add(Component[i]->BoundaryId, Component[i]->Conc);
				}
			}
		}
		int n1 = BoundaryKeys->Count;
		int n2 = boundaryConcDict->Count;
		if (BoundaryKeys->Count != boundaryConcDict->Count)
		{
			throw gcnew Exception("Boundary count mismatch");
		}
		_boundaryConcPtrs = (double **)realloc(_boundaryConcPtrs, BoundaryKeys->Count * sizeof(double *));
		for (int i=0; i< BoundaryKeys->Count; i++)
		{
			int key = BoundaryKeys[i];
			_boundaryConcPtrs[i] = boundaryConcDict[key]->NativePointer;
		}
	}

}