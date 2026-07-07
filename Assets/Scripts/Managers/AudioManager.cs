using UnityEngine;
using System.Collections.Generic;

public enum AudioEventType { SimpleClip, RandomContainer, SequenceContainer }

[System.Serializable]
public class UnityAudioEvent
{
    public string eventName;
    public AudioEventType eventType = AudioEventType.SimpleClip;
    
    // Para un clip simple
    public AudioClip clip;
    
    // Para Random o Sequence Containers
    public List<AudioClip> clipList = new List<AudioClip>();
    
    [Range(0f, 1f)] public float volume = 1f;
    public bool isLooping = false;
    [Range(0f, 2f)] public float randomPitchRange = 0f;
    [Range(0f, 0.9f)] public float randomVolumeRange = 0f;
    [Range(0f, 2f)] public float startAtTime = 0f;

    // Control interno para el Sequence Container en Runtime
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

        // VERSIÓN UNITY AUDIO
        if (eventDictionary.TryGetValue(eventName, out UnityAudioEvent audioEvent))
        {
            AudioClip clipToPlay = GetClipFromEvent(audioEvent);
            if (clipToPlay == null) return;

            GameObject targetEmitter = emitter != null ? emitter : this.gameObject;
            AudioSource audioSource = targetEmitter.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = targetEmitter.AddComponent<AudioSource>();
            }

            float finalPitch = 1f + Random.Range(-audioEvent.randomPitchRange, audioEvent.randomPitchRange);
            float finalVolume = Mathf.Clamp01(audioEvent.volume - Random.Range(0f, audioEvent.randomVolumeRange));

            audioSource.clip = clipToPlay;
            audioSource.volume = finalVolume;
            audioSource.loop = audioEvent.isLooping;
            audioSource.pitch = finalPitch;
            audioSource.time = audioEvent.startAtTime;
            
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Evento no encontrado: {eventName}");
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

        // VERSIÓN UNITY AUDIO
        if (eventDictionary.TryGetValue(eventName, out UnityAudioEvent audioEvent))
        {
            AudioClip clipToPlay = GetClipFromEvent(audioEvent);
            if (clipToPlay == null) return;

            GameObject targetEmitter = emitter != null ? emitter : this.gameObject;
            AudioSource audioSource = targetEmitter.GetComponent<AudioSource>();
            
            // EL FIX: Solo lo detenemos si existe y si REALMENTE está reproduciendo este clip
            if (audioSource != null && audioSource.clip == clipToPlay)
            {
                audioSource.Stop();
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Evento no encontrado: {eventName}");
        }
    }

    // Identifica qué clip reproducir según las reglas del contenedor
    public AudioClip GetClipFromEvent(UnityAudioEvent audioEvent)
    {
        if (audioEvent.eventType == AudioEventType.SimpleClip)
        {
            return audioEvent.clip;
        }
        
        if (audioEvent.clipList == null || audioEvent.clipList.Count == 0) return null;

        if (audioEvent.eventType == AudioEventType.RandomContainer)
        {
            int randomIndex = Random.Range(0, audioEvent.clipList.Count);
            return audioEvent.clipList[randomIndex];
        }
        else if (audioEvent.eventType == AudioEventType.SequenceContainer)
        {
            AudioClip clip = audioEvent.clipList[audioEvent.sequenceIndex];
            // Avanza el índice y lo reinicia si llega al final
            audioEvent.sequenceIndex = (audioEvent.sequenceIndex + 1) % audioEvent.clipList.Count;
            return clip;
        }

        return null;
    }
}