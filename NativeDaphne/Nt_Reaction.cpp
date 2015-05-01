#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>

#include "Nt_Reaction.h"
#include "Utility.h"
#include "Nt_Utility.h"
#include "Nt_Compartment.h"

#include <vcclr.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace NativeDaphneLibrary;

namespace NativeDaphne
{
	Nt_Reaction::Nt_Reaction(double rate_const)
	{
		RateConstant = rate_const;
	}

	void Nt_Reaction::AddReaction(Nt_Reaction ^src_rxn)
	{
		//should never be here, we always need to extend this class
		//consider change this to absolute class(?)
		throw gcnew NotImplementedException();
	}

	void Nt_Reaction::Step(double dt)
	{}

	//*************************************************
	//		Nt_Annihilation
	//*************************************************
	Nt_Annihilation::Nt_Annihilation(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_Annihilation::Nt_Annihilation(Nt_MolecularPopulation^ reactant, double rate_const) :Nt_Reaction(rate_const)
	{
		Reactant = reactant;
	}

	void Nt_Annihilation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Annihilation ^tmp = dynamic_cast<Nt_Annihilation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;
		_reactant = tmp->Reactant->NativePointer;

		if (!_reactant)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_Annihilation::CloneParent()
	{
		Nt_Annihilation^ rxn = gcnew Nt_Annihilation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		this->Steppable = false;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Annihilation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0 -RateConstant*dt), _reactant, 1);
	}


	//****************************************
	//implementation of Nt_Association
	//****************************************
	Nt_Association::Nt_Association(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_Association::Nt_Association(Nt_MolecularPopulation^ _reactant1, Nt_MolecularPopulation^ _reactant2, 
		Nt_MolecularPopulation^ _product, double _rateConst):Nt_Reaction(_rateConst)
	{
			Reactant1 = _reactant1;
			Reactant2 = _reactant2;
			Product = _product;
	}

	void Nt_Association::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_Association ^tmp = dynamic_cast<Nt_Association ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant1->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant1 = tmp->Reactant1->NativePointer;
		_reactant2 = tmp->Reactant2->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_reactant1 == NULL || _reactant2 == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Association::CloneParent()
	{
		Nt_Association^ rxn = gcnew Nt_Association(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Association::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _reactant1, _reactant2, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt * RateConstant, _intensity, 1, _reactant1, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt * RateConstant, _intensity, 1, _reactant2, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
	}


	//****************************************
	//implementation of Nt_Dimerization
	//****************************************
	Nt_Dimerization::Nt_Dimerization(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_Dimerization::Nt_Dimerization(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Reactant = _reactant;
		Product = _product;
	}

	void Nt_Dimerization::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_Dimerization ^tmp = dynamic_cast<Nt_Dimerization ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;
		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Dimerization::CloneParent()
	{
		Nt_Dimerization^ rxn = gcnew Nt_Dimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Dimerization::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _reactant, 1, _product, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-RateConstant * dt *2), _reactant, 1);
	}

	//****************************************
	//implementation of Nt_Dimerization
	//****************************************
	Nt_DimerDissociation::Nt_DimerDissociation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_DimerDissociation::Nt_DimerDissociation(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Reactant = _reactant;
		Product = _product;
	}

	void Nt_DimerDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_DimerDissociation ^tmp = dynamic_cast<Nt_DimerDissociation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;
		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_DimerDissociation::CloneParent()
	{
		Nt_DimerDissociation^ rxn = gcnew Nt_DimerDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_DimerDissociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, RateConstant * dt * 2, _reactant, 1, _product, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-RateConstant * dt), _reactant, 1);
	}


	//****************************************
	//implementation of Nt_Dissociation
	//****************************************
	Nt_Dissociation::Nt_Dissociation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_Dissociation::Nt_Dissociation(Nt_MolecularPopulation^ _reactant, Nt_MolecularPopulation^ _product1, 
		Nt_MolecularPopulation^ _product2, double _rateConst):Nt_Reaction(_rateConst)
	{
			Reactant = _reactant;
			Product1 = _product1;
			Product2 = _product2;
	}

	void Nt_Dissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_Dissociation ^tmp = dynamic_cast<Nt_Dissociation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_reactant = tmp->Reactant->NativePointer;
		_product1 = tmp->Product1->NativePointer;
		_product2 = tmp->Product2->NativePointer;
		if (_reactant == NULL || _product1 == NULL || _product2 == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Dissociation::CloneParent()
	{
		Nt_Dissociation^ rxn = gcnew Nt_Dissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Dissociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _reactant, 1, _product1, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _reactant, 1, _product2, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-RateConstant * dt), _reactant, 1);
	}


	//****************************************
	//implementation of Nt_Transformation
	//****************************************
	Nt_Transformation::Nt_Transformation(double rate_const) : Nt_Reaction(rate_const)
	{
	}

	Nt_Transformation::Nt_Transformation(Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double rate_const) : Nt_Reaction(rate_const)
	{
		Reactant = reactant;
		Product = product;
	}

	void Nt_Transformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transformation ^tmp = dynamic_cast<Nt_Transformation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Transformation::CloneParent()
	{
		Nt_Transformation^ rxn = gcnew Nt_Transformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}
	
	void Nt_Transformation::Step(double dt)
	{
		
		NativeDaphneLibrary::Utility::NtDoubleDaxpy(array_length, RateConstant *dt, _reactant, _product);
	}


	//****************************************
	//implementation of Nt_AutocatalyticTransformation
	//****************************************
	Nt_AutocatalyticTransformation::Nt_AutocatalyticTransformation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_AutocatalyticTransformation::Nt_AutocatalyticTransformation(Nt_MolecularPopulation^ reactant1, Nt_MolecularPopulation^ reactant2, 
		Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		if (reactant1->MoleculeKey == product->MoleculeKey)
		{
			Reactant = reactant2;
			Catalyst = reactant1;
		}
		else 
		{
			Reactant = reactant1;
			Catalyst = reactant2;
		}
	}

	void Nt_AutocatalyticTransformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_AutocatalyticTransformation ^tmp = dynamic_cast<Nt_AutocatalyticTransformation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant = tmp->Reactant->NativePointer;
		_catalyst = tmp->Catalyst->NativePointer;
		if (_reactant == NULL || _catalyst == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_AutocatalyticTransformation::CloneParent()
	{
		Nt_AutocatalyticTransformation^ rxn = gcnew Nt_AutocatalyticTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_AutocatalyticTransformation::Step(double dt)
	{

		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _catalyst, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
	}


	//****************************************
	//implementation of Nt_CatalyzedAnnihilation
	//****************************************
	Nt_CatalyzedAnnihilation::Nt_CatalyzedAnnihilation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_CatalyzedAnnihilation::Nt_CatalyzedAnnihilation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, 
		double _rateConst):Nt_Reaction(_rateConst)
	{
		Reactant = reactant;
		Catalyst = catalyst;
	}

	void Nt_CatalyzedAnnihilation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedAnnihilation ^tmp = dynamic_cast<Nt_CatalyzedAnnihilation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant = tmp->Reactant->NativePointer;
		_catalyst = tmp->Catalyst->NativePointer;
		if (_reactant == NULL || _catalyst == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedAnnihilation::CloneParent()
	{
		Nt_CatalyzedAnnihilation^ rxn = gcnew Nt_CatalyzedAnnihilation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAnnihilation::Step(double dt)
	{

		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
	}


	//****************************************
	//implementation of Nt_CatalyzedAssociation
	//****************************************
	Nt_CatalyzedAssociation::Nt_CatalyzedAssociation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_CatalyzedAssociation::Nt_CatalyzedAssociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant1, 
		Nt_MolecularPopulation^ reactant2, Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Catalyst = catalyst;
		Reactant1 = reactant1;
		Reactant2 = reactant2;
		Product = product;
	}

	void Nt_CatalyzedAssociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedAssociation ^tmp = dynamic_cast<Nt_CatalyzedAssociation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant1->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_intensity2 = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant1 = tmp->Reactant1->NativePointer;
		_reactant2 = tmp->Reactant2->NativePointer;
		_catalyst = tmp->Catalyst->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_reactant1 == NULL || _catalyst == NULL || _reactant2 == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedAssociation::CloneParent()
	{
		Nt_CatalyzedAssociation^ rxn = gcnew Nt_CatalyzedAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAssociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant1, _intensity);
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _intensity, _reactant2, _intensity2);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity2, 1, _reactant1, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity2, 1, _reactant2, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity2, 1, _product, 1);
	}

	//****************************************
	//implementation of Nt_CatalyzedCreation
	//****************************************
	Nt_CatalyzedCreation::Nt_CatalyzedCreation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_CatalyzedCreation::Nt_CatalyzedCreation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Catalyst = catalyst;
		Product = product;
	}

	void Nt_CatalyzedCreation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedCreation ^tmp = dynamic_cast<Nt_CatalyzedCreation ^>(ComponentReactions[0]);
		array_length *= tmp->Product->molpopConc->Length;

		_catalyst = tmp->Catalyst->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_catalyst == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedCreation::CloneParent()
	{
		Nt_CatalyzedCreation^ rxn = gcnew Nt_CatalyzedCreation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedCreation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _catalyst, 1, _product, 1);
	}


	//****************************************
	//implementation of Nt_CatalyzedDimerization
	//****************************************
	Nt_CatalyzedDimerization::Nt_CatalyzedDimerization(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_CatalyzedDimerization::Nt_CatalyzedDimerization(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, 
		Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Catalyst = catalyst;
		Reactant = reactant;
		Product = product;
	}

	void Nt_CatalyzedDimerization::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedDimerization ^tmp = dynamic_cast<Nt_CatalyzedDimerization ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_catalyst = tmp->Catalyst->NativePointer;
		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedDimerization::CloneParent()
	{
		Nt_CatalyzedDimerization^ rxn = gcnew Nt_CatalyzedDimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerization::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
	}


	//****************************************
	//implementation of Nt_CatalyzedDimerDissociation
	//****************************************
	Nt_CatalyzedDimerDissociation::Nt_CatalyzedDimerDissociation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_CatalyzedDimerDissociation::Nt_CatalyzedDimerDissociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, 
		Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Catalyst = catalyst;
		Reactant = reactant;
		Product = product;
	}

	void Nt_CatalyzedDimerDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedDimerDissociation ^tmp = dynamic_cast<Nt_CatalyzedDimerDissociation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_catalyst = tmp->Catalyst->NativePointer;
		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedDimerDissociation::CloneParent()
	{
		Nt_CatalyzedDimerDissociation^ rxn = gcnew Nt_CatalyzedDimerDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerDissociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant*2.0, _intensity, 1, _product, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
	}


	//****************************************
	//implementation of Nt_CatalyzedDissociation
	//****************************************
	Nt_CatalyzedDissociation::Nt_CatalyzedDissociation(double _RateConst):Nt_Reaction(_RateConst)
	{
	}

	Nt_CatalyzedDissociation::Nt_CatalyzedDissociation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, 
		Nt_MolecularPopulation^ product1, Nt_MolecularPopulation^ product2, double _RateConst):Nt_Reaction(_RateConst)
	{
		Catalyst = catalyst;
		Reactant = reactant;
		Product1 = product1;
		Product1 = product1;
	}

	void Nt_CatalyzedDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedDissociation ^tmp = dynamic_cast<Nt_CatalyzedDissociation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_catalyst = tmp->Catalyst->NativePointer;
		_reactant = tmp->Reactant->NativePointer;
		_product1 = tmp->Product1->NativePointer;
		_product2 = tmp->Product2->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product1 == NULL || _product2 == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedDissociation::CloneParent()
	{
		Nt_CatalyzedDissociation^ rxn = gcnew Nt_CatalyzedDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDissociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _product1, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _product2, 1);
	}

	//****************************************
	//implementation of Nt_CatalyzedTransformation
	//****************************************
	Nt_CatalyzedTransformation::Nt_CatalyzedTransformation(double _RateConst):Nt_Reaction(_RateConst)
	{
	}

	Nt_CatalyzedTransformation::Nt_CatalyzedTransformation(Nt_MolecularPopulation^ catalyst, Nt_MolecularPopulation^ reactant, 
		Nt_MolecularPopulation^ product, double _RateConst):Nt_Reaction(_RateConst)
	{
		Catalyst = catalyst;
		Reactant = reactant;
		Product = product;
	}

	void Nt_CatalyzedTransformation::AddReaction(Nt_Reaction^ src_rxn)
	{
		src_rxn->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;
		Nt_CatalyzedTransformation ^tmp = dynamic_cast<Nt_CatalyzedTransformation ^>(ComponentReactions[0]);
		array_length *= tmp->Reactant->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_catalyst = tmp->Catalyst->NativePointer;
		_reactant = tmp->Reactant->NativePointer;
		_product = tmp->Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_CatalyzedTransformation::CloneParent()
	{
		Nt_CatalyzedTransformation^ rxn = gcnew Nt_CatalyzedTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedTransformation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
	}

	//************************************
	//implemenation of Nt_Transcription
	//************************************
	Nt_Transcription::Nt_Transcription(double rate_const) : Nt_Reaction(rate_const)
	{
	}

	Nt_Transcription::Nt_Transcription(Nt_Gene^ gene, Nt_MolecularPopulation^ product, double rate_const) 
		: Nt_Reaction(rate_const)
	{
		Gene = gene;
		Product = product;
		_product = NULL;
	}

	void Nt_Transcription::AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);

		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_Transcription ^tmp = dynamic_cast<Nt_Transcription ^>(ComponentReactions[0]);
		array_length *= tmp->Product->molpopConc->Length;
		_product = tmp->Product->NativePointer;
		_activation = tmp->Gene->activation_pointer();
		CopyNumber = tmp->Gene->CopyNumber;
		if (_product == NULL || _activation == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_Transcription::CloneParent()
	{
		Nt_Transcription^ rxn = gcnew Nt_Transcription(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Transcription::Step(double dt)
	{
		//note: transcirption only addes to the first element, not to all 4 elements of _product
		double factor = RateConstant * CopyNumber * dt;
		NativeDaphneLibrary::Utility::NtDaxpy(array_length/4, factor, _activation, 1, _product, 4);
	}

	//**************************************************
	//implementation of Nt_CatalyzedBoundaryActivation
	//**************************************************
	Nt_CatalyzedBoundaryActivation::Nt_CatalyzedBoundaryActivation(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_CatalyzedBoundaryActivation::Nt_CatalyzedBoundaryActivation(Nt_MolecularPopulation^ bulk, Nt_MolecularPopulation^ bulkActivated, 
		Nt_MolecularPopulation^ receptor, double rate_const) :Nt_Reaction(rate_const)
	{
		Bulk = bulk;
		BulkActivated = bulkActivated;
		Receptor = receptor;
	}

	void Nt_CatalyzedBoundaryActivation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);

		bool isCollection = src_rxn->IsCollection();
		if (isCollection == true)
		{
			throw gcnew Exception("add reaction expects a single reaction, not a collection");
		}

		array_length = ComponentReactions->Count;

		Nt_CatalyzedBoundaryActivation ^tmp = dynamic_cast<Nt_CatalyzedBoundaryActivation ^>(ComponentReactions[0]);
		array_length *= tmp->Bulk->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		Nt_MolecularPopulation^ bulk_molpop1 = tmp->Bulk;
		int cellpop_id = bulk_molpop1->Compartment->GetCellPulationId(tmp->boundaryId);
		if (cellpop_id == -1)
		{
			throw gcnew Exception("cell population id error");
		}
		_bulk_BoundaryFlux = bulk_molpop1->BoundaryConcAndFlux[cellpop_id]->FluxPointer;
		_bulk_BoundaryConc = bulk_molpop1->BoundaryConcAndFlux[cellpop_id]->ConcPointer;


		Nt_MolecularPopulation^ bulk_molpop2 = tmp->BulkActivated;
		_bulkActivated_BoundaryFlux = bulk_molpop2->BoundaryConcAndFlux[cellpop_id]->FluxPointer;

		_receptor = tmp->Receptor->NativePointer;

		if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_bulkActivated_BoundaryFlux || !_receptor)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent()
	{
		throw gcnew Exception("boundary reacitn needs boundaryid for partent");
		Nt_CatalyzedBoundaryActivation^ rxn = gcnew Nt_CatalyzedBoundaryActivation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//for debug
		rxn->Bulk = this->Bulk;
		rxn->BulkActivated = this->BulkActivated;
		rxn->Receptor = this->Receptor;

		rxn->AddReaction(this);
		return rxn;
	}

	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent(int boundary_id)
	{
		Nt_CatalyzedBoundaryActivation^ rxn = gcnew Nt_CatalyzedBoundaryActivation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//for debug
		rxn->Bulk = this->Bulk;
		rxn->BulkActivated = this->BulkActivated;
		rxn->Receptor = this->Receptor;
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedBoundaryActivation::Step(double dt)
	{
		int n = array_length;
		NativeDaphneLibrary::Utility::NtMultiplyScalar(n, 4, _receptor, _bulk_BoundaryConc, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(n, RateConstant, _intensity, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(n, -RateConstant, _intensity, 1, _bulkActivated_BoundaryFlux, 1);
	}
		



	//*************************************
	//    Nt_BoundaryTransportTo
	//*************************************
	Nt_BoundaryTransportTo::Nt_BoundaryTransportTo(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_BoundaryTransportTo::Nt_BoundaryTransportTo(Nt_MolecularPopulation^ bulk, Nt_MolecularPopulation^ membrane, double rate_const) 
		:Nt_Reaction(rate_const)
	{
		Bulk = bulk;
		Membrane = membrane;
	}

	void Nt_BoundaryTransportTo:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportTo ^tmp = dynamic_cast<Nt_BoundaryTransportTo ^>(ComponentReactions[0]);
		array_length *= tmp->Bulk->molpopConc->Length;

		Nt_MolecularPopulation^ bulk_molpop = tmp->Bulk;
		int cellpop_id = bulk_molpop->Compartment->GetCellPulationId(tmp->boundaryId);
		if (cellpop_id == -1)
		{
			throw gcnew Exception("cell pop id error");
		}
		_bulk_BoundaryConc = bulk_molpop->BoundaryConcAndFlux[cellpop_id]->ConcPointer; //native_pointer(); //this only works for cytosol, not for ECS
		_bulk_BoundaryFlux = bulk_molpop->BoundaryConcAndFlux[cellpop_id]->FluxPointer;
		_membraneConc = tmp->Membrane->NativePointer;

		if (!_bulk_BoundaryConc)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent()
	{
		Nt_BoundaryTransportTo^ rxn = gcnew Nt_BoundaryTransportTo(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//this is for debugging, not needed for simulaiton
		rxn->Bulk = this->Bulk;
		rxn->Membrane = this->Membrane;

		rxn->AddReaction(this);
		return rxn;
	}

	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent(int boundary_id)
	{
		Nt_BoundaryTransportTo^ rxn = gcnew Nt_BoundaryTransportTo(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//this is for debugging, not needed for simulaiton
		rxn->Bulk = this->Bulk;
		rxn->Membrane = this->Membrane;
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}


	void Nt_BoundaryTransportTo::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, RateConstant, _bulk_BoundaryConc, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, RateConstant * dt, _bulk_BoundaryConc, 1, _membraneConc, 1);
	}


	//*************************************
	//		Nt_BoundaryTransportFrom
	//*************************************
	Nt_BoundaryTransportFrom::Nt_BoundaryTransportFrom(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_BoundaryTransportFrom::Nt_BoundaryTransportFrom(Nt_MolecularPopulation^ membrane, Nt_MolecularPopulation^ bulk, 
		double rate_const) :Nt_Reaction(rate_const)
	{
		Membrane = membrane;
		Bulk = bulk;
		_bulk_BoundaryFlux = NULL;
		_membraneConc = NULL;
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = ComponentReactions->Count;
		if (array_length == 0)return;

		Nt_BoundaryTransportFrom ^tmp = dynamic_cast<Nt_BoundaryTransportFrom ^>(ComponentReactions[0]);
		array_length *= tmp->Bulk->molpopConc->Length;

		Nt_MolecularPopulation^ bulk_molpop = tmp->Bulk;
		int cellpop_id = bulk_molpop->Compartment->GetCellPulationId(tmp->boundaryId);
		if (cellpop_id == -1)
		{
			throw gcnew Exception("cell pop id error");
		}
		_bulk_BoundaryFlux = bulk_molpop->BoundaryConcAndFlux[cellpop_id]->FluxPointer;
		_membraneConc = tmp->Membrane->NativePointer;

		if (!_bulk_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}

	}

	Nt_Reaction^ Nt_BoundaryTransportFrom::CloneParent(int boundary_id)
	{
		Nt_BoundaryTransportFrom^ rxn = gcnew Nt_BoundaryTransportFrom(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}

	//void Nt_BoundaryTransportFrom::initialize()
	//{
	//	array_length = ComponentReactions->Count;
	//	if (array_length == 0)return;

	//	Nt_BoundaryTransportFrom ^tmp = dynamic_cast<Nt_BoundaryTransportFrom ^>(ComponentReactions[0]);
	//	array_length *= tmp->bulk->molpopConc->Length;
	//	Nt_CytosolMolecularPopulation^ bulk_molpop = dynamic_cast<Nt_CytosolMolecularPopulation ^>(tmp->bulk);
	//	_bulk_BoundaryFlux = bulk_molpop->boundary_flux_pointer();
	//	_membraneConc = tmp->membrane->native_pointer();

	//	if (!_bulk_BoundaryFlux)
	//	{
	//		throw gcnew Exception("reaction component not initialized");
	//	}
	//	initialized = true;
	//}

	void Nt_BoundaryTransportFrom::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -RateConstant, _membraneConc, 1, _bulk_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDscal(array_length, (1.0-RateConstant * dt), _membraneConc, 1);
	}

	//*************************************
	// BoundarAssociation
	//*************************************
	Nt_BoundaryAssociation::Nt_BoundaryAssociation(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_BoundaryAssociation::Nt_BoundaryAssociation(Nt_MolecularPopulation ^receptor, Nt_MolecularPopulation ^ligand, 
		Nt_MolecularPopulation ^complex, double rate_const) :Nt_Reaction(rate_const)
	{
		Receptor = receptor;
		Ligand = ligand;
		Complex = complex;
		_receptor = NULL;
		_complex = NULL;
		_ligand_BoundaryConc = NULL;
		_ligand_BoundaryFlux = NULL;
		_intensity = NULL;
	}

	void Nt_BoundaryAssociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		//do initialize
		array_length = ComponentReactions->Count;
		Nt_BoundaryAssociation ^tmp = dynamic_cast<Nt_BoundaryAssociation ^>(ComponentReactions[0]);
		array_length *= tmp->Receptor->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = tmp->Receptor->molpopConc->NativePointer;	//membrane
		_complex = tmp->Complex->NativePointer;					//membrane

		//set boundary info from ecs.
		Nt_MolecularPopulation^ ligand_molpop = tmp->Ligand;
		if (ligand_molpop != nullptr)
		{	
			_ligand_BoundaryConc = ligand_molpop->BoundaryConcAndFlux[boundaryId]->ConcPointer;
			_ligand_BoundaryFlux = ligand_molpop->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		}

		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_BoundaryAssociation::CloneParent(int popid)
	{
		Nt_BoundaryAssociation^ rxn = gcnew Nt_BoundaryAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		//for debug only
		rxn->Complex = this->Complex;
		rxn->Ligand = this->Ligand;
		rxn->Receptor = this->Receptor;
		rxn->boundaryId = popid;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryAssociation::Step(double dt)
	{
		NativeDaphneLibrary::Utility::NtMultiplyScalar(array_length, 4, _receptor, _ligand_BoundaryConc, _intensity);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, RateConstant, _intensity, 1, _ligand_BoundaryFlux, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, -dt*RateConstant, _intensity, 1, _receptor, 1);
		NativeDaphneLibrary::Utility::NtDaxpy(array_length, dt*RateConstant, _intensity, 1, _complex, 1);
	}

	//*************************************
	// BoundarDissociation
	//*************************************	
	Nt_BoundaryDissociation::Nt_BoundaryDissociation(double rate_const) :Nt_Reaction(rate_const)
	{
	}

	Nt_BoundaryDissociation::Nt_BoundaryDissociation(Nt_MolecularPopulation ^receptor, Nt_MolecularPopulation ^ligand, 
		Nt_MolecularPopulation ^complex, double rate_const) :Nt_Reaction(rate_const)
	{
		Receptor = receptor;
		Ligand = ligand;
		Complex = complex;
		_ligand_BoundaryConc = NULL;  //from bulk
		_ligand_BoundaryFlux = NULL; //from bulk
		_receptor = NULL;
		_complex = NULL;
		_intensity = NULL;
	}

	void Nt_BoundaryDissociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);

		//do initialize
		array_length = ComponentReactions->Count;
		Nt_BoundaryDissociation ^tmp = dynamic_cast<Nt_BoundaryDissociation ^>(ComponentReactions[0]);
		array_length *= tmp->Receptor->molpopConc->Length;

		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = tmp->Receptor->molpopConc->NativePointer;	//membrane
		_complex = tmp->Complex->NativePointer;					//membrane

		Nt_MolecularPopulation^ ligand_molpop = tmp->Ligand;
		if (ligand_molpop != nullptr)
		{	
			_ligand_BoundaryConc = ligand_molpop->BoundaryConcAndFlux[boundaryId]->ConcPointer;
			_ligand_BoundaryFlux = ligand_molpop->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		}

		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	Nt_Reaction^ Nt_BoundaryDissociation::CloneParent(int boundary_id)
	{
		Nt_BoundaryDissociation^ rxn = gcnew Nt_BoundaryDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = boundary_id;

		//for debug only
		rxn->Complex = this->Complex;
		rxn->Ligand = this->Ligand;
		rxn->Receptor = this->Receptor;


		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryDissociation::Step(double dt)
	{
		Utility::NtDaxpy(array_length, -RateConstant, _complex, 1, _ligand_BoundaryFlux, 1); //ligand.BoundaryFluxes -=  rateConsant * Complex
		Utility::NtDaxpy(array_length, RateConstant * dt, _complex, 1, _receptor, 1);		//receptor += rateConsant * Complex * dt
		Utility::NtDaxpy(array_length, -RateConstant * dt, _complex, 1, _complex, 1);
	}
}