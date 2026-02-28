using System.Net;
using System.Net.Mail;
using PWCEPortal.Interfaces;

namespace PWCEPortal.Services;

public class EmailSenderService:IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public EmailSenderService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }
    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            string smtpServer = _configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            string smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            string smtpPassword = _configuration["EmailSettings:SmtpPassword"];

            using (SmtpClient smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = smtpPort;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true; // Enable SSL for secure email sending

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpUsername);
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = true;
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
        }
    }

    public async Task SendGmailEmailAsync(string email, string subject, string message)
    { 
        try
        {
            string smtpServer = _configuration["GmailEmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["GmailEmailSettings:SmtpPort"]);
            string smtpUsername = _configuration["GmailEmailSettings:SmtpUsername"];
            string smtpPassword = _configuration["GmailEmailSettings:SmtpPassword"];
            string appPassword = _configuration["GmailEmailSettings:AppPassword"]; 
            // Use the generated App Password


            using (SmtpClient smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = smtpPort;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, appPassword);
                smtpClient.EnableSsl = true; // Enable SSL for secure email sending

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpUsername);
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = true;
                
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            
        }
    }

    public async Task Execute(string apiKey, string subject, string message, string email)
    {
        throw new NotImplementedException();
    }
    
     public string GenerateLoanRecommendationEmail(string firstName, string applicationNo)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are pleased to inform you that your application for the Stimulus Loan Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been recommended for approval.<br/><br/>";
        message += "Our team has reviewed your application and found it to be promising and aligned with our objectives to support innovative projects and businesses " +
                   "that drive significant development in various sectors across Africa.<br/><br/>";
        message += "Your application will now be forwarded to our Approval Head for final review and approval. We will keep you updated on its progress " +
                   "and notify you once a final decision has been made.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to the possibility of working with you to achieve remarkable advancements in your business endeavors.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateApprovalNotificationEmail(string approvalHeadName, string applicationNo, string applicantName)
    {
        string message = "Dear " + approvalHeadName + ",<br/><br/>";
        message += "We are forwarding the application No. <strong>" + applicationNo + "</strong> submitted by <strong>" + applicantName + "</strong> " +
                   "for your final review and approval.<br/><br/>";
        message += "The application has been recommended for approval by our review team after thorough consideration and evaluation.<br/><br/>";
        message += "Please review the application in detail and provide your final decision at your earliest convenience. " +
                   "If you have any questions or need additional information regarding the application, please do not hesitate to contact us.<br/><br/>";
        message += "We appreciate your prompt attention to this matter.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    public string GeneratePendingReviewEmail(string firstName, string applicationNo, string contactNumber, string Reason, string updateLink)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are writing to inform you that your application for the Stimulus Loan Program, application No. <strong>" + applicationNo + "</strong>, " + "is currently pending further review.<br/><br/>";
        message += "Our team needs additional information or clarification regarding your application to proceed with the review process.<br/><br/>";
        message += "Reason for review: <strong>" + Reason + "</strong>.<br/><br/>";
        message += "To assist us in moving forward, please update your proposal by visiting the following link: <a href='" + updateLink + "'>Update Your Proposal</a>.<br/><br/>";
        message += "Please contact us at your earliest convenience at <strong>" + contactNumber + "</strong> for further assistance.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to resolving this matter swiftly.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    public string GenerateProposalUploadConfirmationEmail(string firstName, string applicationNo)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We have successfully received your updated business proposal for the AKL Lumi Funding Program, application No. <strong>" + applicationNo + "</strong>.<br/><br/>";
        message += "Your updated proposal has been forwarded to our review team, and they will thoroughly assess the new information you have provided.<br/><br/>";
        message += "You will be notified via email regarding the status of your application once the review is complete. If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:tradecommissionerghana@gmail.com'>tradecommissionerghana@gmail.com</a>.<br/><br/>";
        message += "We appreciate your dedication to advancing the AKL Lumi initiative and look forward to your continued collaboration.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }
    public string GenerateProposalUploadNotificationForAdmin(string applicationNo, string applicantName)
    {
        string message = "Dear Approval Committee ,<br/><br/>";
        message += "This is to inform you that the applicant <strong>" + applicantName + "</strong> has successfully uploaded an updated business proposal for application No. <strong>" + applicationNo + "</strong>.<br/><br/>";
        message += "The updated proposal is now available for your review. Please review the new submission at your earliest convenience to proceed with the evaluation process.<br/><br/>";
        message += "Thank you for your prompt attention to this matter.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }


  public string GenerateApprovedLoanEmail(string firstName, string applicationNo, double approvedAmount, double Lumi, 
    double applicationFees, double applicationFeesLumi)
{
    string message = "Dear " + firstName + ",<br/><br/>";
    message += "We are pleased to inform you that your application for the Stimulus Loan Program, application No. <strong>" + applicationNo + "</strong>, " +
               "has been approved.<br/><br/>";
    message += "The approved amount of <strong>$" + approvedAmount + "</strong> equivalent to <strong>AKL " + Lumi + "</strong> will be credited to your Lumi AKL account after review by the Compliance Team.<br/><br/>";
    message += "Before the funds can be disbursed into your Lumi AKL account, the application fee of <strong>$" + applicationFees + "</strong> equivalent to <strong>AKL " + applicationFeesLumi + "</strong> needs to be paid.<br/><br/>";
    message += "The application fees can be paid into the following accounts, all under the Account Name: <strong>Hanypay Ghana Limited</strong> and bank <strong>First Atlantic Bank Ltd</strong>:<br/><br/>";
    message += "<strong>GHS Account:</strong> 2316142751012<br/>";
    message += "Branch: OSU MAIN<br/>";
    message += "Swift Code: FAMCGHAC<br/><br/>";
    message += "Sort Code: 170124<br/><br/>";
    message += "<br/><br/>";
    message += "<strong>Foreign Accounts:</strong><br/>";
    message += "<strong>Bank:</strong> Stanbic Bank Ghana Limited<br/>";
    message += "GBP: 9040012268925<br/>";
    message += "USD: 9040012269034<br/><br/>";
    message += "Swift Code: SBICGHAC<br/><br/>";
    message += "<strong>AKL Lumi Account:</strong> 9582274300760326<br/><br/>";
    message += "To proceed, please create an account on the Hanypay market and list your product within one week.<br/><br/>";
    message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
    message += "We appreciate your patience and look forward to your success with our program.<br/><br/>";
    message += "Best regards,<br/>";
    message += "Stimulus Program Team<br/>";
    return message;
}

    public string GenerateRejectionEmail(string firstName, string applicationNo, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We regret to inform you that your application for the Stimulus Loan Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been rejected.<br/><br/>";
        message += "Reason for rejection: <strong>" + reason + "</strong>.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your interest in our program and encourage you to consider reapplying in the future.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    public string GenerateGrantRecommendationEmail(string firstName, string applicationNo)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are pleased to inform you that your application for the Stimulus Grant Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been recommended for approval.<br/><br/>";
        message += "Our team has reviewed your application and found it to be promising and aligned with our objectives to support innovative research and projects " +
                   "that drive significant development in various sectors across Africa.<br/><br/>";
        message += "Your application will now be forwarded to our Approval Head for final review and approval. We will keep you updated on its progress " +
                   "and notify you once a final decision has been made.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to the possibility of working with you to achieve remarkable advancements in your research endeavors.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateGrantApprovalNotificationEmail(string approvalHeadName, string applicationNo, string applicantName)
    {
        string message = "Dear " + approvalHeadName + ",<br/><br/>";
        message += "We are forwarding the application No. <strong>" + applicationNo + "</strong> submitted by <strong>" + applicantName + "</strong> " +
                   "for your final review and approval.<br/><br/>";
        message += "The application has been recommended for approval by our review team after thorough consideration and evaluation.<br/><br/>";
        message += "Please review the application in detail and provide your final decision at your earliest convenience. " +
                   "If you have any questions or need additional information regarding the application, please do not hesitate to contact us.<br/><br/>";
        message += "We appreciate your prompt attention to this matter.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateGrantPendingReviewEmail(string firstName, string applicationNo, string contactNumber, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are writing to inform you that your application for the Stimulus Grant Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "is currently pending further review.<br/><br/>";
        message += "Our team needs additional information or clarification regarding your application to proceed with the review process.<br/><br/>";
        message += "Reason for review: <strong>" + reason + "</strong>.<br/><br/>";
        message += "Please contact us at your earliest convenience at <strong>" + contactNumber + "</strong> for further assistance.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to resolving this matter swiftly.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    public string GenerateApprovedGrantEmail(string firstName, string applicationNo, double approvedAmount, double Lumi)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are pleased to inform you that your application for the Stimulus Grant Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been approved.<br/><br/>";
        message += "The approved grant amount of <strong>$" + approvedAmount + "</strong> equivalent to <strong>AKL " + Lumi + "</strong> will be credited to your Lumi AKL account after review by the Compliance Team.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to your success with our program.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }


    public string GenerateGrantRejectionEmail(string firstName, string applicationNo, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We regret to inform you that your application for the Stimulus Grant Program, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been rejected.<br/><br/>";
        message += "Reason for rejection: <strong>" + reason + "</strong>.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your interest in our program and encourage you to consider reapplying in the future.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    //Traditional Grants
    public string GenerateTraditionalGrantRecommendationEmail(string firstName, string applicationNo)
{
    string message = "Dear " + firstName + ",<br/><br/>";
    message += "We are pleased to inform you that your application for the Traditional Authority Development Grant, application No. <strong>" + applicationNo + "</strong>, " +
               "has been recommended for approval.<br/><br/>";
    message += "Our team has reviewed your application and found it to be promising and aligned with our objectives to support community development projects " +
               "that drive significant improvements and benefits in various traditional areas.<br/><br/>";
    message += "Your application will now be forwarded to our Approval Head for final review and approval. We will keep you updated on its progress " +
               "and notify you once a final decision has been made.<br/><br/>";
    message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
    message += "We appreciate your patience and look forward to the possibility of working with you to achieve remarkable advancements in your community.<br/><br/>";
    message += "Best regards,<br/>";
    message += "Stimulus Program Team<br/>";
    return message;
}

    public string GenerateTraditionalApprovalNotificationEmail(string approvalHeadName, string applicationNo, string applicantName)
    {
        string message = "Dear " + approvalHeadName + ",<br/><br/>";
        message += "We are forwarding the application No. <strong>" + applicationNo + "</strong> submitted by <strong>" + applicantName + "</strong> " +
                   "for your final review and approval.<br/><br/>";
        message += "The application has been recommended for approval by our review team after thorough consideration and evaluation.<br/><br/>";
        message += "Please review the application in detail and provide your final decision at your earliest convenience. " +
                   "If you have any questions or need additional information regarding the application, please do not hesitate to contact us.<br/><br/>";
        message += "We appreciate your prompt attention to this matter.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateTraditionalPendingReviewEmail(string firstName, string applicationNo, string contactNumber, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are writing to inform you that your application for the Traditional Authority Development Grant, application No. <strong>" + applicationNo + "</strong>, " +
                   "is currently pending further review.<br/><br/>";
        message += "Our team needs additional information or clarification regarding your application to proceed with the review process.<br/><br/>";
        message += "Reason for review: <strong>" + reason + "</strong>.<br/><br/>";
        message += "Please contact us at your earliest convenience at <strong>" + contactNumber + "</strong> for further assistance.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to resolving this matter swiftly.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateTraditionalApprovedGrantEmail(string firstName, string applicationNo, double approvedAmount, double Lumi)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are pleased to inform you that your application for the Traditional Authority Development Grant, application No. <strong>" + applicationNo + "</strong>, " + "has been approved.<br/><br/>";
        message += "The approved amount of <strong>$" + approvedAmount + "</strong> equivalent to <strong>AKL " + Lumi + "</strong> will be credited to your Lumi AKL account after review by the Compliance Team.<br/><br/>";
        message += "The approved grant amount will be allocated to your project as specified in your application.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your patience and look forward to your success with our program.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }

    public string GenerateTraditionalRejectionEmail(string firstName, string applicationNo, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We regret to inform you that your application for the Traditional Authority Development Grant, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been rejected.<br/><br/>";
        message += "Reason for rejection: <strong>" + reason + "</strong>.<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:stimulus@diasporastimulusfund.com'>stimulus@diasporastimulusfund.com</a>.<br/><br/>";
        message += "We appreciate your interest in our program and encourage you to consider reapplying in the future.<br/><br/>";
        message += "Best regards,<br/>";
        message += "Stimulus Program Team<br/>";
        return message;
    }
    
    
    //ALK Enrollment
    public string GenerateAKLEnrollmentRecommendationEmail(string firstName, string applicationNo)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are excited to inform you that your application for the AKL Lumi Funding Program: Countries & Strategic Partnerships, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been recommended for approval.<br/><br/>";
        message += "Our team has carefully reviewed your application and believes it is aligned with the goals of the AKL Lumi initiative, contributing to sustainable economic growth and fostering innovation across Africa and globally.<br/><br/>";
        message += "Your application will now proceed to the final stage of review, and our Approval Head will assess it further. We will keep you updated on the final decision and notify you via email once a conclusion has been reached.<br/><br/>";
        message += "If you have any questions or need further assistance, please feel free to contact us at <a href='mailto:vanuatutradecommissiongh@gmail.com'>vanuatutradecommissiongh@gmail.com</a>.<br/><br/>";
        message += "We appreciate your commitment to the AKL Lumi initiative and look forward to collaborating with you towards a future of innovation and sustainable solutions.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }

    public string GenerateAKLEnrollmentApprovalNotificationEmail(string approvalHeadName, string applicationNo, string applicantName)
    {
        string message = "Dear " + approvalHeadName + ",<br/><br/>";
        message += "We are pleased to inform you that the application No. <strong>" + applicationNo + "</strong>, submitted by <strong>" + applicantName + "</strong>, has been successfully recommended for approval by our review team.<br/><br/>";
        message += "The application aligns with the strategic goals of the AKL Lumi initiative, and we believe it contributes to our overarching mission of fostering economic growth and sustainable development. As such, we are forwarding this application for your final review and approval.<br/><br/>";
        message += "Please review the details at your earliest convenience, and kindly provide your final decision. Should you require any further details or clarifications, feel free to reach out to us at any time.<br/><br/>";
        message += "We appreciate your time and diligence in ensuring that this application receives the necessary attention.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }

    public string GenerateAKLEnrollmentPendingReviewEmail(string firstName, string applicationNo, string contactNumber, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are reaching out to inform you that your application for the AKL Lumi Funding Program: Countries & Strategic Partnerships, application No. <strong>" + applicationNo + "</strong>, requires further review.<br/><br/>";
        message += "During our initial assessment, we identified some areas that need additional information or clarification in order to proceed.<br/><br/>";
        message += "Reason for review: <strong>" + reason + "</strong>.<br/><br/>";
        message += "We kindly request that you contact us at <strong>" + contactNumber + "</strong> at your earliest convenience to assist us in completing the review process.<br/><br/>";
        message += "If you have any questions or need further assistance, please feel free to reach out to us at <a href='mailto:vanuatutradecommissiongh@gmail.com'>vanuatutradecommissiongh@gmail.com</a>.<br/><br/>";
        message += "Thank you for your patience, and we look forward to working with you to resolve this matter promptly.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }

    public string GenerateApprovedAKLEnrollmentEmail(string firstName, string applicationNo, double approvedAmount, double Lumi,double applicationFees, double applicationFeesLumi)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We are pleased to inform you that your application for the AKL Lumi Funding Program: Countries & Strategic Partnerships, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been approved.<br/><br/>";
        message += "The approved amount is <strong>$" + approvedAmount + "</strong>, equivalent to <strong>AKL " + Lumi + "</strong>, .<br/><br/>";
        message += "Before the funds can be disbursed, an application fee of <strong>$" + applicationFees + "</strong> (equivalent to <strong>AKL " + applicationFeesLumi + "</strong>) must be paid.<br/><br/>";
        message += "The application fees can be paid into the following accounts, under the Account Name: <strong>Hanypay Ghana Limited</strong> with the bank <strong>First Atlantic Bank Ghana Limited</strong>:<br/><br/>";
        message += "<strong>GHS Account:</strong> 2316142751012<br/>";
        message += "Branch: OSU MAIN<br/>";
        message += "Swift Code: FAMCGHAC<br/><br/>";
        message += "Sort Code: 170124<br/><br/>";
        message += "<br/><br/>";
        message += "<strong>Foreign Accounts:</strong><br/>";
        message += "GBP: 9040012268925<br/>";
        message += "USD: 9040012269034<br/><br/>";
        message += "Swift Code: SBICGHAC<br/><br/>";
        message += "If you have any questions or need further assistance, please do not hesitate to contact us at <a href='mailto:vanuatutradecommissiongh@gmail.com'>vanuatutradecommissiongh@gmail.com</a>.<br/><br/>";
        message += "We appreciate your dedication and look forward to your success with the AKL Lumi program.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }
  
    public string GenerateAKLEnrollmentRejectionEmail(string firstName, string applicationNo, string reason)
    {
        string message = "Dear " + firstName + ",<br/><br/>";
        message += "We regret to inform you that your application for the AKL Lumi Funding Program: Countries & Strategic Partnerships, application No. <strong>" + applicationNo + "</strong>, " +
                   "has been declined.<br/><br/>";
        message += "Reason for rejection: <strong>" + reason + "</strong>.<br/><br/>";
        message += "Our team thoroughly reviewed your submission, and while we recognize your efforts and interest in the AKL Lumi initiative, this decision was made based on current program requirements and alignment.<br/><br/>";
        message += "If you have any questions or require further clarification, please do not hesitate to contact us at <a href='mailto:vanuatutradecommissiongh@gmail.com'>vanuatutradecommissiongh@gmail.com</a>.<br/><br/>";
        message += "We appreciate your commitment to sustainable development and encourage you to consider reapplying or exploring future opportunities within the AKL Lumi initiative.<br/><br/>";
        message += "Best regards,<br/>";
        message += "AKL Lumi Team<br/>";
        return message;
    }



}