using Chimera.Unity3DYamlChecker;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using UnityEngine;

namespace UnityPackageImporter.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
                .Build();
            var yml = ""; // TODO Load
            // var p = deserializer.Deserialize<GameObject>(yml);

            UnityNodeTypeResolver res = new UnityNodeTypeResolver();
        }
    }
}