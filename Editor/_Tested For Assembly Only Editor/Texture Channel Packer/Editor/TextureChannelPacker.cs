using UnityEngine;
using UnityEditor;
using System.IO;

namespace RambosStudios.TextureChannelPacker {
	public class TextureChannelPacker : EditorWindow {

		Texture2D[] textures = new Texture2D[4];
		TextureField[] textureFields = new TextureField[4];
		TextureChannel[] textureChannels = new TextureChannel[4];

		bool isDragging;
		Vector2 draggingOffset;
		int draggingTextureIndex, draggingChannelIndex;
		GUIStyle draggingStyle;

		bool[] outputButtonIsActive = new bool[5];
		Texture2D previewTexture;
		Vector2 scrollPosition;

		string folderPath;
		string fileName = "Untitled";
		int imageSizeIndex = 1;
		int width = 1024;
		int height = 1024;
		bool drawCustomSizeFields;
		char[] invalidFileChars;
		string tempFolder;

		GUIStyle defaultButtonStyle;
		GUIStyle[] inputButtonStyles = new GUIStyle[16];
		GUIStyle[] outputButtonStyles = new GUIStyle[5];
		GUIStyle[] activeButtonStyles = new GUIStyle[5];
		bool[] activeStyleInUse = new bool[5];
		bool stylesInitialized;

		static readonly string[] channelNames = { "R", "G", "B", "A" };
		static readonly string[] outputButtonNames = { "RGB", "R", "G", "B", "A" };
		static readonly string[] sizeOptions = { "512", "1024", "2048", "4096", "Custom" };
		static readonly Color[] activeButtonColors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta };

		[MenuItem("Window/Texture Channel Packer")]
		static void ShowWindow() {
			TextureChannelPacker window = GetWindow<TextureChannelPacker>("Texture Channel Packer");
			window.minSize = new Vector2(380, 564);
		}

		void OnEnable() {
			outputButtonIsActive[0] = true;
			folderPath = Application.dataPath;
			tempFolder = GetTempFolderPath();
			invalidFileChars = Path.GetInvalidFileNameChars();
			string[] supportedFormats = { ".png", ".tif", ".tga", ".psd", ".jpg", ".gif" };
			string[] filters = { "Image files", "png,tif,tga,psd,jpg,gif" };
			for(int i = 0; i < 4; i++) {
				string filePath = tempFolder + "temp" + i + ".png";
				textureFields[i] = new TextureField(filePath, supportedFormats, filters);
				textureChannels[i] = new TextureChannel();
			}
			UpdateTexturePreview();
		}

		void OnDisable() {
			AssetDatabase.DeleteAsset(tempFolder);
			AssetDatabase.Refresh();
		}

		void OnGUI() {
			if(!stylesInitialized) {
				InitButtonStyles();
			}
			Event e = Event.current;
			DrawChannelPacker(e);
			DrawOutputInfo(new Rect(0, 284, 380, 98));
			DrawExportSettings(new Rect(0, 389, 380, 180));
			if(isDragging) {
				ProcessDragging(e);
			}
		}

		void InitButtonStyles() {
			defaultButtonStyle = GUI.skin.button;
			for(int i = 0; i < 16; i++) {
				inputButtonStyles[i] = defaultButtonStyle;
			}
			for(int i = 0; i < 5; i++) {
				outputButtonStyles[i] = defaultButtonStyle;

				activeButtonStyles[i] = new GUIStyle(GUI.skin.button);
				activeButtonStyles[i].normal.textColor = activeButtonColors[i];
				activeButtonStyles[i].onNormal.textColor = activeButtonColors[i];
				activeButtonStyles[i].hover.textColor = activeButtonColors[i];
				activeButtonStyles[i].onHover.textColor = activeButtonColors[i];
				activeButtonStyles[i].active.textColor = activeButtonColors[i];
				activeButtonStyles[i].onActive.textColor = activeButtonColors[i];
			}
			stylesInitialized = true;
		}

