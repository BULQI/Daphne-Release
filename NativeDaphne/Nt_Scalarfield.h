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
#pragma once

#include "Nt_ManifoldUtilities.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace System::Linq;
using namespace System::Text;
using namespace MathNet::Numerics::LinearAlgebra::Double;
using namespace NativeDaphneLibrary;


namespace Nt_ManifoldRing
{
    /// <summary>
    /// helper for field initialization
    /// </summary>
	[SuppressUnmanagedCodeSecurity]
    public interface class IFieldInitializer
    {
        /// <summary>
        /// initialization routine based on 3-dim coordinates
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>field value at point</returns>
        double initialize(array<double>^ point);

        /// <summary>
        /// initialization based on idnex
        /// </summary>
        /// <param name="index">linear index</param>
        /// <returns>field value for the index</returns>
        double initialize(int index);

        void setParameters(array<double>^ parameters);
    };

    /// <summary>
    /// field initialization with a constant
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public ref class ConstFieldInitializer : IFieldInitializer
    {
	private:
		double cVal;
        bool initialized;

	public:
        /// <summary>
        /// constructor
        /// </summary>
        ConstFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
        virtual void setParameters(array<double>^ parameters)
        {
            if (parameters->Length != 1)
            {
                throw gcnew Exception("ConstFieldInitializer length must be 1.");
            }

            cVal = parameters[0];
            initialized = true;
        }

        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>constant value regardless of point</returns>
        virtual double initialize(array<double>^ point)
        {
            if (initialized == false)
            {
                throw gcnew Exception("Must call setParameters prior to using FieldInitializer.");
            }

            return cVal;
        }

        virtual double initialize(int index)
        {
            throw gcnew Exception("ConstFieldInitializer should not be called by index");
        }

    };

    /// <summary>
    /// field initialization with a linear profile
    /// </summary>
	[SuppressUnmanagedCodeSecurity]
    public ref class LinearFieldInitializer : IFieldInitializer
    {
	private:
        double c1;
        double c2;
        double x1;
		double x2;
		int dim;
		double slope;
		double intercept;
		bool initialized;

	public:
        /// <summary>
        /// constructor
        /// </summary>
        LinearFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
        virtual void setParameters(array<double>^ parameters)
        {
            if (parameters->Length != 5)
            {
                throw gcnew Exception("LinearFieldInitializer length must be 5.");
            }

            c1 = parameters[0];
            c2 = parameters[1];
            x1 = parameters[2];
            x2 = parameters[3];
            dim = (int)parameters[4];
            slope = (c2 - c1) / (x2 - x1);
            intercept = c1 - x1 * slope;

            initialized = true;
        }

        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>linear value using the dim component of point</returns>
        virtual double initialize(array<double>^ point)
        {
            if (initialized == false)
            {
                throw gcnew Exception("Must call setParameters prior to using FieldInitializer.");
            }

            return slope * point[dim] + intercept;
        }

        virtual double initialize(int index)
        {
            throw gcnew Exception("LinearFieldInitializer should not be called by index");
        }

    };

    /// <summary>
    /// Gaussian field initializer
    /// </summary>
    public ref class GaussianFieldInitializer : IFieldInitializer
    {
	private:
        array<double>^ center;
        array<double, 2>^ Sigma;
        double max;
        bool initialized;

	public:
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="c">initialization array</param>
        GaussianFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the Gaussian parameters
        /// </summary>
        /// <param name="parameters">the Gaussian's center, sigma/decay vector, maximum value packed into an array</param>
        virtual void setParameters(array<double>^ parameters)
        {
            if (parameters->Length != 13)
            {
                throw gcnew Exception("GaussianFieldInitializer length must be 13: center, peak value, and sigma matrix by columns.");
            }
            
            this->center = gcnew array<double>{ parameters[0], parameters[1], parameters[2] };

            // fill by columns
            this->Sigma = gcnew array<double, 2>(3, 3); //gcnew double[3,3];
            int k = 3;
            for (int i = 0; i < 3; i++, k += 3)
            {
                this->Sigma[0, i] = parameters[0 + k];
                this->Sigma[1, i] = parameters[1 + k];
                this->Sigma[2, i] = parameters[2 + k];
            }

            this->max = parameters[12];

            initialized = true;
        }

        /// <summary>
        /// Gaussian initializer routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>the Gaussian's value at point</returns>
		virtual double initialize(array<double>^ point)
        {
            if (point == nullptr)
            {
                throw gcnew Exception("Initializing Gaussian needs a valid point.");
            }
            if (point->Length != center->Length || point->Length != Sigma->GetLength(0))
            {
                throw gcnew Exception("Exception initializing Gaussian field, array length mismatch.");
            }
            if (initialized == false)
            {
                throw gcnew Exception("Must call setParameters prior to using FieldInitializer.");
            }

            double f = 0;
            array<double>^ temp = gcnew array<double>(3);
            for (int i = 0; i < point->Length; i++)
            {
                temp[i] =   (center[0] - point[0]) * Sigma[0, i]
                          + (center[1] - point[1]) * Sigma[1, i]
                          + (center[2] - point[2]) * Sigma[2, i];
            }

            for (int i = 0; i < point->Length; i++)
            {
                f += temp[i] * (center[i] - point[i]);
            }
            return max * Math::Exp(-f / 2);
        }

		virtual double initialize(int index)
        {
            throw gcnew Exception("GaussianFieldInitializer should not be called by index");
        }
    };

