using System;

namespace BAOOProxy
{
    static class DataHandler
    {
        public static string FixUp(string Input)
        {
            if (string.IsNullOrEmpty(Input))
            {
                return string.Empty;
            }

            //FIX UP
            //Host fix
            string Output = System.Text.RegularExpressions.Regex.Replace(Input, Constants.RegexData.FindHost, Constants.OriginalData.OriginalHost, System.Text.RegularExpressions.RegexOptions.Multiline);

            //Change MOTD
            if (Output.IndexOf("Content-Length: 519") > -1 && Output.IndexOf(Constants.OriginalData.OriginalMOTD) > -1)
            {
                Output = Output.Replace(Constants.OriginalData.OriginalMOTD, Constants.NewData.NewMOTD);
                Output = Output.Replace("Content-Length: 519", string.Format("Content-Length: {0}", (519 - Constants.OriginalData.OriginalMOTD.Length + Constants.NewData.NewMOTD.Length)));
            }

            //NetVars potential fix

            return Output;
        }
        public static bool AnalyzeAndBackupWhenDone(string Data, AppConfig AppConfig)
        {
            if (string.IsNullOrEmpty(Data))
            {
                return false;
            }

            int ContentLength = 0;
            System.Text.RegularExpressions.Match ContentLengthMatch = System.Text.RegularExpressions.Regex.Match(Data, Constants.RegexData.FindHTTPContentLength, System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Singleline);
            if (ContentLengthMatch.Success)
            {
                if (!string.IsNullOrEmpty(ContentLengthMatch.Groups["length"].Value))
                {
                    ContentLength = int.Parse(ContentLengthMatch.Groups["length"].Value);
                }
            }
            if (Data.Length < Data.IndexOf("\r\n\r\n") + 4 + ContentLength)
            {
                return false;
            }
            else
            {
                BackUp(Data, AppConfig);
            }

            return true;
        }
        private static void BackUp(string Input, AppConfig AppConfig)
        {
            if (string.IsNullOrEmpty(Input))
            {
                return;
            }
            try
            {
                //BACK UP
                System.Text.RegularExpressions.Match UserWBIDMatch = System.Text.RegularExpressions.Regex.Match(Input, Constants.RegexData.FindUserWBID, System.Text.RegularExpressions.RegexOptions.Singleline);
                if (UserWBIDMatch.Success)
                {
                    if (UserWBIDMatch.Groups["users_me"].Value.Equals(UserWBIDMatch.Groups["user_id"].Value))
                    {
                        //Set Backup Instance
                        GameSaveInstance.SetInstance(UserWBIDMatch.Groups["user_id"].Value);
                    }
                    else
                    {
                        Console.WriteLine("Couldn't determine the UserId.");
                    }
                }

                System.Text.RegularExpressions.Match InventoryMatch = System.Text.RegularExpressions.Regex.Match(Input, Constants.RegexData.FindInventory, System.Text.RegularExpressions.RegexOptions.Singleline);
                if (InventoryMatch.Success)
                {
                    //Get Backup Instance
                    var BUI = GameSaveInstance.GetInstance(InventoryMatch.Groups["users_me"].Value);
                    BUI.AddInventoryDataWithLimit(InventoryMatch.Groups["inventory"].Value, AppConfig.InventoryHistoryAmount);
                }

                System.Text.RegularExpressions.Match ProfileMatch = System.Text.RegularExpressions.Regex.Match(Input, Constants.RegexData.FindProfile, System.Text.RegularExpressions.RegexOptions.Singleline);
                if (ProfileMatch.Success)
                {
                    //Get Backup Instance
                    var BUI = GameSaveInstance.GetInstance(ProfileMatch.Groups["users_me"].Value);
                    BUI.AddProfileDataWithLimit(ProfileMatch.Groups["profile"].Value, AppConfig.ProfileHistoryAmount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: {0}", e);
            }
        }
    }
}
