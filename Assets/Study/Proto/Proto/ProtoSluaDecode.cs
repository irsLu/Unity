/// <summary>
/// ProtoSluaDecode.cs
/// Created by wangxiangwei 2017-7-17
/// Protobuf解析器：Slua
///
/// 1. 将Proto解析成Slua数据，通过虚拟机传入
///    目标数据为纯table形式，没有对象继承
/// 2. 数据格式以 proto3 为标准
/// 3. 尽量直接使用栈操作，LuaObject使用了装/拆箱封装，效率更慢
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using LuaDLL = XLua.LuaDLL.Lua;

namespace Google.Protobuf
{
    public static partial class ProtoSlua
    {
        // 是否将消息的 proto 类型名，写入到 table._mt 中
        public static bool IsOutputMT = false;

        // 解码函数表
        private static Dictionary<FieldType, Action<IntPtr, CodedInputStream, ProtoField>> mSluaDecodeFuncMap =
            new Dictionary<FieldType, Action<IntPtr, CodedInputStream, ProtoField>>() {
        { FieldType.FT_Double    , DecodeDouble },
        { FieldType.FT_Float     , DecodeFloat },
        { FieldType.FT_Int32     , DecodeInt32 },
        { FieldType.FT_Int64     , DecodeInt64 },
        { FieldType.FT_Uint32    , DecodeUint32 },
        { FieldType.FT_Uint64    , DecodeUint64 },
        { FieldType.FT_Sint32    , DecodeSint32 },
        { FieldType.FT_Sint64    , DecodeSint64 },
        { FieldType.FT_Fixed32   , DecodeFixed32 },
        { FieldType.FT_FixeD64   , DecodeFixed64 },
        { FieldType.FT_Sfixed32  , DecodeSfixed32 },
        { FieldType.FT_Sfixed64  , DecodeSfixed64 },
        { FieldType.FT_Bool      , DecodeBool },
        { FieldType.FT_String    , DecodeString },
        { FieldType.FT_Group     , DecodeGroup },
        { FieldType.FT_Message   , DecodeMessage },
        { FieldType.FT_Bytes     , DecodeBytes },
        { FieldType.FT_Enum      , DecodeEnum },
        };

        #region 内部函数

        /// <summary>
        /// Decodes the double.
        /// </summary>
        /// <returns><c>true</c>, if double was decoded, <c>false</c> otherwise.</returns>
        /// <param name="input">Input.</param>
        private static void DecodeDouble(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushnumber(l, input == null ? 0 : input.ReadDouble());
        }

        /// <summary>
        /// Decodes the float.
        /// </summary>
        /// <returns><c>true</c>, if float was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeFloat(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushnumber(l, input == null ? 0 : input.ReadFloat());
        }

