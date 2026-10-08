using UnityEditor;
using UnityEngine;

// Inspector customizado pro VictoryEffects.VictoryRoutine, mostrando só os campos
// relevantes pro tipo de tween escolhido - igual ao componente "DOTween Animation" do DOTween Pro.
[CustomPropertyDrawer(typeof(VictoryEffects.VictoryRoutine))]
public class VictoryRoutineDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty expandedProp = property.FindPropertyRelative("Expanded");
        SerializedProperty nameProp = property.FindPropertyRelative("RoutineName");
        SerializedProperty targetProp = property.FindPropertyRelative("Target");
        SerializedProperty typeProp = property.FindPropertyRelative("Type");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float y = position.y;

        var type = (VictoryEffects.VictoryTweenType)typeProp.enumValueIndex;
        string headerLabel = string.IsNullOrEmpty(nameProp.stringValue) ? "Rotina de Vitória" : nameProp.stringValue;
        headerLabel += "  (" + ObjectNames.NicifyVariableName(type.ToString()) + ")";

        Rect foldoutRect = new Rect(position.x, y, position.width, lineHeight);
        expandedProp.boolValue = EditorGUI.Foldout(foldoutRect, expandedProp.boolValue, headerLabel, true);
        y += lineHeight + Spacing;

        if (!expandedProp.boolValue)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        y = DrawField(position, y, lineHeight, nameProp, new GUIContent("Nome"));
        y = DrawField(position, y, lineHeight, targetProp, new GUIContent("Alvo"));
        y = DrawField(position, y, lineHeight, typeProp, new GUIContent("Tipo"));

        bool isMoveOrRotate = type is VictoryEffects.VictoryTweenType.Move or VictoryEffects.VictoryTweenType.LocalMove
            or VictoryEffects.VictoryTweenType.Rotate or VictoryEffects.VictoryTweenType.LocalRotate;
        bool isScale = type == VictoryEffects.VictoryTweenType.Scale;
        bool isPunch = type is VictoryEffects.VictoryTweenType.PunchPosition or VictoryEffects.VictoryTweenType.PunchRotation or VictoryEffects.VictoryTweenType.PunchScale;
        bool isShake = type is VictoryEffects.VictoryTweenType.ShakePosition or VictoryEffects.VictoryTweenType.ShakeRotation or VictoryEffects.VictoryTweenType.ShakeScale;
        bool isColor = type == VictoryEffects.VictoryTweenType.Color;
        bool isFade = type == VictoryEffects.VictoryTweenType.Fade;
        bool hasEase = !isPunch && !isShake;

        y = DrawLabel(position, y, lineHeight, "Valores");

        if (isMoveOrRotate)
        {
            bool isRotate = type is VictoryEffects.VictoryTweenType.Rotate or VictoryEffects.VictoryTweenType.LocalRotate;
            SerializedProperty vectorProp = property.FindPropertyRelative("Vector");
            SerializedProperty relativeProp = property.FindPropertyRelative("Relative");

            y = DrawField(position, y, lineHeight, vectorProp, new GUIContent(isRotate ? "Rotação" : "Posição"));
            y = DrawField(position, y, lineHeight, relativeProp, new GUIContent("Relativo"));
        }
        else if (isScale)
        {
            SerializedProperty uniformScaleProp = property.FindPropertyRelative("UniformScale");
            SerializedProperty uniformScaleValueProp = property.FindPropertyRelative("UniformScaleValue");
            SerializedProperty vectorProp = property.FindPropertyRelative("Vector");

            y = DrawField(position, y, lineHeight, uniformScaleProp, new GUIContent("Escala Uniforme"));
            y = uniformScaleProp.boolValue
                ? DrawField(position, y, lineHeight, uniformScaleValueProp, new GUIContent("Escala"))
                : DrawField(position, y, lineHeight, vectorProp, new GUIContent("Escala"));
        }
        else if (isPunch)
        {
            SerializedProperty vectorProp = property.FindPropertyRelative("Vector");
            SerializedProperty vibratoProp = property.FindPropertyRelative("Vibrato");
            SerializedProperty elasticityProp = property.FindPropertyRelative("Elasticity");

            y = DrawField(position, y, lineHeight, vectorProp, new GUIContent("Força do Punch"));
            y = DrawField(position, y, lineHeight, vibratoProp, new GUIContent("Vibrato"));
            y = DrawField(position, y, lineHeight, elasticityProp, new GUIContent("Elasticidade"));
        }
        else if (isShake)
        {
            SerializedProperty vectorProp = property.FindPropertyRelative("Vector");
            SerializedProperty vibratoProp = property.FindPropertyRelative("Vibrato");
            SerializedProperty randomnessProp = property.FindPropertyRelative("Randomness");
            SerializedProperty fadeOutProp = property.FindPropertyRelative("FadeOut");

            y = DrawField(position, y, lineHeight, vectorProp, new GUIContent("Força do Shake"));
            y = DrawField(position, y, lineHeight, vibratoProp, new GUIContent("Vibrato"));
            y = DrawField(position, y, lineHeight, randomnessProp, new GUIContent("Aleatoriedade"));
            y = DrawField(position, y, lineHeight, fadeOutProp, new GUIContent("Fade Out"));
        }
        else if (isColor)
        {
            SerializedProperty colorProp = property.FindPropertyRelative("TargetColor");
            y = DrawField(position, y, lineHeight, colorProp, new GUIContent("Cor Final"));
        }
        else if (isFade)
        {
            SerializedProperty alphaProp = property.FindPropertyRelative("TargetAlpha");
            y = DrawField(position, y, lineHeight, alphaProp, new GUIContent("Alpha Final"));
        }

        y = DrawLabel(position, y, lineHeight, "Tween");

        SerializedProperty durationProp = property.FindPropertyRelative("Duration");
        SerializedProperty delayProp = property.FindPropertyRelative("Delay");
        SerializedProperty easeProp = property.FindPropertyRelative("EaseType");
        SerializedProperty loopCountProp = property.FindPropertyRelative("LoopCount");
        SerializedProperty loopTypeProp = property.FindPropertyRelative("LoopType");

        y = DrawField(position, y, lineHeight, durationProp, new GUIContent("Duração"));
        y = DrawField(position, y, lineHeight, delayProp, new GUIContent("Delay"));
        if (hasEase)
            y = DrawField(position, y, lineHeight, easeProp, new GUIContent("Ease"));
        y = DrawField(position, y, lineHeight, loopCountProp, new GUIContent("Loops (-1 = infinito)"));
        y = DrawField(position, y, lineHeight, loopTypeProp, new GUIContent("Tipo de Loop"));

        SerializedProperty onCompleteProp = property.FindPropertyRelative("OnComplete");
        float onCompleteHeight = EditorGUI.GetPropertyHeight(onCompleteProp, true);
        Rect onCompleteRect = new Rect(position.x, y, position.width, onCompleteHeight);
        EditorGUI.PropertyField(onCompleteRect, onCompleteProp, new GUIContent("Ao Completar"), true);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty expandedProp = property.FindPropertyRelative("Expanded");
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float rowHeight = lineHeight + Spacing;

        // Foldout header
        float height = rowHeight;

        if (!expandedProp.boolValue)
            return height;

        SerializedProperty typeProp = property.FindPropertyRelative("Type");
        var type = (VictoryEffects.VictoryTweenType)typeProp.enumValueIndex;

        bool isPunch = type is VictoryEffects.VictoryTweenType.PunchPosition or VictoryEffects.VictoryTweenType.PunchRotation or VictoryEffects.VictoryTweenType.PunchScale;
        bool isShake = type is VictoryEffects.VictoryTweenType.ShakePosition or VictoryEffects.VictoryTweenType.ShakeRotation or VictoryEffects.VictoryTweenType.ShakeScale;
        bool isScale = type == VictoryEffects.VictoryTweenType.Scale;
        bool isColorOrFade = type is VictoryEffects.VictoryTweenType.Color or VictoryEffects.VictoryTweenType.Fade;
        bool hasEase = !isPunch && !isShake;

        int valueRows;
        if (isPunch) valueRows = 3;
        else if (isShake) valueRows = 4;
        else if (isScale) valueRows = 2;
        else if (isColorOrFade) valueRows = 1;
        else valueRows = 2; // Move / LocalMove / Rotate / LocalRotate

        // Nome, Alvo, Tipo (3) + label "Valores" (1) + campos de valor + label "Tween" (1)
        // + Duração, Delay (2) + Ease (0 ou 1) + Loops, Tipo de Loop (2)
        int rows = 3 + 1 + valueRows + 1 + 2 + (hasEase ? 1 : 0) + 2;

        height += rows * rowHeight;

        SerializedProperty onCompleteProp = property.FindPropertyRelative("OnComplete");
        height += EditorGUI.GetPropertyHeight(onCompleteProp, true);

        return height;
    }

    private static float DrawField(Rect position, float y, float lineHeight, SerializedProperty prop, GUIContent label)
    {
        Rect rect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(rect, prop, label);
        return y + lineHeight + Spacing;
    }

    private static float DrawLabel(Rect position, float y, float lineHeight, string text)
    {
        Rect rect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.LabelField(rect, text, EditorStyles.boldLabel);
        return y + lineHeight + Spacing;
    }
}
