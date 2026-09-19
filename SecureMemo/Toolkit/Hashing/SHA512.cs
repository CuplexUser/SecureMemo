namespace SecureMemo.Toolkit.Hashing
{
    public static class SHA512
    {
        public static byte[] GetSHA512HashAsByteArray(byte[] data)
        {
            System.Security.Cryptography.SHA512 sha512Implementation = System.Security.Cryptography.SHA512.Create();
            return sha512Implementation.ComputeHash(data);
        }
    }
}
