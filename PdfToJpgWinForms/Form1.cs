using PdfToJpgWinForms.Services;
using ReaLTaiizor.Controls;
using System.Diagnostics;

namespace PdfToJpgWinForms
{
    public partial class Form1 : ReaLTaiizor.Forms.MaterialForm
    {
        // PDF işlemlerini yapan servis nesnesi.
        private readonly PdfExportService _pdfExportService;

        public Form1()
        {
            InitializeComponent();

            // Servis oluşturulur.
            _pdfExportService = new PdfExportService();

            // Servisten gelen log event'i forma bağlanır.
            _pdfExportService.LogGenerated += PdfExportService_LogGenerated;

            // Servisten gelen progress event'i forma bağlanır.
            _pdfExportService.ProgressChanged += PdfExportService_ProgressChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "PDF dosyalarının bulunduğu klasörü seçin";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                moonTextBox1.Text = dialog.SelectedPath;
                materialButton2.Enabled = true;

                // Seçilen dizin içerisindeki PDF dosya sayısını getir.
                GetDocumentCount(dialog.SelectedPath);
            }
        }

        // Dönüştürme işlemini başlatan buton.
        private async void materialButton2_Click(object sender, EventArgs e)
        {
            string folderPath = moonTextBox1.Text.Trim();
            bool createFolderPerPdf = foxCheckBoxEdit1.Checked;

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show(
                    "Lütfen geçerli bir klasör seçin.",
                    "Uyarı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;                               
            }

            // arayüzü kilitle
            ToggleUI(false);

            foxBigLabel4.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            foxBigLabel4.Refresh();

            try
            {
                await Task.Run(() =>
                {
                    _pdfExportService.ProcessFolder(folderPath, createFolderPerPdf);
                });

                // arayüzü kilidi kaldır
                ToggleUI(true);

                parrotCircleProgressBar1.Percentage = 100;
                parrotCircleProgressBar1.Text = "100%";

                // ⭐ Eğer checkbox seçiliyse export klasörünü aç
                if (foxCheckBoxEdit2.Checked)
                {
                    string exportFolder = Path.Combine(folderPath, "JPG_Output");

                    if (Directory.Exists(exportFolder))
                    {
                        Process.Start("explorer.exe", exportFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Hata oluştu: {ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                foxBigLabel8.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                foxBigLabel8.Refresh();
            }
        }

        // Servisten gelen log mesajlarını ekranda gösterir.
        private void PdfExportService_LogGenerated(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(PdfExportService_LogGenerated), message);
                return;
            }

            listBox1.Items.Add($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff} - {message}");
            listBox1.TopIndex = listBox1.Items.Count - 1;
        }

        private void PdfExportService_ProgressChanged(int current, int total)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, int>(PdfExportService_ProgressChanged), current, total);
                return;
            }

            // Progress bar
            int percentage = (int)Math.Round((double)current / total * 100);
            parrotCircleProgressBar1.Percentage = percentage;

            // Toplam sayfa sayısı
            foxBigLabel6.Text = string.Format("{0} / {1}", current, total);
            foxBigLabel6.Refresh();
        }

        private void GetDocumentCount(string folderPath)
        {
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
            foxBigLabel5.Text = pdfFiles.Length.ToString();
            foxBigLabel5.Refresh();
        }

        private void ToggleUI(bool enabled)
        {
            foxCheckBoxEdit1.Enabled = enabled;
            foxCheckBoxEdit2.Enabled = enabled;
            foxCheckBoxEdit3.Enabled = enabled;

            materialButton1.Enabled = enabled;
            materialButton2.Enabled = enabled;
        }
    }
}
