using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace PastGuardians.Core
{
    /// <summary>
    /// Removes AR Foundation Sample UI elements that we don't need
    /// These include: Tap to Place, Object Spawner, Debug Menu, Shape Picker
    /// </summary>
    public class RemoveARFoundationSamples : MonoBehaviour
    {
        private static bool hasRun = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (hasRun) return;
            hasRun = true;

            // Create a temporary object to run coroutine
            GameObject obj = new GameObject("ARSampleRemover");
            var remover = obj.AddComponent<RemoveARFoundationSamples>();
            remover.StartCoroutine(remover.RemoveAllUnwantedUI());
        }

        private IEnumerator RemoveAllUnwantedUI()
        {
            // Wait a frame for everything to initialize
            yield return null;
            yield return null;

            Debug.Log("[RemoveARFoundationSamples] Starting cleanup...");

            // 1. Remove the "Tap to Place" coaching overlay
            RemoveByExactNames(new string[] {
                "Tap to Place",
                "TapToPlace",
                "PlacementUI",
                "CoachingOverlay",
                "Coaching Overlay",
                "ARCoachingOverlay",
                "PlacementCoaching"
            });

            // 2. Remove the Object Spawner button (black circle with cube)
            RemoveByExactNames(new string[] {
                "Object Spawner",
                "ObjectSpawner",
                "SpawnerButton",
                "Spawner Button",
                "ObjectButton",
                "Object Button"
            });

            // 3. Remove the Shape Picker / Object Menu bar
            RemoveByExactNames(new string[] {
                "Object Menu",
                "ObjectMenu",
                "ShapeMenu",
                "Shape Menu",
                "SpawnableObjects",
                "Spawnable Objects",
                "ObjectList",
                "Object List",
                "ShapePicker",
                "Shape Picker"
            });

            // 4. Remove the "..." options menu button (top right)
            RemoveByExactNames(new string[] {
                "Options",
                "OptionsButton",
                "Options Button",
                "MoreOptions",
                "More Options",
                "MenuButton",
                "Menu Button",
                "SettingsButton",
                "Settings Button",
                "Hamburger",
                "HamburgerMenu",
                "ThreeDots",
                "DotsButton"
            });

            // 5. Remove the Debug Menu panel
            RemoveByExactNames(new string[] {
                "Debug Menu",
                "DebugMenu",
                "AR Debug Menu",
                "ARDebugMenu",
                "DebugPanel",
                "Debug Panel",
                "InteractionHints",
                "Interaction Hints"
            });

            // 6. Find and destroy components by type name
            DestroyComponentsByTypeName(new string[] {
                "ObjectSpawner",
                "ARPlacementInteractable",
                "PlacementInteractable",
                "ARDebugMenu",
                "DebugMenu",
                "CoachingOverlay",
                "ARCoachingOverlay",
                "InteractionHints",
                "SurfaceVisualizer"
            });

            // 7. Remove any Canvas that isn't ours
            RemoveNonGameCanvases();

            // 8. Find buttons/toggles with specific text
            RemoveByTextContent(new string[] {
                "Tap to Place",
                "Touch surfaces",
                "Visualize Surfaces",
                "Show Interaction",
                "Remove All Objects",
                "AR Debug",
                "Cancel"
            });

            // 9. Remove the black circular button specifically
            RemoveCircularSpawnerButton();

            // 10. Remove plane visualization
            DisablePlaneVisualization();

            Debug.Log("[RemoveARFoundationSamples] Cleanup complete!");

            // Destroy this helper object
            Destroy(gameObject);
        }

        private void RemoveByExactNames(string[] names)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

            foreach (GameObject obj in allObjects)
            {
                // NEVER remove essential AR components
                if (IsEssentialARObject(obj)) continue;

                foreach (string name in names)
                {
                    if (obj.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                        obj.name.Replace(" ", "").Equals(name.Replace(" ", ""), System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[RemoveARFoundationSamples] Removing: {obj.name}");
                        Destroy(obj);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check if this is an essential AR object that should NOT be removed
        /// </summary>
        private bool IsEssentialARObject(GameObject obj)
        {
            if (obj == null) return true;

            string objName = obj.name.ToLower();

            // Essential names to keep
            if (objName.Contains("ar session") ||
                objName.Contains("arsession") ||
                objName.Contains("xr origin") ||
                objName.Contains("xrorigin") ||
                objName.Contains("ar camera") ||
                objName.Contains("arcamera") ||
                objName.Contains("main camera") ||
                objName.Contains("maincamera") ||
                objName.Contains("camera offset") ||
                objName.Contains("cameraoffset") ||
                objName.Contains("trackablesparent") ||
                objName.Contains("trackables") ||
                objName.Equals("camera"))
            {
                return true;
            }

            // Check for essential components
            if (obj.GetComponent<UnityEngine.XR.ARFoundation.ARSession>() != null ||
                obj.GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>() != null ||
                obj.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>() != null ||
                obj.GetComponent<Camera>() != null)
            {
                return true;
            }

            // Check for XR Origin component
            var xrOrigin = obj.GetComponent("XROrigin") ?? obj.GetComponent("Unity.XR.CoreUtils.XROrigin");
            if (xrOrigin != null) return true;

            // Check parent - don't remove children of essential objects
            if (obj.transform.parent != null)
            {
                return IsEssentialARObject(obj.transform.parent.gameObject);
            }

            return false;
        }

        private void DestroyComponentsByTypeName(string[] typeNames)
        {
            MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>(true);

            foreach (MonoBehaviour comp in allComponents)
            {
                // Don't touch essential AR objects
                if (IsEssentialARObject(comp.gameObject)) continue;

                string typeName = comp.GetType().Name;
                foreach (string name in typeNames)
                {
                    if (typeName.Contains(name))
                    {
                        Debug.Log($"[RemoveARFoundationSamples] Destroying component: {typeName} on {comp.gameObject.name}");
                        Destroy(comp.gameObject);
                        break;
                    }
                }
            }
        }

        private void RemoveNonGameCanvases()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);

            foreach (Canvas canvas in canvases)
            {
                // Don't touch essential AR objects
                if (IsEssentialARObject(canvas.gameObject)) continue;

                string canvasName = canvas.name.ToLower();

                // Keep our canvases
                if (canvasName.Contains("main") ||
                    canvasName.Contains("game") ||
                    canvasName.Contains("uiautosetup") ||
                    canvasName.Contains("hud") ||
                    canvasName.Contains("navigation") ||
                    canvasName.Contains("pastguardians"))
                {
                    continue;
                }

                // Check if it has AR sample content
                bool hasARSampleContent = false;

                // Check for shape buttons
                Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("cube") || btnName.Contains("sphere") ||
                        btnName.Contains("pyramid") || btnName.Contains("cylinder") ||
                        btnName.Contains("capsule") || btnName.Contains("torus") ||
                        btnName.Contains("ring") || btnName.Contains("spawner") ||
                        btnName.Contains("option") || btnName.Contains("debug"))
                    {
                        hasARSampleContent = true;
                        break;
                    }
                }

                // Check for toggle switches
                Toggle[] toggles = canvas.GetComponentsInChildren<Toggle>(true);
                if (toggles.Length > 0)
                {
                    hasARSampleContent = true;
                }

                // Check text content
                TMPro.TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    string content = text.text.ToLower();
                    if (content.Contains("tap to place") || content.Contains("visualize") ||
                        content.Contains("interaction hints") || content.Contains("debug menu") ||
                        content.Contains("remove all objects") || content.Contains("touch surfaces"))
                    {
                        hasARSampleContent = true;
                        break;
                    }
                }

                if (hasARSampleContent)
                {
                    Debug.Log($"[RemoveARFoundationSamples] Removing AR sample canvas: {canvas.name}");
                    Destroy(canvas.gameObject);
                }
            }
        }

        private void RemoveByTextContent(string[] textContents)
        {
            // Check TextMeshPro
            TMPro.TextMeshProUGUI[] tmpTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                foreach (string content in textContents)
                {
                    if (tmp.text.ToLower().Contains(content.ToLower()))
                    {
                        // Find the root UI element (parent with Canvas or top-level)
                        Transform root = tmp.transform;
                        while (root.parent != null && root.parent.GetComponent<Canvas>() == null)
                        {
                            root = root.parent;
                        }

                        // Don't destroy our navigation bar
                        if (root.name.ToLower().Contains("nav") ||
                            root.name.ToLower().Contains("bottom") ||
                            root.name.ToLower().Contains("play") ||
                            root.name.ToLower().Contains("codex") ||
                            root.name.ToLower().Contains("shop") ||
                            root.name.ToLower().Contains("profile"))
                        {
                            continue;
                        }

                        Debug.Log($"[RemoveARFoundationSamples] Removing UI with text '{content}': {root.name}");
                        Destroy(root.gameObject);
                        break;
                    }
                }
            }

            // Check legacy Text
            Text[] legacyTexts = FindObjectsOfType<Text>(true);
            foreach (var txt in legacyTexts)
            {
                foreach (string content in textContents)
                {
                    if (txt.text.ToLower().Contains(content.ToLower()))
                    {
                        Transform root = txt.transform;
                        while (root.parent != null && root.parent.GetComponent<Canvas>() == null)
                        {
                            root = root.parent;
                        }

                        Debug.Log($"[RemoveARFoundationSamples] Removing legacy UI with text '{content}': {root.name}");
                        Destroy(root.gameObject);
                        break;
                    }
                }
            }
        }

        private void RemoveCircularSpawnerButton()
        {
            // Find the black circular button at the bottom
            Button[] buttons = FindObjectsOfType<Button>(true);

            foreach (Button btn in buttons)
            {
                // Don't touch essential AR objects
                if (IsEssentialARObject(btn.gameObject)) continue;

                RectTransform rect = btn.GetComponent<RectTransform>();
                if (rect == null) continue;

                Image img = btn.GetComponent<Image>();
                if (img == null) continue;

                // Check if it's dark/black
                bool isDark = img.color.r < 0.3f && img.color.g < 0.3f && img.color.b < 0.3f;

                // Check if it's roughly square/circular
                Vector2 size = rect.sizeDelta;
                bool isSquarish = size.x > 0 && size.y > 0 && Mathf.Abs(size.x - size.y) < 30;

                // Check if it's at the bottom of screen
                bool isAtBottom = rect.anchorMin.y < 0.3f;

                // Check if it contains a 3D object icon (has child with Image)
                bool hasIconChild = false;
                foreach (Transform child in btn.transform)
                {
                    if (child.GetComponent<Image>() != null || child.GetComponent<RawImage>() != null)
                    {
                        hasIconChild = true;
                        break;
                    }
                }

                if (isDark && isSquarish && isAtBottom && hasIconChild)
                {
                    Debug.Log($"[RemoveARFoundationSamples] Removing circular spawner button: {btn.name}");
                    Destroy(btn.gameObject);
                }
            }
        }

        private void DisablePlaneVisualization()
        {
            // Find and disable ARPlaneManager if present
            var planeManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
            if (planeManager != null)
            {
                planeManager.enabled = false;
                Debug.Log("[RemoveARFoundationSamples] Disabled ARPlaneManager");
            }

            // Find and destroy plane visualizers
            var planes = FindObjectsOfType<UnityEngine.XR.ARFoundation.ARPlane>(true);
            foreach (var plane in planes)
            {
                Destroy(plane.gameObject);
            }
        }
    }
}
