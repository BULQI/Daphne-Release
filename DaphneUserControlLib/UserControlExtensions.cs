using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaphneUserControlLib
{
    public static class UserControlExtensions
    {
        /// <summary>
        /// Extension method to calculate the number of significant digits for a given double value
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static int NumSignificantDigits(this double d)
        {            
            string sNumber = d.ToString();
            sNumber = sNumber.Replace("-", "");

            int len = sNumber.Length;

            char[] trimZeroes =  { '0' };
            char[] trimDecimal = { '.' };

            if (sNumber.Contains('.')) {
                sNumber = sNumber.TrimStart(trimZeroes);
                sNumber = sNumber.TrimStart(trimDecimal);
                sNumber = sNumber.TrimStart(trimZeroes);
                sNumber = sNumber.Replace(".", "");
                len = sNumber.Length;
            }
            else {
                sNumber = sNumber.Trim(trimZeroes);
                len = sNumber.Length;
            }

            return len;
        }

        /// <summary>
        /// Formats a number for display
        /// </summary>
        /// <param name="display_number">Number to format - contains correct num of sig digits except for zeroes</param>
        /// <param name="digits">Significant digits to show</param>
        /// <param name="decimalPlaces">How many decimal places to show</param>
        /// <param name="lThresh">Numbers less than lThresh will be displayed in scientific notation</param>
        /// <param name="uThresh">Numbers greater than uThresh will be displayed in scientific notation</param>
        /// <returns></returns>
        public static string ConvertToSignificantDigits(this double display_number, int digits, int decimalPlaces, double lThresh = 1e-20, double uThresh = 1e20)
        {
            string sZeroes = "000000000000000000000000000000";
            string sNum = display_number.ToString();

            double number = Math.Abs(display_number);

            //-----------------------------------------------------------------

            //Display as integer if number of decimal places wanted is 0
            if (decimalPlaces == 0) 
            {
                int displayInt = (int)display_number;
                sNum = displayInt.ToString();
            }
            //If need scientific notation - positive exponent
            else if (number >= uThresh || (number >= 1 && number < lThresh))
            {
                if (digits == 0)
                    digits++;

                string newFormat = "{0:0.";
                for (int i = 0; i < digits - 1; i++)
                {
                    newFormat += "0";
                }

                if (number >= 1 && number < 10)
                {
                    newFormat += "}";
                }
                else
                {
                    newFormat += "E+00}";
                }

                sNum = string.Format(newFormat, display_number);
            }
            //Need scientific notation - negative exponent
            else if (number <= lThresh && number > 0 && number < 1)
            {
                if (digits == 0)
                    digits++;

                string newFormat = "{0:0.";
                for (int i = 0; i < digits - 1; i++)
                {
                    newFormat += "0";  //"#";
                }
                newFormat += "E-00}";
                sNum = string.Format(newFormat, display_number);
            }
            //Don't need scientific notation
            else
            {                
                if (digits == 0)
                    digits++;

                if (sNum.Contains('.'))
                {
                    int sig = number.NumSignificantDigits();
                    int nDiff = digits - sig;
                    if (nDiff > 0)
                    {
                        sNum = sNum + sZeroes.Substring(0, nDiff);
                    }
                }
                else
                {
                    char[] trimZeroes = { '0' };
                    string sTemp = sNum.Replace("-", "");
                    sTemp = sTemp.TrimStart(trimZeroes);
                    int nLen = sTemp.Length;
                    int nDiff = decimalPlaces - nLen;
                    if (nDiff > 0)
                    {
                        sNum = sNum + "." + sZeroes.Substring(0, nDiff);
                    }
                }
            }

            return sNum;
        }

        public static double RoundToSignificantDigits(this double d, int digits)
        {
            if (d == 0.0)
            {
                return 0.0;
            }
            else
            {
                double leftSideNumbers = Math.Floor(Math.Log10(Math.Abs(d))) + 1;
                double scale = Math.Pow(10, leftSideNumbers);
                double result = scale * Math.Round(d / scale, digits, MidpointRounding.AwayFromZero);

                // Clean possible precision error.
                if ((int)leftSideNumbers >= digits)
                {
                    return Math.Round(result, 0, MidpointRounding.AwayFromZero);
                }
                else
                {
                    return Math.Round(result, digits - (int)leftSideNumbers, MidpointRounding.AwayFromZero);
                }
            }
        }

        //public static double RoundToSignificantDigits(this double d, int digits)
        //{
        //    if (d == 0)
        //        return 0;

        //    double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
        //    return scale * Math.Round(d / scale, digits);
        //}

    }    
}
