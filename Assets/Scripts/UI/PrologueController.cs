using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using MazeSpell.Audio;

namespace MazeSpell.UI
{
    /// <summary>
    /// Controller utama untuk Prologue / Cinematic Intro game MazeSpell:
    /// Menjalankan 6 adegan cerita (~20-25 detik) sebelum masuk ke Main Menu:
    /// SCENE 1 — BLACK SCREEN: Kekuatan huruf di dunia kata-kata.
    /// SCENE 2 — HURUF-HURUF HILANG: Collectible Bubble Letters melayang lalu menghilang misterius.
    /// SCENE 3 — LABIRIN MISTERIUS: Reveal maze dengan camera zoom & ambience.
    /// SCENE 4 — PENJAGA LABIRIN: Enemy muncul dengan ayunan pedang & flash.
    /// SCENE 5 — PLAYER: Player menghadap maze, huruf mengelilingi, tekad menaklukkan labirin.
    /// SCENE 6 — TITLE: Logo MAZESPELL & THE WORD LABYRINTH ADVENTURE dengan subtle glow.
    /// Mendukung SKIP kapan saja dengan tombol SPACE atau ESC (fade out ~0.35s ke MainMenu).
    /// </summary>
    public class PrologueController : MonoBehaviour
    {
        [Header("Konfigurasi Scene")]
        [Tooltip("Nama scene Main Menu tujuan setelah prologue selesai")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("UI Canvas & Overlays")]
        [SerializeField] private Canvas prologueCanvas;
        [SerializeField] private CanvasGroup blackOverlay;
        [SerializeField] private CanvasGroup storyTextGroup;
        [SerializeField] private TextMeshProUGUI storyText;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private RectTransform titleRect;
        [SerializeField] private TextMeshProUGUI skipHintText;
        [SerializeField] private CanvasGroup flashOverlay;

        [Header("World Elements")]
        [SerializeField] private Transform letterGroup;
        [SerializeField] private Transform mazeReveal;
        [SerializeField] private Transform enemyReveal;
        [SerializeField] private Transform playerReveal;
        [SerializeField] private Transform playerLettersGroup;
        [SerializeField] private EnemyVisualAnimator enemyVisualAnimator;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ParticleSystem magicParticles;

        [Header("Audio SFX Assets")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip enemyReadyClip;
        [SerializeField] private AudioClip enemySwordSwingClip;
        [SerializeField] private AudioClip enemySwordHitClip;

        // Status Internal
        private bool isSkipping = false;
        private bool isFinished = false;
        private Coroutine prologueRoutine;
        private Coroutine cameraShakeRoutine;
        private Vector3 initialCamPos = new Vector3(0f, 0f, -10f);
        private float initialCamSize = 5f;

        // Procedural Audio Clips
        private AudioClip softAmbientClip;
        private AudioClip magicChimeClip;
        private AudioClip whooshClip;
        private AudioClip mysteriousDroneClip;
        private AudioClip titleImpactClip;

        private void Awake()
        {
            Debug.Log("[Prologue] Awake");

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Self-healing: jika kamera tergeser ke (0,0) atau (0,-0.5), kembalikan ke titik fokus maze (8.5, 5.5, -10)
                if (Mathf.Abs(mainCamera.transform.position.x) < 0.1f && Mathf.Abs(mainCamera.transform.position.y) < 1.0f)
                {
                    mainCamera.transform.position = new Vector3(8.5f, 5.5f, -10f);
                    mainCamera.orthographicSize = 6.2f;
                }
                initialCamPos = mainCamera.transform.position;
                initialCamSize = mainCamera.orthographicSize;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }

            // Hentikan BGM menu/gameplay sementara agar audio atmosfer prologue jernih
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(0.1f);
            }

            // Buat procedural sound effects untuk melengkapi asset fisik
            GenerateProceduralAudio();

