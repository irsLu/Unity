/// <summary>
/// ProtoSluaEncode.cs
/// Created by wangxiangwei 2017-8-9
/// Protobuf解析器：Slua
///
/// 1. 将 slua 的 table 数据，编码为 proto byte[]
///    没有对象继承
/// 2. 数据格式以 proto3 为标准
/// 3. 有一些简单的 LuaObject 接口没有使用，是为了避免object的装拆操作，
///    尽量直接只用 LuaDll.lus_xxx()接口
/// </summary>

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using LuaDLL = XLua.LuaDLL.Lua;
using XLua;

namespace Google.Protobuf
{
    public static partial class ProtoSlua
    {
        // 单字段编码表
        private static Dictionary<FieldType, Action<IntPtr, int, CodedOutputStream, ProtoField>> mSluaEncodeFuncMap =
            new Dictionary<FieldType, Action<IntPtr, int, CodedOutputStream, ProtoField>>() {
        { FieldType.FT_Double    , EncodeDouble },
        { FieldType.FT_Float     , EncodeFloat },
        { FieldType.FT_Int32     , EncodeInt32 },
        { FieldType.FT_Int64     , EncodeInt64 },
        { FieldType.FT_Uint32    , EncodeUint32 },
        { FieldType.FT_Uint64    , EncodeUint64 },
        { FieldType.FT_Sint32    , EncodeSint32 },
        { FieldType.FT_Sint64    , EncodeSint64 },
        { FieldType.FT_Fixed32   , EncodeFixed32 },
        { FieldType.FT_FixeD64   , EncodeFixed64 },
        { FieldType.FT_Sfixed32  , EncodeSfixed32 },
        { FieldType.FT_Sfixed64  , EncodeSfixed64 },
        { FieldType.FT_Bool      , EncodeBool },
        { FieldType.FT_String    , EncodeString },
        { FieldType.FT_Group     , EncodeGroup },
        { FieldType.FT_Message   , EncodeMessage },
        { FieldType.FT_Bytes     , EncodeBytes },
        { FieldType.FT_Enum      , EncodeEnum },
        };

        #region 内部函数

        /// <summary>
        /// Encodes the double.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeDouble(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isnumber(l, p))
                output.WriteDouble(LuaDLL.lua_tonumber(l, p));
        }

