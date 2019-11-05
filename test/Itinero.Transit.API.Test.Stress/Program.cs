using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Itinero.Transit.API.Tests.Functional;

namespace Itinero.Transit.API.Test.Stress
{
    class Program
    {
        private static void Main(string[] _)
        {
            Console.WriteLine("Stresstesting the staging server");

            // Warning shot
            if (!RunChallenge())
            {
                throw new Exception("Server offline");
            }


            var i = 0;
            while (true)
            {
                i++;
                RunTests(i);
            }

        }
    private static void RunTests(int target = 50){

            ServicePointManager.DefaultConnectionLimit = target;
            ThreadPool.SetMinThreads(target, target);
            
            var list = new List<int>();
            for (var i = 0; i < target; i++)
            {
                list.Add(i);
            }


            var start = DateTime.Now;
            var results = list.AsParallel()
                    .WithDegreeOfParallelism(target)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(i =>
                    {
                        var chStart = DateTime.Now;
                        var result = RunChallenge(25000);
                        var chEnd = DateTime.Now;
                        return (result, chStart, (uint) (chEnd - chStart).TotalMilliseconds);
                    })
                    .OrderBy(v => v.Item2)
                    .ToList()
                ;
            var end = DateTime.Now;

            var data = "";
            foreach (var (success, relStart, timeNeeded) in results)
            {
                data += $"{success},{(uint) (relStart - start).TotalMilliseconds},{timeNeeded}\n";
            }
            Console.WriteLine(data);
            var count = 0;
            foreach (var x in results)
            {
                if (x.Item1) count++;
            }

            Console.WriteLine($"Success count: {count}/{target}");
            File.WriteAllText($"output{target}.csv", data);
        }

        private static bool RunChallenge(int range = 5000)
        {
            var stressTester = new ItineroTransitServerTest();
            var challenge = stressTester.GenerateRandomSncbChallenge(range);

            
            try
            {
                stressTester.ChallengeAsync(stressTester.KnownUrls["staging"], challenge, 10000)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}