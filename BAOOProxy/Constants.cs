namespace BAOOProxy
{
    static class Constants
    {
        public class OriginalData
        {
            public static readonly string OriginalHost = "Host: ozzypc-wbid.live.ws.fireteam.net";
            public static readonly string OriginalMOTD = @"{""total_count"": 2, ""next_page"": 2, ""items"": [{""published_at"": ""Tue, 10 Sep 2013 18:57:37 GMT"", ""_id"": ""76c59712-cc37-441e-b675-52f52d51db2d"", ""contents"": null, ""title"": ""Welcome to Batman: Arkham Origins. For more on the game, please visit www.batmanarkhamorigins.com""}, {""published_at"": ""Tue, 25 Oct 2016 22:30:45 GMT"", ""_id"": ""05076e6f-2dc6-461f-943f-b9715b022892"", ""contents"": null, ""title"": ""Online services to be retired.  Please visit http://support.wbgames.com/ for more information.""}], ""page"": 1, ""pages"": 1}";
            public static readonly string OriginalOnlineDefaultWBIDVarsContent = @"[GDHttp]
BaseUrl=""https://ozzypc-wbid.live.ws.fireteam.net/""
EchoBaseURL=""http://in.echo.fireteam.net/""
WBIDTicketURL=""https://tokenservice.psn.turbine.com/TokenService""
WBIDAMSURL=""https://cls.turbine.com/CLS""
ClientId=""0938aa7a-6682-4b90-a97d-90becbddb9ce""
ClientIdSP=""6ca97b4e-d278-48a4-8b66-80468447a513""
ClientSecret=""GXnNQaRSuxaxlm6uR35HVk39u""
ClientSecretSP=""AzyEBlZdY87HO3HINj7rqoBo7""
EchoUsername=""8b8f1d8554d5437b8cdf689082311680""
EchoPassword=""b3014aee79ba4968886003ecb271f764""
Environment=""Live""";
        }
        public class RegexData
        {
            public static readonly string FindHost = "^Host:.*$";
            public static readonly string FindUserWBID = @"Content-Location: https:\/\/ozzypc-wbid.live.wws.fireteam.net\/users\/(?<users_me>[^\r\n]*).*{""user_id"": ""(?<user_id>[^""]*)""}";
            public static readonly string FindInventory = @"Content-Location: https:\/\/ozzypc-wbid.live.wws.fireteam.net\/users\/(?<users_me>[^\/]*)\/inventory.*(?<inventory>{""inventory"":[^\r\n]*)";
            public static readonly string FindProfile = @"PUT \/users\/(?<users_me>[^\/]*)\/profile\/private.*(?<profile>{""data"":[^\r\n]*)";
            public static readonly string FindHTTPContentLength = @"^(HTTP|GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH).*$.*^Content-Length: (?<length>\d+).*$";
        }
        public class NewData
        {
            public static readonly string NewMOTD = @"{""total_count"": 1, ""next_page"": 2, ""items"": [{""published_at"": ""Tue, 16 Feb 2021 18:09:51 GMT"", ""_id"": ""0123456789abcdef"", ""contents"": null, ""title"": ""Online services restored by Reugen.""}], ""page"": 1, ""pages"": 1}";
            public static readonly string SF_NewOnlineDefaultWBIDVarsContent = @"[GDHttp]
BaseUrl=""http://{0}/""
EchoBaseURL=""http://in.echo.fireteam.net/""
WBIDTicketURL=""https://tokenservice.psn.turbine.com/TokenService""
WBIDAMSURL=""https://cls.turbine.com/CLS""
ClientId=""0938aa7a-6682-4b90-a97d-90becbddb9ce""
ClientIdSP=""6ca97b4e-d278-48a4-8b66-80468447a513""
ClientSecret=""GXnNQaRSuxaxlm6uR35HVk39u""
ClientSecretSP=""AzyEBlZdY87HO3HINj7rqoBo7""
EchoUsername=""8b8f1d8554d5437b8cdf689082311680""
EchoPassword=""b3014aee79ba4968886003ecb271f764""
Environment=""Live""";
        }
        public class FileData
        {
            public static readonly string ConfigPath = "Config";
            public static readonly string GameSavePath = "GameSave";
            public static readonly string OnlineDefaultWBIDVarsPath = @"Online\BmGame\Config\DefaultWBIDVars.ini";
            public static readonly string BAOManifestFilePath = @"steamapps\appmanifest_209000.acf";
            public static readonly string SteamLibraryFoldersFilePath = @"steamapps\libraryfolders.vdf";
            public static readonly string SteamGamesFolderPath = @"steamapps\common";
        }
    }
}
