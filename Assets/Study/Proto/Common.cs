using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TFW
{
    public class Common
    {
        /// <summary>
        /// 遍历迭代列表，拼接 ToString().
        /// 用于调试输出
        /// </summary>
        /// <param name="obList">Ob list.</param>
        /// <param name="indent">Indent.</param>
        public static string LogCollectionString(ICollection obList, int indentLevel = 0)
        {
            string indent = GetIndentByLeven(indentLevel);
            string str = string.Empty;
            foreach (var ob in obList)
                str += string.Format("{0}{1},\n", indent, ob.ToString());
            return str;
        }

        /// <summary>
        /// 根据缩进层次获取缩进字符.
        /// </summary>
        /// <returns>The indent by leven.</returns>
        /// <param name="indentLevel">Indent level.</param>
        public static string GetIndentByLeven(int indentLevel = 0, string indentBase = "    ")
        {
            StringBuilder sb = new StringBuilder();
            sb.Insert(0, indentBase, indentLevel);
            return sb.ToString();
        }

        public static string LogDictionaryString(IDictionary map, int indentLevel = 0,
            Func<object, string> valueStringFun = null)
        {
            string indent = GetIndentByLeven(indentLevel);
            string str = string.Empty;

            IDictionaryEnumerator it = map.GetEnumerator();
            while (it.MoveNext())
                str += string.Format("{0}{1} = {2},\n", indent,
                    it.Key.ToString(),
                    (valueStringFun == null) ? it.Value.ToString() : valueStringFun(it.Value));
            return str;
        }

        /// <summary>
        /// 实现C语言sscanf功能
        /// </summary>
        /// <param name="inputStr">待匹配字符串</param>
        /// <param name="pattern">匹配格式字符串</param>
        public static List<string> Scanf(string inputStr, string pattern)
        {
            List<string> ret = new List<string>();

            // 正则表达式匹配
            Match mat = Regex.Match(inputStr, pattern);

            // 匹配失败
            if (!mat.Success)
                return ret;

            // 匹配成功逐个填充数据
            for (int index = 0; index < mat.Groups.Count - 1; index++)
                ret.Add(mat.Groups[index + 1].Value);

            return ret;
        }
        
        /// <summary>
        /// 将字符串转换为流
        /// </summary>
        /// <param name="content">字符串</param>
        /// <returns>流</returns>
        public static Stream String2Stream(string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }
        
        /// <summary>
        /// 向 map<T, List<U>> 中添加一个元素.
        /// 如果 T key 为空，则创建一个List
        /// </summary>
        /// <returns><c>true</c>, 添加了新 list, <c>false</c> 已有 list.</returns>
        /// <param name="dict">Dict.</param>
        /// <param name="key">Key.</param>
        /// <param name="value"><c>Value</c>.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        /// <typeparam name="U">The 2nd type parameter.</typeparam>
        public static bool DictionaryListAdd<T, U>(Dictionary<T, List<U>> dict, T key, U value)
        {
            bool ret = false;
            List<U> listValue;
            if (!dict.TryGetValue(key, out listValue))
            {
                listValue = new List<U>();
                dict.Add(key, listValue);
                ret = true;
            }

            listValue.Add(value);
            return ret;
        }


    }
}