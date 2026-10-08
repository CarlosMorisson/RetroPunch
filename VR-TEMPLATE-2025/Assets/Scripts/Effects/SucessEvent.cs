using DG.Tweening;
using UnityEngine;

public class SucessEvent : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Nome do audio no AudioController.")]
    [SerializeField] private string audioName = "Sucess";

    [Header("Particle")]
    [SerializeField] private ParticleSystem particle;

    [Header("Scale")]
    [Tooltip("Objeto que vai escalar para zero. Se vazio, usa este objeto.")]
    [SerializeField] private Transform target;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.InBack;

    private Tween scaleTween;

    // Chamar no FinishGameController.OnSuccessEvent
    [ContextMenu("Testar Sucess Event")]
    public void Play()
    {
        if (AudioController.Instance != null)
            AudioController.Instance.Play(audioName);

        if (particle != null)
            particle.Play();

        Transform scaleTarget = target != null ? target : transform;
        scaleTween?.Kill();
        scaleTween = scaleTarget.DOScale(Vector3.zero, scaleDuration)
            .SetUpdate(true)
            .SetEase(scaleEase);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }
}
