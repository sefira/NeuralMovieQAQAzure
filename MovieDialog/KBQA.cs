using ChinaOpalSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieDialog
{
    class KBQAAnswerForm
    {
        public bool success = false;
        public string answer = "";
        public bool need_xiaoice = false;
        public KBQAAnswerForm(bool succ, string ans, bool is_need)
        {
            success = succ;
            answer = ans;
            need_xiaoice = is_need;
        }
    }

    class KBQA
    {
        private static PatternBased pattern_qa = new PatternBased();
        private static CNNBased cnn_qa = new CNNBased();
        //GraphEngineQuery graphengine_query = new GraphEngineQuery();

        public static KBQAAnswerForm DoKBQA(Query query, Parser parser)
        {
            parser.ParseAllTag(ref query);
            PatternResponse pattern_response;
            if (pattern_qa.QuestionClassify(query, out pattern_response))// || cnn_qa.QuestionClassify(query, out pattern_response))
            {
                Console.WriteLine("Start to KBQA");
                string question_topic = "";
                try
                {
                    switch (pattern_response.entity_type)
                    {
                        case KBQAEntityType.Movie:
                            question_topic = query.carried_movie[0];
                            break;
                        case KBQAEntityType.Celebrity:
                            question_topic = (query.carried_artist.Count > 0) ? query.carried_artist[0] : query.carried_director[0];
                            break;
                        case KBQAEntityType.RecentMovie:
                            question_topic = "";
                            break;
                        case KBQAEntityType.IsPublish:
                            question_topic = query.carried_movie[0];
                            break;
                    }
                    //List<object> res = graphengine_query.GetGraphEngineData(question_topic, pattern_response.property, pattern_response.hop_num);
                    List<string> res = GetColumnData(pattern_response.entity_type, question_topic, pattern_response.property);
                    res = res.Distinct().ToList();
                    string answer = string.Join(",", res.ToArray());
                    if (answer.Length < 2)
                    {
                        return new KBQAAnswerForm(false, "数据库中没有相关的答案...", true);
                    }
                    else
                    {
                        return new KBQAAnswerForm(true, answer, false);
                    }
                }
                catch (Exception e)
                {
                    return new KBQAAnswerForm(false, "It seems Neural Network makes a mistake", true);
                }
            }
            else
            {
                return new KBQAAnswerForm(false, "It seems Neural Network makes a mistake", false);
            }
        }

        public static List<string> GetColumnData(KBQAEntityType topic_type, string topic, string property)
        {
            List<string> ret = new List<string>();
            // filter which column
            string query_filter = "";
            string result_filter = "";

            string[] edge_property_arr = property.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            property = edge_property_arr[0];
            string rank_policy = "";
            switch (topic_type)
            {
                case KBQAEntityType.Celebrity:
                    {
                        if ("Act".Equals(property))
                        {
                            query_filter = $@"(#:""{topic}Artists "")";
                        }
                        else
                        {
                            query_filter = $@"(#:""{topic}Directors "")";
                        }
                        result_filter = "Name";
                        break;
                    }
                case KBQAEntityType.Movie:
                    {
                        query_filter = $@"(#:""{topic}Name "")";
                        result_filter = property;
                        break;
                    }
                case KBQAEntityType.RecentMovie:
                    {
                        query_filter = "";
                        rank_policy = "recent";
                        result_filter = property;
                        break;
                    }
                case KBQAEntityType.IsPublish:
                    {
                        query_filter = $@"(#:""{topic}Name "")";
                        result_filter = property;
                        List<SnappsEntity> temp_res = (List<SnappsEntity>)SearchObjectStoreClient.Query(query_filter, rank_policy);
                        int datetime = 0;
                        try
                        {
                            ret = ParseResult(temp_res, result_filter);
                            datetime = int.Parse(ret[0]);
                            string publish_date = DateTime.ParseExact(ret[0], "yyyyMMdd", null).ToString("yyyy年MM月dd日");
                            return int.Parse(DateTime.Now.ToString("yyyyMMdd")) >= datetime ? 
                                new List<string> { $"上映了，上映时间是{publish_date}" } : new List<string> { $"还没上映呢，上映时间是{publish_date}" };
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine($"Exception caught: {e}");
                            Utils.WriteError("Column Table has not this record!");
                        }
                        return new List<string>();
                    }
            }

            List<SnappsEntity> res = (List<SnappsEntity>)SearchObjectStoreClient.Query(query_filter, rank_policy);
            try
            {
                ret = ParseResult(res, result_filter);
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Exception caught: {e}");
                Utils.WriteError("Column Table has not this record!");
            }
            return ret;
        }

        static List<string> ParseResult(List<SnappsEntity> input, string property)
        {
            List<string> res = new List<string>();
            if (input.Count() <= 0)
            {
                return res;
            }
            switch (property)
            {
                case "PublishDate":
                    res.Add(input[0].PublishDate.ToString());
                    break;
                case "Rating":
                    res.Add(input[0].Rating.ToString());
                    break;
                case "Genres":
                    res = input[0].Entment.Genres;
                    break;
                case "Country":
                    res = input[0].Geographies;
                    break;
                case "Description":
                    res.Add(input[0].Description);
                    break;
                case "Artists":
                    res = input[0].Entment.Artists;
                    break;
                case "Directors":
                    res = input[0].Entment.Directors;
                    break;
                case "Name":
                    foreach (var item in input)
                    {
                        res.Add(item.Name);
                    }
                    break;
            }
            return res;
        }
    }
}
