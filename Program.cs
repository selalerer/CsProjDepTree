using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CsProjDepTree
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                    Console.WriteLine($"Usage: CsProjDepTree CSPROJ_FILE");
                    return 1;
            }

            var csproj = args[0];
            Console.WriteLine($"CSPROJ_FILE={csproj}");

            var deps = GetDependencies(Path.GetFullPath(csproj));

            PrintDeps(deps);

            return 0;
        }

        private static void PrintDeps(Deps deps, int depth = 0)
        {
            if (deps == null)
                return;
            foreach(var dep in deps.Dependencies)
            {
                Console.WriteLine($"{new string('\t', depth)}{dep.Key}");
                PrintDeps(dep.Value, depth + 1);
            }
        }

        private class Deps 
        {
            public Dictionary<string, Deps> Dependencies { get; } = new Dictionary<string, Deps>();
        }

        private static Deps GetDependencies(string csproj)
        {
            return GetDependencies(csproj, new HashSet<string>());
        }

        private static Deps GetDependencies(string csproj, HashSet<string> handledCsProj)
        {
            //Console.WriteLine($"TRACE: GetDependencies({csproj})");
            var baseName = Path.GetFileName(csproj);
            if (handledCsProj.Contains(baseName))
                    return null;
            handledCsProj.Add(baseName);

            var csprojFolder = Path.GetDirectoryName(csproj);
            var csprojContent = File.ReadAllText(csproj);

            var result = new Deps();
            foreach(var reference in GetAllProjectReferences(csprojContent))
            {
                var innerCsProj = reference;
                //Console.WriteLine($"TRACE: GetDependencies(): innerCsProj={innerCsProj}");
                innerCsProj = innerCsProj.Replace("\\", "/"); 
                innerCsProj = Path.Combine(csprojFolder, innerCsProj);
                //Console.WriteLine($"TRACE: GetDependencies(): innerCsProj after combine ={innerCsProj}");
                innerCsProj = Path.GetFullPath(innerCsProj);
                //Console.WriteLine($"TRACE: GetDependencies(): innerCsProj after get full path ={innerCsProj}");
                result.Dependencies.Add(Path.GetFileName(innerCsProj), GetDependencies(innerCsProj, handledCsProj));
            }
            return result;
        }
        
        private static IEnumerable<string> GetAllProjectReferences(string csprojContent)
        {
            const string PROJ_REF_START = "<ProjectReference Include=\"";
            
            var result = new List<string>();
            var projRefIndex = csprojContent.IndexOf(PROJ_REF_START);
            while(projRefIndex != -1)
            {
                var searchStartIndex = projRefIndex + PROJ_REF_START.Length;
                if (searchStartIndex > csprojContent.Length - 1)
                    break;
                var endQuotesIndex = csprojContent.IndexOf('"', searchStartIndex);
                if (endQuotesIndex == -1)
                {
                    result.Add(csprojContent.Substring(searchStartIndex));
                    break;
                }
                else
                {
                    result.Add(csprojContent.Substring(searchStartIndex, endQuotesIndex - searchStartIndex));
                }
                projRefIndex = csprojContent.IndexOf(PROJ_REF_START, endQuotesIndex);
            }
            
            return result;
        }
    }
}
