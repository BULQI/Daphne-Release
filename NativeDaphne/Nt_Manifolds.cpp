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
#include "stdafx.h"
#include "Nt_Manifolds.h"
#include "Nt_Scalarfield.h"
#include "Nt_Interpolation.h"
#include "NtUtility.h"
#include <acml.h>
using namespace NativeDaphneLibrary;

namespace Nt_ManifoldRing
{

	//******************************************
	//implementation of MomentExpansionManifold
	//******************************************

	/// <summary>
	/// ME scalarfield multiplication
	/// </summary>
	/// <param name="sf1">lh operand</param>
	/// <param name="sf2">rh operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ MomentExpansionManifold::Multiply(ScalarField^ sf1, ScalarField^ sf2)
	{
		NtUtility::MomentExpansion_NtMultiplyScalar(sf1->ArrayLength, sf1->ArrayPointer, sf2->ArrayPointer);
		return sf1;

		//double s1 = sf1->darray[0];
		//double s2 = sf2->darray[0];
		//sf1->darray[0] *= sf2->darray[0];
		//for (int i = 1; i < sf1->darray->Length; i++)
		//{
		//	sf1->darray[i] = sf1->darray[i] * s2 + s1 * sf2->darray[i];
		//}
		return sf1;
	}

	/// <summary>
	/// addition of a constant to a ME scalar field
	/// gradient is not changed
	/// </summary>
	/// <param name="sf">ME scalar field</param>
	/// <param name="d">constant</param>
	/// <returns></returns>
	ScalarField^ MomentExpansionManifold::Add(ScalarField^ sf, double d)
	{
		for (int i=0; i< sf->ArrayLength; i += this->ArraySize)
		{
			sf->darray[i] += d;
		}
		//sf->darray[0] += d;
		return sf;
	}

	/// <summary>
	/// Restriction of a scalar field to an ME boundary manifold
	/// </summary>
	/// <param name="from">scalar field as represented on from.M, the interior manifold</param>
	/// <param name="pos">scalar field as represented on to.M, the boundary manifold</param>
	/// <param name="to">the location of the boundary manifold in the interior manifold</param>
	/// <returns></returns>
	ScalarField^ MomentExpansionManifold::Restrict(ScalarField^ from, Transform^ t, ScalarField^ to) 
	{
		array<double>^ pos = t->Position;
		array<double>^ grad = from->M->Grad(pos, from);

		to->darray[0] = from->Value(pos);
		to->darray[1] = grad[0];
		to->darray[2] = grad[1];
		to->darray[3] = grad[2];

		return to;
	}

	/// <summary>
	/// calculate and return the mean concentration in this scalar field
	/// </summary>
	/// <param name="sf">scalar field generated by this manifold</param>
	/// <returns>the mean value</returns>
	double MomentExpansionManifold::MeanValue(ScalarField^ sf)  
	{
		return sf->darray[0];
	}


	/// <summary>
	/// TinySphere value at position, delegates to scalar field
	/// </summary>
	/// <param name="x">position</param>
	/// <param name="sf">underlying scalar field</param>
	/// <returns>value as double</returns>
	double TinySphere::Value(array<double>^ x, ScalarField^ sf) 
	{
		double value = sf->darray[0],
			norm = 0;

		for (int i = 0; i < 3; i++)
		{
			norm += x[i] * x[i];
		}
		// returns the mean value only for positions very close to the center of the sphere
		if (norm < 1e-6)
		{
			return value;
		}

		norm = 1.0 / Math::Sqrt(norm);
		for (int i = 1; i < 4; i++)
		{
			value += radius * norm * x[i - 1] * sf->darray[i];
		}

		return value;
	}

	/// <summary>
	/// TinySphere Laplacian field
	/// </summary>
	/// <param name="sf">field operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ TinySphere::Laplacian(ScalarField^ sf) 
	{

		int n = sf->ArrayLength;
		if (laplacian->ArrayLength != n)
		{
			laplacian->darray->resize(n);
		}
		double *laplacian_ptr = laplacian->ArrayPointer;
		memcpy(laplacian_ptr, sf->ArrayPointer, n * sizeof(double));
		for (int i=0; i<n; i+=4)laplacian_ptr[i] = 0;
		dscal(n, -2.0/(radius * radius), laplacian_ptr, 1);
		return laplacian;

		//Nt_Darray^ array = laplacian->darray;
		//array[0] = 0;
		//array[1] = sf->darray[1];
		//array[2] = sf->darray[2];
		//array[3] = sf->darray[3];
		//return laplacian->Multiply(-2.0 / (radius * radius));
	}

