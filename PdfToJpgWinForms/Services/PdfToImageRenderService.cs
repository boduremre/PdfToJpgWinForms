using PDFtoImage;

namespace PdfToJpgWinForms.Services
{
    /// <summary>
    /// PDFtoImage kullanarak PDF dosyalarını JPG olarak dışa aktaran servis.
    /// Bu sınıf sadece render/export işini yapar.
    /// </summary>
    public class PdfToImageRenderService
    {
        /// <summary>
        /// JPG kalite değeri.
        /// PDFtoImage tarafında SaveJpeg doğrudan dosyaya render edip kaydeder.
        /// </summary>
        private const int ExportDpi = 300;

        /// <summary>
        /// Log mesajı üretildiğinde tetiklenir.
        /// </summary>
        public event Action<string>? LogGenerated;

        /// <summary>
        /// İlerleme değiştiğinde tetiklenir.
        /// current: işlenen sayfa
        /// total: toplam sayfa
        /// </summary>
        public event Action<int, int>? ProgressChanged;

        /// <summary>
        /// Toplam PDF dosya sayısı bulunduğunda tetiklenir.
        /// </summary>
        public event Action<int>? TotalFileCountDetected;

        /// <summary>
        /// Klasördeki tüm PDF dosyalarını JPG olarak dışa aktarır.
        /// </summary>
        /// <param name="rootFolder">PDF dosyalarının bulunduğu klasör</param>
        /// <param name="createFolderPerPdf">
        /// true ise her PDF için ayrı klasör oluşturulur.
        /// false ise tüm JPG'ler doğrudan JPG_Output içine kaydedilir.
        /// </param>
        public void ProcessFolder(string rootFolder, bool createFolderPerPdf)
        {
            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
                throw new DirectoryNotFoundException("Geçerli bir klasör bulunamadı.");

            string[] pdfFiles = Directory.GetFiles(rootFolder, "*.pdf", SearchOption.TopDirectoryOnly);

            TotalFileCountDetected?.Invoke(pdfFiles.Length);

            if (pdfFiles.Length == 0)
            {
                WriteLog("Klasörde PDF dosyası bulunamadı.");
                return;
            }

            string outputRoot = Path.Combine(rootFolder, "JPG_Output");
            Directory.CreateDirectory(outputRoot);

            int totalPages = CalculateTotalPages(pdfFiles);
            int processedPages = 0;

            //WriteLog($"Toplam PDF: {pdfFiles.Length}");
            //WriteLog($"Toplam sayfa: {totalPages}");

            foreach (string pdfPath in pdfFiles)
            {
                try
                {
                    ProcessSinglePdf(pdfPath, outputRoot, createFolderPerPdf, ref processedPages, totalPages);
                }
                catch (Exception ex)
                {
                    WriteLog($"HATA - {Path.GetFileName(pdfPath)} : {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tüm PDF dosyalarındaki toplam sayfa sayısını hesaplar.
        /// PDFtoImage içinde doğrulanmış GetPageCount API'si kullanılır.
        /// </summary>
        private int CalculateTotalPages(string[] pdfFiles)
        {
            int totalPages = 0;

            foreach (string pdfPath in pdfFiles)
            {
                try
                {
                    string pdfBase64 = Convert.ToBase64String(File.ReadAllBytes(pdfPath));
                    totalPages += Conversion.GetPageCount(pdfBase64);
                }
                catch (Exception ex)
                {
                    WriteLog($"Sayfa sayısı okunamadı: {Path.GetFileName(pdfPath)} / {ex.Message}");
                }
            }

            return totalPages == 0 ? 1 : totalPages;
        }

        /// <summary>
        /// Tek bir PDF dosyasını işler.
        /// </summary>
        private void ProcessSinglePdf(string pdfPath, string outputRoot, bool createFolderPerPdf, ref int processedPages, int totalPages)
        {
            string pdfFileName = Path.GetFileName(pdfPath);
            string pdfNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfPath);

            WriteLog($"İşleniyor: {pdfFileName}");

            string pdfBase64 = Convert.ToBase64String(File.ReadAllBytes(pdfPath));
            int pageCount = Conversion.GetPageCount(pdfBase64);

            string targetFolder = createFolderPerPdf ? Path.Combine(outputRoot, pdfNameWithoutExtension) : outputRoot;

            Directory.CreateDirectory(targetFolder);

            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                string outputFilePath = Path.Combine(targetFolder, $"{pdfNameWithoutExtension}_Sayfa_{pageIndex + 1:D3}.jpg");

                ExportPageAsJpeg(pdfBase64, pageIndex, outputFilePath);

                processedPages++;
                RaiseProgress(processedPages, totalPages);
            }

            WriteLog($"Tamamlandı: {pdfFileName}");
        }

        /// <summary>
        /// Tek bir PDF sayfasını JPG olarak dışa aktarır.
        /// PDFtoImage'in doğrulanmış SaveJpeg API'si kullanılır.
        /// </summary>
        private void ExportPageAsJpeg(string pdfBase64, int pageIndex, string outputFilePath)
        {
            var options = new RenderOptions
            {
                Dpi = ExportDpi
            };

            Conversion.SaveJpeg(outputFilePath, pdfBase64, pageIndex, password: null, options: options);

            WriteLog($"Kaydedildi: {Path.GetFileName(outputFilePath)}");
        }

        /// <summary>
        /// Log event'ini tetikler.
        /// </summary>
        private void WriteLog(string message)
        {
            LogGenerated?.Invoke(message);
        }

        /// <summary>
        /// Progress event'ini tetikler.
        /// </summary>
        private void RaiseProgress(int current, int total)
        {
            ProgressChanged?.Invoke(current, total);
        }
    }
}