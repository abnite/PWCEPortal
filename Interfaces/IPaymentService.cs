using PWCEPortal.Models.Payment;

namespace PWCEPortal.Interfaces;

public interface IPaymentService
{
    Task<FeeStructure> GetFeeStructureAsync(Guid studentId, Guid academicYearId);
    Task<List<Payment>> GetPaymentsByStudentIdAsync(Guid studentId);
    Task<List<Payment>> GetCurrentUnVerifiedPaymentsByStudentIdAsync(Guid studentId);
    Task<List<Payment>> GetCurrentYearVerifiedPaymentsByStudentIdAsync(Guid studentId);
    Task<List<Payment>> GetVerifiedPaymentsByStudentIdAsync(Guid studentId);
    Task<List<Payment>> GetPendingPaymentsAsync(); // Fetch payments awaiting verification
    Task<Payment> GetPaymentByIdAsync(Guid paymentId); // Fetch a payment by ID
    Task UpdatePaymentAsync(Payment payment); // Update a payment record
    Task DeletePaymentAsync(Guid paymentId); // Delete a payment record
    Task<StudentFeeAssignment> GetStudentFeeAssignmentAsync(Guid studentId, Guid academicYearId); // Fetch fee assignment for a student
    Task UpdateStudentFeeAssignmentAsync(StudentFeeAssignment feeAssignment); // Update fee assignment
    
    Task VerifyPaymentAsync(Guid paymentId); // Verify a payment
    Task VerifyPaymentNoUploadAsync(Guid paymentId, string bankReference, string accountantReceipt);
    Task RejectPaymentAsync(Guid paymentId,string rejectionReason); // Reject a payment
    Task<(List<Payment> Payments, int TotalCount)> GetPendingPaymentsAsync(string searchQuery = null, int page = 1, int pageSize = 10);

    Task<(List<Payment> Payments, int TotalCount)> GetPendingPaymentsNoUploadAsync(string searchQuery = null, int page = 1, int pageSize = 10);
    
    // Fetch fees collected with pagination and search
    Task<(List<Payment> Payments, int TotalCount)> GetFeesCollectedAsync(string searchQuery = null, int? level = null, int page = 1, int pageSize = 10);

    // Fetch outstanding fees with pagination and search
    Task<(List<StudentFeeAssignment> OutstandingFees, int TotalCount)> GetOutstandingFeesAsync(string searchQuery = null, int? level = null, int page = 1, int pageSize = 10);
    
    Task<(List<StudentFeeAssignment> Overpayment, int TotalCount)> GetOverpaymentFeesAsync(string searchQuery = null, int? level = null, int page = 1, int pageSize = 10);

    // Download fees collected to Excel
    Task<byte[]> DownloadFeesCollectedAsync(string searchQuery = null, int? level = null);

    // Download outstanding fees to Excel
    Task<byte[]> DownloadOutstandingFeesAsync(string searchQuery = null, int? level = null);
    Task<byte[]> DownloadOverpaymentFeesAsync(string searchQuery = null, int? level = null);
}