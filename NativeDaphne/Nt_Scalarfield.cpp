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
#include "NtUtility.h"

#include <acml.h>


using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace System::Linq;
using namespace System::Text;
using namespace MathNet::Numerics::LinearAlgebra::Double;
using namespace NativeDaphneLibrary;
using namespace NativeDaphneLibrary;


namespace Nt_ManifoldRing
{
	ScalarField::ScalarField(Manifold^ m)
    {
		this->m = m;
		if (m == nullptr)
		{
			darray = gcnew Nt_Darray();
		}
		else 
		{
			//fixed size, won't be appendable to
			darray = gcnew Nt_Darray(m->ArraySize);
		}
    }

	void ScalarField::Initialize(String^ type, array<double>^ parameters)
	{

		if (type == "ScalarFieldCollection")
		{
			this->components = gcnew List<ScalarField^>();
			this->darray->InitializeAsCollection();
			return;
		}
		if (FactoryContainer::fieldInitFactory == nullptr)
		{
			throw gcnew Exception("Calling Initialize without a valid factory.");
		}

		init = FactoryContainer::fieldInitFactory->Initialize(type);
		init->setParameters(parameters);

		if (init->GetType() == ExplicitFieldInitializer::typeid)
		{
			for (int i = 0; i < m->ArraySize; i++)
			{
				darray[i] = init->initialize(i);
			}
		}
		else if (m->GetType() == InterpolatedRectangle::typeid || m->GetType() == InterpolatedRectangularPrism::typeid)
		{
			for (int i = 0; i < m->ArraySize; i++)
			{
				darray[i] = init->initialize((dynamic_cast<InterpolatedNodes^>(m))->linearIndexToLocal(i));
			}
		}
		else
		{
			// for now only the constant field initializer is supported
			if (init->GetType() == ConstFieldInitializer::typeid)
			{
				// initialize the zero-th moment for ME fields; leave gradient equal to zero
				darray[0] = init->initialize(gcnew array<double>{ 0, 0, 0 });
			}
			else
			{
				throw gcnew Exception("Currently only the constant initializer is supported for ME fields.");
			}
		}
	}

	/// <summary>
	/// retrieve field value at a point
	/// </summary>
	/// <param name="point">point parameter</param>
	/// <returns>the field value</returns>
	double ScalarField::Value(array<double>^ point)
	{
		return m->Value(point, this);
	}

	/// <summary>
	/// calculate and return the mean concentration in this scalar field
	/// </summary>
	/// <returns>the mean value</returns>
	double ScalarField::MeanValue()
	{
		return m->MeanValue(this);
	}

	/// <summary>
	/// field gradient at a location
	/// </summary>
	/// <param name="point">point parameter</param>
	/// <returns>gradient vector</returns>
	array<double>^ ScalarField::Gradient(array<double>^ point)
	{
		return m->Grad(point, this);
	}

	/// <summary>
	/// field Laplacian
	/// </summary>
	/// <returns>Laplacian as field</returns>
	ScalarField^ ScalarField::Laplacian()
	{
		return m->Laplacian(this);
	}

	/// <summary>
	/// field diffusion flux term
	/// </summary>
	/// <param name="flux">flux from boundary manifold</param>
	/// <param name="t">Transform that specifies the geometric relationship between 
	/// the boundary and interior manifolds </param>
	/// <returns>diffusion flux term as field in the interior manifold</returns>
	ScalarField^ ScalarField::DiffusionFluxTerm(ScalarField^ flux, Transform^ t, double dt)
	{
		return m->DiffusionFluxTerm(flux, t, this, dt);
	}

	/// <summary>
	/// integrate the field
	/// </summary>
	/// <returns>integral value</returns>
	double ScalarField::Integrate()
	{
		return m->Integrate(this);
	}


	/// <summary>
	/// Impose Dirichlet boundary conditions
	/// </summary>
	/// <param name="from">Field specified on the boundary manifold</param>
	/// <param name="t">Transform that specifies the geometric relationship between 
	/// the boundary and interior manifolds </param>
	/// <returns>The field after imposing Dirichlet boundary conditions</returns>
	ScalarField^ ScalarField::DirichletBC(ScalarField^ from, Transform^ t)
	{
		return m->DirichletBC(from, t, this);
	}

	/// <summary>
	/// multiply the field by a scalar
	/// </summary>
	/// <param name="s">scalar multiplier</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::Multiply(double s)
	{

		dscal(this->ArrayLength, s, this->ArrayPointer, 1);

		/*
		for (int i = 0; i < m->ArraySize; i++)
		{
			darray[i] *= s;
		}
		*/

		return this;
	}

	/// <summary>
	/// this multipy will return a gcnew scalarfield
	/// </summary>
	/// <param name="f"></param>
	/// <returns></returns>
	ScalarField^ ScalarField::Multiply(ScalarField^ f2)
	{

		if (this->m != f2->m)
		{
			throw gcnew Exception("Scalar field multiplicands must share a manifold.");
		}

		return this->m->Multiply(this, f2);
	}

