#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;


namespace NativeDaphne 
{

	public ref class Nt_InterpolatedRectangularPrism
	{
	
	public:
		Nt_InterpolatedRectangularPrism(void);

		int add(int a, int b);

	private:
		
		NativeDaphneLibrary::NtInterpolatedRectangularPrism *irprism;

	};
}
