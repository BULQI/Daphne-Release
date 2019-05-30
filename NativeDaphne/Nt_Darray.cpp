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
#include <stdlib.h>
#include <malloc.h>

#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace Newtonsoft::Json::Linq;

namespace NativeDaphne
{

	void DoubleArraySerializer::WriteJson(JsonWriter^ writer, Object^ value, JsonSerializer ^serializer) 
	{
		//if (value->GetType()->IsGenericType == false)return;
		Nt_Darray^ darray = dynamic_cast<Nt_Darray^>(value);
		if (darray == nullptr)return;
		array<double> ^darr = darray->ArrayCopy;
		writer->WriteStartArray();
		for (int i=0; i< darr->Length; i++)
		{
			writer->WriteValue(darr[i]);
		}
		writer->WriteEndArray();
	}

	Object^ DoubleArraySerializer::ReadJson(JsonReader^ reader, Type^ objectType, Object^ existingValue, JsonSerializer^ serializer) 
	{
		//auto xx = JObject::Parse(reader);
        JArray^ jArray = JArray::Load(reader);
		int count = jArray->Count;
		if (count == 0)
		{
			return gcnew Nt_Darray();
		}
		Nt_Darray^ darray = gcnew Nt_Darray(count);
		for (int i=0; i<count; i++)
		{
			darray[i] = (double)jArray[i];
		}
        return darray;
	}
}