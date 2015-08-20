#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>
#include <cmath>
#include <acml.h>

#include "Nt_Reaction.h"
#include "NtUtility.h"
#include "Nt_Compartment.h"
#include "Nt_Manifolds.h"


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
		_reactant = Reactant->ConcPointer;
		array_length = Reactant->Length;
	}

	void Nt_Annihilation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		_reactant = Reactant->ConcPointer;
		array_length = Reactant->Length;
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
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Annihilation::Step(double dt)
	{
		dscal(array_length, (1.0 - RateConstant*dt), _reactant, 1);
	}


	//****************************************
	//implementation of Nt_Association
	//****************************************
	Nt_Association::Nt_Association(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_Association::Nt_Association(Nt_MolecularPopulation^ reactant1, Nt_MolecularPopulation^ reactant2, 
		Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
			Reactant1 = reactant1;
			Reactant2 = reactant2;
			Product = product;
			array_length = Reactant1->Length;
			intensity = gcnew ScalarField(Reactant1->Conc->M);
			_reactant1 = Reactant1->ConcPointer;
			_reactant2 = Reactant2->ConcPointer;
			_product = Product->ConcPointer;

	}

	void Nt_Association::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant1->Length;
		intensity->darray->resize(array_length);
		_reactant1 = Reactant1->ConcPointer;
		_reactant2 = Reactant2->ConcPointer;
		_product = Product->ConcPointer;
	}

	void Nt_Association::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant1->Length;
		if (array_length != ComponentReactions->Count * Reactant1->Man->ArraySize)
		{
			throw gcnew Exception("incorrect array length");
		}
		intensity->darray->resize(array_length);
	}


	Nt_Reaction^ Nt_Association::CloneParent()
	{
		Nt_Association^ rxn = gcnew Nt_Association(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant1 = Reactant1->parent != nullptr ? Reactant1->parent : Reactant1;
		rxn->Reactant2 = Reactant2->parent != nullptr ? Reactant2->parent : Reactant2;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		//temprary variable
		rxn->intensity = gcnew ScalarField(rxn->Reactant1->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_Association::Step(double dt)
	{	
		  
		//this handles the scalar muliplication.
		double* _intensity = intensity->ArrayPointer;
		NtUtility::mem_copy_d(_intensity, Reactant1->Conc->ArrayPointer, array_length);

		//this handles the scalar muliplication.
		intensity->Multiply(Reactant2->Conc);
		dscal(array_length, RateConstant * dt, _intensity, 1);
		//intensity->reset(Reactant1->Conc)->Multiply(Reactant2->Conc)->Multiply(RateConstant * dt);
		
		daxpy(array_length, -1.0, _intensity, 1, _reactant1, 1);
		daxpy(array_length, -1.0, _intensity, 1, _reactant2, 1);
		daxpy(array_length, 1.0, _intensity, 1, _product, 1);
	}


	//****************************************
	//implementation of Nt_Dimerization
	//****************************************
	Nt_Dimerization::Nt_Dimerization(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_Dimerization::Nt_Dimerization(Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Reactant = reactant;
		Product = product;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
	}

	void Nt_Dimerization::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	}

	void Nt_Dimerization::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_Dimerization::CloneParent()
	{
		Nt_Dimerization^ rxn = gcnew Nt_Dimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->AddReaction(this);
		rxn->intensity = gcnew ScalarField(Reactant->Conc->M);
		return rxn;
	}

	void Nt_Dimerization::Step(double dt)
	{
		double* _intensity = intensity->ArrayPointer;
		NtUtility::mem_copy_d(_intensity, _reactant, array_length);
		//handle scalar muliplication.
		intensity->Multiply(Reactant->Conc);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
		dscal(array_length, (1.0-RateConstant * dt *2), _reactant, 1);
	}

	//****************************************
	//implementation of Nt_Dimerization
	//****************************************
	Nt_DimerDissociation::Nt_DimerDissociation(double _rateConst):Nt_Reaction(_rateConst)
	{
	}

	Nt_DimerDissociation::Nt_DimerDissociation(Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product, double _rateConst):Nt_Reaction(_rateConst)
	{
		Reactant = reactant;
		Product = product;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Reactant->Length;
	}

	void Nt_DimerDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
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

	Nt_Dissociation::Nt_Dissociation(Nt_MolecularPopulation^ reactant, Nt_MolecularPopulation^ product1, 
		Nt_MolecularPopulation^ product2, double _rateConst):Nt_Reaction(_rateConst)
	{
			Reactant = reactant;
			Product1 = product1;
			Product2 = product2;
			_reactant = Reactant->ConcPointer;
			_product1 = Product1->ConcPointer;
			_product2 = Product2->ConcPointer;
			array_length = Reactant->Length;
	}

	void Nt_Dissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		_reactant = Reactant->ConcPointer;
		_product1 = Product1->ConcPointer;
		_product2 = Product2->ConcPointer;
		array_length = Reactant->Length;
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
		//if (array_length == 1)
		//{
		//	double tmp = dt * RateConstant * (*_reactant);
		//	*_product1 += tmp;
		//	*_product2 += tmp;
		//	*_reactant -= tmp;
		//	return;
		//}

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
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Reactant->Length;
	}

	void Nt_Transformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Reactant->Length;
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
		//if (array_length == 1)
		//{
		//	double intensity = RateConstant * dt;
		//	*_product += *_reactant * intensity;
		//	*_reactant *= (1.0 - intensity);
		//	return;
		//}
		daxpy(array_length, RateConstant *dt, _reactant, 1, _product, 1);
		dscal(array_length, (1.0 - RateConstant * dt), _reactant, 1);
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
		intensity = gcnew ScalarField(Reactant->Conc->M);
		_reactant = Reactant->ConcPointer;
		_catalyst = Catalyst->ConcPointer;
		array_length = Reactant->Length;
	}

	void Nt_AutocatalyticTransformation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_reactant = Reactant->ConcPointer;
		_catalyst = Catalyst->ConcPointer;
	}

	void Nt_AutocatalyticTransformation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_AutocatalyticTransformation::CloneParent()
	{
		Nt_AutocatalyticTransformation^ rxn = gcnew Nt_AutocatalyticTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->AddReaction(this);
		intensity = gcnew ScalarField(Reactant->Conc->M);
		return rxn;
	}

	void Nt_AutocatalyticTransformation::Step(double dt)
	{
		//this handles the scalar muliplication.
		intensity->reset(Catalyst->Conc)->Multiply(Reactant->Conc)->Multiply(RateConstant * dt);

		double *_intensity = intensity->ArrayPointer;
		daxpy(array_length, 1.0, _intensity, 1, _catalyst, 1);
		daxpy(array_length, -1.0, _intensity, 1, _reactant, 1);
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
		_reactant = Reactant->ConcPointer;
		_catalyst = Catalyst->ConcPointer;
		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
	}

	void Nt_CatalyzedAnnihilation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_reactant = Reactant->ConcPointer;
		_catalyst = Catalyst->ConcPointer;
	}

	void Nt_CatalyzedAnnihilation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedAnnihilation::CloneParent()
	{
		Nt_CatalyzedAnnihilation^ rxn = gcnew Nt_CatalyzedAnnihilation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->intensity = gcnew ScalarField(Reactant->Man);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAnnihilation::Step(double dt)
	{
		intensity->reset(Catalyst->Conc)->Multiply(Reactant->Conc);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant, 1);
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

		_catalyst = Catalyst->ConcPointer;
		_reactant1 = Reactant1->ConcPointer;
		_reactant2 = Reactant2->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Reactant1->Length;
		intensity = gcnew ScalarField(Reactant1->Conc->M);
	}

	void Nt_CatalyzedAssociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant1->Length;
		intensity->darray->resize(array_length);
		_catalyst = Catalyst->ConcPointer;
		_reactant1 = Reactant1->ConcPointer;
		_reactant2 = Reactant2->ConcPointer;
		_product = Product->ConcPointer;
	}

	void Nt_CatalyzedAssociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant1->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedAssociation::CloneParent()
	{
		Nt_CatalyzedAssociation^ rxn = gcnew Nt_CatalyzedAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant1 = Reactant1->parent != nullptr ? Reactant1->parent : Reactant1;
		rxn->Reactant2 = Reactant2->parent != nullptr ? Reactant2->parent : Reactant2;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->intensity = gcnew ScalarField(Reactant1->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedAssociation::Step(double dt)
	{

		//handles the scalar muliplication.
		intensity->reset(Catalyst->Conc)->Multiply(Reactant1->Conc)->Multiply(Reactant2->Conc);

		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant1, 1);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant2, 1);
		daxpy(array_length, dt*RateConstant, intensity->ArrayPointer, 1, _product, 1);
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
		_catalyst = Catalyst->ConcPointer;
		_product = Product->ConcPointer;
		array_length = Product->Length;
	}

	void Nt_CatalyzedCreation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Product->Length;
		_catalyst = Catalyst->ConcPointer;
		_product = Product->ConcPointer;
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
		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	
	}

	void Nt_CatalyzedDimerization::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	}


	void Nt_CatalyzedDimerization::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedDimerization::CloneParent()
	{
		Nt_CatalyzedDimerization^ rxn = gcnew Nt_CatalyzedDimerization(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->intensity = gcnew ScalarField(Reactant->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerization::Step(double dt)
	{
		//handle scalar multiplicaiton
		double* _intensity = intensity->ArrayPointer;
		NtUtility::mem_copy_d(_intensity, _catalyst, array_length);

		//handles scalar muliplication.
		intensity->Multiply(Reactant->Conc)->Multiply(Reactant->Conc);
		daxpy(array_length, dt*RateConstant, _intensity, 1, _product, 1);
		daxpy(array_length, -dt*RateConstant*2, _intensity, 1, _reactant, 1);
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

		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	}

	void Nt_CatalyzedDimerDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);

		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	}

	void Nt_CatalyzedDimerDissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedDimerDissociation::CloneParent()
	{
		Nt_CatalyzedDimerDissociation^ rxn = gcnew Nt_CatalyzedDimerDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->intensity = gcnew ScalarField(rxn->Reactant->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDimerDissociation::Step(double dt)
	{
		intensity->reset(Catalyst->Conc)->Multiply(Reactant->Conc);
		daxpy(array_length, dt*RateConstant*2.0, intensity->ArrayPointer, 1, _product, 1);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant, 1);
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
		
		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product1 = Product1->ConcPointer;
		_product2 = Product2->ConcPointer;
	}

	void Nt_CatalyzedDissociation::AddReaction(Nt_Reaction ^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product1 = Product1->ConcPointer;
		_product2 = Product2->ConcPointer;
	}

	void Nt_CatalyzedDissociation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedDissociation::CloneParent()
	{
		Nt_CatalyzedDissociation^ rxn = gcnew Nt_CatalyzedDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product1 = Product1->parent != nullptr ? Product1->parent : Product1;
		rxn->Product2 = Product2->parent != nullptr ? Product2->parent : Product2;
		rxn->intensity = gcnew ScalarField(rxn->Reactant->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedDissociation::Step(double dt)
	{
		intensity->reset(Catalyst->Conc)->Multiply(Reactant->Conc);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant, 1);
		daxpy(array_length, dt*RateConstant, intensity->ArrayPointer, 1, _product1, 1);
		daxpy(array_length, dt*RateConstant, intensity->ArrayPointer, 1, _product2, 1);
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
		array_length = Reactant->Length;
		intensity = gcnew ScalarField(Reactant->Conc->M);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;


	}

	void Nt_CatalyzedTransformation::AddReaction(Nt_Reaction^ src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
		_catalyst = Catalyst->ConcPointer;
		_reactant = Reactant->ConcPointer;
		_product = Product->ConcPointer;
	}


	void Nt_CatalyzedTransformation::RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Reactant->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_CatalyzedTransformation::CloneParent()
	{
		Nt_CatalyzedTransformation^ rxn = gcnew Nt_CatalyzedTransformation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->Catalyst = Catalyst->parent != nullptr ? Catalyst->parent : Catalyst;
		rxn->Reactant = Reactant->parent != nullptr ? Reactant->parent : Reactant;
		rxn->Product = Product->parent != nullptr ? Product->parent : Product;
		rxn->intensity = gcnew ScalarField(rxn->Reactant->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedTransformation::Step(double dt)
	{
		intensity->reset(Catalyst->Conc)->Multiply(Reactant->Conc);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _reactant, 1);
		daxpy(array_length, dt*RateConstant, intensity->ArrayPointer, 1, _product, 1);
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
		array_length = Product->Length;
		_product = Product->ConcPointer;
		_activation = Gene->activation_pointer();
	}

	void Nt_Transcription::AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Product->Length;
		_product = Product->ConcPointer;
		_activation = Gene->activation_pointer();
		CopyNumber = Gene->CopyNumber;
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
		double factor = RateConstant * CopyNumber * dt;
		int dim = Product->Conc->M->ArraySize;
		daxpy(array_length/dim, factor, _activation, 1, _product, dim);
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

		array_length = Receptor->Length;
		intensity = gcnew ScalarField(Receptor->Conc->M);
		boundaryId = Receptor->Man->Id;

		int pop_id = bulk->Compartment->GetCellPulationId(boundaryId);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[pop_id]->FluxPointer;
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[pop_id]->ConcPointer;
		_bulkActivated_BoundaryFlux = BulkActivated->BoundaryConcAndFlux[pop_id]->FluxPointer;
		_receptor = Receptor->ConcPointer;

	}

	void Nt_CatalyzedBoundaryActivation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_bulkActivated_BoundaryFlux = BulkActivated->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_receptor = Receptor->ConcPointer;
	}

	void Nt_CatalyzedBoundaryActivation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
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
		rxn->intensity = gcnew ScalarField(rxn->Receptor->Conc->M);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_CatalyzedBoundaryActivation::Step(double dt)
	{
		intensity->reset(Receptor->Conc)->Multiply(Bulk->BoundaryConcAndFlux[boundaryId]->Conc);
		daxpy(array_length, RateConstant, intensity->ArrayPointer, 1, _bulk_BoundaryFlux, 1);
		daxpy(array_length, -RateConstant, intensity->ArrayPointer, 1, _bulkActivated_BoundaryFlux, 1);
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
		array_length = Membrane->Length;
		boundaryId = membrane->Man->Id;

		int pop_id = bulk->Compartment->GetCellPulationId(boundaryId);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[pop_id]->ConcPointer;
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[pop_id]->FluxPointer;
		_membraneConc = Membrane->ConcPointer;
	}

	void Nt_BoundaryTransportTo:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Membrane->Length;
		_bulk_BoundaryConc = Bulk->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_membraneConc = Membrane->ConcPointer;
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
		boundaryId = Membrane->Man->Id;
		array_length = Membrane->Length;

		int pop_id = bulk->Compartment->GetCellPulationId(boundaryId);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[pop_id]->FluxPointer;
		_membraneConc = Membrane->ConcPointer;
	}

	void Nt_BoundaryTransportFrom:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Membrane->Length;
		_bulk_BoundaryFlux = Bulk->BoundaryConcAndFlux[boundaryId]->FluxPointer;
		_membraneConc = Membrane->ConcPointer;
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
		array_length = Receptor->Length;
		boundaryId = Complex->Man->Id;
		intensity = gcnew ScalarField(Complex->Man);
		_receptor = Receptor->ConcPointer;	//membrane
		_complex = Complex->ConcPointer;	//membrane

		int pop_id = ligand->Compartment->GetCellPulationId(boundaryId);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[pop_id]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[pop_id]->FluxPointer;
	}

	void Nt_BoundaryAssociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
		_receptor = Receptor->ConcPointer;	//membrane
		_complex = Complex->ConcPointer;	//membrane
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[boundaryId]->FluxPointer;
	}

	void Nt_BoundaryAssociation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
	}

	Nt_Reaction^ Nt_BoundaryAssociation::CloneParent(int popid)
	{
		Nt_BoundaryAssociation^ rxn = gcnew Nt_BoundaryAssociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = popid;
		rxn->Complex = Complex->parent != nullptr ? Complex->parent : Complex;
		rxn->Ligand = Ligand->parent != nullptr ? Ligand->parent : Ligand;
		rxn->Receptor = Receptor->parent != nullptr ? Receptor->parent : Receptor;
		rxn->intensity = gcnew ScalarField(rxn->Complex->Man);
		rxn->AddReaction(this);
		return rxn;
	}

	void Nt_BoundaryAssociation::Step(double dt)
	{
		//not the intensity has the correct manifold informaiton but it may not have correct
		//information about component, since the reset does not copy that info and the info is also
		//not needed for the computation.
		intensity->reset(Receptor->Conc)->Multiply(Ligand->BoundaryConcAndFlux[boundaryId]->Conc);
		daxpy(array_length, RateConstant, intensity->ArrayPointer, 1, _ligand_BoundaryFlux, 1);
		daxpy(array_length, -dt*RateConstant, intensity->ArrayPointer, 1, _receptor, 1);
		daxpy(array_length, dt*RateConstant, intensity->ArrayPointer, 1, _complex, 1);
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
		intensity = gcnew ScalarField(Complex->Man);
		array_length = Receptor->Length;
		_receptor = Receptor->ConcPointer;	//membrane
		_complex = Complex->ConcPointer;	//membrane

		boundaryId = Complex->Man->Id;
		int pop_id = ligand->Compartment->GetCellPulationId(boundaryId);
		if (pop_id == -1)
		{
			throw gcnew Exception("unknown boundary population id");
		}
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[pop_id]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[pop_id]->FluxPointer;
	}

	void Nt_BoundaryDissociation:: AddReaction(Nt_Reaction ^src_rxn)
	{
		ComponentReactions->Add(src_rxn);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
		_receptor = Receptor->ConcPointer;	//membrane
		_complex = Complex->ConcPointer;		//membrane
		_ligand_BoundaryConc = Ligand->BoundaryConcAndFlux[boundaryId]->ConcPointer;
		_ligand_BoundaryFlux = Ligand->BoundaryConcAndFlux[boundaryId]->FluxPointer;
	}


	void Nt_BoundaryDissociation:: RemoveReaction(int index)
	{
		ComponentReactions->RemoveAt(index);
		array_length = Receptor->Length;
		intensity->darray->resize(array_length);
	}


	Nt_Reaction^ Nt_BoundaryDissociation::CloneParent(int boundary_id)
	{
		Nt_BoundaryDissociation^ rxn = gcnew Nt_BoundaryDissociation(this->RateConstant);
		rxn->ComponentReactions = gcnew List<Nt_Reaction ^>();
		rxn->boundaryId = boundary_id;
		rxn->Complex = Complex->parent != nullptr ? Complex->parent : Complex;
		rxn->Ligand = Ligand->parent != nullptr ? Ligand->parent : Ligand;
		rxn->Receptor = Receptor->parent != nullptr ? Receptor->parent : Receptor;
		rxn->intensity = gcnew ScalarField(rxn->Complex->Man);
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