        /// <summary>
        /// Encodes the float.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeFloat(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isnumber(l, p))
                output.WriteFloat((float)LuaDLL.lua_tonumber(l, p));
        }

        /// <summary>
        /// Encodes the int32.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeInt32(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isinteger(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteInt32(LuaDLL.xlua_tointeger(l, p));
            else
                Debug.LogError(string.Format("EncodeInt32 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the int64.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeInt64(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isint64(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteInt64((long)LuaDLL.lua_toint64(l, p));
            else
                Debug.LogError(string.Format("EncodeInt64 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the uint32.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeUint32(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isinteger(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteUInt32((uint)LuaDLL.xlua_tointeger(l, p));
            else
                Debug.LogError(string.Format("EncodeUint32 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the uint64.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeUint64(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isint64(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteUInt64((ulong)LuaDLL.lua_toint64(l, p));
            else
                Debug.LogError(string.Format("EncodeUint64 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the sint32.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeSint32(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isinteger(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteSInt32(LuaDLL.xlua_tointeger(l, p));
            else
                Debug.LogError(string.Format("EncodeSint32 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the sint64.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeSint64(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isint64(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteSInt64((long)LuaDLL.lua_toint64(l, p));
            else
                Debug.LogError(string.Format("EncodeSint64 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the fixed32.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeFixed32(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isinteger(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteFixed32((uint)LuaDLL.xlua_tointeger(l, p));
            else
                Debug.LogError(string.Format("EncodeFixed32 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the fixe d64.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeFixed64(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isint64(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteFixed64((ulong)LuaDLL.lua_toint64(l, p));
            else
                Debug.LogError(string.Format("EncodeFixed64 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the sfixed32.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeSfixed32(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isinteger(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteSFixed32(LuaDLL.xlua_tointeger(l, p));
            else
                Debug.LogError(string.Format("EncodeSfixed32 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the sfixed64.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeSfixed64(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isint64(l, p) || LuaDLL.lua_isnumber(l, p))
                output.WriteSFixed64((long)LuaDLL.lua_toint64(l, p));
            else
                Debug.LogError(string.Format("EncodeSfixed64 Error {0}", LuaDLL.lua_isnumber(l, p)));
        }

        /// <summary>
        /// Encodes the bool.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeBool(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isboolean(l, p))
                output.WriteBool(LuaDLL.lua_toboolean(l, p));
        }

        /// <summary>
        /// Encodes the string.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeString(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isstring(l, p))
                output.WriteString(LuaDLL.lua_tostring(l, p));  // 会在前端添加 length
            else
                output.WriteString(string.Empty);
        }

        /// <summary>
        /// Encodes the group.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeGroup(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            throw new Exception("[Slua to Proto] EncodeGroup 已废弃");
        }

        /// <summary>
        /// Encodes the message.    /// 递归message，会自动写入长度字段
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeMessage(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            // 获取模板
            ProtoMessage msgTemplate = field.UserDefined as ProtoMessage;
            if (msgTemplate == null)
                throw new Exception("[Slua to Proto] 错误的嵌套msg模板, name = " + field.UserDefined.Name);

            // 递归message，需要写入长度字段
            using (MemoryStream ms = new MemoryStream())
            {
                CodedOutputStream messageStream = new CodedOutputStream(ms);

                // 写消息
                LuaTable m;
                var translator = XLua.ObjectTranslatorPool.Instance.Find(l);
                translator.Get(l, p, out m);
                EncodeMessage(m, msgTemplate, messageStream);

                // 在前端添加 length
                messageStream.Flush();
                output.WriteRawVarint32((uint)ms.Length);
                output.WriteRawBytes(ms.ToArray());
            }
        }

        /// <summary>
        /// Encodes the bytes.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeBytes(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            // 引用 lua 的byte[]
            // 后续 write 的时候会 copy
            var ret = LuaDLL.lua_tobytes(l, p);
            ByteString buf = ByteString.Unsafe.FromBytes(ret); // unsafe，不过用完就扔
            if (ret != null)
                output.WriteBytes(buf); // 会在前端添加 length
            else
                output.WriteBytes(ByteString.Empty);
        }

        /// <summary>
        /// Encodes the enum.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void EncodeEnum(IntPtr l, int p, CodedOutputStream output, ProtoField field)
        {
            if (LuaDLL.lua_isnumber(l, p))
                output.WriteEnum(LuaDLL.xlua_tointeger(l, p));
        }

        /// <summary>
        /// 独立字段编码.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void SingleFieldEncode(LuaTable luaData, CodedOutputStream output, ProtoField field)
        {
            // 跳转lua栈环境，定位到table
            IntPtr l = luaData.L;
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, luaData.Ref);

            // 查找字段
            LuaDLL.lua_pushstring(l, field.Name);
            LuaDLL.xlua_pgettable(l, -2);
            int p = LuaDLL.lua_absindex(l, -1);

            // 获取字段值，并写入对应数据格式
            if (!LuaDLL.lua_isnil(l, p))
            {
                output.WriteTag(field.Tag);
                mSluaEncodeFuncMap[field.FieldType](l, p, output, field);
            }

            // 还原lua栈
            LuaDLL.lua_remove(l, -2);
            LuaDLL.lua_settop(l, oldTop);
        }

        /// <summary>
        /// 重复 + 定长，以add方式添加数组.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void RepeatedLengthDelimitedEncode(LuaTable luaData, CodedOutputStream output, ProtoField field)
        {
            Debug.Assert(field.FieldType == FieldType.FT_String ||
                field.FieldType == FieldType.FT_Message ||
                field.FieldType == FieldType.FT_Bytes,
                "[Slua to Proto] 重复+定长字段中不应该存在 {0} 类型, name = {1}");

            // 此处需要解析的是一个array的table
            // 跳转lua栈环境，定位到table
            IntPtr l = luaData.L;
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, luaData.Ref);

            // 查找字段
            LuaDLL.lua_pushstring(l, field.Name);
            LuaDLL.xlua_pgettable(l, -2);

            // 获取到 array - table
            int p = LuaDLL.lua_absindex(l, -1);
            int count = LuaDLL.lua_rawlen(l, p);
            for (int i = 0; i < count; i++)
            {
                // 用个新的数据块来写东西
                // 这儿是为了绕过 size 计算
                // TODO 最终效果需要测试一下看
                using (MemoryStream ms = new MemoryStream())
                {
                    CodedOutputStream tempOutput = new CodedOutputStream(ms);

                    // 获取字段值，并写入对应数据格式
                    LuaDLL.xlua_rawgeti(l, p, i + 1);
                    mSluaEncodeFuncMap[field.FieldType](l, -1, tempOutput, field);
                    LuaDLL.lua_pop(l, 1);
                    tempOutput.Flush();

                    // 1. 写tag
                    // Add 方式添加元素，每次添加都当做一次字段解析
                    // 所以每次添加的时候都需要填写tag
                    output.WriteTag(field.Tag);

                    // 2. 写长度，buff_length
                    // 不用写长度，这个类型只有 String, Message, Bytes 会用到
                    // 儿而这三种类型，在Encode的时候会自动写入长度

                    // 3. 写数据
                    output.WriteRawBytes(ms.ToArray());
                }
            }

            // 还原lua栈
            LuaDLL.lua_remove(l, -2); // 查找字段
            LuaDLL.lua_settop(l, oldTop); // 进入时的状态
        }

        /// <summary>
        /// 定长字段编码.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void SingleLengthDelimitedEncode(LuaTable luaData, CodedOutputStream output, ProtoField field)
        {
            Debug.Assert(field.FieldType != FieldType.FT_String &&
                field.FieldType != FieldType.FT_Message &&
                field.FieldType != FieldType.FT_Bytes,
                string.Format("[Slua to Proto] 定长字段中不应该存在 {0} 类型, name = {1}", field.FieldType, field.Name));

            // 此处需要解析的是一个array的table
            // 跳转lua栈环境，定位到table
            IntPtr l = luaData.L;
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, luaData.Ref);

            // 查找字段
            LuaDLL.lua_pushstring(l, field.Name);
            LuaDLL.xlua_pgettable(l, -2);

            // 获取到 array - table
            int p = LuaDLL.lua_absindex(l, -1);
            if (!LuaDLL.lua_isnil(l, p))
            {
                // 1.写tag
                output.WriteTag(field.Tag);

                // 用个新的数据块来写东西
                // 这儿是为了绕过 size 计算
                // TODO 最终效果需要测试一下看
                using (MemoryStream ms = new MemoryStream())
                {
                    CodedOutputStream tempOutput = new CodedOutputStream(ms);

                    // 准备数据array
                    int count = LuaDLL.lua_rawlen(l, p);
                    for (int i = 0; i < count; i++)
                    {
                        LuaDLL.xlua_rawgeti(l, p, i + 1);
                        mSluaEncodeFuncMap[field.FieldType](l, -1, tempOutput, field);
                        LuaDLL.lua_pop(l, 1);
                    }

                    // 2.写长度
                    tempOutput.Flush();
                    output.WriteRawVarint32((uint)ms.Length);

                    // 3.写数据
                    output.WriteRawBytes(ms.ToArray());
                }
            }

            // 还原lua栈
            LuaDLL.lua_remove(l, -2); // 查找字段
            LuaDLL.lua_settop(l, oldTop); // 进入时的状态
        }

        /// <summary>
        /// 数据类型编码.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="output">Output.</param>
        /// <param name="field">Field.</param>
        private static void MapFieldEecode(LuaTable luaData, CodedOutputStream output, ProtoField field)
        {
           Debug.Assert(field.FieldType == FieldType.FT_Message,
                string.Format("[Slua to Proto] map 字段只能是 FT_Message 类型，FieldType = {0}, name = {1}", field.FieldType, field.Name));

            // 解析kv模板
            ProtoMessage kvTemplate = field.UserDefined as ProtoMessage;
            if (kvTemplate == null)
            {
                TFW.D.Warning("[Slua to Proto] 错误的map嵌套msg模板, name = " + field.UserDefined.Name);
                return;
            }

            // 解析kv类型
            ProtoField keyT = null, valueT = null;
            var kvTypeIt = kvTemplate.FieldMap.GetEnumerator();
            while (kvTypeIt.MoveNext())
            {
                uint kvFlag = kvTypeIt.Current.Key >> 3;
                if (kvFlag == 2)
                    valueT = kvTypeIt.Current.Value;
                else if (kvFlag == 1)
                    keyT = kvTypeIt.Current.Value;
                else
                {
                    TFW.D.Warning("[Slua to Proto] map 解析异常，数据只能有k、v, kvTag = {0}", kvTypeIt.Current.Key);
                    return;
                }
            }

            // 此处需要解析的是一个map的table
            LuaTable luaMap = luaData.Get<LuaTable>(field.Name);
            if (luaMap == null)
            {
                TFW.D.Warning("[Slua to Proto] 获取 map 字段失败，name = {0}", field.Name);
                return;
            }
            // 遍历所有字段，打包
            // 尽量不要直接使用 it.Current, 避免 boxing 操作
            ///临时加一个解决眼下的问题,2019-01-24 14:10:43 LOP
            IEnumerable keys = luaMap.GetKeys();
            foreach (var key in keys)
            {
                using (MemoryStream elementMs = new MemoryStream())
                {
                    CodedOutputStream eOutput = new CodedOutputStream(elementMs);

                    // 获取key字段值，并写入对应数据格式
                    using (MemoryStream ms = new MemoryStream())
                    {
                        CodedOutputStream tempOutput = new CodedOutputStream(ms);
                        mSluaEncodeFuncMap[keyT.FieldType](luaMap.L, -2, tempOutput, keyT); // key
                        tempOutput.Flush();
                        eOutput.WriteTag(keyT.Tag); // 1. 写tag
                        eOutput.WriteRawBytes(ms.ToArray()); // 3. 写数据
                    }

                    // 获取value字段值，并写入对应数据格式
                    using (MemoryStream ms = new MemoryStream())
                    {
                        CodedOutputStream tempOutput = new CodedOutputStream(ms);
                        mSluaEncodeFuncMap[valueT.FieldType](luaMap.L, -1, tempOutput, valueT); // value
                        tempOutput.Flush();
                        eOutput.WriteTag(valueT.Tag); // 1. 写tag
                        eOutput.WriteRawBytes(ms.ToArray()); // 3. 写数据
                    }

                    // 写入一个 kv 对
                    eOutput.Flush();
                    output.WriteTag(field.Tag);// 1. 写tag
                    output.WriteRawVarint32((uint)elementMs.Length); // 2. 写长度，这个 kvMsg 的 buff_length
                    output.WriteRawBytes(elementMs.ToArray()); // 3. 写数据
                }
            }
            //         var it = 0;
            //        while (it < luaMap.Length)
            //        {
            //            it++;
            //            using (MemoryStream elementMs = new MemoryStream())
            //            {
            //                CodedOutputStream eOutput = new CodedOutputStream(elementMs);
            //
            //                // 获取key字段值，并写入对应数据格式
            //                using (MemoryStream ms = new MemoryStream())
            //                {
            //                    CodedOutputStream tempOutput = new CodedOutputStream(ms);
            //                    mSluaEncodeFuncMap[keyT.FieldType](luaMap.L, -2, tempOutput, keyT); // key
            //                    tempOutput.Flush();
            //                    eOutput.WriteTag(keyT.Tag); // 1. 写tag
            //                    eOutput.WriteRawBytes(ms.ToArray()); // 3. 写数据
            //                }
            //
            //                // 获取value字段值，并写入对应数据格式
            //                using (MemoryStream ms = new MemoryStream())
            //                {
            //                    CodedOutputStream tempOutput = new CodedOutputStream(ms);
            //                    mSluaEncodeFuncMap[valueT.FieldType](luaMap.L, -1, tempOutput, valueT); // value
            //                    tempOutput.Flush();
            //                    eOutput.WriteTag(valueT.Tag); // 1. 写tag
            //                    eOutput.WriteRawBytes(ms.ToArray()); // 3. 写数据
            //                }
            //
            //                // 写入一个 kv 对
            //                eOutput.Flush();
            //                output.WriteTag(field.Tag);// 1. 写tag
            //                output.WriteRawVarint32((uint)elementMs.Length); // 2. 写长度，这个 kvMsg 的 buff_length
            //                output.WriteRawBytes(elementMs.ToArray()); // 3. 写数据
            //            }
            //        }
        }

        /// <summary>
        /// Encodes the message impl.
        /// </summary>
        /// <param name="luaData">Lua data.</param>
        /// <param name="msgTemplate">Message template.</param>
        /// <param name="output">Output.</param>
        private static void EncodeMessage(LuaTable luaData, ProtoMessage msgTemplate, CodedOutputStream output)
        {
            // 遍历所有字段，记录数据值
            var it = msgTemplate.FieldMap.GetEnumerator();
            while (it.MoveNext())
            {
                // 解析一个 field
                ProtoField field = it.Current.Value;

                // map类型
                if (field.LabelType == LabelType.LT_Map)
                    MapFieldEecode(luaData, output, field);

                // 单一字段
                else if (field.LabelType != LabelType.LT_Repeated)
                    SingleFieldEncode(luaData, output, field);

                // 重复 + 定长，以add方式添加数组
                else if (ProtoDescriptor.IsLengthDelimited(field.FieldType))
                    RepeatedLengthDelimitedEncode(luaData, output, field);

                // 简单重复
                else
                    SingleLengthDelimitedEncode(luaData, output, field);
            }
        }

        #endregion

        /// <summary>
        /// 将一个消息，编码为 byte[].
        /// 按照给定模板，编码消息
        /// </summary>
        /// <returns>编码结果，如果失败返回null.</returns>
        /// <param name="l">L.</param>
        /// <param name="luaData">Lua data.</param>
        public static byte[] EncodeFromSlua(uint msgID, LuaTable luaData)
        {
            // 获取模板
            ProtoMessage msgTemplate = ProtoMgr.GetProtoMessage(msgID);
            if (msgTemplate == null)
                throw new Exception("[Slua to Proto] 未定义的消息编号, MsgID = " + msgID);

            // 创建一个编码器，开始干活儿
            using (MemoryStream memStream = new MemoryStream())
            {
                CodedOutputStream output = new CodedOutputStream(memStream);
                // 先写入长度位 4
                output.WriteRawLittleEndian32(0);
                // id
                output.WriteRawLittleEndian32(msgID);

                // 解析数据体
                EncodeMessage(luaData, msgTemplate, output);
                output.Flush();

                // 补上最终长度
                byte[] retBuff = memStream.ToArray();
                // 注意：这里的消息长度已经是完整的长度，因此要减去长度位大小
                byte[] lenByte = BitConverter.GetBytes(Convert.ToInt32(memStream.Length - TFW.NetMsgData.BuffLengthSize));
                lenByte.CopyTo(retBuff, 0);

                return retBuff;
            }
        }

        //    static string GetBytesString(byte[] bytes, int index, int count, string sep)
        //    {
        //        var sb = new StringBuilder();
        //        for (int i = index; i < count - 1; i++)
        //        {
        //            sb.Append(bytes[i].ToString("D3") + sep);
        //        }
        //        sb.Append(bytes[index + count - 1].ToString("D3"));
        //        return sb.ToString();
        //    }
        /// <summary>
        /// Encodes from slua raw, 获取纯数据 buff.
        /// </summary>
        /// <returns>The from slua raw.</returns>
        /// <param name="msgID">Message I.</param>
        /// <param name="luaData">Lua data.</param>
        public static byte[] EncodeFromSluaRaw(uint msgID, LuaTable luaData)
        {
            // 获取模板
            ProtoMessage msgTemplate = ProtoMgr.GetProtoMessage(msgID);
            if (msgTemplate == null)
                throw new Exception("[Slua to Proto] 未定义的消息编号, MsgID = " + msgID);

            // 创建一个编码器，开始干活儿
            using (MemoryStream memStream = new MemoryStream())
            {
                // 解析数据体
                CodedOutputStream output = new CodedOutputStream(memStream);
                EncodeMessage(luaData, msgTemplate, output);
                output.Flush();

                return memStream.ToArray();
            }
        }
    }
}