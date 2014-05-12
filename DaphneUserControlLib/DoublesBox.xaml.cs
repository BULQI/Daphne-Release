using System;
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
    /// Interaction logic for DoublesBox.xaml
    /// </summary>
    public partial class DoublesBox : UserControl, INotifyPropertyChanged
    {
        private double min;                         //minimum value allowed
        private double max;                         //maximum value allowed 
        public double Tick { get; set; }            //slider/edit box increment if applicable
        private string _format;                     //format specifier string
        private string fnumber;                     //string that represents value after formatting is applied

        public double Maximum
        {
            get
            {
                return max;
            }
            set
            {
                max = Number + Number / RangeFactor;
                OnPropertyChanged("Maximum");
            }
        }

        public double Minimum
        {
            get
            {
                return min;
            }
            set
            {
                min = Number - Number / RangeFactor;
                OnPropertyChanged("Minimum");
            }
        }

        public string FNumber
        {
            get
            {
                return fnumber;
            }
            set
            {
                fnumber = string.Format(_format, Number);
                OnPropertyChanged("FNumber");
            }
        }

        public string Format
        {
            get
            {
                return _format;
            }
            set
            {
                _format = "{0:N" + DecimalPlaces.ToString() + "}";
                if (Number >= SNUpperThreshold)
                {
                    if (DecimalPlaces == 0)
                        DecimalPlaces++;

                    _format = "{0:#.";
                    for (int i = 0; i < DecimalPlaces; i++)
                    {
                        _format += "0"; //#
                    }

                    _format += "e+00}";
                }
                else if (Number <= SNLowerThreshold && Number > 0)
                {
                    if (DecimalPlaces == 0)
                        DecimalPlaces++;

                    _format = "{0:#.";
                    for (int i = 0; i < DecimalPlaces; i++)
                    {
                        _format += "#";
                    }

                    _format += "e-00}";
                }
                OnPropertyChanged("Format");
            }
        }

        private void SetMinMax()
        {
            Minimum = 1;
            Maximum = 1;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DoublesBox()
        {
            InitializeComponent();
            //stpMainPanel.DataContext = this;
            //DataContext = this;
            //Number = 3.14159;
            //_decimal_places = 3;
            //SNUpperThreshold = 100;
            //SNLowerThreshold = 0.01;
            Format = "-";
            SetMinMax();
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

        private void tbFNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.Text = Number.ToString();
        }

        private void tbFNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string token = tb.Text;
            token = GetNumericChars(token);
            double d = double.Parse(token);
            Number = d;
            SetMinMax();
        }

        private string GetNumericChars(string input)
        {
            var sb = new StringBuilder();
            string goodChars = "0123456789.eE+-";
            foreach (var c in input)
            {
                if (goodChars.IndexOf(c) >= 0)
                    sb.Append(c);
            }
            string output = sb.ToString();
            return output;
        }

        private void slFNumber_LostMouseCapture(object sender, MouseEventArgs e)
        {
            SetMinMax();
        }


        //------------------------------------------------------------------------
        ///Dependency Properties
        ///

        //NUMBER = ACTUAL DOUBLE VALUE
        public static DependencyProperty NumberProperty = DependencyProperty.Register("Number", typeof(double), typeof(DoublesBox), new FrameworkPropertyMetadata(NumberPropertyChanged));
        public double Number
        {
            get { return (double)GetValue(NumberProperty); }
            set
            {
                SetValue(NumberProperty, value);
                Format = "";
                FNumber = string.Format(_format, Number);
                OnPropertyChanged("Number");
            }
        }
        public static void NumberPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DoublesBox uc = d as DoublesBox;
            uc.Number = (double)(e.NewValue);
        }

        //DECIMAL PLACES        
        public static DependencyProperty DecimalPlacesProperty = DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(3, DecimalPlacesPropertyChanged));
        public int DecimalPlaces
        {
            get { return (int)GetValue(DecimalPlacesProperty); }
            set
            {
                SetValue(DecimalPlacesProperty, value);

                Format = "";
                FNumber = string.Format(_format, Number);

                OnPropertyChanged("DecimalPlaces");
            }
        }
        private static void DecimalPlacesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.DecimalPlaces = (int)(e.NewValue);
        }

        //RANGE FACTOR        
        public static DependencyProperty RangeFactorProperty = DependencyProperty.Register("RangeFactor", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(2, RangeFactorPropertyChanged));
        public int RangeFactor
        {
            get { return (int)GetValue(RangeFactorProperty); }
            set
            {
                SetValue(RangeFactorProperty, value);
                SetMinMax();
                OnPropertyChanged("RangeFactor");
            }
        }
        private static void RangeFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.RangeFactor = (int)(e.NewValue);
        }

        
        //SCIENTIFIC NOTATION LOWER THRESHOLD       
        public static DependencyProperty SNLowerThresholdProperty = DependencyProperty.Register("SNLowerThreshold", typeof(double), typeof(DoublesBox), new FrameworkPropertyMetadata(0.01, SNLowerThresholdPropertyChanged));
        public double SNLowerThreshold
        {
            get { return (double)GetValue(SNLowerThresholdProperty); }
            set
            {
                SetValue(SNLowerThresholdProperty, value);

                Format = "";
                FNumber = string.Format(_format, Number);

                OnPropertyChanged("SNLowerThreshold");
            }
        }
        private static void SNLowerThresholdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SNLowerThreshold = (double)(e.NewValue);
        }

        
        //SCIENTIFIC NOTATION UPPER THRESHOLD       
        public static DependencyProperty SNUpperThresholdProperty = DependencyProperty.Register("SNUpperThreshold", typeof(double), typeof(DoublesBox), new FrameworkPropertyMetadata(100.0, SNUpperThresholdPropertyChanged));
        public double SNUpperThreshold
        {
            get { return (double)GetValue(SNUpperThresholdProperty); }
            set
            {
                SetValue(SNUpperThresholdProperty, value);
                OnPropertyChanged("SNUpperThreshold");
                Format = "";
                FNumber = string.Format(Format, Number);
            }
        }
        private static void SNUpperThresholdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SNUpperThreshold = (double)(e.NewValue);
        }

        
        //CAPTION          
        public static DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof(string), typeof(DoublesBox), new FrameworkPropertyMetadata("CAP", CaptionPropertyChanged));
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set
            {
                SetValue(CaptionProperty, value);
                SetMinMax();
                OnPropertyChanged("Caption");
            }
        }
        private static void CaptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.Caption = (string)(e.NewValue);
        }

        //SLIDERENABLED          
        public static DependencyProperty SliderEnabledProperty = DependencyProperty.Register("SliderEnabled", typeof(bool), typeof(DoublesBox), new FrameworkPropertyMetadata(true, SliderEnabledPropertyChanged));
        public bool SliderEnabled
        {
            get { return (bool)GetValue(SliderEnabledProperty); }
            set
            {
                SetValue(SliderEnabledProperty, value);
                stpControl.Width = 220;
                stpMainPanel.Width = 230;
                if (value == false)
                {
                    stpControl.Width = 110;
                    stpMainPanel.Width = 120;
                }
                OnPropertyChanged("SliderEnabled");
            }
        }
        private static void SliderEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SliderEnabled = (bool)(e.NewValue);
        }
    }
}









