#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecularPopulation
	{
	public:

		//molecule identity
		String^ molguid; 
		String^ Name;

		double DiffusionCoefficient;
		Nt_Darray ^molpopConc;

		List<Nt_MolecularPopulation^> ^ComponentPopulations;

		Nt_MolecularPopulation(String^ _molguid, double diff_coeff);

		Nt_MolecularPopulation(String^ _molguid, double diff_coeff, Nt_Darray^ conc);

		virtual Nt_MolecularPopulation^ CloneParent()
		{
			Nt_MolecularPopulation^ molpop = gcnew Nt_MolecularPopulation(this->molguid, this->DiffusionCoefficient);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->Name = this->Name;
			molpop->AddMolecularPopulation(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void step(double dt);

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
		//the size of memory allocated, number of items.
		int allocedItemCount;
		int array_length;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CytosolMolecularPopulation : public Nt_MolecularPopulation
	{
	public:
		int cellId;
		double CellRadius;

		List<int> ^cellIds;

		//these exist only for access from managed side. but may not really necessary for the unmanaged side.
		//since boundaryFluxes are updated for each step and then reset to 0
		//boundaryConc is actually same thing (for cytosol) as this->Conc except it is marked as tinyShere as its manifold.
		Nt_Darray^ boundaryFlux;
		Nt_Darray^ boundaryConc; //this is mostlikey not necessary fo cytosol

		Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, String^ _molguid, double _diffusionCoefficient, Nt_Darray^ conc, Nt_Darray^ bflux, Nt_Darray^ bconc);

		virtual Nt_MolecularPopulation^ CloneParent() override
		{
			Nt_CytosolMolecularPopulation^ molpop = gcnew Nt_CytosolMolecularPopulation(-1, CellRadius, this->molguid, this->DiffusionCoefficient, nullptr, nullptr, nullptr);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->cellIds = gcnew List<int>();
			molpop->Name = this->Name;
			molpop->AddMolecularPopulation(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;

		virtual void step(double dt) override;

		property double *BoundaryConcPointer
		{
			double* get()
			{
				return boundaryConc != nullptr ? boundaryConc->NativePointer : _boundaryConc;
			}
			void set(double *value)
			{
				if (boundaryConc != nullptr)
				{
					boundaryConc->NativePointer = value;
				}
				else 
				{
					_boundaryConc = value;
				}
			}
		}
		
		property double *BoundaryFluxPointer
		{
			double* get()
			{
				return boundaryFlux != nullptr ? boundaryFlux->NativePointer : _boundaryFlux;
			}
			void set(double *value)
			{
				if (boundaryFlux != nullptr)
				{
					boundaryFlux->NativePointer = value;
				}
				else 
				{
					_boundaryFlux = value;
				}
			}
		}
		
	private:
		//preallocated for step()
		double *_laplacian;
		double *_boundaryFlux;
		double *_boundaryConc;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MembraneMolecularPopulation : public Nt_MolecularPopulation
	{
	public:
		int cellId;
		double CellRadius;

		List<int> ^cellIds;

		Nt_MembraneMolecularPopulation(int _cellId, double _cellRadius, String^ _molguid, double _diffusionCoefficient, Nt_Darray ^conc);

		virtual Nt_MolecularPopulation^ CloneParent() override
		{
			Nt_MembraneMolecularPopulation^ molpop = gcnew Nt_MembraneMolecularPopulation(-1, CellRadius, this->molguid, this->DiffusionCoefficient, nullptr);
			molpop->Name = this->Name;
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->cellIds = gcnew List<int>();
			molpop->AddMolecularPopulation(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;

		virtual void step(double dt) override;

	private:
		double *_laplacian;
	};


	[SuppressUnmanagedCodeSecurity]
	public ref class ECS_Boundary
	{
	public:
		List<int>^ boundaryKeyList;
		Dictionary<int, Nt_Darray^>^ boundaryFluxes;
        Dictionary<int, Nt_Darray^>^ boundaryConcs;

		ECS_Boundary()
		{
			boundaryKeyList = gcnew List<int>();
			boundaryFluxes = gcnew Dictionary<int, Nt_Darray^>();
			boundaryConcs = gcnew Dictionary<int, Nt_Darray^>();
			_boundaryConcs = NULL;
		}

	private:
		double *_boundaryConcs;
	};

	ref class Nt_ECS;

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_ECSMolecularPopulation : public Nt_MolecularPopulation
	{
	public:
		Nt_ECS^ ECS;

		//the boundaries are orgnanized by cellpopulaiton
		//so that we can group boundaries together to perform reactions.
		Dictionary<int, ECS_Boundary^>^ cellBoundaries;

		Nt_ECSMolecularPopulation(String^ _molguid, double _diffusionCoefficient, Nt_Darray ^conc);

		virtual Nt_MolecularPopulation^ CloneParent() override
		{
			throw gcnew Exception("not implemented exception");
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override
		{
			throw gcnew Exception("not implemented exception");
		}

		void AddBoundaryFlux(int cellpop_id, int id, Nt_Darray^ flux)
		{
			if (cellBoundaries->ContainsKey(cellpop_id) == false)
			{
				cellBoundaries->Add(cellpop_id, gcnew ECS_Boundary());
			}
			cellBoundaries[cellpop_id]->boundaryFluxes->Add(id, flux);
		}

		void AddBoundaryConc(int cellpop_id, int id, Nt_Darray^ conc)
		{
			if (cellBoundaries->ContainsKey(cellpop_id) == false)
			{
				cellBoundaries->Add(cellpop_id, gcnew ECS_Boundary());
			}
			cellBoundaries[cellpop_id]->boundaryConcs->Add(id, conc);
		}

		void initialize();

		virtual void step(double dt) override;

	private:
		//this boudnaryConc is for update boundary
		//it contains all cell boundaries for this molpop, irrespective of cell population
		double **_boundaryConc;
		double *_laplacian;
	};

}