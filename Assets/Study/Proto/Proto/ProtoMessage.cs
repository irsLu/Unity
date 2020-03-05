/// <summary>
/// ProtoMessage.cs
/// Created by wangxiangwei 2017-7-23
/// Proto 消息类
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 单独准备一个proto的命名空间，为以后独立拆分做准备.
/// </summary>
namespace Google.Protobuf
{
    /// <summary>
    /// 接口基类.
    /// </summary>
    public abstract class ProtoStructBase
    {
        // 名称
        public string Name;

        // 当前作用域命名空间
        public List<string> NamespaceList = new List<string>();
    }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public class ProtoMessage : ProtoStructBase
    {
        // 消息编号
        public uint ID = 999;

        // 字段索引, tag -> field
        public Dictionary<uint, ProtoField> FieldMap = new Dictionary<uint, ProtoField>();

        // 内部enum
        public Dictionary<string, ProtoEnum> InnerEnum = new Dictionary<string, ProtoEnum>();

        // 打印调试
        public override string ToString()
        {
            return string.Format("ProtoMessage {0} =\n[\n    ID = {1},\n    FieldMap = \n    [\n{2}    ]\n]", 
                Name, ID, TFW.Common.LogCollectionString(FieldMap.Values, 2));
        }
    }

    /// <summary>
    /// 枚举类型.
    /// 允许嵌套入message中
    /// </summary>
    public class ProtoEnum : ProtoStructBase
    {
        // 枚举元素列表
        public Dictionary<uint, string> EnumElementMap = new Dictionary<uint, string>();

        // 打印调试
        public override string ToString()
        {
            string indent = TFW.Common.GetIndentByLeven(0);
            return string.Format("{2}ProtoEnum {0} =\n{3}[\n{1}{4}]", 
                Name, TFW.Common.LogDictionaryString(EnumElementMap, 1), indent, indent, indent);
        }
    }

    /// <summary>
    /// 字段类.
    /// </summary>
    public class ProtoField
    {
        // 字段名
        public string Name = string.Empty;

        // 字段编号
        public int ID = 0;

        // 归属消息类
        public ProtoMessage BindMsg = null;

        // tag
        public uint Tag = 0;

        // 字段标签类型
        public LabelType LabelType = LabelType.LT_Single;

        // 数据类型
        public FieldType FieldType = FieldType.FT_Unknown;
        public ProtoStructBase UserDefined = null;  // 自定义类型

        // 打印调试
        public override string ToString()
        {
            return string.Format("[Tag={0}, Name={1}, ID={2}, LT={3}, FT={4}, UD={5}]", 
                Tag, Name, ID, LabelType.ToString(), FieldType.ToString(), (UserDefined == null) ? "null" : UserDefined.Name
            );
        }
    }
}