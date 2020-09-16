# BotFramework-Discord

## Dependencies

### Bot Framework Composer

https://github.com/microsoft/BotFramework-Composer

### Bot Framework SDK for .NET

https://github.com/microsoft/botbuilder-dotnet

### Discord.Net

https://github.com/discord-net/Discord.Net

## Examples

### Tell Joke

Tell a joke about someone with his name replacement

### Ask a Quiz

Ask a quiz for someone else

### Show Cats

The cats can't wait to see you!

### Guess Number

Play a group game in group.

## Features

### Cached Luis Recognizer

Cache Luis responses in LRU, so it will save your money.

### RecognizerSetWithPriority

Call recognizers in order like Regex -> Luis etc.

### HandleGroupMentionMiddleware

Remove mention of bot and add mention of the user for group conversation.

### InvokeProactiveActivity

Invoke a proactive activity to the bot for yourself or someone else.

### TextInputPlus

Plus timeout. Also wait for this https://github.com/microsoft/botbuilder-dotnet/issues/4555

### WriteStorage

Write to storage directly.

### ConversationUserState

It is changeable. Usually it is a private conversation. When one decides to use some shared flow like playing a game, one could change the conversation type by WriteStorage.

## ToDos

### Bot singleton depends on service

https://github.com/microsoft/botbuilder-dotnet/issues/4521

### Convert not mentioned group messages to events for statistics

### Save and load conversation state and history

### Change locale for conversation (if channel doesn't have it)

We can also play foreign language tests with this.

### Answer data querying from excel/website etc.

For item price, weapon damage etc.

### Invoke Proactive is a workaround

https://github.com/microsoft/botbuilder-dotnet/issues/4018

### How to handle &lt;at&gt;&lt;at/&gt; in utterance for Luis?
