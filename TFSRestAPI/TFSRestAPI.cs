using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TFSRestAPI
{
    internal class TFSRestAPI
    {
		public static async void GetProjects()
		{
			try
			{
				var personalaccesstoken = "43k5vefvv6ljniu74eegmlbcsq2dampettv3uceb5i37i3kdw2ba";

				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Accept.Add(
						new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
						Convert.ToBase64String(
							System.Text.ASCIIEncoding.ASCII.GetBytes(
								string.Format("{0}:{1}", "", personalaccesstoken))));

					using (HttpResponseMessage response = client.GetAsync(
								"https://dev.azure.com/ACVS/_apis/wit/workitems/12?api-version=6.0").Result)
					{
						response.EnsureSuccessStatusCode();
						string responseBody = await response.Content.ReadAsStringAsync();
						Console.WriteLine(responseBody);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
