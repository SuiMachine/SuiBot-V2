using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SuiBot_Core
{
    internal class ImgUploader
    {
        SuiBot botInstance { get; set; }

        string ImageUploadEndPoint = "https://api.imgbb.com/1/upload";

        internal enum ImageUploadResult
        {
            OK,
            FailedToParse,
            Fail,
            FileNotFound,
            NotConfigured
        }

        public ImgUploader(SuiBot botInstance)
        {
            this.botInstance = botInstance;
        }

        public ImageUploadResult UploadImage(string path, out string UrlOfImage)
        {
            string ApiKey = botInstance.GetImgBBKey();
            if (ApiKey == "")
            {
                UrlOfImage = "";
                return ImageUploadResult.NotConfigured;
            }

            if(!File.Exists(path))
            {
                UrlOfImage = "";
                return ImageUploadResult.FileNotFound;
            }

            try
            {
                FileStream fstream = File.OpenRead(path);
                byte[] data = new byte[fstream.Length];
                fstream.Read(data, 0, data.Length);
                fstream.Close();

                HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(string.Format("{0}?key={1}", ImageUploadEndPoint, ApiKey));
                wRequest.Method = "POST";
                wRequest.ContentType = "application/x-www-form-urlencoded";

                wRequest.ServicePoint.Expect100Continue = false;
                NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
                outgoingQueryString.Add("image", Convert.ToBase64String(data));
                string postdata = outgoingQueryString.ToString();

                StreamWriter streamWriter = new StreamWriter(wRequest.GetRequestStream());
                streamWriter.Write(postdata);
                streamWriter.Close();

                WebResponse response = wRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                string responseString = responseReader.ReadToEnd();

                var responseData = JObject.Parse(responseString)["data"];
                if(responseData == null)
                {
                    UrlOfImage = "";
                    return ImageUploadResult.FailedToParse;
                }

                UrlOfImage = responseData["url_viewer"].ToString();
                return ImageUploadResult.OK;
            }
            catch(Exception e)
            {
                ErrorLogging.WriteLine(e.ToString());
                UrlOfImage = "";
                return ImageUploadResult.Fail;
            }
        }
    }
}
