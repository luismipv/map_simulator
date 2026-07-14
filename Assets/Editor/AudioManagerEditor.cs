using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private GameObject previewGO;
    private List<AudioSource> previewSources = new List<AudioSource>();
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    
    // NUEVO: Para guardar si las carpetas están abiertas o cerradas
    private Dictionary<string, bool> folderStates = new Dictionary<string, bool>(); 

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
            if (previewSources[i] != null && previewSources[i].isPlaying)
            {
                isAnyPlaying = true;
                break;
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

            // ==========================================
            // LÓGICA DE CARPETAS
            // ==========================================
            // 1. Recopilar todas las categorías únicas que existen en la lista
            List<string> uniqueCategories = new List<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty catProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("category");
                string catName = string.IsNullOrEmpty(catProp.stringValue) ? "General" : catProp.stringValue;
                if (!uniqueCategories.Contains(catName)) uniqueCategories.Add(catName);
            }

            // Ordenamos alfabéticamente para que se vea más limpio (opcional)
            uniqueCategories.Sort();

            // 2. Dibujar cada carpeta
            foreach (string category in uniqueCategories)
            {
                if (!folderStates.ContainsKey(category)) folderStates[category] = true;

                EditorGUILayout.Space(5);
                
                // Le damos un color ligeramente distinto a la carpeta para que resalte
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
                EditorGUILayout.BeginVertical("window");
                GUI.backgroundColor = Color.white;

                folderStates[category] = EditorGUILayout.Foldout(folderStates[category], $"📁 {category}", true, EditorStyles.foldoutHeader);

                if (folderStates[category])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(5);

                    // 3. Dibujar los eventos que pertenecen a esta carpeta
                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
                        SerializedProperty catProp = elementProp.FindPropertyRelative("category");
                        string currentCat = string.IsNullOrEmpty(catProp.stringValue) ? "General" : catProp.stringValue;

                        // Si no es de esta carpeta, lo saltamos y seguimos buscando
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
                            break; // Rompemos el ciclo principal por seguridad al borrar
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();

                        if (foldoutStates[foldoutKey])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.Space(2);
                            
                            // NUEVO: Ahora la categoría se puede editar desde aquí
                            EditorGUILayout.PropertyField(catProp, new GUIContent("Folder / Category"));
                            EditorGUILayout.Space(5);

                            EditorGUILayout.PropertyField(nameProp, new GUIContent("Event Name"));
                            EditorGUILayout.PropertyField(typeProp, new GUIContent("Event Type"));

                            AudioEventType currentType = (AudioEventType)typeProp.enumValueIndex;
                            if (currentType == AudioEventType.SimpleClip)
                            {
                                EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("clip"), new GUIContent("Clip"));
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("clipList"), new GUIContent("Clip List"), true);
                            }

                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("volume"), new GUIContent("Volume"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("isLooping"), new GUIContent("Looping"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("allowOverlap"), new GUIContent("Allow Overlap"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomPitchRange"), new GUIContent("Rand. Pitch Range"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomVolumeRange"), new GUIContent("Rand. Vol. Range"));
                            EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("startAtTime"), new GUIContent("Start At Time"));

                            // WAVEFORM VISUALIZER
                            AudioClip previewClip = null;
                            if (currentType == AudioEventType.SimpleClip)
                            {
                                previewClip = (AudioClip)elementProp.FindPropertyRelative("clip").objectReferenceValue;
                            }
                            else
                            {
                                SerializedProperty cliplistProp = elementProp.FindPropertyRelative("clipList");
                                if (cliplistProp.arraySize > 0)
                                {
                                    previewClip = (AudioClip)cliplistProp.GetArrayElementAtIndex(0).objectReferenceValue;
                                }
                            }

                            if (previewClip != null)
                            {
                                GUILayout.Space(10);
                                Texture2D waveformTexture = AssetPreview.GetAssetPreview(previewClip);
                                
                                if (waveformTexture != null)
                                {
                                    Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 80, 40);
                                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                                    
                                    GUI.color = new Color(0.2f, 0.8f, 0.9f); 
                                    GUI.DrawTexture(rect, waveformTexture, ScaleMode.StretchToFill);
                                    GUI.color = Color.white; 

                                    float startTime = elementProp.FindPropertyRelative("startAtTime").floatValue;
                                    if (startTime < previewClip.length)
                                    {
                                        float startRatio = startTime / previewClip.length;
                                        float startLineX = rect.x + (startRatio * rect.width);
                                        EditorGUI.DrawRect(new Rect(startLineX, rect.y, 2, rect.height), Color.red);
                                    }

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
                                        float playRatio = Mathf.Clamp01(currentPlayTime / previewClip.length);
                                        float playLineX = rect.x + (playRatio * rect.width);
                                        EditorGUI.DrawRect(new Rect(playLineX, rect.y, 2, rect.height), Color.yellow);
                                    }
                                }
                                else
                                {
                                    Repaint();
                                }
                            }
                            
                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space(5);
                        }

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical(); // Fin de la carpeta
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); 
            if (GUILayout.Button("+ Añadir Nuevo Evento", GUILayout.Width(200), GUILayout.Height(30)))
            {
                listProp.arraySize++;
                // Al añadir, le forzamos la categoría General para que no se pierda
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
    }

    private void PlayPreview(SerializedProperty elementProp)
    {
        StopAllPreviews();

        AudioEventType type = (AudioEventType)elementProp.FindPropertyRelative("eventType").enumValueIndex;
        float randomPitchRange = elementProp.FindPropertyRelative("randomPitchRange").floatValue;
        float randomVolumeRange = elementProp.FindPropertyRelative("randomVolumeRange").floatValue;
        float volume = elementProp.FindPropertyRelative("volume").floatValue;
        float startAtTime = elementProp.FindPropertyRelative("startAtTime").floatValue;

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

            previewSources[i].clip = clipsToPlay[i];
            previewSources[i].volume = finalVolume;
            previewSources[i].pitch = finalPitch;
            previewSources[i].time = startAtTime;
            previewSources[i].Play();
        }
    }
}