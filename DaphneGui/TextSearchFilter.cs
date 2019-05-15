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
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DaphneGui
{
	public class TextSearchFilter
	{
		public TextSearchFilter( 
			ICollectionView filteredView, 
			TextBox textBox )
		{
			string filterText = "";

			filteredView.Filter = delegate( object obj )				
			{
				if( String.IsNullOrEmpty( filterText ) )
					return true;

				string str = obj as string;
				if( String.IsNullOrEmpty( str ) )
					return false;

				int index = str.IndexOf(
					filterText,
					0,
					StringComparison.InvariantCultureIgnoreCase );

				return index > -1;
			};			

			textBox.TextChanged += delegate
			{
				filterText = textBox.Text;
				filteredView.Refresh();
			};
		}
	}
}