	/// <summary>
	/// TinySphere gradient
	/// </summary>
	/// <param name="x">local position</param>
	/// <param name="sf">field operand</param>
	/// <returns>gradient vector</returns>
	array<double>^ TinySphere::Grad(array<double>^ x, ScalarField^ sf) 
	{
		Vector^ u = gcnew DenseVector(x);

		u = dynamic_cast<DenseVector^>(u->Normalize(2.0));

		double d = u[0] * sf->darray[1] + u[1] * sf->darray[2] + u[2] * sf->darray[3];

		return gcnew array<double>{ sf->darray[1] - u[0] * d, sf->darray[2] - u[1] * d, sf->darray[3] - u[2] * d };
	}

	/// <summary>
	/// TinySphere integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double TinySphere::Integrate(ScalarField^ sf) 
	{
		return sf->darray[0] * 4 * Math::PI * radius * radius;
	}

	/// <summary>
	/// constructor
	/// </summary>
	TinySphere::TinySphere(): MomentExpansionManifold(2)
	{
		laplacian = gcnew ScalarField(this);
	}

	//*************************************************
	//implementation of TinyBall
	//*************************************************

	TinyBall::TinyBall(): MomentExpansionManifold(3)
	{
		gradient = gcnew array<double>(3);
		laplacian = gcnew ScalarField(this);
		diffusionField = gcnew ScalarField(this);    
	}

	/// <summary>
	/// TinyBall value at position, delegates to scalar field
	/// </summary>
	/// <param name="x">position</param>
	/// <param name="sf">underlying scalar field</param>
	/// <returns>value as double</returns>
	double TinyBall::Value(array<double>^ x, ScalarField^ sf)  
	{
		// test for out of bounds
		if (localIsOn(x) == false)
		{
			// Return zero when x is not in the ball.
			return 0;
		}

		double value = sf->darray[0];

		for (int i = 1; i < 4; i++)
		{
			value += x[i - 1] * sf->darray[i];
		}

		return value;
	}

	/// <summary>
	/// TinyBall Laplacian field
	/// Contributions from boundary terms must be applied separately through flux update.
	/// </summary>
	/// <param name="sf">field operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ TinyBall::Laplacian(ScalarField^ sf) 
	{
		
		int n = sf->ArrayLength;
		if (laplacian->ArrayLength != n)
		{
			laplacian->darray->resize(n);
		}
		double *laplacian_ptr = laplacian->ArrayPointer;
		NtUtility::TinyBall_laplacian(n, -5.0/(radius * radius), sf->ArrayPointer, laplacian_ptr);

		/*memcpy(laplacian_ptr, sf->ArrayPointer, n * sizeof(double));
		for (int i=0; i<n; i+=4)laplacian_ptr[i] = 0;
		dscal(n, -5.0/(radius * radius), laplacian_ptr, 1);*/
		return laplacian;
		/*
		Nt_Darray^ array = laplacian->darray;
		array[0] = 0;
		array[1] = sf->darray[1];
		array[2] = sf->darray[2];
		array[3] = sf->darray[3];
		laplacian->Multiply(-5.0 / (radius * radius));
		return laplacian;
		*/
	}

	/// <summary>
	/// TinyBall gradient
	/// </summary>
	/// <param name="x">local position</param>
	/// <param name="sf">field operand</param>
	/// <returns>gradient vector</returns>
	array<double>^ TinyBall::Grad(array<double>^ x, ScalarField^ sf) 
	{
		if (localIsOn(x) == false)
		{
			return gcnew array<double>{ 0, 0, 0 };
		}
		gradient[0] = sf->darray[1];
		gradient[1] = sf->darray[2];
		gradient[2] = sf->darray[3];

		return gradient;
	}

