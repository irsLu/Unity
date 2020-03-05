/// <summary>
/// ProtoMgr.cs
/// Created by wangxiangwei 2017-7-25
/// proto解析管理器
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TFW
{
    public interface IProto
    {
        void Init(string folder, string msgid, List<string> protos);
        Dictionary<string, uint> GetMsgidDef();
        Dictionary<string, Dictionary<string, uint>> GetEnumDef();
        void SetDecodeMT(bool isOutputMT);
    }
    public abstract class IProtoMgr : IProto
    {
        public static IProtoMgr Mgr { get; set; }
        protected abstract Dictionary<string, Dictionary<string, uint>> GetEnumDef();
        protected abstract Dictionary<string, uint> GetMsgidDef();
        protected abstract void Init(string folder, string msgid, List<string> protos);
        protected abstract void SetDecodeMT(bool isOutputMT);

        Dictionary<string, Dictionary<string, uint>> IProto.GetEnumDef()
        {
            return GetEnumDef();
        }

        Dictionary<string, uint> IProto.GetMsgidDef()
        {
            return GetMsgidDef();
        }

        void IProto.Init(string folder, string msgid, List<string> protos)
        {
            Init(folder, msgid, protos);
        }

        void IProto.SetDecodeMT(bool isOutputMT)
        {
            SetDecodeMT(isOutputMT);
        }
    }
    public static class ProtoMgr
    {
        static IProto Mgr
        {
            get
            {
                return IProtoMgr.Mgr as IProto;
            }
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init(string folder, string msgid, List<string> protos)
        {
            Mgr?.Init(folder,msgid,protos);
        }

        /// <summary>
        /// Gets the def.
        /// </summary>
        /// <returns>The def.</returns>
        public static Dictionary<string, uint> GetMsgidDef()
        {
            return Mgr?.GetMsgidDef();
        }

        /// <summary>
        /// Gets the enum def.
        /// </summary>
        public static Dictionary<string, Dictionary<string, uint>> GetEnumDef()
        {
            return Mgr?.GetEnumDef();
        }

        /// <summary>
        /// 解码操作中，是否将消息的 proto 类型名，写入到 table._mt 中.
        /// </summary>
        /// <param name="isOutputMT">If set to <c>true</c> is output M.</param>
        public static void SetDecodeMT(bool isOutputMT)
        {
            Mgr?.SetDecodeMT(isOutputMT);
        }
        public static int DecodeToSlua(IntPtr L) { return 0; }
        public static int EncodeFromSlua(IntPtr L) { return 0; }
        public static int EncodeFromSluaRaw(IntPtr L) { return 0; }
    }
}