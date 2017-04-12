namespace Barebones.MasterServer
{
    /// <summary>
    /// Generic success callback declaration.
    /// </summary>
    /// <param name="isSuccessful"></param>
    /// <param name="error"></param>
    public delegate void SuccessCallback(bool isSuccessful, string error);
}