	/// <summary>
	/// TinyBall field diffusion, flux term
	/// The flux scalar field has already been divided by the diffusion coefficient in the boundary reaction Step() method.
	/// </summary>
	/// <param name="flux">flux involved</param>
	/// <param name="t">Transform - not used</param>
	/// <returns>diffusion flux term as field</returns>
	ScalarField^ TinyBall::DiffusionFluxTerm(ScalarField^ flux, Transform^ t, ScalarField^ dst, double dt) 
	{
		if (dynamic_cast<TinySphere^>(flux->M) == nullptr || flux->ArrayLength != dst->ArrayLength)
		{
			throw gcnew Exception("Manifold mismatch: flux for TinyBall must be on TinySphere.");
		}
	
		NtUtility::TinyBall_DiffusionFluxTerm(dst->ArrayLength, -dt/radius, flux->ArrayPointer, dst->ArrayPointer);
		/*
		Nt_Darray^ array = diffusionField->darray;
		array[0] = 3 * flux->darray[0] / radius;
		array[1] = 5 * flux->darray[1] / radius;
		array[2] = 5 * flux->darray[2] / radius;
		array[3] = 5 * flux->darray[3] / radius;
		return dst->Add(diffusionField->Multiply(-dt));
		*/
	}

	/// <summary>
	/// TinyBall integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double TinyBall::Integrate(ScalarField^ sf) 
	{
		return sf->darray[0] * 4 * Math::PI * radius * radius * radius / 3;
	}

	//****************************************************
	// implementation of InterpolatedNodes
	//****************************************************

	/// <summary>
	/// initialize the nodes per side and stepsize members
	/// </summary>
	/// <param name="data">data array with dim + 2 entries</param>
	void InterpolatedNodes::Initialize(array<double>^ data) 
	{
		if (data->Length != Dim + 2)
		{
			throw gcnew Exception("Dimension mismatch in interpolated manifold.");
		}

		nNodesPerSide = gcnew array<int>(data->Length - 2);
		for (int i = 0; i < Dim; i++)
		{
			nNodesPerSide[i] = (int)data[i];
		}
		stepSize = data[data->Length - 2];
		bool toroidal = Convert::ToBoolean(data[data->Length - 1]);

		extent = gcnew array<double>(Dim);
		// accumulate array size and compute extents
		ArraySize = 1;
		for (int i = 0; i < Dim; i++)
		{
			ArraySize *= nNodesPerSide[i];
			extent[i] = (nNodesPerSide[i] - 1) * stepSize;
		}

		PrincipalPoints = gcnew array<Vector^>(ArraySize);
		for (int i = 0; i < ArraySize; i++)
		{
			//PrincipalPoints[i] = linearIndexToLocal(i);
			PrincipalPoints[i] = gcnew DenseVector(linearIndexToLocal(i));
		}

		// initialize interpolator
		interpolator->Init(this, toroidal);
	}



	/// <summary>
	/// IL scalarfield multiplication
	/// </summary>
	/// <param name="sf1">lh operand</param>
	/// <param name="sf2">rh operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ InterpolatedNodes::Multiply(ScalarField^ sf1, ScalarField^ sf2) 
	{
		for (int i = 0; i < ArraySize; i++)
		{
			sf1->darray[i] *= sf2->darray[i];
		}
		return sf1;
	}

	/// <summary>
	/// addition of a constant to interpolated nodes scalar field
	/// </summary>
	/// <param name="sf">interpolated nodes scalar field</param>
	/// <param name="d">constant</param>
	/// <returns></returns>
	ScalarField^ InterpolatedNodes::Add(ScalarField^ sf, double d) 
	{
		for (int i = 0; i < ArraySize; i++)
		{
			sf->darray[i] += d;
		}
		return sf;
	}


	/// <summary>
	/// IL value at position, delegates to scalar field and interpolates
	/// </summary>
	/// <param name="x">position</param>
	/// <param name="sf">underlying scalar field</param>
	/// <returns>value as double</returns>
	double InterpolatedNodes::Value(array<double>^ x, ScalarField^ sf) 
	{
		// machinery for interpolation
		// test for out of bounds
		if (localIsOn(x) == false)
		{
			// Note: is returning zero the right thing to do when x is out of bounds?
			return 0;
		}
		return interpolator->Interpolate(x, sf);
	}

