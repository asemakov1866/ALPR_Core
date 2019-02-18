using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using NLog;


namespace ALPR_Core
{ 
    public class MongoDB
    {
       public string ConnectionString;
       string dbName;
       NLog.Logger _Log = CLogger.Instance().getLogger();

       private readonly object dbLock = new object();

        public MongoDB(string connectionString, string db)
        {
            ////this.ConnectionString = connectionString;
            ////this.dbName = db;
        }

        public IMongoCollection<BsonDocument> ConnectToDB(string connectionString, string databaseName, string collectionName)
        {
            
                MongoClient client = new MongoClient(connectionString);
            ////var database = client.GetDatabase(databaseName);

            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
            ////    var collection = database.GetCollection<BsonDocument>(collectionName);
            ////    return collection;
            ////}
            ////catch (Exception ex)
            ////{
            ////    _Log.Debug(ex.ToString());
            ////    return null;
            ////}

            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}

            return null;

        }

        public bool WriteDataToDatabase(object[] obj, string CollectionName)
        {

            ////if (obj[0] is ALPR_Car)
            ////{
            ////    for (int i = 0; i < obj.Length; i++) 
            ////    {
            ////        ALPR_Car tempobj = (ALPR_Car)obj[i];
            ////        tempobj.TimeRecorded = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            ////        obj[i] = tempobj;
            ////    }
            ////}
            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    var collection = ConnectToDB(ConnectionString, dbName, CollectionName);

            ////    if (collection == null) return false;
            ////    else
            ////    {
            ////        if (obj.Length == 1)
            ////        {
            ////            BsonDocument doc = (BsonDocument)obj[0].ToBsonDocument();
            ////            Task task = collection.InsertOneAsync(doc);
            ////            _Log.Debug("One record was written to MongoDB");
            ////        }
            ////        else
            ////        {
            ////            BsonDocument[] docs = new BsonDocument[obj.Length];
            ////            for (int i = 0; i < obj.Length; i++) docs[i] = (BsonDocument)obj[i].ToBsonDocument();
            ////            Task task = collection.InsertManyAsync(docs);
            ////            _Log.Debug(string.Format("{0} records was written to MongoDB", obj.Length));
            ////        }
            ////    }
    return true;
            ////}
            ////catch(Exception ex)
            ////{
            ////    _Log.Debug(ex.ToString());
            ////    return false;
            ////}
            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}
            ///


        }

        public void UpdateVisitorStatusInDatabase(string lp, string status)
        {
            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    var collection = ConnectToDB(ConnectionString, dbName, "Visitors");
            ////    var filter = Builders<BsonDocument>.Filter.Eq("LicensePlate", lp);
            ////    var f = collection.Find((FilterDefinition<BsonDocument>)filter).ToListAsync().Result;
            ////    var update = Builders<BsonDocument>.Update.Set("Status", status);
            ////    var result = collection.UpdateManyAsync(filter, update);

            ////}
            ////catch { }
            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}

        }

        //public async Task DeleteItem(string CollectionName, string parameterName, string parameterValue)
        //{
        //    var collection = ConnectToDB(ConnectionString, dbName, CollectionName);

        //    //deleting single record
        //    var Deleteone = await collection.FindOneAndDeleteAsync(
        //                 Builders<BsonDocument>.Filter.Eq(parameterName, parameterValue));

        //    ////deleting multiple record
        //    //var DelMultiple = await collection.DeleteManyAsync(
        //    //                 Builders<BsonDocument>.Filter.Lt("MasterID", 1000));
        //}

        public void DeleteItem(string CollectionName, string parameterName, string parameterValue)
        {
            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    var collection = ConnectToDB(ConnectionString, dbName, CollectionName);

            ////    //deleting single record
            ////    DeleteResult result = collection.DeleteOne(Builders<BsonDocument>.Filter.Eq(parameterName, parameterValue));
            ////}
            ////catch { }
            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}

        }

        public long GetLatestTime()
        {
            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    var collection = ConnectToDB(ConnectionString, dbName, "Visitor_Log");
            ////    if (collection == null) return DateTimeOffset.UtcNow.ToUnixTimeSeconds();                                                                                      // Check for connection to MongoDB


            ////    var result = collection.Find(new BsonDocument()).ToListAsync().Result;

            ////    long LatestTime = 0;

            ////    foreach (var data in result)
            ////    {
            ////        Visitor_Car car = BsonSerializer.Deserialize<Visitor_Car>(data);
            ////        if (long.Parse(car.Time) > LatestTime) LatestTime = long.Parse(car.Time);
            ////    }
            ////    return LatestTime;
            ////}
            ////catch
            ////{
            return 0;
            ////}
            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}
        }

        public Visitor_Car[] PopulateVisitorDictionary()
        {
            ////try
            ////{
            ////    Monitor.Enter(dbLock);

            ////    List<Visitor_Car> Visitor_Car_list = new List<Visitor_Car>();
            ////    var collection = ConnectToDB(ConnectionString, dbName, "Visitors");                                                                            
            ////    var result = collection.Find(new BsonDocument()).ToListAsync().Result;
            ////    foreach (var data in result) Visitor_Car_list.Add(BsonSerializer.Deserialize<Visitor_Car>(data));
            ////    return Visitor_Car_list.ToArray();
            ////}
            ////catch
            ////{
                return null;
            ////}
            ////finally
            ////{
            ////    Monitor.Exit(dbLock);
            ////}
        }
        public Resident_Car[] PopulateResidentDictionary()
        {
            List<Resident_Car> Resident_Car_list = new List<Resident_Car>();
            ////var collection = ConnectToDB(ConnectionString, dbName, "Residents");
            ////var result = collection.Find(new BsonDocument()).ToListAsync().Result;
            ////foreach (var data in result) Resident_Car_list.Add(BsonSerializer.Deserialize<Resident_Car>(data));
            return Resident_Car_list.ToArray();
            ///
           
        }
    }
}
