using System;
using NUnit.Framework;
using TOS;

namespace TestTOS
{
    public class TestEnv
    {
        public readonly string AccessKey = Environment.GetEnvironmentVariable("TOS_ACCESS_KEY");
        public readonly string SecretKey = Environment.GetEnvironmentVariable("TOS_SECRET_KEY");
        public readonly string Region = Environment.GetEnvironmentVariable("TOS_REGION");
        public readonly string EndPoint = Environment.GetEnvironmentVariable("TOS_ENDPOINT");
        public readonly string CallbackUrl = Environment.GetEnvironmentVariable("TOS_CALLBACK_URL");

        public TosClientBuilder PrepareClientBuilder() 
        {
            return TosClientBuilder.Builder().SetAk(AccessKey).SetSk(SecretKey).SetEndpoint(EndPoint).SetRegion(Region);
        }
        
        public ITosClient PrepareClient() 
        {
            return  PrepareClientBuilder().Build();
        }
    }
}