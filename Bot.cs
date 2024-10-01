using Discord;
using Discord.WebSocket;
using DotNetEnv;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using momiji3.Models;
using momiji3.Utility;

namespace momiji3
{
    public class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClient _httpClient;

        public Bot()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            Env.Load();
            string token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? throw new InvalidOperationException("Bot token is not set in the environment variables.");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            Database.InitializeDatabase();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            //ensures commands with this tag can only be used in servers
            var guild = (message.Channel as IGuildChannel)?.Guild;
            if (guild == null)
            {
                await message.Channel.SendMessageAsync("This command can only be used in a server.");
                return;
            }

            //initialize munny balance
            await InitializeUserBalance(message.Author.Id);

            //returns users name
            if (message.Content.StartsWith(">hello"))
            {
                await message.Channel.SendMessageAsync($"Hello, {message.Author.Username}!");
            }
            //allows user to roll for a character
            if (message.Content.StartsWith(">roll"))
            {
                string userId = message.Author.Id.ToString();
                const int rollCost = 100;
                //check if user has enough munny
                if (Database.GetUserBalance(userId) < rollCost)
                {
                    await message.Channel.SendMessageAsync("You do not have enough munny to roll. You need at least 100 munny.");
                    return;
                }

                //deduct munny
                if (!Database.DeductUserBalance(userId, rollCost))
                {
                    await message.Channel.SendMessageAsync("Failed to deduct munny. Please try again.");
                    return;
                }

                RollResult result = GetRollResult();
                string imageUrl = await BotHelper.GetChar(_httpClient);
                string rollName = await BotHelper.GetRandomRollName(_httpClient);

                Random random = new Random();
                Element element = (Element)random.Next(0, 3); //add random element selection

                //save roll result for user
                Database.InsertUserRoll(message.Author.Id.ToString(), result.StarRating, imageUrl, rollName, element.ToString(), result.Attack, result.Defense, result.Speed, message.Author.Id.ToString());
                //get the element's emoji
                string elementEmoji = BotHelper.GetElementEmoji(element);
                //create an embed with the roll result and image, makes it prettier
                var embed = new EmbedBuilder()
                {
                    //title of the embed
                    Title = "LUCKY GET",
                    //bold text for rarity
                    Description = $"You rolled a \n **{result.StarRating}**★ **{elementEmoji}** type **\"{rollName}\"** !!\n"
                                    + $"     it has...\n"
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
                    await message.Channel.SendMessageAsync("Sorry, I couldn't fetch an image right now.");
                }
                else
                {
                    await message.Channel.SendMessageAsync(embed: embed);
                }
            }
            //returns users completed rolls from database
            if (message.Content.StartsWith(">myrolls"))
            {
                var rolls = Database.GetUserRolls(message.Author.Id.ToString(), guild.Id.ToString());

                if (rolls.Any())
                {
                    foreach (var roll in rolls)
                    {
                        //get the emoji for the element
                        string elementEmoji = BotHelper.GetElementEmoji(roll.Element);

                        //TODO: define this variable somewhere else, used again in a different function
                        //get the user by their ID
                        var owner = await guild.GetUserAsync(ulong.Parse(roll.OwnerId));
                        string ownerName = owner?.Nickname ?? owner?.Username ?? "Unknown";

                        //create an embed for each roll, showing rarity, roll name, and element emoji
                        var embed = new EmbedBuilder
                        {
                            Title = $"*{roll.Id}*\n    *{roll.RollName.ToUpper()}*",
                            Description = $"I'm a **{roll.Rarity}★** **{elementEmoji}** type with..\n"
                                        + $"**ATK:** {roll.Attack}\n"
                                        + $"**DEF:** {roll.Defense}\n"
                                        + $"**SPD:** {roll.Speed}",
                            Color = Color.Green,
                            ImageUrl = roll.ImageUrl,
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"owned by {ownerName}"
                            }
                        }.Build();

                        await message.Channel.SendMessageAsync(embed: embed);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You don't have any roll history.");
                }
            }
            if (message.Content.StartsWith(">query"))
            {
                string queryParams = message.Content.Substring(">query".Length).Trim();

                if (string.IsNullOrWhiteSpace(queryParams))
                {
                    await message.Channel.SendMessageAsync("Please provide a query parameter.");
                    return;
                }

                var queryResult = Database.QueryDatabase(queryParams);

                if (queryResult.Any())
                {
                    foreach (var entry in queryResult)
                    {
                        string elementEmoji = BotHelper.GetElementEmoji(entry.Element);

                        //TODO: define this variable somewhere else, used again in a different function
                        var owner = await guild.GetUserAsync(ulong.Parse(entry.OwnerId));
                        string ownerName = owner?.Nickname ?? owner?.Username ?? "Unknown";

                        var embed = new EmbedBuilder
                        {
                            Title = $" \n    *{entry.RollName.ToUpper()}*",
                            Description = $"I'm a **{entry.Rarity}★** **{elementEmoji}** type with..\n"
                                        + $"**ATK:** {entry.Attack}\n"
                                        + $"**DEF:** {entry.Defense}\n"
                                        + $"**SPD:** {entry.Speed}",
                            Color = Color.Blue,
                            ImageUrl = entry.ImageUrl,
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"owned by {ownerName}"
                            }
                        }.Build();

                        await message.Channel.SendMessageAsync(embed: embed);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("No results found for your query.");
                }
            }
            if (message.Content.StartsWith(">balance"))
            {
                int balance = Database.GetUserBalance(message.Author.Id.ToString());
                await message.Channel.SendMessageAsync($"You currently have **{balance}** munny.");
            }
        }

        private async Task InitializeUserBalance(ulong userId)
        {
            if (Database.GetUserBalance(userId.ToString()) == 0)
            {
                await Task.Run(() => Database.SetUserBalance(userId.ToString(), 500));
            }
        }
        private RollResult GetRollResult()
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

            return new RollResult
            {
                StarRating = starRating,
                Attack = attack,
                Defense = defense,
                Speed = speed
            };
        }
    }
}
