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

    ///------------------------------------------------------------------------------------------
    /// <summary>
    /// Helper class
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    class Maths
    {

        private static String m_strZeros = "000000000000000000000000000000000";
        /// <summary>
        ///     The minus sign
        /// </summary>
        public const char m_cDASH = '-';

        /// <summary>
        ///     Determines the number of digits before the decimal point
        /// </summary>            
        /// <param name="strValue">
        ///     Value to be parsed
        /// </param>
        /// <returns>
        ///     Number of digits before the decimal point
        /// </returns>
        private static ushort NumOfDigitsBeforeDecimal(String strValue)
        {
            char cDecimal = '.';
            short nDecimalPosition = (short)strValue.IndexOf(cDecimal);
            ushort usSignificantDigits = 0;

            if (nDecimalPosition >= 0)
            {
                strValue = strValue.Substring(0, nDecimalPosition + 1);
            }

            for (int i = 0; i < strValue.Length; i++)
            {
                if (strValue[i] != m_cDASH) usSignificantDigits++;

                if (strValue[i] == cDecimal)
                {
                    usSignificantDigits--;
                    break;
                }
            }

            return usSignificantDigits;
        }

        /// <summary>
        ///     Rounds to a fixed number of significant digits
        /// </summary>
        /// <param name="d">
        ///     Number to be rounded
        /// </param>
        /// <param name="usSignificants">
        ///     Requested significant digits
        /// </param>
        /// <returns>
        ///     The rounded number
        /// </returns>
        public static String Round(char cDecimal, double d, int nSignificants)
        {
            StringBuilder value = new StringBuilder(Convert.ToString(d));

            int nDecimalPosition = value.ToString().IndexOf(cDecimal);
            int nAfterDecimal = 0;
            int nDigitsBeforeDecimalPoint = NumOfDigitsBeforeDecimal(value.ToString());

            if (nDigitsBeforeDecimalPoint == 1)
            {
                nAfterDecimal = (d == 0) ? nSignificants : value.Length - nDecimalPosition - 1;
            }
            else
            {
                if (nSignificants >= nDigitsBeforeDecimalPoint)
                {
                    nAfterDecimal = nSignificants - nDigitsBeforeDecimalPoint;
                }
                else
                {
                    double dPower = Math.Pow(10, nDigitsBeforeDecimalPoint - nSignificants);
                    d = dPower * (long)(d / dPower);
                }
            }

            double dRounded = Math.Round(d, nAfterDecimal);
            StringBuilder result = new StringBuilder();

            result.Append(dRounded);

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(dRounded))) + 1);
            dRounded = dRounded / scale;

            string sRounded = dRounded.ToString();

            char[] charsToTrim1 = { '0', '-' };
            sRounded = sRounded.Trim(charsToTrim1);
            char[] charsToTrim2 = { '.' };
            sRounded = sRounded.Trim(charsToTrim2);

            int nDigits = sRounded.Length;

            // Add lagging zeros, if necessary:
            if (nDigits < nSignificants)
            {
                int nToAppend = nSignificants - nDigits;
                result.Append(m_strZeros.Substring(0, nToAppend));
            }

            return result.ToString();
        }
    }
    //-----------------------------------------------------------------------------------

    /// <summary>
    /// Interaction logic for DoublesBox.xaml
    /// </summary>
    public partial class DoublesBox : UserControl, INotifyPropertyChanged
    {
        /*  Enhancement: Feb 19, 2014
         *   
         *  We need to display the number using significant digits instead 
         *  of just decimal places. Here are the details:
         *  
         *  x = actual original number
         *  d = number of significant digits
         *  n = floor(log10(x))
         *  m = d-1-n
         *  y = number to display
         *    = round(x*10^m) * (10^-m)
         *    
         *  Then take y and run it through the existing formatter
         * 
         */

        private bool SliderInitialized = false;
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
                max = value;
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
                min = value;
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
                fnumber = value;
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
                _format = value;                
                OnPropertyChanged("Format");
            }
        }

        private void SetMinMax()
        {
            if (AutoRange)
            {
                Minimum = Number - Number / RangeFactor;
                Maximum = Number + Number / RangeFactor;
            }
        }

        private void SetMinMax(double min, double max)
        {
            Minimum = min;
            Maximum = max;
        }

        public string ToFormatted(double number)
        {
            return number.ConvertToSignificantDigits(SignificantDigits, SNLowerThreshold, SNUpperThreshold);
        }

        public double ToDisplayNumber()
        {
            if (Number <= 0)
                return Number;

            double result = 1;
            double logvalue = Math.Log10(Number);
            double n = Math.Floor(logvalue);
            double m = SignificantDigits - 1 - n;

            double temp1 = Number * (Math.Pow(10, m));
            double temp2 = Math.Round(temp1);

            double multiplier = (Math.Pow(10,-m));
            result = temp2 * multiplier;

            return result;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DoublesBox()
        {
            InitializeComponent();
            Format = "-";
            FNumber = "0.000";
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
            if (IsReadOnly)
                return;

            TextBox tb = sender as TextBox;
            tb.Text = Number.ToString();
        }

        private void tbFNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string token = tb.Text;
            token = GetNumericChars(token);
            double d = 0 ;
            try
            {
                d = double.Parse(token);
            }
            catch
            {
                MessageBox.Show("Please enter a valid number.");
                //throw new Exception("Invalid number entered");
                return;
            }

            if (Number != d)
            {
                Number = d;
                SetMinMax();
            }

            tb.Text = ToFormatted(ToDisplayNumber());
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
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return;
                }

                double currval = (double)GetValue(NumberProperty);
                double newval = value;

                if (currval != newval)
                {
                    //slFNumber.Value = newval;
                    SetValue(NumberProperty, newval);
                    //OnPropertyChanged("Number");
                }
                FNumber = ToFormatted(ToDisplayNumber());
                if (!SliderInitialized)
                {
                    SetMinMax();
                    SliderInitialized = true;
                }
                else
                {
                    OnPropertyChanged("Number");
                }
            }
        }
        public static void NumberPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DoublesBox uc = d as DoublesBox;
            uc.Number = (double)(e.NewValue);
        }

        //SIGNIFICANT DIGITS        
        public static DependencyProperty SignificantDigitsProperty = DependencyProperty.Register("SignificantDigits", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(3, SignificantDigitsPropertyChanged));
        public int SignificantDigits
        {
            get { return (int)GetValue(SignificantDigitsProperty); }
            set
            {
                SetValue(SignificantDigitsProperty, value);
                FNumber = ToFormatted(ToDisplayNumber());

                OnPropertyChanged("SignificantDigits");
            }
        }
        private static void SignificantDigitsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SignificantDigits = (int)(e.NewValue);
        }


        //DECIMAL PLACES        
        public static DependencyProperty DecimalPlacesProperty = DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(3, DecimalPlacesPropertyChanged));
        public int DecimalPlaces
        {
            get { return (int)GetValue(DecimalPlacesProperty); }
            set
            {
                SetValue(DecimalPlacesProperty, value);
                FNumber = ToFormatted(ToDisplayNumber());

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
                if (value <= 0)
                {
                    SetValue(RangeFactorProperty, 1);
                }
                else
                {
                    SetValue(RangeFactorProperty, value);
                }
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
                FNumber = ToFormatted(ToDisplayNumber());
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
                FNumber = ToFormatted(ToDisplayNumber());
            }
        }
        private static void SNUpperThresholdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SNUpperThreshold = (double)(e.NewValue);
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

        //TEXTFIELDWIDTH          
        public static DependencyProperty TextFieldWidthProperty = DependencyProperty.Register("TextFieldWidth", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(100, TextFieldWidthPropertyChanged));
        public int TextFieldWidth
        {
            get { return (int)GetValue(TextFieldWidthProperty); }
            set
            {
                SetValue(TextFieldWidthProperty, value);
                stpControl.Width = value;
                if (SliderEnabled) {
                    stpControl.Width += slFNumber.Width;                
                    stpMainPanel.Width = stpControl.Width + 10;
                }
                OnPropertyChanged("TextFieldWidth");
            }
        }
        private static void TextFieldWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.TextFieldWidth = (int)(e.NewValue);
        }

        //SLIDERWIDTH          
        public static DependencyProperty SliderWidthProperty = DependencyProperty.Register("SliderWidth", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(100, SliderWidthPropertyChanged));
        public int SliderWidth
        {
            get { return (int)GetValue(SliderWidthProperty); }
            set
            {
                SetValue(SliderWidthProperty, value);

                if (SliderEnabled)
                {
                    stpControl.Width = value;
                    stpControl.Width += tbFNumber.Width;
                    stpMainPanel.Width = stpControl.Width + 10;
                }
                else
                {
                    stpControl.Width = tbFNumber.Width;
                    stpMainPanel.Width = stpControl.Width + 10;
                }
                OnPropertyChanged("SliderWidth");
            }
        }
        private static void SliderWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.SliderWidth = (int)(e.NewValue);
        }

        //TEXTBORDERTHICKNESS          
        public static DependencyProperty TextBorderThicknessProperty = DependencyProperty.Register("TextBorderThickness", typeof(int), typeof(DoublesBox), new FrameworkPropertyMetadata(1, TextBorderThicknessPropertyChanged));
        public int TextBorderThickness
        {
            get { return (int)GetValue(TextBorderThicknessProperty); }
            set
            {
                SetValue(TextBorderThicknessProperty, value);

                if (SliderEnabled)
                {
                    stpControl.Width = value;
                    stpControl.Width += tbFNumber.Width;
                    stpMainPanel.Width = stpControl.Width + 10;
                }
                else
                {
                    stpControl.Width = tbFNumber.Width;
                    stpMainPanel.Width = stpControl.Width + 10;
                }
                OnPropertyChanged("TextBorderThickness");
            }
        }
        private static void TextBorderThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.TextBorderThickness = (int)(e.NewValue);
        }

        //ISREADONLY          
        public static DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(DoublesBox), new FrameworkPropertyMetadata(false, IsReadOnlyPropertyChanged));
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set
            {
                SetValue(IsReadOnlyProperty, value);
                stpControl.Width = 220;
                stpMainPanel.Width = 230;
                if (value == false)
                {
                    stpControl.Width = 110;
                    stpMainPanel.Width = 120;
                }
                OnPropertyChanged("IsReadOnly");
            }
        }
        private static void IsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.IsReadOnly = (bool)(e.NewValue);
        }

        //AUTORANGE - IF TRUE, MIN AND MAX ARE DETERMINED BY NUMBER      
        public static DependencyProperty AutoRangeProperty = DependencyProperty.Register("AutoRange", typeof(bool), typeof(DoublesBox), new FrameworkPropertyMetadata(true, AutoRangePropertyChanged));
        public bool AutoRange
        {
            get { return (bool)GetValue(AutoRangeProperty); }
            set
            {
                SetValue(AutoRangeProperty, value);
                OnPropertyChanged("AutoRange");

                if (value == true)
                {
                    SetMinMax();
                }
                else
                {
                    SetMinMax(AbsMinimum, AbsMaximum); 
                }
            }
        }
        private static void AutoRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            DoublesBox uc = d as DoublesBox;
            uc.AutoRange = (bool)(e.NewValue);
        }

        //ABSMINIMUM - Used for slider if not auto range
        /// <summary>
        /// If automatic range calculation is not desired, use this to indicate slider min value.
        /// </summary>
        public static DependencyProperty AbsMinimumProperty = DependencyProperty.Register("AbsMinimum", typeof(double), typeof(DoublesBox), new FrameworkPropertyMetadata(1.0, AbsMinimumPropertyChanged));
        public double AbsMinimum
        {
            get { return (double)GetValue(AbsMinimumProperty); }
            set
            {
                SetValue(AbsMinimumProperty, value);
                if (!AutoRange)
                {
                    Minimum = value;
                }
                OnPropertyChanged("AbsMinimum");
            }
        }
        public static void AbsMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DoublesBox uc = d as DoublesBox;
            uc.AbsMinimum = (double)(e.NewValue);
        }

        //ABSMAXIMUM - Used for slider if not auto range
        /// <summary>
        /// If automatic range calculation is not desired, use this to indicate slider max value.
        /// </summary>
        public static DependencyProperty AbsMaximumProperty = DependencyProperty.Register("AbsMaximum", typeof(double), typeof(DoublesBox), new FrameworkPropertyMetadata(100.0, AbsMaximumPropertyChanged));
        public double AbsMaximum
        {
            get { return (double)GetValue(AbsMaximumProperty); }
            set
            {
                SetValue(AbsMaximumProperty, value);
                if (!AutoRange)
                {
                    Maximum = value;  //SetMinMax();
                }
                OnPropertyChanged("AbsMaximum");
            }
        }
        public static void AbsMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DoublesBox uc = d as DoublesBox;
            uc.AbsMaximum = (double)(e.NewValue);
        }

        



#if USE_CAPTION
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
#endif


    }

    public class NumberToFormattedNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string s = "";
            double d = (double) value;

            DoublesBox db = new DoublesBox();
            db.Number = d;

            s = db.ToFormatted(d);
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}