	/// <summary>
	/// calculate and return the mean concentration in this scalar field
	/// </summary>
	/// <param name="sf">scalar field generated by this manifold</param>
	/// <returns>the mean value</returns>
	double InterpolatedNodes::MeanValue(ScalarField^ sf) 
	{
		return interpolator->Integration(sf) / (sf->M->Extent(0) * sf->M->Extent(1) * sf->M->Extent(2));
	}

	/// <summary>
	/// IL Laplacian field
	/// </summary>
	/// <param name="sf">field operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ InterpolatedNodes::Laplacian(ScalarField^ sf) 
	{
		return interpolator->Laplacian(sf);
	}

	/// <summary>
	/// IL gradient
	/// </summary>
	/// <param name="x">local position</param>
	/// <param name="sf">field operand</param>
	/// <returns>gradient vector</returns>
	array<double>^ InterpolatedNodes::Grad(array<double>^ x, ScalarField^ sf) 
	{
		if (localIsOn(x) == false)
		{
			return gcnew array<double>(Dim);
		}
		return interpolator->Gradient(x, sf);
	}


	/// <summary>
	/// Restriction of a scalar field to an IL boundary manifold
	/// </summary>
	/// <param name="from">scalar field as represented on from.M, the interior manifold</param>
	/// <param name="t">Transform that defines spatial relation between from and to</param>
	/// <param name="to">the location of the boundary manifold in the interior manifold</param>
	/// <returns></returns>
	ScalarField^ InterpolatedNodes::Restrict(ScalarField^ from, Transform^ t, ScalarField^ to) 
	{
		array<double>^ pos = t->Translation->ArrayCopy;
		Vector^ x = gcnew DenseVector(gcnew array<double>(3));

		for (int i = 0; i < ArraySize; i++)
		{
			// the coordinates of this interpolation point in this boundary manifold
			//x = this.linearIndexToLocal(i);
			x = gcnew DenseVector(this->linearIndexToLocal(i));
			// x+pos are the coordinates of this interpolation point in the interior manifold
			//to.array[i] = from.M.Value(x + pos, from);                
			Vector^ y = gcnew DenseVector(pos);
			y = dynamic_cast<DenseVector^>(x + y);
			to->darray[i] = from->M->Value(y->ToArray(), from);
		}
		return to;
	}

	ScalarField^ InterpolatedNodes::DiffusionFluxTerm(ScalarField^ flux, Transform^ t, ScalarField^ sf, double dt) 
	{
		return interpolator->DiffusionFlux(flux, t, sf, dt);
	}

	/// <summary>
	/// Impose Dirichlet boundary conditions
	/// </summary>
	/// <param name="from">Field specified on the boundary manifold</param>
	/// <param name="t">Transform that specifies the geometric relationship between 
	/// the boundary and interior manifolds </param>
	/// <param name="to">Field specified on the interior manifold</param>
	/// <returns>The field after imposing Dirichlet boundary conditions</returns>
	ScalarField^ InterpolatedNodes::DirichletBC(ScalarField^ from, Transform^ t, ScalarField^ to) 
	{
		return interpolator->DirichletBC(from, t, to);
	}


	//****************************************************
	// implementation of InterpolatedLine
	//****************************************************

	/// <summary>
	/// IL line integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double InterpolatedLine::Integrate(ScalarField^ sf) 
	{
		array<double>^ point = gcnew array<double>{ 0, 0, 0 };
		double sum = 0,
			voxel = stepSize;

		for (int i = 0; i < nNodesPerSide[0] - 1; i++)
		{
			point[0] = (i + 0.5) * stepSize;

			// The value at the center of the voxel
			sum += sf->Value(point);
		}

		return sum * voxel;
	}

	//****************************************************
	// implementation of InterpolatedRectangle
	//****************************************************


	/// <summary>
	/// IL rectangle integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double InterpolatedRectangle::Integrate(ScalarField^ sf) 
	{
		array<double>^ point = gcnew array<double>{ 0, 0, 0 };
		double sum = 0,
			voxel = stepSize * stepSize;

		for (int j = 0; j < nNodesPerSide[1] - 1; j++)
		{
			for (int i = 0; i < nNodesPerSide[0] - 1; i++)
			{
				point[0] = (i + 0.5) * stepSize;
				point[1] = (j + 0.5) * stepSize;

				// The value at the center of the voxel
				sum += sf->Value(point);
			}
		}

		return sum * voxel;
	}

