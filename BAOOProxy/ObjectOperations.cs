namespace BAOOProxy
{
    public static class ObjectOperations
    {
        public static byte[] ByteArrayToSHA512(byte[] ByteArray)
        {
            byte[] Hash;
            using (System.Security.Cryptography.SHA512 SHAM = new System.Security.Cryptography.SHA512Managed())
            {
                Hash = SHAM.ComputeHash(ByteArray);
            }
            return Hash;
        }
        public static string ByteArrayToBase64(byte[] ByteArray, bool Urlsafe = true)
        {
            string B64;
            B64 = System.Convert.ToBase64String(ByteArray);
            if (Urlsafe)
            {
                B64 = B64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
            return B64;
        }
    }
}
