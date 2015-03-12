#pragma once

using namespace System;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_Utility
	{
	public:

		static void Copy4DoublesFromCli(array<double> ^source, double *dst)
		{
			pin_ptr<double> src = &source[0];
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
		}		
		
		static void Copy4DoublesToCli(double *src, array<double> ^dest)
		{
			System::Runtime::InteropServices::Marshal::Copy((IntPtr)src, dest, 0, 4);
			//pin_ptr<double> dst = &dest[0];
			//*dst++ = *src++;
			//*dst++ = *src++;
			//*dst++ = *src++;
			//*dst++ = *src++;
		}

		static void Copy3DoublesFromCli(array<double> ^source, double *dst)
		{
			pin_ptr<double> src = &source[0];
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
		}		
		
		static void Copy3DoublesToCli(double *src, array<double> ^dest)
		{
			pin_ptr<double> dst = &dest[0];
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
		}	


		static void CopyDoublesFromCli(array<double> ^source, double *dst)
		{
			pin_ptr<double> src = &source[0];
			double *ptr = src + source->Length;
			while (src != ptr)
			{
				*dst++ = *src++;
			}
		}		
		
		static void CopyDoublesToCli(double *src, array<double> ^dest)
		{
			pin_ptr<double> dst = &dest[0];
			double *ptr = dst + dest->Length;
			while (dst != ptr)
			{
				*dst++ = *src++;
			}
		}

		//we will alloc memrory in 2^n
		//the memory doubles when not enough
		static int GetAllocSize(int n, int curSize)
		{
			int size = curSize == 0 ? 1: curSize;
			while (n > size)size *= 2;
			return size;
		}

	};

}

