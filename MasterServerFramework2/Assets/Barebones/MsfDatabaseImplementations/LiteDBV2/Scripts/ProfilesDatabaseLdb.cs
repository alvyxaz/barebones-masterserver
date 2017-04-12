#if (!UNITY_WEBGL && !UNITY_IOS) || UNITY_EDITOR
using LiteDB;

namespace Barebones.MasterServer
{
    /// <summary>
    /// LiteDB profiles database implementation
    /// </summary>
    public class ProfilesDatabaseLdb : IProfilesDatabase
    {
        private readonly LiteCollection<ProfileDataLdb> _profiles;
        private readonly LiteDatabase _db;

        public ProfilesDatabaseLdb(LiteDatabase database)
        {
            _db = database;

            _profiles = _db.GetCollection<ProfileDataLdb>("profiles");
            _profiles.EnsureIndex(a => a.Username, new IndexOptions() { Unique = true });
        }

        /// <summary>
        /// Should restore all values of the given profile, 
        /// or not change them, if there's no entry in the database
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public void RestoreProfile(ObservableServerProfile profile)
        {
            var data = FindOrCreateData(profile);
            profile.FromBytes(data.Data);
        }

        private ProfileDataLdb FindOrCreateData(ObservableServerProfile profile)
        {
            var data = _profiles.FindOne(a => a.Username == profile.Username);

            if (data == null)
            {
                data = new ProfileDataLdb()
                {
                    Username = profile.Username,
                    Data = profile.ToBytes()
                };

                // Why did I do this?
                _profiles.Insert(data);
            }
            return data;
        }

        /// <summary>
        /// Should save updated profile into database
        /// </summary>
        /// <param name="profile"></param>
        public void UpdateProfile(ObservableServerProfile profile)
        {
            var data = FindOrCreateData(profile);
            data.Data = profile.ToBytes();
            _profiles.Update(data);
        }

        /// <summary>
        /// LiteDB profile data implementation
        /// </summary>
        private class ProfileDataLdb
        {
            [BsonId]
            public string Username { get; set; }

            public byte[] Data { get; set; }
        }
    }
}

#endif