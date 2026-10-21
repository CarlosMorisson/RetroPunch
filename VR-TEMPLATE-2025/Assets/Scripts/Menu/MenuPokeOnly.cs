using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Deixa apenas o poke ativo no menu: desliga o raio (far casting) e a interacao de UI dos NearFarInteractors.
// O PokeGestureDetector religa o far casting quando a mao abre, por isso ele e forcado a cada frame.
public class MenuPokeOnly : MonoBehaviour
{
    [SerializeField] private List<NearFarInteractor> nearFarInteractors = new List<NearFarInteractor>();
    [SerializeField] private bool disableFarCasting = true;
    [SerializeField] private bool disableUIInteraction = true;

    private readonly Dictionary<NearFarInteractor, (bool farCasting, bool uiInteraction)> originalStates =
        new Dictionary<NearFarInteractor, (bool farCasting, bool uiInteraction)>();

    void OnEnable()
    {
        if (nearFarInteractors.Count == 0)
            nearFarInteractors.AddRange(FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        originalStates.Clear();
        foreach (NearFarInteractor interactor in nearFarInteractors)
        {
            if (interactor != null)
                originalStates[interactor] = (interactor.enableFarCasting, interactor.enableUIInteraction);
        }

        ApplyPokeOnly();
    }

    void LateUpdate()
    {
        ApplyPokeOnly();
    }

    void OnDisable()
    {
        foreach (var pair in originalStates)
        {
            if (pair.Key == null) continue;

            pair.Key.enableFarCasting = pair.Value.farCasting;
            pair.Key.enableUIInteraction = pair.Value.uiInteraction;
        }
        originalStates.Clear();
    }

    private void ApplyPokeOnly()
    {
        foreach (NearFarInteractor interactor in nearFarInteractors)
        {
            if (interactor == null) continue;

            if (disableFarCasting && interactor.enableFarCasting)
                interactor.enableFarCasting = false;

            if (disableUIInteraction && interactor.enableUIInteraction)
                interactor.enableUIInteraction = false;
        }
    }
}
