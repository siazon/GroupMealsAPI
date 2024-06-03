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
      
            container = getContainer(DbConfig.DocumentDbName, DbConfig.DocumentDbEndPoint, DbConfig.DocumentDbAuthKey);
        }
    }
}