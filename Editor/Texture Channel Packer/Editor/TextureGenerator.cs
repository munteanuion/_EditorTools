using UnityEngine;

namespace RambosStudios.TextureChannelPacker {
	public static class TextureGenerator {
		public static Texture2D GenerateTexture(int width, int height, Texture2D[] textures, TextureChannel[] textureChannels) {
			Texture2D[] scaledTextures = GetScaledTextures(width, height, textures);

			Texture2D outputTexture = new Texture2D(width, height);
			outputTexture.SetPixels(GetColorArray(width, height, scaledTextures, textureChannels));
			outputTexture.Apply();

			return outputTexture;
		}

		static Texture2D[] GetScaledTextures(int width, int height, Texture2D[] textures) {
			Texture2D[] scaledTextures = new Texture2D[4];
			for(int i = 0; i < 4; i++) {
				if(textures[i] == null) continue;
				if(textures[i].width == width && textures[i].height == height) {
					scaledTextures[i] = textures[i];
				}
				else {
					Texture2D convertedTexture = new Texture2D(width, height);
					Graphics.ConvertTexture(textures[i], convertedTexture);
					convertedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
					scaledTextures[i] = convertedTexture;
				}
			}
			return scaledTextures;
		}

		static Color[] GetColorArray(int width, int height, Texture2D[] textures, TextureChannel[] textureChannels) {
			Color[] colorArray = new Color[width * height];
			for(int i = 0; i < colorArray.Length; i++) {
				int x = i % width;
				int y = i / height;

				colorArray[i].r = GetPixel(x, y, textures, textureChannels[0]);
				colorArray[i].g = GetPixel(x, y, textures, textureChannels[1]);
				colorArray[i].b = GetPixel(x, y, textures, textureChannels[2]);
				colorArray[i].a = GetPixel(x, y, textures, textureChannels[3]);
			}
			return colorArray;
		}

		static float GetPixel(int x, int y, Texture2D[] textures, TextureChannel textureChannel) {
			if(textureChannel.textureIndex >= 0 && textures[textureChannel.textureIndex] != null) {
				if(!textureChannel.colorsInverted) {
					return CopyPixel(x, y, textures[textureChannel.textureIndex], textureChannel.channelIndex);
				}
				else {
					return 1 - CopyPixel(x, y, textures[textureChannel.textureIndex], textureChannel.channelIndex);
				}
			}
			else if(!textureChannel.colorsInverted) {
				return 1;
			}
			return 0;
		}

		static float CopyPixel(int x, int y, Texture2D texture, int channelIndex) {
			if(channelIndex == 0) {
				return texture.GetPixel(x, y).r;
			}
			else if(channelIndex == 1) {
				return texture.GetPixel(x, y).g;
			}
			else if(channelIndex == 2) {
				return texture.GetPixel(x, y).b;
			}
			return texture.GetPixel(x, y).a;
		}
	}
}
