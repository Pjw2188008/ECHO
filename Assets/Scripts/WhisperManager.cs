using UnityEngine;
using System.Collections.Generic;

// 몬스터 타입 정의 (필수)
public enum MonsterType
{
    Shader,   // 빛 괴물
    Listener  // 소리 괴물
}

public class WhisperManager : MonoBehaviour
{
    public static WhisperManager Instance;

    [Header("👂 리스너 (소리 괴물) 대사")]
    [Tooltip("진실: 움직이지 마, 조용히 해...")]
    public List<AudioClip> listenerTruthClips;
    [Tooltip("거짓: 빨리 뛰어, 소리 질러...")]
    public List<AudioClip> listenerLieClips;

    [Header("🔦 쉐이더 (빛 괴물) 대사")]
    [Tooltip("진실: 불 꺼, 어둠 속에 숨어...")]
    public List<AudioClip> shaderTruthClips;
    [Tooltip("거짓: 불을 켜, 빛으로 공격해...")]
    public List<AudioClip> shaderLieClips;

    [Header("⚙️ 설정")]
    public float whisperCooldown = 5.0f; // 쿨타임
    private float lastWhisperTime = -100f;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void PlayMonsterWhisper(MonsterType type)
    {
        // 쿨타임 체크
        if (Time.time - lastWhisperTime < whisperCooldown) return;
        lastWhisperTime = Time.time;

        int rand = Random.Range(0, 100);
        List<AudioClip> targetList = null;
        string debugMsg = "";

        // ====================================================
        // 🎲 확률 로직 (40:40:20) & 로그 출력
        // ====================================================

        // 1. 진실 (0 ~ 39)
        if (rand < 40)
        {
            debugMsg = "진실";
            Debug.Log($"😇 [Whisper 결과] '{type}'가 진실을 말합니다! (깨끗한 목소리가 나와야 함)");

            if (type == MonsterType.Listener) targetList = listenerTruthClips;
            else if (type == MonsterType.Shader) targetList = shaderTruthClips;
        }
        // 2. 거짓 (40 ~ 79)
        else if (rand < 80)
        {
            debugMsg = "거짓";
            Debug.Log($"😈 [Whisper 결과] '{type}'가 거짓말을 합니다! (🚨 왜곡/에코 소리가 나와야 함)");

            if (type == MonsterType.Listener) targetList = listenerLieClips;
            else if (type == MonsterType.Shader) targetList = shaderLieClips;
        }
        // 3. 침묵 (80 ~ 99)
        else
        {
            Debug.Log($"😶 [Whisper 결과] '{type}' 침묵 당첨 (주사위: {rand}) -> 아무 소리 안 남");
            return;
        }

        // 재생 함수 호출
        PlayRandomClip(targetList, type, debugMsg);
    }

    void PlayRandomClip(List<AudioClip> clips, MonsterType type, string debugMsg)
    {
        // 리스트 비어있음 체크
        if (clips == null || clips.Count == 0)
        {
            Debug.LogWarning($"⚠️ [오류] {type}의 {debugMsg} 대사 리스트가 비어있습니다! 인스펙터를 확인하세요.");
            return;
        }

        // 랜덤 선택
        int index = Random.Range(0, clips.Count);
        AudioClip selectedClip = clips[index];

        // VoiceManager에게 전달
        if (VoiceManager.Instance != null)
        {
            bool isLie = (debugMsg == "거짓");

            // 재생 요청
            VoiceManager.Instance.PlayMonsterVoice(selectedClip, type, isLie);

            // 최종 확인 로그
            Debug.Log($"🔊 [재생 중] 파일명: {selectedClip.name} | 타입: {debugMsg}");
        }
    }
}