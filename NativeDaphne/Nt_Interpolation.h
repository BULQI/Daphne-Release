#pragma once

#include "Nt_ManifoldUtilities.h"
#include "NtInterpolation.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace System::Linq;
using namespace System::Text;
using namespace MathNet::Numerics::LinearAlgebra::Double;
using namespace NativeDaphneLibrary;

namespace Nt_ManifoldRing
{
	ref class ScalarField;
	ref class Manifold;
	ref class InterpolatedNodes;

    /// <summary>
    /// interpolator interface
    /// </summary>
	[SuppressUnmanagedCodeSecurity]
    public interface class Interpolator
    {
        void Init(InterpolatedNodes^ m, bool toroidal);
        double Interpolate(array<double>^ x, ScalarField^ sf);
        double Integration(ScalarField^ sf);
        array<double>^ Gradient(array<double>^ x, ScalarField^ sf);
        ScalarField^ Laplacian(ScalarField^ sf);
        ScalarField^ DiffusionFlux(ScalarField^ flux, Transform^ t, ScalarField^ dst, double dt);
        ScalarField^ DirichletBC(ScalarField^ from, Transform^ t, ScalarField^ to);
    };

	public ref class NodeInterpolator abstract : Interpolator
    {
	protected:
        bool toroidal;
        /// <summary>
        /// Return the sparse matrix with coefficients and indices for interpolation of scalar field at arbitrary position x.
        /// </summary>
        /// <param name="x">Spatial point for interpolation</param>
        /// <returns></returns>
		InterpolatedNodes^ m;
        // Used to create the value, gradient, and laplacian operators
        // We may be able to change access to 'protected' if Convert() is moved out of ScalarField
		virtual array<LocalMatrix>^ interpolationMatrix(array<double>^ x) abstract;
        /// <summary>
        /// Calculate sparse matrix for computing Laplacian at every grid point.
        /// Only computed once.
        /// Impose toroidal or zero flux BCs.
        /// Dirichlet or Neumann BCs will be imposed later, as needed.
        /// </summary>
        /// <returns></returns>
		virtual array<array<LocalMatrix>^>^ laplacianMatrix() abstract;
        /// <summary>
        /// Calculate sparse matrix for computing the gradient at arbitrary position x.
        /// </summary>
        /// <param name="x">Spatial point for gradient calculation</param>
        /// <returns></returns>
		virtual array<array<LocalMatrix>^>^ gradientMatrix(array<double>^ x) abstract;

        // Used to compute value, gradient, and laplacian
		array<LocalMatrix>^ interpolationOperator;
		array<array<LocalMatrix>^>^ gradientOperator;
		array<array<LocalMatrix>^>^ laplacianOperator;

        // computed gradient (at a point) and laplacian
		array<double>^ gradient;
		ScalarField^ laplacian;

	public:
		virtual double Integration(ScalarField^ sf) abstract;

		NodeInterpolator()
        {
        }

		virtual void Init(InterpolatedNodes^ m, bool _toroidal);


        virtual double Interpolate(array<double>^ x, ScalarField^ sf);

        virtual array<double>^ Gradient(array<double>^ x, ScalarField^ sf);

		virtual ScalarField^ Laplacian(ScalarField^ sf);

        /// <summary>
        /// Return a scalar field (in the volume) representing the applied flux at a surface.
        /// Uses interpolation to distribute the flux source to surrounding nodes.
        /// NOTES: 
        ///     This may not be well suited for flux from interpolated-node fields, like natural boundaries 
        ///     Doesn't make use of rotation information in Transform, 
        ///     so interpolated-node surface normals are assumed to be coincident with volume axes.
        /// </summary>
        /// <param name="flux">Flux from the surface element of the volume</param>
        /// <param name="t">Transform information about embedding of surface in volume </param>
        /// <returns></returns>
		virtual ScalarField^ DiffusionFlux(ScalarField^ flux, Transform^ t, ScalarField^ dst, double dt);

         /// <summary>
        /// Return a scalar field (in the volume) representing the applied flux at a surface.
        /// Assigns the entire flux source to the closest node.
        /// NOTES: 
        ///     Doesn't make use of rotation information in Transform, 
        ///     so interpolated-node surface normals are assumed to be coincident with volume axes.
        /// </summary>
        /// <param name="flux">Flux from the surface element of the volume</param>
        /// <param name="t">Transform information about the location and orientation of surface in volume </param>
        /// <returns></returns>       
		ScalarField^ DiffusionFlux_1Pt(ScalarField^ flux, Transform^ t);

        /// <summary>
        /// Impose Dirichlet boundary conditions
        /// NOTE: This algorithm is best when there is a one-to-one correspondance between 
        /// boundary and interior manifold principal points (nodes). May not be as accurate
        /// when there is not a one-to-one correspondance.
        /// </summary>
        /// <param name="from">Field specified on the boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <param name="to">Field specified on the interior manifold</param>
        /// <returns>The field after imposing Dirichlet boundary conditions</returns>
		virtual ScalarField^ DirichletBC(ScalarField^ from, Transform^ t, ScalarField^ sf);

		//get toroidal property
		bool isToroidal()
		{
			return toroidal;
		}
    };