		void DrawChannelPacker(Event e) {
			for(int t = 0; t < 4; t++) {
				string currentTextureName = textureFields[t].GetFileName();
				textures[t] = textureFields[t].DrawTextureField(new Rect(0, t * 71, 64, 64), e);
				if(currentTextureName != textureFields[t].GetFileName()) {
					UpdateTexturePreview();
				}
				Rect[] buttonRects = {
					new Rect(64, t * 71, 32, 32),
					new Rect(96, t * 71, 32, 32),
					new Rect(64, 32 + (t * 71), 32, 32),
					new Rect(96, 32 + (t * 71), 32, 32)
				};
				for(int c = 0; c < 4; c++) {
					DrawInputchannelButton(buttonRects[c], t, c, e);
				}
			}
			for(int i = 0; i < 5; i++) {
				DrawOutputChannelButton(new Rect(135 + (i * 49), 245, 49, 32), i, e);
			}
			DrawTexturePreview(new Rect(135, 0, 245, 245), e);
		}

		void DrawInputchannelButton(Rect rect, int textureIndex, int channelIndex, Event e) {
			GUIStyle style = inputButtonStyles[(4 * textureIndex) + channelIndex];
			if(e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) {
				if(e.button == 0) {
					isDragging = true;
					draggingOffset = e.mousePosition - rect.position;
					draggingTextureIndex = textureIndex;
					draggingChannelIndex = channelIndex;
					draggingStyle = style;
				}
				else if(e.button == 1) {
					ClearInputButton(style);
				}
			}
			GUI.Box(rect, channelNames[channelIndex], style);
		}

