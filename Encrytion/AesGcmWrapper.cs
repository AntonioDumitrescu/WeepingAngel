using System.Security.Cryptography;
using BP = System.Buffers.Binary.BinaryPrimitives;

namespace Encryption;

public class AesGcmWrapper : IDisposable
{
    private readonly AesGcm _aes;

    public AesGcmWrapper(byte[] password)
    {
        var key = new Rfc2898DeriveBytes(password, new byte[8], 1000).GetBytes(16);

        _aes = new AesGcm(key);
    }

    public byte[] Encrypt(byte[] plainBytes)
    {
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;
        var cipherSize = plainBytes.Length;

        var encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;
        var encryptedDataArray = new byte[encryptedDataLength];
        var encryptedData = encryptedDataArray.AsSpan();

        BP.WriteInt32LittleEndian(encryptedData[..4], nonceSize);
        BP.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
            
        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        RandomNumberGenerator.Fill(nonce);

        // Encrypt
        _aes.Encrypt(nonce, plainBytes.AsSpan(), cipherBytes, tag);

        return encryptedDataArray;
    }

    public byte[] Decrypt(Span<byte> encryptedData)
    {
        var nonceSize = BP.ReadInt32LittleEndian(encryptedData[..4]);
        var tagSize = BP.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
        var cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        var plainBytes = new byte[cipherSize];
        _aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return plainBytes;
    }

    public void Dispose()
    {
        _aes.Dispose();
    }
}