using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace App.Infrastructure.Utility.Common
{
    public interface IEncryptionHelper
    {
        string EncryptString(string password);

        string DecryptString(string cipherText);
    }

    public class EncryptionHelper : IEncryptionHelper
    {
        private const string SPassPhrase = "indaSoft2019@tss"; // can be any string
        private const string SaltValue = "bdxaasc@Sru#x"; // can be any string
        private const string HashAlgorithm = "SHA1"; // can be "MD5"
        private const int PasswordIterations = 2; // can be any number
        private const string InitVector = "@1B2c3D4e5F6g7H8"; // must be 16 bytes
        private const int KeySize = 256; // can be 192 or 128

        public string EncryptString(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            return Encrypt(true, password, SPassPhrase, SaltValue, HashAlgorithm, PasswordIterations, InitVector, KeySize);
        }

        public string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
            return Encrypt(false, cipherText, SPassPhrase, SaltValue, HashAlgorithm, PasswordIterations, InitVector, KeySize);
        }

        private string Encrypt(bool bEncrypt, string sInputText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);
            byte[] plainTextBytes = null;
            byte[] cipherTextBytes = null;

            if (bEncrypt)
            {
                //Convert plaintext into a byte array.
                //Let us assume that plaintext contains UTF8-encoded characters.
                plainTextBytes = Encoding.UTF8.GetBytes(sInputText);
            }
            else
            {
                //Convert our ciphertext into a byte array.
                cipherTextBytes = Convert.FromBase64String(sInputText);
            }

            //First, we must create a password, from which the key will be derived.
            //This password will be generated from the specified passphrase and
            //salt value. The password will be created using the specified hash
            //algorithm. Password creation can be done in several iterations.
            var password = new PasswordDeriveBytes(passPhrase, saltValueBytes, hashAlgorithm, passwordIterations);

            //Use the password to generate pseudo-random bytes for the encryption
            //key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(keySize / 8);

            //Create uninitialized Rijndael encryption object.
            var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC };

            //It is reasonable to set encryption mode to Cipher Block Chaining
            //(CBC). Use default options for other symmetric key parameters.

            //Generate encryptor/decryptor from the existing key bytes and initialization
            //vector. Key size will be defined based on the number of the key
            //bytes.
            ICryptoTransform encryptor = bEncrypt ? symmetricKey.CreateEncryptor(keyBytes, initVectorBytes) : symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);

            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            int decryptedByteCount = 0;
            //Define memory stream which will be used to hold encrypted data.
            //Define cryptographic stream (always use Write mode for encryption & Read mode for encryption).
            if (bEncrypt)
            {
                memoryStream = new MemoryStream();
                cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                //Start encrypting.
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

                //Finish encrypting.
                cryptoStream.FlushFinalBlock();

                //Convert our encrypted data from a memory stream into a byte array.
                cipherTextBytes = memoryStream.ToArray();
            }
            else
            {
                memoryStream = new MemoryStream(cipherTextBytes);
                cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Read);

                //Since at this point we don't know what the size of decrypted data
                //will be, allocate the buffer long enough to hold ciphertext;
                //plaintext is never longer than ciphertext.
                plainTextBytes = new byte[cipherTextBytes.Length];

                //Start decrypting.
                decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            }

            //Close both streams.
            memoryStream.Close();
            cryptoStream.Close();

            string sReturnText = bEncrypt ? Convert.ToBase64String(cipherTextBytes) : Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            //Return encrypted/decrypted string.
            return sReturnText;
        }
    }
}