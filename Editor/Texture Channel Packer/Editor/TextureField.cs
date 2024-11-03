using UnityEngine;
using UnityEditor;
using System.IO;

namespace RambosStudios.TextureChannelPacker {
	public class TextureField {

		Texture2D texture;
		string fileName;

		string filePath;
		string[] supportedFormats;
		string[] filters;

		public TextureField(string filePath, string[] supportedFormats, string[] filters) {
			this.filePath = filePath;
			this.supportedFormats = supportedFormats;
			this.filters = filters;
		}

		public Texture2D DrawTextureField(Rect rect, Event e) {
			ProcessDropArea(rect, e);

			GUIStyle style = new GUIStyle("TextArea");
			style.alignment = TextAnchor.MiddleCenter;
			style.fontSize = 10;

			GUI.Box(rect, string.Empty, GUI.skin.button);
			Rect textureRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
			if(texture == null) {
				GUI.Box(textureRect, "Drop an image file here or click", style);
			}
			else {
				GUI.DrawTexture(textureRect, texture);
			}
			return texture;
		}

		public string GetFileName() {
			return fileName;
		}

		void ProcessDropArea(Rect rect, Event e) {
			if(e.type == EventType.DragUpdated || e.type == EventType.DragPerform) {
				if(rect.Contains(e.mousePosition) && DragAndDrop.paths.Length > 0) {
					if(!IsSupportedFormat(DragAndDrop.paths[0])) return;
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					if(e.type == EventType.DragPerform) {
						CopyAsset(DragAndDrop.paths[0]);
					}
				}
			}
			if(e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) {
				if(e.button == 0) {
					string path = EditorUtility.OpenFilePanelWithFilters("Select Image", Application.dataPath, filters);
					if(path == string.Empty) return;
					CopyAsset(path);
				}
				else if(e.button == 1) {
					DeleteAsset();
				}
			}
		}

		bool IsSupportedFormat(string path) {
			for(int i = 0; i < supportedFormats.Length; i++) {
				if(path.EndsWith(supportedFormats[i])) return true;
			}
			return false;
		}

		void CopyAsset(string path) {
			if(path == filePath) return;
			fileName = ExtractFileName(path);
			DeleteAsset();
			FileUtil.CopyFileOrDirectory(path, filePath);
			AssetDatabase.Refresh();
			SetImportSettings(filePath);

			if(texture == null) {
				texture = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
			}
		}

		void DeleteAsset() {
			AssetDatabase.DeleteAsset(filePath);
		}

		void SetImportSettings(string path) {
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.isReadable = true;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.maxTextureSize = 8192;
			importer.SaveAndReimport();
		}

		string ExtractFileName(string path) {		
			string fileName = Path.GetFileName(path);
			for(int i = 0; i < supportedFormats.Length; i++) {
				if(fileName.EndsWith(supportedFormats[i])) {
					return fileName.Remove(fileName.IndexOf(supportedFormats[i]));
				}
			}
			return null;
		}
	}
}
