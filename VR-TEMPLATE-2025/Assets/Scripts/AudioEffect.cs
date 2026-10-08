using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AudioEffect : MonoBehaviour
{
    public static AudioEffect Instance { get; private set; }

    [Header("Mixer Configuration")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameters (Precisa ser igual ao Mixer!)")]
    [SerializeField] private string masterLowPass = "MasterCutoff";
    [SerializeField] private string masterEchoWet = "MasterEchoWet";
    [SerializeField] private string masterDistortion = "MasterDistortion"; // Adicionado para controlar o Distortion do print

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.3f;

    public enum EffectType
    {
        Normal,
        MuffledHeavy,
        RadioAgudo,
        DarkSpace,
        DistortedSlow
    }

    private Sequence effectSequence;

    private float m_MasterLow;
    private float m_MasterEcho;
    private float m_MasterDist;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeAudioPreset(EffectType preset)
    {
        if (audioMixer == null) return;

        effectSequence?.Kill();
        effectSequence = DOTween.Sequence().SetUpdate(true);

        // VALORES PADRÃO (Som limpo - Estado Normal)
        float targetLowPass = 22000f;
        float targetEchoWet = 0f;
        float targetDist = 0f; // Distortion zerado no modo normal

        switch (preset)
        {
            case EffectType.MuffledHeavy:
                targetLowPass = 350f;
                targetEchoWet = 0f;
                targetDist = 0f;
                break;

            case EffectType.RadioAgudo:
                targetLowPass = 4000f;
                targetEchoWet = 0f;
                targetDist = 0.2f; // Uma leve distorção de rádio velho
                break;

            case EffectType.DarkSpace:
                targetLowPass = 250f;
                targetEchoWet = 80f;    // Ativa o rastro do Eco
                targetDist = 0.1f;
                break;

            case EffectType.DistortedSlow:
                targetLowPass = 800f;
                targetEchoWet = 35f;
                targetDist = 0.5f; // Distorção pesada para dar sensação de impacto/tontura
                break;
        }

        // ================= EXECUÇÃO DOS TWEENS =================

        if (audioMixer.GetFloat(masterLowPass, out m_MasterLow))
        {
            var t = DOTween.To(() => m_MasterLow, x => { m_MasterLow = x; audioMixer.SetFloat(masterLowPass, x); }, targetLowPass, transitionDuration).SetEase(Ease.OutCubic);
            if (t != null) effectSequence.Join(t);
        }

        if (audioMixer.GetFloat(masterEchoWet, out m_MasterEcho))
        {
            var t = DOTween.To(() => m_MasterEcho, x => { m_MasterEcho = x; audioMixer.SetFloat(masterEchoWet, x); }, targetEchoWet, transitionDuration).SetEase(Ease.OutCubic);
            if (t != null) effectSequence.Join(t);
        }

        if (audioMixer.GetFloat(masterDistortion, out m_MasterDist))
        {
            var t = DOTween.To(() => m_MasterDist, x => { m_MasterDist = x; audioMixer.SetFloat(masterDistortion, x); }, targetDist, transitionDuration).SetEase(Ease.OutCubic);
            if (t != null) effectSequence.Join(t);
        }
    }

    private void OnDestroy()
    {
        effectSequence?.Kill();
    }
}