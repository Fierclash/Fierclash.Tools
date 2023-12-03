/// #UtilityScript

/// Reader logic for asynchronously obtaining .csv file strings from GoogleSheets.
/// Implements UnityWebRequest so user must have stable internet connection.
/// GoogleSheets must be accessible with a public link.

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Fierclash.Tools
{
	internal static class GoogleSheetsUtility
	{
		/// <summary>
		/// Gets a URL string for the main sheet of a Google Sheets document.
		/// </summary>
		public static string GetMainSheetURL(string docID)
		{
			// Can attach Queries to end of &tq=<INSERT QUERY HERE> using Google's Query Language
			// Must use an encoded version (encoder available in Google's API)
			// <seealso href="https://developers.google.com/chart/interactive/docs/querylanguage"/>
			return string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format=csv", 
									docID);
		}

		/// <summary>
		/// Gets a URL string for a sheet in a Google Sheets document.
		/// </summary>
		public static string GetSheetURL(string docID, string sheetName)
		{
			return string.Format("https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}",
									docID, 
									sheetName);
		}

		/// <summary>
		/// Asynchronously downloads a sheet from Google Sheets as a .csv.
		/// </summary>
		public static async Task ImportGoogleSheetFromURL(string URL, Action<string> OnComplete = null)
		{
			string file = "";
			using (UnityWebRequest webRequest = UnityWebRequest.Get(URL))
			{
				// Send web request to access GoogleSheet
				webRequest.SendWebRequest();
				while (!webRequest.isDone) await Task.Delay(1);

				// Handle request
				if (webRequest.result == UnityWebRequest.Result.Success)
				{
					file = webRequest.downloadHandler.text;
					Debug.LogFormat("Successfully downloaded GoogleSheet from {0}.", URL);
				}
				else
				{
					Debug.LogErrorFormat("Failed to download GoogleSheet from URL: {0}.", URL);
				}
			}

			OnComplete?.Invoke(file);
		}
	}
}
