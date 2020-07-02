using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace SerAPI.Utils
{
    public class AWS
    {
        private readonly ILogger _logger;
        private IConfiguration _config;

        public AWS(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public string PreSignedS3URL(string objectKey)
        {
            string urlString = "";

            using (var client = new AmazonS3Client(
                    _config.GetSection("AWS").GetSection("AccessKeyId").Value,
                    _config.GetSection("AWS").GetSection("SecretAccessKey").Value,
                    RegionEndpoint.USEast1))
            {
                _logger.LogDebug("entra");
                try
                {
                    GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
                    {
                        BucketName = _config.GetSection("AWS").GetSection("S3").GetSection("Bucket").Value,
                        Key = objectKey,
                        Expires = DateTime.Now.AddMinutes(5)
                    };
                    urlString = client.GetPreSignedURL(request);
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                }
                catch (Exception e)
                {
                    _logger.LogError("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                }

                return urlString;
            }
        }
    }
}
