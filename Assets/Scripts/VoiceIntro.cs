using UnityEngine;
using System.Collections;

public class VoiceIntro : MonoBehaviour
{
    public AudioClip warningClip;
    public float delayTime = 10.0f;

    void Start() { StartCoroutine(PlayWarning()); }
    IEnumerator PlayWarning()
    {
        yield return new WaitForSeconds(delayTime);
        if (VoiceManager.Instance && warningClip) VoiceManager.Instance.PlayNarrativeVoice(warningClip);
    }
}