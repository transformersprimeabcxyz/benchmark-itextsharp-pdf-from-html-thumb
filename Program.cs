using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace benchmark_itextsharp_pdf_from_html_thumb
{
    public class Program
    {
        private static readonly string HTML_PATH = @"Data\zengarden_001.html";
        private static readonly string PDF_PATH = @"D:\Temp\benchmark-itextsharp-pdf-from-html-thumb\zengarden_{0}.pdf";
        private static readonly Size BROWSER_SIZE = new Size(700, 794);
        private static readonly iTextSharp.text.Rectangle PDF_SIZE = iTextSharp.text.PageSize.A4;

        public static void Main(string[] args)
        {
            var html = GenerateHtml();
            var image = GenerateScreenShot(html);
            var resized = GenerateResized(image);
            var pdf = GeneratePdf(resized);
            OutputFile(pdf);
        }

        private static void OutputFile(byte[] content)
        {
            var makePath = string.Format(PDF_PATH, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var outputDir = Path.GetDirectoryName(makePath);
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
            using (var stream = new FileStream(makePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                stream.Write(content, 0, content.Length);
        }

        private static byte[] GeneratePdf(byte[] image)
        {
            using (var memory = new MemoryStream())
            using (var pdf = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4))
            using (var writer = PdfWriter.GetInstance(pdf, memory))
            {
                writer.CloseStream = false;
                pdf.Open();
                pdf.Add(iTextSharp.text.Image.GetInstance(image));
                pdf.Close();
                return memory.ToArray();
            }
        }

        private static byte[] GenerateResized(byte[] buffer)
        {
            using (var memory = new MemoryStream(buffer))
            using (var image = Image.FromStream(memory)) {
                
                var thumbFactor = (float) PDF_SIZE.Width / (float) image.Width;
                var thumbSize = new Size(
                    (int) (image.Width * thumbFactor),
                    (int) (image.Height * thumbFactor));

                using (var output = new MemoryStream())
                using (var thumb = image.GetThumbnailImage(thumbSize.Width, thumbSize.Height, null, IntPtr.Zero)) {
                    thumb.Save(output, ImageFormat.Png);
                    return output.ToArray();
                }
            }
        }

        private static byte[] GenerateScreenShot(string html)
        {
            var image = null as Image;
            var thread = new Thread(() =>
            {
                var browser = new WebBrowser();
                browser.DocumentText = html;
                browser.Size = BROWSER_SIZE;
                browser.ScrollBarsEnabled = false;
                browser.DocumentCompleted += (s, e) =>
                {
                    var b = (WebBrowser)s;
                    var r = new Rectangle(b.Location, b.Size);
                    var ss = new Bitmap(browser.Height, browser.Width);
                    b.BringToFront();
                    b.DrawToBitmap(ss, r);
                    image = ss;
                };
                while (browser.ReadyState != WebBrowserReadyState.Complete) Application.DoEvents();
                Application.DoEvents();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            while (image == null) Application.DoEvents();

            using (var memory = new MemoryStream())
            {
                image.Save(memory, ImageFormat.Png);
                return memory.GetBuffer();
            }
        }

        private static string GenerateHtml()
        {
            using (var stream = new FileStream(HTML_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }
    }
}
