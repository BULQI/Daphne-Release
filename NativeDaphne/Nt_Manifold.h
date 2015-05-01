#pragma once

#include <errno.h>
using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{

	public enum class Nt_ManifoldType {TinyBall=0, TinySphere, InterpolatedRectangularPrism};

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Manifold
	{
	public:
		int Dim;

		Nt_Manifold(int dim)
		{
			Dim = dim;
		}

	};

	public ref class Nt_TinyBall : Nt_Manifold
	{
	private:
		double radius;

	public:
		Nt_TinyBall(double r) : Nt_Manifold(3)
		{
			radius = r;
		}

		property double Radius
		{
			double get()
			{
				return radius;
			}
		}
	};

	public ref class Nt_TinySphere : Nt_Manifold
	{
	private:
		double radius;

	public:
		Nt_TinySphere(double r) : Nt_Manifold(2)
		{
			radius = r;
		}

		property double Radius
		{
			double get()
			{
				return radius;
			}
		}
	};

	public ref class Nt_RectangularPrism : Nt_Manifold
	{
	public:
		Nt_RectangularPrism() : Nt_Manifold(3)
		{
		}

	};

	//creating manifold for cells compartment
	//the manifolds are shared between cells of same dimmension
	//the so called flyweight battern.
	public ref class Nt_ManifoldFactory sealed 
	{
	private:
		static Nt_ManifoldFactory^ factory;

		List<Nt_TinySphere^> ^tinySheres;
		List<Nt_TinyBall^>^ tinyBalls;
		List<Nt_RectangularPrism^> ^rectangularPrisms;

		Nt_ManifoldFactory()
		{
			tinySheres = gcnew List<Nt_TinySphere^>();
			tinyBalls = gcnew List<Nt_TinyBall^>();
			rectangularPrisms = gcnew List<Nt_RectangularPrism^>();
		}

	public:
		static property Nt_ManifoldFactory^ Instance
		{
			Nt_ManifoldFactory^ get()
			{
				if (factory == nullptr)
				{
					factory = gcnew Nt_ManifoldFactory();
				}
				return factory;
			}
		}

		Nt_Manifold^ GetManifold(Nt_ManifoldType mtype, array<double>^ data)
		{
			if (mtype == Nt_ManifoldType::TinyBall)
			{
				double r = data[0];
				for (int i= 0; i< tinyBalls->Count; i++)
				{
					if (tinyBalls[i]->Radius == r)return tinyBalls[i];
				}
				
				Nt_TinyBall^ tball = gcnew Nt_TinyBall(r);
				tinyBalls->Add(tball);
				return tball;
			}

			if (mtype == Nt_ManifoldType::TinySphere)
			{
				double r = data[0];
				for (int i= 0; i< tinyBalls->Count; i++)
				{
					if (tinyBalls[i]->Radius == r)return tinyBalls[i];
				}
				
				Nt_TinyBall^ tball = gcnew Nt_TinyBall(r);
				tinyBalls->Add(tball);
				return tball;
			}

			if (mtype == Nt_ManifoldType::InterpolatedRectangularPrism)
			{
				throw gcnew NotImplementedException();
			}

			throw gcnew NotImplementedException();
		}

		//we may need to clear the content
		//in order to free memory of finished simulation.
		void Clear()
		{
			throw gcnew NotImplementedException();
		}

	};


}


