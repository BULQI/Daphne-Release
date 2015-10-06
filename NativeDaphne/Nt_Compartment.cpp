#include "stdafx.h"
#include "Nt_Compartment.h"


namespace NativeDaphne
{

	Nt_Compartment^ Nt_Compartment::getCompartment(Manifold^ m)
	{
		Nt_Compartment^ comp = nullptr;
		if (dynamic_cast<TinyBall^>(m) != nullptr)
		{
			TinyBall^ tb = dynamic_cast<TinyBall^>(m);
			comp = gcnew Nt_Cytosol(tb);
		}
		else if (dynamic_cast<TinySphere^>(m) != nullptr)
		{
			TinySphere^ ts = dynamic_cast<TinySphere^>(m);
			comp =  gcnew Nt_PlasmaMembrane(ts);
		}
		else if (dynamic_cast<InterpolatedRectangularPrism^>(m) != nullptr)
		{
			InterpolatedRectangularPrism^ irp = dynamic_cast<InterpolatedRectangularPrism^>(m);
			comp =  gcnew Nt_ECS(irp);
		}
		else 
		{
			comp = gcnew Nt_Compartment();
		}
		comp->InteriorId = m->Id;
		return comp;
	}
}