        /// <summary>
        /// Decodes the int32.
        /// </summary>
        /// <returns><c>true</c>, if int64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeInt32(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.xlua_pushinteger(l, input == null ? 0 : input.ReadInt32());
        }

        /// <summary>
        /// Decodes the int64.
        /// </summary>
        /// <returns><c>true</c>, if int64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeInt64(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushint64(l, input == null ? 0 : input.ReadInt64());
        }

        /// <summary>
        /// Decodes the uint32.
        /// </summary>
        /// <returns><c>true</c>, if uint32 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeUint32(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.xlua_pushuint(l, input == null ? 0 : input.ReadUInt32());
        }

        /// <summary>
        /// Decodes the uint64.
        /// </summary>
        /// <returns><c>true</c>, if uint64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeUint64(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushuint64(l, input == null ? 0 : input.ReadUInt64());
        }

        /// <summary>
        /// Decodes the sint32.
        /// </summary>
        /// <returns><c>true</c>, if sint32 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeSint32(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.xlua_pushinteger(l, input == null ? 0 : input.ReadSInt32());
        }

        /// <summary>
        /// Decodes the sint64.
        /// </summary>
        /// <returns><c>true</c>, if sint64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeSint64(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushint64(l, input == null ? 0 : input.ReadSInt64());
        }

        /// <summary>
        /// Decodes the fixed32.
        /// </summary>
        /// <returns><c>true</c>, if fixed32 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeFixed32(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.xlua_pushinteger(l, input == null ? 0 : (int)input.ReadFixed32()); // make sure ? uint转int，有缩小
        }

        /// <summary>
        /// Decodes the fixe d64.
        /// </summary>
        /// <returns><c>true</c>, if fixe d64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeFixed64(IntPtr l, CodedInputStream input, ProtoField field)
        {
            // lua没有 long，作为double输入即可
            LuaDLL.lua_pushuint64(l, input == null ? 0 : input.ReadFixed64());
        }

        /// <summary>
        /// Decodes the sfixed32.
        /// </summary>
        /// <returns><c>true</c>, if sfixed32 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeSfixed32(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.xlua_pushinteger(l, input == null ? 0 : input.ReadSFixed32());
        }

        /// <summary>
        /// Decodes the sfixed64.
        /// </summary>
        /// <returns><c>true</c>, if sfixed64 was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeSfixed64(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushint64(l, input == null ? 0 : input.ReadSFixed64());
        }

        /// <summary>
        /// Decodes the bool.
        /// </summary>
        /// <returns><c>true</c>, if bool was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeBool(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushboolean(l, input == null ? false : input.ReadBool());
        }

        /// <summary>
        /// Decodes the string.
        /// </summary>
        /// <returns><c>true</c>, if string was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeString(IntPtr l, CodedInputStream input, ProtoField field)
        {
            LuaDLL.lua_pushstring(l, input == null ? string.Empty : input.ReadString());
        }

        /// <summary>
        /// Decodes the group.
        /// </summary>
        /// <returns><c>true</c>, if group was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeGroup(IntPtr l, CodedInputStream input, ProtoField field)
        {
            throw new Exception("[Proto to Slua] DecodeGroup 已废弃");
        }

        /// <summary>
        /// Decodes the bytes.
        /// </summary>
        /// <returns><c>true</c>, if bytes was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeBytes(IntPtr l, CodedInputStream input, ProtoField field)
        {
            var bytes = input == null ? ByteString.AttachBytes(new byte[0]) : input.ReadBytes();
            // bytes先作为用户数据类型传入
            System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(bytes);
            LuaDLL.lua_pushlightuserdata(l, System.Runtime.InteropServices.GCHandle.ToIntPtr(handle)); // TODO to test
        }

        /// <summary>
        /// Decodes the enum.
        /// </summary>
        /// <returns><c>true</c>, if enum was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeEnum(IntPtr l, CodedInputStream input, ProtoField field)
        {
            // enum 直接当int用即可
            LuaDLL.xlua_pushinteger(l, input == null ? 0 : input.ReadEnum());
        }

        /// <summary>
        /// Reads the key int.
        /// </summary>
        /// <returns>The key int.</returns>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static int ReadKeyInt(CodedInputStream input, FieldType ft)
        {
            switch (ft)
            {
                case FieldType.FT_Int32:
                    return input.ReadInt32();

                case FieldType.FT_Sint32:
                    return input.ReadSInt32();

                case FieldType.FT_Enum:
                    return input.ReadEnum();

                default:
                    input.SkipLastField();
                    TFW.D.Warning("[Proto to Slua] 不支持的map.key字段类型，FieldType = {0}", ft.ToString());
                    return -1;
            }
        }

        private static long ReadKeyInt64(CodedInputStream input)
        {
            return input.ReadInt64();
        }

        /// <summary>
        /// map字段解码.
        /// </summary>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="refMap">Reference map.</param>
        private static void MapFieldDecode(IntPtr l, CodedInputStream input, ProtoField field, uint tag, ref Dictionary<uint, int> refMap)
        {
            // map字段在proto中的编码，是按照
            // repeated message 的形式
            // 但是在解析的时候，期望转化成 key = value 的 table 形式
            // 所以此处需要单独的处理流程
            // 这段补丁真的有点丑……

            // 确认一下是否新的数组
            int rtRef;
            if (!refMap.TryGetValue(tag, out rtRef))
            {
                // 新建
                LuaDLL.lua_newtable(l);
                rtRef = LuaDLL.luaL_ref(l, XLua.LuaIndexes.LUA_REGISTRYINDEX);
                refMap.Add(tag, rtRef); // 记录在案

                // 添加到table
                LuaDLL.lua_getref(l, rtRef);
                LuaDLL.lua_setfield(l, -2, field.Name);
            }

            if (input == null) return;

            // 进入这个map的table
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, rtRef);

            // 字段类型定制为message
            if (field.FieldType != FieldType.FT_Message)
            {
                TFW.D.Warning("[Proto to Slua] map字段解析异常, fieldName = {0}, fieldType = {1} (expect FT_Message)",
                    field.Name, field.FieldType.ToString());
                input.SkipLastField();
                goto jump_end;
            }

            // 获取模板
            ProtoMessage kvTemplate = field.UserDefined as ProtoMessage;
            if (kvTemplate == null)
            {
                TFW.D.Warning("[Proto to Slua] 错误的嵌套msg模板, name = " + field.UserDefined.Name);
                input.SkipLastField();
                goto jump_end;
            }

            // 当场解析一个message
            int length = input.ReadLength();
            int oldLimit = input.PushLimit(length);

            // 记录模板名
            if (IsOutputMT)
            {
                LuaDLL.lua_pushstring(l, kvTemplate.Name);
                LuaDLL.lua_setfield(l, -2, "_mt");
            }

            // 不清楚key是int还是string，都准备一下
            string keyString = string.Empty;
            int keyInt = -1;
            long keyInt64 = -1;

            // 定制为: {1=key, 2=value}
            int kvCount = 0;
            uint kvTag;
            while ((kvTag = input.ReadTag()) != 0)
            {
                // 统计一下，按理说只有2个值
                kvCount++;

                // 从 msg 模板的字段 map 中获取 field 模板
                ProtoField kvField;
                if (!kvTemplate.FieldMap.TryGetValue(kvTag, out kvField))
                {
                    // 字段模板都没找到，跳过
                    TFW.D.Warning("[Proto to Slua] map 内嵌字段模板或字段wire错误, msg = {0}, field_tag = {1}\n1. 请检测 Proto 协议是否更新\n2.连接的服务器是否正确",
                        kvTemplate.Name, kvTag);
                    input.SkipLastField();
                    continue;
                }

                // map 中的 kv，只能是单一字段
                if (kvField.LabelType != LabelType.LT_Single)
                {
                    TFW.D.Warning("[Proto to Slua] map 内嵌字段 label 只支持 single, LabelType = {0}", kvField.LabelType);
                    input.SkipLastField();
                    continue;
                }

                // 看看是key还是value
                uint kvFlag = kvTag >> 3;
                if (kvFlag == 2)
                {
                    // value, 直接压栈
                    mSluaDecodeFuncMap[kvField.FieldType](l, input, kvField);
                }
                else if (kvFlag == 1)
                {
                    // key, 只解析，不压栈
                    if (kvField.FieldType == FieldType.FT_String)
                        keyString = input.ReadString();
                    else if (kvField.FieldType == FieldType.FT_Int64)
                    {
                        keyInt64 = ReadKeyInt64(input);
                    }
                    else
                        keyInt = ReadKeyInt(input, kvField.FieldType);
                }
                else
                {
                    TFW.D.Warning("[Proto to Slua] map 解析异常，数据只能有k、v, kvTag = {0}", kvTag);
                    input.SkipLastField();
                    continue;
                }
            }

            // Google: Check that we've read exactly as much data as expected.
            input.CheckReadEndOfStreamTag();
            if (!input.ReachedLimit)
                throw InvalidProtocolBufferException.TruncatedMessage();
            input.PopLimit(oldLimit);

            // 前面应该已经写入了value，此处压栈key即可
            if (string.IsNullOrEmpty(keyString) && keyInt64 == -1)
                LuaDLL.xlua_rawseti(l, -2, keyInt);
            else if (string.IsNullOrEmpty(keyString))
            {
                LuaDLL.xlua_rawseti(l, -2, keyInt64);
            }
            else
                LuaDLL.lua_setfield(l, -2, keyString);

            // 离开这个map的table
            jump_end:
            LuaDLL.lua_settop(l, oldTop);
        }

        /// <summary>
        /// 数组字段解码.
        /// </summary>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void RepeatedFieldDecode(IntPtr l, CodedInputStream input, ProtoField field, uint tag, ref Dictionary<uint, int> refMap)
        {
            // 确认一下是否新的数组
            int rtRef;
            if (!refMap.TryGetValue(tag, out rtRef))
            {
                // 新建
                LuaDLL.lua_newtable(l);
                rtRef = LuaDLL.luaL_ref(l, XLua.LuaIndexes.LUA_REGISTRYINDEX);
                refMap.Add(tag, rtRef); // 记录在案

                // 添加到table
                LuaDLL.lua_getref(l, rtRef);
                LuaDLL.lua_setfield(l, -2, field.Name);
            }
            if (input == null) return;
            // 进入这个array的table
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, rtRef);

            // 普通字段，区别不同类型进行字段解析，结果压入lua栈
            mSluaDecodeFuncMap[field.FieldType](l, input, field);

            // 解析结果添加到数组，从栈上计算元素个数
            int index = LuaDLL.lua_rawlen(l, oldTop + 1) + 1;
            LuaDLL.xlua_rawseti(l, -2, index);

            // 离开这个array的table
            LuaDLL.lua_settop(l, oldTop);
        }

        /// <summary>
        /// 类似 （Repeated int32) 的数据.
        /// 需要按照LengthDelimited的方式解析
        /// </summary>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void SingleLengthDelimitedDecode(IntPtr l, CodedInputStream input, ProtoField field, uint tag, ref Dictionary<uint, int> refMap)
        {
            // 确认一下是否新的数组
            int rtRef;
            if (!refMap.TryGetValue(tag, out rtRef))
            {
                // 新建
                LuaDLL.lua_newtable(l);
                rtRef = LuaDLL.luaL_ref(l, XLua.LuaIndexes.LUA_REGISTRYINDEX);
                refMap.Add(tag, rtRef); // 记录在案

                // 添加到table
                LuaDLL.lua_getref(l, rtRef);
                LuaDLL.lua_setfield(l, -2, field.Name);
            }
            if (input == null) return;
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, rtRef);

            // 获取打包长度
            int length = input.ReadLength();
            if (length == 0)
            {
                LuaDLL.lua_settop(l, oldTop);
                return;
            }

            // 递归栈解析
            int oldLimit = input.PushLimit(length);
            while (!input.ReachedLimit)
            {
                // 普通字段，区别不同类型进行字段解析，结果压入lua栈
                mSluaDecodeFuncMap[field.FieldType](l, input, field);

                // 解析结果添加到数组，从栈上计算元素个数
                int index = LuaDLL.lua_rawlen(l, oldTop + 1) + 1;
                LuaDLL.xlua_rawseti(l, -2, index);
            }
            input.PopLimit(oldLimit);

            LuaDLL.lua_settop(l, oldTop);
        }

        /// <summary>
        /// 单一字段解码.
        /// </summary>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void SingleFieldDecode(IntPtr l, CodedInputStream input, ProtoField field)
        {
            mSluaDecodeFuncMap[field.FieldType](l, input, field);
            LuaDLL.lua_setfield(l, -2, field.Name);
        }

        /// <summary>
        /// 递归调用解码msg.
        /// lua栈的处理有所不同，所有抽取一个封装
        /// </summary>
        /// <returns><c>true</c>, if message was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="field">Field.</param>
        private static void DecodeMessage(IntPtr l, CodedInputStream input, ProtoField field)
        {
            if (input == null)
            {
                LuaDLL.lua_pushnil(l);
                return;
            }
            // 获取模板
            ProtoMessage msgTemplate = field.UserDefined as ProtoMessage;
            if (msgTemplate == null)
                throw new Exception("[Proto to Slua] 错误的嵌套msg模板, name = " + field.UserDefined.Name);

            // 新建一个table
            LuaDLL.lua_newtable(l);
            int msgRef = LuaDLL.luaL_ref(l, XLua.LuaIndexes.LUA_REGISTRYINDEX);
            int oldTop = LuaDLL.lua_gettop(l);
            LuaDLL.lua_getref(l, msgRef);

            // 将msg数据填写到table中
            // TODO，尚未处理递归msg过深的问题
            int length = input.ReadLength();
            int oldLimit = input.PushLimit(length);
            DecodeMessageImpl(l, input, msgTemplate, field);
            input.CheckReadEndOfStreamTag();

            // Google: Check that we've read exactly as much data as expected.
            if (!input.ReachedLimit)
                throw InvalidProtocolBufferException.TruncatedMessage();
            input.PopLimit(oldLimit);

            // 完成这个table
            LuaDLL.lua_settop(l, oldTop);
            LuaDLL.lua_getref(l, msgRef);
        }

        static Queue<Dictionary<uint, int>> s_RepeatedFieldTableRefQueue = new Queue<Dictionary<uint, int>>();
        static Queue<List<uint>> s_FixedTagsQueue = new Queue<List<uint>>();
        /// <summary>
        /// Decodes the message.
        /// </summary>
        /// <returns><c>true</c>, if message was decoded, <c>false</c> otherwise.</returns>
        /// <param name="l">L.</param>
        /// <param name="input">Input.</param>
        /// <param name="msgTemplate">Message template.</param>
        private static void DecodeMessageImpl(IntPtr l, CodedInputStream input, ProtoMessage msgTemplate, ProtoField parentfield = null)
        {
            // 担心 Repeated 数据不是连续下发，所以用 lua_ref 的方式管理数组table
            // 用个列表缓存一下所有 Repeated Field
            var repeatedFieldTableRef = s_RepeatedFieldTableRefQueue.Count > 0 ? s_RepeatedFieldTableRefQueue.Dequeue() : new Dictionary<uint, int>();

            // 记录模板名
            if (IsOutputMT)
            {
                LuaDLL.lua_pushstring(l, msgTemplate.Name);
                LuaDLL.lua_setfield(l, -2, "_mt");
            }
            var fixedTags = s_FixedTagsQueue.Count > 0 ? s_FixedTagsQueue.Dequeue() : new List<uint>();
            // 开始干活儿
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                // 从 msg 模板的字段 map 中获取 field 模板
                ProtoField field;
                if (!msgTemplate.FieldMap.TryGetValue(tag, out field))
                {
                    // 字段模板都没找到，跳过
                    TFW.D.Warning("[Proto to Slua] 字段模板或字段wire错误, msg = {0}, field_tag = {1}\n1. 请检测 Proto 协议是否更新\n2.连接的服务器是否正确",
                        msgTemplate.Name, tag);
                    input.SkipLastField();
                    continue;
                }

                fixedTags.Add(tag);

                // 解析字段数据
                //TFW.D.Assert(mSluaDecodeFuncMap.ContainsKey(field.FieldType), "没有指定字段类型的解析函数，FT = {0}", field.FieldType);
                // map类型
                if (field.LabelType == LabelType.LT_Map)
                    MapFieldDecode(l, input, field, tag, ref repeatedFieldTableRef);

                // 单一字段
                else if (field.LabelType != LabelType.LT_Repeated)
                    SingleFieldDecode(l, input, field);

                // 原本字段类型就是数组字段，以add方式添加数组
                else if (ProtoDescriptor.IsLengthDelimited(field.FieldType))
                    RepeatedFieldDecode(l, input, field, tag, ref repeatedFieldTableRef);

                // 定长字段组成的数组类型，以简单 LengthDelimited 解析
                else
                    SingleLengthDelimitedDecode(l, input, field, tag, ref repeatedFieldTableRef);
            }

            //Fix empty field
            foreach (var kv in msgTemplate.FieldMap)
            {
                tag = kv.Key;
                if (!fixedTags.Contains(tag))
                {
                    var field = kv.Value;
                    // 解析字段数据
                    //TFW.D.Assert(mSluaDecodeFuncMap.ContainsKey(field.FieldType), "没有指定字段类型的解析函数，FT = {0}", field.FieldType);
                    // map类型
                    if (field.LabelType == LabelType.LT_Map)
                        MapFieldDecode(l, null, field, tag, ref repeatedFieldTableRef);

                    // 单一字段
                    else if (field.LabelType != LabelType.LT_Repeated)
                        SingleFieldDecode(l, null, field);

                    // 原本字段类型就是数组字段，以add方式添加数组
                    else if (ProtoDescriptor.IsLengthDelimited(field.FieldType))
                        RepeatedFieldDecode(l, null, field, tag, ref repeatedFieldTableRef);

                    // 定长字段组成的数组类型，以简单 LengthDelimited 解析
                    else
                        SingleLengthDelimitedDecode(l, null, field, tag, ref repeatedFieldTableRef);
                }
            }
            // 读完，检查剩余，给异常
            input.CheckReadEndOfStreamTag();
            var translator = XLua.ObjectTranslatorPool.Instance.Find(l);
            // 释放所有ref
            //var it = repeatedFieldTableRef.Values.GetEnumerator();
            foreach(var kv in repeatedFieldTableRef)
            //while (it.MoveNext())
            {
                if (kv.Value == 0)
                    continue;

                // 特有释放流程，有lock操作
                translator.luaEnv.equeueGCAction(new XLua.LuaEnv.GCAction { Reference = kv.Value, IsDelegate = false });
                //LuaDLL.lua_unref(l, it.Current);
            }
            repeatedFieldTableRef.Clear();
            fixedTags.Clear();
            s_FixedTagsQueue.Enqueue(fixedTags);
            s_RepeatedFieldTableRefQueue.Enqueue(repeatedFieldTableRef);
        }

        #endregion

        /// <summary>
        /// 将一个消息解析给Slua，push到lua栈上
        /// 按照给定模板，解析消息.
        /// </summary>
        /// <returns>是否解析成功.</returns>
        /// <param name="msgData">Message data.</param>
        public static void DecodeToSlua(IntPtr l, TFW.NetMsgData msgData)
        {
            try
            {
                // 获取模板
                ProtoMessage msgTemplate = ProtoMgr.GetProtoMessage(msgData.MsgID);
                if (msgTemplate == null)
                    throw new Exception("[Proto to Slua] 未定义的消息编号, MsgID = " + msgData.MsgID);

                // 准备一个table，消息数据全写进去
                LuaDLL.lua_newtable(l);

                // 创建一个编码解析器，开始干活
                CodedInputStream input = new CodedInputStream(msgData.Buff);

                DecodeMessageImpl(l, input, msgTemplate);;
            }
            catch (InvalidProtocolBufferException e)
            {
                // 加点内容，抛给外面lua层
                throw new Exception("[Proto to Slua] buffer 解析异常,\ne = " + e.Message);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}