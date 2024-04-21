using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityPackageImporter.FrooxEngineRepresentation;

using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;

namespace UnityPackageImporter.Models
{
    public  class YamlToFrooxEngine
    {
        public static Dictionary<ulong, IUnityObject> parseYaml(string FilePath)
        {

            Dictionary<ulong, IUnityObject> existingIUnityObjects = new Dictionary<ulong, IUnityObject>();
            //begin the parsing of our prefabs.

            //parse loop
            //now using the power of yaml we can make this a bit more reliable and hopefully smaller.
            //reading unity prefabs as yaml allows us to much more easily obtain the data we need.
            //Unity yamls are different, but with a little trickery we can still read them with a library.
            UnityNodeTypeResolver noderesolver = new UnityNodeTypeResolver();
            var deserializer = new DeserializerBuilder().WithNodeTypeResolver(noderesolver)
                .IgnoreUnmatchedProperties()
            //.WithNamingConvention(NullNamingConvention.Instance)//outta here with that crappy conversion!!!! We got unity crap we deal with unity crap. - @989onan
                .Build();
            using var sr = File.OpenText(FilePath);
            var parser = new Parser(sr);
            parser.Consume<StreamStart>();
            DocumentStart variable;
            while (parser.Accept<DocumentStart>(out variable) == true)
            {
                // Deserialize the document
                try
                {
                    try
                    {

                        UnityEngineObjectWrapper docWrapped = deserializer.Deserialize<UnityEngineObjectWrapper>(parser);
                        IUnityObject doc = docWrapped.Result();
                        doc.id = noderesolver.anchor; //this works because they're separate documents and we're deserializing them one by one. Not nessarily in order, we're just gathering them.
                                                      //since deserializing happens before adding to the list and those are done syncronously with each other, it is fine.
                        existingIUnityObjects.Add(doc.id, doc);
                    }
                    catch (Exception e)
                    {

                        try
                        {

                            IUnityObject doc = new FrooxEngineRepresentation.GameObjectTypes.NullType();
                            doc.id = noderesolver.anchor;
                            existingIUnityObjects.Add(doc.id, doc);
                        }
                        catch (ArgumentException e2)
                        {/*idc.*/
                            UnityPackageImporter.Msg("Duplicate key probably for Yaml\"" + FilePath + "\"just ignore this.");
                            UnityPackageImporter.Warn(e2.Message + e2.StackTrace);
                        }
                        throw e;

                    }
                }
                catch (Exception e)
                {
                    UnityPackageImporter.Msg("Couldn't evaluate node type for Yaml\"" + FilePath + "\". stacktrace below");
                    UnityPackageImporter.Warn(e.Message + e.StackTrace);
                    throw e; //TODO: REMOVE
                }



            }

            return existingIUnityObjects;
        }
    }
}
