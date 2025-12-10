public interface IBlockedCustomerService
{
    Task<bool> IsCustomerBlocked();
}