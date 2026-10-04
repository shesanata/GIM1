#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MazeSpell.UI;

namespace MazeSpell.Audio
{
    /// <summary>
    /// Pengelola audio terpusat (Central Audio Manager) untuk game MazeSpell.
    /// Fitur:
    /// 1. Background Music (BGM) modular dengan transisi halus:
    ///    - Main Menu: petualangan tenang (calm adventure).
    ///    - Level 1–2: eksplorasi ringan (light exploration).
    ///    - Level 3–4: menegangkan & misterius (suspense / tense).
    ///    - Level 5: intens & klimaks (intense volcanic climax).
    ///    - Audio Ducking saat Pause (volume otomatis turun ke 35% tanpa terputus).
    /// 2. Sound Effects (SFX) lengkap untuk seluruh interaksi gameplay & UI.
    /// 3. Dual-Mode Audio System: siap menerima asset AudioClip eksternal di Inspector/folder,
    ///    dan memiliki fallback procedural synthesizer berkualitas tinggi jika asset fisik belum ada.
    /// 4. Audio Settings & Persistence (Master, Music, SFX, Mute toggles di PlayerPrefs).
    /// 5. Terintegrasi secara non-invasif dengan sistem yang ada tanpa mengubah script inti terlindungi.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public enum BGMTrack
        {
            None,
            Menu,
            Level1_2,
            Level3_4,
            Level5
        }

        public enum SFXType
        {
            CollectLetter,
            HazardImpact,
            DamageTaken,
            GoalLocked,
            GoalUnlocked,
            WordComplete,
            LevelTransition,
            ButtonHover,
            ButtonClick,
            Pause,
            Resume,
            GameOver,
            GameComplete,
            // Enemy Combat & Movement SFX
            EnemySwordSwing,
            EnemySwordHit,
            EnemyHurt,
            EnemyDefeated,
            EnemyAttackReady,
            EnemyStep,
            EnemyHit,
            EnemySplit,
            MiniEnemySpawn
        }

        // Kunci PlayerPrefs
        private const string PREFS_MASTER_VOL = "MazeSpell_MasterVolume";
        private const string PREFS_MUSIC_VOL = "MazeSpell_MusicVolume";
        private const string PREFS_SFX_VOL = "MazeSpell_SFXVolume";
        private const string PREFS_MUTE_MUSIC = "MazeSpell_MuteMusic";
        private const string PREFS_MUTE_SFX = "MazeSpell_MuteSFX";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Background Music Clips (Opsional - Asset Eksternal)")]
        [Tooltip("Musik Main Menu, Splash Screen, & Level Select")]
        [SerializeField] private AudioClip menuBgmClip;
        [Tooltip("Musik Gameplay Level 1 & 2 (Eksplorasi Ringan)")]
        [SerializeField] private AudioClip level12BgmClip;
        [Tooltip("Musik Gameplay Level 3 & 4 (Menegangkan / Suspense)")]
        [SerializeField] private AudioClip level34BgmClip;
        [Tooltip("Musik Gameplay Level 5 (Intens Climax Lava)")]
        [SerializeField] private AudioClip level5BgmClip;

        [Header("Sound Effect Clips (Opsional - Asset Eksternal)")]
        [SerializeField] private AudioClip collectLetterClip;
        [SerializeField] private AudioClip hazardImpactClip;
        [SerializeField] private AudioClip damageTakenClip;
        [SerializeField] private AudioClip goalLockedClip;
        [SerializeField] private AudioClip goalUnlockedClip;
        [SerializeField] private AudioClip wordCompleteClip;
        [SerializeField] private AudioClip levelTransitionClip;
        [SerializeField] private AudioClip buttonHoverClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip pauseClip;
        [SerializeField] private AudioClip resumeClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private AudioClip gameCompleteClip;
        [Header("Enemy Sound Effect Clips")]
        [SerializeField] private AudioClip enemySwordSwingClip;
        [SerializeField] private AudioClip enemySwordHitClip;
        [SerializeField] private AudioClip enemyHurtClip;
        [SerializeField] private AudioClip enemyDefeatedClip;
        [SerializeField] private AudioClip enemyAttackReadyClip;
        [SerializeField] private AudioClip enemyStepClip;

        // Status Volume & Mute
        private float masterVolume = 1.0f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.8f;
        private bool isMusicMuted = false;
        private bool isSFXMuted = false;

        // Status BGM & Pause
        private BGMTrack currentBgmTrack = BGMTrack.None;
        private bool isPausedState = false;
        private float duckingFactor = 1.0f;
        private Coroutine bgmFadeCoroutine;

        // Procedural Cache
        private readonly Dictionary<BGMTrack, AudioClip> proceduralBgmClips = new Dictionary<BGMTrack, AudioClip>();
        private readonly Dictionary<SFXType, AudioClip> proceduralSfxClips = new Dictionary<SFXType, AudioClip>();

        // Navigation Tracker untuk UI Hover SFX
        private GameObject lastSelectedGameObject;
        private float nextAllowedHoverSoundTime = 0f;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public bool IsMusicMuted => isMusicMuted;
        public bool IsSFXMuted => isSFXMuted;
        public BGMTrack CurrentBgmTrack => currentBgmTrack;

