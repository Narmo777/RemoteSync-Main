using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public enum RequestType:byte //enum for all functions the app will do
    {
        Get=0,
        Kill,
        Rsc,
        Ok,
        Err
    }
    
    public struct Packet {
        public const int HeaderSize = sizeof(int) + sizeof(RequestType);
        
        public RequestType RequestType;
        public int DataSize;
        public byte[] Data;
        
        //packet builder, recive byte[]
        public Packet(RequestType requestType, byte[] data)
        {
            RequestType = requestType;
            DataSize = data.Length;
            Data = data;
        }
        //packet builder, recive string and converts to bytes using the protocol
        public Packet(RequestType requestType, string data)
        {
            RequestType = requestType;
            DataSize = data.Length;
            Data = ProtocolHelpers.StringToBytes(data);
        }

        public async static Task<Packet> FromNetworkStream(NetworkStream ns)
        {
            var header = new byte[HeaderSize]; //the first byte of the header is the request type, according to the enum
            int totalRead = 0, bytesRead = 0;

            // Read the network stream

            //read the header
            while (totalRead < HeaderSize && (bytesRead = await ns.ReadAsync(header, totalRead, HeaderSize - totalRead)) > 0)
            {
                totalRead += bytesRead;
            }

            if (totalRead != HeaderSize)
                throw new InvalidOperationException("Incomplete header read");

            //create variables
            var requestType = RequestTypeExtention.FromByte(header[0]);
            var dataLen = BitConverter.ToInt32(header, 1);
            var data = new byte[dataLen];

            totalRead = 0;
            // Read the data
            while (totalRead < dataLen && (bytesRead = await ns.ReadAsync(data, totalRead, dataLen - totalRead)) > 0)
            {
                totalRead += bytesRead;
            }

            if (totalRead != dataLen)
                throw new InvalidOperationException("Incomplete data read");

            return new Packet(requestType, data);
        }

        public static implicit operator byte[](Packet packet) //func to cast from packet to byte
        {
            var ret = new byte[packet.DataSize + HeaderSize];
            ret[0] = ((byte)packet.RequestType);
            Array.Copy(BitConverter.GetBytes(packet.DataSize), 0, ret, 1, HeaderSize - 1); //ret[1..(HeaderSize - 1)] = BitConverter.GetBytes(packet.DataSize);
            Array.Copy(packet.Data, 0, ret, HeaderSize,packet.DataSize);//ret[HeaderSize..] = packet.Data
            return ret;
        }

        public string GetContentAsString()
        {
            return UTF8Encoding.UTF8.GetString(Data);
        }
    }

    public static class ProtocolHelpers {
        public static byte[] StringToBytes(string s) => UTF8Encoding.UTF8.GetBytes(s);
    }
    public static class RequestTypeExtention
    {
        public static RequestType FromByte(byte b)
        {
            switch (b)
            {
                case 0: return RequestType.Get;
                case 1: return RequestType.Kill;
                case 2: return RequestType.Rsc;
                case 3: return RequestType.Ok;
                default: return RequestType.Err;
            };
        }
    }
}
