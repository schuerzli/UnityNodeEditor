using System;
using KeyZarNodeEditor;
using UnityEngine;

[Serializable]
public class ComprehensivePlugTest
{
    [InPlug]
    public void LenasMethode() { }

    [InPlug]
    public void InString(string str) { }

    [OutPlug]
    public Action OutVoid;

    [OutPlug]
    public Action<float> OutFloat;

    [OutPlug]
    public Action<int> OutInt;

    [OutPlug]
    public Action<string> OutString;

    public string stringConfVar;
    public float floatConfVar;

}

[Serializable]
public class MinimalPlugTest
{
    [InPlug]
    public void In() { }

    [OutPlug]
    public Action Out;

    public float someField;
}
