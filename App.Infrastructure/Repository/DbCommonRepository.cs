using App.Domain;
using App.Domain.Config;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;

namespace App.Infrastructure.Repository
{
    public interface IDbCommonRepository<T> : IDbRepositoryV3<T> where T : DbEntity
    {
    }

    public class DbCommonRepository<T>
        : DbRepositoryV3<T>, IDbCommonRepository<T> where T : DbEntity
    {
        public DbCommonRepository(IOptions<DocumentDbConfig> dbConfig)
        {
            DbConfig = dbConfig.Value;
            CollectionId = typeof(T).Name;
            DatabaseId = DbConfig.DocumentDbName;

            //Client = new DocumentClient(new Uri(DbConfig.DocumentDbEndPoint),
            //    DbConfig.DocumentDbAuthKey, new ConnectionPolicy { EnableEndpointDiscovery = false });
          

            _client = new CosmosClient(DbConfig.DocumentDbEndPoint, DbConfig.DocumentDbAuthKey);
            container = getContainer(DatabaseId);
        }
    }
}