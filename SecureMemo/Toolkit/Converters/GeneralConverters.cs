using System.Text;

namespace SecureMemo.Toolkit.Converters
{
    public static class GeneralConverters
    {
        public static string GetFileNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new System.ArgumentException("Path can not be null or empty");

            int iPos = path.LastIndexOf('\\') + 1;
            if (iPos > 1 && iPos != path.Length)
                return path.Substring(iPos, path.Length - iPos);

            return path;
        }

        public static string ByteArrayToHexString(byte[] data)
        {
            var sb = new StringBuilder();
            foreach (byte b in data)
                sb.AppendFormat("{0:X2}", b);

            return sb.ToString();
        }

        public static string GeneratePasswordDerivedString(string verifiedPassword)
        {
            using (System.Security.Cryptography.SHA512 sha512Implementation = System.Security.Cryptography.SHA512.Create())
            {
                const string salt1 =
                    "MVtdiy4OhAMRMKDSKUojgAirwacYRuUnT9R84DgnwOaOl0QTppuv8m3poCaElfKVBlEClohoXusGzg6vOUEgHK7yHj0vzq8eedTX0sHkmrk1sDH1AXMJ1ELODvbia6R0but4npqsVzuT3q3GukH20pswOatqLVzMSuPZrigZKRUqlJMeG4NoNqkdJyh0QPKQDeznEshFB7VqwIiqeMMtDNQx6H4HXBibpMqBhV2Ptcbf3MdkKvg8stdjsS6cd7ds";
                const string salt2 =
                    "Nv7YkXUghdWRN2MJVF6fe8p3Llo4D5rsHckmtzPdJnsLRQisCT442Wh1nIdnmbgGxE4NuEBTjtthzM42mmFT74knRWiVhoXpBoQWdOc0njJGikZNJbSMQU3sZwtq5uhRNb3WKyRSfOM0RoRB6KqRO5ItxdhxXYVjSEvYq0NlMVllydrV7NjR65eaVdl6RIxHe6y42O3j79N0dL67aeoxTHTRP0YxnfxiOqLtIsdMB2Wb2xulXht9UZjTcTLLMe09";

                var hashInputArray = Encoding.UTF8.GetBytes(salt1 + verifiedPassword + salt2);
                var hashedBytes = sha512Implementation.ComputeHash(hashInputArray, 0, hashInputArray.Length);

                for (int i = 0; i < 10; i++)
                    hashedBytes = sha512Implementation.ComputeHash(hashedBytes, 0, hashedBytes.Length);

                return ByteArrayToHexString(hashedBytes);
            }
        }
    }
}
