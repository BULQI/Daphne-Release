using System;
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
