﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace PWCrackingConsumer
{
    public static class Program
    {
        private const int CHUNK_SIZE = 1000;

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
            var result = SendRequests(words, CHUNK_SIZE);
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
            var finalResult = new List<string[]>(); //The final result list
            var tasks = new Task<string[]>[(words.Length / chunkSize) + 1]; //Each request is a task
            var k = 0; //Which current request/task number
            var serviceId = 0; //The service to use

            //For every chunk of words
            for (var i = 0; i < words.Length; i += chunkSize)
            {
                Console.WriteLine("Started request #" + k + " (word #" + i + " to " + (i + chunkSize) + ")" + " service ID: " + serviceId);
                var request = new Request(SubArray(words, i, chunkSize), serviceId); //Make a request with the chunk and the service
                var task = Task.Factory.StartNew((Func<string[]>)request.DoIt); //Create a new task and run it
                tasks[k] = task; //Add to the task array
                k++; //New request/Task number

                serviceId++; //Select new service to use
                if (serviceId == CrackDelegates.Count) //Loop around to the first service again when all services are used
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
            return finalResult;
        }

        public static void UpdateSlavesUserInfos()
        {
            Console.WriteLine("Updating slaves..");
            foreach (var giveUserInfoDelegate in GiveUserInfoDelegates)
            {
                giveUserInfoDelegate(UserInfos);
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
