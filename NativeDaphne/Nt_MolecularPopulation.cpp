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
	Nt_MolecularPopulation::Nt_MolecularPopulation(Manifold^ m, String ^_molguid, String^ _name, double _diffusionCoefficient)
	{
		manifold = m;
		MoleculeKey = _molguid;
		Name = _name;
		DiffusionCoefficient = _diffusionCoefficient;

		concentration = gcnew ScalarField(m);
		BoundaryConcAndFlux = gcnew Dictionary<int, Nt_MolecluarPopulationBoundary^>();
		parent = nullptr;
		_laplacian = NULL;
		_boundaryConcPtrs = NULL;
	}

	Nt_MolecularPopulation^ Nt_MolecularPopulation::CloneParent(Nt_Compartment^ c)
	{
		Nt_MolecularPopulation^ molpop = gcnew Nt_MolecularPopulation(this->manifold, this->MoleculeKey, this->Name, this->DiffusionCoefficient);
		molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
		molpop->Compartment = c;
		//make the ScalarField as a collection
		molpop->Conc->Initialize("ScalarFieldCollection", nullptr);
		molpop->AddMolecularPopulation(this);
		molpop->IsDiffusing = this->IsDiffusing;
		return molpop;
	}

	Nt_MolecularPopulation::Nt_MolecularPopulation(Manifold^ m, String ^_molguid, String^ _name, double _diffusionCoefficient, ScalarField^ conc)
	{
		manifold = m;
		MoleculeKey = _molguid;
		Name = _name;
		DiffusionCoefficient = _diffusionCoefficient;
		concentration = conc;
		parent = nullptr;
		_laplacian = NULL;
		_boundaryConcPtrs = NULL;
	}

	void Nt_MolecularPopulation::step(double dt)
	{
		//throw gcnew Exception("not currently used");
		if (IsDiffusing == true)
		{
			concentration->Add(concentration->Laplacian()->Multiply(dt * DiffusionCoefficient));
		}

	}


	//for debugging - check memeory alignment
	//bool mem_aligned(void *ptr, int alignment)
	//{
	//	if (((unsigned long long)ptr % alignment ) == 0)
	//	{
	//		return true;
	//	}
	//	else 
	//	{
	//		return false;
	//	}
	//}


	void Nt_MolecularPopulation::step(Nt_Cytosol^ cytosol, double dt)
	{

		if (IsDiffusing == true)
		{
			//concentration->Add(concentration->Laplacian()->Multiply(dt * DiffusionCoefficient));
			ScalarField^ laplacian = concentration->Laplacian();
			daxpy(concentration->darray->Length, dt * DiffusionCoefficient, laplacian->ArrayPointer, 1, concentration->ArrayPointer, 1);
		}

		//handle diffusion flux terms, cytosol has only one boundary plasma membrane
		Nt_MolecluarPopulationBoundary^ molbound = BoundaryConcAndFlux[0];
		concentration->DiffusionFluxTerm(molbound->Flux, cytosol->BoundaryTransform, dt);

		//clear flux
		Nt_Darray^ flux = molbound->Flux->darray;
		NtUtility::mem_zero_d(flux->NativePointer, flux->Length);
		//memset(flux->NativePointer, 0, flux->Length*sizeof(double));

		//update membrane boundary
		NtUtility::mem_copy_d(molbound->Conc->ArrayPointer, concentration->ArrayPointer, molbound->Conc->ArrayLength);
		//memcpy(molbound->Conc->ArrayPointer, concentration->ArrayPointer, molbound->Conc->ArrayLength * sizeof(double));


		//older implementaiton.
		//double *_boundaryConc = BoundaryConcAndFlux[0]->ConcPointer;
		//double *_boundaryFlux = BoundaryConcAndFlux[0]->FluxPointer;

		//int n = ComponentPopulations->Count * 4; //each concentration has 4 items
	
		////handle diffusion - laplacian
		//dcopy(n, _molpopConc, 1, _laplacian, 1);
		//dscal(n/4, 0.0, _laplacian, 4); //set the first element to 0

		//double factor1 = (-5.0 /(cytosol->CellRadius *cytosol->CellRadius)) * DiffusionCoefficient * dt;
		//daxpy(n, factor1, _laplacian, 1, _molpopConc, 1);

		////Diffusion Flux Term
		//double factor2 = -5 * dt /cytosol->CellRadius;


		//dscal(n/4, 0.6, _boundaryFlux, 4); //set the first element to 3/5, so that we can time 5 for all elements
		//daxpy(n, factor2, _boundaryFlux, 1, _molpopConc, 1);

	
		////clear flux
		//memset(_boundaryFlux, 0, n*sizeof(double));

		////update membrane boundary
		//memcpy(_boundaryConc, _molpopConc, n *sizeof(double));
		
	}

	void Nt_MolecularPopulation::step(Nt_PlasmaMembrane^ membrane, double dt)
	{

		if (IsDiffusing == true)
		{
			concentration->Add(concentration->Laplacian()->Multiply(dt * DiffusionCoefficient));
		}

		//non "generic" verison
		//double*  _molpopConc = concentration->ArrayPointer;
		//int n = concentration->Length;
		//dcopy(n, _molpopConc, 1, _laplacian, 1);
		//dscal(n/4, 0.0, _laplacian, 4);
		//double factor1 = (-2.0 /(membrane->CellRadius *membrane->CellRadius)) * DiffusionCoefficient * dt;
		//daxpy(n, factor1, _laplacian, 1, _molpopConc, 1);
	}

	void Nt_MolecularPopulation::step(Nt_ECS^ ECS, double dt)
	{
		if (IsDiffusing == true)
		{
			//note: this is the generic approach, we can implement specialized laplacian for ecs
			//to speed this up.
			ScalarField^ laplacian = concentration->Laplacian();
			daxpy(concentration->darray->Length, dt * DiffusionCoefficient, laplacian->ArrayPointer, 1, concentration->ArrayPointer, 1);
			//concentration->Add(concentration->Laplacian()->Multiply(dt * DiffusionCoefficient));
		}		
	}

	//update boundary for ECS
	void Nt_MolecularPopulation::UpdateBoundary(Nt_ECS^ ECS)
	{
		//this is the speed up version
		NtInterpolatedRectangularPrism *ir_prism = ECS->ir_prism;
		double *sfarray = this->ConcPointer;
		int item_count = ECS->BoundaryKeys->Count;
		
		//ir_prism->NativeRestrict(sfarray, ECS->Positions, item_count, _boundaryConcPtrs);
		//multithread version
		ir_prism->MultithreadNativeRestrict(sfarray, ECS->Positions, item_count, _boundaryConcPtrs);
	}

	void Nt_MolecularPopulation::AddMolecularPopulation(Nt_MolecularPopulation^ molpop)
	{

		concentration->AddComponent(molpop->Conc);
		molpop->parent = this;
		ComponentPopulations->Add(molpop);

		int len = molpop->Conc->darray->Length;
		if (_laplacian != NULL)
		{
			int y = 1;
		}
		_laplacian = (double *)realloc(_laplacian, molpop->Conc->darray->Length);

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
					throw gcnew Exception("cytosol should have only one boundary");
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
		concentration->RemoveComponent(target->Conc);

		Nt_MolecularPopulation^ last_molpop = ComponentPopulations[itemCount-1];
		if (target != last_molpop)
		{
			ComponentPopulations[index] = last_molpop;
		}
		target->parent = nullptr;
		ComponentPopulations->RemoveAt(itemCount-1);

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
	void Nt_MolecularPopulation::AddNtBoundaryFluxConc(int boundId, ScalarField^ conc, ScalarField^ flux)
	{

			if (Compartment == nullptr)
			{
				throw gcnew Exception("parent compartment is null");
			}
			if (Compartment->manifoldType == Nt_ManifoldType::TinyBall)
			{
				//BoundaryConcAndFlux->Add(boundId, gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux));
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
				Nt_MolecluarPopulationBoundary^ boundary = gcnew Nt_MolecluarPopulationBoundary(conc->M, true); 				
				boundary->AddBoundaryConcAndFlux(gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux));
				BoundaryConcAndFlux->Add(pop_id, boundary);
			}
			else 
			{
				BoundaryConcAndFlux[pop_id]->AddBoundaryConcAndFlux(gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux));
			}
	}

	//set boundary and flux - for cytosol which has only one boundary
	void Nt_MolecularPopulation::SetNtBoundaryFluxConc(int boundId, ScalarField^ conc, ScalarField^ flux)
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
				boundaryConcDict->Add(boundary->BoundaryId, boundary->Conc->darray);
			}
			else 
			{
				List<Nt_MolecluarPopulationBoundary^>^ Component = boundary->Component;
				for (int i=0; i< Component->Count; i++)
				{
					boundaryConcDict->Add(Component[i]->BoundaryId, Component[i]->Conc->darray);
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