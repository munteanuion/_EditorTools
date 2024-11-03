#if UNITY_EDITOR


//Common Unity Editor Window Types:
//"UnityEditor.SceneView": The Scene View window.
//"UnityEditor.GameView": The Game View window.
//"UnityEditor.InspectorWindow": The Inspector window.
//"UnityEditor.ProjectBrowser": The Project window.
//"UnityEditor.ConsoleWindow": The Console window.
//"UnityEditor.HierarchyWindow": The Hierarchy window.
//"UnityEditor.AnimatorControllerTool": The Animator window.
//"UnityEditor.AnimationWindow": The Animation window.
//"UnityEditor.AssetStoreWindow": The Asset Store window.
//"UnityEditor.Graphs.AnimationStateMachine.GraphEditor": The Animator state machine window.
//"UnityEditor.VersionControl.ProjectHooks": Version Control window.
//"UnityEditor.AssetBundleBrowserMain": Asset Bundle Browser window.
//"UnityEditor.NavMeshEditorWindow": Navigation Mesh window.
//"UnityEditor.Timeline.TimelineWindow": The Timeline window.
//"UnityEditor.ProBuilder.EditorMainWindow": ProBuilder window.
//"UnityEditor.TestRunnerWindow": The Test Runner window.
//"UnityEditor.LightingSettingsWindow": Lighting settings window.
//"UnityEditor.SpritePackerWindow": The Sprite Packer window.



using UnityEditor;
using UnityEngine;

public class AdjustWindowsSize : EditorWindow
{
    private const string windowBrowserName = "UnityEditor.ProjectBrowser";



    [MenuItem("Tools/Shortcuts/Toggle Size Project Window #z")] // shift + z
    private static void AdjustProjectProjectWindowSize()
    {
        AdjustProjectWindowSize(windowBrowserName); 
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void AdjustProjectProjectWindowSizeReloadDomain()
    {
        //AdjustProjectWindowSize(windowBrowserName, true);   
    }



    #region Main Settings

    private static float windowMinimizeHeight = 0f;
    private static float windowMinimizeWeight = -50f;
    private static float windowMaximizeWeight = 1200f;
    private static float windowMaximizeHeight = 350f; // Amount to increase the window height
    private static float animationDuration = .0f;//.3f; // Duration of the animation in seconds
    private static float animationStartTime;
     
    private static Rect initialPosition;
    private static Rect targetPosition;
    private static EditorWindow window = null;

    private static bool isAnimating = false;
    private static bool isMinimizing = false;

    #endregion

    #region Main Logic

    private static void AdjustProjectWindowSize(string windowName, bool takeCurrentStateWindow = false)
    {
        if (isAnimating)
            return;

        FindProjectWindow(windowName);

        window.minSize = new Vector2(0, 0);
         
        initialPosition = window.position; 
        if ((int)window.position.height == windowMinimizeHeight && !takeCurrentStateWindow
            || (!isMinimizing && takeCurrentStateWindow) ) 
        {
            // Move to bottom-left when opening
            if (animationDuration > 0.01f) window.position = new Rect( 0, -Screen.currentResolution.height, window.position.width, window.position.height);
            initialPosition = window.position;
            isMinimizing = false;
            float screenHeight = Screen.currentResolution.height;
            targetPosition = new Rect(0, screenHeight - windowMaximizeHeight, windowMaximizeWeight, windowMaximizeHeight);
        }
        else if ((int)window.position.height != windowMinimizeHeight && !takeCurrentStateWindow 
            || (isMinimizing && takeCurrentStateWindow)) 
        {
            // Move to top-right and off-screen when closing
            SetOneColumnLayout();
            window.minSize = new Vector2(0, 0);
            isMinimizing = true;
            windowMaximizeWeight = window.position.width;
            float screenWidth = Screen.currentResolution.width;
            targetPosition = new Rect(0, -window.position.height, windowMinimizeWeight, windowMinimizeHeight);
        }

        animationStartTime = (float)EditorApplication.timeSinceStartup;
        isAnimating = true;
        EditorApplication.update += AnimateWindow; 
    }

    private static void FindProjectWindow(string windowName)
    {
        if (window != null)
            return;

        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (EditorWindow w in windows)
        {
            if (w.GetType().ToString() == windowName)
            {
                window = w;
                break;
            }
        }
    }

    private static void AnimateWindow()
    {
        float elapsedTime = (float)EditorApplication.timeSinceStartup - animationStartTime;
        float t = Mathf.Clamp01(elapsedTime / animationDuration);

        window.position = new Rect(
            Mathf.Lerp(initialPosition.x, targetPosition.x, t),
            Mathf.Lerp(initialPosition.y, targetPosition.y, t),
            Mathf.Lerp(initialPosition.width, targetPosition.width, t),
            Mathf.Lerp(initialPosition.height, targetPosition.height, t)
        );

        if (elapsedTime / animationDuration >= 1.05f)
        {
            if (isMinimizing)
            {
                // After minimizing animation ends, instantly move to top-right corner
                float screenWidth = Screen.currentResolution.width;
                float screenHeight = Screen.currentResolution.height;
                window.position = new Rect(screenWidth * 2 + window.position.width, 0, windowMinimizeWeight, windowMinimizeHeight);
            }
            else
            {
                // When maximizing, animation ends in the bottom-left corner
                // Do nothing special here since targetPosition is already set to bottom-left
                SetTwoColumnLayout();
                window.minSize = new Vector2(0, 0);
            }

            isAnimating = false;    
            EditorApplication.update -= AnimateWindow;
        }
    }


    static void SetOneColumnLayout()
    {
        var projectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        var projectBrowser = EditorWindow.GetWindow(projectBrowserType);
        var setOneColumnMethod = projectBrowserType.GetMethod("SetOneColumn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (setOneColumnMethod != null)
        {
            setOneColumnMethod.Invoke(projectBrowser, null);
            //Debug.Log("Project Browser set to One Column Layout");
        }
        else
        {
            Debug.LogError("Failed to find SetOneColumn method");
        }
    }

    static void SetTwoColumnLayout()
    {
        var projectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        var projectBrowser = EditorWindow.GetWindow(projectBrowserType);
        var setTwoColumnsMethod = projectBrowserType.GetMethod("SetTwoColumns", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (setTwoColumnsMethod != null)
        {
            setTwoColumnsMethod.Invoke(projectBrowser, null);
            //Debug.Log("Project Browser set to Two Column Layout");
        }
        else
        {
            Debug.LogError("Failed to find SetTwoColumns method");
        }
    }
    #endregion
}

#endif
