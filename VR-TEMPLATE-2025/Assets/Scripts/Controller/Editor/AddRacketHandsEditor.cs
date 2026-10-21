using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adiciona o par "CONTROLLER-Racket" no bothHandsList do SwitchHandsController da cena aberta,
// seguindo o mesmo padrão do "CONTROLLER-Baseball" (mesmos materiais, mesmos tipos de referência).
// Rode uma vez pelo menu Tools > Switch Hands > Adicionar CONTROLLER-Racket e salve a cena.
public static class AddRacketHandsEditor
{
    private const string EntryName = "CONTROLLER-Racket";
    private const string TemplateName = "CONTROLLER-Baseball";
    private const string InsertAfterName = "CONTROLLER-Hammer";

    [MenuItem("Tools/Switch Hands/Adicionar CONTROLLER-Racket")]
    private static void AddRacket()
    {
        SwitchHandsController controller = Object.FindFirstObjectByType<SwitchHandsController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("[AddRacket] Nenhum SwitchHandsController na cena aberta.");
            return;
        }

        BothHands template = controller.bothHandsList.Find(b => b != null && b.name == TemplateName);
        if (template == null)
        {
            Debug.LogError($"[AddRacket] '{TemplateName}' não encontrado no bothHandsList (usado como modelo dos materiais).");
            return;
        }

        if (controller.bothHandsList.Exists(b => b != null && b.name == EntryName))
        {
            Debug.LogWarning($"[AddRacket] '{EntryName}' já existe no bothHandsList. Nada foi alterado.");
            return;
        }

        List<string> errors = new List<string>();

        HandSwitch left = BuildHand(template.LeftHand, "Left", "Left Hand Model With Racket",
            "Sucess (1)", "Fail (1)", "Hit_02", "PowerParticle (2)", "FreezeParticle (2)", errors);

        HandSwitch right = BuildHand(template.RightHand, "Right", "Right Hand Model With Racket",
            "Sucess", "Fail", "Hit_02 (1)", "PowerParticle (3)", "FreezeParticle (3)", errors);

        if (errors.Count > 0)
        {
            Debug.LogError("[AddRacket] Não foi possível montar o par, nada foi alterado:\n- " + string.Join("\n- ", errors));
            return;
        }

        Undo.RecordObject(controller, "Adicionar CONTROLLER-Racket");

        int index = controller.bothHandsList.FindIndex(b => b != null && b.name == InsertAfterName);
        index = index < 0 ? controller.bothHandsList.Count : index + 1;
        controller.bothHandsList.Insert(index, new BothHands { name = EntryName, LeftHand = left, RightHand = right });

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

        Debug.Log($"[AddRacket] '{EntryName}' adicionado no bothHandsList (índice {index}). Salve a cena.", controller);
    }

    private static HandSwitch BuildHand(HandSwitch template, string side, string rootName,
        string successName, string failName, string touchName, string powerName, string freezeName,
        List<string> errors)
    {
        GameObject root = FindSceneObject(rootName);
        if (root == null)
        {
            errors.Add($"GameObject '{rootName}' não encontrado na cena.");
            return null;
        }

        GameObject racket = FindChild(root.transform, "tennis_bat1", null, errors);

        return new HandSwitch
        {
            name = side,
            parentObject = root,
            handObject = racket,
            targetRenderer = new List<GameObject> { racket },
            handMaterial = template.handMaterial,
            powerHandMaterial = template.powerHandMaterial,
            freezeHandMaterial = template.freezeHandMaterial,
            successParticle = FindParticle(root, successName, errors),
            failParticle = FindParticle(root, failName, errors),
            touchParticle = FindParticle(root, touchName, errors),
            powerParticle = FindParticle(root, powerName, errors),
            freezeParticle = FindParticle(root, freezeName, errors),
            // Ainda não existe modelo da raquete em Shoot/Preview/3dModels
            nextIndicator = null,
            isController = true,
        };
    }

    private static ParticleSystem FindParticle(GameObject root, string name, List<string> errors)
    {
        GameObject go = FindChild(root.transform, name, null, errors);
        if (go == null)
            return null;

        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        if (particle == null)
            errors.Add($"{root.name}: '{name}' não tem ParticleSystem.");

        return particle;
    }

    /// <summary>Procura um GameObject (inclusive inativo) na cena aberta pelo nome exato.</summary>
    private static GameObject FindSceneObject(string name)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == name && t.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(t))
                return t.gameObject;
        }

        return null;
    }

    /// <summary>Procura um descendente pelo nome exato (opcionalmente exigindo o nome do pai). Precisa ser único.</summary>
    private static GameObject FindChild(Transform root, string name, string parentName, List<string> errors)
    {
        List<GameObject> found = new List<GameObject>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name && (parentName == null || (t.parent != null && t.parent.name == parentName)))
                found.Add(t.gameObject);
        }

        if (found.Count == 1)
            return found[0];

        errors.Add(found.Count == 0
            ? $"{root.name}: '{name}' não encontrado."
            : $"{root.name}: '{name}' aparece {found.Count} vezes, referência ambígua.");
        return null;
    }
}