	//****************************************************
	// implementation of InterpolatedRectangularPrism
	//****************************************************

	/// <summary>
	/// IL prism integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double InterpolatedRectangularPrism::Integrate(ScalarField^ sf) 
	{
		array<double>^ point = gcnew array<double>(3);
		double sum = 0,
			voxel = stepSize * stepSize * stepSize;

		for (int k = 0; k < nNodesPerSide[2] - 1; k++)
		{
			for (int j = 0; j < nNodesPerSide[1] - 1; j++)
			{
				for (int i = 0; i < nNodesPerSide[0] - 1; i++)
				{
					point[0] = (i + 0.5) * stepSize;
					point[1] = (j + 0.5) * stepSize;
					point[2] = (k + 0.5) * stepSize;

					// The value at the center of the voxel
					sum += sf->Value(point);
				}
			}
		}

		return sum * voxel;
	}

	//****************************************************
	// implementation of PointManifold
	//****************************************************


	/// <summary>
	/// value at position, delegates to scalar field 
	/// </summary>
	/// <param name="x">position</param>
	/// <param name="sf">underlying scalar field</param>
	/// <returns>value as double</returns>
	double PointManifold::Value(array<double>^ x, ScalarField^ sf) 
	{
		if (localIsOn(x))
		{
			return sf->darray[0];
		}
		else
		{
			return 0;
		}
	}

	/// <summary>
	/// calculate and return the mean concentration in this scalar field
	/// </summary>
	/// <param name="sf">scalar field generated by this manifold</param>
	/// <returns>the mean value</returns>
	double PointManifold::MeanValue(ScalarField^ sf) 
	{
		return sf->darray[0];
	}


	/// <summary>
	/// scalarfield multiplication
	/// </summary>
	/// <param name="sf1">lh operand</param>
	/// <param name="sf2">rh operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ PointManifold::Multiply(ScalarField^ sf1, ScalarField^ sf2) 
	{
		sf1->darray[0] *= sf2->darray[0];
		return sf1;
	}

	/// <summary>
	/// addition of a constant to a point manifold
	/// </summary>
	/// <param name="sf">point manifold scalar field</param>
	/// <param name="d">constant</param>
	/// <returns></returns>
	ScalarField^ PointManifold::Add(ScalarField^ sf, double d) 
	{
		sf->darray[0] += d;
		return sf;
	}

	/// <summary>
	/// Laplacian field
	/// </summary>
	/// <param name="sf">field operand</param>
	/// <returns>resulting field</returns>
	ScalarField^ PointManifold::Laplacian(ScalarField^ sf) 
	{
		// return zeros - no diffusion
		return gcnew ScalarField(this);
	}

	/// <summary>
	/// gradient
	/// </summary>
	/// <param name="x">local position</param>
	/// <param name="sf">field operand</param>
	/// <returns>gradient vector</returns>
	array<double>^ PointManifold::Grad(array<double>^ x, ScalarField^ sf) 
	{
		return gcnew array<double>(x->Length);
	}


	/// <summary>
	/// field diffusion, flux term
	/// </summary>
	/// <param name="flux">flux involved</param>
	/// <returns>diffusion flux term as field</returns>
	ScalarField^ PointManifold::DiffusionFluxTerm(ScalarField^ flux, Transform^ t, ScalarField^ dst, double dt) 
	{
		throw gcnew Exception("DiffusionFluxTerm not implemented for PointManifold fields");
	}

	/// <summary>
	/// integrate over the whole field
	/// </summary>
	/// <param name="sf">field parameter</param>
	/// <returns>integral value</returns>
	double PointManifold::Integrate(ScalarField^ sf) 
	{
		return sf->darray[0];
	}

	/// <summary>
	/// Restriction of a scalar field to the PointManifold
	/// </summary>
	/// <param name="from">Scalar field being restricted</param>
	/// <param name="to">Scalar field in restricted space</param>
	/// <returns></returns>
	ScalarField^ PointManifold::Restrict(ScalarField^ from, Transform^ t, ScalarField^ to) 
	{
		array<double>^ pos = t->Translation->ArrayCopy;
		to->darray[0] = from->Value(pos);

		return to;
	}

}