        #region Inisialisasi Otomatis

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeBeforeSceneLoad()
        {
            if (Instance != null) return;

            GameObject audioMgrObj = new GameObject("AudioManager");
            audioMgrObj.AddComponent<AudioManager>();
            DontDestroyOnLoad(audioMgrObj);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
            LoadAudioSettings();
            GenerateProceduralAudioClips();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnhookGameplayEvents();
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f; // 2D Stereo
                bgmSource.ignoreListenerPause = true; // Musik tetap berputar saat pause
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
                uiSource.spatialBlend = 0f;
                uiSource.ignoreListenerPause = true; // Suara UI tetap berfungsi saat pause
            }

            ApplySourceVolumes();
        }

        #endregion

        #region Siklus Update & Monitoring (Pause, UI Hover, BGM Ducking)

        private void Update()
        {
            MonitorPauseState();
            MonitorUISelection();
            UpdateBgmDuckingVolume();
        }

        /// <summary>
        /// Memonitor status Pause dari PauseMenuController atau Time.timeScale untuk audio ducking & SFX.
        /// </summary>
        private void MonitorPauseState()
        {
            bool currentlyPaused = false;

            if (PauseMenuController.Instance != null)
            {
                currentlyPaused = PauseMenuController.Instance.IsPaused;
            }
            else
            {
                // Fallback deteksi jika Time.timeScale di-freeze pada scene gameplay
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName == "SampleScene" && Time.timeScale == 0f)
                {
                    currentlyPaused = true;
                }
            }

            if (currentlyPaused != isPausedState)
            {
                isPausedState = currentlyPaused;
                if (isPausedState)
                {
                    PlayUI(SFXType.Pause, 0.9f);
                }
                else
                {
                    PlayUI(SFXType.Resume, 0.9f);
                }
            }
        }

        /// <summary>
        /// Mengatur volume BGM secara dinamis dengan efek ducking saat game di-pause (turun halus ke 35%).
        /// </summary>
        private void UpdateBgmDuckingVolume()
        {
            float targetDucking = isPausedState ? 0.35f : 1.0f;
            duckingFactor = Mathf.MoveTowards(duckingFactor, targetDucking, Time.unscaledDeltaTime * 2.5f);

            if (bgmFadeCoroutine == null && bgmSource != null)
            {
                bgmSource.volume = GetEffectiveMusicVolume() * duckingFactor;
            }
        }

        /// <summary>
        /// Memonitor navigasi UI Gamepad & Keyboard untuk memicu suara hover/focus saat elemen terpilih berganti.
        /// </summary>
        private void MonitorUISelection()
        {
            if (EventSystem.current == null) return;

            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected != lastSelectedGameObject)
            {
                if (currentSelected != null && lastSelectedGameObject != null && Time.unscaledTime >= nextAllowedHoverSoundTime)
                {
                    Selectable sel = currentSelected.GetComponent<Selectable>();
                    if (sel != null && sel.interactable)
                    {
                        PlayUI(SFXType.ButtonHover, 0.7f);
                        nextAllowedHoverSoundTime = Time.unscaledTime + 0.08f;
                    }
                }
                lastSelectedGameObject = currentSelected;
            }
        }

        #endregion

        #region Pengaturan Volume & Persistensi (PlayerPrefs)

        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER_VOL, 1.0f);
            musicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC_VOL, 0.8f);
            sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOL, 0.8f);
            isMusicMuted = PlayerPrefs.GetInt(PREFS_MUTE_MUSIC, 0) == 1;
            isSFXMuted = PlayerPrefs.GetInt(PREFS_MUTE_SFX, 0) == 1;

            ApplySourceVolumes();
        }

        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(PREFS_MASTER_VOL, masterVolume);
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOL, musicVolume);
            PlayerPrefs.SetFloat(PREFS_SFX_VOL, sfxVolume);
            PlayerPrefs.SetInt(PREFS_MUTE_MUSIC, isMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_MUTE_SFX, isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplySourceVolumes();
            SaveAudioSettings();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            ApplySourceVolumes();
            SaveAudioSettings();
        }

        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplySourceVolumes();
            SaveAudioSettings();
        }

        public void SetMusicMuted(bool muted)
        {
            isMusicMuted = muted;
            ApplySourceVolumes();
            SaveAudioSettings();
        }

        public void SetSFXMuted(bool muted)
        {
            isSFXMuted = muted;
            ApplySourceVolumes();
            SaveAudioSettings();
        }

        public void ToggleMusicMute()
        {
            SetMusicMuted(!isMusicMuted);
        }

        public void ToggleSFXMute()
        {
            SetSFXMuted(!isSFXMuted);
        }

        private float GetEffectiveMusicVolume()
        {
            if (isMusicMuted) return 0f;
            return masterVolume * musicVolume;
        }

        private float GetEffectiveSFXVolume()
        {
            if (isSFXMuted) return 0f;
            return masterVolume * sfxVolume;
        }

        private void ApplySourceVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetEffectiveMusicVolume() * duckingFactor;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = GetEffectiveSFXVolume();
            }
            if (uiSource != null)
            {
                uiSource.volume = GetEffectiveSFXVolume();
            }
        }

        #endregion

        #region Background Music (BGM) Controller & Switching

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UnhookGameplayEvents();

            if (scene.name == "SplashScreen" || scene.name == "MainMenu" || scene.name == "LevelSelect")
            {
                // Putar musik adventure tenang di menu
                PlayBGM(BGMTrack.Menu);
            }
            else if (scene.name == "SampleScene")
            {
                StartCoroutine(SetupGameplayBGMDeferred());
            }
        }

        private IEnumerator SetupGameplayBGMDeferred()
        {
            // Tunggu 1 frame agar LevelProgressionManager terinisialisasi
            yield return null;

            HookGameplayEvents();

            int level = 1;
            if (LevelProgressionManager.Instance != null)
            {
                level = LevelProgressionManager.Instance.CurrentLevel;
            }

            PlayBGMForLevel(level);
        }

        public void PlayBGMForLevel(int level)
        {
            if (level <= 2)
            {
                PlayBGM(BGMTrack.Level1_2);
            }
            else if (level <= 4)
            {
                PlayBGM(BGMTrack.Level3_4);
            }
            else
            {
                PlayBGM(BGMTrack.Level5);
            }
        }

        public void PlayBGM(BGMTrack track, float crossFadeDuration = 0.6f)
        {
            if (track == currentBgmTrack && bgmSource.isPlaying) return;

            AudioClip clipToPlay = GetBgmClip(track);
            if (clipToPlay == null) return;

            currentBgmTrack = track;

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clipToPlay, crossFadeDuration));
        }

        public void StopBGM(float fadeOutDuration = 0.5f)
        {
            currentBgmTrack = BGMTrack.None;
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeOutDuration));
        }

        private IEnumerator CrossFadeBGM(AudioClip newClip, float duration)
        {
            float targetVol = GetEffectiveMusicVolume() * duckingFactor;

            // 1. Fade out track sebelumnya jika sedang memutar
            if (bgmSource.isPlaying)
            {
                float startVol = bgmSource.volume;
                float halfDuration = duration * 0.45f;
                float elapsed = 0f;

                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                    yield return null;
                }
            }

            // 2. Ganti clip dan putar
            bgmSource.clip = newClip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            // 3. Fade in track baru
            float fadeInDuration = duration * 0.55f;
            float inElapsed = 0f;

            while (inElapsed < fadeInDuration)
            {
                inElapsed += Time.unscaledDeltaTime;
                targetVol = GetEffectiveMusicVolume() * duckingFactor;
                bgmSource.volume = Mathf.Lerp(0f, targetVol, inElapsed / fadeInDuration);
                yield return null;
            }

            bgmSource.volume = GetEffectiveMusicVolume() * duckingFactor;
            bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            if (bgmSource.isPlaying)
            {
                float startVol = bgmSource.volume;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                    yield return null;
                }

                bgmSource.Stop();
                bgmSource.clip = null;
            }

            bgmFadeCoroutine = null;
        }

        private AudioClip GetBgmClip(BGMTrack track)
        {
            switch (track)
            {
                case BGMTrack.Menu:
                    if (menuBgmClip != null) return menuBgmClip;
                    if (proceduralBgmClips.ContainsKey(BGMTrack.Menu)) return proceduralBgmClips[BGMTrack.Menu];
                    break;
                case BGMTrack.Level1_2:
                    if (level12BgmClip != null) return level12BgmClip;
                    if (proceduralBgmClips.ContainsKey(BGMTrack.Level1_2)) return proceduralBgmClips[BGMTrack.Level1_2];
                    break;
                case BGMTrack.Level3_4:
                    if (level34BgmClip != null) return level34BgmClip;
                    if (proceduralBgmClips.ContainsKey(BGMTrack.Level3_4)) return proceduralBgmClips[BGMTrack.Level3_4];
                    break;
                case BGMTrack.Level5:
                    if (level5BgmClip != null) return level5BgmClip;
                    if (proceduralBgmClips.ContainsKey(BGMTrack.Level5)) return proceduralBgmClips[BGMTrack.Level5];
                    break;
            }
            return null;
        }

        #endregion

        #region Integrasi Gameplay Non-Invasif (Events Hooking)

        private void HookGameplayEvents()
        {
            if (LevelProgressionManager.Instance != null)
            {
                if (LevelProgressionManager.Instance.onLevelStarted != null)
                    LevelProgressionManager.Instance.onLevelStarted.AddListener(OnGameplayLevelStarted);
                if (LevelProgressionManager.Instance.onAllLevelsCompleted != null)
                    LevelProgressionManager.Instance.onAllLevelsCompleted.AddListener(OnGameplayAllLevelsCompleted);
            }

            if (WordCollectionManager.Instance != null)
            {
                if (WordCollectionManager.Instance.onLetterCollected != null)
                    WordCollectionManager.Instance.onLetterCollected.AddListener(OnGameplayLetterCollected);
                if (WordCollectionManager.Instance.onWordCompleted != null)
                    WordCollectionManager.Instance.onWordCompleted.AddListener(OnGameplayWordCompleted);
            }

            if (PlayerLivesManager.Instance != null)
            {
                if (PlayerLivesManager.Instance.onDamageTaken != null)
                    PlayerLivesManager.Instance.onDamageTaken.AddListener(OnGameplayDamageTaken);
                if (PlayerLivesManager.Instance.onGameOver != null)
                    PlayerLivesManager.Instance.onGameOver.AddListener(OnGameplayGameOver);
            }
        }

        private void UnhookGameplayEvents()
        {
            if (LevelProgressionManager.Instance != null)
            {
                if (LevelProgressionManager.Instance.onLevelStarted != null)
                    LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnGameplayLevelStarted);
                if (LevelProgressionManager.Instance.onAllLevelsCompleted != null)
                    LevelProgressionManager.Instance.onAllLevelsCompleted.RemoveListener(OnGameplayAllLevelsCompleted);
            }

            if (WordCollectionManager.Instance != null)
            {
                if (WordCollectionManager.Instance.onLetterCollected != null)
                    WordCollectionManager.Instance.onLetterCollected.RemoveListener(OnGameplayLetterCollected);
                if (WordCollectionManager.Instance.onWordCompleted != null)
                    WordCollectionManager.Instance.onWordCompleted.RemoveListener(OnGameplayWordCompleted);
            }

            if (PlayerLivesManager.Instance != null)
            {
                if (PlayerLivesManager.Instance.onDamageTaken != null)
                    PlayerLivesManager.Instance.onDamageTaken.RemoveListener(OnGameplayDamageTaken);
                if (PlayerLivesManager.Instance.onGameOver != null)
                    PlayerLivesManager.Instance.onGameOver.RemoveListener(OnGameplayGameOver);
            }
        }

        private void OnGameplayLevelStarted(int level)
        {
            PlaySFX(SFXType.LevelTransition, 0.85f);
            PlayBGMForLevel(level);
        }

        private void OnGameplayAllLevelsCompleted()
        {
            PlaySFX(SFXType.GameComplete, 1.0f);
        }

        private void OnGameplayLetterCollected(char letter, int collected, int total)
        {
            PlaySFX(SFXType.CollectLetter, 0.95f);
        }

        private void OnGameplayWordCompleted(string word)
        {
            PlaySFX(SFXType.WordComplete, 1.0f);
            PlaySFX(SFXType.GoalUnlocked, 0.85f);
        }

        private void OnGameplayDamageTaken()
        {
            PlaySFX(SFXType.HazardImpact, 0.9f);
            PlaySFX(SFXType.DamageTaken, 0.95f);
        }

        private void OnGameplayGameOver()
        {
            PlaySFX(SFXType.GameOver, 1.0f);
        }

        #endregion

        #region Sound Effect Playback

        public void PlaySFX(SFXType type, float volumeMultiplier = 1.0f)
        {
            if (isSFXMuted) return;

            AudioClip clip = GetSFXClip(type);
            if (clip != null && sfxSource != null)
            {
                float finalVol = Mathf.Clamp01(GetEffectiveSFXVolume() * volumeMultiplier);
                sfxSource.PlayOneShot(clip, finalVol);
            }
        }

        public void PlayUI(SFXType type, float volumeMultiplier = 1.0f)
        {
            if (isSFXMuted) return;

            AudioClip clip = GetSFXClip(type);
            if (clip != null && uiSource != null)
            {
                float finalVol = Mathf.Clamp01(GetEffectiveSFXVolume() * volumeMultiplier);
                uiSource.PlayOneShot(clip, finalVol);
            }
        }

        public static void PlaySFXStatic(SFXType type, float volumeMultiplier = 1.0f)
        {
            if (Instance != null)
            {
                Instance.PlaySFX(type, volumeMultiplier);
            }
        }

        public static void PlayUIStatic(SFXType type, float volumeMultiplier = 1.0f)
        {
            if (Instance != null)
            {
                Instance.PlayUI(type, volumeMultiplier);
            }
        }

        private AudioClip GetSFXClip(SFXType type)
        {
            switch (type)
            {
                case SFXType.CollectLetter:
                    if (collectLetterClip != null) return collectLetterClip;
                    break;
                case SFXType.HazardImpact:
                    if (hazardImpactClip != null) return hazardImpactClip;
                    break;
                case SFXType.DamageTaken:
                    if (damageTakenClip != null) return damageTakenClip;
                    break;
                case SFXType.GoalLocked:
                    if (goalLockedClip != null) return goalLockedClip;
                    break;
                case SFXType.GoalUnlocked:
                    if (goalUnlockedClip != null) return goalUnlockedClip;
                    break;
                case SFXType.WordComplete:
                    if (wordCompleteClip != null) return wordCompleteClip;
                    break;
                case SFXType.LevelTransition:
                    if (levelTransitionClip != null) return levelTransitionClip;
                    break;
                case SFXType.ButtonHover:
                    if (buttonHoverClip != null) return buttonHoverClip;
                    break;
                case SFXType.ButtonClick:
                    if (buttonClickClip != null) return buttonClickClip;
                    break;
                case SFXType.Pause:
                    if (pauseClip != null) return pauseClip;
                    break;
                case SFXType.Resume:
                    if (resumeClip != null) return resumeClip;
                    break;
                case SFXType.GameOver:
                    if (gameOverClip != null) return gameOverClip;
                    break;
                case SFXType.GameComplete:
                    if (gameCompleteClip != null) return gameCompleteClip;
                    break;
                case SFXType.EnemySwordSwing:
                    if (enemySwordSwingClip != null) return enemySwordSwingClip;
#if UNITY_EDITOR
                    enemySwordSwingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_sword_swing.wav");
                    if (enemySwordSwingClip != null) return enemySwordSwingClip;
#endif
                    break;
                case SFXType.EnemySwordHit:
                    if (enemySwordHitClip != null) return enemySwordHitClip;
#if UNITY_EDITOR
                    enemySwordHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_sword_hit.wav");
                    if (enemySwordHitClip != null) return enemySwordHitClip;
#endif
                    break;
                case SFXType.EnemyHurt:
                    if (enemyHurtClip != null) return enemyHurtClip;
#if UNITY_EDITOR
                    enemyHurtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_hurt.wav");
                    if (enemyHurtClip != null) return enemyHurtClip;
#endif
                    break;
                case SFXType.EnemyDefeated:
                    if (enemyDefeatedClip != null) return enemyDefeatedClip;
#if UNITY_EDITOR
                    enemyDefeatedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_defeat.wav");
                    if (enemyDefeatedClip != null) return enemyDefeatedClip;
#endif
                    break;
                case SFXType.EnemyAttackReady:
                    if (enemyAttackReadyClip != null) return enemyAttackReadyClip;
#if UNITY_EDITOR
                    enemyAttackReadyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_ready.wav");
                    if (enemyAttackReadyClip != null) return enemyAttackReadyClip;
#endif
                    break;
                case SFXType.EnemyStep:
                    if (enemyStepClip != null) return enemyStepClip;
#if UNITY_EDITOR
                    enemyStepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Enemy/enemy_step.wav");
                    if (enemyStepClip != null) return enemyStepClip;
#endif
                    break;
            }

            if (proceduralSfxClips.ContainsKey(type))
            {
                return proceduralSfxClips[type];
            }

            return null;
        }

        #endregion

        #region Dual-Mode: Procedural Audio Synthesizer Engine

        private void GenerateProceduralAudioClips()
        {
            // 1. Procedural BGMs (Loops berkualitas tinggi tanpa memory spike)
            proceduralBgmClips[BGMTrack.Menu] = SynthesizeBGM_Menu();
            proceduralBgmClips[BGMTrack.Level1_2] = SynthesizeBGM_Level1_2();
            proceduralBgmClips[BGMTrack.Level3_4] = SynthesizeBGM_Level3_4();
            proceduralBgmClips[BGMTrack.Level5] = SynthesizeBGM_Level5();

            // 2. Procedural SFX
            proceduralSfxClips[SFXType.CollectLetter] = SynthesizeSFX_CollectLetter();
            proceduralSfxClips[SFXType.HazardImpact] = SynthesizeSFX_HazardImpact();
            proceduralSfxClips[SFXType.DamageTaken] = SynthesizeSFX_DamageTaken();
            proceduralSfxClips[SFXType.GoalLocked] = SynthesizeSFX_GoalLocked();
            proceduralSfxClips[SFXType.GoalUnlocked] = SynthesizeSFX_GoalUnlocked();
            proceduralSfxClips[SFXType.WordComplete] = SynthesizeSFX_WordComplete();
            proceduralSfxClips[SFXType.LevelTransition] = SynthesizeSFX_LevelTransition();
            proceduralSfxClips[SFXType.ButtonHover] = SynthesizeSFX_ButtonHover();
            proceduralSfxClips[SFXType.ButtonClick] = SynthesizeSFX_ButtonClick();
            proceduralSfxClips[SFXType.Pause] = SynthesizeSFX_Pause();
            proceduralSfxClips[SFXType.Resume] = SynthesizeSFX_Resume();
            proceduralSfxClips[SFXType.GameOver] = SynthesizeSFX_GameOver();
            proceduralSfxClips[SFXType.GameComplete] = SynthesizeSFX_GameComplete();
            proceduralSfxClips[SFXType.EnemySwordSwing] = SynthesizeSFX_EnemySwordSwing();
            proceduralSfxClips[SFXType.EnemySwordHit] = SynthesizeSFX_EnemySwordHit();
            proceduralSfxClips[SFXType.EnemyHurt] = SynthesizeSFX_EnemyHurt();
            proceduralSfxClips[SFXType.EnemyDefeated] = SynthesizeSFX_EnemyDefeated();
            proceduralSfxClips[SFXType.EnemyAttackReady] = SynthesizeSFX_EnemyAttackReady();
            proceduralSfxClips[SFXType.EnemyStep] = SynthesizeSFX_EnemyStep();
            proceduralSfxClips[SFXType.EnemyHit] = SynthesizeSFX_EnemyHit();
            proceduralSfxClips[SFXType.EnemySplit] = SynthesizeSFX_EnemySplit();
            proceduralSfxClips[SFXType.MiniEnemySpawn] = SynthesizeSFX_MiniEnemySpawn();
        }

        private AudioClip SynthesizeBGM_Menu()
        {
            // Musik Petualangan Tenang (C Major / A Minor arpeggio loop, 6 detik)
            int sampleRate = 22050;
            float duration = 6.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Chords: Cmaj7 (0-1.5s), Am7 (1.5-3.0s), Fmaj7 (3.0-4.5s), G7sus (4.5-6.0s)
            float[][] chordFreqs = new float[][]
            {
                new float[] { 261.63f, 329.63f, 392.00f, 493.88f }, // C4, E4, G4, B4
                new float[] { 220.00f, 261.63f, 329.63f, 392.00f }, // A3, C4, E4, G4
                new float[] { 174.61f, 261.63f, 329.63f, 349.23f }, // F3, C4, E4, F4
                new float[] { 196.00f, 261.63f, 293.66f, 392.00f }  // G3, C4, D4, G4
            };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int chordIndex = Mathf.Clamp((int)(t / 1.5f), 0, 3);
                float[] freqs = chordFreqs[chordIndex];

                // Plucked arpeggio note
                float stepT = (t % 0.375f) / 0.375f;
                int noteIndex = ((int)(t / 0.375f)) % freqs.Length;
                float noteFreq = freqs[noteIndex];
                float noteEnv = Mathf.Exp(-stepT * 5.0f);
                float noteWave = Mathf.Sin(2f * Mathf.PI * noteFreq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * noteFreq * t);

                // Ambient soft bass pad
                float bassFreq = freqs[0] * 0.5f;
                float padWave = Mathf.Sin(2f * Mathf.PI * bassFreq * t);

                // Subtle envelope di batas loop agar tidak ada klik
                float loopFade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.05f);

                samples[i] = (noteWave * noteEnv * 0.18f + padWave * 0.10f) * loopFade;
            }

            AudioClip clip = AudioClip.Create("Procedural_BGM_Menu", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeBGM_Level1_2()
        {
            // Eksplorasi Ringan (Pentatonic G Major, Bouncy walking groove, 6 detik)
            int sampleRate = 22050;
            float duration = 6.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] melodyScale = new float[] { 392.00f, 440.00f, 493.88f, 587.33f, 659.25f, 783.99f }; // G, A, B, D, E, G
            float[] bassNotes = new float[] { 98.00f, 123.47f, 146.83f, 130.81f }; // G2, B2, D3, C3

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int bar = Mathf.Clamp((int)(t / 1.5f), 0, 3);

                // Walking bass note (8th notes)
                float bassT = (t % 0.375f) / 0.375f;
                float bassEnv = Mathf.Exp(-bassT * 7.0f);
                float bFreq = bassNotes[bar];
                float bassWave = Mathf.Sin(2f * Mathf.PI * bFreq * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * bFreq * t);

                // Bright cheerful marimba melody (16th notes with syncopation)
                float melT = (t % 0.1875f) / 0.1875f;
                int melIdx = ((int)(t / 0.1875f) * 3) % melodyScale.Length;
                float mFreq = melodyScale[melIdx];
                float melEnv = Mathf.Exp(-melT * 10.0f);
                float melWave = Mathf.Sin(2f * Mathf.PI * mFreq * t) + 0.25f * Mathf.Sin(6f * Mathf.PI * mFreq * t);

                float loopFade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.05f);
                samples[i] = (bassWave * bassEnv * 0.22f + melWave * melEnv * 0.14f) * loopFade;
            }

            AudioClip clip = AudioClip.Create("Procedural_BGM_Level1_2", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeBGM_Level3_4()
        {
            // Menegangkan / Suspense (D Minor, rhythmic bass pulse, mysterious pad, 6 detik)
            int sampleRate = 22050;
            float duration = 6.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] rootNotes = new float[] { 146.83f, 116.54f, 98.00f, 110.00f }; // D3, Bb2, G2, A2

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int chordIdx = Mathf.Clamp((int)(t / 1.5f), 0, 3);
                float root = rootNotes[chordIdx];

                // Pulsing 16th note bassline (suspense rhythm)
                float pulseT = (t % 0.125f) / 0.125f;
                float pulseEnv = Mathf.Exp(-pulseT * 12.0f);
                float pulseWave = Mathf.Sin(2f * Mathf.PI * root * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * (root * 0.5f) * t);

                // Mysterious minor harmonic drone
                float droneFreq = root * 1.5f; // Fifth
                float mod = 1f + 0.005f * Mathf.Sin(2f * Mathf.PI * 1.5f * t);
                float droneWave = Mathf.Sin(2f * Mathf.PI * droneFreq * mod * t);

                float loopFade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.05f);
                samples[i] = (pulseWave * pulseEnv * 0.20f + droneWave * 0.08f) * loopFade;
            }

            AudioClip clip = AudioClip.Create("Procedural_BGM_Level3_4", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeBGM_Level5()
        {
            // Intens Climax Lava / Volcano (Fast tempo, driving pulse, dramatic accents, 6 detik)
            int sampleRate = 22050;
            float duration = 6.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] intenseRoots = new float[] { 82.41f, 87.31f, 98.00f, 123.47f }; // E2, F2, G2, B2

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int barIdx = Mathf.Clamp((int)(t / 1.5f), 0, 3);
                float root = intenseRoots[barIdx];

                // Fast driving 16th rhythm
                float driveT = (t % 0.09375f) / 0.09375f;
                float driveEnv = Mathf.Exp(-driveT * 9.0f);
                float driveWave = Mathf.Sin(2f * Mathf.PI * root * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * root * t);

                // High dramatic stabs (syncopated)
                float stabT = (t % 0.375f) / 0.375f;
                float stabEnv = Mathf.Exp(-stabT * 14.0f);
                float stabFreq = root * 4.0f;
                float stabWave = Mathf.Sin(2f * Mathf.PI * stabFreq * t);

                float loopFade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.05f);
                samples[i] = (driveWave * driveEnv * 0.24f + stabWave * stabEnv * 0.12f) * loopFade;
            }

            AudioClip clip = AudioClip.Create("Procedural_BGM_Level5", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_CollectLetter()
        {
            // Bright chime ascending triad (E5 -> G#5 -> B5 -> E6)
            int sampleRate = 44100;
            float duration = 0.28f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noteT = t / duration;
                float freq = noteT < 0.25f ? 659.25f : (noteT < 0.5f ? 830.61f : (noteT < 0.75f ? 987.77f : 1318.51f));
                float env = Mathf.Exp(-t * 9.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_CollectLetter", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_HazardImpact()
        {
            // Low punchy impact thud (160Hz -> 45Hz)
            int sampleRate = 44100;
            float duration = 0.22f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(160f, 45f, t / duration);
                float env = Mathf.Exp(-t * 18.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 35.0f) * 0.2f;
                samples[i] = (wave * 0.5f + noise) * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_HazardImpact", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_DamageTaken()
        {
            // Heavy damage / hurt impact
            int sampleRate = 44100;
            float duration = 0.28f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(220f, 55f, t / duration);
                float env = Mathf.Exp(-t * 14.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_DamageTaken", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_GoalLocked()
        {
            // Warning buzz-blip (Double pulse 200Hz)
            int sampleRate = 44100;
            float duration = 0.20f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = t < 0.09f ? 240f : 190f;
                float pulseT = (t % 0.09f) / 0.09f;
                float env = Mathf.Exp(-pulseT * 15.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(6f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_GoalLocked", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_GoalUnlocked()
        {
            // Crystalline rising unlock arpeggio (C5 -> E5 -> G5 -> C6)
            int sampleRate = 44100;
            float duration = 0.45f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int idx = Mathf.Clamp((int)(t / (duration / notes.Length)), 0, notes.Length - 1);
                float freq = notes[idx];
                float stepT = (t % (duration / notes.Length)) / (duration / notes.Length);
                float env = Mathf.Exp(-stepT * 6.0f) * Mathf.Clamp01((duration - t) / 0.1f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(3f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_GoalUnlocked", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_WordComplete()
        {
            // Triumphant victory fanfare (Chord progression to grand resolution)
            int sampleRate = 44100;
            float duration = 0.90f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq1, freq2;

                if (t < 0.20f)
                {
                    freq1 = 523.25f; // C5
                    freq2 = 659.25f; // E5
                }
                else if (t < 0.40f)
                {
                    freq1 = 587.33f; // D5
                    freq2 = 783.99f; // G5
                }
                else
                {
                    freq1 = 659.25f; // E5
                    freq2 = 1046.50f; // C6
                }

                float decay = t >= 0.40f ? Mathf.Exp(-(t - 0.40f) * 3.5f) : 1.0f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq1 * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * freq2 * t);
                samples[i] = wave * decay * 0.30f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_WordComplete", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_LevelTransition()
        {
            // Airy whoosh / portal transition
            int sampleRate = 44100;
            float duration = 0.50f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float sweepFreq = Mathf.Lerp(250f, 900f, Mathf.Sin(t / duration * Mathf.PI));
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float wave = Mathf.Sin(2f * Mathf.PI * sweepFreq * t);
                samples[i] = wave * env * 0.30f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_LevelTransition", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_ButtonHover()
        {
            // Crisp, gentle UI tick (30ms)
            int sampleRate = 44100;
            float duration = 0.035f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 90.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1150f * t);
                samples[i] = wave * env * 0.25f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_ButtonHover", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_ButtonClick()
        {
            // Snappy confirmation pop (55ms)
            int sampleRate = 44100;
            float duration = 0.065f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(600f, 1200f, t / duration);
                float env = Mathf.Exp(-t * 45.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_ButtonClick", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_Pause()
        {
            // Descending blip (700Hz -> 380Hz)
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(720f, 380f, t / duration);
                float env = Mathf.Exp(-t * 22.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_Pause", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_Resume()
        {
            // Ascending blip (380Hz -> 720Hz)
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(380f, 720f, t / duration);
                float env = Mathf.Exp(-t * 22.0f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_Resume", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_GameOver()
        {
            // Somber descending minor phrase (1.2s)
            int sampleRate = 44100;
            float duration = 1.20f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = new float[] { 311.13f, 293.66f, 261.63f, 196.00f }; // Eb4, D4, C4, G3

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int idx = Mathf.Clamp((int)(t / (duration / notes.Length)), 0, notes.Length - 1);
                float freq = notes[idx];
                float noteT = (t % (duration / notes.Length)) / (duration / notes.Length);
                float env = Mathf.Exp(-noteT * 5.0f) * Mathf.Clamp01((duration - t) / 0.15f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);
                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_GameOver", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_GameComplete()
        {
            // Grand coronation fanfare (1.8s)
            int sampleRate = 44100;
            float duration = 1.80f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1, f2, f3;

                if (t < 0.35f)
                {
                    f1 = 523.25f; // C5
                    f2 = 659.25f; // E5
                    f3 = 783.99f; // G5
                }
                else if (t < 0.70f)
                {
                    f1 = 587.33f; // D5
                    f2 = 783.99f; // G5
                    f3 = 880.00f; // A5
                }
                else
                {
                    f1 = 659.25f; // E5
                    f2 = 783.99f; // G5
                    f3 = 1046.50f; // C6
                }

                float decay = t >= 0.70f ? Mathf.Exp(-(t - 0.70f) * 2.2f) : 1.0f;
                float wave = Mathf.Sin(2f * Mathf.PI * f1 * t) +
                             0.7f * Mathf.Sin(2f * Mathf.PI * f2 * t) +
                             0.5f * Mathf.Sin(2f * Mathf.PI * f3 * t);

                samples[i] = wave * decay * 0.28f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_GameComplete", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemySwordSwing()
        {
            // Whoosh sweeping pitch (680Hz -> 180Hz) + noise rush
            int sampleRate = 44100;
            float duration = 0.20f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 680f * Mathf.Exp(-t * 6.0f) + 180f;
                float phase = 2f * Mathf.PI * freq * t;
                float sine = Mathf.Sin(phase);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.45f;
                float env = t < 0.03f ? (t / 0.03f) : Mathf.Exp(-(t - 0.03f) * 16.0f);
                samples[i] = (sine * 0.55f + noise) * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemySwordSwing", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemySwordHit()
        {
            // Sharp impact transient (1800Hz) + punchy body (130Hz)
            int sampleRate = 44100;
            float duration = 0.16f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float transient = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 60f);
                float bodyFreq = Mathf.Max(50f, 130f - t * 400f);
                float body = Mathf.Sin(2f * Mathf.PI * bodyFreq * t) * Mathf.Exp(-t * 22f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.6f;
                float env = Mathf.Exp(-t * 25f);
                samples[i] = (transient * 0.5f + body * 0.7f + noise * 0.4f) * env * 0.40f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemySwordHit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemyHurt()
        {
            // Vocal grunt / impact (320Hz -> 140Hz)
            int sampleRate = 44100;
            float duration = 0.22f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Max(100f, 320f * (1f - t * 3.5f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t);
                float distorted = (float)System.Math.Tanh(tone * 1.8f);
                float env = t < 0.015f ? (t / 0.015f) : Mathf.Exp(-(t - 0.015f) * 14f);
                samples[i] = distorted * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemyHurt", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemyDefeated()
        {
            // Dramatic descending defeat chords (440/330/220Hz)
            int sampleRate = 44100;
            float duration = 0.48f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1 = Mathf.Max(180f, 440f * (1f - t * 1.5f));
                float f2 = Mathf.Max(135f, 330f * (1f - t * 1.5f));
                float f3 = Mathf.Max(90f, 220f * (1f - t * 1.5f));
                float tone = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.4f +
                             Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.45f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.15f;
                float env = t < 0.02f ? (t / 0.02f) : Mathf.Exp(-(t - 0.02f) * 5.5f);
                samples[i] = (tone + noise) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemyDefeated", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemyAttackReady()
        {
            // Blade draw / ready glint (1450Hz & 2100Hz)
            int sampleRate = 44100;
            float duration = 0.14f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float ring = Mathf.Sin(2f * Mathf.PI * 1450f * t) * 0.6f +
                             Mathf.Sin(2f * Mathf.PI * 2100f * t) * 0.4f;
                float env = Mathf.Exp(-t * 26f);
                samples[i] = ring * env * 0.30f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemyAttackReady", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemyStep()
        {
            // Soft stone tap (170Hz -> 80Hz)
            int sampleRate = 44100;
            float duration = 0.08f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Max(70f, 170f - t * 900f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                float env = Mathf.Exp(-t * 40f);
                samples[i] = (tone * 0.75f + noise * 0.25f) * env * 0.15f; // very soft footstep
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemyStep", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemyHit()
        {
            // Crunchy arcade impact with rapid pitch sweep (480Hz -> 110Hz) & noise transient
            int sampleRate = 44100;
            float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Max(60f, 480f * (1f - t * 4.5f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.45f;
                float env = Mathf.Exp(-t * 20f);
                samples[i] = (tone * 0.65f + noise) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemyHit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_EnemySplit()
        {
            // Magical splitting shatter & division pop (Dual octave burst 450Hz & 900Hz)
            int sampleRate = 44100;
            float duration = 0.32f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1 = Mathf.Lerp(450f, 850f, t / duration);
                float f2 = Mathf.Lerp(900f, 1350f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.3f;
                float sparkle = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 12f) * 0.25f;
                float env = t < 0.03f ? (t / 0.03f) : Mathf.Exp(-(t - 0.03f) * 8f);
                samples[i] = (tone + sparkle) * env * 0.40f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_EnemySplit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeSFX_MiniEnemySpawn()
        {
            // Bright cheerful cartoon sprout / chirp (600Hz -> 1200Hz)
            int sampleRate = 44100;
            float duration = 0.14f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(600f, 1200f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                float env = Mathf.Exp(-t * 22f);
                samples[i] = tone * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_SFX_MiniEnemySpawn", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