    /// <summary>
    /// Trilinear 3D interpolation
    /// </summary>
    public ref class Trilinear3D : NodeInterpolator
    {
        //added to improve performce
        int NodePerSide0;
        int NodePerSide1;
        int NodePerSide2;
        //used to speed up compuation
        array<array<LocalMatrix>^>^ gradientOperator16;
        array<array<LocalMatrix>^>^ gradientOperator20;
        array<array<LocalMatrix>^>^ gradientOperatorJagged;
        array<int>^ idxarr;

		//for unmanaged code
		NtTrilinear3D *NtInstance;


	public:
		Trilinear3D() : NodeInterpolator()
        {
            interpolationOperator = gcnew array<LocalMatrix>(8);
			NtInstance = NULL;
        }

		~Trilinear3D()
		{
			this->!Trilinear3D();
		}

		!Trilinear3D()
		{
			if (NtInstance != NULL)delete NtInstance;
		}

		virtual void Init(InterpolatedNodes^ m, bool _toroidal) override;

	protected:
        // Don't need to account for toroidal BCs with this low-order scheme. 
		virtual array<LocalMatrix>^ interpolationMatrix(array<double>^ x) override;

		array<LocalMatrix>^ interpolationMatrix_original(array<double>^ x);

        // NOTES:
        // Gradient operators indices could be created and stored once when the NodeInterpolator is instantiated, similar to Laplacian. 
		virtual array<array<LocalMatrix>^>^ gradientMatrix(array<double>^ x) override;

		array<array<LocalMatrix>^>^ gradientMatrix_original(array<double>^ x);

		virtual array<array<LocalMatrix>^>^ laplacianMatrix() override;
		
	public:
		virtual double Integration(ScalarField^ sf) override;

		virtual ScalarField^ Laplacian(ScalarField^ sf) override;
    };

    /// <summary>
    /// Trilinear 2D interpolation
    /// </summary>
    public ref class Trilinear2D : NodeInterpolator
    {

	public:
		Trilinear2D(): NodeInterpolator()
        {
            interpolationOperator = gcnew array<LocalMatrix>(4);
        }

		virtual void Init(InterpolatedNodes^ m, bool _toroidal) override;

	protected:
        // Don't need to account for toroidal BCs with this low-order scheme. 
		virtual array<LocalMatrix>^ interpolationMatrix(array<double>^ x) override;
        
        // NOTES:
        // Gradient operators indices could be created and stored once when the NodeInterpolator is instantiated, similar to Laplacian. 
		virtual array<array<LocalMatrix>^>^ gradientMatrix(array<double>^ x) override;
        
		virtual array<array<LocalMatrix>^>^ laplacianMatrix() override;
		
	public:
		virtual double Integration(ScalarField^ sf) override;
    };


    /// <summary>
    /// Tricubic 3D interpolation.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public ref class Tricubic3D : NodeInterpolator
    {
	public:
		Tricubic3D()
            : NodeInterpolator()
        {
            throw gcnew NotImplementedException();
            // interpolator = gcnew LocalMatrix[27];
        }

		virtual void Init(InterpolatedNodes^ m, bool _toroidal) override
        {
            NodeInterpolator::Init(m, _toroidal);
        }

		virtual double Integration(ScalarField^ sf) override
        {
            throw gcnew NotImplementedException();
        }

	protected:

		virtual array<LocalMatrix>^ interpolationMatrix(array<double>^ x) override
        {
            throw gcnew NotImplementedException();
            //return interpolator;
        }

		virtual array<array<LocalMatrix>^>^ gradientMatrix(array<double>^ x) override
        {
            throw gcnew NotImplementedException();
            //return gradient;
        }

		virtual array<array<LocalMatrix>^>^ laplacianMatrix() override
        {
            throw gcnew NotImplementedException();
            // return laplacian;
        }
    };
    
    /// <summary>
    /// Tricubic 2D interpolation.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public ref class Tricubic2D : NodeInterpolator
    {
	public:
		Tricubic2D()
            : NodeInterpolator()
        {
            throw gcnew NotImplementedException();
            // interpolator = gcnew LocalMatrix[27];
        }

		virtual void Init(InterpolatedNodes^ m, bool _toroidal) override
        {
            NodeInterpolator::Init(m, _toroidal);
        }

	public:
		virtual double Integration(ScalarField^ sf) override
        {
            throw gcnew NotImplementedException();
        }

	protected:
		virtual array<LocalMatrix>^ interpolationMatrix(array<double>^ x) override
        {
            throw gcnew NotImplementedException();
            //return interpolator;
        }

		virtual array<array<LocalMatrix>^>^ gradientMatrix(array<double>^ x) override
        {
            throw gcnew NotImplementedException();
            //return gradient;
        }

		virtual array<array<LocalMatrix>^>^ laplacianMatrix() override
        {
            throw gcnew NotImplementedException();
            // return laplacian;
        }
    };
}
