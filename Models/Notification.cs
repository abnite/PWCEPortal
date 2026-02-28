using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models;

public class Notification:EntityHelper
{
    [ForeignKey("User")]
    public string UserId { get; set; }

    public string Message { get; set; }
    public bool IsRead { get; set; } // Has the user read the notification?

    public ApplicationUser User { get; set; }
}