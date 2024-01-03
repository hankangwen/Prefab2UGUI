public class AutoBuildTemplate
{
    public static string UIClass =
        @"using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class #类名# : MonoBehaviour
{
    #成员#
    //auto
    private void Awake()
    {
        #查找#
    }

    private void OnDestroy()
    {
        
    }
}
";
}
