using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamsFx.Conversation;

[assembly: FunctionsStartup(typeof(GitHubNotifications.Startup))]

namespace GitHubNotifications
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false);

            // Prepare Configuration for ConfigurationBotFrameworkAuthentication
            var configuration = builder.ConfigurationBuilder.Build();
            builder.ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "MicrosoftAppType", "MultiTenant" },
                { "MicrosoftAppId", configuration.GetSection("BOT_ID")?.Value ?? string.Empty },
                { "MicrosoftAppPassword", configuration.GetSection("BOT_PASSWORD")?.Value ?? string.Empty },
            });
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Cloud Adapter with error handling enabled.
            // Note: some classes expect a BotAdapter and some expect a BotFrameworkHttpAdapter, so
            // register the same adapter instance for all types.
            builder.Services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
            builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetService<CloudAdapter>());
            builder.Services.AddSingleton<BotAdapter>(sp => sp.GetService<CloudAdapter>());

            // Create the Conversation with notification feature enabled.
            builder.Services.AddSingleton(sp =>
            {
                var options = new ConversationOptions()
                {
                    Adapter = sp.GetService<CloudAdapter>(),
                    Notification = new NotificationOptions
                    {
                        BotAppId = configuration["MicrosoftAppId"],
                    },
                };

                return new ConversationBot(options);
            });

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            builder.Services.AddTransient<IBot, TeamsBot>();
        }
    }
}
