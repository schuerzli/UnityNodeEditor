using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KeyZarNodeEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InPlug : Attribute {}

    [AttributeUsage(AttributeTargets.Field)]
    public class OutPlug : Attribute {}

    [AttributeUsage(AttributeTargets.Class)]
    public class NodeAttribute : Attribute {}
}