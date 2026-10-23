using System;

public static class AudioEventSystem
{
    public static event Action<AudioEvent> OnAudioEvent;


    public static void Raise(AudioEvent audioEvent)
    {
        OnAudioEvent?.Invoke(audioEvent);
    }
}