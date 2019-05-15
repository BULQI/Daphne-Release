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
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace Daphne
{
    public class ColorHelper
    {

        private static int SolidColorIndex = 0;
        private static int Cb8Index = 0;
        private static int Cb12Index = 0;

        //some different solid colors
        private static string[] SolidColorValues = new string[] { 
        "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF", 
        "#800000", "#008000", "#000080", "#808000", "#800080", "#008080", "#808080", 
        "#C00000", "#00C000", "#0000C0", "#C0C000", "#C000C0", "#00C0C0", "#C0C0C0", 
        "#400000", "#004000", "#404000", "#400040", "#004040", "#404040", 
        "#600000", "#006000", "#000060", "#606000", "#600060", "#006060", "#606060", 
        "#A00000", "#00A000", "#0000A0", "#A0A000", "#A000A0", "#00A0A0", "#A0A0A0", 
        "#E00000", "#00E000", "#0000E0", "#E0E000", "#E000E0", "#00E0E0", "#E0E0E0"};

        //ColorBrewer 12 color schemes
        //public static string[] Cb12Paried = new string[] {"#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00", "#cab2d6", "#6a3d9a", "#ffff99", "#b15928"};
        //public static string[] Cb12Set3 = new string[] {"#8dd3c7", "#ffffb3", "#bebada", "#fb8072", "#80b1d3", "#fdb462", "#b3de69", "#fccde5", "#d9d9d9", "#bc80bd", "#ccebc5", "#ffed6f" };


        public static List<List<string>> ColorBrewerEightList { get; set; }
        public static List<List<string>> colorBrewerTwelveList { get; set; }

        static ColorHelper() 
        {
            //ColorBrewer has 8 schemes for 8-class colors 
            //the first entry is the name fo the color series, without #, the rest of the entries are color values
            ColorBrewerEightList = new List<List<string>>();
            ColorBrewerEightList.Add(new List<string>() {"Accent", "#7fc97f", "#beaed4", "#fdc086", "#ffff99", "#386cb0", "#f0027f", "#bf5b17", "#666666" });
            ColorBrewerEightList.Add(new List<string>() {"Dark2", "#1b9e77", "#d95f02", "#7570b3", "#e7298a", "#66a61e", "#e6ab02", "#a6761d", "#666666" });
            ColorBrewerEightList.Add(new List<string>() {"Paired", "#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00" });
            ColorBrewerEightList.Add(new List<string>() {"Pastel1", "#fbb4ae", "#b3cde3", "#ccebc5", "#decbe4", "#fed9a6", "#ffffcc", "#e5d8bd", "#fddaec" });
            ColorBrewerEightList.Add(new List<string>() {"Pastel2", "#b3e2cd", "#fdcdac", "#cbd5e8", "#f4cae4", "#e6f5c9", "#fff2ae", "#f1e2cc", "#cccccc" });
            ColorBrewerEightList.Add(new List<string>() {"Set1", "#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#ffff33", "#a65628", "#f781bf" });
            ColorBrewerEightList.Add(new List<string>() {"Set2", "#66c2a5", "#fc8d62", "#8da0cb", "#e78ac3", "#a6d854", "#ffd92f", "#e5c494", "#b3b3b3" });
            ColorBrewerEightList.Add(new List<string>() {"Set3", "#8dd3c7", "#ffffb3", "#bebada", "#fb8072", "#80b1d3", "#fdb462", "#b3de69", "#fccde5" }); 
    
            colorBrewerTwelveList = new List<List<string>>();
            colorBrewerTwelveList.Add(new List<string>(){"Paired", "#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00", "#cab2d6", "#6a3d9a", "#ffff99", "#b15928"});    
            colorBrewerTwelveList.Add(new List<string>(){"Set3", "#8dd3c7", "#ffffb3", "#bebada", "#fb8072", "#80b1d3", "#fdb462", "#b3de69", "#fccde5", "#d9d9d9", "#bc80bd", "#ccebc5", "#ffed6f"});
        }


        /// <summary>
        /// pick a solid color from the sequence
        /// </summary>
        /// <returns></returns>
        public static Color pickASolidColor()
        {
            var item = SolidColorValues[SolidColorIndex];
            SolidColorIndex++;
            if (SolidColorIndex >= SolidColorValues.Length) SolidColorIndex = 0;
            return (Color)ColorConverter.ConvertFromString(item);
        }

        /// <summary>
        /// this is only resetting the solidcoor index
        /// </summary>
        /// <param name="i"></param>
        public static void resetColorPicker(int i = 0)
        {
            SolidColorIndex = i;
        }

        /// <summary>
        /// reset the colorindex to after the given color
        /// so that picked color would be different
        /// return true if the given color is found and the 
        /// pointer is reset to next color
        /// </summary>
        /// <param name="c"></param>
        public static bool resetColorPicker(Color c)
        {

            for (int i = 0; i < SolidColorValues.Length; i++)
            {
                var item = SolidColorValues[i];
                var color = (Color)ColorConverter.ConvertFromString(item);
                if (color == c)
                {
                    SolidColorIndex = i + 1;
                    if (SolidColorIndex >= SolidColorValues.Length) SolidColorIndex = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// using color brower for set of colors
        /// or rotating through the solid colors
        /// </summary>
        /// <param name="numColors"></param>
        /// <returns></returns>
        public static List<Color> pickASetOfColor(int numColors, bool colorbrewer_flag = true)
        {
            List<Color> colorlist = new List<Color>();
            if (colorbrewer_flag == true)
            {
                List<string> item = null;
                if (numColors <= 8)
                {
                    item = ColorBrewerEightList[Cb8Index];
                    Cb8Index = (Cb8Index + 1)%ColorBrewerEightList.Count;
                }
                else if (numColors <= 12)
                {
                    item = colorBrewerTwelveList[Cb12Index];
                    Cb12Index = (Cb12Index + 1)%colorBrewerTwelveList.Count;
                }
                else 
                {
                    throw(new NotImplementedException("No Colorbrewer color series have more than 12 colors"));
                }
                for (int i= 1; i<= numColors; i++)
                {
                    Color color = (Color)ColorConverter.ConvertFromString(item[i]);
                    colorlist.Add(color);
                }
            }
            else 
            {
                if (numColors > SolidColorValues.Length)
                {
                    throw(new NotImplementedException("Maximum number of default solid colors is " + SolidColorValues.Length));
                }
                for (int i=0; i<numColors; i++)
                {
                    Color color = (Color)ColorConverter.ConvertFromString(SolidColorValues[i]);
                    colorlist.Add(color);
                }
            }
            return colorlist;
        }

        /// <summary>
        /// pick a set of color shades
        /// </summary>
        /// <param name="base_color"></param>
        /// <param name="numOfShade"></param>
        /// <param name="max_color">maximize (sharpen) one color component</param>
        /// <returns></returns>
        public static List<Color> pickColorShades(Color base_color, int numOfShade, bool max_color = false)
        {
            if (max_color)
            {
                byte max_rgb = Math.Max(base_color.R, Math.Max(base_color.G, base_color.B));
                base_color.R = (base_color.R == max_rgb) ? (byte)255 : (byte)0;
                base_color.G = (base_color.G == max_rgb) ? (byte)255 : (byte)0;
                base_color.B = (base_color.B == max_rgb) ? (byte)255 : (byte)0;
            }
            
            List<Color> color_list = new List<Color>();
            ColorLab color_from = new ColorLab(base_color);
            ColorLab color_to = new ColorLab(Colors.White);
            numOfShade++;

            for (int n = 0; n < numOfShade-1; n++)
            {
                double r = (double)n / (double)(numOfShade - 1);
                double L = color_from.L + (color_to.L - color_from.L) * r;
                double a = color_from.a + (color_to.a - color_from.a) * r;
                double b = color_from.b + (color_to.b - color_from.b) * r;
                ColorLab newColor = new ColorLab(L, a, b);
                color_list.Add(newColor.ToRGBColor());
            }
            return color_list;
        }

        /// <summary>
        /// check given colors are colorbrewer colors
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        internal static bool IsColorBrewerColors(ObservableCollection<RenderColor> colors)
        {
            List<RenderColor> clist = colors.Where(x => x != null).ToList();
            List<List<string>> target_list = clist.Count < 12 ? ColorBrewerEightList : colorBrewerTwelveList;
            //don't compare that last one - it is added for default for overflow.
            if (clist.Count > 2 && clist.Count < 12)
            {
                clist.RemoveAt(clist.Count-1);
            }

            for (int i=0; i< target_list.Count; i++)
            {
                List<string> tlist = target_list[i];
                int j;
                for (j = 0; j < clist.Count; j++)
                {
                    if (clist[j].EntityColor != (Color)ColorConverter.ConvertFromString(tlist[j+1]))break;
                }
                if (j == clist.Count)return true;
            }
            return false;
        }
    }

    public class ColorLab
    {

        //algoirthm and value from http://www.easyrgb.com/index.php?X=MATH&H=07#text7
        public static double ref_X = 95.047;
        public static double ref_Y = 100.00;
        public static double ref_Z = 108.883;

        public double L { get; set; }

        public double a { get; set; }

        public double b { get; set; }

        public ColorLab() { }

        public ColorLab(double lv, double av, double bv)
        {
            L = lv;
            a = av;
            b = bv;
        }

        public ColorLab(Color srcRgb)
        {
            double[] rgb_vals = new double[3];
            rgb_vals[0] = (double)srcRgb.R / 255.0;
            rgb_vals[1] = (double)srcRgb.G / 255.0;
            rgb_vals[2] = (double)srcRgb.B / 255.0;

            for (int i = 0; i < 3; i++)
            {
                double val = rgb_vals[i];
                if (val > 0.04045) val = Math.Pow((val + 0.055) / 1.055, 2.4);
                else val /= 12.92;
                rgb_vals[i] = val * 100.0;
            }

            double[] xyz_vals = new double[3];
            xyz_vals[0] = rgb_vals[0] * 0.4124 + rgb_vals[1] * 0.3576 + rgb_vals[2] * 0.1805;
            xyz_vals[1] = rgb_vals[0] * 0.2126 + rgb_vals[1] * 0.7152 + rgb_vals[2] * 0.0722;
            xyz_vals[2] = rgb_vals[0] * 0.0193 + rgb_vals[1] * 0.1192 + rgb_vals[2] * 0.9505;

            xyz_vals[0] = xyz_vals[0] / ref_X;
            xyz_vals[1] = xyz_vals[1] / ref_Y;
            xyz_vals[2] = xyz_vals[2] / ref_Z;

            for (int i = 0; i < 3; i++)
            {
                double val = xyz_vals[i];
                if (val > 0.008856)
                {
                    val = Math.Pow(val, 1.0 / 3.0);
                }
                else
                {
                    val = (7.787 * val) + (16.0 / 116.0);
                }
                xyz_vals[i] = val;
            }
            this.L = (116 * xyz_vals[1]) - 16;
            this.a = 500 * (xyz_vals[0] - xyz_vals[1]);
            this.b = 200 * (xyz_vals[1] - xyz_vals[2]);
        }

        public Color ToRGBColor()
        {

            double[] xyz_vals = new double[3];
            xyz_vals[1] = (L + 16.0) / 116.0;
            xyz_vals[0] = a / 500.0 + xyz_vals[1];
            xyz_vals[2] = xyz_vals[1] - b / 200.0;
            for (int i = 0; i < 3; i++)
            {
                double val = xyz_vals[i];
                if (Math.Pow(val, 3.0) > 0.008856)
                {
                    val = Math.Pow(val, 3.0);
                }
                else
                {
                    val = (val - 16.0 / 116.0) / 7.787;
                }
                xyz_vals[i] = val;
            }

            xyz_vals[0] = (xyz_vals[0] * ref_X) / 100.0;
            xyz_vals[1] = (xyz_vals[1] * ref_Y) / 100.0;
            xyz_vals[2] = (xyz_vals[2] * ref_Z) / 100.0;

            double[] rgb_vals = new double[3];
            rgb_vals[0] = xyz_vals[0] * 3.2406 + xyz_vals[1] * (-1.5372) + xyz_vals[2] * (-0.4986);
            rgb_vals[1] = xyz_vals[0] * (-0.9689) + xyz_vals[1] * 1.8758 + xyz_vals[2] * 0.0415;
            rgb_vals[2] = xyz_vals[0] * 0.0557 + xyz_vals[1] * (-0.2040) + xyz_vals[2] * 1.0570;

            for (int i = 0; i < 3; i++)
            {
                double val = rgb_vals[i];
                if (val > 0.0031308) val = 1.055 * Math.Pow(val, (1.0 / 2.4)) - 0.055;
                else val *= 12.92;
                rgb_vals[i] = val * 255;
                if (rgb_vals[i] > 255.0) rgb_vals[i] = 255.0;
                else if (rgb_vals[i] < 0) rgb_vals[i] = 0;
            }

            return Color.FromRgb((byte)rgb_vals[0], (byte)rgb_vals[1], (byte)rgb_vals[2]);
        }
    }

}
