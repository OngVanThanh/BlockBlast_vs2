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
        private static byte[] wavBombExplosion;
        private static byte[] wavCrossLaser;
        private static byte[] wavLightning;
        private static byte[] wavGravity;

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

                Random rng = new Random(42);

                // 6. Bomb 3x3: Tiếng nổ "BÙÙÙM!" trầm rền (Sub-bass 150Hz -> 30Hz trộn nhiễu nổ, 380ms)
                wavBombExplosion = GenerateWavBytes((t, len) =>
                {
                    double progress = t / len;
                    double env = Math.Exp(-4.5 * progress);
                    double bassFreq = 150 * Math.Exp(-3.5 * progress) + 28;
                    double bass = Math.Sin(2 * Math.PI * bassFreq * t);
                    double noise = (rng.NextDouble() * 2.0 - 1.0) * 0.65;
                    return (bass * 0.75 + noise * 0.55) * env;
                }, 0.38f);

                // 7. Cross Laser: Tiếng tia năng lượng "CHÍU-ZZZZ!" (Quét tần số 1400Hz -> 180Hz + biến điệu, 280ms)
                wavCrossLaser = GenerateWavBytes((t, len) =>
                {
                    double progress = t / len;
                    double env = (1.0 - progress);
                    double freq = 1400 - 1220 * Math.Pow(progress, 0.6);
                    double mod = Math.Sin(2 * Math.PI * 55 * t) * 0.3 + 0.7;
                    return Math.Sin(2 * Math.PI * freq * t) * mod * env;
                }, 0.28f);

                // 8. Color Lightning: Tiếng sét đánh "RẮC - ĐOÀNG!" (Xung điện cao tần + sấm rền 70Hz, 340ms)
                wavLightning = GenerateWavBytes((t, len) =>
                {
                    double progress = t / len;
                    double crackle = (rng.NextDouble() * 2.0 - 1.0) * Math.Exp(-3.0 * progress);
                    double zap = Math.Sin(2 * Math.PI * (1800 - 1500 * progress) * t) * Math.Exp(-8.0 * progress);
                    double thunder = Math.Sin(2 * Math.PI * 68 * t) * Math.Exp(-3.5 * progress);
                    return (crackle * 0.55 + zap * 0.45 + thunder * 0.6) * (1.0 - progress * 0.5);
                }, 0.34f);

                // 9. Gravity Fall: Tiếng hút trọng lực & giáng sập đáy "VÙÙ - HUỴCH!" (280ms)
                wavGravity = GenerateWavBytes((t, len) =>
                {
                    double progress = t / len;
                    double sweepFreq = 480 - 410 * progress;
                    double whoosh = Math.Sin(2 * Math.PI * sweepFreq * t) * (1.0 - progress * 0.4);
                    double slam = progress > 0.65 ? Math.Sin(2 * Math.PI * 55 * t) * Math.Exp(-12 * (progress - 0.65)) * 1.4 : 0;
                    return Math.Max(-1.0, Math.Min(1.0, whoosh * 0.6 + slam));
                }, 0.28f);
            }
            catch { }
        }

        public static void PlayPickup() => PlayBytes(wavPickup);
        public static void PlayDrop() => PlayBytes(wavDrop);
        public static void PlaySnap() => PlayBytes(wavSnap);
        public static void PlayBombExplosion() => PlayBytes(wavBombExplosion);
        public static void PlayCrossLaser() => PlayBytes(wavCrossLaser);
        public static void PlayLightningStrike() => PlayBytes(wavLightning);
        public static void PlayGravitySlam() => PlayBytes(wavGravity);

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
