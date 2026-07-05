using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private AudioSource previewSource;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    private void OnEnable()
    {
        GameObject previewGO = EditorUtility.CreateGameObjectWithHideFlags("AudioPreview", HideFlags.HideAndDontSave, typeof(AudioSource));
        previewSource = previewGO.GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        if (previewSource != null)
        {
            DestroyImmediate(previewSource.gameObject);
        }
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
            
            // ELIMINADO: El campo de Size para evitar accidentes.

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);
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
                
                // Botón PLAY
                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f); 
                if (GUILayout.Button("PLAY", GUILayout.Width(55), GUILayout.Height(18)))
                {
                    PlayPreview(elementProp);
                }
                
                // Botón STOP
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("STOP", GUILayout.Width(55), GUILayout.Height(18)))
                {
                    previewSource.Stop();
                }

                // Botón ELIMINAR (X)
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f); 
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    GUI.backgroundColor = Color.white;
                    break; // Rompemos el ciclo para evitar errores de GUI al modificar la lista mientras se dibuja
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();

                if (foldoutStates[foldoutKey])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(2);
                    
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
                    EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomPitchRange"), new GUIContent("Rand. Pitch Range"));
                    EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("randomVolumeRange"), new GUIContent("Rand. Vol. Range"));
                    EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("startAtTime"), new GUIContent("Start At Time"));

                    // ==========================================
                    // NUEVO: VISUALIZADOR DE FORMA DE ONDA
                    // ==========================================
                    AudioClip previewClip = null;
                    if (currentType == AudioEventType.SimpleClip)
                    {
                        previewClip = (AudioClip)elementProp.FindPropertyRelative("clip").objectReferenceValue;
                    }
                    else
                    {
                        // En contenedores, previsualizamos el primer clip de la lista para darnos una idea
                        SerializedProperty cliplistProp = elementProp.FindPropertyRelative("clipList");
                        if (cliplistProp.arraySize > 0)
                        {
                            previewClip = (AudioClip)cliplistProp.GetArrayElementAtIndex(0).objectReferenceValue;
                        }
                    }

                    if (previewClip != null)
                    {
                        GUILayout.Space(10);
                        // Unity genera una textura del waveform en segundo plano
                        Texture2D waveformTexture = AssetPreview.GetAssetPreview(previewClip);
                        
                        if (waveformTexture != null)
                        {
                            // Reservamos un rectángulo en la interfaz (Ancho dinámico, 40px de alto)
                            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 60, 40);
                            
                            // Fondo oscuro estilo Logic Pro
                            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                            
                            // Dibujamos el Waveform
                            GUI.color = new Color(0.2f, 0.8f, 0.9f); // Azul cian
                            GUI.DrawTexture(rect, waveformTexture, ScaleMode.StretchToFill);
                            GUI.color = Color.white; // Reseteamos color

                            // Calculamos y dibujamos la línea roja del Start Time
                            float startTime = elementProp.FindPropertyRelative("startAtTime").floatValue;
                            if (startTime < previewClip.length)
                            {
                                float ratio = Mathf.Clamp01(startTime / previewClip.length);
                                float lineX = rect.x + (ratio * rect.width);
                                EditorGUI.DrawRect(new Rect(lineX, rect.y, 2, rect.height), Color.red);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("El Start Time es mayor a la duración del audio.", MessageType.Warning);
                            }
                        }
                        else
                        {
                            // AssetPreview es asíncrono; si devuelve null, forzamos al editor a repintar 
                            // hasta que la textura esté lista.
                            Repaint();
                        }
                    }
                    // ==========================================
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.EndVertical();
            }

            // BOTÓN SEGURO PARA AÑADIR EVENTOS
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f); // Verde amigable
            if (GUILayout.Button("+ Añadir Nuevo Evento", GUILayout.Width(200), GUILayout.Height(30)))
            {
                listProp.arraySize++;
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void PlayPreview(SerializedProperty elementProp)
    {
        AudioEventType type = (AudioEventType)elementProp.FindPropertyRelative("eventType").enumValueIndex;
        float randomPitchRange = elementProp.FindPropertyRelative("randomPitchRange").floatValue;
        float randomVolumeRange = elementProp.FindPropertyRelative("randomVolumeRange").floatValue;
        float volume = elementProp.FindPropertyRelative("volume").floatValue;
        float startAtTime = elementProp.FindPropertyRelative("startAtTime").floatValue;

        AudioClip clipToPlay = null;

        if (type == AudioEventType.SimpleClip)
        {
            clipToPlay = (AudioClip)elementProp.FindPropertyRelative("clip").objectReferenceValue;
        }
        else
        {
            SerializedProperty listProp = elementProp.FindPropertyRelative("clipList");
            if (listProp.arraySize > 0)
            {
                if (type == AudioEventType.RandomContainer)
                {
                    int randIdx = Random.Range(0, listProp.arraySize);
                    clipToPlay = (AudioClip)listProp.GetArrayElementAtIndex(randIdx).objectReferenceValue;
                }
                else if (type == AudioEventType.SequenceContainer)
                {
                    int targetIdx = Random.Range(0, listProp.arraySize); 
                    clipToPlay = (AudioClip)listProp.GetArrayElementAtIndex(targetIdx).objectReferenceValue;
                }
            }
        }

        if (clipToPlay == null) return;

        float finalPitch = 1f + Random.Range(-randomPitchRange, randomPitchRange);
        float finalVolume = Mathf.Clamp01(volume - Random.Range(0f, randomVolumeRange));

        previewSource.clip = clipToPlay;
        previewSource.volume = finalVolume;
        previewSource.pitch = finalPitch;
        previewSource.time = startAtTime;
        previewSource.Play();
    }
}