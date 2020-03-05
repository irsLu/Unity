/// <summary>
/// ProtoParser.cs
/// Created by wangxiangwei 2017-7-25
/// .proto文件解析器
///
/// 1. /**/注释只做了简单的首字符处理
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
        #region 成员

        // 报错标题
        private const string mWarning = "[.proto解析]";

        // 记录当前解析信息
        private static string mSyntax = string.Empty;
        public static string mPackage = string.Empty;
        private static HashSet<string> mImportList = new HashSet<string>{ };
        private static Dictionary<string, ProtoStructBase> mRefPSBMap = null; // 所有已解析信息的缓存，单独字段存放，和ProtoMgr解耦

        // 缓存的字段实例，尚未 build Tag
        private static Dictionary<string, List<ProtoField>> mNewFieldBuildMap = new Dictionary<string, List<ProtoField>>();

        #endregion

        #region 内部函数

        /// <summary>
        /// 解析顶层 Top level.
        /// </summary>
        /// <param name="sr">Sr.</param>
        private static bool Parse(StreamReader sr)
        {
            // 循环读取词组，解析
            string word;
            while (! sr.EndOfStream)
            {
                // 注释词，跳过本行
                if (IsComments((char)sr.Peek()))
                {
                    sr.ReadLine();
                    continue;
                }

                // 读取一行
                word = ReadUntil(sr, IsWordEnd);
                if (string.IsNullOrEmpty(word))
                {
                    // 空白，按理说只能是
                    // 结束，注释，文末
                    char nextChar = (char)sr.Peek();
                    Debug.Assert(IsComments(nextChar) || IsStreamEnd(nextChar),
                        string.Format(mWarning + "Top level 未知格式， next = {0}", nextChar));
                    continue;
                }

                // 直接根据关键词进行操作
                switch (word)
                {
                    // 服务器用，客户端跳过该行
                    case "option":
                        sr.ReadLine();
                        continue;

                    case "syntax":
                        if (! ParseSyntax(sr))
                            goto Jump_BreakParse;
                        break;

                    case "package":
                        if (! ParsePackage(sr))
                            goto Jump_BreakParse;
                        break;

                    case "import":
                        if (! ParseImport(sr))
                            goto Jump_BreakParse;
                        break;

                    case "service":
                        if (! ParseService(sr))
                            goto Jump_BreakParse;
                        break;

                    case "message":
                        if (! ParseMessage(sr))
                            goto Jump_BreakParse;
                        break;

                    case "enum":
                        if (! ParseEnum(sr, null))
                            goto Jump_BreakParse;
                        break;

                    default:
                        TFW.D.Warning(mWarning + "语法错误，无法识别的 TopLevel 关键字, w = {0}, next = {1}", word, (char)sr.Peek());
                        goto Jump_BreakParse;
                }
            }

            // 结束解析
            return true;

            // 报错，中断
            Jump_BreakParse:
            TFW.D.Warning(mWarning + "语法错误，w = {0}", word);
            return false;
        }

        /// <summary>
        /// message
        /// </summary>
        /// <returns><c>true</c>, if status action message was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseMessage(StreamReader sr)
        {
            // 消息名
            string messageName = ReadUntil(sr, IsWordEnd);
            if (string.IsNullOrEmpty(messageName))
                return false;

            // 入坑识别
            if (! ConsumeEndOf(sr, '{'))
            {
                TFW.D.Warning(mWarning + "语法错误, 丢失 ‘{’，message = {0}", messageName);
                return false;
            }

            // 新的message结构
            ProtoMessage newMsg = new ProtoMessage();
            newMsg.Name = messageName;

            // 解析statement
            while ((char)sr.Peek() != '}')
            {
                // 非正常结束
                if (sr.EndOfStream)
                    goto Jump_BreakParse;

                // 后续是注释词，跳过本行
                if (IsComments((char)sr.Peek()))
                {
                    sr.ReadLine();
                    continue;
                }

                // 取词
                string word = ReadUntil(sr, IsWordEnd);
                if (string.IsNullOrEmpty(word))
                {
                    // 空白，按理说只能是
                    // 结束，注释，文末
                    char nextChar = (char)sr.Peek();
//                    TFW.D.Assert(nextChar == '}' || IsStreamEnd(nextChar) || IsComments(nextChar),
//                        mWarning + "ParseMessage未知格式， next = {0}", nextChar);
                    continue;
                }

                // message内嵌解析
                // 直接根据关键词进行操作
                switch (word)
                {
                    case "enum":
                        // 内嵌枚举
                        if (! ParseEnum(sr, newMsg))
                            goto Jump_BreakParse;
                        break;

                    case "map":
                        // 内嵌map结构
                        if (!ParseMap(sr, newMsg))
                            goto Jump_BreakParse;
                        break;

                    case "message":
                    case "extensions":
                    case "reserved":
                    case "extend":
                    case "option":
                    case "oneof":
                        TFW.D.Warning(mWarning + "语法错误，尚未支持的message内嵌关键字 w = {0}", word);
                        goto Jump_BreakParse;

                    default:
                        // 字段解析
                        if (!ParseMessageField(word, sr, ref newMsg))
                            goto Jump_BreakParse;
                        break;
                }
            }

            // 结束解析
            ConsumeEndOf(sr, '}');
           // TFW.D.Assert(!mRefPSBMap.ContainsKey(newMsg.Name), mWarning + "消息重复定义, msg = {0}", newMsg.Name);
            mRefPSBMap.Add(newMsg.Name, newMsg);
            return true;

            // 报错，中断
            Jump_BreakParse:
            TFW.D.Warning(mWarning + "语法错误，msg = {0}", newMsg.Name);
            return false;
        }

        /// <summary>
        /// message 内部字段解析.
        /// </summary>
        /// <returns><c>true</c>, if message field was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseMessageField(string startWord, StreamReader sr, ref ProtoMessage pMsg)
        {
            // 新建字段
            ProtoField newField = new ProtoField();
            newField.LabelType = LabelType.LT_Single;
            newField.BindMsg = pMsg;

            // 首单词
            string word = startWord; // 为了减少字符串读取操作，此处耦合使用外部传入单词
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;

            // 1. 先看是不是Label
            if (TryParseFieldLabel(word, ref newField))
            {
                word = ReadUntil(sr, IsWordEnd);
                if (string.IsNullOrEmpty(word) || IsComments(word))
                    return false;
            }

            // 2. 解析字段类型
            string fieldTypeName = word;
            newField.FieldType = ParseMessageFieldType(fieldTypeName);

            // 3. 解析字段名
            word = ReadUntil(sr, IsWordEnd);
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;
            else
                newField.Name = word;

            // 4. 解析字段ID
            word = ReadUntil(sr, IsChunkEnd);
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;
            else
                newField.ID = int.Parse(GetEqualValue(word));

            // 结束本行
            sr.ReadLine();

            // 字段 Tag 在后续 BuildTag() 操作中处理，此处缓存即可
            // 确保前面的操作都通过，才能加入
            TFW.Common.DictionaryListAdd(mNewFieldBuildMap, fieldTypeName, newField);

            return true;
        }

        /// <summary>
        /// 拆分长函数
        /// ParseMessageField : 字段类型, 玩家自定义类型解析.
        /// </summary>
        /// <param name="fieldTypeName">Field type name.</param>
        /// <param name="newField">New field.</param>
        /// <param name="pMsg">P message.</param>
        private static FieldType ParseMessageFieldType(string fieldTypeName)
        {
            FieldType fieldType;
            if (ProtoDescriptor.TryParseFieldType(fieldTypeName, out fieldType))
                // 普通字段类型
                return fieldType;
            else
                // 自定义字段类型，返回 Unknown 即可，后续 BuildTag() 操作中会进行实例映射
                return FieldType.FT_Unknown;
        }

        /// <summary>
        /// enum 解析.
        /// </summary>
        /// <returns><c>true</c>, if enum was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseEnum(StreamReader sr, ProtoMessage pMsg)
        {
            // 定义名
            string enumName = ReadUntil(sr, IsWordEnd);
            if (string.IsNullOrEmpty(enumName))
                return false;

            // 入坑识别
            if (! ConsumeEndOf(sr, '{'))
            {
                TFW.D.Warning(mWarning + "语法错误, 丢失 ‘{’，enum = {0}", enumName);
                return false;
            }

            // 新的 enum 结构
            ProtoEnum newEnum = new ProtoEnum();

            // 命名空间根据父节点定义
            if (pMsg == null)
            {
                // Toplevel定义
                newEnum.Name = enumName;
            }
            else
            {
                // message内嵌定义
                newEnum.Name = string.Format("{0}_{1}", pMsg.Name, enumName); // 随 proto 官方方法，用下划线连接
                newEnum.NamespaceList.AddRange(pMsg.NamespaceList);
            }

            // 解析statement
            while ((char)sr.Peek() != '}')
            {
                // 非正常结束
                if (sr.EndOfStream)
                    goto Jump_BreakParse;

                // 后续是注释词，跳过本行
                if (IsComments((char)sr.Peek()))
                {
                    sr.ReadLine();
                    continue;
                }

                // 取词
                string word = ReadUntil(sr, IsWordEnd);
                if (string.IsNullOrEmpty(word))
                {
                    // 空白，按理说只能是
                    // 结束，注释，文末
                    char nextChar = (char)sr.Peek();
//                    TFW.D.Assert(nextChar == '}' || IsStreamEnd(nextChar) || IsComments(nextChar),
//                        mWarning + "ParseEnum未知格式， next = {0}", nextChar);
                    continue;
                }

                // 全是定义，直接记录即可
                string name = word;
                word = ReadUntil(sr, IsChunkEnd);
                if (string.IsNullOrEmpty(word))
                    return false;

                uint enumID = uint.Parse(GetEqualValue(word));
                newEnum.EnumElementMap.Add(enumID, name);

                // 额外记录给父节点
                if (pMsg != null)
                    pMsg.InnerEnum.Add(newEnum.Name, newEnum);
            }

            // 结束解析
            ConsumeEndOf(sr, '}');
            //TFW.D.Assert(! mRefPSBMap.ContainsKey(newEnum.Name), mWarning + "enum重复定义, enum = {0}", newEnum.Name);
            mRefPSBMap.Add(newEnum.Name, newEnum);
            return true;

            // 报错，中断
            Jump_BreakParse:
            TFW.D.Warning(mWarning + "语法错误，enum = {0}", newEnum.Name);
            return false;
        }

        /// <summary>
        /// map结构的字段解析.
        /// </summary>
        /// <returns><c>true</c>, if map was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        /// <param name="pMsg">P message.</param>
        private static bool ParseMap(StreamReader sr, ProtoMessage pMsg)
        {
            // 目前把map当做一个内嵌 message 来处理
            // 1. 解析map，定义成一个内部 message 结构
            // 2. 字段就按照“repeated + 自定义message结构”来处理即可

            // 1. 读取map结构
            string word = ReadBrecketChunk(sr, '<', '>');
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;

            // 拆分 key, value
            string[] kv = word.Split(',');
            if (kv.Length < 2)
                return false;
            string keyStr = kv[0].Trim();
            string valueStr = kv[1].Trim();

            // 2. 为这个(key, value)组合，添加一个内部message数据结构
            // 新的message结构
            string msgName = ProtoDescriptor.GetMapMessageName(keyStr, valueStr);
            if (! mRefPSBMap.ContainsKey(msgName))
            {
                // 如果是同名字段，就直接复用就行了
                // 如果是新的类型组合，则需要创建一个
                ProtoMessage newMsg = new ProtoMessage();
                newMsg.Name = msgName;
                mRefPSBMap.Add(newMsg.Name, newMsg);

                // 解析key字段类型
                ProtoField keyField = new ProtoField();
                keyField.FieldType = ParseMessageFieldType(keyStr);
                keyField.Name = "key";
                keyField.ID = 1;
                keyField.BindMsg = newMsg;
                TFW.Common.DictionaryListAdd(mNewFieldBuildMap, keyStr, keyField);

                // 解析value字段类型
                ProtoField valueField = new ProtoField();
                valueField.FieldType = ParseMessageFieldType(valueStr);
                valueField.Name = "value";
                valueField.ID = 2;
                valueField.BindMsg = newMsg;
                TFW.Common.DictionaryListAdd(mNewFieldBuildMap, valueStr, valueField);
            }

            // 3. 把这个map字段，当做 repeated message field 类型来处理即可
            ProtoField kvField = new ProtoField();
            kvField.LabelType = LabelType.LT_Map; // 标签还是给个指定的
            kvField.FieldType = FieldType.FT_Unknown; // 设定为未知，后续 build tag 的时候会处理
            kvField.BindMsg = pMsg;

            // 解析字段名
            word = ReadUntil(sr, IsWordEnd);
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;
            else
                kvField.Name = word;

            // 解析字段ID
            word = ReadUntil(sr, IsChunkEnd);
            if (string.IsNullOrEmpty(word) || IsComments(word))
                return false;
            else
                kvField.ID = int.Parse(GetEqualValue(word));

            // 结束本行
            sr.ReadLine();

            // 记录 Tag build 回调
            TFW.Common.DictionaryListAdd(mNewFieldBuildMap, msgName, kvField);
            return true;
        }

        /// <summary>
        /// 字段标签解析.
        /// </summary>
        /// <returns><c>true</c>, if field label was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        /// <param name="field">Field.</param>
        private static bool TryParseFieldLabel(string word, ref ProtoField field)
        {
            switch (word)
            {
                case "optional":
                    field.LabelType = LabelType.LT_Optional;
                    return true;

                case "required":
                    field.LabelType = LabelType.LT_Required;
                    return true;

                case "repeated":
                    field.LabelType = LabelType.LT_Repeated;
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 解析用户自定义字段类型.
        /// </summary>
        /// <returns>The user defined type.</returns>
        /// <param name="typeWord">Type word.</param>
        private static ProtoStructBase ParseUserDefinedType(string typeWord, ProtoMessage pMsg)
        {
            // 目前无视掉命名空间前缀
            int ci = typeWord.LastIndexOf('.');
            ci++;
            if (ci > 0 && typeWord.Length > ci)
                typeWord = typeWord.Substring(ci);

            // 尝试从已解析列表中，查找指定类型
            ProtoStructBase userType;
            return (mRefPSBMap.TryGetValue(string.Format("{0}_{1}", pMsg.Name, typeWord), out userType)) ? userType : // 先找内嵌
                (mRefPSBMap.TryGetValue(typeWord, out userType)) ? userType : // 再找公共
                null; // 尚未解析该定义
        }

        /// <summary>
        /// syntax
        /// </summary>
        /// <returns><c>true</c>, if status action syntax was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseSyntax(StreamReader sr)
        {
            string chunk = ReadUntil(sr, IsChunkEnd);
            if (string.IsNullOrEmpty(chunk))
                mSyntax = string.Empty;
            else
                mSyntax = GetEqualValue(chunk).Trim('\"');

            Debug.Assert(mSyntax == "proto3", string.Format(mWarning + "目前只支持 proto3 格式, p = {0}", mSyntax));
            return true;
        }

        /// <summary>
        /// package
        /// </summary>
        /// <returns><c>true</c>, if status action package was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParsePackage(StreamReader sr)
        {
            string chunk = ReadUntil(sr, IsChunkEnd);
            if (string.IsNullOrEmpty(chunk))
                mPackage = string.Empty;
            else
                mPackage = chunk.Trim();

            return true;
        }

        /// <summary>
        /// import
        /// </summary>
        /// <returns><c>true</c>, if status action import was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseImport(StreamReader sr)
        {
            string chunk = ReadUntil(sr, IsChunkEnd);
            if (string.IsNullOrEmpty(chunk))
                return true;
            //remove import by LiYu
            if (!string.IsNullOrEmpty(chunk)) return true;
            // 解析依赖文件，直接获取纯文件名即可
            string file = Path.GetFileName(chunk.Trim('\"'));
            // 递归加载
            if (ParseFromFile(file))
                return true;

            TFW.D.Warning(mWarning + "递归加载proto文件失败, import = {0}", file);
            return false;
        }

        /// <summary>
        /// service.
        /// </summary>
        /// <returns><c>true</c>, if service was parsed, <c>false</c> otherwise.</returns>
        /// <param name="sr">Sr.</param>
        private static bool ParseService(StreamReader sr)
        {
            Debug.LogError(mWarning + " service 类型字段尚未支持.");
            return false;
        }

        #endregion

        /// <summary>
        /// 解析一个 proto 文件
        /// </summary>
        /// <returns><c>true</c>, if from file was parsed, <c>false</c> otherwise.</returns>
        /// <param name="resInfo">Res info.</param>
        /// <param name="refMap">外部传入已解析的数据池，硬解耦.</param>
        public static bool ParseFromFile(string proto)
        {
            try
            {
                if (!mImportList.Add(proto))
                    return true;
                //bugfix
                //Stream streamRes = TFW.Common.String2Stream(TFW.ResourceMgr.LoadText(ProtoFolder + proto));
                Stream streamRes = null;

                //Stream streamRes = ResourceMgr.LoadTextStream(resInfo);
                return ParseFromFile(streamRes);
            }
            catch (Exception e)
            {
                TFW.D.Warning(mWarning + "解析异常, file = {0}\nexception = {1}", (ProtoFolder + proto), e.Message);
                return false;
            }
        }

        public static string ProtoFolder {
            get;
            set;
        }
        /// <summary>
        /// 解析一个 proto 文件
        /// </summary>
        /// <returns><c>true</c>, if from file was parsed, <c>false</c> otherwise.</returns>
        /// <param name="filename">Filename.</param>
        /// <param name="streamRes">Stream res.</param>
        /// <param name="refMap">Reference map.</param>
        static bool ParseFromFile(Stream streamRes)
        {
            try
            {
                // 读取解析文件
                using (StreamReader sr = new StreamReader(streamRes))
                {
                    mSyntax = string.Empty;
                    mPackage = string.Empty;
                    mRefPSBMap = ProtoMgr.MessageMap;
                    bool ret = Parse(sr);
                    sr.Close();
                    return ret;
                }
            }
            catch (Exception e)
            {
                TFW.D.Warning(mWarning + "解析异常, nexception = {0}", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Builds the tag.
        /// 消息体的tag编码，需要所有消息定义完成之后进行，故而单独提取为一个流程
        /// </summary>
        /// <returns><c>true</c>, if tag was built, <c>false</c> otherwise.</returns>
        public static bool BuildTag()
        {
            mRefPSBMap = ProtoMgr.MessageMap;

            // 遍历所有类型
            var keyIt = mNewFieldBuildMap.GetEnumerator();
            while (keyIt.MoveNext())
            {
                // 类型标签
                string fieldTypeName = keyIt.Current.Key;

                // 遍历本类型所有字段
                var listIt = keyIt.Current.Value.GetEnumerator();
                while (listIt.MoveNext())
                {
                    ProtoField pf = listIt.Current;

                    // 自定义类型，需要解析具体子类
                    if (pf.FieldType == FieldType.FT_Unknown)
                    {
                        TFW.D.Assert(pf.BindMsg != null);
                        pf.UserDefined = ParseUserDefinedType(fieldTypeName, pf.BindMsg);
                        Debug.Assert(pf.UserDefined != null, string.Format("字段 FieldType 类型没有定义, type = {0}, msg = {1}", fieldTypeName, pf.BindMsg.Name));

                        // 分析一下是具体那种
                        pf.FieldType = (pf.UserDefined.GetType() == typeof(ProtoMessage)) ? FieldType.FT_Message : FieldType.FT_Enum;
                    }

                    // 记录 Tag
                    uint fieldTag = WireFormat.MakeTag(pf.ID, ProtoDescriptor.GetWireType(pf.FieldType), pf.LabelType);
                    pf.Tag = fieldTag;
                    Debug.Assert(pf.BindMsg != null, String.Format(mWarning + " build tag failed, field没有记录 bindMsg, name = {0}", pf.Name));
                    Debug.Assert(!pf.BindMsg.FieldMap.ContainsKey(fieldTag),
                        String.Format(mWarning + "消息{0}字段id重复，id = {1}, tag = {2}", pf.BindMsg.Name, pf.ID, fieldTag));
                    pf.BindMsg.FieldMap.Add(fieldTag, pf);
                }
            }

            mNewFieldBuildMap.Clear();
            return true;
        }

    } // end class
} // end namespace
