using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace DaphneUserControlLib
{
    public class DoublesValidator : ValidationRule
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }

        public DoublesValidator()
        {
            Minimum = -100000000000000000000.0;
            Maximum = 100000000000000000000.0;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, "Value cannot be empty.");
            else
            {
                string strValue = value.ToString();
                strValue = strValue.Trim();

                if (strValue.Length <= 0)
                    return new ValidationResult(false, "Value cannot be blank.");

                double dValue;
                bool result = double.TryParse(strValue, out dValue);
                if (result == false)
                    return new ValidationResult(false, "Invalid Value entered.");

                //if (dValue < Minimum || dValue > Maximum)
                //    return new ValidationResult(false, "Value must be in the range: " + Minimum + " to " + Maximum );
                if (dValue < 0)
                    return new ValidationResult(false, "Value must be greater than 0.");

            }
            return ValidationResult.ValidResult;
        }
    }
}
