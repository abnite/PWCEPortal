using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models;

public class AuditLog:EntityHelper
{
    
    [ForeignKey("User")]
    public string? UserId { get; set; } 
    public ApplicationUser? User { get; set; }

    public string ActionPerformed { get; set; }
    public string IPAddress { get; set; }
    
    public string OldValues { get; set; }
    public string NewValues { get; set; }

   
}