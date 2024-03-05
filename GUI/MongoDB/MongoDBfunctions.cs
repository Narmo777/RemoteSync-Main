using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Authentication;

namespace GUI.MongoDB
{
    public class MongoDBfunctions
    {
        public static IMongoCollection<BsonDocument> GetUserInfoCollection()
        {
            MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");

            return collection;
        }
        public static bool IsUsernameExists (string username, IMongoCollection<BsonDocument> collection)
        {
            var existingUser = collection.Find(new BsonDocument("username", username)).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static bool IsUsernameExists(string username, string email, IMongoCollection<BsonDocument> collection)
        {
            var existingUser = collection.Find(new BsonDocument { { "username", username }, { "email", email } }).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static bool IsUserSignedUp(string username, string password, IMongoCollection<BsonDocument> collection)
        {
            var existingUser = collection.Find(new BsonDocument { { "username", username }, { "password", password } }).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static void InsertUser(string username, string password, string email, IMongoCollection<BsonDocument> collection) 
        {
            collection.InsertOne(new BsonDocument { { "username", username }, { "password", password }, { "email", email } });
        }
        public static void ChangePassword(string user, string NewPassword, IMongoCollection<BsonDocument> collection)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("username", user);
            var update = Builders<BsonDocument>.Update.Set("password", NewPassword);

            collection.UpdateOne(filter, update);
        }
    }
}
