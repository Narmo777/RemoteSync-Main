﻿using System;
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
        private static MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
        //private static MongoClient dbClient = new MongoClient("mongodb+srv://Narmod:NimrodBenHamo85@remotesync.wgwarwp.mongodb.net/?retryWrites=true&w=majority&appName=RemoteSync\r\n\r\n");

        //public static IMongoCollection<BsonDocument> GetUserInfoCollection()
        //{
        //    MongoClient dbClient = new MongoClient("mongodb+srv://Nimrod:NimrodBenHamo85@cluster0.nvpsjki.mongodb.net/");
        //    var db = dbClient.GetDatabase("LoginSystem");
        //    var collection = db.GetCollection<BsonDocument>("UserInfo");

        //    return collection;
        //}
        public static bool IsUsernameExists (string username)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var existingUser = collection.Find(new BsonDocument("username", username)).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static bool IsUsernameExists(string username, string email)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var existingUser = collection.Find(new BsonDocument { { "username", username }, { "email", email } }).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static bool IsUserSignedUp(string username, string password)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var existingUser = collection.Find(new BsonDocument { { "username", username }, { "password", password } }).FirstOrDefault();
            if (existingUser != null)
                return true;
            else
                return false;
        }
        public static void InsertUser(string username, string password, string email) 
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            collection.InsertOne(new BsonDocument { { "username", username }, { "password", password }, { "email", email }, { "client", new BsonArray() } });
        }
        public static void ChangePassword(string user, string NewPassword)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");
            var filter = Builders<BsonDocument>.Filter.Eq("username", user);
            var update = Builders<BsonDocument>.Update.Set("password", NewPassword);

            collection.UpdateOne(filter, update);
        }
        public static async Task<List<Tuple<string, string, int>>> GetAllClientsAsync(string username)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");

            // Asynchronously find the document matching the username
            BsonDocument userClients = await collection.Find(new BsonDocument { { "username", username } }).FirstOrDefaultAsync();

            var clientTuplesList = new List<Tuple<string, string, int>>();

            if (userClients != null)
            {
                // Extract the client array from the document
                BsonArray clientArray = userClients.GetValue("client").AsBsonArray;

                if (clientArray != null)
                {
                    // Loop through each client object in the array
                    foreach (BsonDocument clientDoc in clientArray)
                    {
                        string clientName = clientDoc.GetValue("name").ToString();
                        string clientIp = clientDoc.GetValue("ip").ToString();
                        int clientIndex = int.Parse(clientDoc.GetValue("index").ToString());

                        // Add a tuple to the list
                        clientTuplesList.Add(Tuple.Create(clientName, clientIp, clientIndex));
                    }
                }
                else
                {
                    Console.WriteLine("Client array not found in the user data.");
                }
            }
            else
            {
                Console.WriteLine($"User with username '{username}' not found.");
            }

            return clientTuplesList;
        }
        //public static int GetClientsCount(string username)
        //{
        //    var db = dbClient.GetDatabase("LoginSystem");
        //    var collection = db.GetCollection<BsonDocument>("UserInfo");

        //    // Asynchronously find the document matching the username
        //    BsonDocument userClients = collection.Find(new BsonDocument { { "username", username } }).FirstOrDefault();

        //    int count = 0;  
        //    if (userClients != null)
        //    {
        //        // Extract the client array from the document
        //        BsonArray clientArray = userClients.GetValue("client").AsBsonArray;

        //        foreach (BsonDocument clientDoc in clientArray)
        //            count++;
        //    }

        //    return count;
        //}
        public static async Task RemoveDisconnectedClientAsync(string technician, string clientIP)
        {
            var db = dbClient.GetDatabase("LoginSystem");
            var collection = db.GetCollection<BsonDocument>("UserInfo");

            var filter = Builders<BsonDocument>.Filter.Eq("username", technician);
            var update = Builders<BsonDocument>.Update.PullFilter("client", Builders<BsonDocument>.Filter.Eq("ip", clientIP));

            await collection.UpdateOneAsync(filter, update);
        }
    }
}
