using UnityEngine;
using System.Collections.Generic;

public enum PathDirection { Left, Right, Center }

public class PathWhisperer : MonoBehaviour
{
    [Header("😇 진실 음성")]
    public AudioClip[] truthLeftClips;
    public AudioClip[] truthRightClips;
    public AudioClip[] truthCenterClips;

    [Header("😈 거짓 음성")]
    public AudioClip[] lieLeftClips;
    public AudioClip[] lieRightClips;
    public AudioClip[] lieCenterClips;

    [Header("⚙️ 설정")]
    private bool isFirstEncounter = true;
    private float whisperCooldown = 10.0f;
    private float lastWhisperTime = -100f;

    // ★ 외부(트리거)에서 호출하는 함수
    public void OnEnterIntersection(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        // 트리거는 강제로 소리를 내야 하므로 바로 로직 실행
        ProcessWhisper(availablePaths, correctPath);

        // 소리를 냈으니 마지막 시간 갱신
        lastWhisperTime = Time.time;
    }

    void ProcessWhisper(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        int rand = Random.Range(0, 100);
        PathDirection directionToSpeak;
        bool isLieVoice = false;

        // 1. 튜토리얼 (첫 만남) : 50 대 50
        if (isFirstEncounter)
        {
            if (rand < 50)
            {
                directionToSpeak = correctPath;
                isLieVoice = false;
            }
            else
            {
                directionToSpeak = GetLieDirection(availablePaths, correctPath);
                isLieVoice = true;
            }
            isFirstEncounter = false;
        }
        // 2. 평소 : 40 진실 / 40 거짓 / 20 침묵
        else
        {
            if (rand < 40) // 진실
            {
                directionToSpeak = correctPath;
                isLieVoice = false;
            }
            else if (rand < 80) // 거짓
            {
                directionToSpeak = GetLieDirection(availablePaths, correctPath);
                isLieVoice = true;
            }
            else // 침묵
            {
                Debug.Log("😶 [Path] 침묵 당첨 (트리거는 작동했으나 운이 좋아 조용함)");
                return;
            }
        }

        PlayVoice(directionToSpeak, isLieVoice, correctPath);
    }

    PathDirection GetLieDirection(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        List<PathDirection> candidates = new List<PathDirection>(availablePaths);
        candidates.Remove(correctPath);

        if (candidates.Count > 0) return candidates[Random.Range(0, candidates.Count)];
        return correctPath;
    }

    void PlayVoice(PathDirection dir, bool isLie, PathDirection correctPath)
    {
        if (VoiceManager.Instance == null) return;
        AudioClip clip = null;

        if (isLie)
        {
            switch (dir)
            {
                case PathDirection.Left: clip = GetRandomClip(lieLeftClips); break;
                case PathDirection.Right: clip = GetRandomClip(lieRightClips); break;
                case PathDirection.Center: clip = GetRandomClip(lieCenterClips); break;
            }
            Debug.Log($"😈 [Path] 거짓말: 정답은 {correctPath}인데 {dir}로 가라 함.");
        }
        else
        {
            switch (dir)
            {
                case PathDirection.Left: clip = GetRandomClip(truthLeftClips); break;
                case PathDirection.Right: clip = GetRandomClip(truthRightClips); break;
                case PathDirection.Center: clip = GetRandomClip(truthCenterClips); break;
            }
            Debug.Log($"😇 [Path] 진실: {dir}이 정답임.");
        }

        // ★★★ 여기가 수정되었습니다! ★★★
        // 이제 클립과 함께 "거짓말 여부(isLie)"도 같이 보냅니다.
        if (clip)
        {
            VoiceManager.Instance.PlayPathVoice(clip, isLie);
        }
    }

    AudioClip GetRandomClip(AudioClip[] clips) => (clips != null && clips.Length > 0) ? clips[Random.Range(0, clips.Length)] : null;
}