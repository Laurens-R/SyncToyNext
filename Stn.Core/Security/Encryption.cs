using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.Security
{
    internal class Encryption
    {
        public static string Key {  
            get
            {
                //todo: if this project gets any more traction we need to come up with something
                //way more secure. This right now is just to prevent dat people randomly hitting local
                //STN config files don't instantly can make out usernames and passwords etc. But right now
                //if they have access to these source files: THEY ARE A SECURITY CONCERN. This needs 
                //to be addressed by using some proper keyvault/secret store type solution. Or if that
                //is complicated be replaced by user provided passwords.
                return Environment.UserName;
            }
        }

        private static byte[] GetAes256KeyFromString(string keyString)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
        }

        private static byte[] GenerateIV()
        {
            var iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        public static string EncryptString(string input)
        {
            var key = GetAes256KeyFromString(Key); // 32 bytes
            var iv = GenerateIV(); // 16 bytes

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(input);
                    }
                    var encryptedBytes = msEncrypt.ToArray();

                    // Combine IV + encryptedBytes
                    var result = new byte[iv.Length + encryptedBytes.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string DecryptString(string input)
        {
            var key = GetAes256KeyFromString(Key); // 32 bytes
            var fullCipher = Convert.FromBase64String(input);

            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);

            // Extract ciphertext
            var cipherText = new byte[fullCipher.Length - iv.Length];
            Buffer.BlockCopy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new MemoryStream(cipherText))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }   
    }
}
