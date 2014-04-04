using System;

namespace PWCrackingConsumer
{
    public class Request
    {
        private readonly string[] _words;
        private readonly int _serviceId;

        /// <summary>
        /// Constructs a request object.
        /// </summary>
        /// <param name="words">Which words to request.</param>
        /// <param name="userInfos">Which user info to request.</param>
        /// <param name="serviceId">Which service to use.</param>
        public Request(string[] words, int serviceId)
        {
            _words = words;
            _serviceId = serviceId;
        }

        /// <summary>
        /// Requests with the given data.
        /// </summary>
        /// <returns>Returns an array of found usernames and passwords.</returns>
        public string[] DoIt()
        {
            var result = Program.CrackDelegates[_serviceId](_words);

            if (result.Length > 0)
            {
                Console.WriteLine("\tFound:");
                foreach (var s in result)
                {
                    var parts = s.Split(':');
                    Console.WriteLine("\t\t" + string.Join(", ", s));
                }
                return result; //Return the result if something is found
            }
            return null;
        }
    }
}
