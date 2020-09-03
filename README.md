# BotFramework-Discord

## Dependencies

### Bot Framework Composer

https://github.com/microsoft/BotFramework-Composer

### Discord.Net

https://github.com/discord-net/Discord.Net

## Examples

### Tell Joke

Tell a joke about someone with his name replacement

## Features

### Cached Luis Recognizer

Cache Luis responses in LRU, so it will save your money.

### HandleGroupMentionMiddleware

Remove mention of bot and add mention of the user for group conversation.

## ToDos

### Bot singleton depends on service

https://github.com/microsoft/botbuilder-dotnet/issues/4521

### Multiple users in group conversation

#### How to tell the difference between shared conversation and private conversation in group conversation?

Make it changeable. Usually it is private conversation. When one decides to use some shared flow like playing a game, we could change the conversation type.

### Convert not mentioned group messages to events for statistics

### Save and load conversation state and history

### Change locale for conversation (if channel doesn't have it)

We can also play foreign language tests with this.

### Answer data querying from excel/website etc.

For item price, weapon damage etc.

### Proactive/time out prompt

Like time limited quiz
