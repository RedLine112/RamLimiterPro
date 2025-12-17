using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RamLimiterPro
{
    public static class RamKasabi
    {
        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        // Artık geriye iki tane sayı döndürüyoruz: (Mevcut RAM, Tokatlanan Miktar)
        public static (long MevcutMB, long KazancMB) TokatlaVeOlc(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return (0, 0);

                long toplamMevcut = 0;
                long toplamEski = 0;

                foreach (var proc in processes)
                {
                    try
                    {
                        // Tokattan önceki halini kaydet
                        long eskiRam = proc.WorkingSet64;
                        toplamEski += eskiRam;

                        // TOKATLA!
                        EmptyWorkingSet(proc.Handle);

                        // Tokattan sonraki halini hemen ölçmeye gerek yok, 
                        // farkı "Eski Hal - (Teorik Min)" gibi düşünmek yerine
                        // Basitçe: Windows temizledi, ne kadar temizlediğini WorkingSet farkından tam göremeyiz 
                        // çünkü anlık değişir. 
                        // AMA: Şöyle bir hile yapalım:
                        // EmptyWorkingSet başarılı olursa genelde RAM kullanımı drastic düşer.
                        // Biz raporda "Şu anki hali"ni gösterelim. Kazancı tahmin edelim.

                        // Düzeltme: En sağlıklısı tokattan sonra tekrar ölçmek.
                        proc.Refresh(); // Verileri tazele
                        toplamMevcut += proc.WorkingSet64;
                    }
                    catch { }
                }

                long mevcutMB = toplamMevcut / 1024 / 1024;
                long eskiMB = toplamEski / 1024 / 1024;
                long kazanc = eskiMB - mevcutMB;

                if (kazanc < 0) kazanc = 0; // Eksi çıkarsa saçmalamasın

                return (mevcutMB, kazanc);
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}