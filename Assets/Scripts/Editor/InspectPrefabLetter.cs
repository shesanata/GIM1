using UnityEngine;
using UnityEditor;
using TMPro;

namespace MazeSpell.Editor
{
    public static class InspectPrefabLetter
    {
        [MenuItem("MazeSpell/Inspect Prefab Letter")]
        public static void Inspect()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab");
            var clList = prefab.GetComponentsInChildren<CollectibleLetter>(true);
            foreach (var cl in clList)
            {
                if (cl.Letter == 'A')
                {
                    Debug.Log($"=== FOUND LETTER A: {cl.gameObject.name} at {cl.transform.localPosition} ===");
                    Transform vis = cl.transform.Find("Visual");
                    if (vis == null)
                    {
                        Debug.LogError("No Visual found!");
                        continue;
                    }
                    Debug.Log($"Visual childCount = {vis.childCount}");
                    for (int i = 0; i < vis.childCount; i++)
                    {
                        Transform ch = vis.GetChild(i);
                        TextMeshPro tmp = ch.GetComponent<TextMeshPro>();
                        Debug.Log($"Child[{i}]: '{ch.name}' localPos={ch.localPosition} tmp={(tmp != null)}");
                        if (tmp != null)
                        {
                            Debug.Log($"   TMP font={tmp.font?.name} text='{tmp.text}' fontSharedMat={tmp.fontSharedMaterial?.name} color={tmp.color} sortingOrder={tmp.sortingOrder}");
                            if (tmp.fontSharedMaterial != null)
                            {
                                Debug.Log($"   Mat _FaceColor={tmp.fontSharedMaterial.GetColor(ShaderUtilities.ID_FaceColor)} _FaceDilate={tmp.fontSharedMaterial.GetFloat(ShaderUtilities.ID_FaceDilate)}");
                            }
                        }
                    }
                }
            }
        }
    }
}
