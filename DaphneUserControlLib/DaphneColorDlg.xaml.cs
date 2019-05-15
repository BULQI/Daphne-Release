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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace DaphneUserControlLib
{
    /// <summary>
    /// Interaction logic for DaphneColorDlg.xaml
    /// </summary>
    public partial class DaphneColorDlg : UserControl, INotifyPropertyChanged
    {
        byte rvalue;     //red
        byte gvalue;     //green
        byte bvalue;     //blue
        Color xcolor;    //rgb color
        SolidColorBrush xbrush; //converted from xcolor

        
        public DaphneColorDlg()
        {
            InitializeComponent();
            RValue = 100;
            GValue = 0;
            BValue = 100;
            xbrush = new SolidColorBrush(Colors.Red);
        }

        public byte RValue
        {
            get
            {
                return rvalue;
            }
            set
            {
                rvalue = value;
                XColor = System.Windows.Media.Color.FromRgb(rvalue, GValue, BValue);
                OnPropertyChanged("RValue");
            }
        }
        public byte GValue
        {
            get
            {
                return gvalue;
            }
            set
            {
                gvalue = value;
                XColor = System.Windows.Media.Color.FromRgb(RValue, gvalue, BValue);
                OnPropertyChanged("GValue");
            }
        }
        public byte BValue
        {
            get
            {
                return bvalue;
            }
            set
            {
                bvalue = value;
                XColor = System.Windows.Media.Color.FromRgb(RValue, GValue, bvalue);
                OnPropertyChanged("BValue");
            }
        }

        public Color XColor
        {
            get
            {
                return xcolor;
            }
            set
            {
                xcolor = value;
                XBrush = new SolidColorBrush(xcolor);
                OnPropertyChanged("XColor");
            }
        }

        public SolidColorBrush XBrush
        {
            get
            {
                return xbrush;
            }
            set
            {
                xbrush = value;
                OnPropertyChanged("XBrush");
            }
        }
        

        ///
        //Notification handling
        /// 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}
