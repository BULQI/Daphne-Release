#pragma once

#include "Nt_Utility.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Gene
	{
	public:
		String^ Name; 
		int CopyNumber;
		int cellId;
		double ActivationLevel;

		List<Nt_Gene^> ^ComponentGenes;
		List<int> ^cellIds;

		Nt_Gene(int _cellId, String ^_name, int _cpnum, double _act_level)
		{
			Name = _name;
			CopyNumber = _cpnum;
			ActivationLevel = _act_level;
			_activation = NULL;
			cellId = _cellId;
			allocedItemCount = 0;
		}

		void AddGene(Nt_Gene^ gene)
		{
			int itemCount = ComponentGenes->Count;
			if (itemCount+1 > allocedItemCount)
			{
				allocedItemCount = Nt_Utility::GetAllocSize(itemCount+1, allocedItemCount);
				//activation is one number per gene, not 4 elements like concentration
				int allocSize = allocedItemCount * sizeof(double);
				_activation = (double *)realloc(_activation, allocSize);

				//update unamanged pointers for components
				double *act_ptr = _activation;
				for (int i=0; i< ComponentGenes->Count; i++, act_ptr++)
				{
					ComponentGenes[i]->_activation = act_ptr;
					*act_ptr = ComponentGenes[i]->ActivationLevel;
				}
			}
			ComponentGenes->Add(gene);
			cellIds->Add(gene->cellId);
			double *tptr = _activation + itemCount;
			*tptr = gene->ActivationLevel;
			gene->_activation = tptr;
		}

		Nt_Gene ^CloneParent()
		{
			Nt_Gene^ gene = gcnew Nt_Gene(cellId, Name, CopyNumber, 0);
			gene->ComponentGenes = gcnew List<Nt_Gene^>();
			gene->cellIds = gcnew List<int>();
			gene->AddGene(this);
			return gene;
		}

		//void initialize()
		//{
		//	int itemCount = ComponentGenes->Count;
		//	if (itemCount > allocedItemCount)
		//	{
		//		if (allocedItemCount > 0)
		//		{
		//			free(_activation);
		//		}
		//		allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
		//		//activation is one number per gene, not 4 elements like concentration
		//		int allocSize = allocedItemCount * sizeof(double);
		//		_activation = (double *)malloc(allocSize);

		//		//update unamanged pointers for components
		//		double *act_ptr = _activation;
		//		for (int i=0; i< ComponentGenes->Count; i++, act_ptr++)
		//		{
		//			ComponentGenes[i]->_activation = act_ptr;
		//			*act_ptr = ComponentGenes[i]->ActivationLevel;
		//		}
		//	}
		//	initialized = true;
		//}

		//getter for pinter
		double* activation_pointer()
		{
			return _activation;
		}

		//interface for setting activation level from managed side.
		void SetActivationLevel(double level)
		{
			this->ActivationLevel = level;
			if (_activation != NULL)
			{
				*_activation = level;
			}
		}

	private:
		double *_activation;
		int allocedItemCount;
	};
}