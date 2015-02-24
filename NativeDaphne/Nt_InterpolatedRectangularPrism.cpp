// This is the main DLL file.

#include "stdafx.h"

#include "Nt_InterpolatedRectangularPrism.h"
#include <vcclr.h>


namespace NativeDaphne
{
	 
	Nt_InterpolatedRectangularPrism::Nt_InterpolatedRectangularPrism()
	{
		irprism = new NativeDaphneLibrary::NtInterpolatedRectangularPrism();
	}

	int Nt_InterpolatedRectangularPrism::add(int a, int b)
	{
		return irprism->TestAddition(a, b);
	}
}


