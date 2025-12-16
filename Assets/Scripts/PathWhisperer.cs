using UnityEngine;
using System.Collections.Generic;

// 방향 타입 정의
public enum PathDirection { Left, Right, Center }

public class PathWhisperer : MonoBehaviour
{
    [Header("?? 진실 음성")]
    public AudioClip[] truthLeftClips;
    public AudioClip[] truthRightClips;
    public AudioClip[] truthCenterClips;

    [Header("?? 거짓 음성")]
    public AudioClip[] lieLeftClips;
    public AudioClip[] lieRightClips;
    public AudioClip[] lieCenterClips;

    [Header("?? 설정")]
    private bool isFirstEncounter = true; // 첫 만남 체크
    private float whisperCooldown = 10.0f;
    private float lastWhisperTime = -100f;

    // ★ 외부(트리거)에서 호출하는 함수
    public void OnEnterIntersection(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        // 쿨타임 체크
        if (Time.time - lastWhisperTime < whisperCooldown) return;

        // 갈림길 처리 시작
        ProcessWhisper(availablePaths, correctPath);
        lastWhisperTime = Time.time;
    }

    void ProcessWhisper(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        int rand = Random.Range(0, 100); // 0 ~ 99 랜덤 숫자
        PathDirection directionToSpeak;
        bool isLieVoice = false;

        // ====================================================
        // CASE 1: 첫 번째 갈림길 (무조건 소리남, 50:50)
        // ====================================================
        if (isFirstEncounter)
        {
            Debug.Log($"?? [첫 만남] 주사위: {rand} (기준: 50미만 진실, 50이상 거짓)");

            if (rand < 50) // 0~49 (50%): 진실
            {
                directionToSpeak = correctPath;
                isLieVoice = false;
                Debug.Log("?? 결과: [진실] 당첨!");
            }
            else // 50~99 (50%): 거짓
            {
                directionToSpeak = GetLieDirection(availablePaths, correctPath);
                isLieVoice = true;
                Debug.Log("?? 결과: [거짓] 당첨!");
            }

            isFirstEncounter = false; // 다음부터는 평소대로
        }
        // ====================================================
        // CASE 2: 평소 갈림길 (40:40:20)
        // ====================================================
        else
        {
            Debug.Log($"?? [평소] 주사위: {rand} (0~39:진실, 40~79:거짓, 80~99:침묵)");

            if (rand < 40) // 0~39 (40%): 진실
            {
                directionToSpeak = correctPath;
                isLieVoice = false;
                Debug.Log("?? 결과: [진실] 당첨!");
            }
            else if (rand < 80) // 40~79 (40%): 거짓
            {
                directionToSpeak = GetLieDirection(availablePaths, correctPath);
                isLieVoice = true;
                Debug.Log("?? 결과: [거짓] 당첨!");
            }
            else // 80~99 (20%): 침묵
            {
                Debug.Log("?? 결과: [침묵]... (운이 좋군)");
                return; // 아무 소리 안 내고 종료
            }
        }

        // 최종 소리 재생
        PlayVoice(directionToSpeak, isLieVoice, correctPath);
    }

    // 거짓말 방향 뽑기
    PathDirection GetLieDirection(List<PathDirection> availablePaths, PathDirection correctPath)
    {
        // 가능한 길 목록을 복사
        List<PathDirection> candidates = new List<PathDirection>(availablePaths);

        // 정답을 리스트에서 제거 (거짓말 해야 하니까)
        candidates.Remove(correctPath);

        // 남은 길이 있다면 그 중에서 랜덤 선택
        if (candidates.Count > 0)
        {
            PathDirection lieDir = candidates[Random.Range(0, candidates.Count)];
            Debug.Log($"?? 거짓말 생성 중... (정답: {correctPath} -> 거짓말 선택: {lieDir})");
            return lieDir;
        }

        // 만약 거짓말 할 다른 길이 아예 없다면(외길인데 설정 실수 등) 어쩔 수 없이 정답 리턴
        Debug.LogWarning("?? 거짓말을 하려 했으나, 정답 외에 다른 길이 설정되지 않았습니다!");
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
            Debug.Log($"?? [소리 재생] 거짓말 톤으로 '{dir}'라고 말함. (원래 정답: {correctPath})");
        }
        else
        {
            switch (dir)
            {
                case PathDirection.Left: clip = GetRandomClip(truthLeftClips); break;
                case PathDirection.Right: clip = GetRandomClip(truthRightClips); break;
                case PathDirection.Center: clip = GetRandomClip(truthCenterClips); break;
            }
            Debug.Log($"?? [소리 재생] 진실 톤으로 '{dir}'라고 말함.");
        }

        if (clip) VoiceManager.Instance.PlayPathVoice(clip);
        else Debug.LogWarning($"?? {dir} 방향의 오디오 클립이 비어있습니다!");
    }

    AudioClip GetRandomClip(AudioClip[] clips) => (clips != null && clips.Length > 0) ? clips[Random.Range(0, clips.Length)] : null;
}