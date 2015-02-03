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
using System.Windows.Shapes;
using System.ComponentModel;

namespace DaphneGui
{

    public enum OutputColorList { White, LightGrey, Grey, DarkGrey, Black, Custom }

    /// <summary>
    /// Interaction logic for Save3DView.xaml
    /// </summary>
    public partial class Save3DView : Window, INotifyPropertyChanged
    {
        public string FileName { get; set; }

        public Save3DView()
        {
            InitializeComponent();
            DataContext = this;
            PredefColorIndex = OutputColorList.White;
            CustomColor = Colors.White;
        }

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

        private OutputColorList predefColorIndex;
        public OutputColorList PredefColorIndex
        {
            get
            {
                return predefColorIndex;
            }
            set
            {
                predefColorIndex = value;
                ColorListToColorConverter conv = new ColorListToColorConverter();
                Color cc = (Color)conv.Convert(value, typeof(Color), CustomColor, System.Globalization.CultureInfo.CurrentCulture);
                ActualColor = cc;
                //OnPropertyChanged("cellpopulation_color");
            }
        }

        private Color actualColor;   //this is used if predef_color is set to ColorList.Custom
        public Color ActualColor
        {
            get
            {
                return actualColor;
            }
            set
            {
                actualColor = value;
                OnPropertyChanged("ActualColor");
            }
        }

        private Color customColor;
        public Color CustomColor
        {
            get
            {
                return customColor;
            }
            set
            {
                customColor = value;
                if (PredefColorIndex == OutputColorList.Custom)
                {
                    ActualColor = customColor;
                    OnPropertyChanged("CustomColor");
                }
            }
        }

        private void btnFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Create a new save file dialog
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();

            // Sets the current file name filter string, which determines 
            // the choices that appear in the "Save as file type" or 
            // "Files of type" box in the dialog box.
            saveFileDialog1.Filter = "Bitmap (*.bmp)|*.bmp|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|TIFF (*.tif)|*.tif";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileName = saveFileDialog1.FileName;
                this.DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }
        }

        private void cbBackColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int x = 1;
            x++;
        }



    }
    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(OutputColorList), typeof(int))]
    public class ColorListToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 1;

            try
            {
                int index = (int)value;
                return index;
            }
            catch
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return OutputColorList.White;

            int idx = (int)value;
            return (OutputColorList)Enum.ToObject(typeof(OutputColorList), (int)idx);
        }
    }

    /// <summary>
    /// Convert color enum to type Color
    /// </summary>
    [ValueConversion(typeof(OutputColorList), typeof(Color))]
    public class ColorListToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(255, 255, 255);

            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                OutputColorList colEnum = (OutputColorList)Enum.ToObject(typeof(OutputColorList), (int)index);

                switch (colEnum)
                {
                    case OutputColorList.White:
                        col = Color.FromRgb(255, 255, 255);
                        break;
                    case OutputColorList.LightGrey:
                        col = Color.FromRgb(194, 194, 194);
                        break;
                    case OutputColorList.Grey:
                        col = Color.FromRgb(128, 128, 128);
                        break;
                    case OutputColorList.DarkGrey:
                        col = Color.FromRgb(64, 64, 64);
                        break;
                    case OutputColorList.Black:
                        col = Color.FromRgb(0, 0, 0);
                        break;
                    case OutputColorList.Custom:
                        col = (Color)parameter;
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

    /// <summary>
    /// Convert ColorList enum to SolidBrush for rectangle fills
    /// </summary>
    public class ColorListToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(255, 255, 255);
            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                OutputColorList colEnum = (OutputColorList)Enum.ToObject(typeof(OutputColorList), (int)index);

                switch (colEnum)
                {
                    case OutputColorList.White:
                        col = Color.FromRgb(255, 255, 255);
                        break;
                    case OutputColorList.LightGrey:
                        col = Color.FromRgb(194, 194, 194);
                        break;
                    case OutputColorList.Grey:
                        col = Color.FromRgb(128, 128, 128);
                        break;
                    case OutputColorList.DarkGrey:
                        col = Color.FromRgb(64, 64, 64);
                        break;
                    case OutputColorList.Black:
                        col = Color.FromRgb(0, 0, 0);
                        break;
                    case OutputColorList.Custom:
                        col = (Color)parameter;
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

}
