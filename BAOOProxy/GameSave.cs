using System;
using System.Linq;

namespace BAOOProxy
{
    class GameSave
    {
        public Version Version { get; set; } = new Version(1, 0);
        public string UserWBID { get; set; } = null;
        public System.Collections.Generic.List<ProfileState> ProfileData { get; set; } = new System.Collections.Generic.List<ProfileState>();
        public System.Collections.Generic.List<InventoryState> InventoryData { get; set; } = new System.Collections.Generic.List<InventoryState>();

        public void AddProfileDataWithLimit(string Profile, int Limit)
        {
            if (Limit < 1)
            {
                return;
            }
            if (ProfileData.Count >= Limit)
            {
                ProfileData = ProfileData.OrderBy(x => x.ID).ToList();
                int Removed = 0;
                while (ProfileData.Count >= Limit)
                {
                    ProfileData.RemoveAt(0);
                    Removed++;
                }
                if (Removed > 0)
                {
                    foreach (ProfileState ProfileState in ProfileData)
                    {
                        ProfileState.ID -= Removed;
                    }
                }
            }

            int MaxID = 0;
            if (ProfileData.Count > 0)
            {
                MaxID = ProfileData.Max(x => x.ID);
            }
            ProfileState NewEntry = new()
            {
                ID = MaxID + 1,
                Profile = ObjectOperations.ByteArrayToBase64(System.Text.Encoding.UTF8.GetBytes(Profile)),
                Check_p = ObjectOperations.ByteArrayToBase64(ObjectOperations.ByteArrayToSHA512(System.Text.Encoding.UTF8.GetBytes(Profile)))
            };

            //Profile data will always be new and unique (because it includes the date it's created) so just add NewEntry at every occurrence
            ProfileData.Add(NewEntry);
            CreateBackup();
        }
        public void AddInventoryDataWithLimit(string Inventory, int Limit)
        {
            if (Limit < 1)
            {
                return;
            }
            if (InventoryData.Count >= Limit)
            {
                InventoryData = InventoryData.OrderBy(x => x.ID).ToList();
                int Removed = 0;
                while (InventoryData.Count >= Limit)
                {
                    InventoryData.RemoveAt(0);
                    Removed++;
                }
                if (Removed > 0)
                {
                    foreach (InventoryState InventoryState in InventoryData)
                    {
                        InventoryState.ID -= Removed;
                    }
                }
            }

            int MaxID = 0;
            if (InventoryData.Count > 0)
            {
                MaxID = InventoryData.Max(x => x.ID);
            }
            InventoryState NewEntry = new()
            {
                ID = MaxID + 1,
                Inventory = ObjectOperations.ByteArrayToBase64(System.Text.Encoding.UTF8.GetBytes(Inventory)),
                Check_i = ObjectOperations.ByteArrayToBase64(ObjectOperations.ByteArrayToSHA512(System.Text.Encoding.UTF8.GetBytes(Inventory)))
            };

            //Inventory data can be duplicate so only add NewWntry when list is empty otherwise use Check_i to check and prevent duplication
            if (MaxID == 0 || MaxID > 0 && InventoryData.First(x => x.ID == MaxID).Check_i != NewEntry.Check_i)
            {
                InventoryData.Add(NewEntry);
                CreateBackup();
            }
        }
        public void CreateBackup()
        {
            string FilePath = System.IO.Path.Combine(Constants.FileData.GameSavePath, string.Format("{0}.json", UserWBID ?? "Unknown"));
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(FilePath)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
            }
            using System.IO.StreamWriter SWriter = System.IO.File.CreateText(FilePath);
            Newtonsoft.Json.JsonSerializer Serializer = new()
            {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include
            };
            Serializer.Serialize(SWriter, this);
        }
    }
    class ProfileState
    {
        public int ID { get; set; }
        public string Profile { get; set; }
        public string Check_p { get; set; }
    }
    class InventoryState
    {
        public int ID { get; set; }
        public string Inventory { get; set; }
        public string Check_i { get; set; }
    }

    class GameSaveInstance
    {
        private static GameSave GameSave = null;
        static GameSaveInstance()
        {
        }
        private GameSaveInstance()
        {
        }
        public static GameSave GetInstance(string UserWBID)
        {
            if (GameSave == null || string.IsNullOrEmpty(GameSave.UserWBID) || !GameSave.UserWBID.Equals(UserWBID))
            {
                SetInstance(UserWBID);
            }
            return GameSave;
        }
        public static void SetInstance(string UserWBID)
        {
            if (GameSave == null || string.IsNullOrEmpty(GameSave.UserWBID) || !GameSave.UserWBID.Equals(UserWBID))
            {
                if (!System.IO.Directory.Exists(Constants.FileData.GameSavePath))
                {
                    System.IO.Directory.CreateDirectory(Constants.FileData.GameSavePath);
                }
                string UserFile = System.IO.Path.Combine(Constants.FileData.GameSavePath, string.Format("{0}.json", UserWBID));
                if (System.IO.File.Exists(UserFile))
                {
                    GameSave = Newtonsoft.Json.JsonConvert.DeserializeObject<GameSave>(System.IO.File.ReadAllText(UserFile));
                }
                else
                {
                    GameSave = new GameSave
                    {
                        UserWBID = UserWBID
                    };
                    GameSave.CreateBackup();
                }
            }
        }
    }
}
