using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PastGuardians.Core
{
    /// <summary>
    /// Removes unwanted AR Foundation sample UI elements
    /// Runs automatically at startup
    /// </summary>
    public class RemoveUnwantedUI : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RemoveUnwantedElements()
        {
            // List of UI elements to remove (by name or partial name)
            string[] unwantedNames = new string[]
            {
                "Tap to Place",
                "TapToPlace",
                "PlaceOnPlane",
                "Place On Plane",
                "ARPlacement",
                "PlacementIndicator",
                "Placement Indicator",
                "PlacementUI",
                "AR Debug",
                "ARDebug",
                "DebugUI",
                "SampleUI",
                "Sample UI",
                "DefaultUI",
                "AR UI",
                "ARUI",
                // Object spawner / debug menu
                "ObjectSpawner",
                "Object Spawner",
                "SpawnMenu",
                "Spawn Menu",
                "DebugMenu",
                "Debug Menu",
                "ObjectMenu",
                "Object Menu",
                "PrimitiveMenu",
                "Primitive Menu",
                "ShapeMenu",
                "Shape Menu",
                "ARMenu",
                "AR Menu",
                "XRMenu",
                "XR Menu",
                "InteractionMenu",
                "Interaction Menu",
                "XRI",
                "MenuCanvas",
                "Menu Canvas",
                "OptionsMenu",
                "Options Menu",
                "ToolMenu",
                "Tool Menu",
                "CreateMenu",
                "Create Menu",
                "SpawnablesMenu",
                "Spawnables",
                "ObjectPanel",
                "Object Panel",
                "DebugPanel",
                "Debug Panel",
                "XRDebug",
                "XR Debug"
            };

            // Find and destroy unwanted objects
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);

            foreach (GameObject obj in allObjects)
            {
                // SKIP essential AR components
                if (IsEssentialObject(obj)) continue;

                string objName = obj.name.ToLower();

                foreach (string unwanted in unwantedNames)
                {
                    if (objName.Contains(unwanted.ToLower()))
                    {
                        Debug.Log($"[RemoveUnwantedUI] Removing: {obj.name}");
                        Object.Destroy(obj);
                        break;
                    }
                }
            }

            // Also find buttons with specific text
            Button[] allButtons = GameObject.FindObjectsOfType<Button>(true);
            foreach (Button btn in allButtons)
            {
                // Check for text components
                Text legacyText = btn.GetComponentInChildren<Text>(true);
                TextMeshProUGUI tmpText = btn.GetComponentInChildren<TextMeshProUGUI>(true);

                string buttonText = "";
                if (legacyText != null) buttonText = legacyText.text.ToLower();
                if (tmpText != null) buttonText = tmpText.text.ToLower();

                if (buttonText.Contains("tap to place") ||
                    buttonText.Contains("place") ||
                    buttonText.Contains("spawn"))
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing button: {btn.name} ({buttonText})");
                    Object.Destroy(btn.gameObject);
                }
            }

            // Find any circle/ring indicators (placement indicators)
            SpriteRenderer[] sprites = GameObject.FindObjectsOfType<SpriteRenderer>(true);
            foreach (SpriteRenderer sprite in sprites)
            {
                string spriteName = sprite.name.ToLower();
                if (spriteName.Contains("indicator") ||
                    spriteName.Contains("placement") ||
                    spriteName.Contains("reticle") ||
                    spriteName.Contains("cursor"))
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing sprite: {sprite.name}");
                    Object.Destroy(sprite.gameObject);
                }
            }

            // Remove any Image components that look like placement indicators
            Image[] images = GameObject.FindObjectsOfType<Image>(true);
            foreach (Image img in images)
            {
                string imgName = img.name.ToLower();
                if (imgName.Contains("indicator") ||
                    imgName.Contains("placement") ||
                    imgName.Contains("reticle") ||
                    imgName.Contains("cursor") ||
                    imgName.Contains("crosshair"))
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing image: {img.name}");
                    Object.Destroy(img.gameObject);
                }
            }

            // Remove any toggle buttons (circular buttons that open menus)
            Toggle[] toggles = GameObject.FindObjectsOfType<Toggle>(true);
            foreach (Toggle toggle in toggles)
            {
                // Remove toggles that aren't part of our UI
                if (toggle.GetComponentInParent<PastGuardians.UI.UIAutoSetup>() == null)
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing toggle: {toggle.name}");
                    Object.Destroy(toggle.gameObject);
                }
            }

            // Find circular button at corners (likely debug menu toggle)
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                // Skip our main canvas
                if (canvas.name.Contains("Main") || canvas.name.Contains("Game") || canvas.name.Contains("UIAutoSetup"))
                    continue;

                // Check if this canvas has suspicious content
                bool hasShapeButtons = false;
                Button[] buttons = canvas.GetComponentsInChildren<Button>(true);

                foreach (Button btn in buttons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("cube") || btnName.Contains("sphere") ||
                        btnName.Contains("pyramid") || btnName.Contains("cylinder") ||
                        btnName.Contains("ring") || btnName.Contains("torus") ||
                        btnName.Contains("capsule") || btnName.Contains("plane"))
                    {
                        hasShapeButtons = true;
                        break;
                    }
                }

                if (hasShapeButtons)
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing shape spawner canvas: {canvas.name}");
                    Object.Destroy(canvas.gameObject);
                }
            }

            // Nuclear option: Remove any small circular buttons in corners
            Button[] allBtns = GameObject.FindObjectsOfType<Button>(true);
            foreach (Button btn in allBtns)
            {
                RectTransform rect = btn.GetComponent<RectTransform>();
                if (rect == null) continue;

                // Check if button is small and in a corner (likely debug toggle)
                Vector2 size = rect.sizeDelta;
                Vector2 anchorMin = rect.anchorMin;
                Vector2 anchorMax = rect.anchorMax;

                bool isSmall = size.x < 100 && size.y < 100 && size.x > 0 && size.y > 0;
                bool isSquarish = Mathf.Abs(size.x - size.y) < 20;
                bool isInCorner = (anchorMin.x < 0.2f || anchorMin.x > 0.8f) &&
                                  (anchorMin.y < 0.2f || anchorMin.y > 0.8f);

                // Check if it has a dark/black background
                Image img = btn.GetComponent<Image>();
                bool isDark = img != null && img.color.r < 0.3f && img.color.g < 0.3f && img.color.b < 0.3f;

                if (isSmall && isSquarish && isDark)
                {
                    Debug.Log($"[RemoveUnwantedUI] Removing suspicious dark button: {btn.name}");
                    Object.Destroy(btn.gameObject);
                }
            }

            Debug.Log("[RemoveUnwantedUI] Cleanup complete");
        }

        /// <summary>
        /// Check if object is essential and should NOT be removed
        /// </summary>
        private static bool IsEssentialObject(GameObject obj)
        {
            if (obj == null) return true;

            string name = obj.name.ToLower();

            // Keep AR essential objects
            if (name.Contains("ar session") || name.Contains("arsession") ||
                name.Contains("xr origin") || name.Contains("xrorigin") ||
                name.Contains("camera") || name.Contains("trackable"))
            {
                return true;
            }

            // Keep if has camera or AR components
            if (obj.GetComponent<Camera>() != null ||
                obj.GetComponent<UnityEngine.XR.ARFoundation.ARSession>() != null ||
                obj.GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>() != null)
            {
                return true;
            }

            return false;
        }
    }
}
