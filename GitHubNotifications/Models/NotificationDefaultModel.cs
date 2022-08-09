using Octokit;

namespace GitHubNotifications.Models
{
    public class NotificationDefaultModel
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string NotificationUrl { get; set; }

        public Issue[] Issues { get; set; }
    }
}
