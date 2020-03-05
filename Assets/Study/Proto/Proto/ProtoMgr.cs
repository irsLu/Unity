/// <summary>
/// ProtoMgr.cs
/// Created by wangxiangwei 2017-7-25
/// proto解析管理器
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

namespace Google.Protobuf
{
    public class ProtoMgr : TFW.IProtoMgr
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CheckInstance()
        {
            if (Mgr == null)
            {
                Mgr = new ProtoMgr();
            }
        }
        #region 成员变量

        // 消息ID -> 消息模板
        private static Dictionary<uint, ProtoStructBase> mIDMessageMap = new Dictionary<uint, ProtoStructBase>();

        // 消息名 -> 消息模板
        private static Dictionary<string, ProtoStructBase> mNameMessageMap = new Dictionary<string, ProtoStructBase>();

        // 解析得到的全部数据
        // 理论上只开放给 ProtoParser 用
        public static Dictionary<string, ProtoStructBase> MessageMap { get { return mNameMessageMap; } }

        #endregion

        #region 内部函数

        /// <summary>
        /// 解析 msgid.def 文件，获取id->msg映射.
        /// 覆盖写入，不清理
        /// </summary>
        /// <returns>解析是否成功.</returns>
        private static bool ParseIDMessageDef(string msgid)
        {
            mIDMessageMap.Clear();
            // bugfix
            //if (TFW.ResourceMgr.Exists(msgid))
            if(true)
            {
                // bugfix
                //string defFile = TFW.ResourceMgr.LoadText(msgid);
                string defFile = string.Empty;

                // 逐行解析
                string[] lines = defFile.Split('\n');
                foreach (var line in lines)
                {
                    List<string> cells = TFW.Common.Scanf(line, @"(\d+)\s+(.+)");
                    if (cells.Count < 2)
                        continue;

                    // 查找并添加消息编号映射
                    ProtoStructBase pm;
                    if (!mNameMessageMap.TryGetValue(cells[1].Replace("\r","").Replace("\n","").Trim(), out pm))
                    {
                        TFW.D.Warning("[解析.def文件]未定义的消息编号, [ {0}, {1} ]", cells[0], cells[1]);
                        continue;
                    }
                    else
                    {
                        // 记录消息id表
                        var id = uint.Parse(cells[0]);
                        mIDMessageMap[id] = pm;

                        // 更新消息id
                        ProtoMessage pMsg = pm as ProtoMessage;
                        if (pMsg != null)
                            pMsg.ID = id;
                    }
                }
            }
            else
            {
                //这里Hash可能会发生碰撞，但是服务器说了 服务器保证 协议不会碰撞，出现问题找 周帆/蔡波
                foreach (var kv in mNameMessageMap)
                {
                    uint id = BKDRHash(kv.Key);
#if UNITY_EDITOR
                    if (mIDMessageMap.ContainsKey(id))
                    {
                        Debug.LogError("Hash 碰撞了 找你们服务器 -> " + kv.Key + " " + mIDMessageMap[id].Name);
                    }
#endif
                    mIDMessageMap[id] = kv.Value;
                    // 更新消息id
                    ProtoMessage pMsg = kv.Value as ProtoMessage;
                    if (pMsg != null)
                        pMsg.ID = id;
                }
            }
            return true;
        }

        private static uint BKDRHash(string s)
        {
            uint seed = 131;
            uint hash = 0;

            for (int i = 0; i < s.Length; i++)
            {
                hash = hash * seed + s[i];
            }

            return hash;
        }


        /// <summary>
        /// 解析 proto 文件.
        /// </summary>
        /// <returns><c>true</c>, if proto was parsed, <c>false</c> otherwise.</returns>
        /// <param name="file">File.</param>
        private static void ParseProto(string folder, List<string> protos)
        {
            if (!folder.EndsWith("/")) { folder += "/"; }
            ProtoParser.ProtoFolder = folder;
            // 遍历解析所有 proto 文件
            foreach (var proto in protos)
            {
                if (!ProtoParser.ParseFromFile(proto))
                    TFW.D.Warning(".proto 文件解析失败, file = {0}", proto);
            }

            //var it = ResourceMgr.LoadTextStreamBatch(ResourceDict.ProtoBatch).GetEnumerator();
            //while (it.MoveNext())
            //{
            //    D.Log(it.Current.Key);
            //    if (!ProtoParser.ParseFromFile(it.Current.Key, it.Current.Value))
            //        D.Warning(".proto 文件解析失败, file = {0}", it.Current.Key);
            //}

            // 统一的 Tag 编码
            ProtoParser.BuildTag();
        }

        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void Init(string folder, string msgid, List<string> protos)
        {
            // 1.解析每个 message 定义
            ParseProto(folder, protos);

            // 2.解析 msgid.def 生成 id 映射
            ParseIDMessageDef(folder + msgid);
        }

        /// <summary>
        /// 获取消息模板.
        /// </summary>
        /// <returns>The proto message.</returns>
        /// <param name="id">消息id.</param>
        public static ProtoMessage GetProtoMessage(uint id)
        {
            ProtoStructBase structBase;
            if (!mIDMessageMap.TryGetValue(id, out structBase))
                return null;

            return structBase as ProtoMessage;
        }

