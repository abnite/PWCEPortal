using System.ComponentModel.DataAnnotations;
using PWCEPortal.Data;

namespace PWCEPortal.CommonEntity;

public class EntityHelper
{
    [Key]
    public Guid Id { get; set; }
    public DateTime? DateAdded { get; set; }
    public  ApplicationUser? AddedBy { get; set; }
    public Boolean? IsDeleted { get; set; }
    public DateTime? DateDeleted { get; set; }
    public ApplicationUser? DeletedBy { get; set; }
    public string? Token { get; set; }

    public EntityHelper()
    {
        DateAdded = DateTime.UtcNow;
        IsDeleted = false;
    }
}