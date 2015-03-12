#pragma once

#include "Utility.h"
#include "Nt_NormalDist.h"
#include "Nt_ScalarField.h"

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

		bool initialized;
		double DiffusionCoefficient;
		Nt_ScalarField ^molpopConc;

		List<Nt_MolecularPopulation^> ^ComponentPopulations;

		Nt_MolecularPopulation(String^ _molguid, double diff_coeff);

		Nt_MolecularPopulation(String^ _molguid, double diff_coeff, Nt_ScalarField^ conc);

		virtual Nt_MolecularPopulation^ CloneParent()
		{
			Nt_MolecularPopulation^ molpop = gcnew Nt_MolecularPopulation(this->molguid, this->DiffusionCoefficient);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->Name = this->Name;
			molpop->ComponentPopulations->Add(this);
			return molpop;
		}

		virtual void updateManaged()
		{
			//update component
			if (this->molpopConc != nullptr)
			{
				molpopConc->pull();
				//double *dptr = _molpopConc;
				//for (int i=0; i< molpopConc->Length; i++)
				//{
				//	molpopConc[i] = *dptr++;
				//}
			}
			else 
			{
				//update collection
				double *dptr = _molpopConc;
				for (int i=0; i< ComponentPopulations->Count; i++)
				{
					ComponentPopulations[i]->molpopConc->pull();
					//array<double> ^item = ComponentPopulations[i]->molpopConc;
					//for (int j= 0; j< item->Length; j++)
					//{
					//	item[j] = *dptr++;
					//}
				}
			}
		}

		virtual void updateUnmanaged(array<double> ^conc)
		{
			double *dptr = _molpopConc;
			for (int i=0; i<conc->Length; i++)
			{
				*dptr++ = conc[i];
			}
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void initialize();

		virtual void step(double dt);

		virtual double* native_pointer(){ return _molpopConc;}

		property double *nativePointer
		{
			double* get()
			{
				return _molpopConc;
			}
			void set(double *value)
			{
				_molpopConc = value;
				if (molpopConc != nullptr)
				{
					molpopConc->_array = value;
				}
			}
		}
		double *_molpopConc;
	protected:
		//double *_molpopConc;
		//the size of memory allocated, number of items.
		int allocedItemCount;
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
		Nt_ScalarField^ boundaryFlux;
		Nt_ScalarField^ boundaryConc; //this is mostlikey not necessary fo cytosol

		Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, String^ _molguid, double _diffusionCoefficient, Nt_ScalarField^ conc, Nt_ScalarField^ bflux, Nt_ScalarField^ bconc);

		virtual Nt_MolecularPopulation^ CloneParent() override
		{
			Nt_CytosolMolecularPopulation^ molpop = gcnew Nt_CytosolMolecularPopulation(-1, CellRadius, this->molguid, this->DiffusionCoefficient, nullptr, nullptr, nullptr);
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->Name = this->Name;
			molpop->ComponentPopulations->Add(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;

		virtual void updateManaged() override;

		virtual void updateUnmanaged(array<double> ^conc) override;

		virtual void initialize() override;

		virtual void step(double dt) override;

		double *boundary_conc_pointer(){return _boundaryConc;}

		double *boundary_flux_pointer(){return _boundaryFlux;}
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
		bool initialized;

		List<int> ^cellIds;

		Nt_MembraneMolecularPopulation(int _cellId, double _cellRadius, String^ _molguid, double _diffusionCoefficient, Nt_ScalarField ^conc);


		virtual Nt_MolecularPopulation^ CloneParent() override
		{
			Nt_MembraneMolecularPopulation^ molpop = gcnew Nt_MembraneMolecularPopulation(-1, CellRadius, this->molguid, this->DiffusionCoefficient, nullptr);
			molpop->Name = this->Name;
			molpop->ComponentPopulations = gcnew List<Nt_MolecularPopulation^>();
			molpop->ComponentPopulations->Add(this);
			return molpop;
		}

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;

		virtual void step(double dt) override;

		virtual void initialize() override;

		virtual void updateManaged() override;

		virtual void updateUnmanaged(array<double> ^conc) override;

	private:
		double *_laplacian;
	};
}