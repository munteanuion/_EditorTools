using System.IO;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal struct FileFormat
	{
		public System.Version FileVersion;
		public string SavePath;
		public string HierarchySavePath;
		public string ProjectLinksFileName;
		public string FileExtension;


		public string GetProjectLinksFilePath()
		{
			return Path.Combine(SavePath, $"{ProjectLinksFileName}.{FileExtension}");
		}

		public string GetHierarchyLinkFilePath(string sceneGuid)
		{
			return Path.Combine(HierarchySavePath, $"{sceneGuid}.{FileExtension}");
		}

		public bool CreateSaveDirectories()
		{
			bool created = true;

			if (!Directory.Exists(SavePath))
			{
				try
				{
					Directory.CreateDirectory(SavePath);
				}
				catch (PathTooLongException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[6]);
					Debug.LogError(string.Format(logFormat, SavePath));
				}
				catch (IOException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[7]);
					Debug.LogError(string.Format(logFormat, SavePath));
				}
				catch (System.UnauthorizedAccessException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[8]);
					Debug.LogError(string.Format(logFormat, SavePath));
				}
				catch (System.ArgumentException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[9]);
					Debug.LogError(string.Format(logFormat, SavePath));
				}
			}

			if (created && !Directory.Exists(HierarchySavePath))
			{
				try
				{
					Directory.CreateDirectory(HierarchySavePath);
				}
				catch (PathTooLongException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[10]);
					Debug.LogError(string.Format(logFormat, HierarchySavePath));
				}
				catch (IOException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[11]);
					Debug.LogError(string.Format(logFormat, HierarchySavePath));
				}
				catch (System.UnauthorizedAccessException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[12]);
					Debug.LogError(string.Format(logFormat, HierarchySavePath));
				}
				catch (System.ArgumentException)
				{
					created = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[13]);
					Debug.LogError(string.Format(logFormat, HierarchySavePath));
				}
			}

			return created;
		}
	}
}
