#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Gene
	{

	internal:
		Nt_Gene^ parent;

	private:
		double activationLevel;
	public:
		property String^ Name; 
		property int CopyNumber;
		property double ActivationLevel
		{
			double get()
			{
				return activationLevel;
			}

			void set(double value)
			{
				activationLevel = value;
				if (_activation != NULL)
				{
					*_activation = value;
				}
			}
		}

		Nt_Gene(String^ name, int copyNumber, double actLevel)
		{
			Name = name;
			CopyNumber = copyNumber;
			ActivationLevel = actLevel;
			_activation = NULL;
			cellId = -1;
			allocedItemCount = 0;
		}

		int cellId;
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
				allocedItemCount = NtUtility::GetAllocSize(itemCount+1, allocedItemCount);
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
			gene->parent = this;
			ComponentGenes->Add(gene);
			cellIds->Add(gene->cellId);
			double *tptr = _activation + itemCount;
			*tptr = gene->ActivationLevel;
			gene->_activation = tptr;
		}

		void RemoveGene(int index)
		{
			int itemCount = ComponentGenes->Count;
			if (index < 0 || index >= itemCount)
			{
				throw gcnew Exception("Error RemoveGene: index out of range");
			}
			Nt_Gene^ target = ComponentGenes[index];
			Nt_Gene^ last_gene = ComponentGenes[itemCount-1];
			if (index != itemCount-1)
			{
				//swap with last item
				double *act_ptr = last_gene->_activation;
				last_gene->_activation = target->_activation;
				*(last_gene->_activation) = last_gene->activationLevel;
				target->_activation = act_ptr;
				*(target->_activation) = target->activationLevel;
				ComponentGenes[index] = last_gene;
				ComponentGenes[itemCount-1] = target;
				cellIds[index] = cellIds[itemCount-1];
			}

			target->_activation = NULL;
			ComponentGenes->RemoveAt(itemCount-1);
			//cellIds is not really needed, keep for debugging for now
			cellIds->RemoveAt(itemCount-1);
		}

		Nt_Gene ^CloneParent()
		{
			Nt_Gene^ gene = gcnew Nt_Gene(cellId, Name, CopyNumber, 0);
			gene->ComponentGenes = gcnew List<Nt_Gene^>();
			gene->cellIds = gcnew List<int>();
			gene->AddGene(this);
			return gene;
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