using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bots.Extensions.Polling;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BotTopot
{
        static class Program
        {
            public static async Task Main()
            {
                Console.OutputEncoding = Encoding.Unicode;

                var host = new HostBuilder()
                    .ConfigureServices((hostContext, services) => ConfigureServices(services))
                    .UseConsoleLifetime()
                    .Build();

                Console.WriteLine("Starting Service");
                await host.RunAsync();
                Console.WriteLine("Service stopped");
            }

            static void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("6115840099:AAE7b7qAkvKCpL8sJDU_IHWuOgTszoi-KHQ"));
                services.AddHostedService<Bot>();
            }
        }
    
    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions() { AllowedUpdates = { } }, // ����� ��������, ����� ���������� ����� ��������. � ������ ������ ��������� ���
                cancellationToken: stoppingToken);

            Console.WriteLine("��� �������");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //  ������������ ������� �� ������  �� Telegram Bot API: https://core.telegram.org/bots/api#callbackquery
            if (update.Type == UpdateType.CallbackQuery)
            {
                await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, "�� ������ ������", cancellationToken: cancellationToken);
                return;
            }

            // ������������ �������� ��������� �� Telegram Bot API: https://core.telegram.org/bots/api#message
            if (update.Type == UpdateType.Message)
            {
                switch (update.Message!.Type)
                {
                    case MessageType.Text:
                        await _telegramClient.SendTextMessageAsync(update.Message.From.Id, $"����� ���������: {update.Message.Text.Length} ������", cancellationToken: cancellationToken);
                        return;
                    default: // unsupported message
                        await _telegramClient.SendTextMessageAsync(update.Message.From.Id, $"������ ��� ��������� �� ��������������. ���������� ��������� �����.", cancellationToken: cancellationToken);
                        return;
                }
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            Console.WriteLine("Waiting 10 seconds before retry");
            Thread.Sleep(10000);
            return Task.CompletedTask;
        }
    }
}
