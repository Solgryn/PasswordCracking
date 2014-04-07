using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PWCrackingConsumer
{
    public static class Program
    {
        private const int ChunkSize = 1000; //The amount of words to send with each request

        private const string PASSWORDS_FILENAME = "../../passwords.txt";

//        private const string DICTIONARY_FILENAME = "../../webster-dictionary.txt"; //311141 words
        private const string DICTIONARY_FILENAME = "../../webster-dictionary-reduced.txt"; //5619 words

        public delegate string[] CrackDelegate(string[] words);
        public delegate void GiveUserInfoDelegate(string[] userInfos);

        public static List<CrackDelegate> CrackDelegates = new List<CrackDelegate>();
        public static List<GiveUserInfoDelegate> GiveUserInfoDelegates = new List<GiveUserInfoDelegate>();

        public static string[] UserInfos;

        static Program()
        {
            var localhost = new PWCrack.PWCrackingService();
            AddService(localhost.Crack, localhost.GiveUserInfo);
        }

        /// <summary>
        /// Adds a service to the delegate list.
        /// </summary>
        /// <param name="crackDelegate">The Crack() method delegate.</param>
        /// <param name="giveUserInfoDelegate">The GiveUserInfo() method delegate.</param>
        static void AddService(CrackDelegate crackDelegate, GiveUserInfoDelegate giveUserInfoDelegate)
        {
            CrackDelegates.Add(crackDelegate);
            GiveUserInfoDelegates.Add(giveUserInfoDelegate);
        }

        static void Main(string[] args)
        {
            //Split the file into an array
            UserInfos = ReadFile(PASSWORDS_FILENAME);
            var words = ReadFile(DICTIONARY_FILENAME);

            UpdateSlavesUserInfos();

            var stopwatch = Stopwatch.StartNew();
            var result = SendRequests(words, ChunkSize);
            stopwatch.Stop();

            //Output results
            Console.WriteLine("____");
            Console.WriteLine("Final results:");
            foreach (var s in result)
            {
                Console.WriteLine("\t" + string.Join(", ", s));
            }
            Console.WriteLine("____");

            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
        }

        /// <summary>
        /// Returns an array of a text file.
        /// </summary>
        /// <param name="filename">The file to read.</param>
        /// <returns></returns>
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
        public static List<string[]> SendRequests(string[] words, int chunkSize)
        {
            if (CrackDelegates.Count != GiveUserInfoDelegates.Count || CrackDelegates.Count == 0)
                throw new Exception("Problem in service delegates.");

            var finalResult = new List<string[]>(); //The final result list
            ConcurrentBag<int> indexes = new ConcurrentBag<int>();

            //For every chunk of words
            for (var i = 0; i < words.Length; i += chunkSize)
            {
                indexes.Add(i);
            }

            List<Thread> threads = new List<Thread>();
            //thread for each service
            for (int i = 0; i < CrackDelegates.Count; ++i)
            {
                int serviceId = i;
                Thread thread = new Thread(() =>
                {
                    while (!indexes.IsEmpty)
                    {
                        int index;
                        if (indexes.TryTake(out index))
                        {
                            Console.WriteLine("Started request (word " + index + " to " + (index + chunkSize) + ")" + " service ID: " + serviceId);
                            var request = new Request(SubArray(words, index, chunkSize), serviceId);
                            var result = request.DoIt(); //TODO: if fails, return index to bag or call another service
                            if (result != null)
                            {
                                lock (finalResult)
                                {
                                    finalResult.Add(result);
                                }
                            }
                        }
                    }
                });
                threads.Add(thread);
                thread.Start();
            }

            //wait for all service threads
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            Console.WriteLine("All tasks done");
            /*
            TODO: update exception handling
            catch (AggregateException e)
            {
                e.Handle((x) =>
                {
                    if (x is WebException)
                    {
                        Console.WriteLine("WebException({0}): {1}", serviceId, x.Message);
                        if (x.InnerException is SocketException)
                        {
                            Console.WriteLine("SocketException({0}): {1}", serviceId, x.InnerException.Message);
                        }
                        return true;
                    }
                    Console.WriteLine("Unknown {0}({1}): {2}", x.GetType(), serviceId, x.Message);
                    return true;
                });
            }
            */
            return finalResult;
        }

        /// <summary>
        /// Updates the UserInfo list on each web service.
        /// </summary>
        public static void UpdateSlavesUserInfos()
        {
            Console.WriteLine("Updating slaves..");
            foreach (var giveUserInfoDelegate in GiveUserInfoDelegates)
            {
                giveUserInfoDelegate(UserInfos); //TODO: add exception handling
            }
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
