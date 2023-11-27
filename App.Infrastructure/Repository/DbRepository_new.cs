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
    public interface IDbRepository_new<T> where T : DbEntity
    {
        //Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate);
        Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null);

        Task<T> GetOneAsync(Expression<Func<T, bool>> predicate);

        //Task<T> CreateAsync(T item);

        //Task<T> UpdateAsync(T item);

        //Task<T> DeleteAsync(T item);

        //Task CreateCollectionIfNotExists();

        //Task CreateDatabaseIfNotExists();

        void SetUpConnection(string documentDbEndPoint, string documentDbAuthKey, string documentDbName);
    }

    public class DbRepository_new<T>
        : IDbRepository_new<T> where T : DbEntity
    {
        private CosmosClient _client;
        protected string CollectionId;
        protected string DatabaseId;
        protected DocumentDbConfig DbConfig;
        protected Container container;
        private Container getContainer(string databaseId)
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
            QueryDefinition query = new QueryDefinition("SELECT * FROM "+ CollectionId);
            string continuation = null;
            
            List<T> results = new List<T>();
            using (FeedIterator<T> resultSetIterator = container.GetItemQueryIterator<T>(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    MaxItemCount = 5, 
                }))

               

            // Execute query and get 1 item in the results. Then, get a continuation token to resume later
            while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<T> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }

                    // Get continuation token once we've gotten > 0 results. 
                    if (response.Count > 0)
                    {
                        continuation = response.ContinuationToken;
                        break;
                    }
                }

            // Check if query has already been fully drained
            if (continuation == null)
            {
                return null;
            }

            // Resume query using continuation token
            using (FeedIterator<T> resultSetIterator = container.GetItemQueryIterator<T>(
                    query,
                    requestOptions: new QueryRequestOptions()
                    {
                        MaxItemCount = -1
                    },
                    continuationToken: continuation))
            {
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<T> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }
                }
            }
            return null;
        }

        public async Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null)
        {
            List<T> results = new List<T>();
            string continuation=null;
            var query = container.GetItemLinqQueryable<T>(true, continueToken, new QueryRequestOptions { MaxItemCount= pageSize, EnableScanInQuery=true,  })
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

            return new KeyValuePair<string, IEnumerable<T>>(continuation, results);
        }
        //public async Task<(IEnumerable<T> Results, string ContinuationToken)> Where<T>(Expression<Func<T, bool>> pred, 
        //    int maxRecords = 0, string partitionKey = "", string continuationToken = "") where T : IDocumentModel
        //{

        //    QueryRequestOptions options = new QueryRequestOptions();

        //    if (partitionKey != "")
        //        options.PartitionKey = new PartitionKey(partitionKey);

        //    if (maxRecords == 0)
        //    {
        //        return (Container.GetItemLinqQueryable<T>(true, null, options).Where(x => x.Type == typeof(T).Name).Where(pred), "");
        //    }
        //    else
        //    {
        //        options.MaxItemCount = maxRecords;
        //        string token = "";
        //        FeedIterator<T> feed;
        //        List<T> res = new List<T>();

        //        if (continuationToken == "")
        //            feed = Container.GetItemLinqQueryable<T>(true, null, options).Where(x => x.Type == typeof(T).Name).Where(pred).ToFeedIterator();
        //        else
        //            feed = Container.GetItemLinqQueryable<T>(true, continuationToken, options).Where(x => x.Type == typeof(T).Name).Where(pred).ToFeedIterator();

        //        Microsoft.Azure.Cosmos.FeedResponse<T> f = await feed.ReadNextAsync();
        //        token = f.ContinuationToken;

        //        foreach (var item in f)
        //        {
        //            res.Add(item);
        //        }

        //        return (res, token);
        //    }

        //}


        //public async Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate)
        //{
        //    try
        //    {
        //        var query = Client.CreateDocumentQuery<T>(
        //                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
        //                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
        //            .Where(predicate).AsEnumerable();
        //        return query;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

        //public async Task<KeyValuePair<string, IEnumerable<T>>> GetManyAsync(Expression<Func<T, bool>> predicate, int pageSize = -1, string continueToken = null)
        //{
        //    try
        //    {
        //        FeedResponse<T> feedRespose = null;// = await query.ExecuteNextAsync<T>();
        //        IDocumentQuery<T> query = null;
        //        List<T> documents = new List<T>();
        //        var options = new FeedOptions
        //        {
        //            MaxItemCount = pageSize,
        //            EnableCrossPartitionQuery = true,
        //            RequestContinuation = continueToken,
        //            EnableScanInQuery = true
        //        };
        //        query = Client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), options).Where(predicate).AsDocumentQuery();
        //        while (query.HasMoreResults)
        //        {
        //            feedRespose = await query.ExecuteNextAsync<T>();
        //            documents.AddRange(feedRespose);
        //            return new KeyValuePair<string, IEnumerable<T>>(feedRespose.ResponseContinuation, documents);
        //        }
        //        string continuation = null;
        //        if (feedRespose != null) { continuation = feedRespose.ResponseContinuation; }
        //        return new KeyValuePair<string, IEnumerable<T>>(continuation, documents);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

        //public async Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
        //{
        //    try
        //    {
        //        var query = Client.CreateDocumentQuery<T>(
        //                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
        //                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
        //            .Where(predicate).AsEnumerable();
        //        return query.FirstOrDefault();
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

        //public async Task<T> CreateAsync(T item)
        //{
        //    try
        //    {
        //        var doc =
        //            await Client.CreateDocumentAsync(
        //                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        //        return JsonConvert.DeserializeObject<T>(doc.Resource.ToString());
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

        //public async Task<T> UpdateAsync(T item)
        //{
        //    try
        //    {
        //        var doc = await Client.ReplaceDocumentAsync(
        //            UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id), item);
        //        return JsonConvert.DeserializeObject<T>(doc.Resource.ToString());
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

        //public async Task<T> DeleteAsync(T item)
        //{
        //    try
        //    {
        //        var doc =
        //            await Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, item.Id),
        //                new RequestOptions { PartitionKey = new PartitionKey(Undefined.Value) });
        //        return item;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new DataRepositoryException(e);
        //    }
        //}

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