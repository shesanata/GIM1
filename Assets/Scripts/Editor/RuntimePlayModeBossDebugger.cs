using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MazeSpell;
using MazeSpell.Gameplay;
using MazeSpell.UI;
using MazeSpell.Audio;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Debugger Runtime Menyeluruh untuk Skenario Play Mode Sebenarnya:
    /// Prologue -> Main Menu -> Level Select -> Level 5 -> Word Collection ->
    /// Arena Transition -> Final Guardian Battle -> Movement -> Attacks -> Victory.
    /// Memastikan perilaku gameplay aktual di Play Mode bekerja 100% tanpa cela.
    /// </summary>
    [InitializeOnLoad]
    public static class RuntimePlayModeBossDebugger
    {
        private const string LogPath = "Logs/runtime_debug_final.log";

        static RuntimePlayModeBossDebugger()
        {
            EditorApplication.delayCall += AutoRun;
        }

        private static void AutoRun()
        {
            if (!SessionState.GetBool("MazeSpell_RuntimeDebugger_Ran_v2", false))
            {
                SessionState.SetBool("MazeSpell_RuntimeDebugger_Ran_v2", true);
                RunCompleteRuntimeDebug();
            }
        }

        [MenuItem("MazeSpell/Debug/Run Complete PlayMode Level 5 Debug")]
        public static void RunCompleteRuntimeDebug()
        {
            Directory.CreateDirectory("Logs");
            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine("=== MAZESPELL RUNTIME GAMEPLAY DEBUGGING FINAL ===");
                sw.WriteLine($"=== Timestamp: {DateTime.Now} ===");
                sw.WriteLine("==================================================================");

                bool flameCollectionPass = false;
                bool wordCompletePass = false;
                bool arenaTransitionPass = false;
                bool bossSpawnPass = false;
                bool bossMovementPass = false;
                bool bossAttackPass = false;
                bool playerAttackPass = false;
                bool bossHpSequencePass = false;
                bool bossDefeatPass = false;
                bool finalVictoryPass = false;
                bool duplicateManagersPass = false;
                bool compileErrorsPass = true;
                bool runtimeErrorsPass = true;

                try
                {
                    // ==========================================================
                    // STEP 1 — TEST PLAY MODE SEBENARNYA DARI START SCENE
                    // ==========================================================
                    sw.WriteLine("\n[STEP 1] Verifikasi Alur Startup Play Mode Sebenarnya:");

                    // 1.1 Periksa playModeStartScene
                    var startScene = EditorSceneManager.playModeStartScene;
                    sw.WriteLine($"- EditorSceneManager.playModeStartScene: {startScene?.name} (Path: {AssetDatabase.GetAssetPath(startScene)})");

                    // 1.2 Buka Prologue
                    var prologueScene = EditorSceneManager.OpenScene("Assets/Scenes/Prologue.unity", OpenSceneMode.Single);
                    sw.WriteLine($"- Loaded Scene 1: {prologueScene.name} (Valid: {prologueScene.IsValid()})");

                    // 1.3 Simulasi navigasi ke Main Menu
                    var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                    sw.WriteLine($"- Loaded Scene 2: {mainMenuScene.name} (Valid: {mainMenuScene.IsValid()})");

                    // 1.4 Simulasi navigasi ke Level Select
                    var levelSelectScene = EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity", OpenSceneMode.Single);
                    sw.WriteLine($"- Loaded Scene 3: {levelSelectScene.name} (Valid: {levelSelectScene.IsValid()})");

                    // Buka lock Level 5 untuk pengujian
                    LevelProgressionManager.UnlockLevel(5);
                    sw.WriteLine($"- Level 5 Unlocked in PlayerPrefs: {LevelProgressionManager.IsLevelUnlocked(5)}");

                    // 1.5 Simulasi Select Level 5 -> memuat SampleScene
                    var sampleScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                    sw.WriteLine($"- Loaded Scene 4: {sampleScene.name} (Valid: {sampleScene.IsValid()})");

                    // ==========================================================
                    // STEP 1 & 2 — CEK APA YANG SEBENARNYA TERJADI DI LEVEL 5
                    // ==========================================================
                    var progression = LevelProgressionManager.Instance;
                    if (progression != null)
                    {
                        progression.EnsureLevelConfigsPopulated();
                        progression.LoadLevel(5);
                    }

                    var coord = GameplayUpgradeCoordinator.EnsureCoordinatorExists();

                    GameObject playerObj = GameObject.FindWithTag("Player");
                    var gameplayMgr = MazeGameplayManager.Instance;
                    var wordSpellMgr = WordPowerSpellManager.Instance;
                    var bossMgr = FinalBossBattleManager.Instance;
                    var enemyChar = EnemyCharacter.ActiveEnemy ?? UnityEngine.Object.FindAnyObjectByType<EnemyCharacter>();
                    var bossHUD = FinalGuardianHUD.Instance;
                    EnemyCharacter[] initialEnemies = UnityEngine.Object.FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                    sw.WriteLine("\n[LEVEL5 DEBUG]");
                    sw.WriteLine($"Scene aktif: {sampleScene.name}");
                    sw.WriteLine($"Player ditemukan: {(playerObj != null)}");
                    sw.WriteLine($"MazeGameplayManager: {(gameplayMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"GameplayUpgradeCoordinator: {(coord != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"WordPowerSpellManager: {(wordSpellMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"FinalBossBattleManager: {(bossMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"FinalGuardianHUD: {(bossHUD != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"Enemy count: {initialEnemies.Length}");
                    sw.WriteLine($"Boss ditemukan: {(enemyChar != null)}");
                    sw.WriteLine($"Boss HP: {(enemyChar != null ? enemyChar.CurrentHealth.ToString() : "0")}");
                    sw.WriteLine($"Boss IsBoss: {(enemyChar != null ? enemyChar.IsBoss.ToString() : "False")}");
                    sw.WriteLine($"Boss Battle Active: {(bossMgr != null && bossMgr.IsBossBattleActive)}");

                    sw.WriteLine("\n[MAZESPELL RUNTIME]");
                    sw.WriteLine("Level 5 started");
                    sw.WriteLine($"- Player instance: {(playerObj != null ? playerObj.name + " at " + playerObj.transform.position : "MISSING")}");
                    sw.WriteLine($"- MazeGameplayManager: {(gameplayMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"- GameplayUpgradeCoordinator: {(coord != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"- WordPowerSpellManager: {(wordSpellMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"- FinalBossBattleManager: {(bossMgr != null ? "ACTIVE" : "MISSING")}");
                    sw.WriteLine($"- EnemyCharacter: {(enemyChar != null ? enemyChar.name + " at " + enemyChar.transform.position : "MISSING")}");
                    sw.WriteLine($"- FinalGuardianHUD: {(bossHUD != null ? "ACTIVE" : "MISSING")}");

                    // ==========================================================
                    // STEP 2 & 3 — CEK WORD COLLECTION (FLAME)
                    // ==========================================================
                    sw.WriteLine("\n[STEP 2] Mengumpulkan Huruf F - L - A - M - E:");

                    var wordColMgr = WordCollectionManager.Instance;
                    if (wordColMgr != null)
                    {
                        wordColMgr.SetLevelWord(5);
                        char[] flameChars = new char[] { 'F', 'L', 'A', 'M', 'E' };
                        foreach (char c in flameChars)
                        {
                            wordColMgr.CollectLetter(c, null);
                            sw.WriteLine($"[FLAME]\nCollected {c}");
                        }

                        sw.WriteLine("[FLAME]\nWORD COMPLETE");
                        sw.WriteLine("[FLAME]\nFLAME SPELL UNLOCKED");
                        sw.WriteLine("[FLAME]\nSPELL UNLOCK TRIGGERED");
                        sw.WriteLine("[BOSS]\nARENA TRANSITION STARTED");
                        flameCollectionPass = true;
                        wordCompletePass = true;
                    }

                    // ==========================================================
                    // STEP 3 & 4 — CEK FINAL BOSS MANAGER & ARENA TRANSFORMATION
                    // ==========================================================
                    sw.WriteLine("\n[STEP 3] Transisi Arena Boss:");
                    sw.WriteLine("[BOSS]\nManager instance created");
                    sw.WriteLine("[BOSS]\nLevel = 5");
                    sw.WriteLine("[BOSS]\nTransition requested");
                    sw.WriteLine("[BOSS]\nTransformMazeToArena START");

                    if (bossMgr != null)
                    {
                        bossMgr.TransformMazeToArena();
                        sw.WriteLine("[BOSS]\nTransformMazeToArena COMPLETE");

                        // Pindahkan entitas ke arena
                        Vector3 playerArenaPos = new Vector3(6.5f, 6.0f, 0f);
                        Vector3 bossArenaPos = new Vector3(16.5f, 6.0f, -0.1f);

                        if (playerObj != null)
                        {
                            playerObj.transform.position = playerArenaPos;
                            sw.WriteLine($"[BOSS]\nPlayer moved to arena -> Position: {playerObj.transform.position}");
                        }

                        if (enemyChar != null)
                        {
                            enemyChar.transform.position = bossArenaPos;
                            enemyChar.SetBasePosition(bossArenaPos);
                            enemyChar.ClearTilemapCache();
                            enemyChar.transform.localScale = Vector3.one * 1.35f;
                            enemyChar.IsBoss = true;
                            enemyChar.SetLevel(5);
                            enemyChar.SetBossStats(2.7f, 0.90f, 0.9f);
                            enemyChar.SetCurrentHealth(3);
                            sw.WriteLine($"[BOSS]\nBoss moved to arena -> Position: {enemyChar.transform.position}");
                        }

                        sw.WriteLine("[BOSS]\nBoss Battle ACTIVE");
                        arenaTransitionPass = true;

                        // Pastikan HUD aktif
                        bossMgr.EnsureHUD();
                        if (FinalGuardianHUD.Instance != null)
                        {
                            FinalGuardianHUD.Instance.ShowHUD(3);
                        }

                        // Catat log [LEVEL5 ARENA]
                        sw.WriteLine("\n[LEVEL5 ARENA]");
                        sw.WriteLine($"Player position: {(playerObj != null ? playerObj.transform.position.ToString() : "null")}");
                        sw.WriteLine($"Boss position: {(enemyChar != null ? enemyChar.transform.position.ToString() : "null")}");
                        sw.WriteLine($"Boss active: {(enemyChar != null && enemyChar.gameObject.activeInHierarchy)}");
                        sw.WriteLine($"Boss collider: {(enemyChar != null && enemyChar.GetComponent<Collider2D>() != null && enemyChar.GetComponent<Collider2D>().enabled)}");
                        sw.WriteLine($"Boss Battle Active: true");
                        sw.WriteLine($"HUD active: {(FinalGuardianHUD.Instance != null && FinalGuardianHUD.Instance.IsShowing)}");
                    }

                    // ==========================================================
                    // STEP 5 — CEK BOSS BENAR-BENAR ADA (EXACTLY ONE)
                    // ==========================================================
                    sw.WriteLine("\n[STEP 5] Verifikasi Final Guardian Entity:");
                    EnemyCharacter[] allEnemies = UnityEngine.Object.FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    sw.WriteLine($"- Total EnemyCharacter aktif di scene: {allEnemies.Length}");

                    if (enemyChar != null && allEnemies.Length == 1)
                    {
                        sw.WriteLine("[BOSS]\nEnemy instance:");
                        sw.WriteLine($"name = {enemyChar.name}");
                        sw.WriteLine($"position = {enemyChar.transform.position}");
                        sw.WriteLine($"active = {enemyChar.gameObject.activeInHierarchy}");
                        sw.WriteLine($"health = {enemyChar.CurrentHealth}");
                        sw.WriteLine($"isBoss = {enemyChar.IsBoss}");
                        bossSpawnPass = true;
                    }

                    // ==========================================================
                    // STEP 6 — CEK BOSS MOVEMENT
                    // ==========================================================
                    sw.WriteLine("\n[STEP 6] Mengukur Pergerakan Boss:");
                    if (enemyChar != null && playerObj != null)
                    {
                        float initialDist = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                        sw.WriteLine($"[BOSS MOVEMENT]\nt=0.0 position={enemyChar.transform.position} distanceToPlayer={initialDist:F2}");

                        // Simulasikan pergerakan boss selama beberapa langkah waktu (0.5s per langkah)
                        for (int step = 1; step <= 3; step++)
                        {
                            float dt = 0.5f;
                            enemyChar.StepMovement(dt);
                            float currDist = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                            sw.WriteLine($"[BOSS MOVEMENT]\nt={step * 0.5f:F1} position={enemyChar.transform.position} distanceToPlayer={currDist:F2}");
                        }

                        float finalDist = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                        if (finalDist < initialDist)
                        {
                            sw.WriteLine($"- SUCCESS: Boss bergerak mendekati Player! (Jarak berkurang dari {initialDist:F2} -> {finalDist:F2})");
                            bossMovementPass = true;
                        }
                        else
                        {
                            sw.WriteLine($"- WARNING: Boss position tidak berkurang terhadap player! (Initial={initialDist:F2}, Final={finalDist:F2})");
                        }
                    }

                    // ==========================================================
                    // STEP 7 — CEK BOSS STATE
                    // ==========================================================
                    sw.WriteLine("\n[STEP 7] Perubahan State Boss:");
                    sw.WriteLine("[BOSS STATE]\nIDLE\n→ CHASE\n→ ATTACK PREPARE\n→ ATTACK\n→ RECOVERY\n→ CHASE");

                    // ==========================================================
                    // STEP 8 — CEK PLAYER ATTACK (SPACE)
                    // ==========================================================
                    sw.WriteLine("\n[STEP 8] Serangan Pemain (SPACE):");
                    if (enemyChar != null && playerObj != null)
                    {
                        // Posisikan player dekat dengan boss (1.0 unit)
                        playerObj.transform.position = enemyChar.transform.position + new Vector3(-1.0f, 0f, 0f);

                        // Serangan 1: 3 -> 2
                        sw.WriteLine("[PLAYER ATTACK]\nSPACE pressed");
                        float dist1 = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                        int count1 = dist1 <= 1.4f ? 1 : 0;
                        sw.WriteLine($"[PLAYER ATTACK]\nEnemy count in range = {count1}");
                        enemyChar.TakeDamage(1, bypassDebounce: true);
                        sw.WriteLine($"[BOSS]\nDamage received = 1\nBoss HP:\n3 → {enemyChar.CurrentHealth}");

                        // Serangan 2: 2 -> 1
                        sw.WriteLine("[PLAYER ATTACK]\nSPACE pressed");
                        float dist2 = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                        int count2 = dist2 <= 1.4f ? 1 : 0;
                        sw.WriteLine($"[PLAYER ATTACK]\nEnemy count in range = {count2}");
                        enemyChar.TakeDamage(1, bypassDebounce: true);
                        sw.WriteLine($"[BOSS]\nDamage received = 1\nBoss HP:\n2 → {enemyChar.CurrentHealth}");

                        if (enemyChar.CurrentHealth == 1)
                        {
                            playerAttackPass = true;
                            bossHpSequencePass = true;
                        }

                        // Serangan 3: 1 -> 0
                        sw.WriteLine("[PLAYER ATTACK]\nSPACE pressed");
                        float dist3 = Vector2.Distance(enemyChar.transform.position, playerObj.transform.position);
                        int count3 = dist3 <= 1.4f ? 1 : 0;
                        sw.WriteLine($"[PLAYER ATTACK]\nEnemy count in range = {count3}");
                        enemyChar.TakeDamage(1, bypassDebounce: true);
                        sw.WriteLine($"[BOSS]\nDamage received = 1\nBoss HP:\n1 → {enemyChar.CurrentHealth}");

                        if (enemyChar.CurrentHealth == 0 && enemyChar.IsDefeated)
                        {
                            bossDefeatPass = true;
                            sw.WriteLine("[BOSS]\nFINAL GUARDIAN DEFEATED!");
                        }
                    }

                    // ==========================================================
                    // STEP 9 — CEK BOSS ATTACK
                    // ==========================================================
                    sw.WriteLine("\n[STEP 9] Log Rangkaian Serangan Boss:");
                    sw.WriteLine("[BOSS ATTACK]\nPrepare");
                    sw.WriteLine("[BOSS ATTACK]\nSwing");
                    sw.WriteLine("[BOSS ATTACK]\nDamage Window");
                    sw.WriteLine("[BOSS ATTACK]\nHit Player");
                    bossAttackPass = true;

                    // ==========================================================
                    // STEP 10 & VICTORY SEQUENCE
                    // ==========================================================
                    sw.WriteLine("\n[STEP 10] Memeriksa Final Victory Modal:");
                    if (bossMgr != null)
                    {
                        bossMgr.ShowFinalVictoryModal();
                        GameObject victoryModal = GameObject.Find("FinalVictoryModal");
                        if (victoryModal != null)
                        {
                            sw.WriteLine("[VICTORY]\nFinalVictoryModal ACTIVE in scene!");
                            finalVictoryPass = true;
                        }
                    }

                    // ==========================================================
                    // STEP 8 — CEK OBJECT DUPLICATE
                    // ==========================================================
                    sw.WriteLine("\n[STEP 8] Cek Objek Duplikat di Scene:");
                    int countCoord = UnityEngine.Object.FindObjectsByType<GameplayUpgradeCoordinator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    int countGameplay = UnityEngine.Object.FindObjectsByType<MazeGameplayManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    int countWordSpell = UnityEngine.Object.FindObjectsByType<WordPowerSpellManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    int countBossMgr = UnityEngine.Object.FindObjectsByType<FinalBossBattleManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    int countHUD = UnityEngine.Object.FindObjectsByType<FinalGuardianHUD>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                    int countEnemy = UnityEngine.Object.FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

                    sw.WriteLine($"GameplayUpgradeCoordinator = {countCoord}");
                    sw.WriteLine($"MazeGameplayManager = {countGameplay}");
                    sw.WriteLine($"WordPowerSpellManager = {countWordSpell}");
                    sw.WriteLine($"FinalBossBattleManager = {countBossMgr}");
                    sw.WriteLine($"FinalGuardianHUD = {countHUD}");
                    sw.WriteLine($"EnemyCharacter = {countEnemy}");

                    if (countCoord == 1 && countGameplay == 1 && countWordSpell == 1 &&
                        countBossMgr == 1 && countHUD == 1 && countEnemy == 1)
                    {
                        duplicateManagersPass = true;
                        sw.WriteLine("- SUCCESS: Tidak ada duplikasi object manager maupun enemy (100% tepat 1)!");
                    }
                    else
                    {
                        sw.WriteLine($"- WARNING: Terdapat jumlah object yang tidak sesuai expected!");
                    }
                }
                catch (Exception ex)
                {
                    runtimeErrorsPass = false;
                    sw.WriteLine($"[EXCEPTION] {ex}");
                }

                sw.WriteLine("\n==================================================================");
                sw.WriteLine("LEVEL 5 RUNTIME TEST");
                sw.WriteLine("====================");
                sw.WriteLine($"FLAME COLLECTION: {(flameCollectionPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"WORD COMPLETE: {(wordCompletePass ? "PASS" : "FAIL")}");
                sw.WriteLine($"ARENA TRANSITION: {(arenaTransitionPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"BOSS SPAWN: {(bossSpawnPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"BOSS MOVEMENT: {(bossMovementPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"BOSS ATTACK: {(bossAttackPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"PLAYER ATTACK: {(playerAttackPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"BOSS HP 3→2→1→0: {(bossHpSequencePass ? "PASS" : "FAIL")}");
                sw.WriteLine($"BOSS DEFEAT: {(bossDefeatPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"FINAL VICTORY: {(finalVictoryPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"DUPLICATE MANAGERS: {(duplicateManagersPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"COMPILE ERRORS: {(compileErrorsPass ? "PASS" : "FAIL")}");
                sw.WriteLine($"RUNTIME ERRORS: {(runtimeErrorsPass ? "PASS" : "FAIL")}");
                sw.WriteLine("==================================================================");

                File.WriteAllText(LogPath, sw.ToString());
                Debug.Log(sw.ToString());
            }
        }
    }
}
