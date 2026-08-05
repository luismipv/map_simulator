using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private GameObject previewGO;
    private List<AudioSource> previewSources = new List<AudioSource>();
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> folderStates = new Dictionary<string, bool>();

    private struct PreviewSettings
    {
        public float volume;
        public float pitch;
        public bool isLooping;
        public float stopAtTime;
        public float loopStart;
        public float loopEnd;
        public float loopCrossfade;
        public bool isCrossfading;
        public bool isCrossfadeTarget;
    }

    private Dictionary<AudioSource, PreviewSettings> activePreviewData = new Dictionary<AudioSource, PreviewSettings>();

    private void OnEnable()
    {
        previewGO = EditorUtility.CreateGameObjectWithHideFlags("AudioPreview", HideFlags.HideAndDontSave, typeof(GameObject));
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        if (previewGO != null) DestroyImmediate(previewGO);
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        bool isAnyPlaying = false;
        for (int i = 0; i < previewSources.Count; i++)
        {
            AudioSource src = previewSources[i];
            if (src != null && src.isPlaying && src.clip != null)
            {
                isAnyPlaying = true;

                if (activePreviewData.TryGetValue(src, out PreviewSettings settings))
                {
                    float clipLen = src.clip.length;

                    if (!settings.isLooping)
                    {
                        float stopT = settings.stopAtTime > 0f && settings.stopAtTime <= clipLen ? settings.stopAtTime : clipLen;
                        if (src.time >= stopT)
                        {
                            src.Stop();
                        }
                    }
                    else
                    {
                        float lEnd = settings.loopEnd > 0f && settings.loopEnd <= clipLen ? settings.loopEnd : clipLen;
                        float lStart = Mathf.Clamp(settings.loopStart, 0f, lEnd - 0.01f);
                        float xfadeDur = Mathf.Min(settings.loopCrossfade, lEnd - lStart);

                        // Disparar empalme cruzado cuando la fuente saliente alcanza (lEnd - xfadeDur)
                        if (xfadeDur > 0f && src.time >= (lEnd - xfadeDur) && !settings.isCrossfading)
                        {
                            AudioSource nextSrc = null;
                            for (int j = 0; j < previewSources.Count; j++)
                            {
                                if (previewSources[j] != null && !previewSources[j].isPlaying)
                                {
                                    nextSrc = previewSources[j];
                                    break;
                                }
                            }
                            if (nextSrc == null)
                            {
                                nextSrc = previewGO.AddComponent<AudioSource>();
                                previewSources.Add(nextSrc);
                            }

                            nextSrc.clip = src.clip;
                            nextSrc.pitch = src.pitch;
                            nextSrc.time = lStart;
                            nextSrc.volume = 0f;
                            nextSrc.loop = false;
                            nextSrc.Play();

                            settings.isCrossfading = true;
                            activePreviewData[src] = settings;

                            activePreviewData[nextSrc] = new PreviewSettings
                            {
                                volume = settings.volume,
                                pitch = settings.pitch,
                                isLooping = true,
                                stopAtTime = settings.stopAtTime,
                                loopStart = settings.loopStart,
                                loopEnd = settings.loopEnd,
                                loopCrossfade = settings.loopCrossfade,
                                isCrossfading = false,
                                isCrossfadeTarget = true
                            };
                        }

                        // 1. Fuente en salida (Fade Out de la voz vieja)
                        if (settings.isCrossfading && xfadeDur > 0f)
                        {
                            float progress = Mathf.Clamp01((src.time - (lEnd - xfadeDur)) / xfadeDur);
                            src.volume = Mathf.Lerp(settings.volume, 0f, progress);

                            if (src.time >= lEnd)
                            {
                                src.Stop();
                                src.volume = settings.volume;
                            }
                        }
                        // 2. Fuente en entrada resultante de un empalme (Fade In de la voz nueva)
                        else if (settings.isCrossfadeTarget && xfadeDur > 0f)
                        {
                            if (src.time < (lStart + xfadeDur))
                            {
                                float progress = Mathf.Clamp01((src.time - lStart) / xfadeDur);
                                src.volume = Mathf.Lerp(0f, settings.volume, progress);
                            }
                            else
                            {
                                src.volume = settings.volume;
                                settings.isCrossfadeTarget = false;
                                activePreviewData[src] = settings;
                            }
                        }
                        // 3. Fuente inicial o loop simple (Se reproduce a volumen normal)
                        else if (xfadeDur <= 0f && src.time >= lEnd)
                        {
                            src.time = lStart;
                        }
                    }
                }
            }
        }

        if (isAnyPlaying) Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty listProp = serializedObject.FindProperty("fallbackAudioEvents");
        
        EditorGUILayout.Space(5);
        listProp.isExpanded = EditorGUILayout.Foldout(listProp.isExpanded, "Bypass: Eventos Nativos de Unity", true, EditorStyles.foldoutHeader);
        
        if (listProp.isExpanded)
        {
            EditorGUI.indentLevel++;

            List<string> uniqueCategories = new List<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty catProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("category");
                string catName = string.IsNullOrEmpty(catProp.stringValue) ? "General" : catProp.stringValue;
                if (!uniqueCategories.Contains(catName)) uniqueCategories.Add(catName);
            }

            uniqueCategories.Sort();

            foreach (string category in uniqueCategories)
            {
                if (!folderStates.ContainsKey(category)) folderStates[category] = true;

                EditorGUILayout.Space(5);
                
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
                EditorGUILayout.BeginVertical("window");
                GUI.backgroundColor = Color.white;

                folderStates[category] = EditorGUILayout.Foldout(folderStates[category], $"📁 {category}", true, EditorStyles.foldoutHeader);

                if (folderStates[category])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(5);

                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
                        SerializedProperty catProp = elementProp.FindPropertyRelative("category");
                        string currentCat = string.IsNullOrEmpty(catProp.stringValue) ? "General" : catProp.stringValue;

                        if (currentCat != category) continue;

                        SerializedProperty nameProp = elementProp.FindPropertyRelative("eventName");
                        SerializedProperty typeProp = elementProp.FindPropertyRelative("eventType");

                        string eventNameString = string.IsNullOrEmpty(nameProp.stringValue) ? $"Element {i}" : nameProp.stringValue;
                        string typeLabel = ((AudioEventType)typeProp.enumValueIndex).ToString();
                        string foldoutKey = elementProp.propertyPath;

                        if (!foldoutStates.ContainsKey(foldoutKey)) foldoutStates[foldoutKey] = false;

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        
                        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], $"{eventNameString} ({typeLabel})", true);
                        
                        GUILayout.FlexibleSpace();
                        
                        GUI.backgroundColor = new Color(1f, 0.6f, 0.2f); 
                        if (GUILayout.Button("PLAY", GUILayout.Width(55), GUILayout.Height(18)))
                        {
                            PlayPreview(elementProp);
                        }
                        
                        GUI.backgroundColor = Color.white;
                        if (GUILayout.Button("STOP", GUILayout.Width(55), GUILayout.Height(18)))
                        {
                            StopAllPreviews();
                        }

                        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f); 
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            listProp.DeleteArrayElementAtIndex(i);
                            GUI.backgroundColor = Color.white;
                            break;
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();

                        if (foldoutStates[foldoutKey])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.Space(2);
                            
                            EditorGUILayout.PropertyField(catProp, new GUIContent("Folder / Category"));
                            EditorGUILayout.Space(5);

                            EditorGUILayout.PropertyField(nameProp, new GUIContent("Event Name"));
                            EditorGUILayout.PropertyField(typeProp, new GUIContent("Event Type"));

                            AudioEventType currentType = (AudioEventType)typeProp.enumValueIndex;
                            AudioClip previewClip = null;

                            if (currentType == AudioEventType.SimpleClip)
                            {
                                SerializedProperty clipProp = elementProp.FindPropertyRelative("clip");
                                EditorGUILayout.PropertyField(clipProp, new GUIContent("Clip"));
                                previewClip = (AudioClip)clipProp.objectReferenceValue;
                            }
                            else
                            {
                                SerializedProperty cliplistProp = elementProp.FindPropertyRelative("clipList");
                                EditorGUILayout.PropertyField(cliplistProp, new GUIContent("Clip List"), true);
                                if (cliplistProp.arraySize > 0)
                                {
                                    previewClip = (AudioClip)cliplistProp.GetArrayElementAtIndex(0).objectReferenceValue;
                                }
                            }

                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("volume"), new GUIContent("Volume"));
                            SerializedProperty isLoopingProp = elementProp.FindPropertyRelative("isLooping");
                            EditorGUILayout.PropertyField(isLoopingProp, new GUIContent("Looping"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("allowOverlap"), new GUIContent("Allow Overlap"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomPitchRange"), new GUIContent("Rand. Pitch Range"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomVolumeRange"), new GUIContent("Rand. Vol. Range"));

                            // SLIDERS Y CONTROLES DE TIEMPO
                            SerializedProperty startAtProp = elementProp.FindPropertyRelative("startAtTime");
                            float clipLength = previewClip != null ? previewClip.length : 100f;

                            if (previewClip != null)
                            {
                                startAtProp.floatValue = EditorGUILayout.Slider("Start At Time (s)", startAtProp.floatValue, 0f, clipLength);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(startAtProp, new GUIContent("Start At Time (s)"));
                            }

                            if (!isLoopingProp.boolValue)
                            {
                                SerializedProperty stopAtProp = elementProp.FindPropertyRelative("stopAtTime");
                                if (previewClip != null)
                                {
                                    stopAtProp.floatValue = EditorGUILayout.Slider("Stop At Time (s, 0=End)", stopAtProp.floatValue, 0f, clipLength);
                                }
                                else
                                {
                                    EditorGUILayout.PropertyField(stopAtProp, new GUIContent("Stop At Time (s)"));
                                }
                            }
                            else
                            {
                                SerializedProperty loopStartProp = elementProp.FindPropertyRelative("loopStart");
                                SerializedProperty loopEndProp = elementProp.FindPropertyRelative("loopEnd");
                                SerializedProperty loopCrossfadeProp = elementProp.FindPropertyRelative("loopCrossfade");

                                if (previewClip != null)
                                {
                                    loopStartProp.floatValue = EditorGUILayout.Slider("Loop Start (s)", loopStartProp.floatValue, 0f, clipLength);
                                    loopEndProp.floatValue = EditorGUILayout.Slider("Loop End (s, 0=End)", loopEndProp.floatValue, 0f, clipLength);
                                    loopCrossfadeProp.floatValue = EditorGUILayout.Slider("Loop Crossfade (s)", loopCrossfadeProp.floatValue, 0f, 5f);
                                }
                                else
                                {
                                    EditorGUILayout.PropertyField(loopStartProp, new GUIContent("Loop Start (s)"));
                                    EditorGUILayout.PropertyField(loopEndProp, new GUIContent("Loop End (s)"));
                                    EditorGUILayout.PropertyField(loopCrossfadeProp, new GUIContent("Loop Crossfade (s)"));
                                }
                            }

                            // WAVEFORM VISUALIZER (ESTILO WWISE CON LÍNEAS DIAGONALES)
                            if (previewClip != null)
                            {
                                EditorGUILayout.Space(8);
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Label("<b>Visualizador de Audio</b>", new GUIStyle(EditorStyles.label) { richText = true });
                                GUILayout.FlexibleSpace();
                                GUILayout.Label($"Duración total: {previewClip.length:F2}s", EditorStyles.miniLabel);
                                EditorGUILayout.EndHorizontal();

                                Texture2D waveformTexture = AssetPreview.GetAssetPreview(previewClip);
                                
                                if (waveformTexture != null)
                                {
                                    Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 90, 60);
                                    
                                    // 1. Fondo oscuro base
                                    EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f));
                                    
                                    // 2. Textura de la forma de onda
                                    GUI.color = new Color(0.25f, 0.75f, 0.95f); 
                                    GUI.DrawTexture(rect, waveformTexture, ScaleMode.StretchToFill);
                                    GUI.color = Color.white; 

                                    float startTime = startAtProp.floatValue;
                                    bool isLoop = isLoopingProp.boolValue;
                                    float stopTime = isLoop ? 0f : elementProp.FindPropertyRelative("stopAtTime").floatValue;
                                    float lStart = isLoop ? elementProp.FindPropertyRelative("loopStart").floatValue : 0f;
                                    float lEnd = isLoop ? elementProp.FindPropertyRelative("loopEnd").floatValue : 0f;
                                    float lXfade = isLoop ? elementProp.FindPropertyRelative("loopCrossfade").floatValue : 0f;

                                    float effectiveStop = (!isLoop && stopTime > 0f && stopTime <= clipLength) ? stopTime : clipLength;
                                    float effectiveLoopEnd = (isLoop && lEnd > 0f && lEnd <= clipLength) ? lEnd : clipLength;

                                    // 3. Región inactiva antes de StartAtTime
                                    if (startTime > 0f && startTime < clipLength)
                                    {
                                        float startW = (startTime / clipLength) * rect.width;
                                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, startW, rect.height), new Color(0f, 0f, 0f, 0.6f));
                                    }

                                    // 4. Región inactiva después de StopAtTime (Oneshot)
                                    if (!isLoop && stopTime > 0f && stopTime < clipLength)
                                    {
                                        float stopX = rect.x + ((stopTime / clipLength) * rect.width);
                                        float stopW = rect.width - ((stopTime / clipLength) * rect.width);
                                        EditorGUI.DrawRect(new Rect(stopX, rect.y, stopW, rect.height), new Color(0f, 0f, 0f, 0.6f));
                                    }

                                    // 5. Región de Loop tintada
                                    if (isLoop)
                                    {
                                        float loopStartX = rect.x + ((lStart / clipLength) * rect.width);
                                        float loopEndX = rect.x + ((effectiveLoopEnd / clipLength) * rect.width);
                                        float loopW = loopEndX - loopStartX;
                                        if (loopW > 0f)
                                        {
                                            EditorGUI.DrawRect(new Rect(loopStartX, rect.y, loopW, rect.height), new Color(0f, 0.8f, 1f, 0.12f));
                                        }

                                        // Región inactiva después del LoopEnd
                                        if (lEnd > 0f && lEnd < clipLength)
                                        {
                                            float afterLoopW = rect.width - ((lEnd / clipLength) * rect.width);
                                            EditorGUI.DrawRect(new Rect(loopEndX, rect.y, afterLoopW, rect.height), new Color(0f, 0f, 0f, 0.6f));
                                        }
                                    }

                                    // 6. DIBUJO DE LÍNEAS DIAGONALES ESTILO WWISE
                                    if (isLoop && lXfade > 0f)
                                    {
                                        float xfadeDur = Mathf.Min(lXfade, effectiveLoopEnd - lStart);
                                        float xfadeStart = Mathf.Max(0f, effectiveLoopEnd - xfadeDur);

                                        float xXfadeStart = rect.x + ((xfadeStart / clipLength) * rect.width);
                                        float xLoopEnd = rect.x + ((effectiveLoopEnd / clipLength) * rect.width);
                                        float xfadeW = xLoopEnd - xXfadeStart;

                                        // Fondo naranja translúcido para el rango de empalme
                                        EditorGUI.DrawRect(new Rect(xXfadeStart, rect.y, Mathf.Max(1f, xfadeW), rect.height), new Color(1f, 0.55f, 0f, 0.2f));

                                        // LÍNEA DIAGONAL SALIDA (Fade Out: \ de arriba a abajo)
                                        Handles.color = new Color(1f, 0.6f, 0f, 0.95f);
                                        Handles.DrawAAPolyLine(2.5f, new Vector3(xXfadeStart, rect.y, 0), new Vector3(xLoopEnd, rect.yMax, 0));

                                        // LÍNEA DIAGONAL ENTRADA SIMULADA EN SALIDA (Fade In: / de abajo a arriba)
                                        Handles.color = new Color(0f, 0.9f, 1f, 0.75f);
                                        Handles.DrawAAPolyLine(1.5f, new Vector3(xXfadeStart, rect.yMax, 0), new Vector3(xLoopEnd, rect.y, 0));

                                        // LÍNEA DIAGONAL EN LUGAR DE ENTRADA (Loop Start -> Loop Start + xfade)
                                        float xLoopStart = rect.x + ((lStart / clipLength) * rect.width);
                                        float xLoopStartFadeEnd = rect.x + (((lStart + xfadeDur) / clipLength) * rect.width);

                                        // LÍNEA DIAGONAL ENTRADA REAL (Fade In: / de abajo a arriba en Loop Start)
                                        Handles.color = new Color(0f, 0.9f, 1f, 0.95f);
                                        Handles.DrawAAPolyLine(2.5f, new Vector3(xLoopStart, rect.yMax, 0), new Vector3(xLoopStartFadeEnd, rect.y, 0));

                                        // LÍNEA DIAGONAL SALIDA ANTERIOR (Fade Out: \ de arriba a abajo en Loop Start)
                                        Handles.color = new Color(1f, 0.6f, 0f, 0.75f);
                                        Handles.DrawAAPolyLine(1.5f, new Vector3(xLoopStart, rect.y, 0), new Vector3(xLoopStartFadeEnd, rect.yMax, 0));
                                    }

                                    // 7. Líneas demarcadoras de colores verticales
                                    if (startTime < clipLength)
                                    {
                                        float startX = rect.x + ((startTime / clipLength) * rect.width);
                                        EditorGUI.DrawRect(new Rect(startX, rect.y, 2, rect.height), new Color(0.2f, 0.9f, 0.3f)); 
                                    }

                                    if (!isLoop && stopTime > 0f && stopTime < clipLength)
                                    {
                                        float stopX = rect.x + ((stopTime / clipLength) * rect.width);
                                        EditorGUI.DrawRect(new Rect(stopX, rect.y, 2, rect.height), new Color(0.95f, 0.25f, 0.25f)); 
                                    }

                                    if (isLoop)
                                    {
                                        if (lStart > 0f && lStart < clipLength)
                                        {
                                            float lStartX = rect.x + ((lStart / clipLength) * rect.width);
                                            EditorGUI.DrawRect(new Rect(lStartX, rect.y, 2, rect.height), new Color(0f, 0.9f, 1f)); 
                                        }

                                        if (lEnd > 0f && lEnd < clipLength)
                                        {
                                            float lEndX = rect.x + ((lEnd / clipLength) * rect.width);
                                            EditorGUI.DrawRect(new Rect(lEndX, rect.y, 2, rect.height), new Color(1f, 0.3f, 0.8f)); 
                                        }
                                    }

                                    // 8. Cabezal de reproducción (Amarillo brillante)
                                    bool isThisClipPlaying = false;
                                    float currentPlayTime = 0f;

                                    foreach(var src in previewSources)
                                    {
                                        if (src != null && src.isPlaying && src.clip == previewClip)
                                        {
                                            isThisClipPlaying = true;
                                            currentPlayTime = src.time;
                                            break;
                                        }
                                    }

                                    if (isThisClipPlaying)
                                    {
                                        float playRatio = Mathf.Clamp01(currentPlayTime / clipLength);
                                        float playLineX = rect.x + (playRatio * rect.width);
                                        EditorGUI.DrawRect(new Rect(playLineX, rect.y, 2, rect.height), new Color(1f, 0.92f, 0.01f));
                                    }
                                }
                                else
                                {
                                    Repaint();
                                }

                                // 9. Leyenda Limpia de Colores
                                EditorGUILayout.Space(4);
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();

                                GUIStyle legendStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };
                                string legendText = "<color=#33E64D>🟢 Start</color>  ";
                                if (!isLoopingProp.boolValue)
                                {
                                    legendText += "<color=#F24040>🔴 Stop</color>  ";
                                }
                                else
                                {
                                    legendText += "<color=#00E6FF>🩵 Loop Start (Fade In /)</color>  <color=#FF4DCD>🩷 Loop End</color>  <color=#FF8C00>🟧 Crossfade (\\)</color>  ";
                                }
                                legendText += "<color=#FFEB03>🟡 Reproducción</color>";

                                GUILayout.Label(legendText, legendStyle);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.EndVertical();
                            }
                            
                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space(5);
                        }

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); 
            if (GUILayout.Button("+ Añadir Nuevo Evento", GUILayout.Width(200), GUILayout.Height(30)))
            {
                listProp.arraySize++;
                SerializedProperty newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newElement.FindPropertyRelative("category").stringValue = "General";
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void StopAllPreviews()
    {
        foreach (var src in previewSources)
        {
            if (src != null) src.Stop();
        }
        activePreviewData.Clear();
    }

    private void PlayPreview(SerializedProperty elementProp)
    {
        StopAllPreviews();

        AudioEventType type = (AudioEventType)elementProp.FindPropertyRelative("eventType").enumValueIndex;
        float randomPitchRange = elementProp.FindPropertyRelative("randomPitchRange").floatValue;
        float randomVolumeRange = elementProp.FindPropertyRelative("randomVolumeRange").floatValue;
        float volume = elementProp.FindPropertyRelative("volume").floatValue;
        float startAtTime = elementProp.FindPropertyRelative("startAtTime").floatValue;

        bool isLooping = elementProp.FindPropertyRelative("isLooping").boolValue;
        float stopAtTime = elementProp.FindPropertyRelative("stopAtTime").floatValue;
        float loopStart = elementProp.FindPropertyRelative("loopStart").floatValue;
        float loopEnd = elementProp.FindPropertyRelative("loopEnd").floatValue;
        float loopCrossfade = elementProp.FindPropertyRelative("loopCrossfade").floatValue;

        List<AudioClip> clipsToPlay = new List<AudioClip>();

        if (type == AudioEventType.SimpleClip)
        {
            clipsToPlay.Add((AudioClip)elementProp.FindPropertyRelative("clip").objectReferenceValue);
        }
        else
        {
            SerializedProperty listProp = elementProp.FindPropertyRelative("clipList");
            if (listProp.arraySize > 0)
            {
                if (type == AudioEventType.BlendContainer)
                {
                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        clipsToPlay.Add((AudioClip)listProp.GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                }
                else if (type == AudioEventType.RandomContainer || type == AudioEventType.SequenceContainer)
                {
                    int targetIdx = Random.Range(0, listProp.arraySize); 
                    clipsToPlay.Add((AudioClip)listProp.GetArrayElementAtIndex(targetIdx).objectReferenceValue);
                }
            }
        }

        while (previewSources.Count < clipsToPlay.Count)
        {
            previewSources.Add(previewGO.AddComponent<AudioSource>());
        }

        for (int i = 0; i < clipsToPlay.Count; i++)
        {
            if (clipsToPlay[i] == null) continue;

            float finalPitch = 1f + Random.Range(-randomPitchRange, randomPitchRange);
            float finalVolume = Mathf.Clamp01(volume - Random.Range(0f, randomVolumeRange));

            AudioSource src = previewSources[i];
            src.clip = clipsToPlay[i];
            src.volume = finalVolume;
            src.pitch = finalPitch;
            src.time = Mathf.Clamp(startAtTime, 0f, clipsToPlay[i].length - 0.01f);
            src.loop = false;
            src.Play();

            activePreviewData[src] = new PreviewSettings
            {
                volume = finalVolume,
                pitch = finalPitch,
                isLooping = isLooping,
                stopAtTime = stopAtTime,
                loopStart = loopStart,
                loopEnd = loopEnd,
                loopCrossfade = loopCrossfade,
                isCrossfading = false
            };
        }
    }
}