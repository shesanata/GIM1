using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class PrologueSceneInspector
    {
        private const string LogPath = "Logs/prologue_inspector.log";

        [MenuItem("MazeSpell/Inspect Prologue Scene Details")]
        public static void InspectDetails()
        {
            var sb = new StringBuilder();
            void Log(string msg)
            {
                Debug.Log(msg);
                sb.AppendLine(msg);
            }

            Log("=== PROLOGUE SCENE INSPECTION ===");
            Log($"Timestamp: {DateTime.Now}");

            // 1. Play Mode Start Scene
            var startScene = EditorSceneManager.playModeStartScene;
            Log($"playModeStartScene: {(startScene != null ? AssetDatabase.GetAssetPath(startScene) : "NULL")}");

            // 2. Build Settings
            var buildScenes = EditorBuildSettings.scenes;
            Log($"Build Settings count: {buildScenes.Length}");
            for (int i = 0; i < buildScenes.Length; i++)
            {
                Log($"  [{i}] {buildScenes[i].path} (enabled={buildScenes[i].enabled})");
            }

            // 3. Open Prologue Scene
            string scenePath = "Assets/Scenes/Prologue.unity";
            if (!File.Exists(scenePath))
            {
                Log($"ERROR: Scene file '{scenePath}' does NOT exist!");
                File.WriteAllText(LogPath, sb.ToString());
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Log($"Loaded scene: {scene.name} (isValid={scene.IsValid()})");

            // 4. Root Objects
            var rootObjs = scene.GetRootGameObjects();
            Log($"Root GameObjects count: {rootObjs.Length}");
            foreach (var r in rootObjs)
            {
                Log($"  Root: {r.name} (active={r.activeSelf})");
            }

            // 5. Main Camera
            var cam = Camera.main;
            if (cam == null)
            {
                Log("ERROR: Camera.main is NULL!");
            }
            else
            {
                Log($"Main Camera: pos={cam.transform.position}, ortho={cam.orthographic}, size={cam.orthographicSize}, clearFlags={cam.clearFlags}, bg={cam.backgroundColor}, cullingMask={cam.cullingMask}, depth={cam.depth}");
            }

            // 6. PrologueCanvas
            var canvasObj = GameObject.Find("PrologueCanvas");
            if (canvasObj == null)
            {
                Log("ERROR: PrologueCanvas GameObject not found!");
            }
            else
            {
                var canvas = canvasObj.GetComponent<Canvas>();
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                Log($"PrologueCanvas: active={canvasObj.activeSelf}, enabled={canvas.enabled}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}");
                if (scaler != null)
                {
                    Log($"CanvasScaler: mode={scaler.uiScaleMode}, refRes={scaler.referenceResolution}");
                }

                for (int i = 0; i < canvasObj.transform.childCount; i++)
                {
                    var ch = canvasObj.transform.GetChild(i);
                    var cg = ch.GetComponent<CanvasGroup>();
                    var img = ch.GetComponent<Image>();
                    var tmp = ch.GetComponent<TextMeshProUGUI>();
                    Log($"  Canvas Child [{i}]: {ch.name} (active={ch.gameObject.activeSelf}, alpha={(cg != null ? cg.alpha.ToString("F2") : "no CG")}, img={(img != null ? img.color.ToString() : "no Img")}, text={(tmp != null ? tmp.text : "no TMP")})");
                }
            }

            // 7. PrologueController
            var controller = UnityEngine.Object.FindAnyObjectByType<PrologueController>();
            if (controller == null)
            {
                Log("ERROR: PrologueController not found!");
            }
            else
            {
                Log($"PrologueController found on: {controller.gameObject.name} (active={controller.gameObject.activeInHierarchy}, enabled={controller.enabled})");
                
                // Inspect fields via SerializedObject
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty p = so.GetIterator();
                p.NextVisible(true);
                while (p.NextVisible(false))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Log($"  Field '{p.name}': {(p.objectReferenceValue != null ? p.objectReferenceValue.name : "NULL!")}");
                    }
                    else if (p.propertyType == SerializedPropertyType.String)
                    {
                        Log($"  Field '{p.name}': \"{p.stringValue}\"");
                    }
                }
            }

            // 8. StoryText details
            var storyObj = GameObject.Find("StoryText");
            if (storyObj != null)
            {
                var rt = storyObj.GetComponent<RectTransform>();
                var tmp = storyObj.GetComponent<TextMeshProUGUI>();
                var cg = storyObj.GetComponent<CanvasGroup>();
                Log($"StoryText: pos={rt.anchoredPosition}, size={rt.sizeDelta}, alpha={(cg != null ? cg.alpha : -1)}, font={(tmp != null && tmp.font != null ? tmp.font.name : "NULL")}, fontSize={(tmp != null ? tmp.fontSize : 0)}, color={(tmp != null ? tmp.color.ToString() : "NULL")}");
            }

            // 9. BlackOverlay details
            var blackObj = GameObject.Find("BlackOverlay");
            if (blackObj != null)
            {
                var rt = blackObj.GetComponent<RectTransform>();
                var img = blackObj.GetComponent<Image>();
                var cg = blackObj.GetComponent<CanvasGroup>();
                Log($"BlackOverlay: anchors=({rt.anchorMin} to {rt.anchorMax}), offsets=({rt.offsetMin} to {rt.offsetMax}), alpha={(cg != null ? cg.alpha : -1)}, color={(img != null ? img.color.ToString() : "NULL")}, raycast={(img != null ? img.raycastTarget : false)}");
            }

            // 10. World Elements
            string[] worldNames = new string[] { "LetterGroup", "MazeReveal", "EnemyReveal", "PlayerReveal", "PlayerLetters", "MagicParticles" };
            foreach (var wn in worldNames)
            {
                var obj = GameObject.Find(wn);
                Log($"World Element '{wn}': found={(obj != null)}, activeInHierarchy={(obj != null && obj.activeInHierarchy)}, pos={(obj != null ? obj.transform.position.ToString() : "N/A")}");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, sb.ToString());
            Log($"[INSPECTION COMPLETE] Log written to {LogPath}");
        }
    }
}
