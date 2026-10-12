using System;
using System.IO;
using System.Media;

namespace BlockBlast.Module
{
    /// <summary>
    /// Bộ phát âm thanh tự sinh (Procedural SFX Engine) trong bộ nhớ:
    /// Không cần file WAV ngoài, không sợ thiếu file hay lỗi đường dẫn.
    /// </summary>
    public static class SoundManager
    {
        public static bool IsMuted { get; set; } = false;
        private static byte[] wavPickup;
        private static byte[] wavDrop;
        private static byte[][] wavCombos;
        private static byte[] wavGameOver;
        private static byte[] wavSnap;

        static SoundManager()
        {
            try
            {
                // 1. Pickup: Tiếng "pop" nhanh, vui tai (520Hz -> 780Hz, 35ms)
                wavPickup = GenerateWavBytes((t, len) =>
                {
                    double curFreq = 520 + (780 - 520) * (t / len);
                    return Math.Sin(2 * Math.PI * curFreq * t) * (1 - t / len);
                }, 0.035f);

                // 2. Drop: Tiếng "cộp" đầm chắc của khối gỗ (180Hz decaying, 50ms)
                wavDrop = GenerateWavBytes((t, len) =>
                {
                    return Math.Sin(2 * Math.PI * 180 * t) * Math.Exp(-6 * (t / len));
                }, 0.05f);

                // 3. Combos: 4 cấp độ nốt nhạc thăng hoa (C5=523Hz, E5=659Hz, G5=784Hz, C6=1046Hz)
                wavCombos = new byte[][]
                {
                    GenerateToneBytes(523, 0.12f),
                    GenerateToneBytes(659, 0.14f),
                    GenerateToneBytes(784, 0.16f),
                    GenerateToneBytes(1046, 0.22f)
                };

                // 4. Game Over: Tiếng trượt trầm buồn (420Hz -> 180Hz, 260ms)
                wavGameOver = GenerateWavBytes((t, len) =>
                {
                    double curFreq = 420 + (180 - 420) * (t / len);
                    return Math.Sin(2 * Math.PI * curFreq * t) * (1 - t / len);
                }, 0.26f);

                // 5. Snap: Tiếng đứt dây thun "PẰNG" đanh gọn (880Hz -> 160Hz, 50ms)
                wavSnap = GenerateWavBytes((t, len) =>
                {
                    double curFreq = 880 - (880 - 160) * (t / len);
                    return Math.Sin(2 * Math.PI * curFreq * t) * (1 - t / len);
                }, 0.05f);
            }
            catch { }
        }

        public static void PlayPickup() => PlayBytes(wavPickup);
        public static void PlayDrop() => PlayBytes(wavDrop);
        public static void PlaySnap() => PlayBytes(wavSnap);

        public static void PlayLineClear(int comboLevel)
        {
            if (wavCombos == null || wavCombos.Length == 0) return;
            int idx = Math.Max(0, Math.Min(wavCombos.Length - 1, comboLevel - 1));
            PlayBytes(wavCombos[idx]);
        }

        public static void PlayGameOver() => PlayBytes(wavGameOver);

        private static void PlayBytes(byte[] wavBytes)
        {
            if (IsMuted || wavBytes == null) return;
            try
            {
                using (MemoryStream ms = new MemoryStream(wavBytes))
                using (SoundPlayer player = new SoundPlayer(ms))
                {
                    player.Play();
                }
            }
            catch { }
        }

        #region PCM WAV Synthesizer (22050Hz, 16-bit Mono)
        private static byte[] GenerateToneBytes(double freq, float durationSec)
        {
            return GenerateWavBytes((t, len) => Math.Sin(2 * Math.PI * freq * t) * (1 - t / len), durationSec);
        }

        private static byte[] GenerateWavBytes(Func<double, double, double> waveFunc, float durationSec)
        {
            int sampleRate = 22050;
            int numSamples = (int)(sampleRate * durationSec);
            short[] samples = new short[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double val = waveFunc(t, durationSec);
                samples[i] = (short)(Math.Max(-1.0, Math.Min(1.0, val)) * 28000);
            }

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + numSamples * 2);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1); // PCM format
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(numSamples * 2);
                for (int i = 0; i < numSamples; i++) bw.Write(samples[i]);
                return ms.ToArray();
            }
        }
        #endregion
    }
}
