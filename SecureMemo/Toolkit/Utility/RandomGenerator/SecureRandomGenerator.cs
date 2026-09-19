using System;
using System.Security.Cryptography;
using System.Text;

namespace SecureMemo.Toolkit.Utility.RandomGenerator
{
    /// <summary>
    /// Secure Random generator
    /// </summary>
    public class SecureRandomGenerator : IDisposable
    {
        private const string AlphaNumericStr = "0123456789abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMNOPQRSTUVXYZ";
        private const string SpecialCharStr = "!+-=#_[]<>?$@" + AlphaNumericStr;
        private readonly RandomNumberGenerator _randomNumberGenerator;

        public SecureRandomGenerator()
        {
            _randomNumberGenerator = RandomNumberGenerator.Create();
        }

        public int GetRandomInt(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            if (minValue == maxValue) return minValue;
            long diff = maxValue - minValue;
            byte[] buffer = new byte[4];
            while (true)
            {
                _randomNumberGenerator.GetBytes(buffer);
                uint rand = BitConverter.ToUInt32(buffer, 0);

                long max = (1 + (long)uint.MaxValue);
                long remainder = max % diff;
                if (rand < max - remainder)
                {
                    return (int)(minValue + rand % diff);
                }
            }
        }

        public byte[] GetRandomData(int length)
        {
            byte[] data = new byte[length];
            _randomNumberGenerator.GetBytes(data);
            return data;
        }

        public string GetPasswordString(int length)
        {
            var buffer = new byte[(length * 16)];
            var sb = new StringBuilder();
            _randomNumberGenerator.GetBytes(buffer);

            for (int i = 0; i < buffer.Length; i += 16)
            {
                ushort rndVal = BitConverter.ToUInt16(buffer, i);
                sb.Append(SpecialCharStr[rndVal % SpecialCharStr.Length]);
            }

            if (sb.Length > length)
                sb.Remove(length, sb.Length - length);

            return sb.ToString();
        }

        public void Dispose()
        {
            _randomNumberGenerator?.Dispose();
        }
    }
}
