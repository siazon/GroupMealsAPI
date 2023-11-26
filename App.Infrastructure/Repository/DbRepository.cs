using App.Domain;
using App.Domain.Config;
using App.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace App.Infrastructure.Repository
{
    public interface IDbRepository<T> where T : DbEntity
    {
        Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate);
        Task<KeyValuePair<string,IEnumerable<T>>> GetManyAsync( Expression<Func<T, bool>> predicate, int pageSize=-1, string continueToken=null);

        Task<T> GetOneAsync(Expression<Func<T, bool>> predicate);

        Task<T> CreateAsync(T item);

        Task<T> UpdateAsync(T item);

        Task<T> DeleteAsync(T item);

        Task CreateCollectionIfNotExists();

        Task CreateDatabaseIfNotExists();

        void SetUpConnection(string documentDbEndPoint, string documentDbAuthKey, string documentDbName);
    }

    public class DbRepository<T>
        : IDbRepository<T> where T : DbEntity
    {
        protected DocumentClient Client;
        protected string CollectionId;
        protected string DatabaseId;
        protected DocumentDbConfig DbConfig;


        public void SetUpConnection(string documentDbEndPoint, string documentDbAuthKey, string documentDbName)
        {
            
            CollectionId = typeof(T).Name;
            Client = new DocumentClient(new Uri(documentDbEndPoint),
                documentDbAuthKey, new ConnectionPolicy { EnableEndpointDiscovery = false });
            DatabaseId = documentDbName;

        }

        public async Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var query = Client.CreateDocumentQuery<T>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                    .Where(predicate).AsEnumerable();
                return query;
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<KeyValuePair<string,IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate,  int pageSize = -1, string continueToken = null)
        {
            try
            {
                FeedResponse<T> feedRespose=null;// = await query.ExecuteNextAsync<T>();
                IDocumentQuery<T> query = null;
                List<T> documents = new List<T>();
                    var options = new FeedOptions
                    {
                        MaxItemCount = pageSize,
                        EnableCrossPartitionQuery = true,
                        RequestContinuation = continueToken,
                        EnableScanInQuery = true
                    };
                    query = Client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), options).Where(predicate).AsDocumentQuery();
                    while (query.HasMoreResults)
                    {
                        feedRespose =await query.ExecuteNextAsync<T>();
                        documents.AddRange(feedRespose);
                        return new KeyValuePair<string, IEnumerable<T>>(feedRespose.ResponseContinuation, documents);
                    }
                string continuation = null;
                if(feedRespose != null) { continuation = feedRespose.ResponseContinuation; }
                return new KeyValuePair<string, IEnumerable<T>>(continuation, documents);
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var query = Client.CreateDocumentQuery<T>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                    .Where(predicate).AsEnumerable();
                return query.FirstOrDefault();
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<T> CreateAsync(T item)
        {
            try
            {
                var doc =
                    await Client.CreateDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                return JsonConvert.DeserializeObject<T>(doc.Resource.ToString());
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<T> UpdateAsync(T item)
        {
            try
            {
                var doc = await Client.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id), item);
                return JsonConvert.DeserializeObject<T>(doc.Resource.ToString());
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<T> DeleteAsync(T item)
        {
            try
            {
                var doc =
                    await Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id),
                        new RequestOptions { PartitionKey = new PartitionKey(Undefined.Value) });
                return item;
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task CreateCollectionIfNotExists()
        {
            try
            {
                var result =
                    await Client.ReadDocumentCollectionAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (Exception ex)
            {
                var e = (DocumentClientException)ex.InnerException;

                if (e.StatusCode == HttpStatusCode.NotFound)
                    await Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                else
                    throw new DataRepositoryException(ex);
            }
        }

        public async Task CreateDatabaseIfNotExists()
        {
            try
            {
                await Client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    await Client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                else
                    throw new DataRepositoryException(e);
            }
        }
    }
}