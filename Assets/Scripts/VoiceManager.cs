using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;

    [Header("📢 내레이션 (Intro/Ending)")]
    public AudioSource narrativeSource;

    [Header("🧭 길 안내 (Path)")]
    public AudioSource pathTruthSource; // 😇 진실 (깨끗함)
    public AudioSource pathLieSource;   // 😈 거짓 (찢어짐 + 필터 적용)

    [Header("😈 몬스터 거짓말 (공용)")]
    public AudioSource monsterLieSource;   // 에코/기계음

    [Header("😇 몬스터 진실 (공용)")]
    public AudioSource monsterTruthSource;  // 깨끗함

    private int currentPriority = 0;
    // 0:없음, 1:길안내, 2:몬스터, 3:내레이션

    void Awake() { if (Instance == null) Instance = this; }

    bool IsAnyMonsterSpeaking()
    {
        return (monsterLieSource.isPlaying || monsterTruthSource.isPlaying);
    }

    // 내레이션
    public void PlayNarrativeVoice(AudioClip clip)
    {
        if (!clip) return;
        StopAllVoices();
        currentPriority = 3;
        narrativeSource.PlayOneShot(clip);
    }

    // 몬스터 목소리
    public void PlayMonsterVoice(AudioClip clip, MonsterType type, bool isLie)
    {
        if (!clip || currentPriority >= 3) return;

        StopAllVoices();
        currentPriority = 2;

        if (isLie) monsterLieSource.PlayOneShot(clip);
        else monsterTruthSource.PlayOneShot(clip);
    }

    // ★ [수정됨] 길 안내 목소리 (이제 거짓 여부를 받습니다!)
    public void PlayPathVoice(AudioClip clip, bool isLie)
    {
        // 몬스터(2)나 내레이션(3) 중이면 무시
        if (!clip || currentPriority >= 2) return;

        // 기존 길 안내 소리만 끄기 (겹침 방지)
        pathTruthSource.Stop();
        pathLieSource.Stop();

        currentPriority = 1;

        // ★ 진실/거짓에 따라 다른 스피커 사용
        if (isLie)
        {
            pathLieSource.PlayOneShot(clip); // 😈 찢어지는 스피커
        }
        else
        {
            pathTruthSource.PlayOneShot(clip); // 😇 깨끗한 스피커
        }
    }

    void StopAllVoices()
    {
        if (monsterLieSource) monsterLieSource.Stop();
        if (monsterTruthSource) monsterTruthSource.Stop();

        // 길 안내도 2개 다 꺼야 함
        if (pathTruthSource) pathTruthSource.Stop();
        if (pathLieSource) pathLieSource.Stop();

        if (narrativeSource) narrativeSource.Stop();
    }

    void Update()
    {
        // 아무 소리도 안 나면 우선순위 초기화
        bool isPathPlaying = pathTruthSource.isPlaying || pathLieSource.isPlaying;

        if (!IsAnyMonsterSpeaking() && !isPathPlaying && !narrativeSource.isPlaying)
        {
            currentPriority = 0;
        }
    }
}