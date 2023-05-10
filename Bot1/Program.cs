using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bots.Extensions.Polling;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VoiceTexterBot.Controllers;
using VoiceTexterBot.Services;
using VoiceTexterBot.Configuration;

namespace VoiceTexterBot
{
    public class Program
    {
        public static async Task Main()
        {
            Console.OutputEncoding = Encoding.Unicode;
            
            // ������, ���������� �� ���������� ��������� ���� ����������
            var host = new HostBuilder()
                .ConfigureServices((hostContext, services) => ConfigureServices(services)) // ������ ������������
                .UseConsoleLifetime() // ��������� ������������ ���������� �������� � �������
                .Build(); // ��������

            Console.WriteLine("������ �������");
            // ��������� ������
            await host.RunAsync();
            Console.WriteLine("������ ����������");
        }

        static AppSettings BuildAppSettings()
        {
            return new AppSettings()
            {
                DownloadsFolder = "C:\\Users\\denma\\Downloads",
                BotToken = "6059424142:AAHg2eYcIX2q0cb8EWM97zmy9deUo2NOOKQ",
                AudioFileName = "audio",
                InputAudioFormat = "ogg",
                OutputAudioFormat = "wav",
            };
        }

        static void ConfigureServices(IServiceCollection services)
        {
            AppSettings appSettings = BuildAppSettings();
            services.AddSingleton(BuildAppSettings());
            services.AddSingleton<IFileHandler, AudioFileHandler>();
            services.AddSingleton<IStorage, MemoryStorage>();

            services.AddTransient<DefaultMessageController>();
            services.AddTransient<VoiceMessageController>();
            services.AddTransient<TextMessageController>();
            services.AddTransient<InlineKeyboardController>();
            // ������������ ������ TelegramBotClient c ������� �����������
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("6059424142:AAHg2eYcIX2q0cb8EWM97zmy9deUo2NOOKQ"));
            // ������������ ��������� �������� ������ ����
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

        private InlineKeyboardController _inlineKeyboardController;
        private TextMessageController _textMessageController;
        private VoiceMessageController _voiceMessageController;
        private DefaultMessageController _defaultMessageController;

        public Bot(
            ITelegramBotClient telegramClient,
            InlineKeyboardController inlineKeyboardController,
            TextMessageController textMessageController,
            VoiceMessageController voiceMessageController,
            DefaultMessageController defaultMessageController)
        {
            _telegramClient = telegramClient;
            _inlineKeyboardController = inlineKeyboardController;
            _textMessageController = textMessageController;
            _voiceMessageController = voiceMessageController;
            _defaultMessageController = defaultMessageController;
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
                await _inlineKeyboardController.Handle(update.CallbackQuery, cancellationToken);
                return;
            }

            // ������������ �������� ��������� �� Telegram Bot API: https://core.telegram.org/bots/api#message
            if (update.Type == UpdateType.Message)
            {
                switch (update.Message!.Type)
                {
                    case MessageType.Voice:
                        await _voiceMessageController.Handle(update.Message, cancellationToken);
                        return;
                    case MessageType.Text:
                        await _textMessageController.Handle(update.Message, cancellationToken);
                        return;
                    default:
                        await _defaultMessageController.Handle(update.Message, cancellationToken);
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
            Console.WriteLine("������� 10 ������ ����� ��������� ������������.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}
