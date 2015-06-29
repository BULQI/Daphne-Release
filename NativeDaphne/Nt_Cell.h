#pragma once

#include <errno.h>
#include "NtUtility.h"
#include "Nt_NormalDist.h"
#include "Nt_MolecularPopulation.h"
#include "Nt_DArray.h"
//#include "NtCellPair.h"
#include "Nt_Gene.h"
#include "Nt_Compartment.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellSpatialState
	{
	public:
		Nt_Darray^ X;
		Nt_Darray^ V;				
		Nt_Darray^ F;

		static int SingleDim = 3;
		static int Dim = 9;

		Nt_CellSpatialState()
		{
			_X = _V = _F = NULL;
		}

		Nt_CellSpatialState(Nt_Darray^ x, Nt_Darray^ v, Nt_Darray^ f)
		{
			X = x;
			V = v;
			F = f;
			_X = _V = _F = NULL;
		}

	internal:
		double *_X;
		double *_V;
		double *_F;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Cell
	{
	protected:
		/// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
		bool alive;

		/// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        bool cytokinetic;

		/// <summary>
        /// a flag that signals that the cell is motile
        /// </summary>
        bool isMotile;

		/// <summary>
        /// a flag that signals that the cell responds to chemokine gradients
        /// </summary>
        bool isChemotactic;

        /// <summary>
        /// a flag that signals that the cell is subject to stochastic forces
        /// </summary>
        bool isStochastic;

		/// <summary>
        /// A flag that signals to the cell manager whether the cell is exiting the simulation space.
        /// </summary>
        bool exiting;

		/// <summary>
        /// The radius of the cell
        /// </summary>
        double radius;

		Nt_CellSpatialState ^spatialState;

		Dictionary<String^, Nt_Gene^>^ genes;

		property Nt_Compartment^ BaseCytosol
		{
			Nt_Compartment^ get()
			{
				return cytosol;
			}
			void set(Nt_Compartment^ value)
			{
				cytosol = value;
			}
		}

		property Nt_Compartment^ BasePlasmaMembrane
		{
			Nt_Compartment^ get()
			{
				return plasmaMembrane;
			}
			void set(Nt_Compartment^ value)
			{
				plasmaMembrane = value;
			}
		}

	internal:
		int *gridIndex;
		int* PreviousGridIndex;
		//to be remvoed
		int Membrane_id;

		Nt_Compartment^ cytosol;
		Nt_Compartment^ plasmaMembrane;


	public:
		int Cell_id;
		static int SafeCell_id = 0;
		static double defaultRadius = 5.0;
		property int Population_id;

		//Nt_Compartment^ cytosol;
		//Nt_Compartment^ plasmaMembrane;


		property bool IsMotile
		{
			bool get(){ return isMotile;}
			void set(bool value){ isMotile = value;}
		}

		property bool IsChemotactic
		{
			bool get(){ return isChemotactic;}
			void set(bool value){ isChemotactic = value;}
		}

		property bool IsStochastic
		{
			bool get(){ return isStochastic;}
			void set(bool value){ isStochastic = value;}
		}

		property bool Alive
		{
			bool get(){ return alive;}
			void set(bool value){ alive = value;}
		}

		property bool Cytokinetic
		{
			bool get(){ return cytokinetic;}
			void set(bool value){ cytokinetic = value;}
		}

		property bool Exiting
		{
			bool get(){ return exiting;}
			void set(bool value){ exiting = value;}
		}

		property double Radius
		{
			double get()
			{
				return radius;
			}
		}

		property Nt_CellSpatialState^ SpatialState
        {
			Nt_CellSpatialState^ get()
			{
				return spatialState;
				
			}

			void set(Nt_CellSpatialState^ value)
			{
				spatialState = value;
			}
        }

		property Nt_Iarray^ GridIndex;


		property Dictionary<String^, Nt_Gene^>^ Genes
		{
			Dictionary<String^, Nt_Gene^>^ get()
			{
				return genes;
			}
			void set(Dictionary<String^, Nt_Gene^>^ g)
			{
				genes = g;
			}
		}

		void AddGene(String^ gene_guid, Nt_Gene^ gene)
		{
			genes->Add(gene_guid, gene);
		}


		Nt_MolecularPopulation^ Driver; //point cells->cytosol->puplulaiton[driver-key]
		double TransductionConstant;
		double DragCoefficient;
		double Sigma;

		static int count = 0;

		List<Nt_Cell^>^ ComponentCells;

		//NtCell *nt_cell;

		Nt_Cell(double r)
		{	
			radius = r;
			//cellIds = gcnew List<int>();
			//only need 3, but 4 needed when using ssc2
			GridIndex = gcnew Nt_Iarray(3);
			GridIndex[0] = -1;
			GridIndex[1] = -1;
			GridIndex[2] = -1;
			gridIndex = (int*)malloc(4 *sizeof(int));
			GridIndex->NativePointer = gridIndex;
			PreviousGridIndex = (int*)malloc(4 *sizeof(int));
			PreviousGridIndex[0] = -1; //evaludate into invalid
			//nt_cell = new NtCell(radius, gridIndex);
		}

		~Nt_Cell()
		{
			this->!Nt_Cell();
		}

		!Nt_Cell()
		{
			free(gridIndex);
			//delete nt_cell;

		}

		void addForce(array<double>^ f)
        {
            spatialState->F[0] += f[0];
            spatialState->F[1] += f[1];
            spatialState->F[2] += f[2];
        }

	};
}