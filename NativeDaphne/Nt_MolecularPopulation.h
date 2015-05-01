#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_DArray.h"
#include "Nt_Utility.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecluarPopulationBoundary
	{
	public:
		List<int>^ boundIdList;
		List<Nt_Darray^>^ boundaryConcs;
		List<Nt_Darray^>^ boundaryFluxes;
		int allocedItemCount; 

		Nt_MolecluarPopulationBoundary()
		{
			boundIdList = gcnew List<int>();
			boundaryFluxes = gcnew List<Nt_Darray^>();
			boundaryConcs = gcnew List<Nt_Darray^>();
			_boundaryConcs = NULL;
			_boundaryFlux = NULL;
			allocedItemCount = 0;
			array_length = 0;
		}

		void AddBoundaryConcAndFlux(int boundId, Nt_Darray^ conc, Nt_Darray^ flux)
		{
			int itemCount = boundaryConcs->Count;
			int itemLength = conc->Length;
			if (itemCount + 1 > allocedItemCount)
			{
				allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
				int alloc_size = allocedItemCount * itemLength * sizeof(double);
				_boundaryConcs = (double *)realloc(_boundaryConcs, alloc_size);
				_boundaryFlux = (double *)realloc(_boundaryFlux, alloc_size);
				if (! _boundaryConcs || !_boundaryFlux)
				{
					throw gcnew Exception("Error realloc memory");
				}
				//reassign memory address
				for (int i=0; i< itemCount; i++)
				{
					boundaryConcs[i]->NativePointer = _boundaryConcs + i * itemLength;
					boundaryFluxes[i]->NativePointer = _boundaryFlux + i * itemLength;
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
			conc->NativePointer = _cptr;
			flux->NativePointer = _fptr;
			boundaryConcs->Add(conc);
			boundaryFluxes->Add(flux);
			boundIdList->Add(boundId);
			array_length = boundaryConcs->Count * itemLength;
		}

		property double *ConcPointer
		{
			double* get()
			{
				return _boundaryConcs;
			}
		}

		property double *FluxPointer
		{
			double* get()
			{
				return _boundaryFlux;
			}
		}
	private:
		//int allocedItemCount; 
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
		Nt_MolecularPopulation(String^ _molguid, double diff_coeff);

		Nt_MolecularPopulation(String^ _molguid, double diff_coeff, Nt_Darray^ conc);

		virtual Nt_MolecularPopulation^ CloneParent()
		{
			Nt_MolecularPopulation^ molpop = gcnew Nt_MolecularPopulation(this->MoleculeKey, this->DiffusionCoefficient);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->Name = this->Name;
			molpop->AddMolecularPopulation(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void AddNtBoundaryFluxConc(int boundId, Nt_Darray^ conc, Nt_Darray^ flux);
		
		virtual void step(double dt);

		void step(Nt_Cytosol^ cytosol, double dt);

		void step(Nt_PlasmaMembrane ^membrane, double dt);

		void step(Nt_ECS^ ecs, double dt);

		void initialize(Nt_Cytosol^ cytosol);

		void initialize(Nt_PlasmaMembrane ^membrane);

		void initialize(Nt_ECS^ ecs);

		Nt_Compartment^ Compartment;

	internal:

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