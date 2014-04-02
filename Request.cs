using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PWCrackingConsumer
{
    public class Request
    {
        private readonly string[] _words;
        private readonly string[] _userInfos;
        private readonly int _serviceId;

        /// <summary>
        /// Constructs a request object.
        /// </summary>
        /// <param name="words">Which words to request.</param>
        /// <param name="userInfos">Which user info to request.</param>
        /// <param name="serviceId">Which service to use.</param>
        public Request(string[] words, string[] userInfos, int serviceId)
        {
            _words = words;
            _userInfos = userInfos;
            _serviceId = serviceId;
        }

        /// <summary>
        /// Requests with the given data.
        /// </summary>
        /// <returns>Returns an array of found usernames and passwords.</returns>
        public string[] DoIt()
        {
            var result = Program.CrackDelegates[_serviceId](_words, _userInfos);

            if (result.Length > 0)
            {
                Console.WriteLine("\tFound something:");
                foreach (var s in result)
                {
                    Console.WriteLine("\t\t" + string.Join(", ", s));
                }
                return result; //Return the result if something is found
            }
            return null;
        }
    }
}
