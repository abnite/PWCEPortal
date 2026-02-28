using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;

namespace PWCEPortal.Models;

public class DeletedRecordsLog:EntityHelper
{
    [ForeignKey("User")]
    public string UserId { get; set; }

    public string RecordType { get; set; } // Table name (e.g., "Student", "Payment")
    public string RecordId { get; set; } // ID of the deleted record

    public ApplicationUser User { get; set; }
}