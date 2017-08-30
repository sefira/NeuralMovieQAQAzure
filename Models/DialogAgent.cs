using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

using MovieDialog;

namespace MovieDomainWeb.Models
{
    public class DialogThreadInfo
    {
        public Thread dialog_thread;
        public string user_query;
        public List<string> dialog_history = new List<string>();
        public Semaphore bot_response_sem = new Semaphore(0, 1);
        public Semaphore user_request_sem = new Semaphore(0, 1);

        public DialogThreadInfo(Thread thread)
        {
            dialog_thread = thread;
        }
    }

    public class ResponseBody
    {
        public string UserID;
        public string Type;
        public string Content;
        public List<string> Answer;

        public ResponseBody(string userID, string type = "start", string content = "Success")
        {
            UserID = userID;
            Type = type;
            Content = content;
        }
    }

    public class DialogServer
    {
        private static DialogServer _instance = null;
        public static Dictionary<string, DialogThreadInfo> dialog_threads = new Dictionary<string, DialogThreadInfo>();

        private DialogServer()
        {
        }

        public static DialogServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DialogServer();
                }
                return _instance;
            }
        }

        public ResponseBody StartDialogThread(string thread_name)
        {
            var dialog_thread = new Thread(() => DialogThread(thread_name));
            dialog_thread.Start();
            dialog_threads[thread_name] = new DialogThreadInfo(dialog_thread);
            dialog_threads[thread_name].bot_response_sem.WaitOne();
            return new ResponseBody(thread_name);
        }

        private void DialogThread(string thread_name)
        {
            try
            {
                DialogManager movie_dialog = new DialogManager(thread_name);
                movie_dialog.DialogFlow(null);
            }
            catch (Exception e)
            {
                Utils.WriteError(e.ToString());
            }
            dialog_threads[thread_name].bot_response_sem.Release();
            return;
        }

        public ResponseBody EndDialogThread(string thread_name)
        {
            // stop dialog thread
            if (dialog_threads[thread_name].dialog_thread != null &&
                ((dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.WaitSleepJoin) == ThreadState.WaitSleepJoin) ||
                (dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.Running) == ThreadState.Running
                )
            {
                dialog_threads[thread_name].dialog_thread.Abort();
            }
            // remove thread from dictionary
            dialog_threads.Remove(thread_name);
            return new ResponseBody(thread_name, "end");
        }

        public ResponseBody SendQueryToDialogThread(string query, string thread_name)
        {
            // if a thread is not started, start it first
            if (!dialog_threads.ContainsKey(thread_name) || dialog_threads[thread_name].dialog_thread == null ||
                (
                ((dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.WaitSleepJoin) != ThreadState.WaitSleepJoin) &&
                ((dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.Running) != ThreadState.Running)
                )
                )
            {
                StartDialogThread(thread_name);
            }
            // response for a query
            ResponseBody response = new ResponseBody(thread_name);
            response.Type = "dialog";
            if (dialog_threads[thread_name].dialog_thread != null &&
                ((dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.WaitSleepJoin) == ThreadState.WaitSleepJoin) ||
                (dialog_threads[thread_name].dialog_thread.ThreadState & ThreadState.Running) == ThreadState.Running
                )
            {
                dialog_threads[thread_name].user_query = query;
                try
                {
                    dialog_threads[thread_name].user_request_sem.Release();
                    dialog_threads[thread_name].bot_response_sem.WaitOne();
                    response.Answer = dialog_threads[thread_name].dialog_history;
                }
                catch (Exception e)
                {
                    response.Content = "没有对话在进行， 你的输入太快了";
                }
                return response;
            }
            else
            {
                response.Content = "Dialog Thread Has Not Been Started Yet";
                return response;
            }
        }
    }
}