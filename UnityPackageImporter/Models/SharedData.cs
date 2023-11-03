using FrooxEngine;
using System.Collections.Generic;

namespace UnityPackageImporter;

public class SharedData
{
    public Dictionary<string, string> FileName_To_AssetIDDict;
    public Dictionary<string, string> AssetIDDict;
    public List<string> ListOfMetas;
    public List<string> ListOfPrefabs;
    public Slot importTaskAssetRoot;

    public SharedData()
    {
        FileName_To_AssetIDDict = new Dictionary<string, string>();
        AssetIDDict = new Dictionary<string, string>();
        ListOfMetas = new List<string>();
        ListOfPrefabs = new List<string>();
    }

    public SharedData(Dictionary<string, string> FileName_To_AssetIDDict, Dictionary<string, string> AssetIDDict, List<string> ListOfMetas, List<string> ListOfPrefabs, Slot assetsroot)
    {
        this.FileName_To_AssetIDDict = FileName_To_AssetIDDict;
        this.AssetIDDict = AssetIDDict;
        this.ListOfMetas = ListOfMetas;
        this.ListOfPrefabs = ListOfPrefabs;
        this.importTaskAssetRoot = assetsroot;
    }
};