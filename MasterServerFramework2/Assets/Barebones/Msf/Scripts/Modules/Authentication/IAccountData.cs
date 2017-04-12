using System;
using System.Collections.Generic;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents account data
    /// </summary>
    public interface IAccountData
    {
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Token { get; set; }
        bool IsAdmin { get; set; }
        bool IsGuest { get; set; }
        bool IsEmailConfirmed { get; set; }
        Dictionary<string, string> Properties { get; set; }

        event Action<IAccountData> OnChange;
        void MarkAsDirty();
    }
}