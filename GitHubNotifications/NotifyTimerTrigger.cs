using GitHubNotifications.Models;
using AdaptiveCards.Templating;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.TeamsFx.Conversation;
using Newtonsoft.Json;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using Octokit;

namespace GitHubNotifications
{
    public sealed class NotifyTimerTrigger
    {
        private readonly ConversationBot _conversation;
        private readonly ILogger<NotifyTimerTrigger> _log;

        public NotifyTimerTrigger(ConversationBot conversation, ILogger<NotifyTimerTrigger> log)
        {
            _conversation = conversation;
            _log = log;
        }

        [FunctionName("NotifyTimerTrigger")]
        public async Task Run([TimerTrigger("0 0 7 * * *")]TimerInfo myTimer, ExecutionContext context, CancellationToken cancellationToken)
        {
            _log.LogInformation($"NotifyTimerTrigger is triggered at {DateTime.Now}.");

            var github = new GitHubClient(new ProductHeaderValue("GitHubNotificationsApp"));

            var shouldPrioritize = new RepositoryIssueRequest
            {
                Assignee = "therealjohn",
                State = ItemStateFilter.Open,
                Filter = IssueFilter.All
            };

            var issues = await github.Issue.GetAllForRepository("MicrosoftDocs", "visualstudio-docs", shouldPrioritize);

            // Don't send a notification if there are no assigned issues
            if (!issues.Any())
                return;

            // Read adaptive card template
            var adaptiveCardFilePath = Path.Combine(context.FunctionAppDirectory, "Resources", "NotificationDefault.json");
            var cardTemplate = await File.ReadAllTextAsync(adaptiveCardFilePath, cancellationToken);

            var installations = await _conversation.Notification.GetInstallationsAsync(cancellationToken);
            foreach (var installation in installations)
            {
                // Build and send adaptive card
                var cardContent = new AdaptiveCardTemplate(cardTemplate).Expand
                (
                    new NotificationDefaultModel
                    {
                        Title = "Your assigned GitHub issues!",
                        Description = $"You have {issues.Count} assigned issues.",
                        NotificationUrl = "https://github.com/issues/assigned",
                        Issues = issues.ToArray()
                    }
                );
                await installation.SendAdaptiveCard(JsonConvert.DeserializeObject(cardContent), cancellationToken);
            }
        }
    }
}
