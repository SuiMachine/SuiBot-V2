using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core
{
    internal static class JsonGrabber
    {
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
            catch(Exception e)
            {
                result = "";
                return false;
            }

        }

        public static bool GrabJson(Uri address, Dictionary<string, string> headers, string contantType, string acceptStr, string Method, out string result)
        {
            try
            {
                HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(address);

                //Headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        wRequest.Headers[header.Key] = header.Value;
                    }
                }

                //ConstantType
                if (contantType != null)
                {
                    wRequest.ContentType = contantType;
                }

                //AcceptString
                if (acceptStr != null)
                {
                    wRequest.Accept = acceptStr;
                }

                //Method
                if (Method != null)
                {
                    wRequest.Method = Method;
                }
                

                dynamic wResponse = wRequest.GetResponse().GetResponseStream();
                StreamReader reader = new StreamReader(wResponse);
                result = reader.ReadToEnd();
                reader.Close();
                wResponse.Close();
                return true;
            }
            catch(Exception e)
            {
                result = "";
                return false;
            }
        }
    }
}
