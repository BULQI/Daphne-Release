using System;
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

namespace DaphneGui
{
    public class CellPopChartSurface : SciChartSurface
    {
        private string filename, filetype;
        SciChartSurface surface;

        public void SaveToFile(string fileName, string fileType, SciChartSurface surf)
        {
            filename = fileName;  filetype = fileType; surface = surf;
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
            iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Jpeg);

            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();
            
            doc.Add(pdfImage);
            //A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
            //You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)
            // Add meta information to the document
            doc.AddAuthor("Sanjeev Gupta");
            doc.AddCreator("Daphne PDF output");
            doc.AddKeywords("PDF export daphne");
            doc.AddSubject("Document subject - Save the SciChart graph to a PDF document");
            doc.AddTitle("The document title - Daphne graph in PDF format");
            doc.Close();
            //------------------------------------------------------------------------------------------

        }


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
    }
}
