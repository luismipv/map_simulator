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

    public void PostEvent(string eventName, GameObject emitter = null)
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

            // Si no permitimos encimarse, matamos cualquier capa de este evento que esté sonando antes de darle Play
            if (!audioEvent.allowOverlap) StopEvent(eventName, targetEmitter);

            if (audioEvent.eventType == AudioEventType.BlendContainer)
            {
                if (audioEvent.clipList == null || audioEvent.clipList.Count == 0) return;
                
                // Reproducimos TODAS las capas a la vez
                foreach (var clip in audioEvent.clipList)
                {
                    if (clip != null) PlayClip(clip, audioEvent, targetEmitter);
                }
            }
            else
            {
                AudioClip clipToPlay = GetClipFromEvent(audioEvent);
                if (clipToPlay != null) PlayClip(clipToPlay, audioEvent, targetEmitter);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Evento no encontrado: {eventName}");
        }
    }

    private void PlayClip(AudioClip clip, UnityAudioEvent audioEvent, GameObject targetEmitter)
    {
        AudioSource[] existingSources = targetEmitter.GetComponents<AudioSource>();
        AudioSource audioSource = null;

        // Buscamos un AudioSource libre
        foreach (var src in existingSources)
        {
            if (!src.isPlaying)
            {
                audioSource = src;
                break;
            }
        }

        // Si no hay libres, instanciamos uno nuevo
        if (audioSource == null) audioSource = targetEmitter.AddComponent<AudioSource>();

        float finalPitch = 1f + Random.Range(-audioEvent.randomPitchRange, audioEvent.randomPitchRange);
        float finalVolume = Mathf.Clamp01(audioEvent.volume - Random.Range(0f, audioEvent.randomVolumeRange));

        audioSource.clip = clip;
        audioSource.volume = finalVolume;
        audioSource.loop = audioEvent.isLooping;
        audioSource.pitch = finalPitch;
        audioSource.time = audioEvent.startAtTime;
        
        audioSource.Play();
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