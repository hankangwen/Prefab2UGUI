using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AutoBuild
{
    [MenuItem("KGFramework/生成/生成或刷新界面")]
    public static void BuildUIScript()
    {
        //预知名前缀到UIType
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

        GameObject[] selectObjects = Selection.gameObjects;

        foreach (var go in selectObjects)
        {
            //选择的物体
            GameObject selectObj = go.transform.root.gameObject;
            //物体的子物体
            Transform[] transforms = selectObj.GetComponentsInChildren<Transform>(true);
            
            //UI需要查询的物体
            var uiNodeList = GetUINodeList(transforms, dicUIType.Keys);

            var nodePathList = new Dictionary<string, string>();
            //循环得到物体路径
            foreach (var node in uiNodeList)
            {
                Transform tempNode = node;
                string nodePath = (tempNode.parent != selectObj.transform) ? "/" + tempNode.name : tempNode.name;
                while (tempNode != tempNode.root && tempNode.parent != selectObj.transform)
                {
                    var parent = tempNode.parent;
                    tempNode = parent;
                    int index = nodePath.IndexOf('/');
                    string nodeName = parent != selectObj.transform ? "/" + tempNode.name : tempNode.name;
                    nodePath = nodePath.Insert(index, nodeName);
                }
                nodePathList.Add(node.name, nodePath);
            }

            //成员变量字符串
            string memberString = "";
            //查询代码字符串
            string loadedContant = "";

            foreach (Transform itemTran in uiNodeList)
            {
                string typeStr = dicUIType[itemTran.name.Split('_')[0]];

                memberString += $"private {typeStr} {itemTran.name} = null;\r\n\t";
                loadedContant += $"{itemTran.name} = transform.Find(\"{nodePathList[itemTran.name]}\")" +
                                 $".GetComponent<{typeStr}>();\r\n\t\t";
            }

            string scriptPath = $"{Application.dataPath}/Scripts/{selectObj.name}.cs";
            string classStr = "";
            
            //如果已经存在脚本，则只替换//auto下方的字符串
            if (File.Exists(scriptPath))
            {
                FileStream classFile = new FileStream(scriptPath, FileMode.Open);
                StreamReader reader = new StreamReader(classFile);
                classStr = reader.ReadToEnd();
                reader.Close();
                classFile.Close();
                // File.Delete(scriptPath);

                string splitStr = "//auto";
                string unChangeStr = Regex.Split(classStr, splitStr, RegexOptions.IgnoreCase)[0];
                string changeStr = Regex.Split(AutoBuildTemplate.UIClass, splitStr, RegexOptions.IgnoreCase)[1];

                StringBuilder builder = new StringBuilder();
                builder.Append(unChangeStr);
                builder.Append(splitStr);
                builder.Append(changeStr);
                classStr = builder.ToString();
            }
            else
            {
                classStr = AutoBuildTemplate.UIClass;
            }

            classStr = classStr.Replace("#类名#", selectObj.name);
            classStr = classStr.Replace("#查找#", loadedContant);
            classStr = classStr.Replace("#成员#", memberString);

            FileStream fileStream = new FileStream(scriptPath, FileMode.Create); //FileMode.CreateNew);
            StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
            streamWriter.Write(classStr);
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();
            Debug.Log($"创建脚本 {Application.dataPath}/Script/{selectObj.name}.cs 成功！");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    //UI需要查询的物体
    private static List<Transform> GetUINodeList(Transform[] childList, Dictionary<string, string>.KeyCollection keys)
    {
        List<Transform> uiNodeList = new List<Transform>();
        foreach (var tran in childList)
        {
            string tranName = tran.name;
            if (tranName.Contains('_') && keys.Contains(tranName.Split('_')[0]))
            {
                uiNodeList.Add(tran);
            }
        }

        return uiNodeList;
    }
}
