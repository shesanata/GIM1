using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.UI;
using MazeSpell.Gameplay;

namespace MazeSpell.Editor
{
    public static class MazeRuntimeTestRunner
    {
        [MenuItem("MazeSpell/Run Full Gameplay Verification")]
        public static void RunAllTests()
        {
            Debug.Log("=================================================");
            Debug.Log(">>> STARTING MAZESPELL RUNTIME VERIFICATION <<<");
            Debug.Log("=================================================");

            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;

            void Assert(bool condition, string testName)
            {
                totalTests++;
                if (condition)
                {
                    passedTests++;
                    Debug.Log($"[TEST PASS] {testName}");
                }
                else
                {
                    failedTests++;
                    Debug.LogError($"[TEST FAIL] {testName}");
                }
            }

            void TestEnemyFull(int lvl, EnemyCharacter enemy, GameObject player)
            {
                Assert(enemy != null, $"Level {lvl}: Enemy Found");
                enemy.StepMovement(0.02f);
                Assert(player != null, $"Level {lvl}: Target Player assigned");

                enemy.ClearTilemapCache();
                Vector3Int startCell = new Vector3Int(Mathf.FloorToInt(enemy.transform.position.x), Mathf.FloorToInt(enemy.transform.position.y), 0);
                Vector3Int targetCell = new Vector3Int(Mathf.FloorToInt(player.transform.position.x), Mathf.FloorToInt(player.transform.position.y), 0);
                var path = enemy.CalculateBFSPath(startCell, targetCell);
                Assert(path != null && path.Count > 0, $"Level {lvl}: Path Found (Length: {(path != null ? path.Count : 0)})");

                Vector3 prePos = enemy.transform.position;
                for (int i = 0; i < 20; i++)
                {
                    enemy.StepMovement(0.02f);
                }
                float moved = Vector3.Distance(prePos, enemy.transform.position);
                Assert(moved > 0.04f, $"Level {lvl}: Movement occurred ({moved:F3} units)");
                Assert(enemy.IsChasing, $"Level {lvl}: Chase state active");

                Vector3 origPos = enemy.transform.position;
                enemy.transform.position = player.transform.position + new Vector3(0.85f, 0f, 0f);
                enemy.ResetDamageCooldown();
                int livesBefore = PlayerLivesManager.Instance.CurrentLives;
                enemy.CheckAndDamagePlayer(player);
                Assert(PlayerLivesManager.Instance.CurrentLives == livesBefore - 1, $"Level {lvl}: Attack successfully deals damage");
                PlayerLivesManager.Instance.ResetLives();

                int origHp = enemy.CurrentHealth;
                enemy.SetCurrentHealth(origHp + 1);
                int hpBefore = enemy.CurrentHealth;
                bool playerAttackHit = enemy.TryPlayerAttack(player, 1);
                Assert(playerAttackHit, $"Level {lvl}: Player Attack hit enemy");
                Assert(enemy.CurrentHealth == hpBefore - 1, $"Level {lvl}: Player Attack reduced enemy HP ({hpBefore} -> {enemy.CurrentHealth})");

                if (enemy != null)
                {
                    enemy.transform.position = origPos;
                    enemy.SetCurrentHealth(enemy.MaxHealth);
                }
            }

            try
            {
                // 0. Verify MainMenu Scene (Visual Reference, PLAY & LEVEL buttons)
                var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                Assert(menuScene.IsValid(), "MainMenu.unity successfully loaded");
                GameObject menuCanvas = GameObject.Find("Canvas");
                Assert(menuCanvas != null, "Canvas exists in MainMenu");
                var menuCtrl = menuCanvas.GetComponent<MainMenuController>();
                Assert(menuCtrl != null, "MainMenuController attached to Canvas");

                Transform playBtnT = menuCanvas.transform.Find("PlayButton");
                Assert(playBtnT != null && playBtnT.gameObject.activeSelf, "PlayButton exists and is active");
                var playBtnComp = playBtnT.GetComponent<UnityEngine.UI.Button>();
                Assert(playBtnComp != null && playBtnComp.interactable, "PlayButton is interactable and clickable");
                var playImg = playBtnT.GetComponent<UnityEngine.UI.Image>();
                Assert(playImg != null && playImg.sprite != null && playImg.sprite.name.Contains("cartoon_play"), "PlayButton has green cartoon sprite 'btn_cartoon_play'");

                Transform levelBtnT = menuCanvas.transform.Find("LevelButton");
                Assert(levelBtnT != null && levelBtnT.gameObject.activeSelf, "LevelButton exists and is active");
                var levelBtnComp = levelBtnT.GetComponent<UnityEngine.UI.Button>();
                Assert(levelBtnComp != null && levelBtnComp.interactable, "LevelButton is interactable and clickable");
                var levelImg = levelBtnT.GetComponent<UnityEngine.UI.Image>();
                Assert(levelImg != null && levelImg.sprite != null && levelImg.sprite.name.Contains("cartoon_level"), "LevelButton has green cartoon sprite 'btn_cartoon_level'");

                // Strict rule: ONLY 2 buttons in Main Menu
                var allButtons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>();
                Assert(allButtons.Length == 2, $"Only 2 buttons exist in Main Menu (Found: {allButtons.Length})");
                Assert(menuCanvas.transform.Find("ExitButton") == null, "Old ExitButton is completely removed");
                Assert(menuCanvas.transform.Find("AudioSettingsButton") == null, "Old AudioSettingsButton is completely removed");

                Transform bgT = menuCanvas.transform.Find("Background");
                Assert(bgT != null && bgT.gameObject.activeSelf, "Background exists and is active");
                var bgImg = bgT.GetComponent<UnityEngine.UI.Image>();
                Assert(bgImg != null && bgImg.sprite != null && bgImg.sprite.name.Contains("menuBG"), "Background Image uses reference stone wall artwork 'menuBG'");

                Transform logoT = menuCanvas.transform.Find("Logo");
                Assert(logoT == null || !logoT.gameObject.activeSelf, "Redundant text Logo is removed/inactive");

                // 1. Load Gameplay Scene
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                Assert(scene.IsValid(), "SampleScene.unity successfully loaded");

                // Clean up duplicate PauseMenuController instances if any
                var pauseControllers = UnityEngine.Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (pauseControllers.Length > 1)
                {
                    Debug.Log($"[Cleanup] Found {pauseControllers.Length} PauseMenuController instances. Cleaning duplicates...");
                    for (int i = 1; i < pauseControllers.Length; i++)
                    {
                        UnityEngine.Object.DestroyImmediate(pauseControllers[i].gameObject);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[Cleanup] Duplicate PausePanels removed and scene saved.");
                }

                // 2. Setup Managers
                var progression = LevelProgressionManager.Instance;
                var wordManager = WordCollectionManager.Instance;
                var livesManager = PlayerLivesManager.Instance;

                Assert(progression != null, "LevelProgressionManager instance exists");
                Assert(wordManager != null, "WordCollectionManager instance exists");
                Assert(livesManager != null, "PlayerLivesManager instance exists");

                // Ensure prefabs and configs are populated
                progression.EnsureLevelConfigsPopulated();

                // 3. Test Level 1 Loading
                progression.LoadLevel(1);
                Assert(progression.CurrentLevel == 1, "Level 1 is loaded");
                Assert(wordManager.CurrentLevelNumber == 1, "WordCollectionManager synchronized to Level 1");
                Assert(wordManager.CurrentTargetWord == "EARTH", "Level 1 Target Word is EARTH");
                Assert(wordManager.TotalRequired == 5, "Level 1 required letters count is 5");
                Assert(!wordManager.IsWordComplete, "Level 1 Word is initially incomplete");
                Assert(livesManager.CurrentLives == 3, "Player starts with 3 lives");

                // 3b. Verify Level 1 Dimensions & BFS Pathfinding
                Assert(progression.CurrentData != null && progression.CurrentData.columns == 20 && progression.CurrentData.rows == 16, "Level 1 Grid is 20x16 Cartoon House");

                int l1Rows = 16;
                int l1Cols = 20;
                int walkableCells = 0;
                for (int r = 0; r < l1Rows; r++)
                    for (int c = 0; c < l1Cols; c++)
                        if (MazeMatrixRegistry.Level1Matrix[r, c] == 0) walkableCells++;

                bool[,] bfsVisited = new bool[l1Rows, l1Cols];
                Queue<Vector2Int> bfsQ = new Queue<Vector2Int>();
                Vector2Int bfsStart = new Vector2Int(2, 7);
                bfsVisited[15 - bfsStart.y, bfsStart.x] = true;
                bfsQ.Enqueue(bfsStart);
                int reached = 0;
                int[] bdx = { 0, 0, 1, -1 };
                int[] bdy = { 1, -1, 0, 0 };
                while (bfsQ.Count > 0)
                {
                    Vector2Int cur = bfsQ.Dequeue();
                    reached++;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cur.x + bdx[i];
                        int ny = cur.y + bdy[i];
                        if (nx >= 0 && nx < l1Cols && ny >= 0 && ny < l1Rows)
                        {
                            int nr = 15 - ny;
                            if (MazeMatrixRegistry.Level1Matrix[nr, nx] == 0 && !bfsVisited[nr, nx])
                            {
                                bfsVisited[nr, nx] = true;
                                bfsQ.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
                Assert(reached == walkableCells, $"Level 1 BFS: 100% of walkable cells reached from Start ({reached}/{walkableCells})");

                // 4. Test Goal Locked before collection
                GameObject grid1 = GameObject.Find("Level1_MazeGrid");
                Assert(grid1 != null, "Level1_MazeGrid instantiated in scene");

                var goalTr = grid1.transform.Find("GameplayObjects/Goal");
                Assert(goalTr != null, "Goal object exists in Level 1");
                Assert(Vector3.Distance(goalTr.position, new Vector3(18.5f, 4.5f, 0f)) < 0.2f, "Goal positioned at exit (18.5, 4.5)");
                var goalTrigger = goalTr.GetComponent<LevelGoalTrigger>();
                Assert(goalTrigger != null, "Goal has LevelGoalTrigger component");
                var goalCol = goalTr.GetComponent<Collider2D>();
                Assert(goalCol != null && goalCol.isTrigger, "Goal has Collider2D with isTrigger = true");

                var playerObj = GameObject.FindWithTag("Player");
                Assert(playerObj != null, "Player object found");
                Assert(Vector3.Distance(playerObj.transform.position, new Vector3(2.5f, 7.5f, 0f)) < 0.2f, "Player correctly spawned at Level 1 SpawnPoint in Yard (2.5, 7.5)");
                var playerCol = playerObj.GetComponent<Collider2D>();

                // Simulate Player touching Goal before word is complete
                var triggerEnterMethod = typeof(LevelGoalTrigger).GetMethod("OnTriggerEnter2D",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert(triggerEnterMethod != null, "LevelGoalTrigger has OnTriggerEnter2D method");

                triggerEnterMethod.Invoke(goalTrigger, new object[] { playerCol });
                Assert(progression.CurrentLevel == 1, "Goal is locked: Player remains in Level 1 before collecting letters");

                // 4.5. Test Level 1 Enemy Guard System
                var enemies1 = grid1.GetComponentsInChildren<EnemyCharacter>();
                Assert(enemies1.Length == 1, $"Level 1 has exactly 1 EnemyCharacter object (Found {enemies1.Length})");
                bool hasStartCorridorEnemy = false;
                bool hasGoalCorridorEnemy = false;
                foreach (var en in enemies1)
                {
                    if (Mathf.Abs(en.transform.position.x - 2.5f) < 0.5f) hasStartCorridorEnemy = true;
                    if (Mathf.Abs(en.transform.position.x - 18.5f) < 0.5f) hasGoalCorridorEnemy = true;
                }
                Assert(!hasStartCorridorEnemy, "Yard Path X=2 is clear: Player can freely exit start area");
                Assert(!hasGoalCorridorEnemy, "Goal exit X=18.5 is clear: Player can freely reach Goal");

                Assert(enemies1[0].Speed == 5f, $"Enemy 1 has Speed == 5 (Actual: {enemies1[0].Speed})");
                Assert(enemies1[0].Health == 1, $"Enemy 1 has Health == 1 (Actual: {enemies1[0].Health})");
                Assert(enemies1[0].DamageAmount == 1, $"Enemy 1 has DamageAmount == 1 (Actual: {enemies1[0].DamageAmount})");

                int cellX = Mathf.FloorToInt(enemies1[0].transform.position.x);
                int cellY = Mathf.FloorToInt(enemies1[0].transform.position.y);
                Assert(MazeMatrixRegistry.Level1Matrix[15 - cellY, cellX] == 0, $"Enemy 1 is on walkable cell ({cellX}, {cellY})");

                // ==========================================
                // 12 VALIDASI RESMI SISTEM ENEMY & COMBAT
                // ==========================================

                // TEST 1: Enemy jauh dari Player -> Enemy tidak memberikan damage
                livesManager.ResetLives();
                playerObj.transform.position = new Vector3(2.5f, 7.5f, 0f);
                enemies1[0].transform.position = new Vector3(11.5f, 10.5f, 0f);
                enemies1[0].ResetDamageCooldown();
                enemies1[0].CheckAndDamagePlayer(playerObj);
                Assert(livesManager.CurrentLives == 3, $"TEST 1: Enemy jauh ({Vector2.Distance(enemies1[0].transform.position, playerObj.transform.position):F1} unit) -> tidak memberikan damage (Nyawa: {livesManager.CurrentLives})");

                // TEST 2: Enemy mendekati Player -> Enemy mengejar dan BERPINDAH POSISI (Displacement Check)
                Vector3 preMovePos = enemies1[0].transform.position;
                for (int step = 0; step < 20; step++)
                {
                    enemies1[0].StepMovement(0.02f);
                }
                float distanceMoved = Vector3.Distance(preMovePos, enemies1[0].transform.position);
                Assert(distanceMoved > 0.05f && enemies1[0].IsChasing, 
                    $"TEST 2: Enemy mengejar Player dan berpindah posisi saat chase (Berpindah: {distanceMoved:F2} unit, IsChasing: {enemies1[0].IsChasing})");

                // TEST 3: Enemy masuk attack range (0.8 - 1.0 unit) -> Enemy dapat menyerang
                enemies1[0].transform.position = playerObj.transform.position + new Vector3(0.85f, 0f, 0f);
                float nearDist = Vector2.Distance(enemies1[0].transform.position, playerObj.transform.position);
                Assert(nearDist <= enemies1[0].AttackRange, $"TEST 3: Enemy masuk attack range ({nearDist:F2} <= {enemies1[0].AttackRange})");

                // TEST 4: Enemy menyerang -> Player kehilangan tepat 1 ❤️
                enemies1[0].ResetDamageCooldown();
                enemies1[0].CheckAndDamagePlayer(playerObj);
                Assert(livesManager.CurrentLives == 2, $"TEST 4: Enemy menyerang saat dalam attack range -> Player kehilangan tepat 1 ❤️ (Nyawa: {livesManager.CurrentLives})");

                // Test cooldown kontak mencegah damage bertubi-tubi pada satu kontak
                enemies1[0].CheckAndDamagePlayer(playerObj);
                Assert(livesManager.CurrentLives == 2, $"TEST 4 (Cooldown): Cooldown mencegah pengurangan nyawa bertubi-tubi (Nyawa: {livesManager.CurrentLives})");

                // TEST 4 (Sub-test Game Over): Player memiliki 1 nyawa -> Enemy menyerang -> Game Over
                livesManager.SetLivesForTesting(1);
                Assert(livesManager.CurrentLives == 1, "TEST 4 (Game Over Setup): Player disimulasikan memiliki 1 nyawa");
                enemies1[0].ResetDamageCooldown();
                enemies1[0].CheckAndDamagePlayer(playerObj);
                Assert(livesManager.CurrentLives == 0, $"TEST 4 (Game Over): Enemy menyerang saat 1 nyawa -> Nyawa menjadi 0 (Actual: {livesManager.CurrentLives})");
                Assert(livesManager.IsGameOver, "TEST 4 (Game Over): Game Over aktif saat nyawa mencapai 0");
                livesManager.ResetLives();

                // TEST 5: Player terlalu jauh -> SPACE tidak membunuh / merusak Enemy dan NYAWA PLAYER TETAP AMAN
                livesManager.ResetLives();
                GameObject farTestEnemy = new GameObject("FarTestEnemy");
                farTestEnemy.AddComponent<CircleCollider2D>();
                farTestEnemy.transform.position = playerObj.transform.position + new Vector3(3.0f, 0f, 0f);
                var farChar = farTestEnemy.AddComponent<EnemyCharacter>();
                farChar.SetLevel(5);
                bool farHit = farChar.TryPlayerAttack(playerObj);
                Assert(!farHit && farChar.Health == 3 && livesManager.CurrentLives == 3, 
                    $"TEST 5: Player terlalu jauh (3.0 unit) -> SPACE tidak merusak Enemy & NYAWA PLAYER AMAN (Hit: {farHit}, Enemy HP: {farChar.Health}/3, Player ❤️: {livesManager.CurrentLives}/3)");
                UnityEngine.Object.DestroyImmediate(farTestEnemy);

                // TEST 6: Player dekat Enemy + tekan SPACE -> Enemy menerima damage dan NYAWA PLAYER TIDAK BERKURANG
                livesManager.ResetLives();
                GameObject nearTestEnemy = new GameObject("NearTestEnemy");
                nearTestEnemy.AddComponent<CircleCollider2D>();
                nearTestEnemy.transform.position = playerObj.transform.position + new Vector3(1.0f, 0f, 0f);
                var nearChar = nearTestEnemy.AddComponent<EnemyCharacter>();
                nearChar.SetLevel(5);
                bool nearHit = nearChar.TryPlayerAttack(playerObj, 1);
                Assert(nearHit && nearChar.Health < 3 && livesManager.CurrentLives == 3, 
                    $"TEST 6: Player dekat (1.0 unit) + tekan SPACE -> Enemy menerima damage & NYAWA PLAYER AMAN (Enemy HP: {nearChar.Health}/3, Player ❤️: {livesManager.CurrentLives}/3)");
                UnityEngine.Object.DestroyImmediate(nearTestEnemy);

                // TEST 7: Enemy dikalahkan -> Player +1 ❤️
                Assert(livesManager.CurrentLives == 3, "Player starts with 3 lives before defeat reward test");
                livesManager.SetLivesForTesting(2);
                Assert(livesManager.CurrentLives == 2, "TEST 7: Player memiliki 2 nyawa sebelum Enemy dikalahkan");
                GameObject combatTestEnemy = new GameObject("CombatTestEnemy");
                combatTestEnemy.AddComponent<CircleCollider2D>();
                var combatChar = combatTestEnemy.AddComponent<EnemyCharacter>();
                combatChar.Defeat();
                Assert(livesManager.CurrentLives == 3, $"TEST 7: Enemy dikalahkan -> Player mendapatkan +1 ❤️ (Nyawa: {livesManager.CurrentLives})");

                // TEST 8: Player ❤️ = 3 + defeat Enemy -> tetap 3
                Assert(livesManager.CurrentLives == 3, "TEST 8: Player memiliki 3 nyawa penuh");
                GameObject combatTestEnemy2 = new GameObject("CombatTestEnemy2");
                combatTestEnemy2.AddComponent<CircleCollider2D>();
                var combatChar2 = combatTestEnemy2.AddComponent<EnemyCharacter>();
                combatChar2.Defeat();
                Assert(livesManager.CurrentLives == 3, $"TEST 8: Player ❤️ = 3 + defeat Enemy -> tetap maksimal 3 (Nyawa: {livesManager.CurrentLives})");

                // TEST 9: Enemy sudah kalah -> tidak respawn dan reward tidak berulang
                livesManager.SetLivesForTesting(2);
                GameObject combatTestEnemy3 = new GameObject("CombatTestEnemy3");
                combatTestEnemy3.AddComponent<CircleCollider2D>();
                var combatChar3 = combatTestEnemy3.AddComponent<EnemyCharacter>();
                combatChar3.Defeat();
                Assert(livesManager.CurrentLives == 3, "TEST 9: Defeat pertama kali memberikan +1 ❤️");
                livesManager.SetLivesForTesting(2);
                combatChar3.Defeat();
                Assert(livesManager.CurrentLives == 2 && combatChar3.IsDefeated, $"TEST 9: Enemy sudah kalah -> reward tidak berulang (Nyawa tetap: {livesManager.CurrentLives})");
                livesManager.ResetLives();

                // TEST 11: Speed Enemy: Level 1 (1.5) < Level 2 (1.8) < Level 3 (2.1) < Level 4 (2.4) < Level 5 (2.7)
                float spd1 = EnemyCharacter.GetSpeedForLevel(1);
                float spd2 = EnemyCharacter.GetSpeedForLevel(2);
                float spd3 = EnemyCharacter.GetSpeedForLevel(3);
                float spd4 = EnemyCharacter.GetSpeedForLevel(4);
                float spd5 = EnemyCharacter.GetSpeedForLevel(5);
                Assert(spd1 < spd2 && spd2 < spd3 && spd3 < spd4 && spd4 < spd5,
                    $"TEST 11: Speed Enemy meningkat sesuai level: L1({spd1}) < L2({spd2}) < L3({spd3}) < L4({spd4}) < L5({spd5})");

                // TEST 12: Enemy pathfinding tidak melewati dinding
                var safePath = enemies1[0].CalculateBFSPath(new Vector3Int(11, 10, 0), new Vector3Int(2, 7, 0));
                Assert(safePath != null && safePath.Count > 0, "TEST 12: Jalur BFS Enemy ke Player berhasil dihitung");
                bool pathHasWall = false;
                if (safePath != null)
                {
                    foreach (var pt in safePath)
                    {
                        if (MazeMatrixRegistry.Level1Matrix[15 - pt.y, pt.x] != 0)
                        {
                            pathHasWall = true;
                            break;
                        }
                    }
                }
                Assert(!pathHasWall, "TEST 12: Seluruh rute BFS 100% pada sel walkable (Enemy tidak menembus dinding)");

                // Kembalikan posisi awal Enemy 1 dan Player 1 untuk tes gameplay berikutnya
                enemies1[0].transform.position = new Vector3(11.5f, 10.5f, -0.1f);
                playerObj.transform.position = new Vector3(2.5f, 7.5f, 0f);
                TestEnemyFull(1, enemies1[0], playerObj);

                // 5. Test Collecting Level 1 Letters (EARTH)
                var letters = grid1.GetComponentsInChildren<CollectibleLetter>();
                Assert(letters.Length == 5, $"Level 1 has exactly 5 CollectibleLetters (Found {letters.Length})");

                // Map letters by char
                Dictionary<char, CollectibleLetter> letterMap = new Dictionary<char, CollectibleLetter>();
                foreach (var cl in letters)
                {
                    letterMap[cl.Letter] = cl;
                }

                Assert(letterMap.ContainsKey('E'), "Letter E exists in Level 1");
                Assert(letterMap.ContainsKey('A'), "Letter A exists in Level 1");
                Assert(letterMap.ContainsKey('R'), "Letter R exists in Level 1");
                Assert(letterMap.ContainsKey('T'), "Letter T exists in Level 1");
                Assert(letterMap.ContainsKey('H'), "Letter H exists in Level 1");

                // Collect E
                letterMap['E'].Collect();
                Assert(letterMap['E'].IsCollected, "Letter E marked as collected");
                Assert(wordManager.CollectedCount == 1, "Word progress has 1 letter collected");
                Assert(wordManager.GetProgressString() == "E _ _ _ _", $"Progress string is 'E _ _ _ _' (Actual: {wordManager.GetProgressString()})");

                // Collect A
                letterMap['A'].Collect();
                Assert(wordManager.GetProgressString() == "E A _ _ _", $"Progress string is 'E A _ _ _' (Actual: {wordManager.GetProgressString()})");

                // Collect R
                letterMap['R'].Collect();
                Assert(wordManager.GetProgressString() == "E A R _ _", $"Progress string is 'E A R _ _' (Actual: {wordManager.GetProgressString()})");

                // Collect T
                letterMap['T'].Collect();
                Assert(wordManager.GetProgressString() == "E A R T _", $"Progress string is 'E A R T _' (Actual: {wordManager.GetProgressString()})");

                // Verify not complete yet
                Assert(!wordManager.IsWordComplete, "Word is not yet complete before H");
                Assert(progression.CurrentLevel == 1, "Player still in Level 1 before last letter");

                // Collect H
                letterMap['H'].Collect();
                Assert(wordManager.IsWordComplete, "Word EARTH is now complete!");
                Assert(wordManager.GetProgressString() == "E A R T H", $"Progress string is 'E A R T H' (Actual: {wordManager.GetProgressString()})");
                Assert(progression.CurrentLevel == 1, "Player is STILL in Level 1 after word completion (must touch Goal)");

                // 6. Test Goal Open after word is complete
                triggerEnterMethod.Invoke(goalTrigger, new object[] { playerCol });
                Assert(progression.CurrentLevel == 2, $"Touching Goal with complete word transitions to Level 2 (Actual: {progression.CurrentLevel})");

                // 7. Test Level 2 (STONE)
                Assert(wordManager.CurrentLevelNumber == 2, "WordCollectionManager is at Level 2");
                Assert(wordManager.CurrentTargetWord == "STONE", $"Level 2 Target Word is STONE (Actual: {wordManager.CurrentTargetWord})");
                Assert(wordManager.CollectedCount == 0, "Level 2 collected count is reset to 0");
                Assert(wordManager.GetProgressString() == "_ _ _ _ _", $"Level 2 Progress is reset to empty (Actual: {wordManager.GetProgressString()})");

                GameObject grid2 = GameObject.Find("Level2_MazeGrid");
                Assert(grid2 != null, "Level2_MazeGrid instantiated in scene");

                var enemies2 = grid2.GetComponentsInChildren<EnemyCharacter>();
                Assert(enemies2.Length == 1, $"TEST 6 (Level 2): Tepat 1 Enemy di Level 2 (Actual: {enemies2.Length})");
                TestEnemyFull(2, enemies2[0], playerObj);

                // Collect STONE
                var letters2 = grid2.GetComponentsInChildren<CollectibleLetter>();
                Assert(letters2.Length == 5, $"Level 2 has 5 letters (Found {letters2.Length})");
                foreach (var cl in letters2)
                {
                    cl.Collect();
                }
                Assert(wordManager.IsWordComplete, "Word STONE is complete");

                var goalTr2 = grid2.transform.Find("GameplayObjects/Goal");
                var goalTrigger2 = goalTr2.GetComponent<LevelGoalTrigger>();
                triggerEnterMethod.Invoke(goalTrigger2, new object[] { playerCol });
                Assert(progression.CurrentLevel == 3, $"Touching Goal 2 transitions to Level 3 (Actual: {progression.CurrentLevel})");

                // 8. Test Level 3 (GRASS - Two 'S's)
                Assert(wordManager.CurrentLevelNumber == 3, "WordCollectionManager is at Level 3");
                Assert(wordManager.CurrentTargetWord == "GRASS", $"Level 3 Target Word is GRASS (Actual: {wordManager.CurrentTargetWord})");
                Assert(wordManager.TotalRequired == 5, "Level 3 Total Required is 5 letters");

                GameObject grid3 = GameObject.Find("Level3_MazeGrid");
                Assert(grid3 != null, "Level3_MazeGrid instantiated in scene");

                var enemies3 = grid3.GetComponentsInChildren<EnemyCharacter>();
                Assert(enemies3.Length == 1, $"TEST 6 (Level 3): Tepat 1 Enemy di Level 3 (Actual: {enemies3.Length})");
                TestEnemyFull(3, enemies3[0], playerObj);

                var letters3 = grid3.GetComponentsInChildren<CollectibleLetter>();
                Assert(letters3.Length == 5, $"Level 3 has 5 letters (Found {letters3.Length})");

                int sCount = 0;
                foreach (var cl in letters3)
                {
                    if (cl.Letter == 'S') sCount++;
                }
                Assert(sCount == 2, $"Level 3 has exactly TWO 'S' letters in maze (Found {sCount})");

                // Collect all letters in Level 3
                foreach (var cl in letters3)
                {
                    cl.Collect();
                    Assert(cl.IsCollected, $"Letter '{cl.Letter}' was accepted and collected");
                }
                Assert(wordManager.CollectedCount == 5, $"Level 3 collected count is 5/5 (Actual: {wordManager.CollectedCount})");
                Assert(wordManager.IsWordComplete, "Word GRASS with two 'S's is complete!");
                Assert(wordManager.GetProgressString() == "G R A S S", $"Progress string is 'G R A S S' (Actual: {wordManager.GetProgressString()})");

                var goalTr3 = grid3.transform.Find("GameplayObjects/Goal");
                var goalTrigger3 = goalTr3.GetComponent<LevelGoalTrigger>();
                triggerEnterMethod.Invoke(goalTrigger3, new object[] { playerCol });
                Assert(progression.CurrentLevel == 4, $"Touching Goal 3 transitions to Level 4 (Actual: {progression.CurrentLevel})");

                // 9. Test Level 4 (FROST - House Layout)
                Assert(wordManager.CurrentLevelNumber == 4, "WordCollectionManager is at Level 4");
                Assert(wordManager.CurrentTargetWord == "FROST", $"Level 4 Target Word is FROST (Actual: {wordManager.CurrentTargetWord})");
                Assert(wordManager.TotalRequired == 5, "Level 4 Total Required is 5 letters");
                GameObject grid4 = GameObject.Find("Level4_MazeGrid");
                Assert(grid4 != null, "Level4_MazeGrid instantiated in scene");

                // 9b. Verify Level 4 Dimensions & BFS Pathfinding
                Assert(progression.CurrentData != null && progression.CurrentData.columns == 20 && progression.CurrentData.rows == 16, "Level 4 Grid is 20x16 Cartoon House");

                int l4Rows = 16;
                int l4Cols = 20;
                int walkableL4 = 0;
                for (int r = 0; r < l4Rows; r++)
                    for (int c = 0; c < l4Cols; c++)
                        if (MazeMatrixRegistry.Level4Matrix[r, c] == 0) walkableL4++;

                bool[,] bfsVisited4 = new bool[l4Rows, l4Cols];
                Queue<Vector2Int> bfsQ4 = new Queue<Vector2Int>();
                Vector2Int bfsStart4 = new Vector2Int(7, 2);
                bfsVisited4[15 - bfsStart4.y, bfsStart4.x] = true;
                bfsQ4.Enqueue(bfsStart4);
                int reached4 = 0;
                while (bfsQ4.Count > 0)
                {
                    Vector2Int cur = bfsQ4.Dequeue();
                    reached4++;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cur.x + bdx[i];
                        int ny = cur.y + bdy[i];
                        if (nx >= 0 && nx < l4Cols && ny >= 0 && ny < l4Rows)
                        {
                            int nr = 15 - ny;
                            if (MazeMatrixRegistry.Level4Matrix[nr, nx] == 0 && !bfsVisited4[nr, nx])
                            {
                                bfsVisited4[nr, nx] = true;
                                bfsQ4.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
                Assert(reached4 == walkableL4, $"Level 4 BFS: 100% of walkable cells reached from Start ({reached4}/{walkableL4})");

                // 9c. Verify Level 4 Spawn Point
                Assert(Vector3.Distance(playerObj.transform.position, new Vector3(7.5f, 2.5f, 0f)) < 0.2f, "Player correctly spawned at Level 4 SpawnPoint (7.5, 2.5)");

                // 9d. Verify Enemy Guard
                var enemies4 = grid4.GetComponentsInChildren<EnemyCharacter>();
                Assert(enemies4.Length == 1, $"TEST 6 (Level 4): Tepat 1 Enemy di Level 4 (Actual: {enemies4.Length})");
                bool l4StartBlocked = false;
                bool l4GoalBlocked = false;
                foreach (var en in enemies4)
                {
                    int ex = Mathf.FloorToInt(en.transform.position.x);
                    int ey = Mathf.FloorToInt(en.transform.position.y);
                    Assert(MazeMatrixRegistry.Level4Matrix[15 - ey, ex] == 0, $"Enemy at ({ex}, {ey}) is on walkable path");
                    if (Mathf.Abs(en.transform.position.x - 7.5f) < 0.5f && Mathf.Abs(en.transform.position.y - 2.5f) < 0.5f) l4StartBlocked = true;
                    if (Mathf.Abs(en.transform.position.x - 17.5f) < 0.5f && Mathf.Abs(en.transform.position.y - 4.5f) < 0.5f) l4GoalBlocked = true;
                }
                Assert(!l4StartBlocked, "Spawn porch is clear: Player can exit start freely");
                Assert(!l4GoalBlocked, "Goal doorway is clear: Player can reach goal freely");
                TestEnemyFull(4, enemies4[0], playerObj);

                // 9e. Collect Letters F - R - O - S - T
                var letters4 = grid4.GetComponentsInChildren<CollectibleLetter>();
                Assert(letters4.Length == 5, $"Level 4 has exactly 5 CollectibleLetters (Found {letters4.Length})");
                Dictionary<char, CollectibleLetter> letterMap4 = new Dictionary<char, CollectibleLetter>();
                foreach (var cl in letters4) { letterMap4[cl.Letter] = cl; }
                Assert(letterMap4.ContainsKey('F'), "Letter F exists in Level 4");
                Assert(letterMap4.ContainsKey('R'), "Letter R exists in Level 4");
                Assert(letterMap4.ContainsKey('O'), "Letter O exists in Level 4");
                Assert(letterMap4.ContainsKey('S'), "Letter S exists in Level 4");
                Assert(letterMap4.ContainsKey('T'), "Letter T exists in Level 4");

                foreach (var cl in letters4)
                {
                    int lx = Mathf.FloorToInt(cl.transform.position.x);
                    int ly = Mathf.FloorToInt(cl.transform.position.y);
                    Assert(MazeMatrixRegistry.Level4Matrix[15 - ly, lx] == 0, $"Letter {cl.Letter} is placed on walkable path ({lx}, {ly})");
                    foreach (var en in enemies4)
                    {
                        Assert(Vector3.Distance(cl.transform.position, en.transform.position) > 0.6f, $"Letter {cl.Letter} is not blocked directly by Enemy");
                    }
                    cl.Collect();
                }
                Assert(wordManager.IsWordComplete, "Word FROST is complete!");
                Assert(wordManager.GetProgressString() == "F R O S T", $"Progress string is 'F R O S T' (Actual: {wordManager.GetProgressString()})");

                // 9f. Goal unlocked and transition to Level 5
                var goalTr4 = grid4.transform.Find("GameplayObjects/Goal");
                Assert(goalTr4 != null, "Goal object exists in Level 4");
                Assert(Vector3.Distance(goalTr4.position, new Vector3(17.5f, 4.5f, 0f)) < 0.2f, "Goal positioned at exit (17.5, 4.5)");
                var goalTrigger4 = goalTr4.GetComponent<LevelGoalTrigger>();
                triggerEnterMethod.Invoke(goalTrigger4, new object[] { playerCol });
                Assert(progression.CurrentLevel == 5, $"Touching Goal 4 transitions to Level 5 (Actual: {progression.CurrentLevel})");

                // 10. Test Level 5 (FLAME - Mansion Layout & Game Completion)
                Assert(wordManager.CurrentLevelNumber == 5, "WordCollectionManager synchronized to Level 5");
                Assert(wordManager.CurrentTargetWord == "FLAME", $"Level 5 Target Word is FLAME (Actual: {wordManager.CurrentTargetWord})");
                Assert(wordManager.TotalRequired == 5, "Level 5 required letters count is 5");
                GameObject grid5 = GameObject.Find("Level5_MazeGrid");
                Assert(grid5 != null, "Level5_MazeGrid instantiated in scene");

                // 10b. Verify Level 5 Dimensions & BFS Pathfinding
                Assert(progression.CurrentData != null && progression.CurrentData.columns == 22 && progression.CurrentData.rows == 16, "Level 5 Grid is 22x16 Cartoon House");

                int l5Rows = 16;
                int l5Cols = 22;
                int walkableL5 = 0;
                for (int r = 0; r < l5Rows; r++)
                    for (int c = 0; c < l5Cols; c++)
                        if (MazeMatrixRegistry.Level5Matrix[r, c] == 0) walkableL5++;

                bool[,] bfsVisited5 = new bool[l5Rows, l5Cols];
                Queue<Vector2Int> bfsQ5 = new Queue<Vector2Int>();
                Vector2Int bfsStart5 = new Vector2Int(9, 2);
                bfsVisited5[15 - bfsStart5.y, bfsStart5.x] = true;
                bfsQ5.Enqueue(bfsStart5);
                int reached5 = 0;
                while (bfsQ5.Count > 0)
                {
                    Vector2Int cur = bfsQ5.Dequeue();
                    reached5++;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cur.x + bdx[i];
                        int ny = cur.y + bdy[i];
                        if (nx >= 0 && nx < l5Cols && ny >= 0 && ny < l5Rows)
                        {
                            int nr = 15 - ny;
                            if (MazeMatrixRegistry.Level5Matrix[nr, nx] == 0 && !bfsVisited5[nr, nx])
                            {
                                bfsVisited5[nr, nx] = true;
                                bfsQ5.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
                Assert(reached5 == walkableL5, $"Level 5 BFS: 100% of walkable cells reached from Start ({reached5}/{walkableL5})");

                // 10c. Verify Level 5 Spawn Point
                Assert(Vector3.Distance(playerObj.transform.position, new Vector3(9.5f, 2.5f, 0f)) < 0.2f, "Player correctly spawned at Level 5 SpawnPoint (9.5, 2.5)");

                // 10d. Verify Level 5 Enemies (Patrol Enemy & Final Guardian)
                var enemies5 = grid5.GetComponentsInChildren<EnemyCharacter>();
                Assert(enemies5.Length >= 1, $"Level 5 has Enemies (Actual: {enemies5.Length})");
                bool l5StartBlocked = false;
                bool l5GoalBlocked = false;
                foreach (var en in enemies5)
                {
                    int ex = Mathf.FloorToInt(en.transform.position.x);
                    int ey = Mathf.FloorToInt(en.transform.position.y);
                    Assert(MazeMatrixRegistry.Level5Matrix[15 - ey, ex] == 0, $"Enemy at ({ex}, {ey}) is on walkable path");
                    if (Mathf.Abs(en.transform.position.x - 9.5f) < 0.5f && Mathf.Abs(en.transform.position.y - 2.5f) < 0.5f) l5StartBlocked = true;
                    if (Mathf.Abs(en.transform.position.x - 20.5f) < 0.5f && Mathf.Abs(en.transform.position.y - 4.5f) < 0.5f) l5GoalBlocked = true;
                }
                Assert(!l5StartBlocked, "Level 5 Spawn is clear: Player can exit start freely");
                Assert(!l5GoalBlocked, "Level 5 Goal doorway is clear: Player can reach goal freely");
                TestEnemyFull(5, enemies5[0], playerObj);

                // 10e. Collect Letters F - L - A - M - E
                var letters5 = grid5.GetComponentsInChildren<CollectibleLetter>();
                Assert(letters5.Length == 5, $"Level 5 has exactly 5 CollectibleLetters (Found {letters5.Length})");
                Dictionary<char, CollectibleLetter> letterMap5 = new Dictionary<char, CollectibleLetter>();
                foreach (var cl in letters5) { letterMap5[cl.Letter] = cl; }
                Assert(letterMap5.ContainsKey('F'), "Letter F exists in Level 5");
                Assert(letterMap5.ContainsKey('L'), "Letter L exists in Level 5");
                Assert(letterMap5.ContainsKey('A'), "Letter A exists in Level 5");
                Assert(letterMap5.ContainsKey('M'), "Letter M exists in Level 5");
                Assert(letterMap5.ContainsKey('E'), "Letter E exists in Level 5");

                foreach (var cl in letters5)
                {
                    int lx = Mathf.FloorToInt(cl.transform.position.x);
                    int ly = Mathf.FloorToInt(cl.transform.position.y);
                    Assert(MazeMatrixRegistry.Level5Matrix[15 - ly, lx] == 0, $"Letter {cl.Letter} is placed on walkable path ({lx}, {ly})");
                    foreach (var en in enemies5)
                    {
                        Assert(Vector3.Distance(cl.transform.position, en.transform.position) > 0.6f, $"Letter {cl.Letter} is not blocked directly by Enemy");
                    }
                    cl.Collect();
                }
                Assert(wordManager.IsWordComplete, "Word FLAME is complete!");
                Assert(wordManager.GetProgressString() == "F L A M E", $"Progress string is 'F L A M E' (Actual: {wordManager.GetProgressString()})");

                // 10f. Level 5 Goal & Boss Defeat Verification
                var goalTr5 = grid5.transform.Find("GameplayObjects/Goal");
                Assert(goalTr5 != null, "Goal object exists in Level 5");
                Assert(Vector3.Distance(goalTr5.position, new Vector3(20.5f, 4.5f, 0f)) < 0.2f, "Goal positioned at exit (20.5, 4.5)");
                var goalTrigger5 = goalTr5.GetComponent<LevelGoalTrigger>();

                // Verify Goal 5 is locked while Boss is alive
                triggerEnterMethod.Invoke(goalTrigger5, new object[] { playerCol });
                Assert(!progression.IsGameCompleted, "Level 5 Goal is locked until Final Guardian is defeated");

                // Defeat boss and verify Goal unlocks and completes the game
                if (FinalBossBattleManager.Instance != null)
                {
                    FinalBossBattleManager.Instance.SetBossDefeated(true);
                }
                triggerEnterMethod.Invoke(goalTrigger5, new object[] { playerCol });
                Assert(progression.IsGameCompleted, "Game is marked as COMPLETED after Level 5 Goal with Boss defeated!");

                // 11. Test Hazard Obstacle Does NOT deal damage
                var hazardTestObj = new GameObject("HazardTest");
                var hazardCol = hazardTestObj.AddComponent<BoxCollider2D>();
                hazardCol.isTrigger = true;
                var hazardComp = hazardTestObj.AddComponent<HazardObstacle>();
                Assert(!hazardComp.DealsDamage, "HazardObstacle has DealsDamage = false (Damage disabled in favor of Enemy)");
                livesManager.ResetLives();
                Assert(livesManager.CurrentLives == 3, "Lives is 3 before hazard test");
                var hazardMethod = typeof(HazardObstacle).GetMethod("CheckAndDamagePlayer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                hazardMethod?.Invoke(hazardComp, new object[] { playerObj });
                Assert(livesManager.CurrentLives == 3, "Hazard does NOT reduce player lives (Lives remains 3)");
                UnityEngine.Object.DestroyImmediate(hazardTestObj);

                // 12. Test Player Lives Progression & Game Over
                livesManager.ResetLives();
                Assert(livesManager.CurrentLives == 3, "Lives reset to 3");
                livesManager.TakeDamage(1, ignoreInvulnerable: true);
                Assert(livesManager.CurrentLives == 2, "Lives reduced to 2 after 1 damage");
                livesManager.TakeDamage(1, ignoreInvulnerable: true);
                Assert(livesManager.CurrentLives == 1, "Lives reduced to 1 after 1 damage");
                livesManager.TakeDamage(1, ignoreInvulnerable: true);
                Assert(livesManager.CurrentLives == 0, "Lives reduced to 0");
                Assert(livesManager.IsGameOver, "Game Over triggered when lives reach 0");
                livesManager.ResetLives();
                Assert(livesManager.CurrentLives == 3, "Lives reset to 3 after Game Over");
                Assert(!livesManager.IsGameOver, "Game Over reset");

                // Clean up runtime test objects
                progression.RestartGame();

                Debug.Log("=================================================");
                Debug.Log($">>> VERIFICATION RESULT: {passedTests}/{totalTests} TESTS PASSED ({failedTests} FAILED) <<<");
                Debug.Log("=================================================");

                if (failedTests == 0)
                {
                    Debug.Log(">>> [SUCCESS] ALL GAMEPLAY FLOWS VERIFIED 100% CORRECT! <<<");
                }
                else
                {
                    Debug.LogError($">>> [FAILURE] {failedTests} TESTS FAILED! <<<");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FATAL TEST RUNNER EXCEPTION]: {ex}");
            }
        }
    }
}
