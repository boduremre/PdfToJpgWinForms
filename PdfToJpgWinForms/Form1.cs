using PdfToJpgWinForms.Services;
using ReaLTaiizor.Child.Material;
using ReaLTaiizor.Controls;
using System.Diagnostics;

namespace PdfToJpgWinForms
{
    public partial class Form1 : ReaLTaiizor.Forms.MaterialForm
    {
        // PDF işlemlerini yapan servis nesnesi.
        private readonly PdfToImageRenderService _pdfToImageRenderService;

        // PDF işlemi başladığında zamanı kaydetmek için bir alan. İlerleme güncellemelerinde bu zamanı kullanarak geçen süreyi hesaplayacağız.
        private DateTime _processStartTime;

        public Form1()
        {
            InitializeComponent();

            // PDF işlemlerini yapan servis nesnesini oluştur ve event handler'ları bağla.
            _pdfToImageRenderService = new PdfToImageRenderService();

            _pdfToImageRenderService.LogGenerated += PdfExportService_LogGenerated;
            _pdfToImageRenderService.ProgressChanged += PdfExportService_ProgressChanged;
            _pdfToImageRenderService.TotalFileCountDetected += PdfExportService_TotalFileCountDetected;
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

            // Dönüştürme işlemi başladığında zamanı kaydet
            _processStartTime = DateTime.Now;

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Lütfen geçerli bir klasör seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // arayüzü kilitle
            ToggleUI(false);

            try
            {
                // ⭐ Eğer checkbox seçiliyse export klasörünü aç
                if (foxCheckBoxEdit4.Checked)
                {
                    string exportFolder = Path.Combine(folderPath, "JPG_Output");

                    if (Directory.Exists(exportFolder))
                        Directory.Delete(exportFolder, true); // true = içindekilerle birlikte sil

                    Directory.CreateDirectory(exportFolder);
                }

                if (!foxCheckBoxEdit5.Checked)
                {
                    parrotGroupBox3.Visible = false;
                    this.Height -= parrotGroupBox3.Height + 5; // 5px boşluk için
                }

                await Task.Run(() =>
            {
                _pdfToImageRenderService.ProcessFolder(folderPath, createFolderPerPdf);
            });

                // arayüzü kilidi kaldır
                ToggleUI(true);

                // İşlem tamamlandığında ilerleme çubuğunu %100 yap ve metni "100%" olarak güncelle
                parrotCircleProgressBar1.Percentage = 100;
                parrotCircleProgressBar1.Text = "100%";

                // Eğer checkbox seçiliyse export klasörünü aç
                if (foxCheckBoxEdit2.Checked)
                {
                    string exportFolder = Path.Combine(folderPath, "JPG_Output");

                    if (Directory.Exists(exportFolder))
                        Process.Start("explorer.exe", exportFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Servisten gelen log mesajlarını ekranda gösterir.
        private void PdfExportService_LogGenerated(string message)
        {
            // Eğer log gösterme checkbox'ı seçili değilse, log mesajlarını ekranda göstermiyoruz.
            if (foxCheckBoxEdit5.Checked)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(PdfExportService_LogGenerated), message);
                    return;
                }

                // Log mesajını zaman damgasıyla birlikte listbox'a ekle
                materialListBox1.Items.Add(new MaterialListBoxItem($"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff} - {message}"));
                materialListBox1.TabIndex = materialListBox1.Items.Count - 1;
            }
        }

        private void PdfExportService_ProgressChanged(int current, int total)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, int>(PdfExportService_ProgressChanged), current, total);
                return;
            }

            // İşlem süresini hesapla ve ekranda göster (örneğin "00:01:23" gibi)
            DateTime now = DateTime.Now;
            foxBigLabel4.Text = string.Format("{0} / {1} sn.", _processStartTime.ToString("HH:mm:ss"), Math.Round((now - _processStartTime).TotalSeconds, 0).ToString());
            foxBigLabel4.Refresh();

            // İşlenen sayfa sayısına göre yüzdelik hesapla
            int percentage = total <= 0 ? 0 : (int)Math.Round((double)current / total * 100);

            // Yüzde değerini 0-100 aralığında sınırla
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            // İlerleme yüzdesini güncelle
            parrotCircleProgressBar1.Percentage = percentage;

            // İşlenen sayfa sayısını güncelle (örneğin "5/20" gibi)
            foxBigLabel6.Text = string.Format("{0} / {1}", current, total);
            foxBigLabel6.Refresh();
        }

        private void PdfExportService_TotalFileCountDetected(int totalFileCount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(PdfExportService_TotalFileCountDetected), totalFileCount);
                return;
            }

            foxBigLabel5.Text = totalFileCount.ToString();
            foxBigLabel5.Refresh();
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
            foxCheckBoxEdit4.Enabled = enabled;
            foxCheckBoxEdit5.Enabled = enabled;

            materialButton1.Enabled = enabled;
            materialButton2.Enabled = enabled;
        }

        private void foxCheckBoxEdit6_CheckedChanged(object sender, EventArgs e)
        {
            if (foxCheckBoxEdit6.Checked)
            {
                DialogResult result = MessageBox.Show("Uygulama yeniden başlatılacak. Devam etmek istiyor musunuz?", "Bilgi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    Application.Restart();

                foxCheckBoxEdit6.Checked = false; // Checkbox'ı tekrar kapat    
            }
        }

        private void foxCheckBoxEdit7_CheckedChanged(object sender, EventArgs e)
        {
            if (foxCheckBoxEdit7.Checked)
            {
                DialogResult dialogResult = MessageBox.Show(this, "PDF To JPG Converter (Emre Bodur-software@emrebodur.com)", "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.OK)
                {
                    foxCheckBoxEdit7.Checked = false; // Checkbox'ı tekrar kapat
                }
            }
        }
    }
}