            // Set state awal visual
            if (blackOverlay != null)
            {
                blackOverlay.alpha = 1f;
                blackOverlay.blocksRaycasts = false;
            }

            if (storyTextGroup != null) storyTextGroup.alpha = 0f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (flashOverlay != null) flashOverlay.alpha = 0f;

            if (letterGroup != null) letterGroup.gameObject.SetActive(false);
            if (mazeReveal != null) mazeReveal.gameObject.SetActive(false);
            if (enemyReveal != null) enemyReveal.gameObject.SetActive(false);
            if (playerReveal != null) playerReveal.gameObject.SetActive(false);
            if (playerLettersGroup != null) playerLettersGroup.gameObject.SetActive(false);
        }

        private void Start()
        {
            Debug.Log("[Prologue] Start");
            Debug.Log("[Prologue] Cinematic started");
            prologueRoutine = StartCoroutine(PlayPrologueSequence());
        }

        private void Update()
        {
            if (isSkipping || isFinished) return;

            // Periksa input SKIP (SPACE atau ESC)
            if (CheckSkipInput())
            {
                SkipPrologue();
            }

            // Animasi halus berkedip pada teks skip hint
            if (skipHintText != null && skipHintText.gameObject.activeInHierarchy)
            {
                float alpha = 0.45f + 0.35f * Mathf.Sin(Time.time * 3.5f);
                Color c = skipHintText.color;
                c.a = alpha;
                skipHintText.color = c;
            }
        }

