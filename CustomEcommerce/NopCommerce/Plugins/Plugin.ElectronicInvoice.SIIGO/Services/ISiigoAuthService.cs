namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public interface ISiigoAuthService
    {
        /// <summary>
        /// Gets a valid bearer token, creating a new one if necessary
        /// </summary>
        /// <returns>Valid bearer token</returns>
        Task<string> GetValidTokenAsync();

        /// <summary>
        /// Forces token refresh
        /// </summary>
        /// <returns>New bearer token</returns>
        Task<string> RefreshTokenAsync();

        /// <summary>
        /// Checks if the current token is valid and not expired
        /// </summary>
        /// <returns>True if token is valid</returns>
        bool IsTokenValid();
    }
}
