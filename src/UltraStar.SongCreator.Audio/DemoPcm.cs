namespace UltraStar.SongCreator.Audio;

public static class DemoPcm
{
    public static PcmClip Create()
    {
        const int rate = 48000, seconds = 8, channels = 2;
        var samples = new float[rate * seconds * channels];
        for (var frame = 0; frame < rate * seconds; frame++)
        {
            var amplitude = 0.005 * (1 + frame / rate % 4);
            var fade = Math.Min(1.0, Math.Min(frame / 240.0, (rate * seconds - 1 - frame) / 240.0));
            var value = (float)(amplitude * fade * Math.Sin(2 * Math.PI * 220 * frame / rate));
            samples[frame * channels] = value; samples[frame * channels + 1] = value;
        }
        return new(rate, channels, samples);
    }
}
