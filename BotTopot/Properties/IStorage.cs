using VoiceTexterBot.Models;

namespace VoiceTexterBot.Services
{
    public interface IStorage
    {
        Session GetSession(long chatId);
    }
}