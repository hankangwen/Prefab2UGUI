> 本文由 [简悦 SimpRead](http://ksria.com/simpread/) 转码， 原文地址 [mp.weixin.qq.com](https://mp.weixin.qq.com/s/NLvfOnLV4e8YinVg-HQ-NQ?poc_token=HFcojmWjqhdnNI3PDzbBWUyDKLevSI2kPHUqL8jZ)

## Unity：使用预制体自动生成代码脚本的技术实现

**1、简述**

在 Unity 游戏开发中，使用预制体（Prefab）是一种有效的方式来重复利用对象和组件，提高开发效率。然而，当项目规模逐渐增大时，手动编写与每个预制体相关联的代码脚本可能会变得繁琐。为了提高开发效率，我们可以通过自动生成代码的方式，根据预制体的属性和组件生成相应的代码脚本。

**2、为什么需要自动生成代码？**

手动编写每个预制体相关的代码脚本可能会导致以下问题：

- 重复劳动： 随着预制体数量的增加，手动创建与之关联的代码变得冗长且容易出错。

- 维护困难： 当需要修改预制体的结构或属性时，需要手动同步修改相关的代码，容易出现不一致性。

**3、技术实现**

**3.1 反射（Reflection）**

Unity 中的反射机制允许我们在运行时获取和使用程序集中的类型信息。通过反射，我们可以动态地获取预制体的属性和组件信息，从而生成相应的代码。

```
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AutoBuild
{
    [MenuItem("生成/创建或刷新界面")]
    public static void BuildUIScript()
    {
        var dicUIType = new Dictionary<string, string>();
        dicUIType.Add("Img", "Image");
        dicUIType.Add("Btn", "Button");
        dicUIType.Add("Txt", "Text");
        dicUIType.Add("Tran", "Transform");
        dicUIType.Add("Input", "InputField");
        dicUIType.Add("Raw", "RawImage");
        dicUIType.Add("Drop", "Dropdown");
        dicUIType.Add("Slider", "Slider");
        dicUIType.Add("Scr", "Scrollbar");

        GameObject[] selectobjs = Selection.gameObjects;

        foreach (GameObject go in selectobjs)
        {
            //选择的物体
            GameObject selectobj = go.transform.root.gameObject;
            //物体的子物体
            Transform[] _transforms = selectobj.GetComponentsInChildren<Transform>(true);
            List<Transform> childList = new List<Transform>(_transforms);
            //UI需要查询的物体
            var mainNode = from trans in childList where trans.name.Contains('_') && dicUIType.Keys.Contains(trans.name.Split('_')[0]) select trans;
            var nodePathList = new Dictionary<string, string>();
            //循环得到物体路径
            foreach (Transform node in mainNode)
            {
                Transform tempNode = node;
                string nodePath = tempNode.name;
                if (tempNode.parent != selectobj.transform)
                {
                    nodePath = "/" +  tempNode.name;
                }

                while (tempNode != tempNode.root && (tempNode.parent != selectobj.transform))
                {
                    tempNode = tempNode.parent;
                    int index = nodePath.IndexOf('/');
                    string nodeName =  tempNode.name;
                    if (tempNode.parent != selectobj.transform)
                    {
                        nodeName = "/" +  tempNode.name;
                    }
                    nodePath = nodePath.Insert(index, nodeName);
                }
                nodePathList.Add(node.name, nodePath);
            }

            //成员变量字符串
            string memberstring = "";
            //查询代码字符串
            string loadedcontant = "";

            foreach (Transform itemtran in mainNode)
            {
                string typeStr = dicUIType[itemtran.name.Split('_')[0]];

                memberstring += "private " + typeStr + " " + itemtran.name + " = null;\r\n\t";
                loadedcontant += itemtran.name + " = " + "transform.Find(\"" + nodePathList[itemtran.name] + "\").GetComponent<" + typeStr + ">();\r\n\t\t";
            }
            string scriptPath = Application.dataPath + "/Scripts/" + selectobj.name + ".cs";

            string classStr = "";

            //如果已经存在了脚本，则只替换//auto下方的字符串
            if (File.Exists(scriptPath))
            {
                FileStream classfile = new FileStream(scriptPath, FileMode.Open);
                StreamReader read = new StreamReader(classfile);
                classStr = read.ReadToEnd();
                read.Close();
                classfile.Close();
                File.Delete(scriptPath);

                string splitStr = "//auto";
                string unchangeStr = Regex.Split(classStr, splitStr, RegexOptions.IgnoreCase)[0];
                string changeStr = Regex.Split(AutoBuildTemplate.UIClass, splitStr, RegexOptions.IgnoreCase)[1];

                StringBuilder build = new StringBuilder();
                build.Append(unchangeStr);
                build.Append(splitStr);
                build.Append(changeStr);
                classStr = build.ToString();
            }
            else
            {
                classStr = AutoBuildTemplate.UIClass;
            }

            classStr = classStr.Replace("#类名#", selectobj.name);
            classStr = classStr.Replace("#查找#", loadedcontant);
            classStr = classStr.Replace("#成员#", memberstring);

            FileStream file = new FileStream(scriptPath, FileMode.CreateNew);
            StreamWriter fileW = new StreamWriter(file, System.Text.Encoding.UTF8);
            fileW.Write(classStr);
            fileW.Flush();
            fileW.Close();
            file.Close();
            Debug.Log("创建脚本 " + Application.dataPath + "/Scripts/" + selectobj.name + ".cs 成功!");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
```

**3.2 模板生成**  

结合反射，我们可以使用模板生成技术，根据预制体的信息生成代码模板，然后填充相应的属性和方法。

```
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
```

**4、脚本解析**

**4.1 AutoBuildTemplate 模板类**

AutoBuildTemplate 为 UI 基本样例类，很多项目开发都会有自己定义的 UI 基类，我们可以更改 AutoBuildTemplate 类来迎合我们自己的项目。通过替换其中的 #类名 #、# 查找 #、# 成员 #等关键词来加载对应参数展示，最终保存成 cs 文件。

**4.2 UI 样式列表映射**  

我们可以通过当前列表来映射 UGUI 的 UI 控件。这样在添加对应控件的时候，脚本就能够解析到，映射对应的控件：

```
var dicUIType = new Dictionary<string, string>();
dicUIType.Add("Img", "Image");
dicUIType.Add("Btn", "Button");
dicUIType.Add("Txt", "Text");
dicUIType.Add("Tran", "Transform");
dicUIType.Add("Input", "InputField");
dicUIType.Add("Raw", "RawImage");
dicUIType.Add("Drop", "Dropdown");
dicUIType.Add("Slider", "Slider");
dicUIType.Add("Scr", "Scrollbar");
```

**5、编辑生成**  

通过自定义的下划线 (_) 命名规则，来查找和解析预制体。

![](https://mmbiz.qpic.cn/mmbiz_png/1zA0SMrjkUEZXtJiaHIM8gxSqxlU0LWcfBUIia8FXGtt1zDZSgNHVZjeeNFiaAVVXXSfe9VWBiapRfDVyo4qWa6d6w/640?wx_fmt=png&from=appmsg)

生成的代码样例：

```
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class Page_Panel : MonoBehaviour
{
   private Text Txt_Text = null;
  private Image Img_Image = null;
  private Button Btn_Button = null;
  private Text Txt_ls = null;
  private RawImage Raw_Image = null;
  private InputField Input_Field = null;
  private Slider Slider_bg = null;
  private Scrollbar Scr_ollbar = null;
  
//auto
   private void Awake()
  {
    Txt_Text = transform.Find("Txt_Text").GetComponent<Text>();
    Img_Image = transform.Find("Img_Image").GetComponent<Image>();
    Btn_Button = transform.Find("Btn_Button").GetComponent<Button>();
    Txt_ls = transform.Find("Btn_Button/Txt_ls").GetComponent<Text>();
    Raw_Image = transform.Find("Raw_Image").GetComponent<RawImage>();
    Input_Field = transform.Find("Input_Field").GetComponent<InputField>();
    Slider_bg = transform.Find("Slider_bg").GetComponent<Slider>();
    Scr_ollbar = transform.Find("Scr_ollbar").GetComponent<Scrollbar>();
    
  }

   private void OnDestroy()
    {

    }
}
```

**5、总结**

以上 UnityUI 预制体自动生成脚本，您可能需要根据项目需求和预制体的复杂性进行更复杂的代码生成。可以考虑生成属性的序列化、添加事件处理逻辑等。

总的来说，通过使用反射和模板生成技术，可以在一定程度上减轻手动编写与预制体相关的代码的负担，提高开发效率。但在实际应用中，请注意代码的质量和一致性，以确保生成的代码符合项目的规范和要求。