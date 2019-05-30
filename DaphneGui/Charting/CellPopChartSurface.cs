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
using Abt.Controls.SciChart.Visuals;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

//------------------------------------------------------------
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Daphne;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel;

namespace DaphneGui
{

    /// <summary>
    /// This class derives from SciChartSurface and serves as a base class for Lineage and Cell Pop Dynamics chart surface classes.
    /// Common methods are located here.  This is not an abstract class.
    /// </summary>
    public class CellPopChartSurface : SciChartSurface, INotifyPropertyChanged
    {
        // x axis default units are minutes 
        // For minutes, xScale = 1.0;
        // For hours,   xScale = 1.0/60 
        // For days,    xScale = 1.0/(60*24)
        // For weeks,   xScale = 1.0/(60*24*7)

        protected double[] xScaleValues = { 1.0, 1.0 / 60, 1.0 / (60 * 24), 1.0 / (60 * 24 * 7) };
        protected string[] xAxisLabels = { "Time in minutes", "Time in hours", "Time in days", "Time in weeks" };

        public string XAxisLabel { get; set; }
        public double XScale { get; set; }
        public double pdfScaleFactor = 8;

        /// <summary>
        /// Constructo
        /// </summary>
        public CellPopChartSurface()
        {
            XAxisLabel = "Time in minutes";
            XScale = 1.0;
            
        }

        /// <summary>
        /// This method saves the chart to file - types png, bmp, jpg, pdf, tif, xps
        /// </summary>
        /// <param name="filename"></param>
        public void SaveToFile(string filename)
        {
            //For png, bmp, jpg - easy
            if (filename.EndsWith("png"))
            {
                ExportToFile(filename, ExportType.Png);
            }
            else if (filename.EndsWith("bmp"))
            {
                ExportToFile(filename, ExportType.Bmp);
            }
            else if (filename.EndsWith("jpg"))
            {
                ExportToFile(filename, ExportType.Jpeg);
            }
            //Tiff file is also pretty easily saved
            else if (filename.EndsWith("tif"))
            {
                ExportToTiff(filename);
            }
            else if (filename.EndsWith("pdf"))
            {
                OutputToPDF(filename);
                //ExportToPDF(filename);
            }
            else if (filename.EndsWith("xps"))
            {
                ExportToXps(filename);
            }
        }

        public virtual void ExportToPDF(string filename)
        {
        }

        /// <summary>
        /// This method outputs a PDF file without first outputting a .bmp file.
        /// </summary>
        /// <param name="filename"></param>
        public void OutputToPDF(string filename)
        {
            //Export this graph to BitmapSource
            var source = this.ExportToBitmapSource();

            //Then retrieve from BitmapSource into a Bitmap object
            Bitmap bmp1 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp1.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp1.Size),
            ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp1.UnlockBits(data);

            //-------------------------------------------------
            System.Drawing.Image image = bmp1;
            iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Bmp);

            Document doc = new Document(PageSize.LETTER);
            doc.SetMargins(4, doc.RightMargin, doc.TopMargin, doc.BottomMargin);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();
            
            doc.Add(pdfImage);
            doc.Close();
            //------------------------------------------------------------------------------------------
           
        }

        /// <summary>
        /// This method outputs a tiff file without outputting a bmp file. This does not increase the resolution.  We should.
        /// Save surface to bitmap source, extract bitmap, and save in tiff format.
        /// </summary>
        /// <param name="outFile"></param>
        public void ExportToTiff(string outFile)
        {
            var source = this.ExportToBitmapSource();
            Bitmap bmp3 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp3.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp3.Size),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp3.UnlockBits(data);
            bmp3.Save(outFile, ImageFormat.Tiff);
        }

        /// <summary>
        /// This method outputs an Xps file
        /// </summary>
        /// <param name="filename"></param>
        public void ExportToXps(string filename)
        {
            var dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                var size = new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaWidth * 3 / 4);

                //var scs = CreateSciChartSurfaceWithoutShowingIt(size);
                //And print. This works particularly well to XPS! 
                Action printAction = () => dialog.PrintVisual(this, "XPS file");
                Dispatcher.BeginInvoke(printAction);
            }

            //OutputXpsDoc(filename);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
