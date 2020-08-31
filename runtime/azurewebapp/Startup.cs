// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Discord;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotFramework.Composer.Core;
using Microsoft.BotFramework.Composer.Core.Settings;
using Microsoft.BotFramework.Composer.CustomAction.CachedLuis;

//using Microsoft.BotFramework.Composer.CustomAction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.BotFramework.Composer.WebAppTemplates
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.HostingEnvironment = env;
            this.Configuration = configuration;
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }
        
        public void ConfigureTranscriptLoggerMiddleware(BotAdapter adapter, BotSettings settings)
        {
            if (ConfigSectionValid(settings.BlobStorage.ConnectionString) && ConfigSectionValid(settings.BlobStorage.Container))
            {
                adapter.Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
            }
        }

        public void ConfigureShowTypingMiddleWare(BotAdapter adapter, BotSettings settings)
        {
            if (settings.Feature.UseShowTypingMiddleware)
            {
                adapter.Use(new ShowTypingMiddleware());
            }
        }

        public void ConfigureInspectionMiddleWare(BotAdapter adapter, BotSettings settings, IStorage storage)
        {
            if (settings.Feature.UseInspectionMiddleware)
            {
                adapter.Use(new InspectionMiddleware(new InspectionState(storage)));
            }
        }

        public IStorage ConfigureStorage(BotSettings settings)
        {
            IStorage storage;
            if (ConfigSectionValid(settings.CosmosDb.AuthKey))
            {
                storage = new CosmosDbPartitionedStorage(settings.CosmosDb);
            }
            else
            {
                storage = new MemoryStorage();
            }

            return storage;
        }

        public BotFrameworkHttpAdapter GetBotAdapter(IStorage storage, BotSettings settings, UserState userState, ConversationState conversationState, IServiceProvider s, TelemetryInitializerMiddleware telemetryInitializerMiddleware)
        {
            var adapter = new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(this.Configuration));
            SetBotAdapter(adapter, storage, settings, userState, conversationState, s, telemetryInitializerMiddleware);            
            return adapter;
        }

        public void SetBotAdapter(BotAdapter adapter, IStorage storage, BotSettings settings, UserState userState, ConversationState conversationState, IServiceProvider s, TelemetryInitializerMiddleware telemetryInitializerMiddleware)
        {
            adapter
              .UseStorage(storage)
              .UseBotState(userState, conversationState)
              .Use(new RegisterClassMiddleware<IConfiguration>(Configuration))
              .Use(telemetryInitializerMiddleware);

            // Configure Middlewares
            ConfigureTranscriptLoggerMiddleware(adapter, settings);
            ConfigureInspectionMiddleWare(adapter, settings, storage);
            ConfigureShowTypingMiddleWare(adapter, settings);

            adapter.OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(exception.Message).ConfigureAwait(false);
                await conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                await conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
            };
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton<IConfiguration>(this.Configuration);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);

            var storage = ConfigureStorage(settings);
            services.AddSingleton(storage);
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            services.AddSingleton(userState);
            services.AddSingleton(conversationState);

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton<BotAdapter>(sp => (BotFrameworkHttpAdapter)sp.GetService<IBotFrameworkHttpAdapter>());

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton<AuthenticationConfiguration>();

            // register components.
            ComponentRegistration.Add(new DialogsComponentRegistration());
            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new QnAMakerComponentRegistration());

            // ComponentRegistration.Add(new LuisComponentRegistration());

            // This is for custom action component registration.
            //ComponentRegistration.Add(new CustomActionComponentRegistration());

            // Register the skills client and skills request handler.
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);
            services.AddSingleton<SkillConversationIdFactoryBase>(skillConversationIdFactory);
            services.AddHttpClient<BotFrameworkClient, SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, SkillHandler>();

            // Register telemetry client, initializers and middleware
            services.AddApplicationInsightsTelemetry(settings.ApplicationInsights.InstrumentationKey);
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
            services.AddSingleton<TelemetryLoggerMiddleware>(sp =>
            {
                var telemetryClient = sp.GetService<IBotTelemetryClient>();
                return new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: settings.Telemetry.LogPersonalInformation);
            });
            services.AddSingleton<TelemetryInitializerMiddleware>(sp =>
            {
                var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                var telemetryLoggerMiddleware = sp.GetService<TelemetryLoggerMiddleware>();
                return new TelemetryInitializerMiddleware(httpContextAccessor, telemetryLoggerMiddleware, settings.Telemetry.LogActivities);
            });

            var cachedLuisManager = new CachedLuisManager(settings.CachedLuis);

            // Configure bot loading path
            var botDir = settings.Bot;
            var resourceExplorer = new ResourceExplorer().AddFolder(botDir).RegisterType(LuisAdaptiveRecognizer.Kind, typeof(CachedLuisRecognizer), new CachedLuisLoader(cachedLuisManager));
            var rootDialog = GetRootDialog(botDir);

            var defaultLocale = Configuration.GetValue<string>("defaultLocale") ?? "en-us";

            services.AddSingleton(resourceExplorer);

            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>((s) => GetBotAdapter(storage, settings, userState, conversationState, s, s.GetService<TelemetryInitializerMiddleware>()));

            var removeRecipientMention = settings?.Feature?.RemoveRecipientMention ?? false;

            services.AddSingleton<IBot>((s) => new ComposerBot(
                    conversationState,
                    userState,
                    resourceExplorer,
                    s.GetService<BotFrameworkClient>(),
                    skillConversationIdFactory,
                    s.GetService<IBotTelemetryClient>(),
                    rootDialog,
                    defaultLocale,
                    removeRecipientMention));

            var discordAdapterOptions = new DiscordAdapterOptions
            {
                Token = settings.DiscordToken
            };
            services.AddSingleton((s) =>
            {
                var adapter = new DiscordAdapter(
                    discordAdapterOptions,
                    s.GetService<IBot>());
                SetBotAdapter(adapter, storage, settings, userState, conversationState, s, s.GetService<TelemetryInitializerMiddleware>());
                return adapter;
            });

            Task.Run(async () =>
            {
                var client = new HttpClient();
                await client.GetAsync(settings.HostUrl + "/discord");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });
        }

        private static bool ConfigSectionValid(string val)
        {
            return !string.IsNullOrEmpty(val) && !val.StartsWith('<');
        }

        private string GetRootDialog(string folderPath)
        {
            var dir = new DirectoryInfo(folderPath);
            foreach (var f in dir.GetFiles())
            {
                if (f.Extension == ".dialog")
                {
                    return f.Name;
                }
            }

            throw new Exception($"Can't locate root dialog in {dir.FullName}");
        }
    }
}
