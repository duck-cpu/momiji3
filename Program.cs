using System.Threading.Tasks;

namespace momiji3
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Creates the bot and starts it
            var bot = new Bot();
            await bot.InitializeAsync();
            await Task.Delay(-1);  // Keeps the program running
        }
    }
}