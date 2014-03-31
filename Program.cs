using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using PWCrackingConsumer.PWCS;

namespace PWCrackingConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var pwc = new PWCS.PWCrackingService();

            //Split the file into an array
            var userInfos = ReadFile("passwords.txt");

            //Using a small array for testing purposes
            UserInfo userInfo = new UserInfo();
            userInfo.Username = "user";
            userInfo.EntryptedPasswordBase64 = "asd";
            var result = pwc.Crack(new[] { "BOAT", "someword", "secret", "Flower" }, new [] { userInfo });
//            var result = pwc.Crack(new[] { "BOAT", "someword", "secret", "Flower" }, userInfos);
            //Output results
            foreach (UserInfoClearText s in result)
            {
                Console.WriteLine(s.UserName + ": " + s.Password);
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
    }
}
