/// #LogicScript

#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;

namespace Fierclash.Tools
{
	/// <summary>
	/// Accesses GoogleSheets via asynchronous web requests to download,
	/// process, and store sheet data in batches.
	/// </summary>
	internal sealed class GoogleSheetsImporter
	{
		public string assetPath;
		public string assetPrefix;
		public string docID;
		public string[] sheetBatch;
		public CSVImportMode mode;

		public async Task ImportSheet(Action OnComplete = null)
		{
			switch (mode)
			{
				case CSVImportMode.ImportMain: await ImportSheetMain(); break;
				case CSVImportMode.ImportBatch: await ImportSheetBatch(); break;
			}
			OnComplete?.Invoke();
		}

		private async Task ImportSheetMain()
		{
			// Download sheet
			string URL = GoogleSheetsUtility.GetMainSheetURL(docID);
			string file = "";
			await GoogleSheetsUtility.ImportGoogleSheetFromURL(URL, x => file = x);

			// Create and write as text file
			if (!Directory.Exists(assetPath)) Directory.CreateDirectory(assetPath);
			string filePath = string.Format("{0}/{1}{2}-{3}.txt",
											assetPath, 
											assetPrefix, 
											docID, 
											"Main");
			using (var writer = new StreamWriter(filePath))
			{
				await writer.WriteAsync(file);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private async Task ImportSheetBatch()
		{
			foreach (var sheetName in sheetBatch)
			{
				string URL = GoogleSheetsUtility.GetSheetURL(docID, sheetName);
				string file = "";
				await GoogleSheetsUtility.ImportGoogleSheetFromURL(URL, x => file = x);

				// Create and write as text file
				if (!Directory.Exists(assetPath)) Directory.CreateDirectory(assetPath);
				string filePath = string.Format("{0}/{1}{2}.txt",
												assetPath,
												assetPrefix,
												sheetName);
				using (var writer = new StreamWriter(filePath))
				{
					await writer.WriteAsync(file);
					writer.Close();
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}
#endif