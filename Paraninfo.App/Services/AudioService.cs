using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Paraninfo.App.Models;

namespace Paraninfo.App.Services;

public sealed class AudioService : IDisposable
{
    private readonly Dictionary<string, PcmVoice> _sounds = [];
    public bool Muted { get; private set; }

    public AudioService(AudioConfig config)
    {
        Muted = !config.Enabled || config.Volume <= 0;
        foreach (var name in new[] { "type", "alert", "collapse", "success", "failure" })
        {
            try
            {
                var resource = Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/Sounds/{name}.wav"));
                if (resource == null) continue;
                using var input = resource.Stream;
                using var buffer = new MemoryStream();
                input.CopyTo(buffer);
                var data = buffer.ToArray();
                // Assets próprios: WAV PCM mono de 16 bits, cabeçalho de 44 bytes.
                if (data.Length < 46 || BitConverter.ToInt16(data, 20) != 1
                    || BitConverter.ToInt16(data, 22) != 1 || BitConverter.ToInt16(data, 34) != 16)
                    continue;
                var samples = data[44..];
                var volume = config.Volume * (name == "type" ? 0.45 : 1);
                for (var offset = 0; offset + 1 < samples.Length; offset += 2)
                {
                    var sample = (short)(BitConverter.ToInt16(samples, offset) * volume);
                    samples[offset] = (byte)(sample & 255);
                    samples[offset + 1] = (byte)((sample >> 8) & 255);
                }
                var voice = PcmVoice.Create(samples, BitConverter.ToUInt32(data, 24));
                if (voice != null) _sounds.Add(name, voice);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or NotSupportedException)
            {
                // Recurso ou dispositivo indisponível: o convite continua silenciosamente.
            }
        }
    }

    public void ToggleMute()
    {
        Muted = !Muted;
        Stop();
    }

    // Sem relógio, throttle ou fila: só reproduz quando uma view revela conteúdo.
    // Canais independentes impedem que a digitação corte o som de um popup.
    public void Play(string name)
    {
        if (!Muted && _sounds.TryGetValue(name, out var sound)) sound.Play();
    }

    public void Stop()
    {
        foreach (var sound in _sounds.Values) sound.Stop();
    }

    public void Dispose()
    {
        foreach (var sound in _sounds.Values) sound.Dispose();
        _sounds.Clear();
    }

    private sealed class PcmVoice : IDisposable
    {
        private IntPtr _device;
        private IntPtr _samples;
        private IntPtr _header;
        private bool _prepared;
        private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveHeader>();

        public static PcmVoice? Create(byte[] samples, uint sampleRate)
        {
            var voice = new PcmVoice();
            var format = new WaveFormat
            {
                FormatTag = 1, Channels = 1, SamplesPerSecond = sampleRate,
                AverageBytesPerSecond = sampleRate * 2, BlockAlign = 2, BitsPerSample = 16
            };
            if (Native.waveOutOpen(out voice._device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0)
                return null;
            try
            {
                voice._samples = Marshal.AllocHGlobal(samples.Length);
                Marshal.Copy(samples, 0, voice._samples, samples.Length);
                voice._header = Marshal.AllocHGlobal((int)HeaderSize);
                Marshal.StructureToPtr(new WaveHeader { Data = voice._samples, BufferLength = (uint)samples.Length }, voice._header, false);
                if (Native.waveOutPrepareHeader(voice._device, voice._header, HeaderSize) != 0)
                {
                    voice.Dispose();
                    return null;
                }
                voice._prepared = true;
                return voice;
            }
            catch
            {
                voice.Dispose();
                throw;
            }
        }

        public void Play()
        {
            if (_device == IntPtr.Zero || !_prepared) return;
            // Não reinicia um sample ainda em reprodução: resets frequentes podem
            // cancelar o ataque antes de ele chegar ao dispositivo. Também não enfileira sons atrasados.
            const uint InQueue = 0x10;
            if ((Marshal.PtrToStructure<WaveHeader>(_header).Flags & InQueue) != 0) return;
            Native.waveOutWrite(_device, _header, HeaderSize);
        }

        public void Stop()
        {
            if (_device != IntPtr.Zero) Native.waveOutReset(_device);
        }

        public void Dispose()
        {
            Stop();
            if (_device != IntPtr.Zero)
            {
                if (_prepared) Native.waveOutUnprepareHeader(_device, _header, HeaderSize);
                Native.waveOutClose(_device);
                _device = IntPtr.Zero;
            }
            if (_header != IntPtr.Zero) { Marshal.FreeHGlobal(_header); _header = IntPtr.Zero; }
            if (_samples != IntPtr.Zero) { Marshal.FreeHGlobal(_samples); _samples = IntPtr.Zero; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WaveFormat
        {
            public ushort FormatTag, Channels;
            public uint SamplesPerSecond, AverageBytesPerSecond;
            public ushort BlockAlign, BitsPerSample, ExtraSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr Data;
            public uint BufferLength, BytesRecorded;
            public UIntPtr User;
            public uint Flags, Loops;
            public IntPtr Next;
            public UIntPtr Reserved;
        }

        private static class Native
        {
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutOpen(out IntPtr device, uint deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutReset(IntPtr device);
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
            [DllImport("winmm.dll", ExactSpelling = true)]
            internal static extern uint waveOutClose(IntPtr device);
        }
    }
}
