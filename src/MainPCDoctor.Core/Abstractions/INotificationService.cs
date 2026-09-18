namespace MainPCDoctor.Core.Abstractions;

public interface INotificationService
{
    void NotifyLevel2Ram(string incidentId);
    void NotifyLevel4Upgrade(string component, string confidence);
}
