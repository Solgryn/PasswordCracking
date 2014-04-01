using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PWCrackingConsumer.PWCrack;

namespace PWCrackingConsumer
{
    static class Program
    {
        public static PWCrackingService Pwc = new PWCrack.PWCrackingService();
        static void Main(string[] args)
        {
            //Split the file into an array
            var userInfos = ReadFile("passwords.txt");

            //var words = ReadFile("webster-dictionary.txt"); //311141 words
            //var result = SendRequests(words, userInfos, 500);

            var words = ReadFile("webster-dictionary-reduced.txt"); //5619 words
            var result = SendRequests(words, userInfos, 1000);

            //var words = new[] { "BOAT", "someword", "secret", "yep", "power", "Flower" };
            //var result = SendRequests(words, userInfos, 4);

            //Using a small array for testing purposes
            //var result = Pwc.Crack(new[] { "BOAT", "someword", "secret", "Flower" }, userInfos);
            
            //Output results
            Console.WriteLine("____");
            Console.WriteLine("Final results:");
            foreach (var s in result)
            {
                Console.WriteLine("\t" + string.Join(", ", s));
            }
        }

        public static string[] ReadFile(String filename)
        {
            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEnd().Split('\n');
            }
        }

        public static List<string[]> SendRequests(string[] words, string[] userInfos, int chunkSize)
        {
            var finalResult = new List<string[]>();
            for (var i = 0; i < words.Length; i+=chunkSize)
            {
                Console.WriteLine("Sent request #" + (i/chunkSize) + " (word #" + i + " to " + (i+chunkSize) + ")");
                var result = Pwc.Crack(SubArray(words, i, chunkSize), userInfos);
                if (result.Length > 0)
                {
                    finalResult.Add(result);
                    Console.WriteLine("\tFound something:");
                    foreach (var s in result)
                    {
                        Console.WriteLine("\t\t" + string.Join(", ", s));
                    }
                }
            }
            return finalResult;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            while ((index + length) > data.Length)
                length--;
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /*
        public static string[,] SplitArray(string[] array, int parts)
        {
            var partLength = array.Length/parts;
            var result = new string[parts, partLength];
            var id = 0;

            for (var i = 0; i < parts; i++)
            {
                for (var k = 0; k < partLength; k++)
                {
                    result[i, k] = array[id];
                    id++;
                }
            }

            return result;
        }*/
    }
}
