using System.Security.Cryptography;
using Encryption;

namespace EncryptionBenchmark;

internal static class ThroughputTest
{
    private const int TestTime = 3;     // seconds
    private const int MaxPower = 26;    // power of 2

    public static void Test()
    {
        var encryptionResults = new List<(int size, double throughput)>();
        var decryptionResults = new List<(int size, double throughput)>();

        for (var power = 1; power <= MaxPower; power++)
        {
            var length = (int)Math.Pow(2, power);
            Console.WriteLine($"Testing {Suffix.Convert(length, 2)}");
            var buffer = RandomNumberGenerator.GetBytes(length);

            using var encryption = new AesGcmWrapper(RandomNumberGenerator.GetBytes(10));

            var startTime = DateTime.Now;
            var dataTransferred = 0L;

            byte[] cipherBuffer = null;

            // test encryption throughput
            while ((DateTime.Now - startTime).TotalSeconds < TestTime)
            {
                cipherBuffer = encryption.Encrypt(buffer);
                dataTransferred += length;
            }

            var totalTime = (DateTime.Now - startTime).TotalSeconds;
            var averageRate = dataTransferred / totalTime;

            Console.WriteLine($"Encryption test complete: {totalTime} seconds. Throughput: {Suffix.Convert((long)averageRate, 2)}");
            encryptionResults.Add((length, averageRate));

            startTime = DateTime.Now;
            dataTransferred = 0L;

            while ((DateTime.Now - startTime).TotalSeconds < TestTime)
            {
                encryption.Decrypt(cipherBuffer);
                dataTransferred += length;
            }

            totalTime = (DateTime.Now - startTime).TotalSeconds;
            averageRate = dataTransferred / totalTime;

            Console.WriteLine($"Decryption test complete: {totalTime} seconds. Throughput: {Suffix.Convert((long)averageRate, 2)}");
            decryptionResults.Add((length, averageRate));
        }

        void PrintResultCollection(List<(int size, double troughput)> resultCollection)
        {
            var results = resultCollection.Select(x => (Suffix.Convert(x.size), x.troughput)).ToList();
            var longestSizeString = results.Max(x => x.Item1.ToString().Length);
            foreach (var encryptionResult in results)
            {
                Console.Write("  ");
                Console.Write(encryptionResult.Item1 + new string(' ', longestSizeString - encryptionResult.Item1.ToString().Length));
                Console.Write(": ");
                Console.WriteLine($"{Suffix.Convert((long)encryptionResult.troughput),2}");
            }
        }

        Console.WriteLine("\r\n\r\n");
        Console.WriteLine("Encryption results:\n");
        PrintResultCollection(encryptionResults);

        Console.WriteLine("\r\nDecryption results:\n");
        PrintResultCollection(decryptionResults);
    }
}