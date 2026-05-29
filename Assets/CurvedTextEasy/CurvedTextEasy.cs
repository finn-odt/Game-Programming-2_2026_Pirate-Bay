/// <summary>
/// CurvedTextEasy v1.0.0
/// A powerful Unity component for creating curved and circular text effects using TextMeshPro.
/// Add this component to a TextMeshProUGUI or TextMeshPro game object to create a curved and circular text effect.
/// </summary>

using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CurvedTextEasy
{
    [ExecuteAlways]
    public class CurvedTextEasy : MonoBehaviour
    {
        public enum CurveMode
        {
            Circle,
            Curve,
        }

        public enum CurveAxis
        {
            Y,  // Vertical curve
            Z   // Circular wrap
        }

        private TMP_Text text;

        [Header("Curve Mode Settings")]
        [Tooltip("Select the text curve mode")]
        public CurveMode curveMode = CurveMode.Circle;

        [Header("General Parameters")]
        [Tooltip("Select the curve axis")]
        public CurveAxis curveAxis = CurveAxis.Y;

        [Tooltip("Text rotation strength (0 = text stays horizontal, 1 = text fully follows curve rotation)")]
        [SerializeField][Range(0f, 1f)] private float _rotationStrength = 1f;

        /// <summary> Text rotation strength </summary>
        public float rotationStrength
        {
            get => _rotationStrength;
            set
            {
                float clampedValue = Mathf.Clamp01(value);
                if (_rotationStrength != clampedValue)
                {
                    _rotationStrength = clampedValue;
                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        [Header("Curve Mode Parameters")]
        [Tooltip("Animation curve for text bending")]
        public AnimationCurve vertexCurve = new AnimationCurve(
                new Keyframe(0, 0, 0, 30, 0, 0.01f), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0, -30, 0, 0.01f, 0));

        [Tooltip("Curve scaling coefficient")]
        [SerializeField] private float _curveScaling = 100f;

        /// <summary> Curve scaling coefficient </summary>
        public float curveScaling
        {
            get => _curveScaling;
            set
            {
                if (_curveScaling != value)
                {
                    _curveScaling = value;

                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        [Header("Circle Mode Parameters")]
        [Tooltip("Radius of the circular arc")]
        [SerializeField] private float _radius = 100.0f;

        /// <summary> Radius of the circular arc </summary>
        public float radius
        {
            get => _radius;
            set
            {
                if (_radius != value)
                {
                    _radius = value;
                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        [Tooltip("Starting angle of text (degrees). 0°=left, 90°=top, 180°=right, 270°=bottom")]
        [SerializeField] private float _fromArcDegrees = 0.0f;

        /// <summary> Starting angle of text </summary>
        public float fromArcDegrees
        {
            get => _fromArcDegrees;
            set
            {
                if (_fromArcDegrees != value)
                {
                    _fromArcDegrees = value;
                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        [Tooltip("Ending angle of text (degrees). 0°=left, 90°=top, 180°=right, 270°=bottom")]
        [SerializeField] private float _toArcDegrees = 180.0f;

        /// <summary> Ending angle of text </summary>
        public float toArcDegrees
        {
            get => _toArcDegrees;
            set
            {
                if (_toArcDegrees != value)
                {
                    _toArcDegrees = value;
                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        [Tooltip("Maximum angular distance between each letter")]
        [SerializeField] private int _maxDegreesPerLetter = 360;

        /// <summary> Maximum angular distance between each letter </summary>
        public int maxDegreesPerLetter
        {
            get => _maxDegreesPerLetter;
            set
            {
                if (_maxDegreesPerLetter != value)
                {
                    _maxDegreesPerLetter = value;
                    if (!Application.isPlaying || hasInitializedInPlayMode)
                    {
                        RefreshCurve();
                    }
                }
            }
        }

        private bool isForceUpdatingMesh;
        private bool hasInitializedInPlayMode = false;
        private bool isInitializing = false;
        private string lastTextContent = string.Empty;
        private Material lastMaterial = null;
        private bool isMaterialOnlyUpdate = false;

        private bool hasRegisteredCanvasRebuild = false;
        private float lastRebuildTime = 0f;
        private const float REBUILD_INTERVAL = 0.033f; // ~30fps

        private TMP_MeshInfo[] originalMeshInfo = null;

#if UNITY_EDITOR && !UNITY_2021_1_OR_NEWER
        private float lastHierarchyChangeTime = 0f;
        private const float HIERARCHY_CHANGE_INTERVAL = 0.1f;
#endif

        [Header("Debug Info")]
        [Tooltip("Whether to show detailed debug info")]
        public bool showDebugInfo = false;

        /// <summary>
        /// Display debug message if enabled
        /// </summary>
        private void ShowDebugMessage(string message, LogType logType = LogType.Log)
        {
            if (!showDebugInfo) return;

            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogType.Error:
                    Debug.LogError(message);
                    break;
            }
        }

        private void Reset()
        {
            text = gameObject.GetComponent<TMP_Text>();
            vertexCurve = new AnimationCurve(
                new Keyframe(0, 0, 0, 30, 0, 0.01f), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0, -30, 0, 0.01f, 0));
            vertexCurve.preWrapMode = WrapMode.Clamp;
            vertexCurve.postWrapMode = WrapMode.Clamp;

            bool is2DUI = text is TextMeshProUGUI;

            if (is2DUI)
            {
                _curveScaling = 100f;
                _radius = 100f;
            }
            else
            {
                _curveScaling = 2f;
                _radius = 2f;
            }

            WarpText();
        }

        void Awake()
        {
            if (!text) text = gameObject.GetComponent<TMP_Text>();

            if (Application.isPlaying)
            {
                ShowDebugMessage($"[CurvedTextEasy] Awake - GameObject: {gameObject.name}, Text Component: {(text != null ? "Found" : "NULL")}");
            }

#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += PrefabStageOpened;
#else
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
#endif
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        void Start()
        {
            if (Application.isPlaying)
            {
                ShowDebugMessage($"[CurvedTextEasy] Start - GameObject: {gameObject.name}, Text: {(text != null ? text.text : "NULL")}, hasInitialized: {hasInitializedInPlayMode}, isInitializing: {isInitializing}");
            }

            if (Application.isPlaying && text != null && !hasInitializedInPlayMode && !isInitializing)
            {
                ShowDebugMessage($"[CurvedTextEasy] Start - Attempting initialization");
                isInitializing = true;

                if (TryWarpImmediately())
                {
                    ShowDebugMessage($"[CurvedTextEasy] Start - TryWarpImmediately SUCCESS");
                    hasInitializedInPlayMode = true;
                    isInitializing = false;
                }
                else
                {
                    ShowDebugMessage($"[CurvedTextEasy] Start - TryWarpImmediately FAILED, starting coroutine");
                    HideTextRenderer();
                    StartCoroutine(WarpTextAtEndOfFrame());
                }
            }
            else if (Application.isPlaying)
            {
                ShowDebugMessage($"[CurvedTextEasy] Start - Skipped (text={text != null}, hasInitialized={hasInitializedInPlayMode}, isInitializing={isInitializing})");
            }
        }

        /// <summary> Applies curve before each render frame </summary>
        private void OnPreRenderTextHandler(TMP_TextInfo textInfo)
        {
            if (!Application.isPlaying || !hasInitializedInPlayMode || isForceUpdatingMesh)
            {
                return;
            }

            ApplyCurveToTextInfo(textInfo);
        }

        /// <summary> Cache original mesh vertices </summary>
        private void CacheOriginalMeshInfo(TMP_TextInfo textInfo)
        {
            if (textInfo == null || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
            {
                originalMeshInfo = null;
                return;
            }

            originalMeshInfo = new TMP_MeshInfo[textInfo.meshInfo.Length];
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                TMP_MeshInfo sourceMesh = textInfo.meshInfo[i];
                originalMeshInfo[i] = new TMP_MeshInfo();
                originalMeshInfo[i].mesh = sourceMesh.mesh;

                if (sourceMesh.vertices != null && sourceMesh.vertices.Length > 0)
                {
                    originalMeshInfo[i].vertices = new Vector3[sourceMesh.vertices.Length];
                    System.Array.Copy(sourceMesh.vertices, originalMeshInfo[i].vertices, sourceMesh.vertices.Length);
                }
                else
                {
                    originalMeshInfo[i].vertices = null;
                }
            }
        }

        /// <summary> Apply curve effect to TMP_TextInfo </summary>
        private void ApplyCurveToTextInfo(TMP_TextInfo textInfo)
        {
            if (textInfo == null || textInfo.characterCount == 0 || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
            {
                return;
            }

            TMP_MeshInfo[] sourceMeshInfo = originalMeshInfo != null && originalMeshInfo.Length == textInfo.meshInfo.Length
                ? originalMeshInfo
                : textInfo.meshInfo;

            float boundsMinX, boundsMaxX;
            if (originalMeshInfo != null && originalMeshInfo.Length > 0 && originalMeshInfo[0].vertices != null && originalMeshInfo[0].vertices.Length > 0)
            {
                boundsMinX = float.MaxValue;
                boundsMaxX = float.MinValue;
                for (int i = 0; i < originalMeshInfo.Length; i++)
                {
                    if (originalMeshInfo[i].vertices != null)
                    {
                        foreach (Vector3 v in originalMeshInfo[i].vertices)
                        {
                            boundsMinX = Mathf.Min(boundsMinX, v.x);
                            boundsMaxX = Mathf.Max(boundsMaxX, v.x);
                        }
                    }
                }
                if (boundsMinX == float.MaxValue)
                {
                    boundsMinX = text.bounds.min.x;
                    boundsMaxX = text.bounds.max.x;
                }
            }
            else
            {
                boundsMinX = text.bounds.min.x;
                boundsMaxX = text.bounds.max.x;
            }

            if (curveMode == CurveMode.Circle)
            {
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int vertexIndex = charInfo.vertexIndex;
                    int materialIndex = charInfo.materialReferenceIndex;
                    if (materialIndex >= textInfo.meshInfo.Length || materialIndex >= sourceMeshInfo.Length) continue;

                    Vector3[] sourceVertices = sourceMeshInfo[materialIndex].vertices;
                    Vector3[] targetVertices = textInfo.meshInfo[materialIndex].vertices;
                    if (sourceVertices == null || targetVertices == null || vertexIndex + 3 >= sourceVertices.Length || vertexIndex + 3 >= targetVertices.Length) continue;

                    Vector3 offsetToMidBaseline = new Vector2(
                        (sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);

                    Vector3 v0 = sourceVertices[vertexIndex + 0] - offsetToMidBaseline;
                    Vector3 v1 = sourceVertices[vertexIndex + 1] - offsetToMidBaseline;
                    Vector3 v2 = sourceVertices[vertexIndex + 2] - offsetToMidBaseline;
                    Vector3 v3 = sourceVertices[vertexIndex + 3] - offsetToMidBaseline;

                    float zeroToOnePos = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
                    Matrix4x4 matrix = ComputeCircleTransformationMatrix(zeroToOnePos, textInfo, i);

                    targetVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(v0);
                    targetVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(v1);
                    targetVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(v2);
                    targetVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(v3);
                }
            }
            else
            {
                float totalArcLength = CalculateCurveArcLength(boundsMinX, boundsMaxX);
                float totalWidth = 0f;
                float[] charWidths = new float[textInfo.characterCount];

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (textInfo.characterInfo[i].isVisible)
                    {
                        TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                        int vertexIndex = charInfo.vertexIndex;
                        int materialIndex = charInfo.materialReferenceIndex;
                        if (materialIndex < sourceMeshInfo.Length)
                        {
                            Vector3[] tempVertices = sourceMeshInfo[materialIndex].vertices;
                            if (tempVertices != null && vertexIndex + 3 < tempVertices.Length)
                            {
                                charWidths[i] = tempVertices[vertexIndex + 2].x - tempVertices[vertexIndex + 1].x;
                                totalWidth += charWidths[i];
                            }
                        }
                    }
                }

                float currentWidth = 0f;
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int vertexIndex = charInfo.vertexIndex;
                    int materialIndex = charInfo.materialReferenceIndex;
                    if (materialIndex >= textInfo.meshInfo.Length || materialIndex >= sourceMeshInfo.Length) continue;

                    Vector3[] sourceVertices = sourceMeshInfo[materialIndex].vertices;
                    Vector3[] targetVertices = textInfo.meshInfo[materialIndex].vertices;
                    if (sourceVertices == null || targetVertices == null || vertexIndex + 3 >= sourceVertices.Length || vertexIndex + 3 >= targetVertices.Length) continue;

                    float charHalfWidth = charWidths[i] * 0.5f;
                    float centerWidth = currentWidth + charHalfWidth;
                    float ratio = totalWidth > 0.0001f ? centerWidth / totalWidth : 0.5f;
                    float targetArcLength = totalArcLength * ratio;
                    float t = totalArcLength > 0.0001f
                        ? FindParameterFromArcLength(targetArcLength, totalArcLength, boundsMinX, boundsMaxX)
                        : 0.5f;

                    currentWidth += charWidths[i];

                    float curveValue = vertexCurve.Evaluate(t) * curveScaling;
                    Vector3 center = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 1] + sourceVertices[vertexIndex + 2] + sourceVertices[vertexIndex + 3]) / 4f;

                    if (rotationStrength > 0.0001f && !Mathf.Approximately(curveScaling, 0f))
                    {
                        float delta = 0.005f;
                        float tPrev = Mathf.Clamp01(t - delta);
                        float tNext = Mathf.Clamp01(t + delta);
                        float xPrev = Mathf.Lerp(boundsMinX, boundsMaxX, tPrev);
                        float xNext = Mathf.Lerp(boundsMinX, boundsMaxX, tNext);
                        float yPrev = vertexCurve.Evaluate(tPrev) * curveScaling;
                        float yNext = vertexCurve.Evaluate(tNext) * curveScaling;
                        Vector3 tangent = new Vector3(xNext - xPrev, yNext - yPrev, 0);
                        if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.right;
                        tangent.Normalize();
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        if (angle > 90f) angle = 180f - angle;
                        if (angle < -90f) angle = -180f - angle;
                        float finalAngle = angle * rotationStrength;
                        Quaternion rot = curveAxis == CurveAxis.Y
                            ? Quaternion.Euler(0, 0, finalAngle)
                            : Quaternion.Euler(0, finalAngle, 0);
                        Vector3 offset = curveAxis == CurveAxis.Y
                            ? new Vector3(0, curveValue, 0)
                            : new Vector3(0, 0, curveValue);

                        for (int j = 0; j < 4; j++)
                        {
                            Vector3 v = sourceVertices[vertexIndex + j];
                            Vector3 local = v - center;
                            targetVertices[vertexIndex + j] = center + rot * local + offset;
                        }
                    }
                    else
                    {
                        Vector3 offset = curveAxis == CurveAxis.Y
                            ? new Vector3(0, curveValue, 0)
                            : new Vector3(0, 0, curveValue);
                        for (int j = 0; j < 4; j++)
                            targetVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + offset;
                    }
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
                if (meshInfo.vertices != null && meshInfo.vertices.Length > 0 && meshInfo.mesh != null)
                {
                    meshInfo.mesh.vertices = meshInfo.vertices;
                    meshInfo.mesh.RecalculateBounds();
                }
            }
        }

        /// <summary> Debug helper for vertex reset issues </summary>
        private void InvestigateVertexReset(Vector3 currentVertex, Vector3 expectedVertex, float distance)
        {
            ShowDebugMessage($"[CurvedTextEasy] Investigating vertex reset cause...", LogType.Warning);

            UnityEngine.UI.VerticalLayoutGroup vlg = GetComponentInParent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Found VerticalLayoutGroup on: {vlg.gameObject.name}", LogType.Warning);
            }

            UnityEngine.UI.HorizontalLayoutGroup hlg = GetComponentInParent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Found HorizontalLayoutGroup on: {hlg.gameObject.name}", LogType.Warning);
            }

            UnityEngine.UI.GridLayoutGroup glg = GetComponentInParent<UnityEngine.UI.GridLayoutGroup>();
            if (glg != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Found GridLayoutGroup on: {glg.gameObject.name}", LogType.Warning);
            }

            UnityEngine.UI.ContentSizeFitter csf = GetComponentInParent<UnityEngine.UI.ContentSizeFitter>();
            if (csf != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Found ContentSizeFitter on: {csf.gameObject.name}", LogType.Warning);
            }

            if (text != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Text properties - havePropertiesChanged: {text.havePropertiesChanged}, text: '{text.text}'", LogType.Warning);

                if (text.text != lastTextContent)
                {
                    ShowDebugMessage($"[CurvedTextEasy] Text content changed! Previous: '{lastTextContent}', Current: '{text.text}'", LogType.Warning);
                }
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] Canvas found: {canvas.gameObject.name}, renderMode: {canvas.renderMode}, enabled: {canvas.enabled}", LogType.Warning);
            }

            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            ShowDebugMessage($"[CurvedTextEasy] Stack trace (last 10 frames):\n{GetRelevantStackTrace(stackTrace)}", LogType.Warning);
        }

        /// <summary> Get relevant stack trace </summary>
        private string GetRelevantStackTrace(System.Diagnostics.StackTrace stackTrace)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int frameCount = stackTrace.FrameCount;
            int startFrame = System.Math.Max(0, frameCount - 10);

            for (int i = startFrame; i < frameCount; i++)
            {
                System.Diagnostics.StackFrame frame = stackTrace.GetFrame(i);
                if (frame != null)
                {
                    System.Reflection.MethodBase method = frame.GetMethod();
                    if (method != null)
                    {
                        string className = method.DeclaringType != null ? method.DeclaringType.Name : "Unknown";
                        string methodName = method.Name;
                        string fileName = frame.GetFileName();
                        int lineNumber = frame.GetFileLineNumber();

                        if (!className.StartsWith("UnityEngine.") && !className.StartsWith("System."))
                        {
                            sb.AppendLine($"  [{i}] {className}.{methodName}()");
                            if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
                            {
                                sb.AppendLine($"      at {fileName}:{lineNumber}");
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary> Refresh text and re-apply curve </summary>
        private void RefreshTextAndWarp()
        {
            if (text == null) return;

            string savedText = text.text;

            CanvasRenderer canvasRenderer = text.GetComponent<CanvasRenderer>();
            bool wasVisible = false;
            if (canvasRenderer != null)
            {
                wasVisible = canvasRenderer.GetAlpha() > 0;
                if (wasVisible)
                {
                    canvasRenderer.SetAlpha(0);
                }
            }

            text.text = string.Empty;
            text.ForceMeshUpdate(true);

            StartCoroutine(RefreshTextAndWarpCoroutine(savedText, canvasRenderer, wasVisible));
        }

        /// <summary> Restore text and apply curve </summary>
        private IEnumerator RefreshTextAndWarpCoroutine(string savedText, CanvasRenderer canvasRenderer, bool wasVisible)
        {
            yield return null;

            if (text == null) yield break;

            text.text = savedText;
            text.ForceMeshUpdate(true);

            yield return null;

            WarpText();

            if (canvasRenderer != null && wasVisible)
            {
                canvasRenderer.SetAlpha(1);
            }
        }

        private void OnEnable()
        {
            if (text != null)
            {
                if (text is TextMeshProUGUI)
                {
                    TextMeshProUGUI tmpUGUI = text as TextMeshProUGUI;
                    tmpUGUI.OnPreRenderText -= OnPreRenderTextHandler;
                    tmpUGUI.OnPreRenderText += OnPreRenderTextHandler;
                }
                else if (text is TextMeshPro)
                {
                    TextMeshPro tmp = text as TextMeshPro;
                    tmp.OnPreRenderText -= OnPreRenderTextHandler;
                    tmp.OnPreRenderText += OnPreRenderTextHandler;
                }
            }

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged);

            if (text != null && text is TextMeshProUGUI)
            {
                TextMeshProUGUI tmpUGUI = text as TextMeshProUGUI;
                lastMaterial = tmpUGUI.fontSharedMaterial;
            }

            if (NeedsCanvasRebuildListener())
            {
                Canvas.willRenderCanvases -= OnCanvasRebuild;
                Canvas.willRenderCanvases += OnCanvasRebuild;
                hasRegisteredCanvasRebuild = true;
            }
            else
            {
                hasRegisteredCanvasRebuild = false;
            }
            if (Application.isPlaying)
            {
                ShowDebugMessage($"[CurvedTextEasy] OnEnable - GameObject: {gameObject.name}, hasInitialized: {hasInitializedInPlayMode}, isInitializing: {isInitializing}");
                if (!hasInitializedInPlayMode && !isInitializing)
                {
                    ShowDebugMessage($"[CurvedTextEasy] OnEnable - Attempting initialization");
                    isInitializing = true;

                    if (text != null)
                    {
                        if (TryWarpImmediately())
                        {
                            ShowDebugMessage($"[CurvedTextEasy] OnEnable - TryWarpImmediately SUCCESS");
                            hasInitializedInPlayMode = true;
                            isInitializing = false;
                        }
                        else
                        {
                            ShowDebugMessage($"[CurvedTextEasy] OnEnable - TryWarpImmediately FAILED, starting coroutine");
                            HideTextRenderer();
                            StartCoroutine(WarpTextAtEndOfFrame());
                        }
                    }
                    else
                    {
                        ShowDebugMessage($"[CurvedTextEasy] OnEnable - Text component is NULL, resetting flag");
                        isInitializing = false;
                    }
                }
                else
                {
                    ShowDebugMessage($"[CurvedTextEasy] OnEnable - Skipped (hasInitialized={hasInitializedInPlayMode}, isInitializing={isInitializing})");
                }
            }
            else
            {
                StartCoroutine(WarpTextDelayed());
            }
        }

        private void OnDisable()
        {
            if (text != null)
            {
                if (text is TextMeshProUGUI)
                {
                    TextMeshProUGUI tmpUGUI = text as TextMeshProUGUI;
                    tmpUGUI.OnPreRenderText -= OnPreRenderTextHandler;
                }
                else if (text is TextMeshPro)
                {
                    TextMeshPro tmp = text as TextMeshPro;
                    tmp.OnPreRenderText -= OnPreRenderTextHandler;
                }
            }

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);

            if (hasRegisteredCanvasRebuild)
            {
                Canvas.willRenderCanvases -= OnCanvasRebuild;
                hasRegisteredCanvasRebuild = false;
            }

            if (Application.isPlaying)
            {
                isInitializing = false;
                hasInitializedInPlayMode = false;
            }

            if (text != null && Application.isPlaying)
            {
                text.ForceMeshUpdate(true);
            }
        }

        private void OnDestroy()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);

            if (hasRegisteredCanvasRebuild)
            {
                Canvas.willRenderCanvases -= OnCanvasRebuild;
            }

#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened -= PrefabStageOpened;
#else
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
#endif
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        /// <summary> Re-curve text after stopping playback </summary>
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                if (text != null && gameObject != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(WarpTextDelayed());
                }
            }
        }
#endif

        /// <summary> Re-apply curve when Canvas rebuilds </summary>
        private void OnCanvasRebuild()
        {
            if (!Application.isPlaying || !hasInitializedInPlayMode)
            {
                return;
            }

            if (isForceUpdatingMesh)
            {
                return;
            }

            if (text == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] OnCanvasRebuild - Text component is NULL", LogType.Warning);
                return;
            }

            text.ForceMeshUpdate(true);

            TMP_TextInfo textInfo = text.textInfo;
            if (textInfo == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] OnCanvasRebuild - textInfo is NULL", LogType.Warning);
                return;
            }

            if (textInfo.characterCount == 0)
            {
                ShowDebugMessage($"[CurvedTextEasy] OnCanvasRebuild - characterCount is 0", LogType.Warning);
                return;
            }

            if (textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
            {
                ShowDebugMessage($"[CurvedTextEasy] OnCanvasRebuild - meshInfo is invalid", LogType.Warning);
                return;
            }

            if (Time.time - lastRebuildTime < REBUILD_INTERVAL)
            {
                return;
            }

            ShowDebugMessage($"[CurvedTextEasy] OnCanvasRebuild - Re-applying curve effect");
            WarpText();
            lastRebuildTime = Time.time;
        }


        /// <summary> Check if Canvas rebuild listener is needed </summary>
        private bool NeedsCanvasRebuildListener()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null)
                    return true;

                if (current.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null)
                    return true;

                if (current.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null)
                    return true;

                if (current.GetComponent<UnityEngine.UI.ContentSizeFitter>() != null)
                    return true;

                var layoutElement = current.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement != null && !layoutElement.ignoreLayout)
                    return true;

                current = current.parent;
            }

            return false;
        }

#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
        private void PrefabStageOpened(UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            WarpText();
        }
#else
        private void OnHierarchyChanged()
        {
            if (Application.isPlaying || !gameObject.activeInHierarchy || text == null)
                return;

            float currentTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
            if (currentTime - lastHierarchyChangeTime < HIERARCHY_CHANGE_INTERVAL)
                return;

            var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                if (prefabStage.scene == gameObject.scene)
                {
                    lastHierarchyChangeTime = currentTime;
                    StartCoroutine(WarpTextDelayed());
                }
            }
        }
#endif
#endif

        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy) return;

            if (Application.isPlaying)
            {
                if (hasInitializedInPlayMode)
                {
                    ShowDebugMessage($"[CurvedTextEasy] OnValidate - Parameter changed in Play Mode, re-applying curve");
                    StartCoroutine(WarpTextDelayed());
                }
            }
            else
            {
                StartCoroutine(WarpTextDelayed());
            }
        }


        private void ReactToTextChanged(UnityEngine.Object obj)
        {
            if (isForceUpdatingMesh)
            {
                return;
            }

            if (this == null || !this.enabled || !this.gameObject.activeInHierarchy)
            {
                return;
            }

            if (text == null)
            {
                return;
            }

            TMP_Text tmpText = obj as TMP_Text;
            if (tmpText == null || !object.ReferenceEquals(tmpText, text))
            {
                return;
            }

            if (tmpText.gameObject != this.gameObject)
            {
                return;
            }

            string currentTextContent = text.text;
            bool textContentChanged = currentTextContent != lastTextContent;

            Material currentMaterial = null;
            if (text is TextMeshProUGUI)
            {
                TextMeshProUGUI tmpUGUI = text as TextMeshProUGUI;
                currentMaterial = tmpUGUI.fontSharedMaterial;
            }
            bool materialChanged = !object.ReferenceEquals(currentMaterial, lastMaterial);

            bool shouldUpdate = false;
            if (!Application.isPlaying)
            {
                shouldUpdate = textContentChanged || materialChanged;
                isMaterialOnlyUpdate = materialChanged && !textContentChanged;
            }
            else
            {
                shouldUpdate = textContentChanged && hasInitializedInPlayMode;
                isMaterialOnlyUpdate = false;
            }

            if (!shouldUpdate)
            {
                return;
            }

            ShowDebugMessage($"[CurvedTextEasy] ReactToTextChanged - Text changed, isPlaying: {Application.isPlaying}, hasInitialized: {hasInitializedInPlayMode}");

            lastTextContent = currentTextContent;
            lastMaterial = currentMaterial;

            if (Application.isPlaying && hasInitializedInPlayMode)
            {
                text.ForceMeshUpdate(true);
                if (text.textInfo != null)
                {
                    CacheOriginalMeshInfo(text.textInfo);
                    ShowDebugMessage($"[CurvedTextEasy] ReactToTextChanged - Cached original mesh info after text change");
                }
            }

            if (!Application.isPlaying && isMaterialOnlyUpdate)
            {
                ShowDebugMessage($"[CurvedTextEasy] ReactToTextChanged - Editor Mode material change, executing immediately");
                WarpText();
            }
            else
            {
                ShowDebugMessage($"[CurvedTextEasy] ReactToTextChanged - Using delayed execution");
                StartCoroutine(WarpTextDelayed());
            }
        }

        /// <summary> Execute curve effect at EndOfFrame </summary>
        private IEnumerator WarpTextAtEndOfFrame()
        {
            ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Started coroutine");
            yield return new WaitForEndOfFrame();
            ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - After WaitForEndOfFrame");

            int maxRetries = 15;
            int retryCount = 0;
            bool warpSuccessful = false;

            while (retryCount < maxRetries && !warpSuccessful)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}/{maxRetries}");
                if (text != null)
                {
                    text.ForceMeshUpdate(true);

                    TMP_TextInfo textInfo = text.textInfo;
                    if (textInfo == null)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: textInfo is NULL", LogType.Warning);
                        yield return null;
                        retryCount++;
                        continue;
                    }

                    if (textInfo.characterCount == 0)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: characterCount is 0", LogType.Warning);
                        yield return null;
                        retryCount++;
                        continue;
                    }

                    ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: characterCount={textInfo.characterCount}");
                    if (textInfo.meshInfo == null)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: meshInfo is NULL", LogType.Warning);
                        yield return null;
                        retryCount++;
                        continue;
                    }

                    if (textInfo.meshInfo.Length == 0)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: meshInfo.Length is 0", LogType.Warning);
                        yield return null;
                        retryCount++;
                        continue;
                    }

                    bool hasValidVertices = false;
                    int visibleCharCount = 0;
                    for (int i = 0; i < textInfo.characterCount && i < textInfo.characterInfo.Length; i++)
                    {
                        TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                        if (charInfo.isVisible)
                        {
                            visibleCharCount++;
                            int materialIndex = charInfo.materialReferenceIndex;
                            if (materialIndex >= textInfo.meshInfo.Length)
                            {
                                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: Character {i} materialIndex {materialIndex} >= {textInfo.meshInfo.Length}", LogType.Warning);
                                continue;
                            }

                            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                            if (vertices == null)
                            {
                                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: Character {i} vertices is NULL", LogType.Warning);
                                continue;
                            }

                            if (charInfo.vertexIndex + 3 >= vertices.Length)
                            {
                                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: Character {i} vertexIndex {charInfo.vertexIndex} + 3 >= {vertices.Length}", LogType.Warning);
                                continue;
                            }

                            hasValidVertices = true;
                            ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: Found valid vertices at character {i}");
                            break;
                        }
                    }

                    ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: visibleCharCount={visibleCharCount}, hasValidVertices={hasValidVertices}");
                    if (hasValidVertices)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Retry {retryCount + 1}: Calling WarpText()");
                        WarpText();
                        warpSuccessful = true;
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - SUCCESS at retry {retryCount + 1}");
                    }
                    else
                    {
                        yield return null;
                        retryCount++;
                    }
                }
                else
                {
                    ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Text component is NULL, exiting", LogType.Error);
                    break;
                }
            }

            if (!warpSuccessful && text != null)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Failed after {maxRetries} retries, forcing final attempt", LogType.Warning);
                text.ForceMeshUpdate(true);
                WarpText();
            }
            else if (!warpSuccessful)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Failed and text is NULL", LogType.Error);
            }

            if (Application.isPlaying)
            {
                hasInitializedInPlayMode = true;
                isInitializing = false;
                ShowDebugMessage($"[CurvedTextEasy] WarpTextAtEndOfFrame - Marked as initialized, warpSuccessful={warpSuccessful}");
            }
        }

        /// <summary> Wait one frame before executing curve </summary>
        private IEnumerator WarpTextDelayed()
        {
            ShowDebugMessage($"[CurvedTextEasy] WarpTextDelayed - Started, isPlaying: {Application.isPlaying}, hasInitialized: {hasInitializedInPlayMode}");
            yield return null;

            if (Application.isPlaying && hasInitializedInPlayMode)
            {
                if (text != null)
                {
                    text.ForceMeshUpdate(true);
                    TMP_TextInfo textInfo = text.textInfo;
                    if (textInfo == null || textInfo.characterCount == 0 || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpTextDelayed - Mesh not ready, skipping", LogType.Warning);
                        yield break;
                    }
                }
            }

            ShowDebugMessage($"[CurvedTextEasy] WarpTextDelayed - Executing WarpText()");
            WarpText();
        }

        /// <summary> Try to curve text immediately </summary>
        private bool TryWarpImmediately()
        {
            if (text == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Text component is NULL", LogType.Warning);
                return false;
            }

            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Text: {text.text}, ForceMeshUpdate called");
            text.ForceMeshUpdate(true);

            TMP_TextInfo textInfo = text.textInfo;
            if (textInfo == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - textInfo is NULL", LogType.Warning);
                return false;
            }

            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - textInfo.characterCount: {textInfo.characterCount}");
            if (textInfo.characterCount > 0)
            {
                if (textInfo.meshInfo == null)
                {
                    ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - meshInfo is NULL", LogType.Warning);
                    return false;
                }

                if (textInfo.meshInfo.Length == 0)
                {
                    ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - meshInfo.Length is 0", LogType.Warning);
                    return false;
                }

                ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - meshInfo.Length: {textInfo.meshInfo.Length}");
                bool hasValidVertices = false;
                int visibleCharCount = 0;
                for (int i = 0; i < textInfo.characterCount && i < textInfo.characterInfo.Length; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (charInfo.isVisible)
                    {
                        visibleCharCount++;
                        int materialIndex = charInfo.materialReferenceIndex;
                        if (materialIndex >= textInfo.meshInfo.Length)
                        {
                            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Character {i}: materialIndex {materialIndex} >= meshInfo.Length {textInfo.meshInfo.Length}", LogType.Warning);
                            continue;
                        }

                        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                        if (vertices == null)
                        {
                            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Character {i}: vertices is NULL", LogType.Warning);
                            continue;
                        }

                        if (charInfo.vertexIndex + 3 >= vertices.Length)
                        {
                            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Character {i}: vertexIndex {charInfo.vertexIndex} + 3 >= vertices.Length {vertices.Length}", LogType.Warning);
                            continue;
                        }

                        hasValidVertices = true;
                        ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Found valid vertices at character {i}");
                        break;
                    }
                }

                ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - visibleCharCount: {visibleCharCount}, hasValidVertices: {hasValidVertices}");
                if (hasValidVertices)
                {
                    ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Calling WarpText()");
                    WarpText();
                    return true;
                }
            }

            ShowDebugMessage($"[CurvedTextEasy] TryWarpImmediately - Mesh not ready, returning false", LogType.Warning);
            return false;
        }

        /// <summary> Hide text renderer </summary>
        private void HideTextRenderer()
        {
            if (text == null) return;

            CanvasRenderer canvasRenderer = text.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                canvasRenderer.SetAlpha(0);
            }
        }

        /// <summary> Manually trigger curve update </summary>
        public void RefreshCurve()
        {
            if (!Application.isPlaying)
            {
                WarpText();
            }
            else if (hasInitializedInPlayMode)
            {
                WarpText();
            }
        }

        /// <summary> Apply curve to text </summary>
        private void WarpText()
        {
            if (!text)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - Text component is NULL", LogType.Error);
                return;
            }

            ShowDebugMessage($"[CurvedTextEasy] WarpText - Started, Text: {text.text}, Mode: {curveMode}");

            isForceUpdatingMesh = true;

            Vector3[] vertices;
            Matrix4x4 matrix;

            CanvasRenderer canvasRenderer = text.GetComponent<CanvasRenderer>();
            bool wasRendererEnabled = false;
            if (canvasRenderer != null)
            {
                float currentAlpha = canvasRenderer.GetAlpha();
                wasRendererEnabled = currentAlpha > 0;

                if (!wasRendererEnabled && Application.isPlaying)
                {
                    wasRendererEnabled = true;
                }

                bool shouldHide = Application.isPlaying ? wasRendererEnabled : !wasRendererEnabled;
                if (shouldHide)
                {
                    canvasRenderer.SetAlpha(0);
                }
            }

            text.havePropertiesChanged = true;
            text.ForceMeshUpdate(true);

            if (Application.isPlaying)
            {
                text.havePropertiesChanged = false;
            }

            TMP_TextInfo textInfo = text.textInfo;

            if (Application.isPlaying && textInfo != null)
            {
                CacheOriginalMeshInfo(textInfo);
            }
            if (textInfo == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - textInfo is NULL after ForceMeshUpdate", LogType.Error);
                RestoreRenderer(canvasRenderer, wasRendererEnabled);
                isForceUpdatingMesh = false;
                return;
            }

            int characterCount = textInfo.characterInfo.Length;
            ShowDebugMessage($"[CurvedTextEasy] WarpText - characterCount: {textInfo.characterCount}, characterInfo.Length: {characterCount}");

            if (characterCount == 0 || textInfo.characterCount == 0)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - characterCount is 0", LogType.Warning);
                RestoreRenderer(canvasRenderer, wasRendererEnabled);
                isForceUpdatingMesh = false;
                return;
            }

            if (textInfo.meshInfo == null)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - meshInfo is NULL", LogType.Error);
                RestoreRenderer(canvasRenderer, wasRendererEnabled);
                isForceUpdatingMesh = false;
                return;
            }

            if (textInfo.meshInfo.Length == 0)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - meshInfo.Length is 0", LogType.Error);
                RestoreRenderer(canvasRenderer, wasRendererEnabled);
                isForceUpdatingMesh = false;
                return;
            }

            ShowDebugMessage($"[CurvedTextEasy] WarpText - meshInfo.Length: {textInfo.meshInfo.Length}");

            float boundsMinX = text.bounds.min.x;
            float boundsMaxX = text.bounds.max.x;
            float boundsWidth = boundsMaxX - boundsMinX;

            if (curveMode == CurveMode.Curve)
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - Using Curve Mode");
                float totalArcLength = CalculateCurveArcLength(boundsMinX, boundsMaxX);

                float totalWidth = 0f;
                float[] charWidths = new float[characterCount];

                int firstVisibleIndex = -1;
                int lastVisibleIndex = -1;

                int visibleCharCount = 0;
                for (int i = 0; i < characterCount; i++)
                {
                    if (textInfo.characterInfo[i].isVisible)
                    {
                        visibleCharCount++;
                        TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                        int vertexIndex = charInfo.vertexIndex;
                        int materialIndex = charInfo.materialReferenceIndex;

                        float charWidth = 0f;
                        if (materialIndex < textInfo.meshInfo.Length)
                        {
                            Vector3[] tempVertices = textInfo.meshInfo[materialIndex].vertices;
                            if (tempVertices != null && vertexIndex + 3 < tempVertices.Length)
                            {
                                charWidth = tempVertices[vertexIndex + 2].x - tempVertices[vertexIndex + 1].x;
                            }
                        }

                        charWidths[i] = charWidth;
                        totalWidth += charWidth;

                        if (firstVisibleIndex == -1)
                            firstVisibleIndex = i;
                        lastVisibleIndex = i;
                    }
                    else
                    {
                        charWidths[i] = 0f;
                    }
                }

                ShowDebugMessage($"[CurvedTextEasy] WarpText - Curve Mode: visibleCharCount={visibleCharCount}, totalWidth={totalWidth}, boundsWidth={boundsWidth}");

                float currentWidth = 0f;
                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    if (!charInfo.isVisible) continue;

                    int vertexIndex = charInfo.vertexIndex;
                    int materialIndex = charInfo.materialReferenceIndex;

                    if (materialIndex >= textInfo.meshInfo.Length) continue;
                    vertices = textInfo.meshInfo[materialIndex].vertices;
                    if (vertices == null || vertexIndex + 3 >= vertices.Length) continue;

                    float charHalfWidth = charWidths[i] * 0.5f;
                    float centerWidth = currentWidth + charHalfWidth;

                    float ratio = totalWidth > 0.0001f ? centerWidth / totalWidth : 0.5f;

                    float targetArcLength = totalArcLength * ratio;

                    float t = totalArcLength > 0.0001f
                        ? FindParameterFromArcLength(targetArcLength, totalArcLength, boundsMinX, boundsMaxX)
                        : 0.5f;

                    currentWidth += charWidths[i];

                    float curveValue = vertexCurve.Evaluate(t) * curveScaling;
                    float xPos = Mathf.Lerp(boundsMinX, boundsMaxX, t);

                    Vector3 charMidBaselinePos = new Vector3(xPos, 0, 0);

                    Vector3 v0 = vertices[vertexIndex + 0];
                    Vector3 v1 = vertices[vertexIndex + 1];
                    Vector3 v2 = vertices[vertexIndex + 2];
                    Vector3 v3 = vertices[vertexIndex + 3];

                    Vector3 center = (v0 + v1 + v2 + v3) / 4f;

                    if (rotationStrength > 0.0001f && !Mathf.Approximately(curveScaling, 0f))
                    {
                        float delta = 0.005f;
                        float tPrev = Mathf.Clamp01(t - delta);
                        float tNext = Mathf.Clamp01(t + delta);

                        float xPrev = Mathf.Lerp(boundsMinX, boundsMaxX, tPrev);
                        float xNext = Mathf.Lerp(boundsMinX, boundsMaxX, tNext);
                        float yPrev = vertexCurve.Evaluate(tPrev) * curveScaling;
                        float yNext = vertexCurve.Evaluate(tNext) * curveScaling;

                        Vector3 tangent = new Vector3(xNext - xPrev, yNext - yPrev, 0);

                        if (tangent.sqrMagnitude < 1e-8f)
                            tangent = Vector3.right;

                        tangent.Normalize();

                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

                        if (angle > 90f) angle = 180f - angle;
                        if (angle < -90f) angle = -180f - angle;

                        float angleMultiplier = 1f;
                        bool isFirstChar = (i == firstVisibleIndex);
                        bool isLastChar = (i == lastVisibleIndex);
                        if (isFirstChar || isLastChar)
                        {
                            //angleMultiplier = 0.7f;
                        }

                        float finalAngle = angle * rotationStrength * angleMultiplier;

                        Quaternion rot = curveAxis == CurveAxis.Y
                            ? Quaternion.Euler(0, 0, finalAngle)
                            : Quaternion.Euler(0, finalAngle, 0);

                        Vector3 offset = curveAxis == CurveAxis.Y
                            ? new Vector3(0, curveValue, 0)
                            : new Vector3(0, 0, curveValue);

                        for (int j = 0; j < 4; j++)
                        {
                            Vector3 v = vertices[vertexIndex + j];
                            Vector3 local = v - center;
                            Vector3 newPos = center + rot * local + offset;
                            vertices[vertexIndex + j] = newPos;

                            if (Application.isPlaying && i == firstVisibleIndex && j == 0)
                            {
                                ShowDebugMessage($"[CurvedTextEasy] WarpText - Curve Mode: First char vertex modified from {v} to {newPos}, curveValue: {curveValue}, angle: {finalAngle}");
                            }
                        }
                    }
                    else
                    {
                        Vector3 offset = curveAxis == CurveAxis.Y
                            ? new Vector3(0, curveValue, 0)
                            : new Vector3(0, 0, curveValue);

                        for (int j = 0; j < 4; j++)
                            vertices[vertexIndex + j] += offset;
                    }
                }
            }
            else
            {
                ShowDebugMessage($"[CurvedTextEasy] WarpText - Using Circle Mode");
                int processedCharCount = 0;
                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    if (!charInfo.isVisible) continue;

                    int vertexIndex = charInfo.vertexIndex;
                    int materialIndex = charInfo.materialReferenceIndex;

                    if (materialIndex >= textInfo.meshInfo.Length) continue;
                    vertices = textInfo.meshInfo[materialIndex].vertices;
                    if (vertices == null || vertexIndex + 3 >= vertices.Length) continue;

                    Vector3 offsetToMidBaseline = new Vector2(
                        (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, charInfo.baseLine);

                    vertices[vertexIndex + 0] += -offsetToMidBaseline;
                    vertices[vertexIndex + 1] += -offsetToMidBaseline;
                    vertices[vertexIndex + 2] += -offsetToMidBaseline;
                    vertices[vertexIndex + 3] += -offsetToMidBaseline;

                    float zeroToOnePos = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);

                    matrix = ComputeCircleTransformationMatrix(zeroToOnePos, textInfo, i);

                    Vector3 origV0 = vertices[vertexIndex + 0];

                    vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    if (Application.isPlaying && processedCharCount == 0)
                    {
                        ShowDebugMessage($"[CurvedTextEasy] WarpText - Circle Mode: First char vertex modified from {origV0} to {vertices[vertexIndex + 0]}, zeroToOnePos: {zeroToOnePos}");
                    }

                    processedCharCount++;
                }
                ShowDebugMessage($"[CurvedTextEasy] WarpText - Circle Mode: processedCharCount={processedCharCount}");
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
                if (meshInfo.vertices != null && meshInfo.vertices.Length > 0 && meshInfo.mesh != null)
                {
                    meshInfo.mesh.vertices = meshInfo.vertices;
                    meshInfo.mesh.RecalculateBounds();

                    if (Application.isPlaying && meshInfo.vertices.Length > 0)
                    {
                        Vector3 sampleVertex = meshInfo.mesh.vertices[0];
                        ShowDebugMessage($"[CurvedTextEasy] WarpText - Mesh {i} updated, first vertex: {sampleVertex}, vertices count: {meshInfo.vertices.Length}");
                    }
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            ShowDebugMessage($"[CurvedTextEasy] WarpText - UpdateVertexData called, meshInfo.Length: {textInfo.meshInfo.Length}, isPlaying: {Application.isPlaying}, originalMeshInfo cached: {originalMeshInfo != null}");

            lastTextContent = text.text;

            if (text is TextMeshProUGUI)
            {
                TextMeshProUGUI tmpUGUI = text as TextMeshProUGUI;
                lastMaterial = tmpUGUI.fontSharedMaterial;
            }

            RestoreRenderer(canvasRenderer, wasRendererEnabled);
            ShowDebugMessage($"[CurvedTextEasy] WarpText - Renderer restored, wasEnabled: {wasRendererEnabled}");

            isForceUpdatingMesh = false;
            isMaterialOnlyUpdate = false;
            ShowDebugMessage($"[CurvedTextEasy] WarpText - COMPLETED successfully");
        }

        /// <summary> Restore renderer state </summary>
        private void RestoreRenderer(CanvasRenderer canvasRenderer, bool wasEnabled)
        {
            if (canvasRenderer != null && wasEnabled)
            {
                canvasRenderer.SetAlpha(1);
            }
        }

        /// <summary> Calculate total arc length </summary>
        private float CalculateCurveArcLength(float boundsMinX, float boundsMaxX)
        {
            return CalculateCurveArcLengthBetweenParameters(0f, 1f, boundsMinX, boundsMaxX);
        }

        /// <summary> Calculate arc length between parameters </summary>
        private float CalculateCurveArcLengthBetweenParameters(float t0, float t1, float boundsMinX, float boundsMaxX)
        {
            int samples = 100;
            float totalLength = 0f;
            float tRange = t1 - t0;
            float dt = tRange / samples;

            float boundsWidth = boundsMaxX - boundsMinX;
            float dx_dt = boundsWidth;

            for (int i = 0; i < samples; i++)
            {
                float tMid = t0 + (i + 0.5f) * dt;
                float deltaT = 0.0001f;
                float y0 = vertexCurve.Evaluate(Mathf.Clamp01(tMid - deltaT)) * curveScaling;
                float y1 = vertexCurve.Evaluate(Mathf.Clamp01(tMid + deltaT)) * curveScaling;
                float dy_dt = (y1 - y0) / (2f * deltaT);

                float segmentLength = Mathf.Sqrt(dx_dt * dx_dt + dy_dt * dy_dt) * dt;
                totalLength += segmentLength;
            }

            return totalLength;
        }

        /// <summary> Find parameter from arc length </summary>
        private float FindParameterFromArcLength(float targetArcLength, float totalArcLength, float boundsMinX, float boundsMaxX)
        {
            return FindParameterFromArcLengthBetweenParameters(targetArcLength, totalArcLength, 0f, 1f, boundsMinX, boundsMaxX);
        }

        /// <summary> Find parameter from arc length between parameters </summary>
        private float FindParameterFromArcLengthBetweenParameters(float targetArcLength, float totalArcLength, float t0, float t1, float boundsMinX, float boundsMaxX)
        {
            int samples = 100;
            float tRange = t1 - t0;
            float dt = tRange / samples;
            float boundsWidth = boundsMaxX - boundsMinX;
            float dx_dt = boundsWidth;

            float[] cumulativeLengths = new float[samples + 1];
            cumulativeLengths[0] = 0f;

            for (int i = 0; i < samples; i++)
            {
                float tMid = t0 + (i + 0.5f) * dt;
                float deltaT = 0.0001f;
                float y0 = vertexCurve.Evaluate(Mathf.Clamp01(tMid - deltaT)) * curveScaling;
                float y1 = vertexCurve.Evaluate(Mathf.Clamp01(tMid + deltaT)) * curveScaling;
                float dy_dt = (y1 - y0) / (2f * deltaT);

                float segmentLength = Mathf.Sqrt(dx_dt * dx_dt + dy_dt * dy_dt) * dt;
                cumulativeLengths[i + 1] = cumulativeLengths[i] + segmentLength;
            }

            float normalizedTarget = Mathf.Clamp01(targetArcLength / totalArcLength) * cumulativeLengths[samples];

            int left = 0;
            int right = samples;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (cumulativeLengths[mid] < normalizedTarget)
                {
                    left = mid;
                }
                else
                {
                    right = mid;
                }
            }

            float param0 = t0 + left * dt;
            float param1 = t0 + right * dt;
            float length0 = cumulativeLengths[left];
            float length1 = cumulativeLengths[right];

            if (Mathf.Abs(length1 - length0) < 0.0001f)
            {
                return param0;
            }

            float t = Mathf.Lerp(param0, param1, (normalizedTarget - length0) / (length1 - length0));
            return Mathf.Clamp01(t);
        }

        /// <summary> Calculate transformation matrix for Curve mode </summary>
        private Matrix4x4 ComputeCurveTransformationMatrix(float zeroToOnePos, Vector3 offsetToMidBaseline, float boundsMinX, float boundsMaxX)
        {
            float t0 = Mathf.Clamp01(zeroToOnePos);
            float t1 = Mathf.Clamp01(t0 + 0.0001f);

            float boundsWidth = boundsMaxX - boundsMinX;
            float x0 = t0 * boundsWidth + boundsMinX;
            float x1 = t1 * boundsWidth + boundsMinX;

            float curveValue0 = vertexCurve.Evaluate(t0) * curveScaling;
            float curveValue1 = vertexCurve.Evaluate(t1) * curveScaling;

            if (curveAxis == CurveAxis.Y)
            {
                Vector3 horizontal = new Vector3(1, 0, 0);
                Vector3 tangent = new Vector3(x1, curveValue1) - new Vector3(x0, curveValue0);

                if (tangent.sqrMagnitude < 1e-6f)
                {
                    tangent = new Vector3(1, 0, 0);
                }

                float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(horizontal, tangent.normalized), -1f, 1f)) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float angle = cross.z > 0 ? dot : 360 - dot;

                float finalAngle = angle * rotationStrength;

                return Matrix4x4.TRS(new Vector3(0, curveValue0, 0), Quaternion.Euler(0, 0, finalAngle), Vector3.one);
            }
            else
            {
                Vector3 horizontal = new Vector3(1, 0, 0);
                Vector3 tangent = new Vector3(x1, 0, curveValue1) - new Vector3(x0, 0, curveValue0);

                if (tangent.sqrMagnitude < 1e-6f)
                {
                    tangent = new Vector3(1, 0, 0);
                }

                float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(horizontal, tangent.normalized), -1f, 1f)) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float angle = cross.y > 0 ? dot : 360 - dot;

                float finalAngle = angle * rotationStrength;

                return Matrix4x4.TRS(new Vector3(0, 0, curveValue0), Quaternion.Euler(0, finalAngle, 0), Vector3.one);
            }
        }

        /// <summary> Calculate transformation matrix for Circle mode </summary>
        private Matrix4x4 ComputeCircleTransformationMatrix(float zeroToOnePos, TMP_TextInfo textInfo, int charIdx)
        {
            float arcRange = Mathf.Abs(toArcDegrees - fromArcDegrees);
            float actualArcRange = Mathf.Min(arcRange, textInfo.characterCount * maxDegreesPerLetter);

            float centerOffset = (arcRange - actualArcRange) / 2.0f;
            float startAngle = fromArcDegrees < toArcDegrees ?
                fromArcDegrees + centerOffset :
                fromArcDegrees - centerOffset;
            float endAngle = fromArcDegrees < toArcDegrees ?
                startAngle + actualArcRange :
                startAngle - actualArcRange;
            float angleDegrees = Mathf.Lerp(startAngle, endAngle, zeroToOnePos);
            float angle = angleDegrees * Mathf.Deg2Rad;

            float x0 = -Mathf.Cos(angle);
            float y0 = Mathf.Sin(angle);

            float radiusForThisLine = radius;
            if (textInfo.lineInfo != null && textInfo.lineInfo.Length > 0)
            {
                radiusForThisLine = radius - textInfo.lineInfo[0].lineExtents.max.y * textInfo.characterInfo[charIdx].lineNumber;
            }

            if (curveAxis == CurveAxis.Y)
            {
                Vector2 newMidBaselinePos = new Vector2(x0 * radiusForThisLine, y0 * radiusForThisLine);

                float nextZeroToOne = Mathf.Clamp01(zeroToOnePos + 0.0001f);
                float nextAngleDegrees = Mathf.Lerp(startAngle, endAngle, nextZeroToOne);
                float nextAngle = nextAngleDegrees * Mathf.Deg2Rad;
                float x1 = -Mathf.Cos(nextAngle);
                float y1 = Mathf.Sin(nextAngle);

                Vector3 horizontal = new Vector3(1, 0, 0);
                Vector3 tangent = new Vector3(x1, y1, 0) - new Vector3(x0, y0, 0);
                if (tangent.sqrMagnitude < 1e-6f)
                {
                    tangent = new Vector3(-y0, x0, 0);
                }

                tangent.Normalize();

                float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(horizontal, tangent), -1f, 1f)) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float rotationAngle = cross.z > 0 ? dot : 360 - dot;

                float targetAngle = rotationAngle + 180f;
                float finalRotationAngle = targetAngle * rotationStrength;

                return Matrix4x4.TRS(
                    new Vector3(newMidBaselinePos.x, newMidBaselinePos.y, 0),
                    Quaternion.AngleAxis(finalRotationAngle, Vector3.forward),
                    Vector3.one
                );
            }
            else
            {
                Vector2 newMidBaselinePos = new Vector2(x0 * radiusForThisLine, y0 * radiusForThisLine);

                float nextZeroToOne = Mathf.Clamp01(zeroToOnePos + 0.0001f);
                float nextAngleDegrees = Mathf.Lerp(startAngle, endAngle, nextZeroToOne);
                float nextAngle = nextAngleDegrees * Mathf.Deg2Rad;
                float x1 = -Mathf.Cos(nextAngle);
                float y1 = Mathf.Sin(nextAngle);

                Vector3 horizontal = new Vector3(1, 0, 0);
                Vector3 tangent = new Vector3(x1, 0, y1) - new Vector3(x0, 0, y0);
                if (tangent.sqrMagnitude < 1e-6f)
                {
                    tangent = new Vector3(-y0, 0, x0);
                }

                tangent.Normalize();

                float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(horizontal, tangent), -1f, 1f)) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float rotationAngle = cross.y > 0 ? dot : 360 - dot;

                float targetAngle = rotationAngle + 180f;
                float finalAngle = targetAngle * rotationStrength;

                return Matrix4x4.TRS(
                    new Vector3(newMidBaselinePos.x, 0, newMidBaselinePos.y),
                    Quaternion.Euler(0, finalAngle, 0),
                    Vector3.one
                );
            }
        }
    }
}