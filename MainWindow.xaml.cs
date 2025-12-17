using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RamLimiterPro
{
    public partial class MainWindow : Window
    {
        private bool calisiyorMu = false;
        private CancellationTokenSource iptalSinyali;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!calisiyorMu)
            {
                // -- BAŞLAT --
                calisiyorMu = true;
                btnToggle.Content = "STOP";
                btnToggle.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28)); // Kırmızı

                // Çalışırken ayarları kurcalama, bozulur
                txtApps.IsEnabled = false;
                cmbMod.IsEnabled = false;

                iptalSinyali = new CancellationTokenSource();
                Task.Run(() => AkilliDongu(iptalSinyali.Token));
            }
            else
            {
                // -- DURDUR --
                calisiyorMu = false;
                iptalSinyali.Cancel();
                btnToggle.Content = "START";
                btnToggle.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Yeşil

                txtApps.IsEnabled = true;
                cmbMod.IsEnabled = true;

                lblStatus.Text = "Stopped";
                lblInfo.Text = "Mode: Closed";
            }
        }

        private async Task AkilliDongu(CancellationToken token)
        {
            int beklemeSuresi = 5000; // Başlangıç değeri

            while (!token.IsCancellationRequested)
            {
                // 1. Arayüzden verileri çek
                string hamMetin = "";
                int secilenModIndex = 0;

                Dispatcher.Invoke(() =>
                {
                    hamMetin = txtApps.Text;
                    secilenModIndex = cmbMod.SelectedIndex;
                });

                string[] apps = hamMetin.Split(',');

                // 2. Tablo Başlığı
                StringBuilder tablo = new StringBuilder();
                tablo.AppendLine($"LAST LIMIT: {DateTime.Now:HH:mm:ss}");
                tablo.AppendLine(new string('-', 45));
                tablo.AppendLine(String.Format("{0,-15} | {1,10} | {2,10}", "APPLICATION", "CURRENT MB", "SAVED MB"));
                tablo.AppendLine(new string('-', 45));

                long toplamKazanc = 0;
                long toplamKullanim = 0;

                // 3. Tokatlama İşlemi
                foreach (var app in apps)
                {
                    if (string.IsNullOrWhiteSpace(app)) continue;
                    string temizIsim = app.Trim().Replace(".exe", "");

                    var sonuc = RamKasabi.TokatlaVeOlc(temizIsim);

                    if (sonuc.MevcutMB > 0)
                    {
                        tablo.AppendLine(String.Format("{0,-15} | {1,10} | {2,10}",
                            temizIsim.Length > 15 ? temizIsim.Substring(0, 12) + "..." : temizIsim,
                            sonuc.MevcutMB,
                            sonuc.KazancMB > 0 ? "+" + sonuc.KazancMB : "-"));

                        toplamKazanc += sonuc.KazancMB;
                        toplamKullanim += sonuc.MevcutMB;
                    }
                }

                tablo.AppendLine(new string('-', 45));
                tablo.AppendLine(String.Format("{0,-15} | {1,10} | {2,10}", "TOTAL", toplamKullanim, "+" + toplamKazanc));

                // 4. MOD AYARLAMASI (Senin istediğin saniyeler)
                string modAciklama = "";

                switch (secilenModIndex)
                {
                    case 0: // OTOMATİK (5 sn - 15 sn)
                        if (toplamKazanc > 100) // Eğer 100 MB'dan fazla yer açtıysak, ortalık karışıktır.
                        {
                            beklemeSuresi = 5000;
                            modAciklama = "Mode: AUTO (5sec) - There is some intensity.";
                        }
                        else // Ortalık sakinse sal gitsin.
                        {
                            beklemeSuresi = 12000; // Senin istediğin 15 saniye bu.
                            modAciklama = "Mode: AUTO (12sec) - it's better now.";
                        }
                        break;

                    case 1: // 3 Saniye
                        beklemeSuresi = 3000;
                        modAciklama = "Mode: TERMINATOR (3sec) - No Mercy!";
                        break;

                    case 2: // 5 Saniye
                        beklemeSuresi = 5000;
                        modAciklama = "Mode: ANGRY (5sec) - A little angry.";
                        break;

                    case 3: // 10 Saniye
                        beklemeSuresi = 10000;
                        modAciklama = "Mode: DEFAULT (10sec) - Optimal balance.";
                        break;

                    case 4: // 30 Saniye
                        beklemeSuresi = 30000;
                        modAciklama = "Mode: SLEEPY (30sec) - Doesn't worry about it too much.";
                        break;

                    case 5: // 60 Saniye
                        beklemeSuresi = 60000;
                        modAciklama = "Mode: PLAYING DEAD (60sec) - Continuity isn't really necessary.";
                        break;
                }

                // UI Güncelleme
                Dispatcher.Invoke(() =>
                {
                    lblStatus.Text = tablo.ToString();
                    lblInfo.Text = modAciklama;
                });

                try { await Task.Delay(beklemeSuresi, token); } catch { break; }
            }
        }
    }
}