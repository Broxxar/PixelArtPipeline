using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for the AnimationCaptureHelper.
/// </summary>
[CustomEditor(typeof(AnimationCaptureHelper))]
public class AnimationCaptureHelperEditor : Editor
{
    /// <summary>
    /// A message displayed when the target and source clip aren't assigned yet.
    /// </summary>
    private const string ASSIGN_REFS_INFO = "Assign the Target and SourceClip to start previewing!";

    /// <summary>
    /// A message displayed when the assigned animation is not marked as legacy (required for SampleAnimation).
    /// </summary>
    private const string LEGACY_ANIM_WARN = "The SourceClip must be marked as Legacy!";

    /// <summary>
    /// A message displayed when the capture camera isn't assigned yet.
    /// </summary>
    private const string ASSIGN_CAMERA_INFO = "Assign a camera to start capturing!";

    /// <summary>
    /// The current capture routine in progress.
    /// </summary>
    private IEnumerator _currentCaptureRoutine;

    /// <summary>
    /// Draws the custom inspector for the capture helper.
    /// </summary>
    public override void OnInspectorGUI()
    {
        using (new EditorGUI.DisabledScope(_currentCaptureRoutine != null))
        {
            var helper = (AnimationCaptureHelper)target;
            var targetProp = serializedObject.FindProperty("_target");
            var sourceClipProp = serializedObject.FindProperty("_sourceClip");

            EditorGUILayout.PropertyField(targetProp);
            EditorGUILayout.PropertyField(sourceClipProp);

            if (targetProp.objectReferenceValue == null
                || sourceClipProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(ASSIGN_REFS_INFO, MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var sourceClip = (AnimationClip)sourceClipProp.objectReferenceValue;
            if (!sourceClip.legacy)
            {
                EditorGUILayout.HelpBox(LEGACY_ANIM_WARN, MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Animation Options", EditorStyles.boldLabel);

                var fpsProp = serializedObject.FindProperty("_framesPerSecond");
                EditorGUILayout.PropertyField(fpsProp);

                var previewFrameProp = serializedObject.FindProperty("_currentFrame");
                var numFrames = (int)(sourceClip.length * fpsProp.intValue);

                using (var changeScope = new EditorGUI.ChangeCheckScope())
                {
                    var frame = previewFrameProp.intValue;
                    frame = EditorGUILayout.IntSlider("Current Frame", frame, 0, numFrames - 1);

                    if (changeScope.changed)
                    {
                        previewFrameProp.intValue = frame;
                        helper.SampleAnimation((frame / (float)numFrames) * sourceClip.length);
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Capture Options", EditorStyles.boldLabel);

                var captureCameraProp = serializedObject.FindProperty("_captureCamera");
                EditorGUILayout.ObjectField(captureCameraProp, typeof(Camera));

                if (captureCameraProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(ASSIGN_CAMERA_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                var resolutionProp = serializedObject.FindProperty("_cellSize");
                EditorGUILayout.PropertyField(resolutionProp);

                if (GUILayout.Button("Capture"))
                {
                    RunRoutine(helper.CaptureAnimation(SaveCapture));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Starts running the editor routine.
    /// </summary>
    private void RunRoutine(IEnumerator routine)
    {
        _currentCaptureRoutine = routine;
        EditorApplication.update += UpdateRoutine;
    }

    /// <summary>
    /// Calls MoveNext on the routine each editor frame until the iterator terminates.
    /// </summary>
    private void UpdateRoutine()
    {
        if (!_currentCaptureRoutine.MoveNext())
        {
            EditorApplication.update -= UpdateRoutine;
            _currentCaptureRoutine = null;
        }
    }

    /// <summary>
    /// Saves the captured animation sprite atlases to disk.
    /// </summary>
    private void SaveCapture(Texture2D diffuseMap, Texture2D normalMap)
    {
        var diffusePath = EditorUtility.SaveFilePanel("Save Capture", "", "NewCapture", "png");

        if (string.IsNullOrEmpty(diffusePath))
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(diffusePath);
        var directory = Path.GetDirectoryName(diffusePath);
        var normalPath = string.Format("{0}/{1}{2}.{3}", directory, fileName, "NormalMap", "png");

        File.WriteAllBytes(diffusePath, diffuseMap.EncodeToPNG());
        File.WriteAllBytes(normalPath, normalMap.EncodeToPNG());

        AssetDatabase.Refresh();
    }
}
