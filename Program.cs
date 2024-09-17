using Discord;
using Discord.WebSocket;
using DotNetEnv;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DiscordBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //creates new Bot, and keeps the program running (infinite delay)
            var bot = new Bot();
            await bot.InitializeAsync();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                //save data when the bot is stopping
                bot.SaveData();
            };

            await Task.Delay(-1);
        }
    }
    public class Bot
    {
        //discord api boilerplate, websocket stuff
        private readonly DiscordSocketClient _client;
        private readonly HttpClient _httpClient;
        public Bot()

        {
            //defines bot intents, allows sending and receiving messages
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);
            //subscribes to log event and sends to LogAsync method
            _client.Log += LogAsync;
            //subscribes to message-received event and sends to MessageReceivedAsync method
            _client.MessageReceived += MessageReceivedAsync;
            _httpClient = new HttpClient();
        }
        //initialize bot
        public async Task InitializeAsync()
        {
            //loads .env library
            Env.Load();

            //retrieve token from .env
            string token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? throw new InvalidOperationException("Bot token is not set in the environment variables."); ;

            //error if token is empty
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Token not found! Make sure it's set in your .env file.");
                return;
            }
            //logs into Discord using token and starts bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // loads data when bot starts
            LoadData();
        }
        //prints logs to console
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
        //receives messages sent in server and returns methods based on the input command
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            //returns users name
            if (message.Content.StartsWith(">hello"))
            {
                await message.Channel.SendMessageAsync($"Hello, {message.Author.Username}!");
            }

            //allows user to roll for a waifu
            if (message.Content.StartsWith(">roll"))
            {
                WaifuRollResult result = GetRollResult();
                string imageUrl = await GetAnimeGirl();
                string rollName = await GetRandomRollName();
                Element element = GetRandomElement();  //add random element selection

                //save roll result for user
                SaveRollResult(message.Author.Id, result.StarRating, imageUrl, rollName, element);
                //get the element's emoji
                string elementEmoji = GetElementEmoji(element);
                //create an embed with the roll result and image, makes it prettier
                var embed = new EmbedBuilder()
                {
                    //title of the embed
                    Title = "〃￣ω￣〃ゞ\nWAIFU GET ",
                    //bold text for rarity
                    Description = $"You rolled a \n **{result.StarRating}**★ **{elementEmoji}** type **\"{rollName}\"** !!\n"
                                    + $"     she has...\n"
                                    + $"\n"
                                    + $"**ATK:** {result.Attack}\n"
                                    + $"**DEF:** {result.Defense}\n"
                                    + $"**SPD:** {result.Speed}",
                    //color of the embed sidebar
                    Color = Color.Gold,
                    //image URL
                    ImageUrl = imageUrl,
                }.Build();

                //send the embed message
                if (string.IsNullOrEmpty(imageUrl))
                {
                    await message.Channel.SendMessageAsync("Sorry, I couldn't fetch an anime girl image right now.");
                }
                else
                {
                    await message.Channel.SendMessageAsync(embed: embed);
                }
            }
            //returns users completed rolls from database
            if (message.Content.StartsWith(">myrolls"))
            {
                var rolls = GetUserRolls(message.Author.Id);

                if (rolls.Any())
                {
                    foreach (var roll in rolls)
                    {
                        //get the emoji for the element
                        string elementEmoji = GetElementEmoji(roll.Element);

                        //create an embed for each roll, showing rarity, roll name, and element emoji
                        var embed = new EmbedBuilder
                        {
                            Title = $"╮(︶︿︶)╭ \n    *{roll.RollName.ToUpper()}*",
                            Description = $"I'm a **{roll.Rarity}★** **{elementEmoji}** type with..\n"
                                        + $"**ATK:** {roll.Attack}\n"
                                        + $"**DEF:** {roll.Defense}\n"
                                        + $"**SPD:** {roll.Speed}",
                            Color = Color.Green,
                            ImageUrl = roll.ImageUrl
                        }.Build();

                        await message.Channel.SendMessageAsync(embed: embed);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You don't have any roll history.");
                }
            }
        }
        private WaifuRollResult GetRollResult()
        {
            Random random = new Random();
            //generate a random number between 1 and 100
            int roll = random.Next(1, 101);
            int starRating = roll switch
            {
                <= 40 => 1,   //40% chance for 1-star
                <= 65 => 2,   //25% chance for 2-star
                <= 80 => 3,   //15% chance for 3-star
                <= 90 => 4,   //10% chance for 4-star
                <= 95 => 5,   //5% chance for 5-star
                <= 98 => 6,   //3% chance for 6-star
                <= 99 => 7,   //1% chance for 7-star
                _ => 10       //1% chance for 10-star
            };

            //get random element
            Element element = (Element)random.Next(0, 3); //randomly choose between Water (0), Fire (1), Grass (2)

            int attack = random.Next(1, 101);
            int defense = random.Next(1, 101);
            int speed = random.Next(1, 101);

            return new WaifuRollResult
            {
                StarRating = starRating,
                WaifuElement = element,
                Attack = attack,
                Defense = defense,
                Speed = speed
            };
        }
        //gets pic of waifu from api
        private async Task<string> GetAnimeGirl()
        {
            string apiUrl = "https://api.waifu.pics/sfw/waifu";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);
            return json["url"]?.ToString() ?? "https://example.com/default-image.jpg";
        }
        private const string DataFilePath = "userRollData.json";
        private List<UserRollData> _userRollData = new List<UserRollData>();
        public void SaveData()
        {
            var json = JsonConvert.SerializeObject(_userRollData, Formatting.Indented);
            File.WriteAllText(DataFilePath, json);
        }
        private void LoadData()
        {
            if (File.Exists(DataFilePath))
            {
                var json = File.ReadAllText(DataFilePath);
                _userRollData = JsonConvert.DeserializeObject<List<UserRollData>>(json) ?? new List<UserRollData>();
            }
        }
        private List<UserRollEntry> GetUserRolls(ulong userId)
        {
            var userRollData = _userRollData.FirstOrDefault(d => d.UserId == userId);
            return userRollData?.RollEntries ?? new List<UserRollEntry>();
        }
        private void SaveRollResult(ulong userId, int rarity, string imageUrl, string rollName, Element element)
        {
            var userRollData = _userRollData.FirstOrDefault(d => d.UserId == userId);
            if (userRollData == null)
            {
                userRollData = new UserRollData { UserId = userId };
                _userRollData.Add(userRollData);
            }

            var rollResult = GetRollResult();

            userRollData.RollEntries.Add(new UserRollEntry
            {
                Rarity = rarity,
                ImageUrl = imageUrl,
                RollName = rollName,
                Element = element,
                Attack = rollResult.Attack,
                Defense = rollResult.Defense,
                Speed = rollResult.Speed
            });

            SaveData();
        }
        private async Task<string> GetRandomRollName()
        {
            try
            {
                //base API URL
                string baseUrl = "https://random-word-form.herokuapp.com";

                //fetch random adjective
                string adjectiveUrl = $"{baseUrl}/random/adjective";
                var adjectiveResponse = await _httpClient.GetStringAsync(adjectiveUrl);
                var adjective = JArray.Parse(adjectiveResponse)[0].ToString();

                //fetch random noun
                string nounUrl = $"{baseUrl}/random/noun";
                var nounResponse = await _httpClient.GetStringAsync(nounUrl);
                var noun = JArray.Parse(nounResponse)[0].ToString();

                //combine adjective and noun to form the roll name
                return $"{adjective.ToUpper()} {noun.ToUpper()}";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                return "DEFAULT NAME";
            }
        }
        private async Task<string> GetRandomWord(string type)
        {
            var apiUrl = $"https://random-word-api.herokuapp.com/all";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var json = JArray.Parse(response);
            return json.First?.ToString() ?? "Unknown";
        }
        public enum Element
        {
            Water,
            Fire,
            Grass
        }
        public static string GetElementEmoji(Element element)
        {
            return element switch
            {
                Element.Water => "💧",   // Water element emoji
                Element.Fire => "🔥",    // Fire element emoji
                Element.Grass => "🍃",   // Grass element emoji
                _ => ""  // Default to an empty string if something goes wrong
            };
        }
        private Element GetRandomElement()
        {
            Random random = new Random();
            int roll = random.Next(0, 3);  //generate a random number between 0 and 2

            return roll switch
            {
                0 => Element.Water,
                1 => Element.Fire,
                2 => Element.Grass,
                _ => Element.Water //fallback to Water as default
            };
        }
        public class UserRollData
        {
            public ulong UserId { get; set; }
            public List<UserRollEntry> RollEntries { get; set; } = new List<UserRollEntry>();
        }
        public class UserRollEntry
        {
            public int Rarity { get; set; }
            public required string ImageUrl { get; set; }
            public required string RollName { get; set; }
            public required Element Element { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int Speed { get; set; }
        }
        public class WaifuRollResult
        {
            public int StarRating { get; set; }
            public Element WaifuElement { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int Speed { get; set; }
        }
    }
}


