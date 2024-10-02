using Newtonsoft.Json.Linq;
using momiji3.Models;

namespace momiji3.Utility
{
    public static class BotHelper
    {
        public static async Task<string> GetChar(HttpClient httpClient)
        {
            string apiUrl = "https://api.waifu.pics/sfw/waifu";
            var response = await httpClient.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);
            return json["url"]?.ToString() ?? "https://example.com/default-image.jpg";
        }

        public static async Task<string> GetRandomRollName(HttpClient httpClient)
        {
            try
            {
                string baseUrl = "https://random-word-form.herokuapp.com";
                string adjectiveUrl = $"{baseUrl}/random/adjective";
                var adjectiveResponse = await httpClient.GetStringAsync(adjectiveUrl);
                string adjective = JArray.Parse(adjectiveResponse)[0].ToString();

                string nounUrl = $"{baseUrl}/random/noun";
                var nounResponse = await httpClient.GetStringAsync(nounUrl);
                string noun = JArray.Parse(nounResponse)[0].ToString();

                return $"{adjective.ToUpper()} {noun.ToUpper()}";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                return "DEFAULT NAME";
            }
        }

        public static string GetElementEmoji(Element element)
        {
            return element switch
            {
                Element.Water => "💧",
                Element.Fire => "🔥",
                Element.Grass => "🍃",
                _ => ""
            };
        }
        public static string GetStars(int starRating)
        {
            string starSymbol = "";
            if (starRating >= 1 && starRating <= 3)
            {
                starSymbol = "⭐️";
            }
            else if (starRating >= 4 && starRating <= 6)
            {
                starSymbol = "🌟";
            }
            else if (starRating >= 7 && starRating <= 9)
            {
                starSymbol = "✨";
            }
            else if (starRating >= 10 && starRating <= 12)
            {
                starSymbol = "💫";
            }
            else if (starRating == 13)
            {
                starSymbol = "💎";
            }

            return new string(starSymbol[0], starRating);
        }
    }
}
