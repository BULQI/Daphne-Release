#pragma once

#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecluarPopulationBoundary
	{
	public:
		int BoundaryId;
		Nt_Darray ^Conc;
		Nt_Darray ^Flux;

		List<Nt_MolecluarPopulationBoundary^>^ Component;


		Nt_MolecluarPopulationBoundary()
		{
			_boundaryConcs = NULL;
			_boundaryFlux = NULL;
			allocedItemCount = 0;
			array_length = 0;
			Component = gcnew List<Nt_MolecluarPopulationBoundary^>();
		}

		Nt_MolecluarPopulationBoundary(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
		{
			BoundaryId = boundId;
			Conc = conc;
			Flux = flux;

			allocedItemCount = 1;
			array_length = conc->Length;

			_boundaryConcs = NULL;
			_boundaryFlux = NULL;
		}

		Nt_MolecluarPopulationBoundary^ CloneParent()
		{
			return gcnew Nt_MolecluarPopulationBoundary();
		}

		void AddBoundaryConcAndFlux(Nt_MolecluarPopulationBoundary^ boundary)
		{
			int itemCount = Component->Count;
			Nt_Darray^ conc = boundary->Conc;
			Nt_Darray^ flux = boundary->Flux;
			int itemLength = conc->Length;
			if (itemCount + 1 > allocedItemCount)
			{
				allocedItemCount = NtUtility::GetAllocSize(itemCount+1, allocedItemCount);
				int alloc_size = allocedItemCount * conc->Length * sizeof(double);
				_boundaryConcs = (double *)realloc(_boundaryConcs, alloc_size);
				_boundaryFlux = (double *)realloc(_boundaryFlux, alloc_size);
				if (! _boundaryConcs || !_boundaryFlux)
				{
					throw gcnew Exception("Error realloc memory");
				}
				//reassign memory address
				for (int i=0; i< itemCount; i++)
				{
					Nt_MolecluarPopulationBoundary^ item = Component[i];
					item->Conc->NativePointer = _boundaryConcs + i * itemLength;
					item->Flux->NativePointer = _boundaryFlux + i * itemLength;
				}
			}

			//appened new values
			double *_cptr = _boundaryConcs + itemCount * itemLength;
			double *_fptr = _boundaryFlux + itemCount * itemLength;
			for (int i=0; i< itemLength; i++)
			{
				_cptr[i] = conc[i];
				_fptr[i] = flux[i];
			}
			boundary->Conc->NativePointer = _cptr;
			boundary->Flux->NativePointer = _fptr;
			Component->Add(boundary);
			array_length = Component->Count * conc->Length;
		}

		void AddBoundaryConcAndFlux(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
		{
			Nt_MolecluarPopulationBoundary^ boundary = gcnew Nt_MolecluarPopulationBoundary(boundId, conc, flux);
			AddBoundaryConcAndFlux(boundary);
		}

		//remove a boundary by its component conc
		void RemoveBoundaryConcAndFlux(Nt_MolecluarPopulationBoundary^ item)
		{
			if (Component->Count == 0)
			{
				throw gcnew Exception("remove boundary error 1: component is empty");
			}
			
			int index = (int) (item->Conc->NativePointer - _boundaryConcs)/item->Conc->Length;
			//debug
			if (Component[index] != item)
			{
				throw gcnew Exception("boundary item mismatch");
			}

			Nt_MolecluarPopulationBoundary^ lastItem = Component[Component->Count-1];
			if (item != lastItem)
			{
				item->Conc->MemSwap(lastItem->Conc);
				item->Flux->MemSwap(lastItem->Flux);
				Component[index] = lastItem;
			}
			item->Conc->detach();
			item->Flux->detach();
			Component->RemoveAt(Component->Count-1);

			//release memory if no item left.
			if (Component->Count == 0)
			{
				if (this->allocedItemCount > 0)
				{
					free(_boundaryConcs);
					free(_boundaryFlux);
					_boundaryConcs = NULL;
					_boundaryFlux = NULL;
					allocedItemCount = 0;
				}
			}

		}

		void RemoveBoundaryConcAndFlux(int index)
		{
			if (Component->Count == 0)
			{
				throw gcnew Exception("remoeve boundary error 2: component is empty");
			}
			if (index < 0 || index >= Component->Count)
			{
				throw gcnew Exception("remove boundary error: index out of range");
			}
			RemoveBoundaryConcAndFlux(Component[index]);
		}

		void RemoveBoundaryConcAndFlux(Nt_Darray^ boundary_conc)
		{
			if (Component->Count == 0)
			{
				throw gcnew Exception("remoeve boundary error: component is empty");
			}
			int index = (int)(boundary_conc->NativePointer - _boundaryConcs)/boundary_conc->Length;
			if (index < 0 || index >= Component->Count || Component[index]->Conc != boundary_conc)
			{
				throw gcnew Exception("remove boundary error: index out range");
			}
			RemoveBoundaryConcAndFlux(Component[index]);
		}
				
		Nt_MolecluarPopulationBoundary^ firstComponent()
		{
			if (Component->Count == 0)
			{
				return nullptr;
			}
			return Component[0];
		}

		bool IsContainer()
		{
			return Component != nullptr;
		}

		property double *ConcPointer
		{
			double* get()
			{
				if (Component == nullptr)
				{
					return Conc->NativePointer;
				}
				return _boundaryConcs;
			}
		}

		property double *FluxPointer
		{
			double* get()
			{
				if (Component == nullptr)
				{
					return Flux->NativePointer;
				}
				return _boundaryFlux;
			}
		}

		property int ComponentCount
		{
			int get()
			{
				return Component == nullptr ? -1 : Component->Count;
			}
		}

	private:
		int allocedItemCount; 
		int array_length;
		double *_boundaryConcs;
		double *_boundaryFlux;
	};


	ref class Nt_Compartment;
	ref class Nt_Cytosol;
	ref class Nt_PlasmaMembrane;
	ref class Nt_ECS;

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecularPopulation
	{

	internal:
		//this exist only for help debug
		String^ Name;
		double DiffusionCoefficient;
	public:

		//molecule identity
		String^ MoleculeKey;

		Nt_Darray ^molpopConc;

		List<Nt_MolecularPopulation^> ^ComponentPopulations;

		//for molpop in ECS, the key is population_Id
		//for molpop in Cytosol, the key does not matter, default to 0
		Dictionary<int, Nt_MolecluarPopulationBoundary^>^ BoundaryConcAndFlux;


		//constructor
		Nt_MolecularPopulation(String^ _molguid, String^ _name, double diff_coeff);

		Nt_MolecularPopulation(String^ _molguid, String^ _name, double diff_coeff, Nt_Darray^ conc);

		virtual Nt_MolecularPopulation^ CloneParent(Nt_Compartment^ c)
		{
			Nt_MolecularPopulation^ molpop = gcnew Nt_MolecularPopulation(this->MoleculeKey, this->Name, this->DiffusionCoefficient);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->Compartment = c;
			molpop->AddMolecularPopulation(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void RemoveMolecularPopulation(int index);

		virtual void AddNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux);

		virtual void SetNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux);

		virtual void AddNtBoundaryFluxConc(Nt_MolecluarPopulationBoundary^ boundary);

		virtual void RemoveNtBoundaryFluxConc(int boundId);
		
		virtual void step(double dt);

		void step(Nt_Cytosol^ cytosol, double dt);

		void step(Nt_PlasmaMembrane ^membrane, double dt);

		void step(Nt_ECS^ ecs, double dt);

		void initialize(Nt_Cytosol^ cytosol);

		void initialize(Nt_PlasmaMembrane ^membrane);

		void initialize(Nt_ECS^ ecs);

		Nt_Compartment^ Compartment;

	internal:
		Nt_MolecularPopulation^ parent;

		property double *NativePointer
		{
			double* get()
			{
				return molpopConc != nullptr ? molpopConc->NativePointer : _molpopConc;
			}
			void set(double *value)
			{
				if (molpopConc != nullptr)
				{
					molpopConc->NativePointer = value;
				}
				else 
				{
					_molpopConc = value;
				}
			}
		}

		property int Length
		{
			int get()
			{
				return array_length;
			}
		}
		
	protected:
		double *_molpopConc;
		double *_laplacian;
		//the size of memory allocated, number of items.
		int allocedItemCount;
				
		//for cells in ecs
		double **_boundaryConcPtrs;
		int array_length;
		bool initialized;
	};

}