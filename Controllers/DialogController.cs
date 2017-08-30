using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.IO;
using System.Web;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

using MovieDomainWeb.Models;

namespace MovieDomainWeb.Controllers
{
    public class RequestBody
    {
        public string UserID;
        public string Query;
    }

    public class DialogController : ApiController
    {
        [Route("api/dialog")]
        [HttpPost]
        public async Task<ResponseBody> WorkWithMovieDialog()
        {
            var buffStream = new MemoryStream();
            await HttpContext.Current.Request.InputStream.CopyToAsync(buffStream, 1024 * 1024);
            var contentBytes = buffStream.ToArray();
            var encoding = HttpContext.Current.Request.ContentEncoding ?? Encoding.UTF8;
            var content = encoding.GetString(contentBytes);
            var request_body = JsonConvert.DeserializeObject<RequestBody>(content);

            // Note: The GetContext method blocks while waiting for a request. 
            ResponseBody response;
            switch (request_body.Query.ToLower())
            {
                case "start":
                    response = DialogServer.Instance.StartDialogThread(request_body.UserID);
                    break;

                case "end":
                    response = DialogServer.Instance.EndDialogThread(request_body.UserID);
                    break;

                default:
                    response = DialogServer.Instance.SendQueryToDialogThread(request_body.Query, request_body.UserID);
                    break;
            }
            return response;
        }
    }
}
