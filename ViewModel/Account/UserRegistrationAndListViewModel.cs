namespace PWCEPortal.ViewModel.Account;

public class UserRegistrationAndListViewModel
{
    public UserRegistrationViewModel Registration { get; set; }
    public List<UserRegistrationViewModel> Users { get; set; }
    public UserRegistrationAndListViewModel()
    {
        Users = new List<UserRegistrationViewModel>();
    }
}