using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.UI;
//using UnityEditor.Experimental.TerrainAPI;
using UnityEngine.UI;
#if UNITY_2019_1 || UNITY_2019_2
using UnityEngine.Experimental.PlayerLoop;
#else
using UnityEngine.PlayerLoop;
#endif
using System.Runtime.CompilerServices;

public class NameTheItemTool : EditorWindow
{
    bool useTool = false;

    GameObject objCloseToCursor;
    Vector3 textPos;
    string mouseOnObject;
    string previousObject = "Tool is OFF";

    string mouseOnWindow;
    string windowOnPreviousUpdate = "Tool is OFF";

    //Style settings
    GUIStyle planeStyle;
    GUIStyle style;
    int textFontSize = 14;
    int sizeBeforUpdate;
    Color colorText = new Color32(255, 102, 0, 255);
    Color colorBeforeUpdate;
    bool updateSettings;
    bool showTextOnScene = true;
    bool showTextPast = true;

    [MenuItem("Window/Selection Identity")]
    public static void ShowWindow()
    {
        GetWindow<NameTheItemTool>("Selection Identity");
    }

    private void OnGUI()
    {
        if(planeStyle == null)
        {
            
            planeStyle = new GUIStyle(EditorStyles.label);
            planeStyle.normal.textColor = Color.white;
            planeStyle.fontSize = 18;
            planeStyle.fontStyle = FontStyle.Bold;
        }

        if(style == null)
        {
            //Retreaving text size if changed
            if (!PlayerPrefs.HasKey("Size"))
                PlayerPrefs.SetInt("Size", 18);
            textFontSize = PlayerPrefs.GetInt("Size");

            //Retrieveing text color if changed
            if (!PlayerPrefs.HasKey("R"))
                PlayerPrefs.SetFloat("R", colorText.r);
            if (!PlayerPrefs.HasKey("G"))
                PlayerPrefs.SetFloat("G", colorText.g);
            if (!PlayerPrefs.HasKey("B"))
                PlayerPrefs.SetFloat("B", colorText.b);
            if (!PlayerPrefs.HasKey("A"))
                PlayerPrefs.SetFloat("A", colorText.a);
            colorText = new Color(PlayerPrefs.GetFloat("R"), PlayerPrefs.GetFloat("G"), PlayerPrefs.GetFloat("B"), PlayerPrefs.GetFloat("A"));

            //Retrieveing data if should show text on screne in case changed
            if (!PlayerPrefs.HasKey("ShowText"))
                PlayerPrefs.SetInt("ShowText", 1);
            if (PlayerPrefs.GetInt("ShowText") == 1)
                showTextOnScene = true;
            else
                showTextOnScene = false;

            style = new GUIStyle();
            style.fontStyle = FontStyle.BoldAndItalic;
        }

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("ON"))
        {
            useTool = true;
            mouseOnObject = "Ready For Use";
            mouseOnWindow = "Ready For Use";
        }

        if(GUILayout.Button("OFF"))
        {
            useTool = false;
            mouseOnObject = "Tool is OFF";
            mouseOnWindow = "Tool is OFF";
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Mouse Hovering Over:", EditorStyles.boldLabel);
        GUILayout.Label("Object: " + mouseOnObject);
        GUILayout.Label("Window: " + mouseOnWindow);

        GUILayout.Space(20);
        GUILayout.Label("Tool Settings:", EditorStyles.boldLabel);
        showTextOnScene = EditorGUILayout.Toggle("Show Text on Scene: ", showTextOnScene);
        textFontSize = EditorGUILayout.IntField("Font Size", textFontSize);
        colorText = EditorGUILayout.ColorField("Font Color", colorText);
    }

    void OnInspectorUpdate()
    {
        if (useTool)
        {
            if (mouseOverWindow)
                mouseOnWindow = mouseOverWindow.ToString();
            else
                mouseOnWindow = "Null...";

            if (mouseOnWindow != windowOnPreviousUpdate)
            {
                this.Repaint();
                windowOnPreviousUpdate = mouseOnWindow;
            }

            if (mouseOnObject != previousObject)
            {
                this.Repaint();
                previousObject = mouseOnObject;
            }

            if (textFontSize != sizeBeforUpdate || colorText != colorBeforeUpdate || showTextOnScene != showTextPast)
                updateSettings = true;
        }
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= this.SecenGUI;
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= this.SecenGUI;
        SceneView.duringSceneGui += this.SecenGUI;
    }

    void SecenGUI(SceneView scene)
    {
        if (useTool)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseMove)
            {
                objCloseToCursor = HandleUtility.PickGameObject(e.mousePosition, true);

                if (objCloseToCursor != null)
                {
                    textPos = HandleUtility.GUIPointToWorldRay(e.mousePosition + Vector2.right * 20).origin;
                    mouseOnObject = objCloseToCursor.name;
                }

                if (updateSettings)
                {
                    style.fontSize = textFontSize;
                    sizeBeforUpdate = textFontSize;
                    PlayerPrefs.SetInt("Size", textFontSize);

                    style.normal.textColor = colorText;
                    colorBeforeUpdate = colorText;
                    PlayerPrefs.SetFloat("R", colorText.r);
                    PlayerPrefs.SetFloat("G", colorText.g);
                    PlayerPrefs.SetFloat("B", colorText.b);
                    PlayerPrefs.SetFloat("A", colorText.a);

                    showTextPast = showTextOnScene;
                    if(!showTextOnScene)
                        PlayerPrefs.SetInt("ShowText", 0);
                    else
                        PlayerPrefs.SetInt("ShowText", 1);

                    updateSettings = false;
                }
            }

            if (objCloseToCursor != null)
            {
                Handles.BeginGUI();
                if (showTextOnScene)
                    Handles.Label(textPos, mouseOnObject, style);
                Handles.EndGUI();
            }
            else if (mouseOnObject != "Null...")
                mouseOnObject = "Null...";


            Handles.BeginGUI();
            Rect rect = new Rect(0, 0, Screen.width, 21);
            GUILayout.BeginArea(rect);

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = colorText;

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.Label(mouseOnObject, planeStyle);
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }

}
