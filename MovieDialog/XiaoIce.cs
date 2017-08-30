using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using Newtonsoft.Json;

namespace MovieDialog
{
    class XiaoIce
    {
        struct XiaoIceResponseStyle
        {
            public string Service;
            public string ImpressionID;
            public List<XiaoIceAnswerStyle> Answer;
        }
        struct XiaoIceAnswerStyle
        {
            public string Content;
            public string Type;
        }

        /// <summary>
        /// SAI request sample。
        /// </summary>
        public static string XiaoIceResponse(string query)
        {
            Uri uri = new Uri("https://sai-pilot.msxiaobing.com/api/Conversation/GetResponse?api-version=2017-06-15-Int");
            string requestBody = $"{{\"Query\": \"{query}\",\"MessageType\":\"Text\"}}";
            string appID = "XItst11n0y8rn1f6yn";
            string secret = "cbc7255f4f31483d86966e475d4ce7f2";
            string userID = "e10adc3949ba59abbe56e057f20fxibu";
            long timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            var verb = "Post";
            var path = uri.AbsolutePath;

            List<string> headerList = new List<string>();
            headerList.Add("x-msxiaoice-request-app-id:" + appID);
            headerList.Add("x-msxiaoice-request-user-id:" + userID);
            var paramList = uri.Query.Substring(1)
                               .Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries)
                               .ToList();
            var signature = ComputeSignature(verb, path, paramList, headerList, requestBody, timestamp, secret);

            HttpWebRequest request = BuildRequest(uri, appID, userID, timestamp, signature, requestBody);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Console.WriteLine("Response Code: " + response.StatusCode);
            string xiaoice_res = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var xiaoice_ans = JsonConvert.DeserializeObject<XiaoIceResponseStyle>(xiaoice_res);
            string res = "";
            foreach (var item in xiaoice_ans.Answer)
            {
                if (item.Type == "Text")
                {
                    res = item.Content;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(res))
            {
                Utils.WriteError("XiaoIce Response Nothing");
                return "";
            }
            else
            {
                return res;
            }
        }

        /// <summary>
        /// Build request for SAI.
        /// </summary>
        /// <param name="uri">request uri</param>
        /// <param name="appID">app ID</param>
        /// <param name="userID">user ID</param>
        /// <param name="timestamp">timestamp, the number of seconds since January 1, 1970, 00:00:00 UTC</param>
        /// <param name="signature">signature, computed from request url, headers and body</param>
        /// <param name="requestBody">request body</param>
        /// <returns>
        /// SAI request.
        /// </returns>
        private static HttpWebRequest BuildRequest(
            Uri uri,
            string appID,
            string userID,
            long timestamp,
            string signature,
            string requestBody)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.Headers.Add("x-msxiaoice-request-app-id", appID);
            request.Headers.Add("x-msxiaoice-request-user-id", userID);
            request.Headers.Add("x-msxiaoice-request-timestamp", timestamp.ToString());
            request.Headers.Add("x-msxiaoice-request-signature", signature);

            byte[] postBytes = Encoding.UTF8.GetBytes(requestBody);
            request.ContentType = "application/json";
            request.ContentLength = postBytes.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            return request;
        }

        /// <summary>
        /// Compute signature to be used in request header.
        /// </summary>
        /// <param name="verb">request method</param>
        /// <param name="path">request url path</param>
        /// <param name="paramList">request param list</param>
        /// <param name="headerList">request header list</param>
        /// <param name="requestContent">request body</param>
        /// <param name="timestamp">timestamp, the number of seconds since January 1, 1970, 00:00:00 UTC</param>
        /// <param name="secretKey">user secret key</param>
        /// <returns>
        /// Signature to be used in request header.
        /// </returns>
        private static string ComputeSignature(
            string verb,
            string path,
            List<string> paramList,
            List<string> headerList,
            string requestContent,
            long timestamp,
            string secretKey)
        {
            verb = verb.ToLower();
            path = path.ToLower();
            paramList.Sort();
            var strParam = string.Join("&", paramList);
            headerList = headerList.ConvertAll(header => header = header.ToLower());
            headerList.Sort();
            var strHeader = string.Join(",", headerList);
            var strTimestamp = timestamp.ToString();

            var concatenatedContent = string.Join(
                ";",
                new List<string> {
                    verb,
                    path,
                    strParam,
                    strHeader,
                    requestContent,
                    strTimestamp,
                    secretKey });
            Console.WriteLine("content: " + concatenatedContent);

            using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedContent));
                var computedSignature = Convert.ToBase64String(computedHash);
                Console.WriteLine("signature: " + computedSignature);
                return computedSignature;
            }
        }
    }
}
