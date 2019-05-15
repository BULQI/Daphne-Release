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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace DaphneGui
{
    /// <summary>
    /// This class extends StackPanel nad provides an IsReadOnly flag to allow the programmer
    /// to make all the controls (that have an IsReadOnly property inside a StackPanel read only).
    /// So the programmer does not have to do determine separately whether each control should be
    /// read only or no.
    /// </summary>
    public class DaphneStackPanel : StackPanel
    {
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DaphneStackPanel),
            new PropertyMetadata(new PropertyChangedCallback(OnIsReadOnlyChanged)));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DaphneStackPanel)d).OnIsReadOnlyChanged(e);
        }

        protected virtual void OnIsReadOnlyChanged(DependencyPropertyChangedEventArgs e)
        {
            this.SetIsEnabledOfChildren();
        }

        public DaphneStackPanel()
        {
            this.Loaded += new RoutedEventHandler(DaphneStackPanel_Loaded);
        }

        void DaphneStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetIsEnabledOfChildren();
        }

        private void SetIsEnabledOfChildren()
        {
            //foreach (UIElement child in this.Children)
            //{
            //    var readOnlyProperty = child.GetType().GetProperties().Where(prop => prop.Name.Equals("IsReadOnly")).FirstOrDefault();
            //    if (readOnlyProperty != null)
            //    {
            //        readOnlyProperty.SetValue(child, this.IsReadOnly, null);
            //    }
            //}

            List<UIElement> elements = new List<UIElement>();
            GetLogicalChildCollection(this, elements);

            foreach (UIElement child in elements)
            {
                
                var readOnlyProperty = child.GetType().GetProperties().Where(prop => prop.Name.Equals("IsReadOnly")).FirstOrDefault();
                if (readOnlyProperty != null)
                {
                    readOnlyProperty.SetValue(child, this.IsReadOnly, null);
                }
                if (child.GetType() == typeof(ComboBox))
                {
                    ((ComboBox)child).IsEnabled = !IsReadOnly;
                }
                if (child.GetType() == typeof(Button))
                {
                    ((Button)child).IsEnabled = !IsReadOnly;
                }

            }
        }

        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            List<T> logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }
    }
}
