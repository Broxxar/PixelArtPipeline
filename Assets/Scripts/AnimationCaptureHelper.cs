using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A component used to help capture FBX animations into sprite sheets.
/// </summary>
public class AnimationCaptureHelper : MonoBehaviour
{
    /// <summary>
    /// The target gameobject to capture.
    /// </summary>
    [SerializeField]
    private GameObject _target = null;

    /// <summary>
    /// The animation clip to capture.
    /// </summary>
    [SerializeField]
    private AnimationClip _sourceClip = null;

    /// <summary>
    /// The FPS to capture the animation at.
    /// </summary>
    [SerializeField]
    private int _framesPerSecond = 30;

    /// <summary>
    /// The output resolution of the rendered sprite.
    /// </summary>
    [SerializeField]
    private Vector2Int _cellSize = new Vector2Int(100, 100);

    /// <summary>
    /// The current key frame to be sampled.
    /// </summary>
    [SerializeField]
    private int _currentFrame = 0;

    /// <summary>
    /// The camera used to render the animation.
    /// </summary>
    [SerializeField]
    private Camera _captureCamera = null;

    /// <summary>
    /// Samples the animation clip onto the taret object.
    /// </summary>
    public void SampleAnimation(float time)
    {
        if (_sourceClip == null || _target == null)
        {
            Debug.LogWarning("SourceClip and Target should be set before sample animation!");
            return;
        }
        else
        {
            _sourceClip.SampleAnimation(_target, time);
        }
    }

    /// <summary>
    /// Captures the animation as individual frames into a texture.
    /// 
    /// Returns IEnumerator the work can be distributed over multiple editor frame.
    /// This is necessary for SkinnedMeshRenders to update between calls to AnimactionClip.Sample()
    /// and Camera.Render(). The provided onComplete action is executed after rendering is finished
    /// so that the textures can be saved to disk.
    /// </summary>
    public IEnumerator CaptureAnimation(Action<Texture2D, Texture2D> onComplete)
    {
        if (_sourceClip == null || _target == null)
        {
            Debug.LogWarning("CaptureCamera should be set before capturing animation!");
            yield break;
        }

        var numFrames = (int)(_sourceClip.length * _framesPerSecond);
        var gridCellCount = SqrtCeil(numFrames);
        var atlasSize = new Vector2Int(_cellSize.x * gridCellCount, _cellSize.y * gridCellCount);
        var atlasPos = new Vector2Int(0, atlasSize.y - _cellSize.y);

        if (atlasSize.x > 4096 || atlasSize.y > 4096)
        {
            Debug.LogErrorFormat("Error attempting to capture an animation with a length and" +
                "resolution that would produce a texture of size: {0}", atlasSize);
        }

        var diffuseMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        ClearAtlas(diffuseMap, Color.clear);

        var normalMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        ClearAtlas(normalMap, new Color(0.5f, 0.5f, 1.0f, 0.0f));

        var rtFrame = new RenderTexture(_cellSize.x, _cellSize.y, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point,
            antiAliasing = 1,
            hideFlags = HideFlags.HideAndDontSave
        };

        var normalCaptureShader = Shader.Find("Hidden/ViewSpaceNormal");

        _captureCamera.targetTexture = rtFrame;
        var cachedCameraColor = _captureCamera.backgroundColor;

        try
        {
            for (_currentFrame = 0; _currentFrame < numFrames; _currentFrame++)
            {
                var currentTime = (_currentFrame / (float)numFrames) * _sourceClip.length;
                SampleAnimation(currentTime);
                yield return null;

                _captureCamera.backgroundColor = Color.clear;
                _captureCamera.Render();
                Graphics.SetRenderTarget(rtFrame);
                diffuseMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
                diffuseMap.Apply();

                _captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
                _captureCamera.RenderWithShader(normalCaptureShader, "");
                Graphics.SetRenderTarget(rtFrame);
                normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
                normalMap.Apply();

                atlasPos.x += _cellSize.x;

                if ((_currentFrame + 1) % gridCellCount == 0)
                {
                    atlasPos.x = 0;
                    atlasPos.y -= _cellSize.y;
                }
            }
            onComplete.Invoke(diffuseMap, normalMap);
        }
        finally
        {
            Graphics.SetRenderTarget(null);
            _captureCamera.targetTexture = null;
            _captureCamera.backgroundColor = cachedCameraColor;
            DestroyImmediate(rtFrame);
        }
    }

    /// <summary>
    /// Returns the ceiled square root of the input.
    /// </summary>
    private int SqrtCeil(int input)
    {
        return Mathf.CeilToInt(Mathf.Sqrt(input));
    }

    /// <summary>
    /// Sets all the pixels in the texture to a specified color.
    /// </summary>
    private void ClearAtlas(Texture2D texture, Color color)
    {
        var pixels = new Color[texture.width * texture.height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
    }
}
