using System;
using System.IO;
using System.Security;
using System.Text;
using GeneralToolkitLib.Converters;
using GeneralToolkitLib.Hashing;
using GeneralToolkitLib.Utility.RandomGenerator;

namespace SecureMemo.Managers
{
    public class CryptoManager : ManagerBase
    {
        [SecurityCritical]
        [SecuritySafeCritical]
        public byte[] GenerateBinaryKey()
        {
            const string originalLookupIndex = "SecureMemo";
            using var secureRnd = new SecureRandomGenerator();
            MemoryStream ms = new MemoryStream();
            byte[] buffer = secureRnd.GetRandomData(secureRnd.GetRandomInt(29, 221));
            ms.Write(buffer, 0, buffer.Length);

            buffer = GeneralConverters.ConvertStringToByteArray(Encoding.ASCII, originalLookupIndex);
            ms.Write(buffer, 0, buffer.Length);

            buffer = secureRnd.GetRandomData(secureRnd.GetRandomInt(29, 221));
            ms.Write(buffer, 0, buffer.Length);
            buffer = ms.ToArray();

            // ReSharper disable once SuggestVarOrType_Elsewhere

            for (int i = 0; i < secureRnd.GetRandomInt(83, 101); i++)
            {
                // Tick count being tracked by .Net using 32 bit integers for some strange reason when the WIN_API CALL uses ulong and thus not overflowing in 24 days and 20 hours.
                int tickCount = Environment.TickCount;

                // Just inject a small amount of noise for each iteration 
                int pos = tickCount % buffer.Length;
                buffer[pos] ^= (byte) (tickCount % byte.MaxValue);
                buffer = SHA512.GetSHA512HashAsByteArray(buffer);
            }

            buffer = SHA256.GetSHA256HashAsByteArray(buffer);
            return buffer;
        }


        [SecurityCritical]
        [SecuritySafeCritical]
        public string GenerateKey()
        {
            const string originalLookupIndex = "SecureMemo";
            string key;
            using var secureRnd = new SecureRandomGenerator();
            string tmp = secureRnd.GetPasswordString(secureRnd.GetRandomInt(29, 221)) + originalLookupIndex + secureRnd.GetPasswordString(secureRnd.GetRandomInt(29, 221));

            // ReSharper disable once SuggestVarOrType_Elsewhere
            byte[] buffer = GeneralConverters.ConvertStringToByteArray(Encoding.ASCII, tmp);
            for (int i = 0; i < secureRnd.GetRandomInt(83, 101); i++)
            {
                // Tick count being tracked by .Net using 32 bit integers for some strange reason when the WIN_API CALL uses ulong and thus not overflowing in 24 days and 20 hours.
                int tickCount = Environment.TickCount;

                // Just inject a small amount of noise for each iteration 
                int pos = tickCount % buffer.Length;
                buffer[pos] ^= (byte) (tickCount % byte.MaxValue);
                buffer = SHA512.GetSHA512HashAsByteArray(buffer);
            }

            buffer = SHA256.GetSHA256HashAsByteArray(buffer);
            key = Convert.ToBase64String(buffer, 0, buffer.Length, Base64FormattingOptions.None).Trim("=".ToCharArray());

            return key;
        }
    }
}