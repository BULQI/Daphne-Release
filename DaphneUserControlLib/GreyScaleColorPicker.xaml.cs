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
    //color enums
    ////public enum ColorList { White, LightGrey, Grey, DarkGrey, Black, Custom }
    public enum ColorList { White, LightGrey, Grey, DarkGrey, Black }



    /// <summary>
    /// Interaction logic for GreyScaleColorPicker.xaml
    /// </summary>
    public partial class GreyScaleColorPicker : UserControl, INotifyPropertyChanged
    {


        private ColorList selectedColorEnum;
        public ColorList SelectedColorEnum
        {
            get
            {
                return selectedColorEnum;
            }
            set
            {
                if (value != selectedColorEnum)
                {
                    selectedColorEnum = value;
                    ////ColorListEnumToColorConverter conv = new ColorListEnumToColorConverter();
                    ////Color cc = (Color)conv.Convert(value, typeof(Color), CustomColor, System.Globalization.CultureInfo.CurrentCulture);
                    ////ActualColor = cc;
                }
            }
        }

        ////public Color ActualColor { get; set; }

        ////private Color customColor;
        ////public Color CustomColor
        ////{
        ////    get
        ////    {
        ////        return customColor;
        ////    }
        ////    set
        ////    {
        ////        if (customColor != value)
        ////        {
        ////            customColor = value;
        ////            if (SelectedColorEnum == ColorList.Custom)
        ////            {
        ////                ActualColor = customColor;
        ////            }
        ////            OnPropertyChanged("CustomColor");
        ////        }
        ////    }
        ////}

        public GreyScaleColorPicker()
        {
            SelectedColorEnum = ColorList.Black;
            SelectedColor = Colors.Black;
            ////CustomColor = Colors.Black;
            InitializeComponent();
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

        private void cbBackColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColorList index = (ColorList)e.AddedItems[0];

            switch (index)
            {
                case ColorList.White:
                    SelectedColor = Color.FromArgb(255, 255, 255, 255);
                    break;
                case ColorList.LightGrey:
                    SelectedColor = Color.FromArgb(255, 192, 192, 192);
                    break;
                case ColorList.Grey:
                    SelectedColor = Color.FromArgb(255, 128, 128, 128);
                    break;
                case ColorList.DarkGrey:
                    SelectedColor = Color.FromArgb(255, 64, 64, 64);
                    break;
                case ColorList.Black:
                    SelectedColor = Color.FromArgb(255, 0, 0, 0);
                    break;
                ////case ColorList.Custom:
                ////    CustomColor = Color.FromArgb(255, 0, 0, 0);
                ////    break;
                default:
                    break;
            }
        }


        //***********************************************************************************
        //Dependency Properties
        //

        //SELECTED COLOR        
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(Color), typeof(GreyScaleColorPicker), new FrameworkPropertyMetadata(Colors.White, SelectedColorPropertyChanged));
        public Color SelectedColor
        {
            get 
            {
                return (Color)GetValue(SelectedColorProperty);
            }
            set
            {
                SetValue(SelectedColorProperty, value);
                OnPropertyChanged("SelectedColor");
            }
        }
        private static void SelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            GreyScaleColorPicker uc = d as GreyScaleColorPicker;
            uc.SelectedColor = (Color)(e.NewValue);
        }

    }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(string))]
    public class ColorListEnumToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the MoleculeLocation enum...
        private List<string> _color_strings = new List<string>()
                                {
                                    "White",
                                    "Light Grey",
                                    "Grey",
                                    "Dark Grey",                                    
                                    "Black"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _color_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.White;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }

    /// <summary>
    /// Convert ColorList enum to SolidBrush for rectangle fills
    /// </summary>
    public class ColorListEnumToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromArgb(255, 255, 255, 255);
            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);

                switch (colEnum)
                {
                    case ColorList.White:
                        col = Color.FromArgb(255, 255, 255, 255);
                        break;
                    case ColorList.LightGrey:
                        col = Color.FromArgb(255, 194, 194, 194);
                        break;
                    case ColorList.Grey:
                        col = Color.FromArgb(255, 128, 128, 128);
                        break;
                    case ColorList.DarkGrey:
                        col = Color.FromArgb(255, 64, 64, 64);
                        break;
                    case ColorList.Black:
                        col = Color.FromArgb(255, 0, 0, 0);
                        break;
                    default:
                        break;
                }

                return new System.Windows.Media.SolidColorBrush(col);
            }
            catch
            {
                return new System.Windows.Media.SolidColorBrush(col);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class ColorListEnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return (int)ColorList.Black;

            try
            {
                int index = (int)value;
                return index;
            }
            catch
            {
                return 4;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.Black;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }

    public class ColorListEnumToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(0, 0, 0);

            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);

                switch (colEnum)
                {
                    case ColorList.White:
                        col = Color.FromRgb(255, 255, 255);
                        break;
                    case ColorList.LightGrey:
                        col = Color.FromRgb(194, 194, 194);
                        break;
                    case ColorList.Grey:
                        col = Color.FromRgb(128, 128, 128);
                        break;
                    case ColorList.DarkGrey:
                        col = Color.FromRgb(64, 64, 64);
                        break;
                    case ColorList.Black:
                        col = Color.FromRgb(0, 0, 0);
                        break;
                    default:
                        break;
                }

                return col;
            }
            catch
            {
                return col;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
}
