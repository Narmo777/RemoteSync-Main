using Amazon.Runtime.Documents;
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
    public class MongoDBfunctionsServer
    {

        private static MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
        //private static MongoClient dbClient = new MongoClient("mongodb+srv://Narmod:NimrodBenHamo85@remotesync.wgwarwp.mongodb.net/?retryWrites=true&w=majority&appName=RemoteSync\r\n\r\n");


        private static int clientcount = 0;
        public static bool IsTechnicianExists(string username)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var existingUser = collection.Find( new BsonDocument("username", username)).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static void InsertNewClient(string technician, string clientName, string clientIP)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            
            clientcount = GetClientsCount(technician);
            
            var document = new BsonDocument
            {
                { "name", clientName },
                { "ip", clientIP },
                { "index", clientcount }
            };
            var filter = Builders<BsonDocument>.Filter.Eq("username", technician);
            var update = Builders<BsonDocument>.Update.PushEach("client", new BsonArray { document }, position: 0);

            clientcount++;

            collection.UpdateOne(filter, update);
        }
        public static int GetClientsCount(string username)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");

            // Asynchronously find the document matching the username
            BsonDocument userClients = collection.Find(new BsonDocument { { "username", username } }).FirstOrDefault();

            int count = 0;
            if (userClients != null)
            {
                // Extract the client array from the document
                BsonArray clientArray = userClients.GetValue("client").AsBsonArray;

                foreach (BsonDocument clientDoc in clientArray)
                    count++;
            }

            return count;
        }
    }
}
