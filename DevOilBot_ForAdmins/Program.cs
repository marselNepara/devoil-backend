// Program.cs
using DevOilBot_ForAdmins;
using Microsoft.Extensions.Configuration;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        // Загружаем конфигурацию
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables() // 🔹 Поддержка .env / Docker environment
            .Build();

        // Получаем токен и URL
        string botToken = config["BOT_TOKEN"] ?? config["BotToken"];
        string apiUrl = config["API_URL"] ?? config["ApiUrl"];
        long[] adminIds = config.GetSection("AdminIds").Get<long[]>();

        if (string.IsNullOrEmpty(botToken))
        {
            Console.WriteLine("❌ Bot token не найден (BOT_TOKEN или BotToken)");
            return;
        }

        if (adminIds == null || adminIds.Length == 0)
        {
            Console.WriteLine("⚠️ Не найдены AdminIds, продолжаем без них...");
            adminIds = Array.Empty<long>();
        }

        var host = new Host(botToken, adminIds, apiUrl);
        host.StartBot();

        Console.WriteLine("✅ Бот запущен. Нажмите Ctrl+C для выхода...");
        await Task.Delay(-1);
    }
}
