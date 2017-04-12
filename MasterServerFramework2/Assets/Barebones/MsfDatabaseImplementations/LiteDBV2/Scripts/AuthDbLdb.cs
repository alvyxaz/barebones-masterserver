#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
using System;
using System.Collections.Generic;
using LiteDB;

namespace Barebones.MasterServer
{
    public class AuthDbLdb : IAuthDatabase
    {
        private readonly LiteCollection<AccountDataLdb> _accounts;
        private readonly LiteCollection<PasswordResetData> _resetCodes;
        private readonly LiteCollection<EmailConfirmationData> _emailConfirmations;

        private readonly LiteDatabase _db;

        public AuthDbLdb(LiteDatabase database)
        {
            _db = database;

            _accounts = _db.GetCollection<AccountDataLdb>("accounts");
            _accounts.EnsureIndex(a => a.Username, new IndexOptions() { Unique = true, IgnoreCase = true, TrimWhitespace = true});
            _accounts.EnsureIndex(a => a.Email, new IndexOptions() { Unique = true, IgnoreCase = true, TrimWhitespace = true });

            _resetCodes = _db.GetCollection<PasswordResetData>("resets");
            _resetCodes.EnsureIndex(a => a.Email, new IndexOptions() {Unique = true, IgnoreCase = true, TrimWhitespace = true});

            _emailConfirmations = _db.GetCollection<EmailConfirmationData>("emailConf");
            _emailConfirmations.EnsureIndex(a => a.Email, new IndexOptions() { Unique = true, IgnoreCase = true, TrimWhitespace = true });
        }

        public IAccountData CreateAccountObject()
        {
            var account = new AccountDataLdb();
            return account;
        }

        public IAccountData GetAccount(string username)
        {
            var account = _accounts.FindOne(a => a.Username == username);
            return account;
        }

        public IAccountData GetAccountByToken(string token)
        {
            var account = _accounts.FindOne(a => a.Token == token);

            return account;
        }

        public IAccountData GetAccountByEmail(string email)
        {
            var emailLower = email.ToLower();
            var account = _accounts.FindOne(Query.EQ("Email", emailLower));

            return account;
        }

        public void SavePasswordResetCode(IAccountData account, string code)
        {
            // Delete older codes
            _resetCodes.Delete(Query.EQ("Email", account.Email.ToLower()));

            _resetCodes.Insert(new PasswordResetData()
            {
                Email = account.Email,
                Code = code
            });
        }

        public IPasswordResetData GetPasswordResetData(string email)
        {
            return _resetCodes.FindOne(Query.EQ("Email", email.ToLower()));
        }

        public void SaveEmailConfirmationCode(string email, string code)
        {
            _emailConfirmations.Delete(Query.EQ("Email", email));
            _emailConfirmations.Insert(new EmailConfirmationData()
            {
                Code = code,
                Email = email
            });
        }

        public string GetEmailConfirmationCode(string email)
        {
            var entry = _emailConfirmations.FindOne(Query.EQ("Email", email));

            return entry != null ? entry.Code : null;
        }

        public void UpdateAccount(IAccountData account)
        {
            _accounts.Update(account as AccountDataLdb);
        }

        public void InsertNewAccount(IAccountData account)
        {
            _accounts.Insert(account as AccountDataLdb);
        }

        public void InsertToken(IAccountData account, string token)
        {
            account.Token = token;
            _accounts.Update(account as AccountDataLdb);
        }

        private class PasswordResetData : IPasswordResetData
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }

        private class EmailConfirmationData
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }

        /// <summary>
        ///     LiteDB implementation of account data
        /// </summary>
        private class AccountDataLdb : IAccountData
        {
            [BsonId]
            public string Username { get; set; }

            public string Password { get; set; }
            public string Email { get; set; }
            public string Token { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsGuest { get; set; }
            public bool IsEmailConfirmed { get; set; }
            public Dictionary<string, string> Properties { get; set; }

            public event Action<IAccountData> OnChange;

            public AccountDataLdb()
            {
            }

            public void MarkAsDirty()
            {
                if (OnChange != null)
                    OnChange.Invoke(this);
            }
        }
    }
}

#endif