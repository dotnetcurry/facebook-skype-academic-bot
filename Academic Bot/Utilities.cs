using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Academic_Bot
{
    public static class Utilities
    {
        public static async Task<AcademicResult> Interpret(string query)
        {

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "SECRET");

            // Request parameters
            queryString["query"] = query;
            queryString["model"] = "latest";
            queryString["count"] = "10";
            queryString["complete"] = "1";
            var uri = "https://api.projectoxford.ai/academic/v1.0/interpret?" + queryString;

            var response = await client.GetAsync(uri);
            string resp = await response.Content.ReadAsStringAsync();
            AcademicResult result = JsonConvert.DeserializeObject<AcademicResult>(resp);
            return result;
        }

        public static async Task<EvaluateResult> Evaluate(string query)
        {

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "SECRET");

            // Request parameters
            queryString["expr"] = query;
            queryString["model"] = "latest";
            queryString["count"] = "10";
            queryString["attributes"] = "Id,E";
            var uri = "https://api.projectoxford.ai/academic/v1.0/evaluate?" + queryString;

            var response = await client.GetAsync(uri);
            string resp = await response.Content.ReadAsStringAsync();
            EvaluateResult result = JsonConvert.DeserializeObject<EvaluateResult>(resp);
            return result;
        }

        
    }

    public class Output
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Rule
    {
        public string name { get; set; }
        public Output output { get; set; }
    }

    public class Interpretation
    {
        public double logprob { get; set; }
        public string parse { get; set; }
        public List<Rule> rules { get; set; }
    }

    public class AcademicResult
    {
        public string query { get; set; }
        public List<Interpretation> interpretations { get; set; }
    }

    public class Result
    {
        public double logprob { get; set; }
        public long Id { get; set; }
        public string E { get; set; }
    }

    public class EvaluateResult
    {
        public string expr { get; set; }
        public List<Result> entities { get; set; }
    }

    public class S
    {
        public int Ty { get; set; }
        public string U { get; set; }
    }

    public class EX
    {
        public string DN { get; set; }
        public List<S> S { get; set; }
    }

    public class Item
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }
}