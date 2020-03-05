/// <summary>
/// NetMsgData.cs
/// Created by wangxiangwei 2017-7-12
/// 网络消息数据体
/// </summary>

using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace TFW
{
    public struct NetMsgData
    {
        // 长度定义
        //客户端如果修改这个值需要同时修改this:MakeByte,NetStateObject:SplitPack和ProtoSlua:EncodeFromSlua
        public const int BuffLengthSize = 4; // 整体长度

        public const int MsgIDSize = 4; // ID长度

        public const int HeadSize = 4; // 消息头长度

        public uint MsgID; // 消息ID编号 2 byte
        public byte[] Buff; // 消息数据体

        /// <summary>
        /// 构造一个MsgData类型的消息.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="rawData">Raw data.</param>
        public static byte[] MakeByte(uint msgID, byte[] rawData)
        {
            MemoryStream ms = null;
            using (ms = new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(msgID);
                writer.Write(rawData);
                writer.Flush();
                return ms.ToArray();
            }
        }
        public static NetMsgData FromByte(byte[] bytes)
        {
            var bodyLen = bytes.Length - NetMsgData.MsgIDSize;
            var body = new byte[bodyLen];

            //解MsgID 4 字节
            var msgID = BitConverter.ToUInt32(bytes, 0);
            //解MsgBody
            Buffer.BlockCopy(bytes, MsgIDSize, body, 0, bodyLen);
            //当前buf指针加下Body偏移
            // offset += bodyLen;


            //创建逻辑消息
            NetMsgData newData;
            newData.MsgID = msgID;
            newData.Buff = body;
            return newData;
        }

    }
}