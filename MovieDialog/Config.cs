using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieDialog
{
    public enum ParseStatus { All, Movie, Artist, Director, Country, Genre, PublishDate, Rating, Duration };

    public enum KBQAEntityType { Movie, Celebrity, RecentMovie, IsPublish };

    class Config
    {
        public static string data_path
        {
            get
            {
                var configFileDir = ConfigurationManager.AppSettings["JiebaConfigFileDir"] ?? "Resources";
                if (!Path.IsPathRooted(configFileDir))
                {
                    var domainDir = AppDomain.CurrentDomain.BaseDirectory;
                    configFileDir = Path.GetFullPath(Path.Combine(domainDir, configFileDir));
                }
                return configFileDir + @"\";
            }
        }
        public static readonly string movie_filename = @"movie_name.csv";
        public static readonly string movieindeepdomain_filename = @"movie_list.csv";
        public static readonly string movie_nosplite_filename = @"movie_name_nosplit.csv";
        public static readonly string artist_filename = @"artist_name.csv";
        public static readonly string director_filename = @"director_name.csv";
        public static readonly string celebrityindeepdomain_filename = @"celebrity_list.csv";
        public static readonly string country_filename = @"country_name.csv";
        public static readonly string genre_filename = @"genre_name.csv";

        public static readonly string pattern_filename = @"QA_pattern.txt";
        public static readonly string patternQ_filename = @"QA_pattern_question.txt";
        public static readonly string patternA_filename = @"QA_pattern_answer.txt";

        public static readonly string cnnbased_classifier_endpoint = @"http://10.139.139.99:9005/query?q=";
        // ObjectStore
        //public static readonly string environment = "ObjectStoreMulti.Prod.HK.BingInternal.com:83/sds";
        public static readonly string environment = @"http://dict.bing.com.cn/hk/sds/ObjectStoreQuery/V1";
        public static readonly string osNamespace = "MsnJVFeeds";
        public static readonly string osSearchTable = "SnappsEntity";
        public static readonly string osColumnTable = "SnappsEntityColumn";
    }
    class Entity
    {
        private static Dictionary<ParseStatus, string> entity_tag = new Dictionary<ParseStatus, string>()
        {
            { ParseStatus.Movie,"nmovie" } ,
            { ParseStatus.Artist, "nrcelebrity" },
            { ParseStatus.Director, "nrcelebrity" },
            { ParseStatus.Country, "ncountry" },
            { ParseStatus.Genre, "ngenre" }
        };

        private static HashSet<string> postag_type = new HashSet<string>()
        {
            "nmovie",
            "nrcelebrity",
            "ncountry",
            "ngenre"
        };

        public static Dictionary<ParseStatus, string> EntityTag
        {
            get { return entity_tag; }
        }
        public static HashSet<string> PosTagType
        {
            get { return postag_type; }
        }
    }
}
