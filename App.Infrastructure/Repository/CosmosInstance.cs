using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.TwiML.Voice;

namespace App.Infrastructure.Repository
{
    public class CosmosInstance
    {
        private static CosmosInstance cosmosInstance;

        private CosmosInstance()
        {
            guid=Guid.NewGuid().ToString();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static CosmosInstance GetInstance(string endPoint,string key)
        {
            if (cosmosInstance == null)
            {
                _client = new CosmosClient(endPoint,  key );
                cosmosInstance = new CosmosInstance();
            }
            return cosmosInstance;
        }
        public static CosmosClient _client;
        public static string guid;

       
    }
}