    /// <summary>
    /// field initialization for explicit valus
    /// </summary>
    public ref class ExplicitFieldInitializer : IFieldInitializer
    {
	private:
        bool initialized;
        array<double>^ exp_vals;

	public:
        /// <summary>
        /// constructor
        /// </summary>
		ExplicitFieldInitializer()
        {
            initialized = false;
        }

        /// <summary>
        /// set the constant value
        /// </summary>
        /// <param name="parameters">array with one constant value</param>
		virtual void setParameters(array<double>^ parameters)
        {
            exp_vals = gcnew array<double>(parameters->Length);
            Array::Copy(parameters, exp_vals, parameters->Length);
            initialized = true;
        }
        /// <summary>
        /// initialization routine
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>constant value regardless of point</returns>
        virtual double initialize(array<double>^ point)
        {
            throw gcnew Exception("ExcplictFieldInitializer should be called with index");
        }

        /// <summary>
        /// return the field for the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        virtual double initialize(int index)
        {

            if (initialized == false)
            {
                throw gcnew Exception("Must call setParameters prior to using FieldInitializer.");
            }

            if (index < 0 || index >= exp_vals->Length)
            {
                throw gcnew Exception("ExplicitFieldInitializer: index out of range");
            }
            return exp_vals[index];
        }
      
    };

    /// <summary>
    /// initializer factory
    /// </summary>
    public interface class IFieldInitializerFactory
    {
        IFieldInitializer^ Initialize(String^ type);
    };

	ref class Manifold;

