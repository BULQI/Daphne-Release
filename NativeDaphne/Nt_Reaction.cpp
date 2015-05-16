#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>
#include <acml.h>

#include "Nt_Reaction.h"
#include "NtUtility.h"
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
		//add in derived class
		throw gcnew NotImplementedException();
	}

	void Nt_Reaction::RemoveReaction(int index)
	{
		//add in derived class
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
		_reactant = Reactant->NativePointer;
		array_length = Reactant->Length;
		if (!_reactant)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_Annihilation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}


	Nt_Reaction^ Nt_Annihilation::CloneParent()
	{
		Nt_Annihilation^ rxn = gcnew Nt_Annihilation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = this->Reactant->parent != nullptr ? this->Reactant->parent : this->Reactant;
		this->Steppable = false;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Annihilation::Step(double dt)
	{
		dscal(array_length, (1.0 -RateConstant*dt), _reactant, 1);
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
		array_length = Reactant1->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant1 = Reactant1->NativePointer;
		_reactant2 = Reactant2->NativePointer;
		_product = Product->NativePointer;
		if (_reactant1 == NULL || _reactant2 == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_Association::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant1->Length;
	}


	Nt_Reaction^ Nt_Association::CloneParent()
	{
		Nt_Association^ rxn = gcnew Nt_Association(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant1 = Reactant1->parent != nullptr ? Reactant1->parent : Reactant1;
		rxn->Reactant2 = Reactant2->parent != nullptr ? Reactant2->parent : Reactant2;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Association::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _reactant1, _reactant2, _intensity);
		daxpy(array_length, -dt * RateConstant, _intensity, 1, _reactant1, 1);
		daxpy(array_length, -dt * RateConstant, _intensity, 1, _reactant2, 1);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
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
		array_length = Reactant->Length;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_Dimerization::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_Dimerization::CloneParent()
	{
		Nt_Dimerization^ rxn = gcnew Nt_Dimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Dimerization::Step(double dt)
	{
		daxpy(array_length, dt*RateConstant, _reactant, 1, _product, 1);
		dscal(array_length, (1.0-RateConstant * dt *2), _reactant, 1);
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
		array_length = Reactant->Length;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_DimerDissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_DimerDissociation::CloneParent()
	{
		Nt_DimerDissociation^ rxn = gcnew Nt_DimerDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_DimerDissociation::Step(double dt)
	{
		daxpy(array_length, RateConstant * dt * 2, _reactant, 1, _product, 1);
		dscal(array_length, (1.0-RateConstant * dt), _reactant, 1);
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
		array_length = Reactant->Length;
		_reactant = Reactant->NativePointer;
		_product1 = Product1->NativePointer;
		_product2 = Product2->NativePointer;
		if (_reactant == NULL || _product1 == NULL || _product2 == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_Dissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_Dissociation::CloneParent()
	{
		Nt_Dissociation^ rxn = gcnew Nt_Dissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product1 = Product1->parent != nullptr ? Product1->parent : Product1;
		rxn->Product2 = Product2->parent != nullptr ? Product2->parent : Product2;;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Dissociation::Step(double dt)
	{
		daxpy(array_length, dt*RateConstant, _reactant, 1, _product1, 1);
		daxpy(array_length, dt*RateConstant, _reactant, 1, _product2, 1);
		dscal(array_length, (1.0-RateConstant * dt), _reactant, 1);
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
		array_length = Reactant->Length;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_Transformation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_Transformation::CloneParent()
	{
		Nt_Transformation^ rxn = gcnew Nt_Transformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}
	
	void Nt_Transformation::Step(double dt)
	{
		
		NtUtility::NtDoubleDaxpy(array_length, RateConstant *dt, _reactant, _product);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));

		_reactant = Reactant->NativePointer;
		_catalyst = Catalyst->NativePointer;
		if (_reactant == NULL || _catalyst == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_AutocatalyticTransformation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_AutocatalyticTransformation::CloneParent()
	{
		Nt_AutocatalyticTransformation^ rxn = gcnew Nt_AutocatalyticTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_AutocatalyticTransformation::Step(double dt)
	{
		NativeDaphneLibrary::NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _catalyst, 1);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_reactant = Reactant->NativePointer;
		_catalyst = Catalyst->NativePointer;
		if (_reactant == NULL || _catalyst == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_CatalyzedAnnihilation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_CatalyzedAnnihilation::CloneParent()
	{
		Nt_CatalyzedAnnihilation^ rxn = gcnew Nt_CatalyzedAnnihilation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAnnihilation::Step(double dt)
	{
		NativeDaphneLibrary::NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
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
		array_length = Reactant1->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_intensity2 = (double *)realloc(_intensity, array_length *sizeof(double));
		_reactant1 = Reactant1->NativePointer;
		_reactant2 = Reactant2->NativePointer;
		_catalyst = Catalyst->NativePointer;
		_product = Product->NativePointer;
		if (_reactant1 == NULL || _catalyst == NULL || _reactant2 == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_CatalyzedAssociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant1->Length;
	}

	Nt_Reaction^ Nt_CatalyzedAssociation::CloneParent()
	{
		Nt_CatalyzedAssociation^ rxn = gcnew Nt_CatalyzedAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant1 = Reactant1->parent != nullptr ? Reactant1->parent : Reactant1;
		rxn->Reactant2 = Reactant2->parent != nullptr ? Reactant2->parent : Reactant2;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAssociation::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant1, _intensity);
		NtUtility::NtMultiplyScalar(array_length, 4, _intensity, _reactant2, _intensity2);
		daxpy(array_length, -dt*RateConstant, _intensity2, 1, _reactant1, 1);
		daxpy(array_length, -dt*RateConstant, _intensity2, 1, _reactant2, 1);
		daxpy(array_length, dt*RateConstant, _intensity2, 1, _product, 1);
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
		array_length = Product->Length;
		_catalyst = Catalyst->NativePointer;
		_product = Product->NativePointer;
		if (_catalyst == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_CatalyzedCreation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Product->Length;
	}

	Nt_Reaction^ Nt_CatalyzedCreation::CloneParent()
	{
		Nt_CatalyzedCreation^ rxn = gcnew Nt_CatalyzedCreation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedCreation::Step(double dt)
	{
		daxpy(array_length, dt*RateConstant, _catalyst, 1, _product, 1);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_catalyst = Catalyst->NativePointer;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_CatalyzedDimerization::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_CatalyzedDimerization::CloneParent()
	{
		Nt_CatalyzedDimerization^ rxn = gcnew Nt_CatalyzedDimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerization::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_catalyst = Catalyst->NativePointer;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_CatalyzedDimerDissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_CatalyzedDimerDissociation::CloneParent()
	{
		Nt_CatalyzedDimerDissociation^ rxn = gcnew Nt_CatalyzedDimerDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerDissociation::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, dt*RateConstant*2.0, _intensity, 1, _product, 1);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_catalyst = Catalyst->NativePointer;
		_reactant = Reactant->NativePointer;
		_product1 = Product1->NativePointer;
		_product2 = Product2->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product1 == NULL || _product2 == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_CatalyzedDissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_CatalyzedDissociation::CloneParent()
	{
		Nt_CatalyzedDissociation^ rxn = gcnew Nt_CatalyzedDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product1 = Product1->parent != nullptr ? Product1->parent : Product1;
		rxn->Product2 = Product2->parent != nullptr ? Product2->parent : Product2;
		rxn->AddReaction(this);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDissociation::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product1, 1);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product2, 1);
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
		array_length = Reactant->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_catalyst = Catalyst->NativePointer;
		_reactant = Reactant->NativePointer;
		_product = Product->NativePointer;
		if (_catalyst == NULL || _reactant == NULL || _product == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_CatalyzedTransformation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
	}

	Nt_Reaction^ Nt_CatalyzedTransformation::CloneParent()
	{
		Nt_CatalyzedTransformation^ rxn = gcnew Nt_CatalyzedTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedTransformation::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _catalyst, _reactant, _intensity);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _reactant, 1);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
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
	}

	void Nt_Transcription::AddReaction(Nt_Reaction ^src_rxn)
	{
		this->Steppable = false;
		ComponentReactions->Add(src_rxn);
		array_length = Product->Length;
		_product = Product->NativePointer;
		_activation = Gene->activation_pointer();
		CopyNumber = Gene->CopyNumber;
		if (_product == NULL || _activation == NULL)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_Transcription::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Product->Length;
	}

	Nt_Reaction^ Nt_Transcription::CloneParent()
	{
		Nt_Transcription^ rxn = gcnew Nt_Transcription(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Gene = Gene->parent != nullptr ? Gene->parent : Gene;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Transcription::Step(double dt)
	{
		//note: transcirption only addes to the first element, not to all 4 elements of _product
		double factor = RateConstant * CopyNumber * dt;
		daxpy(array_length/4, factor, _activation, 1, _product, 4);
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
		array_length = Receptor->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_bulkActivated_BoundaryFlux = BulkActivated->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_receptor = Receptor->NativePointer;
		if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_bulkActivated_BoundaryFlux || !_receptor)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_CatalyzedBoundaryActivation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
	}

	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent()
	{
		throw gcnew Exception("boundary reacitn needs boundaryid, call CloneParent(int boundarid) instead");
	}

	Nt_Reaction^ Nt_CatalyzedBoundaryActivation::CloneParent(int boundary_id)
	{
		Nt_CatalyzedBoundaryActivation^ rxn = gcnew Nt_CatalyzedBoundaryActivation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Bulk = Bulk->parent != nullptr ? Bulk->parent : Bulk;
		rxn->BulkActivated = BulkActivated->parent != nullptr ? BulkActivated->parent : BulkActivated;
		rxn->Receptor = Receptor->parent != nullptr ? Receptor->parent : Receptor;
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedBoundaryActivation::Step(double dt)
	{
		int n = array_length;
		NtUtility::NtMultiplyScalar(n, 4, _receptor, _bulk_BoundaryConc, _intensity);
		daxpy(n, RateConstant, _intensity, 1, _bulk_BoundaryFlux, 1);
		daxpy(n, -RateConstant, _intensity, 1, _bulkActivated_BoundaryFlux, 1);
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
		array_length = Membrane->Length;
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[boundaryId]->ConcPointer; //native_pointer(); //this only works for cytosol, not for ECS
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_membraneConc = Membrane->NativePointer;
		if (!_bulk_BoundaryConc || !_bulk_BoundaryFlux || !_membraneConc)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_BoundaryTransportTo:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Membrane->Length;
	}


	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent()
	{
		throw gcnew Exception("not implemented exception");
	}

	Nt_Reaction^ Nt_BoundaryTransportTo::CloneParent(int boundary_id)
	{
		Nt_BoundaryTransportTo^ rxn = gcnew Nt_BoundaryTransportTo(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Bulk = Bulk->parent != nullptr ? Bulk->parent : Bulk;
		rxn->Membrane = Membrane->parent != nullptr ? Membrane->parent : Membrane;
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}


	void Nt_BoundaryTransportTo::Step(double dt)
	{
		daxpy(array_length, RateConstant, _bulk_BoundaryConc, 1, _bulk_BoundaryFlux, 1);
		daxpy(array_length, RateConstant * dt, _bulk_BoundaryConc, 1, _membraneConc, 1);
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
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Membrane->Length;
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_membraneConc = Membrane->NativePointer;
		if (!_bulk_BoundaryFlux || !_membraneConc)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_BoundaryTransportFrom:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Membrane->Length;
	}

	Nt_Reaction^ Nt_BoundaryTransportFrom::CloneParent(int boundary_id)
	{
		Nt_BoundaryTransportFrom^ rxn = gcnew Nt_BoundaryTransportFrom(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Bulk = Bulk->parent != nullptr ? Bulk->parent : Bulk;
		rxn->Membrane = Membrane->parent != nullptr ? Membrane->parent : Membrane;
		rxn->boundaryId = boundary_id;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryTransportFrom::Step(double dt)
	{
		daxpy(array_length, -RateConstant, _membraneConc, 1, _bulk_BoundaryFlux, 1);
		dscal(array_length, (1.0-RateConstant * dt), _membraneConc, 1);
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
		_intensity = NULL;
	}

	void Nt_BoundaryAssociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Receptor->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = Receptor->NativePointer;	//membrane
		_complex = Complex->NativePointer;		//membrane
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[boundaryId]->FluxPointer;

		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}

	void Nt_BoundaryAssociation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
	}

	Nt_Reaction^ Nt_BoundaryAssociation::CloneParent(int popid)
	{
		Nt_BoundaryAssociation^ rxn = gcnew Nt_BoundaryAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = popid;
		rxn->Complex = Complex->parent != nullptr ? Complex->parent : Complex;
		rxn->Ligand = Ligand->parent != nullptr ? Ligand->parent : Ligand;
		rxn->Receptor = Receptor->parent != nullptr ? Receptor->parent : Receptor;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryAssociation::Step(double dt)
	{
		NtUtility::NtMultiplyScalar(array_length, 4, _receptor, _ligand_BoundaryConc, _intensity);
		daxpy(array_length, RateConstant, _intensity, 1, _ligand_BoundaryFlux, 1);
		daxpy(array_length, -dt*RateConstant, _intensity, 1, _receptor, 1);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _complex, 1);
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
		_intensity = NULL;
	}

	void Nt_BoundaryDissociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Receptor->Length;
		_intensity = (double *)realloc(_intensity, array_length *sizeof(double));
		_receptor = Receptor->NativePointer;	//membrane
		_complex = Complex->NativePointer;		//membrane
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		if (!_receptor || !_complex || !_ligand_BoundaryConc || !_ligand_BoundaryFlux)
		{
			throw gcnew Exception("reaction component not initialized");
		}
	}


	void Nt_BoundaryDissociation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
	}


	Nt_Reaction^ Nt_BoundaryDissociation::CloneParent(int boundary_id)
	{
		Nt_BoundaryDissociation^ rxn = gcnew Nt_BoundaryDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = boundary_id;
		rxn->Complex = Complex->parent != nullptr ? Complex->parent : Complex;
		rxn->Ligand = Ligand->parent != nullptr ? Ligand->parent : Ligand;
		rxn->Receptor = Receptor->parent != nullptr ? Receptor->parent : Receptor;
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryDissociation::Step(double dt)
	{
		daxpy(array_length, -RateConstant, _complex, 1, _ligand_BoundaryFlux, 1);	//ligand.BoundaryFluxes -=  rateConsant * Complex
		daxpy(array_length, RateConstant * dt, _complex, 1, _receptor, 1);			//receptor += rateConsant * Complex * dt
		daxpy(array_length, -RateConstant * dt, _complex, 1, _complex, 1);
	}
}