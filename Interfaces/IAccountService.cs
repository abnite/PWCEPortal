using System.Threading.Tasks;
using PWCEPortal.Models.StudentInfo;


namespace PWCEPortal.Interfaces;

public interface IAccountService
{
        Task <bool> ValidateStudentAccount(string applicationNumber, DateTime dateOfBirth);
        Task <bool> ActivateStudentAccount(Student student, string email);
        Task <bool> ConfirmEmail(string userId, string token);
        Task <bool> Login(string email, string password);
        Task <bool> ResetPassword(string email, string token, string newPassword);
}