#pragma once


#include <cmath>
#include "Utility.h"
#include "Nt_NormalDist.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	public enum class Nt_ReactionType {Annihilation, Association, AutocatalyticTransformation, BoundaryAssociation,
		BoundaryDissociation, BoundaryTransportFrom, BoundaryTransportTo, CatalyzedAnnihilation, CatalyzedAssociation,
		CatalyzedBoundaryActivation, CatalyzedCreation, CatalyzedDimerDissociation, CatalyzedDimerization,
		CatalyzedDissociation, CatalyzedTransformation, DimerDissociation, Dimerization, Dissociation, GeneralizedReaction,
		Transcription, Transformation};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Reaction
	{
	public:
	
		Nt_ReactionType reactionType;

		Nt_Reaction();

		Nt_Reaction(Nt_ReactionType type, int cell_id, double rate_const);

		Dictionary<int, bool> ^cellIdDictionary;

		List<int>^ cellIds;
		double rateConstant;

		//tell if the arrays have been allocated or not.
		bool initialized;

	    virtual void AddReaction(Nt_Reaction ^ rxn);

		virtual void step(double dt);

		virtual void initialize();

	private:
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transformation: public Nt_Reaction
	{

	public:
		Nt_Transformation();

		Nt_Transformation(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual void initialize() override;
		virtual void step(double dt) override;

		List<array<double>^>^ reactant;
		List<array<double>^>^ product;
	private:
		double *_reactant;
		double *_product;
		
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Transcription: public Nt_Reaction
	{
	public:
		Nt_Transcription(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual void initialize() override;
		virtual void step(double dt) override;

		//if these numbers change over iteration
		//we will need to update for every iteration
		//then the values will be different and we cannot groups them
		//together. 
		//check if theese values change, will will have to leave these
		//to the managed side to handle.

		List<int>^ CopyNumber;
		List<array<double>^>^ ActivationLevel;	
		List<array<double>^>^ product;
		List<double> ^activationLevelSave;
	private:
		bool CopyNumberIdentical;
		bool ActivationLevelIdentical;

		double *_product;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CatalyzedBoundaryActivation : public Nt_Reaction
	{
	public:
		Nt_CatalyzedBoundaryActivation(int cell_id, double rate_const);

		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual void initialize() override;

		virtual void step(double dt) override;

		List<array<double>^>^ receptor;
		List<array<double>^>^ bulkBoundaryConc;
		List<array<double>^>^ bulkBoundaryFluxes;
		List<array<double>^>^ bulkActivatedBoundaryFluxes;
	private:
		double *_receptor;
		double *_bulkBoundaryConc;
		double *_bulkBoundaryFluxes;
		double *_bulkActivatedBoundaryFluxes;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Annihilation : public Nt_Reaction
	{
	public:
		Nt_Annihilation(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;

		virtual void initialize() override;
		virtual void step(double dt) override;

		List<array<double>^>^ reactant;
	private:
		double *_reactant;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryTransportTo : public Nt_Reaction
	{
	public:
		Nt_BoundaryTransportTo(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void initialize() override;
		virtual void step(double dt) override;

		List<array<double>^>^ BulkBoundaryConc;
		List<array<double>^>^ BulkBoundaryFluxes;
		List<array<double>^>^ MembraneConc;
	private:
		double *_bulkBoundaryConc;
		double *_bulkBoundaryFluxes;
		double *_membraneConc;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_BoundaryTransportFrom : public Nt_Reaction
	{
	public:
		Nt_BoundaryTransportFrom(int cell_id, double rate_const);
		virtual void AddReaction(Nt_Reaction^ rxn) override;
		virtual void initialize() override;
		virtual void step(double dt) override;

		List<array<double>^>^ BulkBoundaryFluxes;
		List<array<double>^>^ MembraneConc;
	private:
		double *_bulkBoundaryFluxes;
		double *_membraneConc;

	};

   
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MolecularPopulation
	{
	public:
		double DiffusionCoefficient;
		List<array<double>^>^ molpopConc;

		Nt_MolecularPopulation(double diff_coeff, array<double> ^conc);

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		virtual void step(double dt);
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CytosolMolecularPopulation : public Nt_MolecularPopulation
	{
	public:
		int cellId;
		double CellRadius;
		bool initialized;

		List<int> ^cellIds;
		List<array<double>^>^ boundaryFluxes;
		List<array<double>^> ^boundaryConc;

		Nt_CytosolMolecularPopulation(int _cellId, double _cellRadius, double _diffusionCoefficient, array<double> ^conc, array<double> ^bflux, array<double> ^bconc);

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;
		void initialize();
		virtual void step(double dt) override;


	private:
		double *_molpopConc;
		double *_laplacian;
		double *_boundaryFluxes;
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_MembraneMolecularPopulation : public Nt_MolecularPopulation
	{
	public:
		int cellId;
		double CellRadius;
		bool initialized;

		List<int> ^cellIds;

		Nt_MembraneMolecularPopulation(int _cellId, double _cellRadius, double _diffusionCoefficient, array<double> ^conc);

		virtual void AddMolecularPopulation(Nt_MolecularPopulation^ molpop) override;
		virtual void step(double dt) override;
		void initialize();

	private:
		double *_molpopConc;
		double *_laplacian;
	};


	//holding info needed to passed in, for now only for motile cells
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Cell
	{
	//these are all set as public for testing, change
	//later when necessary.
	public:
		int Cell_id;

		//figure out ways when these change, then rmove the cells from the collection
		bool alive;
		bool exiting;

        bool cytokinetic;
		bool isMotile;
        bool isChemotactic;
		bool isStochastic;


		double radius;
		double TransductionConsant;
		double DragCoefficient;
		double Sigma;


		array<double> ^X;
		array<double> ^V;
		array<double> ^F;
		array<double> ^driverConc;

		Nt_Cell(){}

		Nt_Cell(int cid, double r, array<double> ^x, array<double> ^v, array<double> ^f)
		{
			Cell_id = cid;
			radius = r;
			X = x;
			V = v;
			F = f;
		}
	};


	/*this class trys to store cells property together so we
	  can manipulate them in vectors
	  note: this is needed only for motile, alive and not existing cells.
    */
	[SuppressUnmanagedCodeSecurity]
	public ref class CellStateCollection
	{
	private:
		//bool IsTransductionConstIdentical;
		/*bool IsSigmaIdentical;
		bool IsRadiusIdentivcal;
		bool IsDragCoefficientIdentical;*/

		//for chemotaxis
		double *_driver_gradient;
		double *_transductionConstant;
		//for Stochastic
		double *_sigma;
		double *_stochForce;
		double *_samples;

		//all
		double *_X;
		double *_V;
		double *_F;
		bool Initialized;
		int allocatedItemCount;

	public:
		//these list may overlap
		List<int> ^cellIds;
		List<double>^ RadiusList;
		List<array<double>^>^ XList;
		List<array<double>^>^ VList;				
		List<array<double>^>^ FList;	

		//for chemotaxis
		List<array<double>^>^ DriverConcList; //driver concentration
		List<double> ^TransductionConstList;
		List<double> ^DragCoefficientList;
		List<double> ^SigmaList;

		//for boundaryFource, these are "global variables" need to be set
		bool boundaryForceFlag; 
		static double PairPhi1;
		static array<double> ^EnvironmentExtent;

		bool IsTransductionConstIdentical;
		bool IsSigmaIdentical;
		bool IsCellRadiusIdentical;
		bool IsDragCoefficientIdentical;

		CellStateCollection()
		{
			cellIds = gcnew List<int>();
			RadiusList = gcnew List<double>();
			XList = gcnew List<array<double>^>();
			VList = gcnew List<array<double>^>();
			FList = gcnew List<array<double>^>();

			DriverConcList = gcnew List<array<double>^>();
			TransductionConstList = gcnew List<double>();
			DragCoefficientList = gcnew List<double>();
			SigmaList = gcnew List<double>();

			IsTransductionConstIdentical = true;
			IsSigmaIdentical = true;
			IsCellRadiusIdentical = true;
			IsDragCoefficientIdentical = true;
			Initialized = false;
			allocatedItemCount = 0;
		}

		~CellStateCollection()
		{
			this->!CellStateCollection();
		}

		!CellStateCollection()
		{
			if (allocatedItemCount > 0)
			{
				free(_driver_gradient);
				free(_transductionConstant);
				free(_sigma);
				free(_stochForce);
				free(_X);
				free(_V);
				free(_F);
			}
		}

		void initialize()
		{
			if (allocatedItemCount >= cellIds->Count)
			{
				Initialized = true;
				return;
			}
			if (allocatedItemCount > 0)
			{
				free(_driver_gradient);
				free(_transductionConstant);
				free(_sigma);
				free(_stochForce);
				free(_X);
				free(_V);
				free(_F);
				free(_samples);
			}
			//in 64 increment
			allocatedItemCount = cellIds->Count + 64;

			_driver_gradient = (double *)malloc(allocatedItemCount * 3 * sizeof(double));
			_samples = (double *)malloc(allocatedItemCount * 3 * sizeof(double));
			_transductionConstant = (double *)malloc(allocatedItemCount * sizeof(double));
			_X = (double *)malloc(allocatedItemCount * 3 * sizeof(double));
			_V = (double *)malloc(allocatedItemCount * 3 * sizeof(double));
			_F = (double *)malloc(allocatedItemCount * 3 * sizeof(double));
			Initialized = true;
		}

		void step(double dt);

		void AddCell(Nt_Cell ^cell)
		{
			cellIds->Add(cell->Cell_id);

			XList->Add(cell->X);
			VList->Add(cell->V);
			FList->Add(cell->F);
			DriverConcList->Add(cell->driverConc);

			int itemCount = cellIds->Count-1;
			if (itemCount > 0 && cell->radius != RadiusList[0])
			{
				IsCellRadiusIdentical = false;
			}
			RadiusList->Add(cell->radius);

			if (itemCount > 0 && cell->TransductionConsant != TransductionConstList[0])
			{
				IsTransductionConstIdentical = false;
			}

			TransductionConstList->Add(cell->TransductionConsant);

			if (itemCount > 0 && cell->DragCoefficient != DragCoefficientList[0])
			{
				IsDragCoefficientIdentical = false;
			}
			DragCoefficientList->Add(cell->DragCoefficient);


			if (itemCount > 0 && cell->Sigma != SigmaList[0])
			{
				IsSigmaIdentical = false;
			}
			SigmaList->Add(cell->Sigma);
		}

		void RemoveCell(int cell_id)
		{
			//we may want to use linkedlist for fast removal
			int index = cellIds->IndexOf(cell_id);
			if (index == -1)return;

			cellIds->RemoveAt(index);
			XList->RemoveAt(index);
			VList->RemoveAt(index);
			FList->RemoveAt(index);
			DriverConcList->RemoveAt(index);
			RadiusList->RemoveAt(index);
			TransductionConstList->RemoveAt(index);
			DragCoefficientList->RemoveAt(index);
			SigmaList->RemoveAt(index);;
		}
	};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellManager
	{
	
	public:
		/// <summary>
		/// create a new instance of Nt_CellManger
		/// </summary>
		/// <returns>none</returns>
		Nt_CellManager(void);

		~Nt_CellManager(void);

		/// <summary>
		/// Add an reaction to be handled by the Nt_CellManager instance.
		/// </summary
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>
		void AddReaction(Nt_Reaction^ reaction);

		void AddMolecularPopulation(Nt_MolecularPopulation^ molpop);

		void AddCell(Nt_Cell ^cell)
		{
			cellStates->AddCell(cell);
		}

		/// <summary>
		/// remove all reactions
		/// </summary>
		/// <param name="reaction">an instance of Nt_Reaction</param>
		/// <returns>void</returns>	
		void Clear();

		void step(double dt);

		void InitializeNormalDistributionSampler(double mean, double variance, int seed)
		{
			//there seems t be a bug in the native initializer...
			//if (IsDistributionSamplerInitialized == true)return;
			normalDist = gcnew Nt_NormalDistribution();
			normalDist->initialize(mean, variance, seed);
			IsDistributionSamplerInitialized = true;
		}

		void SetEnvironmentExtents(double extent0, double extent1, double extent2, bool do_boundary_force, double pair_phi1 )
		{
			EnvironmentExtent[0] = extent0;
			EnvironmentExtent[1] = extent1;
			EnvironmentExtent[2] = extent2;
			boundaryForceFlag = do_boundary_force;
			PairPhi1 = pair_phi1;
			IsEnvironmentInitialzed = true;
		}

		bool IsInitialized()
		{
			return IsEnvironmentInitialzed && IsDistributionSamplerInitialized;
		}


		static Nt_NormalDistribution ^normalDist;

		static array<double> ^EnvironmentExtent;

		//flag indicating if ECS and toroidal = false
		static bool boundaryForceFlag;

		//from Pair.Phil1 name of the parameter?
		static double PairPhi1;

	private:

		Dictionary<int, Nt_Cell^> ^cellIdDictionary;

		//the reactions are of cytosol for now.
		//do we have reactions for membrane?
		List<Nt_Reaction^>^ reactionList;

		List<Nt_MolecularPopulation^> ^molpopList;

		//data 
		CellStateCollection ^cellStates;

		bool IsEnvironmentInitialzed;
		bool IsDistributionSamplerInitialized;

		

	};
}
