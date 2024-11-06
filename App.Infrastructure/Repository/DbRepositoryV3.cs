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
using static FluentValidation.Validators.PredicateValidator;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using App.Domain.TravelMeals.Restaurant;

namespace App.Infrastructure.Repository
{
    public interface IDbRepositoryV3<T> where T : DbEntity
    {
        Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate);
        Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null);
        Task<KeyValuePair<string, IEnumerable<T>>> GetManyBySqlAsync(string sql, int pageSize = -1, string continueToken = null);

        Task<T> GetOneAsync(Expression<Func<T, bool>> predicate);

        Task<T> CreateAsync(T item);

        Task<T> UpsertAsync(T item);

        Task<T> DeleteAsync(T item);

        //Task CreateCollectionIfNotExists();

        //Task CreateDatabaseIfNotExists();

    }

    public class DbRepositoryV3<T>
        : IDbRepositoryV3<T> where T : DbEntity
    {
        protected string CollectionId;
        protected DocumentDbConfig DbConfig;
        protected Container container;
        public Container getContainer(string databaseId,string endPoint,string authKey)
        {
            CosmosInstance.GetInstance(endPoint, authKey);
            CosmosClient _client = CosmosInstance._client;  // new CosmosClient(DbConfig.DocumentDbEndPoint, DbConfig.DocumentDbAuthKey);

            return _client.GetDatabase(databaseId).GetContainer(typeof(T).Name);
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
                if (!string.IsNullOrWhiteSpace(continueToken))
                    continuationToken = continueToken;

                //await Where(predicate, pageSize, continueToken, null);


                var queryRequestOptions = new QueryRequestOptions { MaxItemCount = pageSize, EnableScanInQuery = true, };

                IOrderedQueryable<T> linqQueryable = container.GetItemLinqQueryable<T>(true, continuationToken, queryRequestOptions);


                FeedIterator<T> feed;
                if(typeof(T)==typeof(TrDbRestaurant))
                    feed = linqQueryable.Where(predicate).OrderBy(a=>a.SortOrder).ThenByDescending(a=>a.Created).ToFeedIterator();
                else
                    feed = linqQueryable.Where(predicate).OrderByDescending(a => a.Created).ToFeedIterator();

                List<T> results = new List<T>();
                FeedResponse<T> response = await feed.ReadNextAsync();
                results.AddRange(response);
                continuationToken = response.ContinuationToken;

                return new KeyValuePair<string, IEnumerable<T>>(continuationToken, results);
            }
            catch (Exception e)
            {
                throw new DataRepositoryException(e);
            }
        }

        public async Task<(IEnumerable<T> Results, string ContinuationToken)> Where<T>(Expression<Func<T, bool>> pred, int maxRecords = 0, string partitionKey = "", string continuationToken = "")
        {

            QueryRequestOptions options = new QueryRequestOptions();

            if (partitionKey != "")
                options.PartitionKey = new PartitionKey(partitionKey);


            if (maxRecords == 0)
            {
                return (container.GetItemLinqQueryable<T>(true, null, options).Where(pred), "");
            }
            else
            {
                options.MaxItemCount = maxRecords;
                string token = "";
                FeedIterator<T> feed;
                List<T> res = new List<T>();

                if (continuationToken == "")
                    feed = container.GetItemLinqQueryable<T>(true, null, options).Where(pred).ToFeedIterator();
                else
                    feed = container.GetItemLinqQueryable<T>(true, continuationToken, options).Where(pred).ToFeedIterator();

                Microsoft.Azure.Cosmos.FeedResponse<T> f = await feed.ReadNextAsync();
                token = f.ContinuationToken;

                foreach (var item in f)
                {
                    res.Add(item);
                }

                return (res, token);
            }

        }

        public async Task<KeyValuePair<string, IEnumerable<T>>> GetManyBySqlAsync(string sql, int pageSize = -1, string continueToken = null)
        {
            try
            {
                List<T> results = new List<T>();
                string continuationToken = null;
                if (!string.IsNullOrWhiteSpace(continueToken))
                    continuationToken = continueToken;

                var queryRequestOptions = new QueryRequestOptions { MaxItemCount = pageSize, };
                QueryDefinition queryDefinition = new($"select * from {typeof(T).Name} t where 1=1 {sql}");
                using (FeedIterator<T> feedIterator = container.GetItemQueryIterator<T>(queryDefinition, continuationToken, queryRequestOptions))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<T> response = await feedIterator.ReadNextAsync();
                        results.AddRange(response);
                        continuationToken = response.ContinuationToken;
                    }
                }

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
                         .Where(predicate).OrderByDescending(a=>a.Created);

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
        
        public async Task<T> UpsertAsync(T item)
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