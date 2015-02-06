using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaphneUserControlLib
{
    public static class UserControlExtensions
    {
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

        public static string ConvertToSignificantDigits(this double number, int digits, double lThresh = 1e-20, double uThresh = 1e20)
        {
            string sZeroes = "000000000000000000000000000000";
            string sNum = number.ToString();

            

            //return sNum;
            //-----------------------------------------------------------------

            //If need scientific notation - positive exponent
            //if (number >= uThresh || number < 0 || (number >= 1 && number < lThresh))
            if (number >= uThresh || (number >= 1 && number < lThresh))
            {
                if (digits == 0)
                    digits++;

                string newFormat = "{0:0.";
                for (int i = 0; i < digits-1; i++)
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

                sNum = string.Format(newFormat, number);
            }
            //Need scientific notation - negative exponent
            else if (number <= lThresh && number > 0 && number < 1)
            {
                if (digits == 0)
                    digits++;

                string newFormat = "{0:0.";
                for (int i = 0; i < digits-1; i++)
                {
                    newFormat += "0";  //"#";
                }
                newFormat += "E-00}";
                sNum = string.Format(newFormat, number);
            }
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
                    int nDiff = digits - nLen;
                    if (nDiff > 0)
                    {
                        sNum = sNum + "." + sZeroes.Substring(0, nDiff);
                    }
                }
            }

            ////    Format = newFormat;
            ////    result = string.Format(Format, number);








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
