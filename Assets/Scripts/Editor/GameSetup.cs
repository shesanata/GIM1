using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class GameSetup
{
    static GameSetup()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        // Automatically check if scene needs setup (HANYA untuk SampleScene gameplay)
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != "SampleScene") return;

        var player = GameObject.Find("Player");
        var ground = GameObject.Find("Ground");

        if (player == null || ground == null)
        {
            SetupScene();
        }
    }

    [MenuItem("Game/Setup Character and Ground")]
    public static void SetupScene()
    {
        // 1. Setup Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = new GameObject("Ground");
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }

        ground.transform.position = new Vector3(0f, -3.5f, 0f);
        ground.transform.localScale = new Vector3(16f, 1.5f, 1f);

        SpriteRenderer groundSr = ground.GetComponent<SpriteRenderer>();
        if (groundSr == null) groundSr = ground.AddComponent<SpriteRenderer>();

        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ground.png");
        if (groundSprite != null)
        {
            groundSr.sprite = groundSprite;
        }
        groundSr.sortingOrder = 0;

        BoxCollider2D groundCol = ground.GetComponent<BoxCollider2D>();
        if (groundCol == null) groundCol = ground.AddComponent<BoxCollider2D>();
        groundCol.size = Vector2.one;

        // 2. Setup Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        player.transform.position = new Vector3(0f, 0f, 0f);
        player.transform.localScale = Vector3.one;

        SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
        if (playerSr == null) playerSr = player.AddComponent<SpriteRenderer>();

        Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character.png");
        if (charSprite != null)
        {
            playerSr.sprite = charSprite;
        }
        playerSr.sortingOrder = 5;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        CapsuleCollider2D col = player.GetComponent<CapsuleCollider2D>();
        if (col == null) col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(1.2f, 1.9f);
        col.offset = new Vector2(0f, -0.05f);

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null) controller = player.AddComponent<PlayerController>();

        // 3. Setup Camera framing
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, -0.5f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.24f, 1f);
        }

        // Save current scene
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log("[GameSetup] Scene successfully configured with Player (WASD) and Ground platform!");
        }
    }
}
