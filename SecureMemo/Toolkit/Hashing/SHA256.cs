namespace SecureMemo.Toolkit.Hashing
{
    public static class SHA256
    {
        public static byte[] GetSHA256HashAsByteArray(byte[] data)
        {
            System.Security.Cryptography.SHA256 sha256Implementation = System.Security.Cryptography.SHA256.Create();
            return sha256Implementation.ComputeHash(data);
        }
    }
}