        /// <summary>
        /// Logs the def.
        /// </summary>
        public static void LogDef()
        {
            TFW.D.Log("[{0}]", TFW.Common.LogDictionaryString(mIDMessageMap, 0, (e) => { return (e as ProtoStructBase).Name; }));
        }

        /// <summary>
        /// Logs the proto.
        /// </summary>
        public static void LogProto()
        {
            TFW.D.Log("[proto]\n{0}", TFW.Common.LogCollectionString(mNameMessageMap.Values));
        }

        /// <summary>
        /// Gets the def.
        /// </summary>
        /// <returns>The def.</returns>
        protected override Dictionary<string, uint> GetMsgidDef()
        {
            var retMap = new Dictionary<string, uint>();
            var it = mIDMessageMap.GetEnumerator();
            while (it.MoveNext())
                retMap.Add(it.Current.Value.Name.Replace(".", "_"), it.Current.Key);

            return retMap;
        }

        /// <summary>
        /// Gets the enum def.
        /// </summary>
        protected override Dictionary<string, Dictionary<string, uint>> GetEnumDef()
        {
            var retMap = new Dictionary<string, Dictionary<string, uint>>();
            var it = mNameMessageMap.GetEnumerator();
            while (it.MoveNext())
            {
                // 只解析 Enum 定义
                ProtoEnum pe = it.Current.Value as ProtoEnum;
                if (pe == null)
                    continue;

                // 命名空间暂不拼接

                // 记录所有元素项
                var newEnum = new Dictionary<string, uint>();
                var e = pe.EnumElementMap.GetEnumerator();
                while (e.MoveNext())
                    newEnum.Add(e.Current.Value, e.Current.Key);
                retMap.Add(pe.Name, newEnum);
            }

            return retMap;
        }

        /// <summary>
        /// 解码操作中，是否将消息的 proto 类型名，写入到 table._mt 中.
        /// </summary>
        /// <param name="isOutputMT">If set to <c>true</c> is output M.</param>
        protected override void SetDecodeMT(bool isOutputMT)
        {
            ProtoSlua.IsOutputMT = isOutputMT;
        }

        /// <summary>
        /// <para>将 msg 解析成 Lua 的 table.</para>
        /// <para>1.直接管理器转调，减少映射类</para>
        /// <para>2.手动导出Slua接口，需要进行栈操作</para>
        /// </summary>
        [XLua.MonoPInvokeCallbackAttribute(typeof(XLua.LuaDLL.lua_CSFunction))]
        static public int DecodeToSlua(IntPtr L)
        {
            // 1. 这儿留个空壳，确保Slua注册对应函数
            // 2. 详细实现参看 ProtoSlua 类
            try
            {
                var translator = XLua.ObjectTranslatorPool.Instance.Find(L);
                var data = (TFW.NetMsgData)translator.GetObject(L, 1, typeof(TFW.NetMsgData));
                translator.PushAny(L, data.MsgID);
                ProtoSlua.DecodeToSlua(L, data); // 在接口中进行 lua_push 相关操作
                return 2;
            }
            catch (Exception e)
            {
                return XLua.LuaDLL.Lua.luaL_error(L, e.Message);
            }
        }

        /// <summary>
        /// <para>将 Lua 的 table 编码成 proto msg .</para>
        /// <para>1.直接管理器转调，减少映射类</para>
        /// <para>2.手动导出Slua接口，需要进行栈操作</para>
        /// </summary>
        [XLua.MonoPInvokeCallbackAttribute(typeof(XLua.LuaDLL.lua_CSFunction))]
        static public int EncodeFromSlua(IntPtr L)
        {
            // 1. 这儿留个空壳，确保Slua注册对应函数
            // 2. 详细实现参看 ProtoSlua 类
            try
            {
                var translator = XLua.ObjectTranslatorPool.Instance.Find(L);

                uint msgID = (uint)XLua.LuaDLL.Lua.xlua_tointeger(L, 1);
                XLua.LuaTable data = (XLua.LuaTable)translator.GetObject(L, 2, typeof(XLua.LuaTable));
                translator.PushAny(L, ProtoSlua.EncodeFromSlua(msgID, data));
                return 1;
            }
            catch (Exception e)
            {
                return XLua.LuaDLL.Lua.luaL_error(L, e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// <para>将 Lua 的 table 编码成 proto msg .</para>
        /// <para>1.直接管理器转调，减少映射类</para>
        /// <para>2.手动导出Slua接口，需要进行栈操作</para>
        /// <para>返回值是纯数据buff，不包含 id 和 len</para>
        /// </summary>
        [XLua.MonoPInvokeCallbackAttribute(typeof(XLua.LuaDLL.lua_CSFunction))]
        static public int EncodeFromSluaRaw(IntPtr L)
        {
            // 1. 这儿留个空壳，确保Slua注册对应函数
            // 2. 详细实现参看 ProtoSlua 类
            try
            {
                var translator = XLua.ObjectTranslatorPool.Instance.Find(L);

                var msgID = XLua.LuaDLL.Lua.xlua_touint(L, 1);
                XLua.LuaTable data = (XLua.LuaTable)translator.GetObject(L, 2, typeof(XLua.LuaTable));
                translator.PushAny(L, ProtoSlua.EncodeFromSluaRaw(msgID, data));
                return 1;
            }
            catch (Exception e)
            {
                return XLua.LuaDLL.Lua.luaL_error(L, e.Message);
            }
        }
    }
}