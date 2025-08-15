using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Bloodpebble
{

    public static class StringExtensions
    {
        private static StringBuilder m_stringBuilder = new StringBuilder();

        public static string Bold(this string _string)
        {
            m_stringBuilder.Clear();
            m_stringBuilder.Append("<b>");
            m_stringBuilder.Append(_string);
            m_stringBuilder.Append("</b>");
            return m_stringBuilder.ToString();
        }

        public static string Italic(this string _string)
        {
            m_stringBuilder.Clear();
            m_stringBuilder.Append("<i>");
            m_stringBuilder.Append(_string);
            m_stringBuilder.Append("</i>");
            return m_stringBuilder.ToString();
        }

        public static string Colorify(this string _string, Color _color)
        {
            m_stringBuilder.Clear();
            m_stringBuilder.Append("<color=#");
            m_stringBuilder.Append(_color.ToHexString());
            m_stringBuilder.Append('>');
            m_stringBuilder.Append(_string);
            m_stringBuilder.Append("</color>");
            return m_stringBuilder.ToString();
        }

        public static string Size(this string _string, int size)
        {
            return $"<size={size}>{_string}";
        }

        public static string Emphasize(this string _string)
        {
            return _string.Colorify(ExtendedColor.AntiqueWhite);
        }

        public static string White(this string _string)
        {
            return _string.Colorify(ExtendedColor.LightGray);
        }

        public static string Error(this string _string)
        {
            return (_string).Colorify(ExtendedColor.Crimson);
            return (_string + " ㄨ").Colorify(ExtendedColor.Crimson);
        }

        public static string Warning(this string _string)
        {
            return (_string).Colorify(ExtendedColor.ReunoYellow);
            return (_string + " ⚠").Colorify(ExtendedColor.ReunoYellow);
        }

        public static string Success(this string _string)
        {
            return (_string).Colorify(ExtendedColor.MediumSeaGreen);
            return (_string + " ✓").Colorify(ExtendedColor.LimeGreen);
        }

        public static string FriendlyTeam(this string _string)
        {
            return Success(_string);
        }

        public static string EnemyTeam(this string _string)
        {
            return Error(_string);
        }

        public static string NeutralTeam(this string _string)
        {
            return Warning(_string);
        }

        public static string ConvertToEngineeringNotation(this float _value)
        {
            _value = (int)_value;

            if (_value == 0)
                return "0"; // Return 0 if the input value is zero

            // Define the suffixes for engineering notation
            string[] suffixes = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(_value)) / 3); // Calculate the exponent

            // Ensure the exponent is within the range of the suffixes array
            exponent = Math.Max(-suffixes.Length + 1, Math.Min(exponent, suffixes.Length - 1));

            float newValue = _value / (float)Math.Pow(10, exponent * 3); // Calculate the new value
            string formattedValue = newValue.ToString("0.0"); // Format to one decimal place

            if (_value >= 10000)
                return $"{formattedValue}{suffixes[exponent]}"; // Return the value with the appropriate suffix
            else
                return _value.ToString();
        }
    }

}