		void DrawOutputChannelButton(Rect rect, int buttonIndex, Event e) {
			GUIStyle style = outputButtonStyles[buttonIndex];
			if(e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition) && isDragging) {
				FillTextureChannel(buttonIndex);
			}
			if(e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition)) {
				DrawGenericMenu(buttonIndex);
				return;
			}
			EditorGUI.BeginChangeCheck();
			outputButtonIsActive[buttonIndex] = GUI.Toggle(rect, outputButtonIsActive[buttonIndex], outputButtonNames[buttonIndex], style);
			if(EditorGUI.EndChangeCheck()) {
				for(int i = 0; i < 5; i++) {
					outputButtonIsActive[i] = false;
				}
				outputButtonIsActive[buttonIndex] = true;
				UpdateTexturePreview();
			}
		}

		void DrawTexturePreview(Rect rect, Event e) {
			GUI.Box(rect, string.Empty, GUI.skin.window);
			Rect textureRect = new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12);
			GUI.DrawTexture(textureRect, previewTexture, ScaleMode.StretchToFill, false);
		}

		void DrawOutputInfo(Rect rect) {
			GUI.Box(rect, string.Empty, GUI.skin.window);
			Rect textfieldRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
			GUILayout.BeginArea(textfieldRect, GUI.skin.textArea);
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, true, false, GUI.skin.horizontalScrollbar, GUIStyle.none);
			for(int i = 0; i < 4; i++) {
				string text = channelNames[i] + ": ";
				if(textureChannels[i].textureIndex == -1 || textures[textureChannels[i].textureIndex] == null) {
					if(!textureChannels[i].colorsInverted) {
						text += "Solid white";
						if(i == 3) {
							text += " (No alpha channel)";
						}
					}
					else {
						text += "Solid black";
					}
				}
				else {
					text += textureFields[textureChannels[i].textureIndex].GetFileName() + ", Channel: " + channelNames[textureChannels[i].channelIndex];
					if(textureChannels[i].colorsInverted) {
						text += " (Colors inverted)";
					}
				}
				GUILayout.Label(text);
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		void DrawExportSettings(Rect rect) {
			GUI.Box(rect, string.Empty, GUI.skin.window);
			Rect textfieldRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
			GUILayout.BeginArea(textfieldRect, GUI.skin.box);

			DrawPathSetting();
			DrawNameSetting();
			DrawSizeSettings();

			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Export Texture")) {
				ExportTexture();
			}

			GUILayout.EndArea();
		}

		void DrawPathSetting() {
			GUIStyle style = new GUIStyle(GUI.skin.button);
			style.wordWrap = true;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Path", GUILayout.Width(50));
			if(GUILayout.Button(folderPath, style)) {
				string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, string.Empty);
				if(path != string.Empty) {
					folderPath = path;
				}
			}
			GUILayout.EndHorizontal();
		}

		void DrawNameSetting() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name", GUILayout.Width(50));

			EditorGUI.BeginChangeCheck();
			fileName = GUILayout.TextField(fileName);
			if(EditorGUI.EndChangeCheck()) {
				for(int i = 0; i < invalidFileChars.Length; i++) {
					if(fileName.EndsWith(invalidFileChars[i].ToString())) {
						fileName = fileName.Remove(fileName.Length - 1);
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		void DrawSizeSettings() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Size", GUILayout.Width(50));

			EditorGUI.BeginChangeCheck();
			imageSizeIndex = EditorGUILayout.Popup(imageSizeIndex, sizeOptions);
			if(EditorGUI.EndChangeCheck()) {
				if(imageSizeIndex == 4) {
					drawCustomSizeFields = true;
				}
				else {
					width = int.Parse(sizeOptions[imageSizeIndex]);
					height = int.Parse(sizeOptions[imageSizeIndex]);
					drawCustomSizeFields = false;
				}
			}
			GUILayout.EndHorizontal();

			if(drawCustomSizeFields) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Width", GUILayout.Width(50));
				EditorGUI.BeginChangeCheck();
				width = EditorGUILayout.IntField(width);
				if(EditorGUI.EndChangeCheck()) {
					if(width < 1) {
						width = 1;
					}
					else if(width > 8192) {
						width = 8192;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Height", GUILayout.Width(50));
				EditorGUI.BeginChangeCheck();
				height = EditorGUILayout.IntField(height);
				if(EditorGUI.EndChangeCheck()) {
					if(height < 1) {
						height = 1;
					}
					else if(height > 8192) {
						height = 8192;
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		void ExportTexture() {
			Texture2D exportTexture = TextureGenerator.GenerateTexture(width, height, textures, textureChannels);
			string path = folderPath + Path.DirectorySeparatorChar + fileName;
			TextureExporter.ExportTexture(exportTexture, path);
		}

		void ProcessDragging(Event e) {
			Rect rect = new Rect(e.mousePosition - draggingOffset, new Vector2(32, 32));
			GUI.Box(rect, channelNames[draggingChannelIndex], draggingStyle);
			GUI.changed = true;

			if(e.type == EventType.MouseUp && e.button == 0) {
				isDragging = false;
			}
		}

		void DrawGenericMenu(int buttonIndex) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Invert Colors"), false, () => InvertChannelColors(buttonIndex));
			if(buttonIndex == 0) {
				for(int i = 0; i < 3; i++) {
					if(textureChannels[i].textureIndex >= 0) {
						menu.AddItem(new GUIContent("Clear"), false, () => ClearTextureChannel(buttonIndex));
						break;
					}
				}
			}
			else if(textureChannels[buttonIndex - 1].textureIndex >= 0) {
				menu.AddItem(new GUIContent("Clear"), false, () => ClearTextureChannel(buttonIndex));
			}
			menu.ShowAsContext();
		}

		void FillTextureChannel(int buttonIndex) {
			if(buttonIndex == 0) {
				for(int i = 0; i < 3; i++) {
					SetActiveButtonStyles(i);
					textureChannels[i].textureIndex = draggingTextureIndex;
					textureChannels[i].channelIndex = draggingChannelIndex;
					textureChannels[i].colorsInverted = false;
				}
			}
			else {
				SetActiveButtonStyles(buttonIndex - 1);
				textureChannels[buttonIndex - 1].textureIndex = draggingTextureIndex;
				textureChannels[buttonIndex - 1].channelIndex = draggingChannelIndex;
				textureChannels[buttonIndex - 1].colorsInverted = false;
			}
			UpdateTexturePreview();
		}

		void ClearInputButton(GUIStyle inputButtonStyle) {
			for(int i = 0; i < 4; i++) {
				if(outputButtonStyles[i + 1] == inputButtonStyle) {
					ClearTextureChannel(i + 1);
				}
			}
			GUI.changed = true;
		}

		void ClearTextureChannel(int buttonIndex) {
			if(buttonIndex == 0) {
				for(int i = 0; i < 3; i++) {
					if(textureChannels[i].textureIndex < 0) continue;
					ClearActiveButtonStyles(i);
					textureChannels[i].textureIndex = -1;
					textureChannels[i].channelIndex = -1;
					textureChannels[i].colorsInverted = false;
					}
				}
			else {
				if(textureChannels[buttonIndex - 1].textureIndex < 0) return;
				ClearActiveButtonStyles(buttonIndex - 1);
				textureChannels[buttonIndex - 1].textureIndex = -1;
				textureChannels[buttonIndex - 1].channelIndex = -1;
				textureChannels[buttonIndex - 1].colorsInverted = false;
			}
			UpdateTexturePreview();
		}

		void InvertChannelColors(int buttonIndex) {
			if(buttonIndex == 0) {
				for(int i = 0; i < 3; i++) {
					textureChannels[i].colorsInverted = !textureChannels[i].colorsInverted;
				}
			}
			else {
				textureChannels[buttonIndex - 1].colorsInverted = !textureChannels[buttonIndex - 1].colorsInverted;
			}
			UpdateTexturePreview();
		}

		void UpdateTexturePreview() {
			TextureChannel[] previewTextureChannels = new TextureChannel[4];
			for(int i = 0; i < 4; i++) {
				if(outputButtonIsActive[0]) {
					previewTextureChannels[i] = textureChannels[i];
				}
				else if(outputButtonIsActive[1]) {
					previewTextureChannels[i] = textureChannels[0];
				}
				else if(outputButtonIsActive[2]) {
					previewTextureChannels[i] = textureChannels[1];
				}
				else if(outputButtonIsActive[3]) {
					previewTextureChannels[i] = textureChannels[2];
				}
				else {
					previewTextureChannels[i] = textureChannels[3];
				}
			}
			previewTexture = TextureGenerator.GenerateTexture(233, 233, textures, previewTextureChannels);
		}

		void SetActiveButtonStyles(int channelIndex) {
			GUIStyle replacedStyle = outputButtonStyles[channelIndex + 1];
			if(draggingStyle == defaultButtonStyle) {
				for(int i = 0; i < 5; i++) {
					if(activeStyleInUse[i]) continue;
					int inputButtonIndex = (4 * draggingTextureIndex) + draggingChannelIndex;
					inputButtonStyles[inputButtonIndex] = activeButtonStyles[i];
					outputButtonStyles[channelIndex + 1] = activeButtonStyles[i];
					draggingStyle = activeButtonStyles[i];
					activeStyleInUse[i] = true;
					break;
				}
			}
			else {
				outputButtonStyles[channelIndex + 1] = draggingStyle;
			}
			for(int i = 0; i < 4; i++) {
				if(outputButtonStyles[i + 1] == replacedStyle) return;
			}
			for(int i = 0; i < 16; i++) {
				if(inputButtonStyles[i] == replacedStyle) {
					inputButtonStyles[i] = defaultButtonStyle;
					break;
				}
			}
			for(int i = 0; i < 5; i++) {
				if(activeButtonStyles[i] == replacedStyle) {
					activeStyleInUse[i] = false;
					return;
				}
			}
		}

		void ClearActiveButtonStyles(int channelIndex) {
			int inputButtonIndex = (4 * textureChannels[channelIndex].textureIndex + textureChannels[channelIndex].channelIndex);
			outputButtonStyles[channelIndex + 1] = defaultButtonStyle;

			for(int i = 0; i < 4; i++) {
				if(outputButtonStyles[i + 1] == inputButtonStyles[inputButtonIndex]) return;
			}
			for(int i = 0; i < 5; i++) {
				if(activeButtonStyles[i] == inputButtonStyles[inputButtonIndex]) {
					inputButtonStyles[inputButtonIndex] = defaultButtonStyle;
					activeStyleInUse[i] = false;
					return;
				}
			}
		}

		string GetTempFolderPath() {
			MonoScript ms = MonoScript.FromScriptableObject(this);
			string scriptPath = AssetDatabase.GetAssetPath(ms);
			string folderPath = scriptPath.Substring(0, scriptPath.IndexOf(ms.name)) + "Temp" + Path.DirectorySeparatorChar;
			if(!Directory.Exists(folderPath)) {
				Directory.CreateDirectory(folderPath);
				AssetDatabase.Refresh();
			}
			return folderPath;
		}
	}
}
