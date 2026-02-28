namespace PWCEPortal.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendGmailEmailAsync(string email, string subject, string message);

    public Task Execute(string apiKey, string subject, string message, string email);
    
    string GenerateLoanRecommendationEmail(string firstName, string applicationNo);
    string GenerateApprovalNotificationEmail(string approvalHeadName, string applicationNo, string applicantName);



}