using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VictoryEffects : MonoBehaviour
{
    // Tipos de tween disponiveis, igual ao componente "DOTween Animation" do DOTween Pro
    public enum VictoryTweenType
    {
        Move,
        LocalMove,
        Rotate,
        LocalRotate,
        Scale,
        PunchPosition,
        PunchRotation,
        PunchScale,
        ShakePosition,
        ShakeRotation,
        ShakeScale,
        Color,
        Fade
    }

    [Serializable]
    public class VictoryRoutine
    {
        // Usado só pelo editor pra saber se o item está expandido no Inspector
        [HideInInspector] public bool Expanded = true;

        [Tooltip("Nome apenas para organização no Inspector.")]
        public string RoutineName = "Nova Rotina";

        [Tooltip("Objeto que vai receber o efeito.")]
        public GameObject Target;

        [Tooltip("Tipo de efeito do DOTween.")]
        public VictoryTweenType Type = VictoryTweenType.PunchScale;

        [Header("Valores")]
        public Vector3 Vector = Vector3.one;
        public bool UniformScale = false;
        public float UniformScaleValue = 1.2f;
        public bool Relative = false;
        public Color TargetColor = Color.white;
        [Range(0f, 1f)] public float TargetAlpha = 1f;

        [Header("Punch / Shake")]
        public int Vibrato = 10;
        [Range(0f, 180f)] public float Randomness = 90f;
        [Range(0f, 1f)] public float Elasticity = 1f;
        public bool FadeOut = true;

        [Header("Configuração do Tween")]
        public float Duration = 0.5f;
        public float Delay = 0f;
        public Ease EaseType = Ease.OutBack;
        [Tooltip("-1 = loop infinito, 0 = sem loop.")]
        public int LoopCount = 0;
        public LoopType LoopType = LoopType.Restart;

        [Header("Evento")]
        public UnityEvent OnComplete;

        [NonSerialized] public Tween RuntimeTween;
    }

    // Lado do ambiente que um efeito aleatório pode afetar
    public enum VictorySide
    {
        Left,
        Right,
        Both
    }

    // Cada BuildEnviriomments da cena tem seu próprio BuildMovemmentVisual com as listas
    // de objetos instanciados do lado direito/esquerdo. Um grupo aqui representa "uma fileira".
    [Serializable]
    public class VictoryEnvironmentGroup
    {
        [Tooltip("Nome apenas para organização no Inspector.")]
        public string GroupName = "Ambiente";

        [Tooltip("BuildMovemmentVisual desse ambiente, de onde os objetos instanciados são obtidos.")]
        public BuildMovemmentVisual Source;

        [Tooltip("Objetos instanciados do lado direito desse ambiente (preenchido a partir do Source).")]
        public List<GameObject> RightObjects = new List<GameObject>();

        [Tooltip("Objetos instanciados do lado esquerdo desse ambiente (preenchido a partir do Source).")]
        public List<GameObject> LeftObjects = new List<GameObject>();

        // Copia as listas de Transform do BuildMovemmentVisual pras listas de GameObject deste grupo
        public void RefreshFromSource()
        {
            RightObjects.Clear();
            LeftObjects.Clear();

            if (Source == null)
                return;

            foreach (Transform t in Source.RightList)
            {
                if (t != null)
                    RightObjects.Add(t.gameObject);
            }

            foreach (Transform t in Source.LeftList)
            {
                if (t != null)
                    LeftObjects.Add(t.gameObject);
            }
        }
    }

    // Gera VictoryRoutines pra um conjunto de ambientes: todo objeto sorteado usa o mesmo
    // Type (ex: todos PunchScale), mas quantidade, distância, duração, etc. são aleatórias.
    [Serializable]
    public class VictoryRandomEffectGroup
    {
        [Tooltip("Nome apenas para organização no Inspector.")]
        public string GroupName = "Nova Randomização";

        [Tooltip("Todos os objetos sorteados por esse grupo recebem o mesmo tipo de efeito.")]
        public VictoryTweenType Type = VictoryTweenType.PunchScale;

        [Tooltip("Lado dos ambientes que pode ser afetado.")]
        public VictorySide Side = VictorySide.Both;

        [Tooltip("Índices na lista 'Ambientes' que esse grupo afeta. Vazio = todos os ambientes.")]
        public List<int> EnvironmentIndices = new List<int>();

        [Header("Distância / Força")]
        [Tooltip("Direção base do efeito, multiplicada pela distância sorteada.")]
        public Vector3 Direction = Vector3.one;
        [Tooltip("Distância/força mínima e máxima sorteada pra cada objeto.")]
        public Vector2 DistanceRange = new Vector2(0.3f, 1f);
        [Tooltip("Usado só quando Type é Scale.")]
        public bool UniformScale = true;
        public Vector2 UniformScaleRange = new Vector2(1.1f, 1.5f);

        [Header("Punch / Shake")]
        public Vector2Int VibratoRange = new Vector2Int(6, 14);
        public Vector2 RandomnessRange = new Vector2(60f, 120f);
        public Vector2 ElasticityRange = new Vector2(0.5f, 1f);
        public bool FadeOut = true;

        [Header("Tempo")]
        public Vector2 DurationRange = new Vector2(0.3f, 0.8f);
        [Tooltip("Atraso sorteado pra cada objeto. Maior que 0 cria um efeito cascata.")]
        public Vector2 DelayRange = new Vector2(0f, 0.2f);
        public Ease EaseType = Ease.OutBack;

        // Junta os objetos dos ambientes/lado selecionados, sorteia quantos vão ser usados
        // e monta uma VictoryRoutine (com Type fixo e valores aleatórios) pra cada um.
        public List<VictoryRoutine> GenerateRoutines(List<VictoryEnvironmentGroup> environments)
        {
            List<VictoryRoutine> routines = new List<VictoryRoutine>();

            if (environments == null)
                return routines;

            List<GameObject> pool = new List<GameObject>();

            for (int i = 0; i < environments.Count; i++)
            {
                if (EnvironmentIndices != null && EnvironmentIndices.Count > 0 && !EnvironmentIndices.Contains(i))
                    continue;

                VictoryEnvironmentGroup env = environments[i];
                if (env == null)
                    continue;

                if (Side is VictorySide.Right or VictorySide.Both)
                    pool.AddRange(env.RightObjects);

                if (Side is VictorySide.Left or VictorySide.Both)
                    pool.AddRange(env.LeftObjects);
            }

            pool.RemoveAll(go => go == null);
            if (pool.Count == 0)
                return routines;

            // Todos os objetos do pool são animados; só a ordem é sorteada
            // (usada como cascata via DelayRange).
            Shuffle(pool);

            foreach (GameObject go in pool)
                routines.Add(BuildRandomRoutine(go));

            return routines;
        }

        private VictoryRoutine BuildRandomRoutine(GameObject target)
        {
            float distance = UnityEngine.Random.Range(DistanceRange.x, DistanceRange.y);

            return new VictoryRoutine
            {
                RoutineName = $"{GroupName} - {target.name}",
                Target = target,
                Type = Type,
                Vector = Direction * distance,
                UniformScale = UniformScale,
                UniformScaleValue = UnityEngine.Random.Range(UniformScaleRange.x, UniformScaleRange.y),
                Vibrato = UnityEngine.Random.Range(VibratoRange.x, VibratoRange.y + 1),
                Randomness = UnityEngine.Random.Range(RandomnessRange.x, RandomnessRange.y),
                Elasticity = UnityEngine.Random.Range(ElasticityRange.x, ElasticityRange.y),
                FadeOut = FadeOut,
                Duration = UnityEngine.Random.Range(DurationRange.x, DurationRange.y),
                Delay = UnityEngine.Random.Range(DelayRange.x, DelayRange.y),
                EaseType = EaseType
            };
        }

        private static void Shuffle(List<GameObject> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    [Header("Configuração Geral")]
    [Tooltip("Marcado: cada rotina só começa depois que a anterior terminar.\nDesmarcado: todas as rotinas tocam ao mesmo tempo.")]
    [SerializeField] private bool followOrder = false;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Rotinas de Vitória")]
    [SerializeField] private List<VictoryRoutine> victoryRoutines = new List<VictoryRoutine>();

    [Header("Ambientes (BuildEnviriomments)")]
    [Tooltip("Um item por BuildEnviriomments da cena, cada um com suas próprias listas de objetos Left/Right.")]
    [SerializeField] private List<VictoryEnvironmentGroup> environmentGroups = new List<VictoryEnvironmentGroup>();

    [Header("Efeitos Aleatórios")]
    [Tooltip("Cada grupo sorteia objetos dos Ambientes acima e aplica o mesmo Type neles, com valores aleatórios.")]
    [SerializeField] private List<VictoryRandomEffectGroup> randomEffectGroups = new List<VictoryRandomEffectGroup>();

    [Header("Queda dos Objetos")]
    [Tooltip("Força (impulso) mínima e máxima aplicada no topo de cada objeto pra ele tombar.")]
    [SerializeField] private Vector2 fallForceRange = new Vector2(0.5f, 1.5f);
    [Tooltip("Atraso aleatório máximo entre os objetos começarem a cair (efeito cascata). 0 = todos juntos.")]
    [SerializeField] private float fallMaxDelay = 0.3f;
    [Tooltip("Desliga o BuildMovemmentVisual dos ambientes pra ele parar de mexer na escala dos objetos enquanto caem.")]
    [SerializeField] private bool stopVisualOnFall = true;
    [Tooltip("Todos os BuildMovemmentVisual da cena (preenchido pelo refresh da queda).")]
    [SerializeField] private List<BuildMovemmentVisual> fallSources = new List<BuildMovemmentVisual>();
    [Tooltip("Todos os objetos que vão cair (preenchido pelo refresh da queda).")]
    [SerializeField] private List<GameObject> fallObjects = new List<GameObject>();

    [Header("Eventos")]
    public UnityEvent OnVictoryEffectsComplete;

    private Sequence victorySequence;
    private List<VictoryRoutine> activeRoutines = new List<VictoryRoutine>();

    [ContextMenu("Testar Efeitos de Vitória")]
    public void PlayVictoryEffects()
    {
        RefreshEnvironmentObjects();

        // Toca as rotinas manuais junto com as geradas a partir dos ambientes
        // (BuildMovemmentVisual), senão os objetos dos ambientes nunca animam.
        List<VictoryRoutine> routines = new List<VictoryRoutine>(victoryRoutines);
        routines.AddRange(GenerateRandomRoutines());

        PlayRoutines(routines);
    }

    // Atualiza as listas de objetos de cada ambiente a partir do BuildMovemmentVisual referenciado
    [ContextMenu("Atualizar Objetos dos Ambientes")]
    public void RefreshEnvironmentObjects()
    {
        if (environmentGroups == null)
            return;

        foreach (VictoryEnvironmentGroup group in environmentGroups)
            group?.RefreshFromSource();
    }

    // Sorteia, pra cada grupo aleatório, quais objetos recebem o efeito (mesmo Type, valores aleatórios)
    public List<VictoryRoutine> GenerateRandomRoutines()
    {
        List<VictoryRoutine> generated = new List<VictoryRoutine>();

        if (randomEffectGroups == null)
            return generated;

        foreach (VictoryRandomEffectGroup randomGroup in randomEffectGroups)
        {
            if (randomGroup == null)
                continue;

            generated.AddRange(randomGroup.GenerateRoutines(environmentGroups));
        }

        return generated;
    }

    [ContextMenu("Testar Efeitos Aleatórios de Vitória")]
    public void PlayRandomVictoryEffects()
    {
        RefreshEnvironmentObjects();
        PlayRoutines(GenerateRandomRoutines());
    }

    // Busca todos os BuildMovemmentVisual da cena e junta os objetos Left/Right de cada um,
    // independente da lista de Ambientes usada pelos tweens
    [ContextMenu("Atualizar Objetos da Queda")]
    public void RefreshFallObjects()
    {
        fallSources.Clear();
        fallObjects.Clear();

        BuildMovemmentVisual[] visuals = FindObjectsByType<BuildMovemmentVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (BuildMovemmentVisual visual in visuals)
        {
            fallSources.Add(visual);

            foreach (Transform t in visual.RightList)
            {
                if (t != null)
                    fallObjects.Add(t.gameObject);
            }

            foreach (Transform t in visual.LeftList)
            {
                if (t != null)
                    fallObjects.Add(t.gameObject);
            }
        }
    }

    // Liga a física de todos os objetos dos BuildMovemmentVisual e dá um empurrão fraco
    // no topo de cada um pra ele tombar
    [ContextMenu("Derrubar Objetos dos Ambientes")]
    public void DropEnvironmentObjects()
    {
        RefreshFallObjects();

        if (stopVisualOnFall)
        {
            foreach (BuildMovemmentVisual visual in fallSources)
            {
                if (visual != null)
                    visual.enabled = false;
            }
        }

        foreach (GameObject go in fallObjects)
            DropObject(go);
    }

    private void DropObject(GameObject go)
    {
        if (go == null)
            return;

        Rigidbody rb = GetChildRigidbody(go.transform);
        if (rb == null)
        {
            Debug.LogWarning($"[VictoryEffects] Nenhum Rigidbody encontrado nos filhos de '{go.name}'.");
            return;
        }

        // Cancela um empurrão atrasado pendente, caso a queda seja disparada de novo
        rb.DOKill();

        // O objeto da lista é o Pivot (escala não uniforme, animada pelo BuildMovemmentVisual)
        // e o Rigidbody fica no filho rotacionado. Girar um filho embaixo de um pai com escala
        // não uniforme distorce a malha, então solta o filho do Pivot mantendo a escala do mundo.
        DetachKeepingWorldScale(rb.transform);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        float delay = UnityEngine.Random.Range(0f, fallMaxDelay);
        if (delay <= 0f)
            PushTop(rb);
        else
            DOVirtual.DelayedCall(delay, () => PushTop(rb), useUnscaledTime).SetTarget(rb);
    }

    // Procura o Rigidbody primeiro nos filhos do objeto da lista e só depois no próprio objeto
    private static Rigidbody GetChildRigidbody(Transform root)
    {
        foreach (Transform child in root)
        {
            Rigidbody rb = child.GetComponentInChildren<Rigidbody>(true);
            if (rb != null)
                return rb;
        }

        return root.GetComponent<Rigidbody>();
    }

    private static void DetachKeepingWorldScale(Transform t)
    {
        if (t.parent == null)
            return;

        Vector3 worldScale = t.lossyScale;
        t.SetParent(null, true);
        t.localScale = worldScale;
    }

    // Empurra o canto superior do objeto numa direção horizontal aleatória pra ele tombar
    private void PushTop(Rigidbody rb)
    {
        if (rb == null)
            return;

        Vector3 topPoint = GetTopPoint(rb);

        Vector2 dir2D = UnityEngine.Random.insideUnitCircle.normalized;
        if (dir2D == Vector2.zero)
            dir2D = Vector2.right;

        Vector3 direction = new Vector3(dir2D.x, 0f, dir2D.y);
        float force = UnityEngine.Random.Range(fallForceRange.x, fallForceRange.y);

        rb.AddForceAtPosition(direction * force, topPoint, ForceMode.Impulse);
    }

    private Vector3 GetTopPoint(Rigidbody rb)
    {
        Collider col = rb.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        Renderer rend = rb.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds b = rend.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        return rb.worldCenterOfMass + Vector3.up;
    }

    private void PlayRoutines(List<VictoryRoutine> routines)
    {
        StopVictoryEffects();

        if (routines == null || routines.Count == 0)
            return;

        activeRoutines = routines;

        victorySequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetAutoKill(true);

        foreach (VictoryRoutine routine in routines)
        {
            if (routine == null || routine.Target == null)
                continue;

            Tween tween = BuildTween(routine);
            if (tween == null)
                continue;

            tween.SetUpdate(useUnscaledTime);

            VictoryRoutine capturedRoutine = routine;
            tween.OnComplete(() => capturedRoutine.OnComplete?.Invoke());

            routine.RuntimeTween = tween;

            // Join se comporta como Append quando a Sequence está vazia,
            // então isso funciona certinho pro primeiro item também.
            if (followOrder)
                victorySequence.Append(tween);
            else
                victorySequence.Join(tween);
        }

        victorySequence.OnComplete(() => OnVictoryEffectsComplete?.Invoke());
    }

    [ContextMenu("Parar Efeitos de Vitória")]
    public void StopVictoryEffects()
    {
        victorySequence?.Kill();
        victorySequence = null;

        if (activeRoutines == null)
            return;

        foreach (VictoryRoutine routine in activeRoutines)
        {
            routine.RuntimeTween?.Kill();
            routine.RuntimeTween = null;
        }
    }

    private Tween BuildTween(VictoryRoutine routine)
    {
        Transform target = routine.Target.transform;
        Tween tween;

        switch (routine.Type)
        {
            case VictoryTweenType.Move:
                tween = target.DOMove(routine.Vector, routine.Duration).SetRelative(routine.Relative);
                break;

            case VictoryTweenType.LocalMove:
                tween = target.DOLocalMove(routine.Vector, routine.Duration).SetRelative(routine.Relative);
                break;

            case VictoryTweenType.Rotate:
                tween = target.DORotate(routine.Vector, routine.Duration, routine.Relative ? RotateMode.LocalAxisAdd : RotateMode.Fast);
                break;

            case VictoryTweenType.LocalRotate:
                tween = target.DOLocalRotate(routine.Vector, routine.Duration, routine.Relative ? RotateMode.LocalAxisAdd : RotateMode.Fast);
                break;

            case VictoryTweenType.Scale:
                tween = routine.UniformScale
                    ? target.DOScale(routine.UniformScaleValue, routine.Duration)
                    : target.DOScale(routine.Vector, routine.Duration);
                break;

            case VictoryTweenType.PunchPosition:
                tween = target.DOPunchPosition(routine.Vector, routine.Duration, routine.Vibrato, routine.Elasticity);
                break;

            case VictoryTweenType.PunchRotation:
                tween = target.DOPunchRotation(routine.Vector, routine.Duration, routine.Vibrato, routine.Elasticity);
                break;

            case VictoryTweenType.PunchScale:
                tween = target.DOPunchScale(routine.Vector, routine.Duration, routine.Vibrato, routine.Elasticity);
                break;

            case VictoryTweenType.ShakePosition:
                tween = target.DOShakePosition(routine.Duration, routine.Vector, routine.Vibrato, routine.Randomness, fadeOut: routine.FadeOut);
                break;

            case VictoryTweenType.ShakeRotation:
                tween = target.DOShakeRotation(routine.Duration, routine.Vector, routine.Vibrato, routine.Randomness, routine.FadeOut);
                break;

            case VictoryTweenType.ShakeScale:
                tween = target.DOShakeScale(routine.Duration, routine.Vector, routine.Vibrato, routine.Randomness, routine.FadeOut);
                break;

            case VictoryTweenType.Color:
                tween = BuildColorTween(routine.Target, routine.TargetColor, routine.Duration);
                break;

            case VictoryTweenType.Fade:
                tween = BuildFadeTween(routine.Target, routine.TargetAlpha, routine.Duration);
                break;

            default:
                tween = null;
                break;
        }

        if (tween == null)
            return null;

        // Punch e Shake já tem curva própria, o DOTween Pro nem mostra Ease pra eles
        bool isPunchOrShake = routine.Type is VictoryTweenType.PunchPosition or VictoryTweenType.PunchRotation or VictoryTweenType.PunchScale
            or VictoryTweenType.ShakePosition or VictoryTweenType.ShakeRotation or VictoryTweenType.ShakeScale;

        if (!isPunchOrShake)
            tween.SetEase(routine.EaseType);

        tween.SetDelay(routine.Delay)
            .SetLoops(routine.LoopCount, routine.LoopType);

        return tween;
    }

    // Tenta achar um jeito de tingir o alvo: UI Graphic, TMP, SpriteRenderer ou material
    private Tween BuildColorTween(GameObject target, Color color, float duration)
    {
        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            return graphic.DOColor(color, duration);

        TMP_Text tmpText = target.GetComponent<TMP_Text>();
        if (tmpText != null)
            return DOTween.To(() => tmpText.color, x => tmpText.color = x, color, duration);

        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite != null)
            return sprite.DOColor(color, duration);

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.material.DOColor(color, duration);

        Debug.LogWarning($"[VictoryEffects] Nenhum componente de cor encontrado em '{target.name}'.");
        return null;
    }

    // Tenta achar um jeito de dar fade no alvo: CanvasGroup, UI Graphic, TMP, SpriteRenderer ou material
    private Tween BuildFadeTween(GameObject target, float alpha, float duration)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            return canvasGroup.DOFade(alpha, duration);

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            return graphic.DOFade(alpha, duration);

        TMP_Text tmpText = target.GetComponent<TMP_Text>();
        if (tmpText != null)
            return DOTween.To(() => tmpText.color.a, x =>
            {
                Color c = tmpText.color;
                c.a = x;
                tmpText.color = c;
            }, alpha, duration);

        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite != null)
            return sprite.DOFade(alpha, duration);

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.material.DOFade(alpha, duration);

        Debug.LogWarning($"[VictoryEffects] Nenhum componente de fade encontrado em '{target.name}'.");
        return null;
    }

    private void OnDestroy()
    {
        StopVictoryEffects();
    }
}
