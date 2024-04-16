using App.Domain;
using App.Domain.Config;
using App.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace App.Infrastructure.Repository
{
    public interface IDbRepositoryV3<T> where T : DbEntity
    {
        Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate);
        Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null);

        Task<T> GetOneAsync(Expression<Func<T, bool>> predicate);

        Task<T> CreateAsync(T item);

        Task<T> UpdateAsync(T item);

        Task<T> DeleteAsync(T item);

        //Task CreateCollectionIfNotExists();

        //Task CreateDatabaseIfNotExists();

        void SetUpConnection(string documentDbEndPoint, string documentDbAuthKey, string documentDbName);
    }

    public class DbRepositoryV3<T>
        : IDbRepositoryV3<T> where T : DbEntity
    {
        protected CosmosClient _client;
        protected string CollectionId;
        protected string DatabaseId;
        protected DocumentDbConfig DbConfig;
        protected Container container;
        public Container getContainer(string databaseId)
        {
            return _client.GetDatabase(databaseId).GetContainer(typeof(T).Name);
        }

        public void SetUpConnection(string documentDbEndPoint, string documentDbAuthKey, string documentDbName)
        {
            _client = new CosmosClient(documentDbEndPoint, documentDbAuthKey);
            CollectionId = typeof(T).Name;
            container = getContainer(documentDbName);
        }


        public async Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                List<T> results = new List<T>();
                string continuation = null;
                var query = container.GetItemLinqQueryable<T>(true)
                         .Where(predicate);

                using (var iterator = query.ToFeedIterator())
                {
                    if (iterator.HasMoreResults)
                    {
                        FeedResponse<T> response = await iterator.ReadNextAsync();
                        results.AddRange(response.Resource);
                        continuation = response.ContinuationToken;

                        if (iterator.ReadNextAsync().Result.Count == 0)
                            continuation = null;
                    }
                }

                return results.FirstOrDefault();
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null)
        {
            try
            {
                string continuationToken = null;

                var queryRequestOptions = new QueryRequestOptions { MaxItemCount = pageSize, EnableScanInQuery = true, };

                IOrderedQueryable<T> linqQueryable = container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true, continuationToken: continuationToken, requestOptions: queryRequestOptions);
                List<T> results = linqQueryable.Where(predicate).ToList();

                var tokens = linqQueryable.ToFeedIterator();
                FeedResponse<T> responses = await tokens.ReadNextAsync();
                continuationToken = responses.ContinuationToken;

                return new KeyValuePair<string, IEnumerable<T>>(continuationToken, results);
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }



        public async Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                List<T> results = new List<T>();
                string continuation = null;
                var query = container.GetItemLinqQueryable<T>(true)
                         .Where(predicate);

                using (var iterator = query.ToFeedIterator())
                {
                    if (iterator.HasMoreResults)
                    {
                        FeedResponse<T> response = await iterator.ReadNextAsync();
                        results.AddRange(response.Resource);
                        continuation = response.ContinuationToken;

                        if (iterator.ReadNextAsync().Result.Count == 0)
                            continuation = null;
                    }
                }
                return results;
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
                var doc = await container.UpsertItemAsync(item);
                return doc.Resource;
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
                var doc = await container.UpsertItemAsync(item);
                return doc.Resource;
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
                var doc = await container.DeleteItemAsync<T>($"{item.Id}", new PartitionKey($"{item.Id}"));
                return doc.Resource;
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        //public async Task CreateCollectionIfNotExists()
        //{
        //    try
        //    {
        //        var result =
        //            await Client.ReadDocumentCollectionAsync(
        //                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
        //    }
        //    catch (Exception ex)
        //    {
        //        var e = (DocumentClientException)ex.InnerException;

        //        if (e.StatusCode == HttpStatusCode.NotFound)
        //            await Client.CreateDocumentCollectionAsync(
        //                UriFactory.CreateDatabaseUri(DatabaseId),
        //                new DocumentCollection { Id = CollectionId },
        //                new RequestOptions { OfferThroughput = 1000 });
        //        else
        //            throw new DataRepositoryException(ex);
        //    }
        //}

        //public async Task CreateDatabaseIfNotExists()
        //{
        //    try
        //    {
        //        await Client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
        //    }
        //    catch (DocumentClientException e)
        //    {
        //        if (e.StatusCode == HttpStatusCode.NotFound)
        //            await Client.CreateDatabaseAsync(new Database { Id = DatabaseId });
        //        else
        //            throw new DataRepositoryException(e);
        //    }
        //}
    }
}