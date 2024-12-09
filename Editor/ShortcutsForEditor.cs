using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plugins._EditorTools.Editor
{
    public class ShortcutsForEditor : EditorWindow
    {
        private static bool _isGameWindowMaximized;
        private static bool _isProjectWindowLocked;


        
        [MenuItem("Tools/Shortcuts/Toggle Layer UI Visible _&U")]
        private static void ToggleLayerVisibility()
        {
            string layerName = "UI";
            
            int layer = LayerMask.NameToLayer(layerName);

            if (layer == -1)
            {
                Debug.LogError($"Layer \"{layerName}\" nu există!");
                return;
            }

            // Obține masca curentă a vizibilității layer-urilor
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            Tools.visibleLayers ^= (1 << layer); // Comută vizibilitatea layer-ului
            SceneView.RepaintAll(); // Actualizează vizualizarea Scene
        }
        

        // Toggle maximize Game Window
        [MenuItem("Tools/Shortcuts/Toggle Maximize Game Window _&F")] // Shortcut: Alt + F
        private static void ToggleMaximizeGameWindow()
        {
            _isGameWindowMaximized = !_isGameWindowMaximized;

            EditorApplication.ExecuteMenuItem("Window/General/Game");

            var gameWindow = EditorWindow.focusedWindow;
            if (gameWindow != null)
            {
                gameWindow.maximized = _isGameWindowMaximized;
            }
        }


        [MenuItem("Tools/Shortcuts/Toggle Lock _&q")] // Alt + Q
        public static void ToggleInspectorLock()
        {
            // Obține Inspectorul activ
            var inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);

            if (inspectorWindow == null)
            {
                Debug.LogWarning("Nu s-a găsit fereastra Inspector.");
                return;
            }

            // Folosește reflecția pentru a accesa câmpul `isLocked`
            var isLockedProperty = inspectorWindowType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            if (isLockedProperty != null)
            {
                // Toggle Lock
                bool isLocked = (bool)isLockedProperty.GetValue(inspectorWindow, null);
                isLockedProperty.SetValue(inspectorWindow, !isLocked, null);
                inspectorWindow.Repaint();
            }
            else
            {
                Debug.LogWarning("Nu s-a putut accesa proprietatea isLocked.");
            }
        }


        // Toggle lock on Project Window
        [MenuItem("Tools/Shortcuts/Toggle Project Window Lock _&W")] // Shortcut: Alt + W
        private static void ToggleProjectWindowLock()
        {
            // Get the Project Window instance
            var projectWindow = GetProjectWindow();
            if (projectWindow == null) return;

            // Use reflection to toggle the lock property
            var isLockedProperty = projectWindow.GetType().GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isLockedProperty != null)
            {
                _isProjectWindowLocked = !_isProjectWindowLocked;
                isLockedProperty.SetValue(projectWindow, _isProjectWindowLocked);
                projectWindow.Repaint(); // Refresh the Project Window
            }
        }

        
        // Execute Assets > Properties using Alt + E
        [MenuItem("Tools/Shortcuts/Show Properties _&E")] // Shortcut: Alt + E
        private static void ShowProperties()
        {
            // Execute "Assets > Properties" from the main menu
            EditorApplication.ExecuteMenuItem("Assets/Properties...");
        }
        
        
        
        
        
    private static bool toggleLayout;

    // Încarcă layout-ul specificat prin combinația de taste Alt + A
    [MenuItem("Tools/Shortcuts/Switch Between Two Layout _&S")]
    private static void SwitchToCustomLayout()
    {
        // Dacă toggle este fals, atunci salvăm layout-ul curent înainte de a comuta la animation layout
        if (!toggleLayout)
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/Save Layout...");
            SimulateEnterKey();

            EditorApplication.ExecuteMenuItem("Window/Layouts/Tall");
        }
        else
        {
            EditorApplication.ExecuteMenuItem("Window/Layouts/Save Layout...");
            SimulateEnterKey();
            
            EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
        }

        // Schimbăm starea toggle-ului pentru a alterna între cele două layout-uri
        toggleLayout = !toggleLayout;
    }

    private static void SimulateEnterKey()
    {
        // Creăm un eveniment de tastatură de tip Enter
        Event enterEvent = new Event
        {
            type = EventType.KeyDown,
            keyCode = KeyCode.Return // Return este pentru Enter
        };

        // Trimitem evenimentul la fereastra activă
        EditorWindow.focusedWindow.SendEvent(enterEvent);

        Debug.Log("Am trimis evenimentul Enter către fereastra activă.");
    }
    
        
        
        
        
        
        // Focus Scene Window when entering Prefab Mode
        [InitializeOnLoadMethod]
        private static void FocusSceneOnPrefabEdit()
        {
            PrefabStage.prefabStageOpened += _ =>
            {
                EditorApplication.ExecuteMenuItem("Window/General/Scene");
            };
        }
        

        // Helper function to get the Project Window instance
        private static EditorWindow GetProjectWindow()
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
            return EditorWindow.GetWindow(type);
        }
    }
}
