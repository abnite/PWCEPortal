using System.ComponentModel.DataAnnotations;

namespace PWCEPortal.ViewModel.PaymentVM;

public class MakePaymentViewModel
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal OutstandingFee { get; set; }
        
    [Required(ErrorMessage = "Please enter an amount to pay.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal AmountToPay { get; set; }
}