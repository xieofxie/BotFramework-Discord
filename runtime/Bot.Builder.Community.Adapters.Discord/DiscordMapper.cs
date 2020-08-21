using AdaptiveCards;
using AdaptiveCards.Rendering;
using AdaptiveCards.Rendering.Wpf;
using Discord.WebSocket;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Builder.Community.Adapters.Discord
{
    public class DiscordMapper
    {
        private readonly DiscordMapperOptions _options;
        private readonly AdaptiveHostConfig _adaptiveHostConfig;
        private ChannelAccount _channelAccount;
        private string[] _mentionStrings;

        public DiscordMapper(DiscordMapperOptions options)
        {
            _options = options;
            // Create a host config with no interactivity 
            // (buttons in images would be deceiving)
            _adaptiveHostConfig = new AdaptiveHostConfig()
            {
                SupportsInteractivity = false
            };
        }

        public void SetupSelf(SocketUser socketUser)
        {
            _channelAccount = CreateChannelAccount(socketUser);

            // https://discord.com/developers/docs/reference#message-formatting-formats
            _mentionStrings = new string[] { $"<@{socketUser.Id}>", $"<@!{socketUser.Id}>" };
        }

        public virtual Activity MessageToActivity(SocketMessage socketMessage, bool directMessage)
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
            if (!directMessage)
            {
                foreach (var mention in _mentionStrings)
                {
                    activity.Text = activity.Text.Replace(mention, string.Empty);
                }

                activity.Text = activity.Text.Trim();
            }
            return activity;
        }

        public async Task ActivityToMessageAsync(SocketMessage socketMessage, bool directMessage, Activity activity, CancellationToken cancellationToken)
        {
            if (activity.Type != ActivityTypes.Message)
            {
                return;
            }

            var text = activity.Text;
            if (!directMessage)
            {
                text = $"<@{socketMessage.Author.Id}> {text}";
            }

            bool hasAdaptiveCard = false;
            int i = 0;
            for (;i < activity.Attachments.Count; ++i)
            {
                var stream = await ConvertAsync(activity.Attachments[i], cancellationToken);
                await socketMessage.Channel.SendFileAsync(stream, $"Attachment {i}.png", text).ConfigureAwait(false);
                hasAdaptiveCard = true;
                ++i;
                break;
            }

            if (hasAdaptiveCard)
            {
                for (;i < activity.Attachments.Count;++i)
                {
                    var stream = await ConvertAsync(activity.Attachments[i], cancellationToken);
                    await socketMessage.Channel.SendFileAsync(stream, $"Attachment {i}.png").ConfigureAwait(false);
                }
            }
            else
            {
                await socketMessage.Channel.SendMessageAsync(text).ConfigureAwait(false);
            }
        }

        private ChannelAccount CreateChannelAccount(SocketUser socketUser)
        {
            return new ChannelAccount(socketUser.Id.ToString(), socketUser.Username);
        }

        private async Task<Stream> ConvertAsync(Attachment attachment, CancellationToken cancellationToken)
        {
            if (attachment.ContentType == AdaptiveCard.ContentType)
            {
                // https://docs.microsoft.com/en-us/adaptive-cards/sdk/rendering-cards/net-image/render-a-card
                AdaptiveCard card = null;
                if (attachment.Content is AdaptiveCard adaptiveCard)
                {
                    card = adaptiveCard;
                }
                else if (attachment.Content is JToken jtoken)
                {
                    AdaptiveCardParseResult parseResult = AdaptiveCard.FromJson(jtoken.ToString());
                    card = parseResult.Card;
                }
                else
                {
                    throw new NotImplementedException($"{attachment.Content.GetType().Name} is not supported yet!");
                }

                // TODO don't know if it is thread safe or not
                var adaptiveRenderer = new AdaptiveCardRenderer(_adaptiveHostConfig);

                // Set any XAML resource Dictionary if you have one
                //renderer.ResourcesPath = <path-to-your-resourcedictionary.xaml>;

                // Render the card to png
                // Set createStaThread to true if running from a server
                var renderedCard = await adaptiveRenderer.RenderCardToImageAsync(card, createStaThread: true, cancellationToken: cancellationToken);
                return renderedCard.ImageStream;
            }
            else
            {
                throw new NotSupportedException($"{attachment.ContentType} is not supported yet!");
            }
        }
    }
}
