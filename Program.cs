using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using PWCrackingConsumer.PWCrack;

namespace PWCrackingConsumer
{
    public static class Program
    {
        public static PWCrackingService Pwc = new PWCrack.PWCrackingService();

        public delegate string[] CrackDelegate(string[] words, string[] userInfos);

        public static List<CrackDelegate> crackDelegates = new List<CrackDelegate>();

        static Program()
        {
            var a = new PWCrack.PWCrackingService();
            crackDelegates.Add(a.Crack);
            //            var b = new PWCrack2.PWCrackingService();
            //            crackDelegates.Add(b.Crack);
        }

        static void Main(string[] args)
        {
            //Split the file into an array
            var userInfos = ReadFile("passwords.txt");

            //var words = ReadFile("webster-dictionary.txt"); //311141 words
            var words = ReadFile("webster-dictionary-reduced.txt"); //5619 words

            Console.WriteLine(words[1] + "hi");
            /*
            var stopwatch = Stopwatch.StartNew();
            var result = SendRequests(words, userInfos, 1000);
            stopwatch.Stop();

            //Output results
            Console.WriteLine("____");
            Console.WriteLine("Final results:");
            foreach (var s in result)
            {
                Console.WriteLine("\t" + string.Join(", ", s));
            }
            Console.WriteLine("____");

            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);*/
        }

        public static string[] ReadFile(String filename)
        {
            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEnd().Split('\n');
            }
        }

        /// <summary>
        /// Sends web service requests.
        /// </summary>
        /// <param name="words">Array of words.</param>
        /// <param name="userInfos">Array of user info.</param>
        /// <param name="chunkSize">Amount of words per request.</param>
        /// <returns>A list of found user names and passwords.</returns>
        public static List<string[]> SendRequests(string[] words, string[] userInfos, int chunkSize)
        {
            var finalResult = new List<string[]>(); //The final result list
            var tasks = new Task<string[]>[(words.Length / chunkSize) + 1]; //Each request is a task
            var k = 0; //Which current request/task number
            var serviceId = 0; //The service to use

            //For every chunk of words
            for (var i = 0; i < words.Length; i += chunkSize)
            {
                Console.WriteLine("Started request #" + k + " (word #" + i + " to " + (i + chunkSize) + ")" + " service ID: " + serviceId);
                var request = new Request(SubArray(words, i, chunkSize), userInfos, serviceId); //Make a request with the chunk, userInfos and the service
                var task = Task.Factory.StartNew((Func<string[]>)request.DoIt); //Create a new task and run it
                tasks[k] = task; //Add to the task array
                k++; //New request/Task number

                serviceId++; //Select new service to use
                if (serviceId == crackDelegates.Count) //Loop around to the first service again when all services are used
                    serviceId = 0;
            }
            try
            {
                Task.WaitAll(tasks); //Wait for all requests to complete
                Console.WriteLine("All tasks done");
                foreach (var task in tasks)
                {
                    if (task.Result != null)
                        finalResult.Add(task.Result); //If the result isn't empty, add it to the final result
                }
            }
            catch (AggregateException)
            {
                Console.WriteLine("Something went wrong");
            }
            return finalResult;
        }

        /// <summary>
        /// Creates a sub-array.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="data">The original array.</param>
        /// <param name="index">Where to start from.</param>
        /// <param name="length">How long the sub-array is.</param>
        /// <returns>A sub-array of the original array.</returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            while ((index + length) > data.Length)
                length--;
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
