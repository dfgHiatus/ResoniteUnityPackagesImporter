using System.Collections.Generic;
using System.Threading.Tasks;
using FrooxEngine;
using UnityPackageImporter.FrooxEngineRepresentation;

namespace UnityPackageImporter.Models;

public interface IUnityStructureImporter
{
    Dictionary<ulong, IUnityObject> existingIUnityObjects { get; set; }
    UnityProjectImporter unityProjectImporter { get; set; }
    ProgressBarInterface progressIndicator { get; set; }
    Slot CurrentStructureRootSlot { get; set; }
    public Slot allimportsroot { get; set; }
    public KeyValuePair<string,string> ID { get; set; }
    Task StartImport();
}
