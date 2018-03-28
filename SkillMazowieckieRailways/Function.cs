using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SkillMazowieckieRailways
{
    public class Function
    {

        public const string INVOCATION_NAME = "Mazowieckie Railways";
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest) && (input.Request as IntentRequest).Intent.Name == "GetCurrentTrain")
            {
                var trains = await GetTrains(context);
                if (trains.Count == 2)
                {
                    return MakeSkillResponse($"Train to Warsaw is at {trains[0]}.", true);
                }
                if (trains.Count == 1)
                {
                    return MakeSkillResponse($"Train to Warsaw is at {trains[0]}. Looks like this is last one today.", true);
                }
                if (trains.Count == 0)
                {
                    return MakeSkillResponse($"Today there is no more trains to Warsaw.", true);
                }
            }
            else if (requestType == typeof(IntentRequest) && (input.Request as IntentRequest).Intent.Name == "GetCurrentAndNextTrain")
            {
                var trains = await GetTrains(context);
                if (trains.Count == 0)
                {
                    return MakeSkillResponse($"Today there is no more trains to Warsaw.", true);
                }
                if (trains.Count == 1)
                {
                    return MakeSkillResponse($"Train to Warsaw is at {trains[0]}. Looks like this is last one today.", true);
                }
                if (trains.Count == 2)
                {
                    return MakeSkillResponse($"Train to Warsaw is at {trains[0]}. Next is at {trains[1]}", true);
                }
            }
            return MakeSkillResponse($"I don't know how to handle this intent. Please say something like Alexa, ask {INVOCATION_NAME} when next train to Warsaw.", true);
        }

        private async Task<List<string>> GetTrains(ILambdaContext context)
        {
            var polishDate = DateTime.Now.AddHours(6);
            //var polishTime = await GetPolishTime(context);

            var date = polishDate.ToString("yyyy-MM-dd");
            var hours = polishDate.Hour;
            var minutes = polishDate.Minute;
            List<string> trains = new List<string>();

            string req =
                $"http://www.mazowieckie.com.pl/pl/jsearch?station_from=Dobczyn&station_from_id=36384&station_to=Warszawa+Wile%C5%84ska&station_to_id=37440&date={date}&hour={hours}%3A{minutes}&by%5Bstation_by%5D%5B0%5D=&by%5Bstation_by_id%5D%5B0%5D=&op=Szukaj";
            var uri = new Uri(req);
            try
            {
                var _httpClient = new HttpClient();
                var response = await _httpClient.GetStringAsync(uri);
                Regex regex = new Regex("<span class=\"top\">Odjazd</span><div class=\"center\">(\\d{1,2}:\\d{1,2})</div>");
                var matches = regex.Matches(response);
                int c = 0;
                foreach (Match m in matches)
                {
                    trains.Add(m.Groups[1].Value);
                    c++;
                    if (c == 2)
                    {
                        break;
                    }
                }
                return trains;
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"\nException: {ex.Message}");
                throw;
            }

            return trains;
        }       

        private SkillResponse MakeSkillResponse(string outputSpeech, bool shouldEndSession, string repromptText = "Just say, when is train to Warsaw. To exit, say, exit.")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            return skillResponse;
        }

        private async Task<(string hour, string minutes)> GetPolishTime(ILambdaContext context)
        {
            try
            {
                var _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36");
                _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");
                _httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                _httpClient.DefaultRequestHeaders.Add("Host", "www.worldtimeserver.com");
                _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                var uri = new Uri("https://www.worldtimeserver.com/current_time_in_PL.aspx");
                var response = await _httpClient.GetStringAsync(uri);
                Regex re = new Regex(
                    "<span id=\"theTime\" class=\"fontTS\">(?:\\s*?)(\\d\\d:\\d\\d:\\d\\d)(?:\\s*?)</span>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
                var m = re.Match(response);
                string time = m.Groups[1].Value;
                var split = time.Split(':');
                return (hour: split[0], minutes: split[1]);
                //return (hour: "14", minutes: "20");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"\nException: {ex.Message}");
                throw;
            }
        }
    }
}
