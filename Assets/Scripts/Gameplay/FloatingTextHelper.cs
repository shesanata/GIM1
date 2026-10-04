using System.Collections;
using UnityEngine;
using TMPro;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Helper serbaguna untuk menampilkan floating text feedback:
    /// "+ BONUS ⭐", "SECRET FOUND!", "-1 HP", "+1 NYAWA", "OUCH!"
    /// Menggunakan animasi pop scale, float upward perlahan, dan fade out lembut.
    /// </summary>
    public class FloatingTextHelper : MonoBehaviour
    {
        public static void Show(string text, Vector3 worldPosition, Color color, float fontSize = 3.2f)
        {
            GameObject go = new GameObject("FloatingTextWorld");
            go.transform.position = worldPosition;

            var helper = go.AddComponent<FloatingTextHelper>();
            helper.StartCoroutine(helper.AnimateWorldText(text, worldPosition, color, fontSize));
        }

        public static void ShowFloatingText(Vector3 worldPosition, string text, Color color, float fontSize = 3.2f)
        {
            Show(text, worldPosition, color, fontSize);
        }

        private IEnumerator AnimateWorldText(string text, Vector3 startPos, Color color, float fontSize)
        {
            TextMeshPro tmp = gameObject.AddComponent<TextMeshPro>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.sortingOrder = 30;

            float duration = 0.95f;
            float elapsed = 0f;
            Vector3 targetPos = startPos + Vector3.up * 0.85f;
            transform.localScale = Vector3.one * 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Pop scale overshoot
                if (t < 0.25f)
                {
                    float punchT = t / 0.25f;
                    transform.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 1.3f, punchT);
                }
                else if (t < 0.45f)
                {
                    float settleT = (t - 0.25f) / 0.20f;
                    transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, settleT);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }

                // Bergerak perlahan ke atas
                transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

                // Fade out lembut
                if (t > 0.45f)
                {
                    float alphaT = (t - 0.45f) / 0.55f;
                    tmp.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, alphaT));
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
