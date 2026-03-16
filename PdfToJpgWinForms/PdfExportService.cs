using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;

namespace PdfToJpgWinForms.Services
{
    /// <summary>
    /// PDF dosyalarını kontrol eder.
    /// A4 olmayan PDF dosyalarını HATA_boyut klasörüne kopyalar.
    /// Uygun olan PDF dosyalarının tüm sayfalarını 300 DPI JPG olarak dışa aktarır.
    /// </summary>
    public class PdfExportService
    {
        private const int A4PortraitWidthPx = 2480;
        private const int A4PortraitHeightPx = 3508;
        private const int ExportDpi = 300;

        private const double A4WidthPoints = 595.2756;
        private const double A4HeightPoints = 841.8898;
        private const double PointTolerance = 2.0;

        public event Action<string>? LogGenerated;
        public event Action<int, int>? ProgressChanged;

        /// <summary>
        /// Klasördeki PDF dosyalarını işler.
        /// </summary>
        /// <param name="rootFolder">PDF klasörü</param>
        /// <param name="createFolderPerPdf">
        /// true ise her PDF için ayrı klasör açılır.
        /// false ise tüm JPG dosyaları doğrudan JPG_Output içine kaydedilir.
        /// </param>
        public void ProcessFolder(string rootFolder, bool createFolderPerPdf)
        {
            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
                throw new DirectoryNotFoundException("Geçerli bir klasör bulunamadı.");

            string[] pdfFiles = Directory.GetFiles(rootFolder, "*.pdf", SearchOption.TopDirectoryOnly);

            if (pdfFiles.Length == 0)
            {
                WriteLog("Klasörde PDF dosyası bulunamadı.");
                return;
            }

            string successRoot = Path.Combine(rootFolder, "JPG_Output");
            string errorRoot = Path.Combine(rootFolder, "HATA_boyut");

            Directory.CreateDirectory(successRoot);
            Directory.CreateDirectory(errorRoot);

            int totalPages = CalculateTotalPages(pdfFiles);
            int processedPages = 0;

            WriteLog($"Toplam PDF: {pdfFiles.Length}");
            WriteLog($"Toplam sayfa: {totalPages}");

            foreach (string pdfPath in pdfFiles)
            {
                try
                {
                    ProcessSinglePdf(
                        pdfPath,
                        successRoot,
                        errorRoot,
                        createFolderPerPdf,
                        ref processedPages,
                        totalPages);
                }
                catch (Exception ex)
                {
                    WriteLog($"HATA - {Path.GetFileName(pdfPath)} : {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tüm PDF'lerdeki toplam sayfa sayısını hesaplar.
        /// </summary>
        private int CalculateTotalPages(string[] pdfFiles)
        {
            int totalPages = 0;

            foreach (string pdfPath in pdfFiles)
            {
                try
                {
                    using PdfDocument document = PdfDocument.Load(pdfPath);
                    totalPages += document.PageCount;
                }
                catch
                {
                    // İstenirse burada ayrıca log da üretilebilir.
                }
            }

            return totalPages == 0 ? 1 : totalPages;
        }

        /// <summary>
        /// Tek PDF dosyasını işler.
        /// </summary>
        private void ProcessSinglePdf(
            string pdfPath,
            string successRoot,
            string errorRoot,
            bool createFolderPerPdf,
            ref int processedPages,
            int totalPages)
        {
            string pdfFileName = Path.GetFileName(pdfPath);
            string pdfNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);

            WriteLog($"Kontrol ediliyor: {pdfFileName}");

            using PdfDocument document = PdfDocument.Load(pdfPath);

            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                SizeF pageSize = document.PageSizes[pageIndex];

                if (!IsA4Page(pageSize))
                {
                    WriteLog(
                        $"Boyut hatası: {pdfFileName} / Sayfa {pageIndex + 1} / " +
                        $"{pageSize.Width:0.##} x {pageSize.Height:0.##} pt");

                    string errorPdfPath = Path.Combine(errorRoot, pdfFileName);
                    File.Copy(pdfPath, errorPdfPath, true);

                    WriteLog($"HATA_boyut klasörüne kopyalandı: {pdfFileName}");

                    for (int i = 0; i < document.PageCount; i++)
                    {
                        processedPages++;
                        RaiseProgress(processedPages, totalPages);
                    }

                    return;
                }
            }

            string outputFolder;

            if (createFolderPerPdf)
            {
                // Her PDF için ayrı klasör oluştur.
                outputFolder = Path.Combine(successRoot, pdfNameWithoutExtension);
                Directory.CreateDirectory(outputFolder);

                WriteLog($"Ayrı klasör oluşturuldu: {pdfNameWithoutExtension}");
            }
            else
            {
                // Tüm JPG dosyaları doğrudan JPG_Output içine kaydedilsin.
                outputFolder = successRoot;
            }

            WriteLog($"Dönüştürme başladı: {pdfFileName}");

            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                ExportPageAsJpeg(document, pageIndex, outputFolder, pdfNameWithoutExtension);

                processedPages++;
                RaiseProgress(processedPages, totalPages);
            }

            WriteLog($"Tamamlandı: {pdfFileName}");
        }

        /// <summary>
        /// PDF sayfasını JPG olarak dışa aktarır.
        /// </summary>
        private void ExportPageAsJpeg(
            PdfDocument document,
            int pageIndex,
            string outputFolder,
            string pdfNameWithoutExtension)
        {
            SizeF pageSize = document.PageSizes[pageIndex];
            bool isLandscape = pageSize.Width > pageSize.Height;

            int targetWidth = isLandscape ? A4PortraitHeightPx : A4PortraitWidthPx;
            int targetHeight = isLandscape ? A4PortraitWidthPx : A4PortraitHeightPx;

            using Bitmap renderedImage = (Bitmap)document.Render(
                pageIndex,
                targetWidth,
                targetHeight,
                ExportDpi,
                ExportDpi,
                PdfRenderFlags.Annotations | PdfRenderFlags.CorrectFromDpi
            );

            string outputFilePath = Path.Combine(
                outputFolder,
                $"{pdfNameWithoutExtension}_Sayfa_{pageIndex + 1:D3}.jpg");

            SaveJpeg(outputFilePath, renderedImage, 85L);
        }

        /// <summary>
        /// Sayfanın A4 olup olmadığını kontrol eder.
        /// </summary>
        private bool IsA4Page(SizeF pageSize)
        {
            double width = pageSize.Width;
            double height = pageSize.Height;

            bool isPortraitA4 =
                Math.Abs(width - A4WidthPoints) <= PointTolerance &&
                Math.Abs(height - A4HeightPoints) <= PointTolerance;

            bool isLandscapeA4 =
                Math.Abs(width - A4HeightPoints) <= PointTolerance &&
                Math.Abs(height - A4WidthPoints) <= PointTolerance;

            return isPortraitA4 || isLandscapeA4;
        }

        /// <summary>
        /// Resmi JPEG formatında kaydeder.
        /// </summary>
        private void SaveJpeg(string filePath, Image image, long quality)
        {
            ImageCodecInfo? jpgEncoder = ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

            if (jpgEncoder == null)
            {
                image.Save(filePath, ImageFormat.Jpeg);
                return;
            }

            using EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            image.Save(filePath, jpgEncoder, encoderParameters);
        }

        /// <summary>
        /// Log mesajı üretir.
        /// </summary>
        private void WriteLog(string message)
        {
            LogGenerated?.Invoke(message);
        }

        /// <summary>
        /// İlerleme event'ini tetikler.
        /// </summary>
        private void RaiseProgress(int current, int total)
        {
            ProgressChanged?.Invoke(current, total);
        }
    }
}