        /// <summary>
        /// Memeriksa input SPACE atau ESC dari Legacy maupun New Input System.
        /// </summary>
        private bool CheckSkipInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    return true;
                }
            }
            catch
            {
                // Fallback aman jika New Input System aktif tanpa Event Provider Legacy
            }

            return false;
        }

        /// <summary>
        /// Melompati seluruh intro secara instan dan memuat Main Menu.
        /// </summary>
        public void SkipPrologue()
        {
            if (isSkipping || isFinished) return;
            isSkipping = true;

            Debug.Log("[Prologue] Skip diminta oleh pemain (SPACE / ESC). Menghentikan cinematic...");

            if (prologueRoutine != null)
            {
                StopCoroutine(prologueRoutine);
                prologueRoutine = null;
            }

            if (cameraShakeRoutine != null)
            {
                StopCoroutine(cameraShakeRoutine);
                cameraShakeRoutine = null;
                if (mainCamera != null) mainCamera.transform.position = initialCamPos;
            }

            StartCoroutine(FastFadeOutAndLoadMenu());
        }

        private IEnumerator FastFadeOutAndLoadMenu()
        {
            // Mainkan suara transisi singkat jika ada
            PlaySFX(whooshClip, 0.4f);

            float elapsed = 0f;
            float duration = 0.35f;
            float startAlpha = blackOverlay != null ? blackOverlay.alpha : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (blackOverlay != null)
                {
                    blackOverlay.alpha = Mathf.Lerp(startAlpha, 1f, t);
                }
                yield return null;
            }

            if (blackOverlay != null) blackOverlay.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.08f);

            LoadMainMenu();
        }

        private void LoadMainMenu()
        {
            isFinished = true;
            Debug.Log($"[Prologue] Memuat scene Main Menu: {mainMenuSceneName}");

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                // Fallback berdasarkan build index 1
                SceneManager.LoadScene(1);
            }
        }

        #region Alur Cinematic (6 Scenes)

        private IEnumerator PlayPrologueSequence()
        {
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(true);
            }

            // Pastikan background murni hitam
            if (blackOverlay != null) blackOverlay.alpha = 1f;

            PlaySFX(softAmbientClip, 0.6f);
            yield return new WaitForSeconds(0.5f);

            // 1. TITLE: LABIRIN KATA
            Debug.Log("[Prologue] Title Screen");
            if (titleGroup != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(titleGroup, 0f, 1f, 0.8f));
                PlaySFX(magicChimeClip, 0.8f);
                yield return new WaitForSeconds(1.8f);
                yield return StartCoroutine(FadeCanvasGroup(titleGroup, 1f, 0f, 0.6f));
            }
            yield return new WaitForSeconds(0.3f);

            // 2. Story Line 1: "Dahulu, kata-kata memberi makna pada dunia..."
            Debug.Log("[Prologue] Story 1");
            yield return StartCoroutine(DisplayStoryText(
                "Dahulu, kata-kata memberi makna pada dunia...",
                0.7f, 2.2f, 0.6f));
            yield return new WaitForSeconds(0.3f);

            // 3. Story Line 2: "Hingga huruf-huruf mulai menghilang satu per satu."
            Debug.Log("[Prologue] Story 2");
            PlaySFX(whooshClip, 0.7f);
            yield return StartCoroutine(DisplayStoryText(
                "Hingga huruf-huruf mulai menghilang satu per satu.",
                0.7f, 2.2f, 0.6f));
            yield return new WaitForSeconds(0.3f);

            // 4. Story Line 3: "Kini, kata-kata telah hancur dan terpecah..."
            Debug.Log("[Prologue] Story 3");
            PlaySFX(mysteriousDroneClip, 0.7f);
            yield return StartCoroutine(DisplayStoryText(
                "Kini, kata-kata telah hancur dan terpecah...",
                0.7f, 2.0f, 0.6f));
            yield return new WaitForSeconds(0.3f);

            // 5. Story Line 4: "Hanya penjelajah pemberani yang dapat menyatukannya kembali."
            Debug.Log("[Prologue] Story 4");
            PlaySFX(magicChimeClip, 0.85f);
            yield return StartCoroutine(DisplayStoryText(
                "Hanya penjelajah pemberani yang dapat menyatukannya kembali.",
                0.7f, 2.2f, 0.6f));
            yield return new WaitForSeconds(0.4f);

            // 6. Objective: "Temukan hurufnya.\nSusun katanya.\nKeluar dari bangunan ini."
            Debug.Log("[Prologue] Objective");
            PlaySFX(titleImpactClip, 0.9f);
            yield return StartCoroutine(DisplayStoryText(
                "<size=120%><color=#FCD34D>Temukan hurufnya.\nSusun katanya.\nKeluar dari bangunan ini.</color></size>",
                0.8f, 2.8f, 0.8f));
            yield return new WaitForSeconds(0.5f);

            // 7. Transisi Fade Out ke Main Menu
            Debug.Log("[Prologue] Transitioning to Main Menu...");
            isFinished = true;
            yield return StartCoroutine(TransitionToMainMenu());
        }

        private IEnumerator TransitionToMainMenu()
        {
            if (blackOverlay != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(blackOverlay, blackOverlay.alpha, 1f, 0.6f));
            }
            yield return new WaitForSeconds(0.2f);
            LoadMainMenu();
        }

        #endregion

        #region Helper Animasi Teks & Visual

        private IEnumerator DisplayStoryText(string content, float fadeInTime, float stayTime, float fadeOutTime)
        {
            if (storyText == null || storyTextGroup == null) yield break;

            storyText.characterSpacing = 0f;
            storyText.wordSpacing = 0f;
            storyText.text = content;
            yield return StartCoroutine(FadeCanvasGroup(storyTextGroup, 0f, 1f, fadeInTime));

            float elapsed = 0f;
            while (elapsed < stayTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return StartCoroutine(FadeCanvasGroup(storyTextGroup, 1f, 0f, fadeOutTime));
        }

        private IEnumerator DisplaySequentialStoryLines(string[] lines, float intervalPerLine)
        {
            if (storyText == null || storyTextGroup == null) yield break;

            storyTextGroup.alpha = 1f;

            for (int i = 0; i < lines.Length; i++)
            {
                string combined = "";
                for (int j = 0; j <= i; j++)
                {
                    if (j > 0) combined += "\n";
                    if (j == i) combined += $"<b><color=#FFE359>{lines[j]}</color></b>";
                    else combined += $"<color=#CBD5E1>{lines[j]}</color>";
                }

                storyText.text = combined;
                PlaySFX(softAmbientClip, 0.4f);

                yield return new WaitForSeconds(intervalPerLine);
            }

            yield return new WaitForSeconds(1.0f);
            yield return StartCoroutine(FadeCanvasGroup(storyTextGroup, 1f, 0f, 0.5f));
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null) yield break;

            float elapsed = 0f;
            cg.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            cg.alpha = to;
        }

        private IEnumerator PopInTransform(Transform tr, float duration, float delay)
        {
            if (tr == null) yield break;
            tr.localScale = Vector3.zero;

            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Overshoot bounce ringan
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) + 0.1f * Mathf.Sin(t * Mathf.PI);
                tr.localScale = Vector3.one * Mathf.Clamp01(scale);
                yield return null;
            }
            tr.localScale = Vector3.one;
        }

        private IEnumerator DisperseAndVanishTransform(Transform tr, float duration, float xDrift)
        {
            if (tr == null) yield break;

            Vector3 startPos = tr.localPosition;
            Vector3 targetPos = startPos + new Vector3(xDrift, UnityEngine.Random.Range(1.2f, 2.0f), 0f);
            Vector3 startScale = tr.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tr.localPosition = Vector3.Lerp(startPos, targetPos, t);
                tr.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 15f);
                yield return null;
            }

            tr.gameObject.SetActive(false);
        }

        private void AnimateFloatingLetters(Transform group, float elapsed)
        {
            if (group == null) return;
            for (int i = 0; i < group.childCount; i++)
            {
                Transform ch = group.GetChild(i);
                float yOffset = Mathf.Sin(elapsed * 3f + i * 0.8f) * 0.08f;
                Vector3 p = ch.localPosition;
                p.y += yOffset * Time.deltaTime * 6f;
                ch.localPosition = p;
            }
        }

        private IEnumerator AnimateTitleScale(RectTransform tr, float fromScale, float toScale, float duration)
        {
            if (tr == null) yield break;

            float elapsed = 0f;
            tr.localScale = Vector3.one * fromScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = Mathf.SmoothStep(fromScale, toScale, t);
                tr.localScale = Vector3.one * s;
                yield return null;
            }

            tr.localScale = Vector3.one * toScale;
        }

        private IEnumerator CameraZoomPan(float fromSize, float toSize, Vector3 fromPos, Vector3 toPos, float duration)
        {
            if (mainCamera == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, t);
                mainCamera.transform.position = Vector3.Lerp(fromPos, toPos, t);
                yield return null;
            }

            mainCamera.orthographicSize = toSize;
            mainCamera.transform.position = toPos;
        }

        private void TriggerCameraShake(float intensity, float duration)
        {
            if (cameraShakeRoutine != null) StopCoroutine(cameraShakeRoutine);
            cameraShakeRoutine = StartCoroutine(CameraShakeRoutine(intensity, duration));
        }

        private IEnumerator CameraShakeRoutine(float intensity, float duration)
        {
            if (mainCamera == null) yield break;

            Vector3 origin = mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - (elapsed / duration);
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity * damper;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity * damper;
                mainCamera.transform.position = new Vector3(origin.x + x, origin.y + y, origin.z);
                yield return null;
            }

            mainCamera.transform.position = origin;
            cameraShakeRoutine = null;
        }

        private void TriggerFlash()
        {
            if (flashOverlay != null)
            {
                StartCoroutine(FlashRoutine(0.45f));
            }
        }

        private IEnumerator FlashRoutine(float duration)
        {
            if (flashOverlay == null) yield break;

            flashOverlay.alpha = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashOverlay.alpha = Mathf.Lerp(0.55f, 0f, elapsed / duration);
                yield return null;
            }

            flashOverlay.alpha = 0f;
        }

        #endregion

        #region Audio & SFX Helpers

        private void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, volume);
        }

        private void PlayEnemyReadySFX()
        {
            if (enemyReadyClip != null)
            {
                PlaySFX(enemyReadyClip, 0.75f);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyAttackReady, 0.75f);
            }
        }

        private void PlayEnemySwordSwingSFX()
        {
            if (enemySwordSwingClip != null)
            {
                PlaySFX(enemySwordSwingClip, 0.85f);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemySwordSwing, 0.85f);
            }
        }

        /// <summary>
        /// Menghasilkan procedural ambient & SFX halus tanpa file fisik tambahan.
        /// </summary>
        private void GenerateProceduralAudio()
        {
            softAmbientClip = CreateSineClip("SoftAmbient", 220f, 1.2f, 0.25f, true);
            magicChimeClip = CreateArpeggioClip("MagicChime", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.85f);
            whooshClip = CreateNoiseSweepClip("Whoosh", 0.5f);
            mysteriousDroneClip = CreateDroneClip("MysteriousDrone", 110f, 1.6f);
            titleImpactClip = CreateImpactChimeClip("TitleImpact", 0.9f);
        }

        private AudioClip CreateSineClip(string name, float freq, float duration, float maxVol, bool softDecay)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = softDecay ? Mathf.Sin(t / duration * Mathf.PI) : 1f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.3f;
                data[i] = wave * env * maxVol;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] notes, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, notes.Length - 1);
                float noteT = t - (noteIndex * noteDuration);
                float env = Mathf.Exp(-noteT * 7f);
                float wave = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
                data[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseSweepClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(400f, 150f, t) * ((float)i / sampleRate)) * 0.4f;
                data[i] = (noise + tone) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDroneClip(string name, float rootFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float wave1 = Mathf.Sin(2f * Mathf.PI * rootFreq * t);
                float wave2 = Mathf.Sin(2f * Mathf.PI * (rootFreq * 1.503f) * t) * 0.6f;
                float wave3 = Mathf.Sin(2f * Mathf.PI * (rootFreq * 2.001f) * t) * 0.3f;
                data[i] = (wave1 + wave2 + wave3) * env * 0.28f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateImpactChimeClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 3.8f);
                float wave = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.4f;
                data[i] = wave * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        #region Setup Helper di Editor

        public void ConfigureReferences(
            Canvas canvas,
            CanvasGroup blackOvr,
            CanvasGroup storyGrp,
            TextMeshProUGUI storyTxt,
            CanvasGroup titleGrp,
            RectTransform titleTr,
            TextMeshProUGUI skipTxt,
            CanvasGroup flashOvr,
            Transform lettersTr,
            Transform mazeTr,
            Transform enemyTr,
            Transform playerTr,
            Transform playerLettersTr,
            EnemyVisualAnimator enemyAnim,
            Camera cam,
            ParticleSystem particles,
            AudioClip readyClip,
            AudioClip swingClip,
            AudioClip hitClip)
        {
            prologueCanvas = canvas;
            blackOverlay = blackOvr;
            storyTextGroup = storyGrp;
            storyText = storyTxt;
            titleGroup = titleGrp;
            titleRect = titleTr;
            skipHintText = skipTxt;
            flashOverlay = flashOvr;
            letterGroup = lettersTr;
            mazeReveal = mazeTr;
            enemyReveal = enemyTr;
            playerReveal = playerTr;
            playerLettersGroup = playerLettersTr;
            enemyVisualAnimator = enemyAnim;
            mainCamera = cam;
            magicParticles = particles;
            enemyReadyClip = readyClip;
            enemySwordSwingClip = swingClip;
            enemySwordHitClip = hitClip;
        }

        #endregion
    }
}
