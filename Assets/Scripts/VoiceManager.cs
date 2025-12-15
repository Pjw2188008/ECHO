using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;
    public AudioSource headAudioSource;
    private int currentPriority = 0; // 0:None, 1:Path, 2:Monster, 3:Intro

    void Awake() { if (Instance == null) Instance = this; }
    void Update() { if (!headAudioSource.isPlaying) currentPriority = 0; }

    public void PlayNarrativeVoice(AudioClip clip) // Priority 3
    {
        if (!clip) return;
        currentPriority = 3;
        headAudioSource.Stop();
        headAudioSource.PlayOneShot(clip);
    }

    public void PlayMonsterVoice(AudioClip clip) // Priority 2
    {
        if (!clip || currentPriority >= 3) return;
        currentPriority = 2;
        headAudioSource.Stop();
        headAudioSource.PlayOneShot(clip);
    }

    public void PlayPathVoice(AudioClip clip) // Priority 1
    {
        if (!clip || (currentPriority >= 2 && headAudioSource.isPlaying)) return;
        currentPriority = 1;
        headAudioSource.Stop();
        headAudioSource.PlayOneShot(clip);
    }
}