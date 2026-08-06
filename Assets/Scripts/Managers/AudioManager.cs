using UnityEngine;
using System.Collections.Generic;

public enum AudioEventType { SimpleClip, RandomContainer, SequenceContainer, BlendContainer }

[System.Serializable]
public class UnityAudioEvent
{
    public string eventName;
    
    [Tooltip("Escribe aquí el nombre de la carpeta para agruparlo en el Inspector")]
    public string category = "General";
    public AudioEventType eventType = AudioEventType.SimpleClip;
    
    public AudioClip clip;
    public List<AudioClip> clipList = new List<AudioClip>();
    
    [Range(0f, 1f)] public float volume = 1f;
    public bool isLooping = false;
    
    [Header("Playback Rules")]
    [Tooltip("Si es true, permite que el sonido se encime con otros en el mismo objeto.")]
    public bool allowOverlap = false; 
    
    [Range(0f, 2f)] public float randomPitchRange = 0f;
    [Range(0f, 0.9f)] public float randomVolumeRange = 0f;
    [Range(0f, 2f)] public float startAtTime = 0f;

    [Header("Ajustes Avanzados: Corte y Loop")]
    [Tooltip("Tiempo en segundos donde se detiene la reproducción para oneshot (0 = final del clip)")]
    public float stopAtTime = 0f;

    [Tooltip("Punto de inicio en segundos del loop")]
    public float loopStart = 0f;

    [Tooltip("Punto final en segundos del loop (0 = final del clip)")]
    public float loopEnd = 0f;

    [Tooltip("Duración en segundos del empalme suave (Crossfade) entre el final del loop y el reinicio al loopStart")]
    public float loopCrossfade = 0f;

    [System.NonSerialized] public int sequenceIndex = 0;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Bypass: Eventos Nativos de Unity (Borrar al usar Wwise)")]
    public List<UnityAudioEvent> fallbackAudioEvents = new List<UnityAudioEvent>();
    
