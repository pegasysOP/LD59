using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioClipVolume))]
public class AudioClipVolumePropertyDrawer : PropertyDrawer
{
    const float VolumeWidth = 120f;
    const float DelayLabelWidth = 38f;
    const float DelayFieldWidth = 50f;
    const float Spacing = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty clipProp = property.FindPropertyRelative("_clip");
        SerializedProperty volumeProp = property.FindPropertyRelative("_volume");
        SerializedProperty delayProp = property.FindPropertyRelative("_delay");

        int indent = EditorGUI.indentLevel;
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            EditorGUI.indentLevel = 0;

            float delayFieldX = position.xMax - DelayFieldWidth;
            float delayLabelX = delayFieldX - DelayLabelWidth;
            float volumeX = delayLabelX - Spacing - VolumeWidth;
            float clipWidth = Mathf.Max(0f, volumeX - Spacing - position.x);

            Rect clipRect = new Rect(position.x, position.y, clipWidth, position.height);
            Rect volumeRect = new Rect(volumeX, position.y, VolumeWidth, position.height);
            Rect delayLabelRect = new Rect(delayLabelX, position.y, DelayLabelWidth, position.height);
            Rect delayFieldRect = new Rect(delayFieldX, position.y, DelayFieldWidth, position.height);

            EditorGUI.PropertyField(clipRect, clipProp, GUIContent.none);
            volumeProp.floatValue = EditorGUI.Slider(volumeRect, GUIContent.none, volumeProp.floatValue, 0f, 1f);
            GUIContent delayLabel = new GUIContent("Delay", "Positive delays playback. Negative trims that many seconds off the start.");
            EditorGUI.LabelField(delayLabelRect, delayLabel);
            delayProp.floatValue = EditorGUI.FloatField(delayFieldRect, delayProp.floatValue);
        }
        EditorGUI.indentLevel = indent;
    }
}
