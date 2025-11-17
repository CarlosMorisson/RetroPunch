using UnityEngine;

public class RainTargetMarker : MonoBehaviour
{
    [Header("Warning Marker")]
    public GameObject warningRing; 
    public GameObject impactCircle;

    [Header("Animation Settings")]
    public float warningTime = 5f;
    public float impactGrowthDuration = 2f;

    private float _timer;
    private bool _isAnimating;
    public GameObject impactEffectPrefab;
    public AudioClip impactSound;

    void Start()
    {
        //if (warningRing != null) warningRing.SetActive(false);
        //if (impactCircle != null) impactCircle.SetActive(false);
    }

    public void ActivateMarker()
    {
   
        _timer = warningTime;
        _isAnimating = true;

        if (warningRing != null)
        {
            warningRing.SetActive(true);
            print("Ativou 1");
        }
        if (impactCircle != null)
        {
            impactCircle.SetActive(true);
            impactCircle.transform.localScale = Vector3.zero;
            print("Ativou 2");
        }
    }

    void Update()
    {
        if (!_isAnimating) return;

        _timer -= Time.deltaTime;

        if (_timer > impactGrowthDuration)
        {
            if (impactCircle != null) impactCircle.transform.localScale = Vector3.zero;
        }
        else if (_timer <= impactGrowthDuration && _timer > 0)
        {
            float progress = 1 - (_timer / impactGrowthDuration);
            if (impactCircle != null)
            {
                impactCircle.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
            }
            if (warningRing != null) warningRing.SetActive(true);
            if (impactCircle != null) impactCircle.SetActive(true);
        }
        else 
        {
            _isAnimating = false;
            if (warningRing != null) warningRing.SetActive(false);
            if (impactCircle != null) impactCircle.SetActive(false);

            if (impactEffectPrefab != null)
            {
                GameObject feedback=Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                Destroy(feedback, warningTime);
            }
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position);
            }

        }
    }
}