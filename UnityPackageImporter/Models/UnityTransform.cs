using Elements.Core;
using System.Collections.Generic;

namespace UnityPackageImporter;

public class UnityTransform
{
    public float3 Position;
    public floatQ Rotation;
    public float3 Scale;
    public string EntityID;
    public string SlotParentID;
    public List<string> TransformChildren;
}