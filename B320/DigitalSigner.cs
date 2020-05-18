using System.Security.Cryptography;

namespace B320
{
    public sealed class DigitalSigner
    {
        private readonly string _hashAlgorithm;
        private readonly int _keySize = 2048;
        private RSAParameters _publicKey;
        private RSAParameters _privateKey;

        public DigitalSigner(string hashAlgorithm = Constants.HASH_SIGNATURE_ALGORITHM)
        {
            _hashAlgorithm = hashAlgorithm;
            GenerateKeys();
        }

        private void GenerateKeys()
        {
            using RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(_keySize)
            {
                PersistKeyInCsp = false
            };

            _publicKey = cryptoServiceProvider.ExportParameters(false);
            _privateKey = cryptoServiceProvider.ExportParameters(true);
        }

        public byte[] Sign(byte[] hashed)
        {
            using RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(_keySize)
            {
                PersistKeyInCsp = false
            };

            cryptoServiceProvider.ImportParameters(_privateKey);

            string mapNameToOid = CryptoConfig.MapNameToOID(_hashAlgorithm);
            byte[] signed = cryptoServiceProvider.SignHash(hashed, mapNameToOid);
            return signed;
        }

        public bool Verify(byte[] hashed, byte[] signature)
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(_keySize);
            rsa.ImportParameters(_publicKey);

            bool verified = rsa.VerifyHash(hashed, CryptoConfig.MapNameToOID(_hashAlgorithm), signature);
            return verified;
        }
    }
}