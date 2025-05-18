using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core
{
	internal static class HttpWebRequestHandlers
	{
		/// <summary>
		/// Does a request and gets a Json response.
		/// </summary>
		/// <param name="address">Url to perform a request on</param>
		/// <param name="result">JSON received as response (or empty if failed)</param>
		/// <returns>True if response was successful, false if failed.</returns>
		public static bool GrabJson(Uri address, out string result)
		{
			try
			{
				HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(address);
				wRequest.Credentials = CredentialCache.DefaultCredentials;

				dynamic wResponse = wRequest.GetResponse().GetResponseStream();
				StreamReader reader = new StreamReader(wResponse);
				result = reader.ReadToEnd();
				reader.Close();
				wResponse.Close();
				return true;
			}
			catch (Exception)
			{
				result = "";
				return false;
			}
		}

		public static string GetSync(string baseUrl, string scope, string parameters, Dictionary<string, string> requestHeaders)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + scope + parameters);

				//Headers
				if (requestHeaders != null)
				{
					foreach (var header in requestHeaders)
					{
						request.Headers[header.Key] = header.Value;
					}
				}

				request.ContentType = "application/json";
				request.Method = "GET";

				var webResponse = request.GetResponse();
				using (HttpWebResponse response = (HttpWebResponse)webResponse)
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine($"Failed to perform get: {e}");
				ErrorLogging.WriteLine($"Url and scope were: to perform get: {baseUrl+scope}");
				return "";
			}
		}

		public static async Task<string> PerformGetAsync(string baseUrl, string scope, string parameters, Dictionary<string, string> requestHeaders, int timeout = 5000)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + scope + parameters);

			try
			{
				foreach (var requestHeader in requestHeaders)
					request.Headers[requestHeader.Key] = requestHeader.Value;

				request.Timeout = timeout;
				request.Method = "GET";

				var webResponse = await request.GetResponseAsync();
				using (HttpWebResponse response = (HttpWebResponse)webResponse)
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					return await reader.ReadToEndAsync();
				}
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine($"Failed to perform get: {e}");
				ErrorLogging.WriteLine($"Url and scope were: to perform get: {baseUrl + scope}");
				return "";
			}
		}

		public static async Task<string> PerformDeleteAsync(string baseUrl, string scope, string parameters, Dictionary<string, string> headers, string contentType = "application/json", int timeout = 8000)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + scope + parameters);

			try
			{
				foreach (var header in headers)
				{
					request.Headers[header.Key] = header.Value;
				}
				request.Method = "DELETE";
				request.Timeout = timeout;
				request.ContentType = contentType;

				using (var response = await request.GetResponseAsync())
				using (var stream = response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var result = reader.ReadToEnd();
					return result;
				}
			}
			catch (Exception ex)
			{
				ErrorLogging.WriteLine($"Failed to perform delete: {ex}");
				ErrorLogging.WriteLine($"Url and scope were: to perform get: {baseUrl + scope + parameters}");
				return "";
			}
		}

		public static async Task<string> PerformPostAsync(string baseUrl, string scope, string parameters, string postData, Dictionary<string, string> headers, string contentType = "application/json", int timeout = 8000)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + scope + parameters);

			try
			{
				foreach (var header in headers)
				{
					request.Headers[header.Key] = header.Value;
				}

				byte[] encodedPostData = Encoding.UTF8.GetBytes(postData);

				request.Timeout = timeout;
				request.Method = "POST";
				request.ContentType = contentType;
				request.ContentLength = encodedPostData.Length;

				var requestStream = request.GetRequestStreamAsync();
				using (var rqStream = await requestStream)
				{
					await rqStream.WriteAsync(encodedPostData, 0, encodedPostData.Length);
				}

				var webResponse = await request.GetResponseAsync();
				using (HttpWebResponse response = (HttpWebResponse)webResponse)
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					return await reader.ReadToEndAsync();
				}
			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine($"Failed to perform post: {e}");
				ErrorLogging.WriteLine($"Url and scope were: to perform get: {baseUrl + scope + parameters}");
				ErrorLogging.WriteLine($"Content was: {postData}");
				return "";
			}
		}


		/// <summary>
		/// A wrapper function for building a URL requests
		/// </summary>
		/// <param name="BaseURI">Base of URL</param>
		/// <param name="variables">Parameters to build on</param>
		/// <returns>Combined URL</returns>
		internal static Uri BuildRequestUri(string BaseURI, string[] variables)
		{
			string uri = BaseURI;
			for (int i = 0; i < variables.Length; i++)
			{
				if (i == 0)
				{
					uri += "?" + variables[i];
				}
				else
				{
					uri += "&" + variables[i];
				}
			}
			return new Uri(uri);
		}

		/// <summary>
		/// Formats parameter to key=Value
		/// </summary>
		/// <param name="header">Header</param>
		/// <param name="variable">Variable</param>
		/// <returns>Formatted parameter</returns>
		internal static string FormatParameter(string header, string variable)
		{
			return header + "=" + Uri.EscapeDataString(variable);
		}
	}
}
