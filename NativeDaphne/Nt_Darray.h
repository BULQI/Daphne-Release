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
		int capacity;
		int length;
		//indicate pointer is replaced with external pointer
		bool is_pointer_owner;
		double *_array;
		List<Nt_Darray^>^ component;
	public:

		//default constructor, servers as collection
		Nt_Darray()
		{
			length = 0;
			capacity = 0;
			is_pointer_owner = true;
			_array = NULL;
			component = gcnew List<Nt_Darray^>();
		}

		//constructor with fixed array length
		Nt_Darray(int len)
		{
			_array = (double *)_aligned_malloc(len *sizeof(double), 32);
			memset(_array, 0, len *sizeof(double));
			length = len;
			capacity = len;
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
				_aligned_free(_array);
				_array = NULL;
			}
		}

		//set as a colleciton
		void InitializeAsCollection()
		{
			component = gcnew List<Nt_Darray^>();
			length = 0;
		}

		//expand the memory length
		void resize(int len)
		{
			if (is_pointer_owner == false)
			{
				throw gcnew Exception("Error resize memory the object does not own");
			}
			if (len > capacity)
			{
				capacity = len;
				_array = (double *)_aligned_realloc(_array, capacity * sizeof(double), 32);
			}
			length = len;
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

		void AddComponent(Nt_Darray^ src)
		{
			if (component == nullptr)
			{
				throw gcnew Exception("Object is not of collection type");
			}
			int item_len = src->Length;
			if (component->Count > 0 && component[0]->Length != item_len)
			{
				throw gcnew Exception("Component is not of same length");
			}

			if (length + src->Length > capacity)
			{
				capacity = NtUtility::GetAllocSize(length + src->Length, capacity);
				_array = (double *)_aligned_realloc(_array, capacity * sizeof(double), 32);
				double *head = _array;
				for (int i=0; i< component->Count; i++)
				{
					component[i]->NativePointer = head;
					head += item_len;
				}
			}
			double *dstptr = _array + component->Count * item_len;
			for (int i=0; i< item_len; i++)
			{
				dstptr[i] = src[i];
			}
			src->NativePointer = dstptr;
			length += item_len;
			component->Add(src);
		}

		//when a component is removed from the collection, the component's storage
		//is also reallocated.
		//return the index of the component before removal
		int RemoveComponent(Nt_Darray^ src)
		{
			if (component == nullptr || component->Count == 0)
			{
				throw gcnew Exception("Collection empty");
			}
			int index = (int)(src->NativePointer - _array)/src->Length;
			if (index < 0 || index > component->Count || component[index] != src)
			{
				throw gcnew Exception("object not found");
			}
			Nt_Darray^ last_item = component[component->Count - 1];
			if (src != last_item)
			{
				src->MemSwap(last_item);
				component[index] = last_item;
			}
			component->RemoveAt(component->Count-1);
			length -= src->Length;
			src->detach();
			return index;
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

		property int ComponentCount
		{
			int get()
			{
				return component == nullptr ? 0 : component->Count;
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

			//only call lower level for long arrays.
			if (length <= 4)
			{
				for (int i=0; i<length; i++)
				{
					_array[i] += dst[i];
				}
				return this;
			}
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
					_aligned_free(_array);
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
			double *tmp = (double *)_aligned_malloc(length *sizeof(double), 32);
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
			_array = (int *)_aligned_malloc(len *sizeof(int), 16);
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
				_aligned_free(_array);
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
					_aligned_free(_array);
				}
				_array = dptr;
				is_pointer_owner = false;
			}
		}

		void MemSwap(Nt_Iarray^ item)
		{
			if (item->Length != this->length)
			{
				throw gcnew Exception("Nt_Iarray.memswap, item length mismatch");
			}
			//this is enforced to avoid memory leak
			//need to handle ownership if swaping item with different ownership is needed.
			if (item->is_pointer_owner != this->is_pointer_owner)
			{
				throw gcnew Exception("Nt_Iarray.memswap only allowed for item in same collection");
			}
			int *array2 = item->NativePointer;
			for (int i=0; i< length; i++)
			{
				int tmp = _array[i];
				_array[i] = array2[i];
				array2[i] = tmp;
			}
			item->NativePointer = _array;
			this->_array = array2;
		}

		//allocate and take owner ship of the pointer
		//called when remoe this object from a collection
		void detach()
		{
			if (is_pointer_owner == true || _array == NULL || length == 0)return;
			int *tmp = (int *)_aligned_malloc(length *sizeof(int), 16);
			memcpy(tmp, _array, length * sizeof(int));
			_array = tmp;
			is_pointer_owner = true;
		}
	};

}





