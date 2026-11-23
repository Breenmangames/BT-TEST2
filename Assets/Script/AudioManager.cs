using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 10;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Hit Sounds")]
        [SerializeField] private AudioClip[] hitSounds;
        [SerializeField, Range(0f, 1f)] private float hitVolume = 0.7f;
        [SerializeField, Range(0.8f, 1.2f)] private float hitPitchMin = 0.9f;
        [SerializeField, Range(0.8f, 1.2f)] private float hitPitchMax = 1.1f;

        [Header("Impact Sounds (Heavy Hits)")]
        [SerializeField] private AudioClip[] heavyHitSounds;
        [SerializeField, Range(0f, 1f)] private float heavyHitVolume = 0.8f;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip missSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.6f;

        [Header("Player Sounds")]
        [SerializeField] private AudioClip playerHitSound;
        [SerializeField] private AudioClip playerDeathSound;
        [SerializeField, Range(0f, 1f)] private float playerSoundVolume = 0.7f;

        
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private int currentPoolIndex = 0;

        private int lastHitSoundIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void Start()
        {
            PlayMusic();
        }

        private void InitializeAudioSources()
        {
            
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

           
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                sfxPool.Add(source);
            }

            
        }

        #region Music Control

        public void PlayMusic()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.Play();
                
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        #endregion

        #region Hit Sounds

        public void PlayHitSound()
        {
            if (hitSounds == null || hitSounds.Length == 0)
            {
                Debug.LogWarning("No hit sounds assigned to AudioManager!");
                return;
            }

            
            AudioClip clip = GetRandomClip(hitSounds, ref lastHitSoundIndex);

            if (clip != null)
            {
                float pitch = Random.Range(hitPitchMin, hitPitchMax);
                PlaySFX(clip, hitVolume, pitch);
            }
        }

        public void PlayHeavyHitSound()
        {
            if (heavyHitSounds != null && heavyHitSounds.Length > 0)
            {
                AudioClip clip = heavyHitSounds[Random.Range(0, heavyHitSounds.Length)];
                PlaySFX(clip, heavyHitVolume, 1f);
            }
            else
            {
                
                PlayHitSound();
            }
        }

        #endregion

        #region UI Sounds

        public void PlayComboSound()
        {
            if (comboSound != null)
            {
                PlaySFX(comboSound, uiVolume);
            }
        }

        public void PlayMissSound()
        {
            if (missSound != null)
            {
                PlaySFX(missSound, uiVolume, 0.9f);
            }
        }

        public void PlayGameOverSound()
        {
            if (gameOverSound != null)
            {
                PlaySFX(gameOverSound, uiVolume);
            }
        }

        #endregion

        #region Player Sounds

        public void PlayPlayerHitSound()
        {
            if (playerHitSound != null)
            {
                PlaySFX(playerHitSound, playerSoundVolume);
            }
        }

        public void PlayPlayerDeathSound()
        {
            if (playerDeathSound != null)
            {
                PlaySFX(playerDeathSound, playerSoundVolume);
            }
        }

        #endregion

        #region Core Audio Methods

        
        private void PlaySFX(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;

            
            AudioSource source = sfxPool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % sfxPoolSize;

            
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
        {
            if (clips.Length == 1)
            {
                return clips[0];
            }

            int newIndex;
            do
            {
                newIndex = Random.Range(0, clips.Length);
            }
            while (newIndex == lastIndex && clips.Length > 1);

            lastIndex = newIndex;
            return clips[newIndex];
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            
            hitVolume = Mathf.Clamp01(volume);
            heavyHitVolume = Mathf.Clamp01(volume);
            uiVolume = Mathf.Clamp01(volume);
            playerSoundVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Hit Sound")]
        public void TestHitSound()
        {
            PlayHitSound();
        }

        [ContextMenu("Test Heavy Hit Sound")]
        public void TestHeavyHitSound()
        {
            PlayHeavyHitSound();
        }

        [ContextMenu("Test Combo Sound")]
        public void TestComboSound()
        {
            PlayComboSound();
        }

        #endregion
 }