    private Dictionary<string, UnityAudioEvent> eventDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        eventDictionary = new Dictionary<string, UnityAudioEvent>();
        foreach (var audioEvent in fallbackAudioEvents)
        {
            if (!eventDictionary.ContainsKey(audioEvent.eventName))
            {
                eventDictionary.Add(audioEvent.eventName, audioEvent);
            }
        }
    }

    public void PostEvent(string eventName, GameObject emitter = null, float pitchMultiplier = 1f)
    {
        /*
          DOCUMENTACIÓN DE PARÁMETROS:
          - eventName: El String exacto con el nombre del sonido (Ej: "UI_Button_Press", "SFX_Footstep").
          - emitter: GameObject emisor (OPCIONAL). 
            - Si pasas 'this.gameObject', el sonido nacerá del objeto que lo pidió.
            - Si lo dejas vacío o en 'null', el sonido nacerá del propio AudioManager.
        */

        if (eventDictionary.TryGetValue(eventName, out UnityAudioEvent audioEvent))
        {
            GameObject targetEmitter = emitter != null ? emitter : this.gameObject;

            // Si no permitimos encimarse, matamos cualquier capa de este evento que esté sonando antes de darle Play
            if (!audioEvent.allowOverlap) StopEvent(eventName, targetEmitter);

            if (audioEvent.eventType == AudioEventType.BlendContainer)
            {
                if (audioEvent.clipList == null || audioEvent.clipList.Count == 0) return;
                
                // Reproducimos TODAS las capas a la vez
                foreach (var clip in audioEvent.clipList)
                {
                    if (clip != null) PlayClip(clip, audioEvent, targetEmitter, pitchMultiplier);
                }
            }
            else
            {
                AudioClip clipToPlay = GetClipFromEvent(audioEvent);
                if (clipToPlay != null) PlayClip(clipToPlay, audioEvent, targetEmitter, pitchMultiplier);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Evento no encontrado: {eventName}");
        }
    }

    private AudioSource GetFreeAudioSource(GameObject targetEmitter)
    {
        AudioSource[] existingSources = targetEmitter.GetComponents<AudioSource>();
        foreach (var src in existingSources)
        {
            if (!src.isPlaying) return src;
        }
        return targetEmitter.AddComponent<AudioSource>();
    }

    private void PlayClip(AudioClip clip, UnityAudioEvent audioEvent, GameObject targetEmitter, float pitchMultiplier = 1f)
    {
        AudioSource audioSource = GetFreeAudioSource(targetEmitter);

        float finalPitch = (1f + Random.Range(-audioEvent.randomPitchRange, audioEvent.randomPitchRange)) * pitchMultiplier;
        float finalVolume = Mathf.Clamp01(audioEvent.volume - Random.Range(0f, audioEvent.randomVolumeRange));

        audioSource.clip = clip;
        audioSource.volume = finalVolume;
        
        bool customLoop = audioEvent.isLooping && (audioEvent.loopStart > 0f || audioEvent.loopEnd > 0f || audioEvent.loopCrossfade > 0f);
        audioSource.loop = audioEvent.isLooping && !customLoop;
        audioSource.pitch = finalPitch;
        audioSource.time = Mathf.Clamp(audioEvent.startAtTime, 0f, clip.length - 0.01f);
        
        audioSource.Play();

        // Control avanzado para cutoffs o loops con Crossfade
        if ((!audioEvent.isLooping && audioEvent.stopAtTime > 0f) || customLoop)
        {
            StartCoroutine(HandleAdvancedPlaybackRoutine(audioSource, clip, audioEvent, targetEmitter, finalVolume, finalPitch));
        }
    }

    private System.Collections.IEnumerator HandleAdvancedPlaybackRoutine(AudioSource initialSource, AudioClip clip, UnityAudioEvent audioEvent, GameObject targetEmitter, float baseVolume, float pitch)
    {
        if (initialSource == null || clip == null) yield break;

        AudioSource currentSource = initialSource;
        float stopTime = (audioEvent.stopAtTime > 0f && audioEvent.stopAtTime <= clip.length) ? audioEvent.stopAtTime : clip.length;
        float lEnd = (audioEvent.loopEnd > 0f && audioEvent.loopEnd <= clip.length) ? audioEvent.loopEnd : clip.length;
        float lStart = Mathf.Clamp(audioEvent.loopStart, 0f, lEnd - 0.01f);
        float crossfadeDur = Mathf.Min(audioEvent.loopCrossfade, lEnd - lStart);

        while (currentSource != null && currentSource.isPlaying && currentSource.clip == clip)
        {
            float currentTime = currentSource.time;

            if (!audioEvent.isLooping)
            {
                // Oneshot Cutoff
                if (currentTime >= stopTime)
                {
                    currentSource.Stop();
                    yield break;
                }
            }
            else
            {
                // Loop con Crossfade
                if (crossfadeDur > 0f && currentTime >= (lEnd - crossfadeDur))
                {
                    AudioSource nextSource = GetFreeAudioSource(targetEmitter);
                    nextSource.clip = clip;
                    nextSource.pitch = pitch;
                    nextSource.time = lStart;
                    nextSource.volume = 0f;
                    nextSource.loop = false;
                    nextSource.Play();

                    float elapsed = 0f;
                    AudioSource fadingOutSource = currentSource;
                    currentSource = nextSource;

                    while (elapsed < crossfadeDur && fadingOutSource != null && nextSource != null)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / crossfadeDur);
                        fadingOutSource.volume = Mathf.Lerp(baseVolume, 0f, t);
                        nextSource.volume = Mathf.Lerp(0f, baseVolume, t);
                        yield return null;
                    }

                    if (fadingOutSource != null)
                    {
                        fadingOutSource.Stop();
                        fadingOutSource.volume = baseVolume;
                    }
                    if (nextSource != null)
                    {
                        nextSource.volume = baseVolume;
                    }
                }
                else if (crossfadeDur <= 0f && currentTime >= lEnd)
                {
                    currentSource.time = lStart;
                }
            }

            yield return null;
        }
    }

    public void StopEvent(string eventName, GameObject emitter = null)
    {
        /*
        // VERSIÓN WWISE
        if (emitter != null) AkSoundEngine.PostEvent(eventName, emitter);
        else AkSoundEngine.PostEvent(eventName, this.gameObject);
        return; 
        */

        if (eventDictionary.TryGetValue(eventName, out UnityAudioEvent audioEvent))
        {
            GameObject targetEmitter = emitter != null ? emitter : this.gameObject;
            AudioSource[] sources = targetEmitter.GetComponents<AudioSource>();
            
            // Recopilamos todos los clips que pertenecen a este evento
            List<AudioClip> eventClips = new List<AudioClip>();
            if (audioEvent.eventType == AudioEventType.SimpleClip) 
                eventClips.Add(audioEvent.clip);
            else 
                eventClips.AddRange(audioEvent.clipList);

            foreach (var src in sources)
            {
                // Si el AudioSource está reproduciendo CUALQUIERA de los clips de este evento, lo apagamos
                if (src.isPlaying && eventClips.Contains(src.clip))
                {
                    src.Stop();
                    // Si no es Blend Container, con encontrar uno es suficiente. Si es Blend, seguimos buscando para apagar todas las voces.
                    if (audioEvent.eventType != AudioEventType.BlendContainer) break; 
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Evento no encontrado: {eventName}");
        }
    }

    public AudioClip GetClipFromEvent(UnityAudioEvent audioEvent)
    {
        if (audioEvent.eventType == AudioEventType.SimpleClip) return audioEvent.clip;
        if (audioEvent.clipList == null || audioEvent.clipList.Count == 0) return null;

        if (audioEvent.eventType == AudioEventType.RandomContainer)
        {
            int randomIndex = Random.Range(0, audioEvent.clipList.Count);
            return audioEvent.clipList[randomIndex];
        }
        else if (audioEvent.eventType == AudioEventType.SequenceContainer)
        {
            AudioClip clip = audioEvent.clipList[audioEvent.sequenceIndex];
            audioEvent.sequenceIndex = (audioEvent.sequenceIndex + 1) % audioEvent.clipList.Count;
            return clip;
        }

        return null;
    }
}