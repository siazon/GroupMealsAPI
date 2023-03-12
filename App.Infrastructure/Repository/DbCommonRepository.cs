using App.Domain;
using App.Domain.Config;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;

namespace App.Infrastructure.Repository
{
    public interface IDbCommonRepository<T> : IDbRepository<T> where T : DbEntity
    {
    }

    public class DbCommonRepository<T>
        : DbRepository<T>, IDbCommonRepository<T> where T : DbEntity
    {
        public DbCommonRepository(IOptions<DocumentDbConfig> dbConfig)
        {
            DbConfig = dbConfig.Value;
            CollectionId = typeof(T).Name;
            Client = new DocumentClient(new Uri(DbConfig.DocumentDbEndPoint),
                DbConfig.DocumentDbAuthKey, new ConnectionPolicy { EnableEndpointDiscovery = false });
            DatabaseId = DbConfig.DocumentDbName;
        }
    }
}