    /// <summary>
    /// scalar field class with operations
    /// </summary>
	[SuppressUnmanagedCodeSecurity]
    public ref class ScalarField
    {
	private:
        Manifold^ m;
        IFieldInitializer^ init;
		List<ScalarField^>^ components;

	//internal:
	public:
		Nt_Darray^ darray;
	public:
		
		/// <summary>
        /// underlying manifold
        /// </summary>
        property Manifold^ M 
		{ 
			Manifold^ get()
			{ 
				return m; 
			} 
		}

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="m">manifold</param>
        ScalarField(Manifold^ m);

		void AddComponent(ScalarField^ src)
		{
			if (this->components == nullptr)
			{
				throw gcnew Exception("Object is not of collection type");
			}
			if (this->m == nullptr)this->m = src->M;
			darray->AddComponent(src->darray);
			components->Add(src);
		}

		//returning the index of the component before removal
		int RemoveComponent(ScalarField^ src)
		{
			int index = darray->RemoveComponent(src->darray);
			if (index != components->Count -1)
			{
				components[index] = components[components->Count -1];
			}
			components->RemoveAt(components->Count -1);
			return index;
		}
		
	internal:
		property double* ArrayPointer
		{
			double *get()
			{
				return darray->NativePointer;
			}
		}

		property int ArrayLength
		{
			int get()
			{
				return darray->Length;
			}
		}
	public:

        /// <summary>
        /// initialize the field according to the initializer object
        /// </summary>
        /// <param name="init">initializer object</param>
        void Initialize(String^ type, array<double>^ parameters);

        ScalarField^ reset(ScalarField^ src)
        {
			if (this->ArrayLength != src->ArrayLength)
			{
				throw gcnew ArgumentException("noncompatabile array length");
			}
			memcpy(this->ArrayPointer, src->ArrayPointer, darray->Length * sizeof(double));
            //for (int i = 0; i < darray->Length; i++) darray[i] = src->darray[i];
            return this;
        }

        ScalarField^ reset(double d)
        {
            for (int i = 0; i < darray->Length; i++) darray[i] = d;
            return this;
        }

        /// <summary>
        /// copy array value to valarr, used to access the array
        /// for saving states only
        /// </summary>
        /// <param name="val">destination array</param>
        /// <param name="start">destination start index</param>
        /// <returns> number of elements copied</returns>
		int CopyArray(array<double>^ valarr, int start)
        {
            Array::Copy(darray->ArrayCopy, 0, valarr, start, darray->Length);
            return darray->Length;
        }

		/// <summary>
        /// copy array value to valarr, used to access the array
        /// for saving states only
        /// </summary>
        /// <param name="val">destination array</param>
        /// <returns> number of elements copied</returns>
		int CopyArray(array<double>^ valarr)
        {
            Array::Copy(darray->ArrayCopy, 0, valarr, 0, darray->Length);
            return darray->Length;
        }

        /// <summary>
        /// retrieve field value at a point
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>the field value</returns>
		double Value(array<double>^ point);

        /// <summary>
        /// calculate and return the mean concentration in this scalar field
        /// </summary>
        /// <returns>the mean value</returns>
		double MeanValue();

        /// <summary>
        /// field gradient at a location
        /// </summary>
        /// <param name="point">point parameter</param>
        /// <returns>gradient vector</returns>
		array<double>^ Gradient(array<double>^ point);

        /// <summary>
        /// field Laplacian
        /// </summary>
        /// <returns>Laplacian as field</returns>
		ScalarField^ Laplacian();

        /// <summary>
        /// field diffusion flux term
        /// </summary>
        /// <param name="flux">flux from boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <returns>diffusion flux term as field in the interior manifold</returns>
		ScalarField^ DiffusionFluxTerm(ScalarField^ flux, Transform^ t, double dt);

        /// <summary>
        /// integrate the field
        /// </summary>
        /// <returns>integral value</returns>
		double Integrate();

        /// <summary>
        /// Impose Dirichlet boundary conditions
        /// </summary>
        /// <param name="from">Field specified on the boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <returns>The field after imposing Dirichlet boundary conditions</returns>
		ScalarField^ DirichletBC(ScalarField^ from, Transform^ t);

        /// <summary>
        /// multiply the field by a scalar
        /// </summary>
        /// <param name="s">scalar multiplier</param>
        /// <returns>resulting field</returns>
		ScalarField^ Multiply(double s);

		/// <summary>
		/// this multipy will return a gcnew scalarfield
		/// </summary>
		/// <param name="f"></param>
		/// <returns></returns>
		ScalarField^ ScalarField::Multiply(ScalarField^ f2);

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="f">field</param>
        /// <param name="s">scalar multiplier</param>
        /// <returns>resulting field</returns>
		static ScalarField^ operator *(ScalarField^ f, double s);

        /// <summary>
        /// scalar multiplication operator
        /// </summary>
        /// <param name="s">scalar multiplier</param>
        /// <param name="f">field</param>
        /// <returns>resulting field</returns>
        static ScalarField^ operator *(double s, ScalarField^ f);

        /// <summary>
        /// scalar field multiplication operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
		static ScalarField^ operator *(ScalarField^ f1, ScalarField^ f2);

        /// <summary>
        /// scalar field addition
        /// </summary>
        /// <param name="f">field addend</param>
        /// <returns>resulting field</returns>
		ScalarField^ Add(ScalarField^ f);

        /// <summary>
        /// addition of a constant to this scalar field
        /// </summary>
        /// <param name="d">constant</param>
        /// <returns></returns>
		ScalarField^ Add(double d);

        /// <summary>
        /// field addition operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
		static ScalarField^ operator +(ScalarField^ f1, ScalarField^ f2);
        

        /// <summary>
        /// addition of constant to scalar field
        /// </summary>
        /// <param name="f">scalar field</param>
        /// <param name="d">constant</param>
        /// <returns></returns>
		static ScalarField^ operator +(ScalarField ^f, double d);


        /// <summary>
        /// addition of constant to scalar field
        /// </summary>
        /// <param name="d">constant</param>
        /// <param name="f">scalar field</param>
        /// <returns></returns>
		static ScalarField^ operator +(double d, ScalarField^ f);

        /// <summary>
        /// scalar field subtraction
        /// </summary>
        /// <param name="f">field subtrahend</param>
        /// <returns>resulting field</returns>
		ScalarField^ Subtract(ScalarField^ f);

        /// <summary>
        /// field subtraction operator
        /// </summary>
        /// <param name="f1">field 1</param>
        /// <param name="f2">field 2</param>
        /// <returns>resulting field</returns>
		static ScalarField^ operator -(ScalarField^ f1, ScalarField^ f2);

        /// <summary>
        /// Restrict the scalar field to a boundary
        /// </summary>
        /// <param name="from">The scalar field to be restricted</param>
        /// <param name="pos">The position of the restricted manifold in the space</param>
        // public void Restrict(ScalarField from, array<double>^ pos)
        void Restrict(ScalarField^ from, Transform^ t);
    };
}
