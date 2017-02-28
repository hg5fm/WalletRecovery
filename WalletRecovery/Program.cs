using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerkeleyDB;
using System.IO;

namespace WalletRecovery
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please specify the filename of the wallet to be recovered and a fileName for the new recovered wallet");
                return;
            }
            string srcWalletFileName = args[0];
            string destWalletFileName = args[1];

            var dump = Salvage(srcWalletFileName);
            
            var keyValDict = ParseWalletDump(dump);

            CreateNewWallet(keyValDict, destWalletFileName);

            Console.WriteLine("Done!");
        }


        private static DatabaseConfig cfg;

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static Dictionary<byte[], byte[]> ParseWalletDump(string txtDump)
        {
            var allLines = txtDump.Split('\n');
            int lineIdx = 0;
            //ignore header
            var result = new Dictionary<byte[], byte[]>();
            do
            {
                var line = allLines[lineIdx];
                if (line == "HEADER=END")
                    break;
                lineIdx++;
            } while (lineIdx < allLines.Length);

            // collect the key-value pairs
            do
            {
                lineIdx++;
                var key = allLines[lineIdx];                
                if (key == "DATA=END")
                    break;
                lineIdx++;
                var val = allLines[lineIdx];
                if (val == "DATA=END")
                    break;
                var keyBytes = StringToByteArray(key.Trim());
                var valBytes = StringToByteArray(val.Trim());
                result.Add(keyBytes, valBytes);
                
            } while (lineIdx <= allLines.Length);

            return result;
        }

        private static string Salvage(string walletDbFileName, bool aggressive = false)
        {
            cfg = new DatabaseConfig
            {
                ErrorPrefix = "Salvage: ",
                ErrorFeedback = (pfx, msg) => Console.WriteLine(pfx + msg)
            };
            using (StringWriter writer = new StringWriter())
            {
                BerkeleyDB.Database.Salvage(walletDbFileName, cfg, false, aggressive, writer);
                return writer.ToString();
            }
            
        }

        static void CreateNewWallet(Dictionary<byte[], byte[]> keyValDict, string dbfileCopy)
        {
            File.Delete(dbfileCopy);
            var environment = DatabaseEnvironment.Open(null,
            new DatabaseEnvironmentConfig()
            {
                Create = true,
                Private = true,
                UseMPool = true,
                ErrorPrefix = "Env: ",
                ErrorFeedback = (pfx, msg) => Console.WriteLine(pfx + msg),
            });

            var targetDb = BTreeDatabase.Open(dbfileCopy, "main",
                new BTreeDatabaseConfig()
                {

                    Env = environment,
                    Creation = CreatePolicy.ALWAYS,
                    ErrorPrefix = "Table: ",
                    ErrorFeedback = (pfx, msg) => Console.WriteLine(pfx + msg),
                });

            foreach (var item in keyValDict)
            {
                targetDb.Put(new DatabaseEntry(item.Key), new DatabaseEntry(item.Value));
            }
            targetDb.Close();
            environment.Close();
        }

    }

}
