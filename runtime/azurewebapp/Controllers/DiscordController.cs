// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Discord;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.BotFramework.Composer.WebAppTemplates.Controllers
{
    // TODO due to dependency, use this to initialize
    [Route("discord")]
    [ApiController]
    public class DiscordController : ControllerBase
    {
        private readonly DiscordAdapter _adapter;

        public DiscordController(DiscordAdapter adapter)
        {
            this._adapter = adapter;
        }

        [HttpPost]
        [HttpGet]
        public async Task PostAsync()
        {
            await Response.WriteAsync("OK").ConfigureAwait(false);
        }
    }
}
