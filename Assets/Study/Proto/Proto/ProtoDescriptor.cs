/// <summary>
/// ProtoDescriptor.cs
/// Created by wangxiangwei 2017-7-23
/// 符号描述
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 单独准备一个proto的命名空间，为以后独立拆分做准备.
/// </summary>
namespace Google.Protobuf
{
    /// <summary>
    /// 标签类型.
    /// </summary>
    public enum LabelType
    {
        LT_Single       = 0,    // 简单字段
        LT_Optional     = 1,    // 可选字段
        LT_Required     = 2,    // 必备字段
        LT_Repeated     = 3,    // 重复字段（数组）
        LT_Map          = 4,    // map字段，map 是 proto_v3 的一个 Repeated 扩展补丁，N1 把它当做标签处理
    }

    /// <summary>
    /// 字段配置类型.
    /// </summary>
    public enum FieldType
    {
        FT_Unknown      = 0,    // 未知类型，占位待定
        FT_Double       = 1,
        FT_Float        = 2,
        FT_Int64        = 3,
        FT_Uint64       = 4,
        FT_Int32        = 5,
        FT_FixeD64      = 6,
        FT_Fixed32      = 7,
        FT_Bool         = 8,
        FT_String       = 9,
        FT_Group        = 10,
        FT_Message      = 11,   // 引用类型，别的 Message 作为字段类型
        FT_Bytes        = 12,
        FT_Uint32       = 13,
        FT_Enum         = 14,
        FT_Sfixed32     = 15,
        FT_Sfixed64     = 16,
        FT_Sint32       = 17,
        FT_Sint64       = 18,
    }

    /// <summary>
    /// 格式类型解析器.
    /// </summary>
    public static class ProtoDescriptor
    {
        #region const

        /// <summary>
        /// 内部map字段类型，命名规则.
        /// </summary>
        /// <returns>The map message name.</returns>
        /// <param name="keyType">Key type.</param>
        /// <param name="valueType">Value type.</param>
        public static string GetMapMessageName(string keyType, string valueType)
        {
            return string.Format("map_{0}_{1}", keyType, valueType);
        }

        #endregion
        
        #region Wire
        
        // FieldType -> WireType
        public static Dictionary<FieldType, WireFormat.WireType> FieldWireMap = new Dictionary<FieldType, WireFormat.WireType>() {
            {FieldType.FT_Double     , WireFormat.WireType.Fixed64},
            {FieldType.FT_Float      , WireFormat.WireType.Fixed32},
            {FieldType.FT_Int64      , WireFormat.WireType.Varint},
            {FieldType.FT_Uint64     , WireFormat.WireType.Varint},
            {FieldType.FT_Int32      , WireFormat.WireType.Varint},
            {FieldType.FT_FixeD64    , WireFormat.WireType.Fixed64},
            {FieldType.FT_Fixed32    , WireFormat.WireType.Fixed32},
            {FieldType.FT_Bool       , WireFormat.WireType.Varint},
            {FieldType.FT_String     , WireFormat.WireType.LengthDelimited},
            {FieldType.FT_Group      , WireFormat.WireType.StartGroup}, // TODO and end?
            {FieldType.FT_Message    , WireFormat.WireType.LengthDelimited},
            {FieldType.FT_Bytes      , WireFormat.WireType.LengthDelimited},
            {FieldType.FT_Uint32     , WireFormat.WireType.Varint},
            {FieldType.FT_Enum       , WireFormat.WireType.Varint},
            {FieldType.FT_Sfixed32   , WireFormat.WireType.Fixed32},
            {FieldType.FT_Sfixed64   , WireFormat.WireType.Fixed64},
            {FieldType.FT_Sint32     , WireFormat.WireType.Varint},
            {FieldType.FT_Sint64     , WireFormat.WireType.Varint},
        };

        /// <summary>
        /// 判断 FieldType 和 WireType 是否有关联.
        /// </summary>
        /// <returns><c>true</c>, if field type and wire type was linked, <c>false</c> otherwise.</returns>
        /// <param name="ft">Ft.</param>
        /// <param name="wt">Wt.</param>
        public static bool IsLengthDelimited(FieldType ft)
        {
            return (FieldWireMap[ft] == WireFormat.WireType.LengthDelimited);
        }

        /// <summary>
        /// 查询 FieldType 对应的 WireType.
        /// </summary>
        /// <returns>The wire type.</returns>
        /// <param name="ft">Ft.</param>
        public static WireFormat.WireType GetWireType(FieldType ft)
        {
            TFW.D.Assert(FieldWireMap.ContainsKey(ft));
            return FieldWireMap[ft];
        }

        #endregion

        #region FieldType

        // 根据字符串解析字段类型
        public static Dictionary<string, FieldType> FieldTypeNameMap = new Dictionary<string, FieldType>(){
            {"double"  , FieldType.FT_Double  },
            {"float"   , FieldType.FT_Float   },
            {"int64"   , FieldType.FT_Int64   },
            {"uint64"  , FieldType.FT_Uint64  },
            {"int32"   , FieldType.FT_Int32   },
            {"fixed64" , FieldType.FT_FixeD64 },
            {"fixed32" , FieldType.FT_Fixed32 },
            {"bool"    , FieldType.FT_Bool    },
            {"string"  , FieldType.FT_String  },
            {"group"   , FieldType.FT_Group   },
            {"bytes"   , FieldType.FT_Bytes   },
            {"uint32"  , FieldType.FT_Uint32  },
            {"sfixed32", FieldType.FT_Sfixed32},
            {"sfixed64", FieldType.FT_Sfixed64},
            {"sint32"  , FieldType.FT_Sint32  },
            {"sint64"  , FieldType.FT_Sint64  },
        };

        /// <summary>
        /// Tries the type of the parse field.
        /// </summary>
        /// <returns><c>true</c>, if parse field type was tryed, <c>false</c> otherwise.</returns>
        /// <param name="str">String.</param>
        /// <param name="field">Field.</param>
        public static bool TryParseFieldType(string str, out FieldType field)
        {
            return FieldTypeNameMap.TryGetValue(str, out field);
        }

        #endregion
    }
}