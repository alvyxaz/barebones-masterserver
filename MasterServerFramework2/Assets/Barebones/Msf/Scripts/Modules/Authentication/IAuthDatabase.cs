namespace Barebones.MasterServer
{
    public interface IAuthDatabase
    {
        /// <summary>
        ///     Should create an empty object with account data.
        /// </summary>
        /// <returns></returns>
        IAccountData CreateAccountObject();

        IAccountData GetAccount(string username);
        IAccountData GetAccountByToken(string token);
        IAccountData GetAccountByEmail(string email);

        void SavePasswordResetCode(IAccountData account, string code);
        IPasswordResetData GetPasswordResetData(string email);

        void SaveEmailConfirmationCode(string email, string code);
        string GetEmailConfirmationCode(string email);

        void UpdateAccount(IAccountData account);
        void InsertNewAccount(IAccountData account);
        void InsertToken(IAccountData account, string token);
    }
}