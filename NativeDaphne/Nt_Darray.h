#pragma once

#include <stdio.h>
#include <stdlib.h>
#include "NtUtility.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace Newtonsoft::Json;
using namespace NativeDaphneLibrary;

namespace NativeDaphne 
{
	//forward declaration
	ref class Nt_Darray;

	public ref class DoubleArraySerializer : public JsonConverter
	{
	public:
		virtual void WriteJson(JsonWriter^ writer, Object^ value, JsonSerializer ^serializer) override; 

		virtual Object^ ReadJson(JsonReader^ reader, Type^ objectType, Object^ existingValue, JsonSerializer^ serializer) override; 

		virtual bool CanConvert(Type^ objectType) override 
		{
			Type^ t = Nt_Darray::typeid;
			return t->IsAssignableFrom(objectType);
		}	
	};

	[SuppressUnmanagedCodeSecurity]
	[JsonConverter(DoubleArraySerializer::typeid)]
	public ref class Nt_Darray
	{
	private:
		int length;
		//indicate pointer is replaced with external pointer
		bool is_pointer_owner;
		double *_array;
	public:

		Nt_Darray()
		{
			length = 0;
			is_pointer_owner = true;
			_array = NULL;
		}

		Nt_Darray(int len)
		{
			_array = (double *)malloc(len *sizeof(double));
			memset(_array, 0, len *sizeof(double));
			length = len;
			is_pointer_owner = true;
		}

		~Nt_Darray()
		{
			this->!Nt_Darray();
		}

		!Nt_Darray()
		{
			if (is_pointer_owner == true && _array != NULL)
			{
				free(_array);
				_array = NULL;
			}
		}

		property array<double>^ ArrayCopy
		{
			array<double>^ get() 
			{
				array<double> ^tmp = gcnew array<double>(length);
				for (int i=0; i<length; i++)
				{
					tmp[i] = _array[i];
				}
				return tmp;
			}
		}


			

		[JsonIgnore]
		property double default[int]
		{
			double get(int index)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				}
				return _array[index];
			}
			void set(int index, double value)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				} 
				_array[index] = value;
			}
		}

		property int Length
		{
			int get()
			{
				return length;
			}
		}

		static void Copy(Nt_Darray ^src, Nt_Darray ^dst, int len)
		{
			for (int i=0; i<len; i++)
			{
				dst[i] = src[i];
			}
		}

		//these methods may be called from c# layer to avoid the cost of accessing the pointers
		Nt_Darray^ Add(Nt_Darray^ d)
		{
			if (d->Length != length)
			{
				throw gcnew Exception("Error Nt_Darray.Add: dimension mismsatch");
			}
			double *dst = d->NativePointer;
			NtUtility::AddDoubleArray(_array, dst, length);
			return this;
		}

		Nt_Darray^ Multiply(double s)
		{
			for (int i=0; i< length; i++)
			{
				_array[i] *= s;
			}
			return this;
		}

	//internal:
		[JsonIgnore]
		property double *NativePointer
		{
			double *get()
			{
				return _array;
			}
			void set(double *dptr)
			{
				if (is_pointer_owner == true && _array != NULL)
				{
					free(_array);
				}
				_array = dptr;
				is_pointer_owner = false;
			}
		}

		//allocate and take owner ship of the pointer
		//called when remoe this object from a collection
		void detach()
		{
			if (is_pointer_owner == true || _array == NULL || length == 0)return;
			double *tmp = (double *)malloc(length *sizeof(double));
			memcpy(tmp, _array, length * sizeof(double));
			_array = tmp;
			is_pointer_owner = true;
		}

		property bool IsPointerOwner
		{
			bool get()
			{
				return is_pointer_owner;
			}
			//void set(bool value)
			//{
			//	is_pointer_owner = value;
			//}
		}

		/// <summary>
        /// swap the memorry of this with that of item
        /// </summary>
        /// <param name="item">item to be swaped</param>
        /// <returns>void</returns>
		void MemSwap(Nt_Darray^ item)
		{
			if (item->Length != this->length)
			{
				throw gcnew Exception("Nt_Darray.memswap, item length mismatch");
			}
			//this is enforced to avoid memory leak
			//need to handle ownership if swaping item with different ownership is needed.
			if (item->IsPointerOwner != this->IsPointerOwner)
			{
				throw gcnew Exception("Nt_Darray.memswap only allowed for item in same collection");
			}
			double *array2 = item->NativePointer;
			for (int i=0; i< length; i++)
			{
				double tmp = _array[i];
				_array[i] = array2[i];
				array2[i] = tmp;
			}
			item->NativePointer = _array;
			this->_array = array2;
		}
	};




	///int type array
	[SuppressUnmanagedCodeSecurity]
	[JsonConverter(DoubleArraySerializer::typeid)]
	public ref class Nt_Iarray
	{
	private:
		int length;
		//indicate pointer is replaced with external pointer
		bool is_pointer_owner;
		int *_array;
	public:

		Nt_Iarray()
		{
			length = 0;
			is_pointer_owner = true;
			_array = NULL;
		}

		Nt_Iarray(int len)
		{
			_array = (int *)malloc(len *sizeof(int));
			memset(_array, 0, len *sizeof(int));
			length = len;
			is_pointer_owner = true;
		}

		~Nt_Iarray()
		{
			this->!Nt_Iarray();
		}

		!Nt_Iarray()
		{
			if (is_pointer_owner == true && _array != NULL)
			{
				//there might be a problem for this free
				free(_array);
			}
			_array = NULL;
		}

		[JsonIgnore]
		property int default[int]
		{
			int get(int index)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				}
				return _array[index];
			}
			void set(int index, int value)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				} 
				_array[index] = value;
			}
		}

		property int Length
		{
			int get()
			{
				return length;
			}
		}

		array<int>^ ToArray()
		{
			array<int>^ arr = gcnew array<int>(length);
			for (int i=0; i< length; i++)
			{
				arr[i] = _array[i];
			}
			return arr;
		}

	internal:
		[JsonIgnore]
		property int *NativePointer
		{
			int *get()
			{
				return _array;
			}
			
			void set(int *dptr)
			{
				if (is_pointer_owner == true && _array != NULL)
				{
					free(_array);
				}
				_array = dptr;
				is_pointer_owner = false;
			}
		}
	};



}





