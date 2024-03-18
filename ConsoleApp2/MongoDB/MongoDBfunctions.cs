using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApp2.MongoDB
{
    public class MongoDBfunctions
    {
        private static MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");

        public static bool IsTechnicianExists(string username)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var existingUser = collection.Find(new BsonDocument("username", username)).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static void InsertNewClient(string technician, string clientName, string clientIP)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var document = new BsonDocument
            {
                { "name", clientName },
                { "ip", clientIP }
            };
            var filter = Builders<BsonDocument>.Filter.Eq("username", technician);
            var update = Builders<BsonDocument>.Update.PushEach("client", new BsonArray { document }, position: 0);

            collection.UpdateOne(filter, update);
        }
    }
}
