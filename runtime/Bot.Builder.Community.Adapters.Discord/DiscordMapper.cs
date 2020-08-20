using Discord.WebSocket;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Builder.Community.Adapters.Discord
{
    public class DiscordMapper
    {
        private readonly DiscordMapperOptions _options;
        private ChannelAccount _channelAccount;

        public DiscordMapper(DiscordMapperOptions options)
        {
            _options = options;
        }

        public void SetupSelf(SocketUser socketUser)
        {
            _channelAccount = CreateChannelAccount(socketUser);
        }

        public Activity MessageToActivity(SocketMessage socketMessage)
        {
            var activity = new Activity();
            activity.ChannelId = _options.ChannelId;
            activity.Recipient = _channelAccount;
            activity.Conversation = new ConversationAccount(false, id: socketMessage.Channel.Id.ToString());
            activity.From = CreateChannelAccount(socketMessage.Author);
            activity.Id = socketMessage.Id.ToString();
            activity.Timestamp = socketMessage.Timestamp;
            activity.Locale = _options.Locale;

            activity.Type = ActivityTypes.Message;
            activity.Text = socketMessage.Content;
            return activity;
        }

        public async Task ActivityToMessageAsync(ISocketMessageChannel socketMessageChannel, Activity activity, CancellationToken cancellationToken)
        {
            await socketMessageChannel.SendMessageAsync(activity.Text).ConfigureAwait(false);
        }

        private ChannelAccount CreateChannelAccount(SocketUser socketUser)
        {
            return new ChannelAccount(socketUser.Id.ToString(), socketUser.Username);
        }
    }
}
