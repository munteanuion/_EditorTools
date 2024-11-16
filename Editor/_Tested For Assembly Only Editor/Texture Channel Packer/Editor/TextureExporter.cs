using UnityEngine;
using UnityEditor;
using System.IO;

namespace RambosStudios.TextureChannelPacker {
	public static class TextureExporter {	
		public static void ExportTexture(Texture2D texture, string path) {
			byte[] bytes = texture.EncodeToPNG();
			File.WriteAllBytes(path + ".png", bytes);
			AssetDatabase.Refresh();
		}
	}
}
