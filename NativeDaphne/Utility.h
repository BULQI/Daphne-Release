#pragma once

using namespace System;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Utility
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
		
		static void Copy4DoublesToCli(double *src, array<double> ^dst)
		{
			pin_ptr<double> dst = &dst[0];
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
		}

		static void Copy3DoublesFromCli(array<double> ^source, double *dst)
		{
			pin_ptr<double> src = &source[0];
			*dst++ = *src++;
			*dst++ = *src++;
			*dst++ = *src++;
		}		
		
		static void Copy3DoublesToCli(double *src, array<double> ^dst)
		{
			pin_ptr<double> dst = &dst[0];
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
		
		static void CopyDoublesToCli(double *src, array<double> ^dst)
		{
			pin_ptr<double> dst = &dst[0];
			*ptr = dst + dst->Length;
			while (dst != ptr)
			{
				*dst++ = *src++;
			}
		}
	};

}