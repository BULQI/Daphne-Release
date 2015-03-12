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
		double ActivationLevel;
		bool initialized;

		List<Nt_Gene^> ^ComponentGenes;

		Nt_Gene(String ^_name, int _cpnum, double _act_level)
		{
			Name = _name;
			CopyNumber = _cpnum;
			ActivationLevel = _act_level;
			initialized = false;
			_activation = NULL;
			allocedItemCount = 0;
		}

		void AddGene(Nt_Gene^ gene)
		{
			ComponentGenes->Add(gene);
			initialized = false;
		}

		Nt_Gene ^CloneParent()
		{
			Nt_Gene^ gene = gcnew Nt_Gene(Name, CopyNumber, 0);
			gene->ComponentGenes = gcnew List<Nt_Gene^>();
			gene->ComponentGenes->Add(this);
			return gene;
		}

		void initialize()
		{
			int itemCount = ComponentGenes->Count;
			if (itemCount > allocedItemCount)
			{
				if (allocedItemCount > 0)
				{
					free(_activation);
				}
				allocedItemCount = Nt_Utility::GetAllocSize(itemCount, allocedItemCount);
				//activation is one number per gene, not 4 elements like concentration
				int allocSize = allocedItemCount * sizeof(double);
				_activation = (double *)malloc(allocSize);

				//update unamanged pointers for components
				double *act_ptr = _activation;
				for (int i=0; i< ComponentGenes->Count; i++, act_ptr++)
				{
					ComponentGenes[i]->_activation = act_ptr;
					*act_ptr = ComponentGenes[i]->ActivationLevel;
				}
			}
			initialized = true;
		}

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