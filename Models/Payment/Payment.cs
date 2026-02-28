using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.Academic;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Payment;

public class Payment:EntityHelper
{
    [ForeignKey("Student")]
    public Guid StudentId { get; set; } 
    public Student Student { get; set; }
    
    [ForeignKey("CurrentAcademicYear")]
    public Guid? CurrentAcademicYearId { get; set; }
    public AcademicYear? CurrentAcademicYear { get; set; }

    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool IsVerified { get; set; } // Approved by Finance Officer
    
    public ApplicationUser? VerifiedBy { get; set; } //The Finance Officer ID
    
    public DateTime? VerifiedDate { get; set; }

    // Additional properties for enhanced tracking:
    public string? PaymentReference { get; set; }  // Unique reference code for the payment transaction.
    public string? ReceiptFilePath { get; set; }     // Path or URL to the uploaded payment receipt.
    public DateTime? uploadReceiptDate { get; set; }
    public string? PaymentMethod { get; set; }  
    public string? BankReferenceNumber { get; set; }
    public string? AccountantReceipt { get; set; }
}