using System;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using simple.esb;
using System.Threading.Tasks;

namespace simple.esb.mongo
{
    public class SagaData
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public object Data { get; set; }
    }

    public class LockInfo
    {
        [BsonId]
        public ObjectId ResourceId { get; set; }
        public DateTime Expires { get; set; }
        public Guid Owner { get; set; }
    }

    public static class MongoHelper
    {
        public static void Register<T>()
        {
            BsonClassMap.RegisterClassMap<T>();
        }
    }

    public class MongoDoHickey : IDataStore
    {
        private readonly MongoOptions _options;

        public MongoDoHickey(MongoOptions options)
        {
            _options = options;
        }

        public object Get(string propertyName, object propertyValue)
        {
            var collection = GetCollectionForSagaData();
            var wrapper = GetWrapper(collection, propertyName, propertyValue);
            return wrapper?.Data;
        }

        public void Save(string propertyName, object propertyValue, object dataInstanceValue)
        {
            var collection = GetCollectionForSagaData();
            SagaData dataWrapper = GetWrapper(collection, propertyName, propertyValue);

            bool created = false;

            if (dataWrapper == null)
            {
                created = true;
                dataWrapper = new SagaData() { DateCreated = DateTime.UtcNow };
            }

            dataWrapper.Data = dataInstanceValue;
            dataWrapper.DateUpdated = DateTime.UtcNow;

            if (created)
            {
                collection.InsertOne(dataWrapper);
            }
            else
            {
                var filter = Builders<SagaData>.Filter.Eq("Data." + propertyName, propertyValue);
                var result = collection.ReplaceOne(filter, dataWrapper, new UpdateOptions { IsUpsert = false });
            }
        }

        public void Delete(string propertyName, object propertyValue)
        {
            var collection = GetCollectionForSagaData();
            var filter = Builders<SagaData>.Filter.Eq("Data." + propertyName, propertyValue);
            var result = collection.DeleteOne(filter);

            if (result.DeletedCount == 0)
            {
                // todo: unable to delete item - has it already been deleted?
            }
        }

        public async Task<IDisposable> LockResource(string propertyName, object propertyValue)
        {
            var collection = GetCollectionForSagaData();
            SagaData dataWrapper = GetWrapper(collection, propertyName, propertyValue);

            var locks = GetCollectionForLocks();

            var lockObject = new DistributedLock(locks, dataWrapper.Id);
            return await lockObject.AquireLock();
        }

        private SagaData GetWrapper(IMongoCollection<SagaData> collection, string propertyName, object propertyValue)
        {
            var filter = Builders<SagaData>.Filter.Eq("Data." + propertyName, propertyValue);
            var list = collection.Find(filter).ToList();
            if (list.Count > 1)
            {
                throw new InvalidOperationException("to many items found --- should be unique.");
            }

            return list.SingleOrDefault();
        }

        private IMongoCollection<SagaData> GetCollectionForSagaData()
        {
            IMongoClient c = new MongoClient(_options.Host);
            IMongoDatabase db = c.GetDatabase(_options.Database);
            var collection = db.GetCollection<SagaData>(_options.Collection);

            return collection;
        }

        private IMongoCollection<LockInfo> GetCollectionForLocks()
        {
            IMongoClient c = new MongoClient(_options.Host);
            IMongoDatabase db = c.GetDatabase(_options.Database);
            var collection = db.GetCollection<LockInfo>(_options.Collection + "Locks");

            return collection;
        }
    }

    public class DistributedLock : IDisposable
    {
        private readonly Guid me = Guid.NewGuid();

        private readonly IMongoCollection<LockInfo> _locks;
        private readonly ObjectId _resource;

        public DistributedLock(IMongoCollection<LockInfo> locks, ObjectId resource)
        {
            _locks = locks;
            _resource = resource;
        }

        public async Task<DistributedLock> AquireLock()
        {
            while (!TryAquireLock())
            {
                await Task.Delay(1000);
            }

            return this;
        }

        public void Dispose()
        {
            Console.WriteLine("Removing the lock");
            var result = _locks.DeleteOne(x => x.ResourceId == _resource && x.Owner == me);

            if (result.DeletedCount == 0)
            {
                Console.WriteLine("WARNING!  Your lock expired before it could be deleted.");
            }
        }

        private bool TryAquireLock()
        {
            var foundItem = _locks.Find(x => x.ResourceId == _resource).FirstOrDefault();
            if (foundItem != null)
            {
                Console.Write("Lock Found! ");
                if (DateTime.UtcNow < foundItem.Expires)
                {
                    Console.WriteLine("{0} remaining till expiration.", foundItem.Expires.Subtract(DateTime.UtcNow));
                    return false;
                }

                Console.Write("It expired {0} ago. ", DateTime.UtcNow.Subtract(foundItem.Expires));
                _locks.DeleteOne(x => x.ResourceId == _resource && x.Expires == foundItem.Expires);
                Console.WriteLine("Removed the lock.");
            }

            Console.WriteLine("Creating a new lock!");
            try
            {
                _locks.InsertOne(new LockInfo
                {
                    ResourceId = _resource,
                    Expires = DateTime.UtcNow.AddSeconds(25),
                    Owner = me,
                });
            }
            catch (MongoWriteException ex)
            {
                Console.WriteLine("Failed to create lock Reason: {0}", ex.Message);
                return false;
            }

            return true;
        }
    }
}
