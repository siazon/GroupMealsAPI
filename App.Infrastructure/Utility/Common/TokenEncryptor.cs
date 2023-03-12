using JWT;
using JWT.Algorithms;
using JWT.Serializers;

namespace App.Infrastructure.Utility.Common
{
    public interface ITokenEncryptorHelper
    {
        T Decrypt<T>(string data);

        string Encrypt(object data);
    }

    public class TokenEncryptorHelper : ITokenEncryptorHelper
    {
        private const string PrivateKey = "CgFc74MMFHU2aTxc756SX8Ybb08JqvE2";

        public T Decrypt<T>(string data)
        {
            try
            {
                var serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                var validator = new JwtValidator(serializer, provider);
                var urlEncoder = new JwtBase64UrlEncoder();
                var decoder = new JwtDecoder(serializer, validator, urlEncoder);

                return decoder.DecodeToObject<T>(data, PrivateKey, verify: true);
            }
            catch (TokenExpiredException)
            {
                throw;
            }
            catch (SignatureVerificationException)
            {
                throw;
            }
        }

        public string Encrypt(object data)
        {
            var algorithm = new HMACSHA256Algorithm();
            var serializer = new JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(data, PrivateKey);
        }
    }
}