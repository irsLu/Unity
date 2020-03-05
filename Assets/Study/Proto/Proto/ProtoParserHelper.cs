/// <summary>
/// ProtoParserHelper.cs
/// Created by wangxiangwei 2017-20-13
/// .proto文件解析器
/// 内容太多，稍微拆分一下文件
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Google.Protobuf
{
    public static partial class ProtoParser
    {
        #region 成员变量

        // 语句结束符
        private static HashSet<char> mChunkEndSet = new HashSet<char>{
            ';',
            '\n',
            '\r',
            '\uffff',
            '['
        };

        // 注释字符
        // TODO:粗暴处理
        private static HashSet<char> mCommentSet = new HashSet<char>{
            '/',
            '*',
        };

        #endregion

        /// <summary>
        /// 是否是前导空白符.
        /// </summary>
        /// <returns><c>true</c> if is blank the specified c; otherwise, <c>false</c>.</returns>
        /// <param name="c">C.</param>
        private static bool IsTrimStart(char c)
        {
            return char.IsWhiteSpace(c) || (c==';');
        }

        /// <summary>
        /// 是否空白符.
        /// </summary>
        /// <returns><c>true</c> if is blank the specified c; otherwise, <c>false</c>.</returns>
        /// <param name="c">C.</param>
        private static bool IsBlank(char c)
        {
            return char.IsWhiteSpace(c);
        }

        /// <summary>
        /// 是否文件结束.
        /// </summary>
        /// <returns><c>true</c> if is stream end the specified c; otherwise, <c>false</c>.</returns>
        /// <param name="c">C.</param>
        private static bool IsStreamEnd(char c)
        {
            return c == '\uffff';
        }

        /// <summary>
        /// 是否词素结束.
        /// </summary>
        /// <returns><c>true</c> if is word end the specified c; otherwise, <c>false</c>.</returns>
        /// <param name="c">C.</param>
        private static bool IsWordEnd(char c)
        {
            // A~Z, a~z, 0~9, _, .
            return ! (char.IsLetterOrDigit(c) || (c == '_') || (c == '.'));
        }

        /// <summary>
        /// 是否语句结束.
        /// </summary>
        /// <returns><c>true</c> if is chunk end the specified c; otherwise, <c>false</c>.</returns>
        /// <param name="c">C.</param>
        private static bool IsChunkEnd(char c)
        {
            return mChunkEndSet.Contains(c);
        }

        /// <summary>
        /// 清除空白.
        /// </summary>
        /// <returns><c>true</c>, if start was trimed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static void TrimStartFromCurrent(StreamReader sr)
        {
            while (! sr.EndOfStream &&
                IsTrimStart((char)sr.Peek()))
            {
                sr.Read();
            }
        }

        /// <summary>
        /// 是否是注释词.
        /// </summary>
        /// <returns><c>true</c> if is comments the specified str; otherwise, <c>false</c>.</returns>
        /// <param name="str">String.</param>
        private static bool IsComments(string str)
        {
            return !string.IsNullOrEmpty(str) && mCommentSet.Contains(str[0]);
        }

        /// <summary>
        /// 是否是注释词.
        /// </summary>
        /// <returns><c>true</c> if is comments the specified str; otherwise, <c>false</c>.</returns>
        /// <param name="c">char.</param>
        private static bool IsComments(char c)
        {
            return mCommentSet.Contains(c);
        }

        /// <summary>
        /// 读取语句chunk中，=后面的值.
        /// </summary>
        /// <returns>The equal value.</returns>
        /// <param name="chunk">Chunk.</param>
        private static string GetEqualValue(string chunk, string s = "=")
        {
            int index = chunk.IndexOf(s);
            return (index >= 0) ? chunk.Substring(index + 1).Trim() : string.Empty;
        }

        /// <summary>
        /// 读字符，直到读取到指定字符.
        /// </summary>
        /// <returns>是否成功消耗指定字符.</returns>
        /// <param name="sr">Sr.</param>
        /// <param name="equalChar">Equal char.</param>
        private static bool ConsumeEndOf(StreamReader sr, char chackChar)
        {
            // 一直读取，直到check
            while (true)
            {
                // 获取到目标才终止读取
                char c = (char)sr.Read();
                if (c == chackChar)
                    return true;
                else if(sr.EndOfStream)
                    return false;
            }
        }

        /// <summary>
        /// 读后续字符，直到获得指定char.
        /// 中途跳过Blank，但会停在其他字符位置
        /// </summary>
        /// <returns>The until.</returns>
        /// <param name="sr">Sr.</param>
        /// <param name="checkFun">Check fun.</param>
        private static string ReadUntil(StreamReader sr, Func<char, bool> checkFun, bool isTrimStart = true)
        {
            StringBuilder sb = new StringBuilder();
            if (isTrimStart)
                TrimStartFromCurrent(sr);

            // 一直读取，直到check
            while (true)
            {
                // 数据读完，条件满足，则中断
                char c = (char)sr.Peek();
                if (IsStreamEnd(c) ||
                    checkFun(c))
                    break;

                // 移动指针，记录数据
                sr.Read();
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 读取一个括号块.
        /// </summary>
        /// <returns>The brecket chunk.</returns>
        /// <param name="sr">Sr.</param>
        /// <param name="start">左括号.</param>
        /// <param name="end">右括号.</param>
        private static string ReadBrecketChunk(StreamReader sr, char start, char end)
        {
            StringBuilder sb = new StringBuilder();
            TrimStartFromCurrent(sr);

            if (sr.Peek() != start)
            {
                TFW.D.Warning(mWarning + "括号读取异常，'{0}' 之前有异常字符", start);
                return string.Empty;
            }

            if (!ConsumeEndOf(sr, start))
            {
                TFW.D.Warning(mWarning + "括号读取异常，没有读到 '{0}'", start);
                return string.Empty;
            }

            // 一直读取，直到 匹配括号读完end
            int pairFlag = 1;
            while (true)
            {
                // 数据读完，条件满足，则中断
                char c = (char)sr.Peek();
                if (IsStreamEnd(c))
                {
                    TFW.D.Warning(mWarning + "括号读取异常，左右括号数量不匹配，BrecketPair = {0}", pairFlag);
                    break;
                }

                // 移动指针
                sr.Read();

                // 统计括号层数
                if (c == start)
                    pairFlag++;
                else if (c == end)
                    pairFlag--;
                else
                    sb.Append(c); // 记录读取的字符，排除括号

                if (pairFlag == 0)
                    break;
            }

            return sb.ToString();
        }
    }
}