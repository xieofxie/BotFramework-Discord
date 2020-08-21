using Discord;
using Discord.WebSocket;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Builder.Community.Adapters.Discord
{
    public class DiscordAdapter : BotAdapter
    {
        private readonly DiscordAdapterOptions _options;
        private readonly DiscordSocketClient _client;
        private readonly DiscordMapper _mapper;
        private readonly IBot _bot;

        public DiscordAdapter(DiscordAdapterOptions discordAdapterOptions, IBot bot, DiscordMapper discordMapper = null, DiscordMapperOptions discordMapperOptions = null)
        {
            _options = discordAdapterOptions;
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _mapper = discordMapper ?? new DiscordMapper(discordMapperOptions ?? new DiscordMapperOptions());
            _bot = bot;

            _client.LoginAsync(TokenType.Bot, _options.Token).GetAwaiter().GetResult();
            _client.StartAsync().GetAwaiter().GetResult();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResourceResponse[0]);
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            _mapper.SetupSelf(_client.CurrentUser);
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // https://github.com/discord-net/Discord.Net/issues/1115
        private Task MessageReceivedAsync(SocketMessage message)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    if (message.Author.Id == _client.CurrentUser.Id)
                    {
                        return;
                    }

                    bool directMessage = false;
                    if (message.Channel is IDMChannel)
                    {
                        directMessage = true;
                    }
                    else if (message.MentionedUsers.All(user => user.Id != _client.CurrentUser.Id))
                    {
                        return;
                    }

                    var cancellationToken = default(CancellationToken);
                    var activity = _mapper.MessageToActivity(message, directMessage);
                    var context = new TurnContextEx(this, activity);
                    await RunPipelineAsync(context, _bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);
                    foreach (var sentActivity in context.SentActivities)
                    {
                        await _mapper.ActivityToMessageAsync(message, directMessage, sentActivity, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            return Task.CompletedTask;
        }
    }
}
