using System.Linq;
using System.Security.Cryptography;
using Encryption;
using NUnit.Framework;

namespace EncryptionTests
{
    public class Tests
    {
        [Test]
        public void TestRoundTrip()
        {
            var key = RandomNumberGenerator.GetBytes(10);
            var data = RandomNumberGenerator.GetBytes(1024);

            using var encryptAes = new AesGcmWrapper(key);
            var cipher = encryptAes.Encrypt(data);

            using var decryptAes = new AesGcmWrapper(key);
            var result = decryptAes.Decrypt(cipher);

            Assert.IsTrue(result.SequenceEqual(data));
        }
    }
}