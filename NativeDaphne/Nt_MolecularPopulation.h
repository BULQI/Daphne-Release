/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#pragma once

#include <acml.h>

#include "NtUtility.h"
#include "Nt_DArray.h"
#include "Nt_Scalarfield.h"


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;
using namespace Nt_ManifoldRing;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecluarPopulationBoundary
	{
	private:
		List<Nt_MolecluarPopulationBoundary^>^ components;
	public:
		int BoundaryId;
		ScalarField^ Conc;
		ScalarField^ Flux;

		property List<Nt_MolecluarPopulationBoundary^>^ Component
		{
			List<Nt_MolecluarPopulationBoundary^>^ get()
			{
				return components;
			}
		}

		//default constructor for collection
		Nt_MolecluarPopulationBoundary()
		{
		}

		//default constructor for collection
		Nt_MolecluarPopulationBoundary(Manifold^ m, bool isCollection)
		{
			Conc = gcnew ScalarField(m);
			Flux = gcnew ScalarField(m);
			if (isCollection)
			{
				components = gcnew List<Nt_MolecluarPopulationBoundary^>();
				Conc->Initialize("ScalarFieldCollection", nullptr);
				Flux->Initialize("ScalarFieldCollection", nullptr);
			}
		}

		Nt_MolecluarPopulationBoundary(int boundId, ScalarField^ conc, ScalarField^ flux)
		{
			BoundaryId = boundId;
			Conc = conc;
			Flux = flux;
		}

		Nt_MolecluarPopulationBoundary^ CloneParent()
		{
			return gcnew Nt_MolecluarPopulationBoundary(this->Conc->M, true);
			
		}

		void AddBoundaryConcAndFlux(Nt_MolecluarPopulationBoundary^ boundary)
		{
			this->Conc->AddComponent(boundary->Conc);
			this->Flux->AddComponent(boundary->Flux);
			components->Add(boundary);
		}

		//remove a boundary by its component conc
		void RemoveBoundaryConcAndFlux(Nt_MolecluarPopulationBoundary^ item)
		{
			if (components->Count == 0)
			{
				throw gcnew Exception("remove boundary error 1: component is empty");
			}
			int index = this->Conc->RemoveComponent(item->Conc);
			this->Flux->RemoveComponent(item->Flux);

			if (components[index] != item)
			{
				throw gcnew Exception("boundary mismatch error");
			}
			
			if (index != components->Count-1 )
			{
				components[index] = components[components->Count -1];
			}	
			components->RemoveAt(components->Count -1);
		}
				
		Nt_MolecluarPopulationBoundary^ firstComponent()
		{
			if (components->Count == 0)
			{
				return nullptr;
			}
			return components[0];
		}

		bool IsContainer()
		{
			return components != nullptr;
		}

		property double *ConcPointer
		{
			double* get()
			{
				return Conc->ArrayPointer;
			}
		}

		property double *FluxPointer
		{
			double* get()
			{
				return Flux->ArrayPointer;
			}
		}

		property int ComponentCount
		{
			int get()
			{
				return components == nullptr ? -1 : components->Count;
			}
		}
	};


	ref class Nt_Compartment;
	ref class Nt_Cytosol;
	ref class Nt_PlasmaMembrane;
	ref class Nt_ECS;

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecularPopulation
	{

	protected:
		ScalarField^ concentration;
		initonly Manifold^ manifold;
	internal:
		//this exist only for help debug
		String^ Name;
		double DiffusionCoefficient;
	public:

		//molecule identity
		String^ MoleculeKey;

		property Manifold^ Man
		{
			Manifold^ get()
			{
				return manifold;
			}
		}


		property bool IsDiffusing;

		property ScalarField^ Conc
        {
            ScalarField^ get() 
            {
                return concentration; 
            }
            void set(ScalarField^ value) 
            {
                concentration = value; 
            }
        }

		List<Nt_MolecularPopulation^> ^ComponentPopulations;

		//this is the boundary grouped by cellpopulation.
		//for molpop in ECS, the key is cell, population_Id
		//for molpop in Cytosol, the key does not matter, default to 0
		Dictionary<int, Nt_MolecluarPopulationBoundary^>^ BoundaryConcAndFlux;

		//the is the individual boundary that is not organized into colleciton
		//it exist to facilitate removal of individual boundaries.
		Dictionary<int, Nt_MolecluarPopulationBoundary^>^ ComponentBoundaryConcAndFlux;

		//constructor
		Nt_MolecularPopulation(Manifold^ m, String^ _molguid, String^ _name, double diff_coeff);

		Nt_MolecularPopulation(Manifold^ m, String^ _molguid, String^ _name, double diff_coeff, ScalarField^ conc);

		virtual Nt_MolecularPopulation^ CloneParent(Nt_Compartment^ c);

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void RemoveMolecularPopulation(int index);

		virtual void AddNtBoundaryFluxConc(int boundId, ScalarField^ conc, ScalarField^ flux);

		virtual void SetNtBoundaryFluxConc(Nt_MolecluarPopulationBoundary^ boundary);

		virtual void AddNtBoundaryFluxConc(Nt_MolecluarPopulationBoundary^ boundary);

		virtual void RemoveNtBoundaryFluxConc(int boundary_id);

		virtual void RemoveNtBoundaryFluxConc(Nt_MolecluarPopulationBoundary^ item);
		
		virtual void step(double dt);

		void step(Nt_Cytosol^ cytosol, double dt);

		void step(Nt_PlasmaMembrane ^membrane, double dt);

		void step(Nt_ECS^ ecs, double dt);

		void initialize(Nt_Cytosol^ cytosol);

		void initialize(Nt_PlasmaMembrane ^membrane);

		void initialize(Nt_ECS^ ecs);
		void UpdateBoundary(Nt_ECS^ ECS);

		Nt_Compartment^ Compartment;

	internal:
		
		Nt_MolecularPopulation^ parent;

		property double *ConcPointer
		{
			double* get()
			{
				return concentration->ArrayPointer;
			}
			//cannot set
		}

		//array length
		property int Length
		{
			int get()
			{
				return concentration->darray->Length;
			}
		}
		
	protected:
		double *_laplacian;				
		//for cells in ecs
		double **_boundaryConcPtrs;
		bool initialized;
	};

}