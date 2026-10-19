using NAudio.CoreAudioApi;

namespace DesktopWidget.Services;

public sealed class AudioService : IDisposable
{
    private readonly MMDeviceEnumerator enumerator;
    private readonly MMDevice device;

    public AudioService()
    {
        enumerator = new MMDeviceEnumerator();
        try
        {
            device = enumerator.GetDefaultAudioEndpoint(
                DataFlow.Render, Role.Multimedia);
        }
        catch
        {
            enumerator.Dispose();
            throw;
        }
    }

    public float Volume
    {
        get => device.AudioEndpointVolume.MasterVolumeLevelScalar * 100f;
        set => device.AudioEndpointVolume.MasterVolumeLevelScalar =
            Math.Clamp(value / 100f, 0f, 1f);
    }

    public bool IsMuted
    {
        get => device.AudioEndpointVolume.Mute;
        set => device.AudioEndpointVolume.Mute = value;
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    public void Dispose()
    {
        device.Dispose();
        enumerator.Dispose();
    }
}
