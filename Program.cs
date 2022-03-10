using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;

namespace APDataStorageScanner
{
    class Program
    {
	    const int MaxKeySize = 20;
		const int BatchSize = 10000;

	    static char[] charactersToTest =
	    {
		    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
		    'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
		    'u', 'v', 'w', 'x', 'y', 'z','A','B','C','D','E',
		    'F','G','H','I','J','K','L','M','N','O','P','Q','R',
		    'S','T','U','V','W','X','Y','Z','1','2','3','4','5',
		    '6','7','8','9','0','!','$','#','@','-', ':', ';', 
		    '+', '-', '|'
	    };

	    static ArchipelagoSession session;

        static void Main(string[] args)
        {
	        session = ArchipelagoSessionFactory.CreateSession("localhost");
			session.TryConnectAndLogin("Archipelago", "Spectator", new Version(0, 2, 4), ItemsHandlingFlags.NoItems, new List<string> { "IgnoreGame", "TextOnly" });

			session.DataStorage.Initialize("key", 0);

			session.Socket.PacketReceived += PacketReceived;

			FindDataStorageKeys();

			Console.ReadLine();
        }

		static void PacketReceived(ArchipelagoPacketBase packet)
		{
			if (!(packet is RetrievedPacket retrievedPacket)) return;

			foreach (var data in retrievedPacket.Data)
			{
				if (data.Value.Type != JTokenType.Null)
					Console.WriteLine($"Found key: {data.Key}, with value: {data.Value}");
			}
		}

		static void FindDataStorageKeys()
        {
	        List<int> chars = new List<int>(MaxKeySize);
	        List<string> keys = new List<string>(BatchSize);

	        while (chars.Count <= MaxKeySize)
	        {
		        keys.Add(IncrementChar(chars, 0));

		        if (keys.Count >= BatchSize)
		        {
			        QueryKeys(keys);

					keys.Clear();
		        }
	        }
        }

        static void QueryKeys(List<string> keys)
        {
	        session.Socket.SendPacketAsync(new GetPacket {
				Keys = keys.ToArray()
	        });
        }

        static string IncrementChar(List<int> chars, int charPos)
        {
	        if (charPos == chars.Count)
	        {
		        chars.Add(0);

		        return CharsToString(chars);
			}

			if (chars[charPos] < charactersToTest.Length - 1)
	        {
		        chars[charPos]++;

				return CharsToString(chars);
	        }

	        chars[charPos] = 0;

	        return IncrementChar(chars, ++charPos);
		}

        static string CharsToString(List<int> chars) => 
	        new string(chars.Select(i => charactersToTest[i]).ToArray());
    }
}
