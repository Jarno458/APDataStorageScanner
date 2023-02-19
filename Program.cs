using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        static void Main()
        {
	        session = ArchipelagoSessionFactory.CreateSession("archipelago.gg");

			var result = session.TryConnectAndLogin("", "Jarno", ItemsHandlingFlags.NoItems, tags: new [] { "TextOnly" });
            if (!result.Successful)
            {
                Console.WriteLine($"Failed to connect {string.Join(", ", ((LoginFailure)result).Errors)}");
                Console.ReadLine();
				return;
            }
			
            session.DataStorage["DataStorageScanner_FoundKeys"].Initialize(Array.Empty<string>());
            session.DataStorage["DataStorageScanner_LastQuery"].Initialize(Array.Empty<int>());

            Console.WriteLine($"Found Keys: {session.DataStorage["DataStorageScanner_FoundKeys"].To<JArray>()}");

			do
            {
                Console.WriteLine($"Press ENTER to start scan at \"{CharsToString(session.DataStorage["DataStorageScanner_LastQuery"].To<List<int>>())}\"");
            } while (Console.ReadKey().Key != ConsoleKey.Enter);

			session.Socket.PacketReceived += PacketReceived;

            try
            {
                FindDataStorageKeys();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine(e.StackTrace);
			}

			Console.WriteLine("Done");
            Console.WriteLine($"Found Keys: {session.DataStorage["DataStorageScanner_FoundKeys"].To<JArray>()}");

			Console.ReadLine();
        }

		static void PacketReceived(ArchipelagoPacketBase packet)
		{
			if (!(packet is RetrievedPacket retrievedPacket)) return;

			foreach (var data in retrievedPacket.Data)
			{
                if (data.Value.Type != JTokenType.Null)
                {
					if (data.Key == "DataStorageScanner_FoundKeys" || data.Key == "DataStorageScanner_LastQuery")
						continue;
                    
                    Console.WriteLine($"Found key: {data.Key}, with value: {data.Value}");

					session.DataStorage["DataStorageScanner_FoundKeys"] += new []{ data.Key };
				}
			}
		}

		static void FindDataStorageKeys()
        {
	        List<int> chars = session.DataStorage["DataStorageScanner_LastQuery"].To<List<int>>();
			List<string> keys = new List<string>(BatchSize);

	        while (chars.Count <= MaxKeySize)
	        {
		        keys.Add(IncrementChar(chars, chars.Count - 1));

		        if (keys.Count >= BatchSize)
                {
                    session.DataStorage["DataStorageScanner_LastQuery"] = chars;

                    Console.WriteLine($"Completed batch: \"{CharsToString(chars)}\"");

				    QueryKeys(keys);

					keys.Clear();
		        }
	        }
        }

        static void QueryKeys(List<string> keys)
        {
            Task.Run(() =>
            {
                session.Socket.SendPacketAsync(new GetPacket
                {
                    Keys = keys.ToArray()
                });
            });
        }

        static string IncrementChar(List<int> chars, int charPos)
        {
	        if (charPos < 0)
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

	        return IncrementChar(chars, --charPos);
		}

        static string CharsToString(List<int> chars) => 
	        new string(chars.Select(i => charactersToTest[i]).ToArray());
    }
}
