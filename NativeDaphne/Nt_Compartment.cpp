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

