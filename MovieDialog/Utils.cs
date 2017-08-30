using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MovieDomainWeb.Models;

namespace MovieDialog
{
    class Utils
    {
        public static string ReadLine(string thread_name)
        {
            string query = "";
            while (string.IsNullOrWhiteSpace(query))
            {
                // before bot waiting for http, bot must return all response
                // so http server can fetch data
                DialogServer.dialog_threads[thread_name].bot_response_sem.Release();
                // wait http server get user query
                DialogServer.dialog_threads[thread_name].user_request_sem.WaitOne();
                query = DialogServer.dialog_threads[thread_name].user_query;
                Console.WriteLine(query);
            }
            return query;
        }

        public static void WriteQuery(string thread_name, string str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.ResetColor();
            DialogServer.dialog_threads[thread_name].dialog_history.Add($"User: {str}");
        }

        public static void WriteMachine(string thread_name, string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ResetColor();
            DialogServer.dialog_threads[thread_name].dialog_history.Add($"Bot: {str}");
        }

        public static void WriteResult(string str)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(str);
            Console.ResetColor();
        }

        public static void WriteUnknow(string thread_name, string str, string query)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(str);
            string xiaoice = XiaoIce.XiaoIceResponse(query);
            Console.WriteLine(xiaoice);
            Console.ResetColor();
            if (string.IsNullOrWhiteSpace(xiaoice) || xiaoice.Length <= 1)
            {
                xiaoice = str;
            }
            //DialogHttpServer.dialog_history.Add($"Bot-Unknow: {str}");
            DialogServer.dialog_threads[thread_name].dialog_history.Add($"Bot: {xiaoice}");
        }

        public static void WriteError(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }
}
