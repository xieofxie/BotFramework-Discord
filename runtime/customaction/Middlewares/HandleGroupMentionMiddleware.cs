using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Middlewares
{
    public class HandleGroupMentionMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Conversation.IsGroup == true)
            {
                turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
                {
                    foreach (var activity in activities)
                    {
                        var text = $"<at>{activity.Recipient.Name}</at>";
                        var mention = new Mention(activity.Recipient, text, "mention");
                        activity.Text = text + activity.Text;
                        if (activity.Entities == null)
                        {
                            activity.Entities = new List<Entity>();
                        }
                        activity.Entities.Add(mention);
                    }

                    // run full pipeline
                    var responses = await nextSend().ConfigureAwait(false);
                    return responses;
                });

                turnContext.Activity.RemoveRecipientMention();
            }

            await next(cancellationToken);
        }
    }
}