	/// <summary>
	/// scalar multiplication operator
	/// </summary>
	/// <param name="f">field</param>
	/// <param name="s">scalar multiplier</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::operator *(ScalarField^ f, double s)
	{
		ScalarField^ product = gcnew ScalarField(f->m);
		if (f->ArrayLength != product->ArrayLength)
		{
			product->darray->resize(f->ArrayLength);
		}

		for (int i = 0; i < f->ArrayLength; i++)
		{
			product->darray[i] = s * f->darray[i];
		}

		return product;
	}

	/// <summary>
	/// scalar multiplication operator
	/// </summary>
	/// <param name="s">scalar multiplier</param>
	/// <param name="f">field</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::operator *(double s, ScalarField^ f)
	{
		return f * s;
	}

	/// <summary>
	/// scalar field multiplication operator
	/// </summary>
	/// <param name="f1">field 1</param>
	/// <param name="f2">field 2</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::operator *(ScalarField^ f1, ScalarField^ f2)
	{
		if (f1->m != f2->m)
		{
			throw gcnew Exception("Scalar field multiplicands must share a manifold.");
		}
		ScalarField^ product = gcnew ScalarField(f1->m);
		if (f1->ArrayLength != product->ArrayLength)
		{
			product->darray->resize(f1->ArrayLength);
		}
		return f1->m->Multiply(product->reset(f1), f2);
	}

	/// <summary>
	/// scalar field addition
	/// </summary>
	/// <param name="f">field addend</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::Add(ScalarField^ f)
	{
		if (f->m != this->m)
		{
			throw gcnew Exception("Scalar field addends must share a manifold.");
		}

		this->darray->Add(f->darray);
		//for (int i = 0; i < m->ArraySize; i++)
		//{
		//	darray[i] += f->darray[i];
		//}

		return this;
	}

	/// <summary>
	/// addition of a constant to this scalar field
	/// </summary>
	/// <param name="d">constant</param>
	/// <returns></returns>
	ScalarField^ ScalarField::Add(double d)
	{
		return m->Add(this, d);
	}

	/// <summary>
	/// field addition operator
	/// </summary>
	/// <param name="f1">field 1</param>
	/// <param name="f2">field 2</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::operator +(ScalarField^ f1, ScalarField^ f2)
	{
		if (f1->m != f2->m)
		{
			throw gcnew Exception("Scalar field addends must share a manifold.");
		}

		ScalarField^ sum = gcnew ScalarField(f1->m);
		if (sum->ArrayLength != f1->ArrayLength)
		{
			sum->darray->resize(f1->ArrayLength);
		}

		for (int i = 0; i < f1->ArrayLength; i++)
		{
			sum->darray[i] = f1->darray[i] + f2->darray[i];
		}

		return sum;
	}

	/// <summary>
	/// addition of constant to scalar field
	/// </summary>
	/// <param name="f">scalar field</param>
	/// <param name="d">constant</param>
	/// <returns></returns>
	ScalarField^ ScalarField::operator +(ScalarField ^f, double d)
	{
		ScalarField^ sf = gcnew ScalarField(f->m);
		if (f->ArrayLength != sf->ArrayLength)
		{
			sf->darray->resize(f->ArrayLength);
		}
		return sf->reset(f)->Add(d);
	}

	/// <summary>
	/// addition of constant to scalar field
	/// </summary>
	/// <param name="d">constant</param>
	/// <param name="f">scalar field</param>
	/// <returns></returns>
	ScalarField^ ScalarField::operator +(double d, ScalarField ^f)
	{
		return f->Add(d);
	}

	/// <summary>
	/// scalar field subtraction
	/// </summary>
	/// <param name="f">field subtrahend</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::Subtract(ScalarField^ f)
	{
		if (f->m != this->m)
		{
			throw gcnew Exception("Scalar field addends must share a manifold->");
		}

		for (int i = 0; i < this->ArrayLength; i++)
		{
			darray[i] -= f->darray[i];
		}
		return this;
	}

	/// <summary>
	/// field subtraction operator
	/// </summary>
	/// <param name="f1">field 1</param>
	/// <param name="f2">field 2</param>
	/// <returns>resulting field</returns>
	ScalarField^ ScalarField::operator -(ScalarField^ f1, ScalarField^ f2)
	{
		if (f1->m != f2->m)
		{
			throw gcnew Exception("Scalar field addends must share a manifold.");
		}

		ScalarField^ difference = gcnew ScalarField(f1->m);
		if (difference->ArrayLength != f1->ArrayLength)
		{
			difference->darray->resize(f1->ArrayLength);
		}

		for (int i = 0; i < f1->m->ArraySize; i++)
		{
			difference->darray[i] = f1->darray[i] - f2->darray[i];
		}

		return difference;
	}

	/// <summary>
	/// Restrict the scalar field to a boundary
	/// </summary>
	/// <param name="from">The scalar field to be restricted</param>
	/// <param name="pos">The position of the restricted manifold in the space</param>
	// public void Restrict(ScalarField from, array<double>^ pos)
	void ScalarField::Restrict(ScalarField^ from, Transform^ t)
	{
		// this->M->Restrict(from, pos, this);
		this->M->Restrict(from, t, this);
	}

}









