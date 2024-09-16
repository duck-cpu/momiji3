using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DotNetEnv;
using System.Net.Http;
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
                bot.SaveData(); // Save data when the bot is stopping
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

            LoadData(); // Load data when bot starts
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

            //allows user to roll for stars
            if (message.Content.StartsWith(">roll"))
            {
                int rarity = GetRollResult();
                string imageUrl = await GetAnimeGirl();
                string rollName = GetRandomRollName();

                // save roll result for user
                SaveRollResult(message.Author.Id, rarity, imageUrl, rollName);

                // create an embed with the roll result and image, makes it prettier
                var embed = new EmbedBuilder()
                {
                    Title = "🪙 〃￣ω￣〃ゞ \n\nLUCKY GET ", // Title of the embed
                    Description = $"You rolled a **{rarity}★** {rollName}!!", // Bold text for rarity
                    Color = Color.Gold, // Color of the embed sidebar
                    ImageUrl = imageUrl, // Image URL
                    Footer = new EmbedFooterBuilder { Text = "(ノ= ⩊ = )ノ congratulations ✨ " } // Optional footer text        
                }.Build();

                // send the embed message
                if (string.IsNullOrEmpty(imageUrl))
                {
                    await message.Channel.SendMessageAsync("Sorry, I couldn't fetch an anime girl image right now.");
                }
                else
                {
                    await message.Channel.SendMessageAsync(embed: embed);
                }
            }
            if (message.Content.StartsWith(">myrolls"))
            {
                var rolls = GetUserRolls(message.Author.Id);
                if (rolls.Any())
                {
                    foreach (var roll in rolls)
                    {
                        var embedBuilder = new EmbedBuilder
                        {
                            Title = "╮(︶︿︶)╭ YOUR HISTORY",
                            Description = $"You rolled a **{roll.Rarity}★** **{roll.RollName}**",
                            Color = Color.Green,
                            ImageUrl = roll.ImageUrl,
                            Footer = new EmbedFooterBuilder { Text = "Roll History" }
                        };

                        var embed = embedBuilder.Build();
                        await message.Channel.SendMessageAsync(embed: embed);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You don't have any roll history.");
                }
            }
        }
        private int GetRollResult()
        {
            Random random = new Random();

            int roll = random.Next(1, 101); // Generate a random number between 1 and 100

            return roll switch
            {
                <= 40 => 1,   // 40% chance for 1-star
                <= 65 => 2,   // 25% chance for 2-star
                <= 80 => 3,   // 15% chance for 3-star
                <= 90 => 4,   // 10% chance for 4-star
                <= 95 => 5,   // 5% chance for 5-star
                <= 98 => 6,   // 3% chance for 6-star
                <= 99 => 7,   // 1% chance for 7-star
                _ => 10       // 1% chance for 10-star
            };
        }
        private async Task<string> GetAnimeGirl()
        {
            string apiUrl = "https://api.waifu.pics/sfw/waifu";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var json = JObject.Parse(response);
            return json["url"]?.ToString() ?? "https://example.com/default-image.jpg";
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
        private void SaveRollResult(ulong userId, int rollResult, string imageUrl, string rollName)
        {
            var userRollData = _userRollData.FirstOrDefault(d => d.UserId == userId);
            if (userRollData == null)
            {
                userRollData = new UserRollData { UserId = userId };
                _userRollData.Add(userRollData);
            }

            userRollData.RollEntries.Add(new UserRollEntry
            {
                Rarity = rollResult,
                ImageUrl = imageUrl,
                RollName = rollName
            });

            SaveData();
        }
        private string GetRandomRollName()
        {
            //will change to random names from api later
            var names = new[] { "Lucky", "Starry", "Shiny", "Mystic", "Glowing", "Radiant", "Sparkling", "Celestial", "Dazzling", "Epic" };
            Random random = new Random();
            return names[random.Next(names.Length)];
        }
    }
}


