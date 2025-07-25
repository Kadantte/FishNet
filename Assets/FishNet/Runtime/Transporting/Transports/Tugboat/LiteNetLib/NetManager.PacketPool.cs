﻿using System;

namespace LiteNetLib
{
    public partial class NetManager
    {
        private NetPacket _poolHead;
        private readonly object _poolLock = new();
        /// <summary>
        /// Maximum packet pool size (increase if you have tons of packets sending)
        /// </summary>
        public int PacketPoolSize = 1000;
        public int PoolCount { get; private set; }

        private NetPacket PoolGetWithData(PacketProperty property, byte[] data, int start, int length)
        {
            int headerSize = NetPacket.GetHeaderSize(property);
            NetPacket packet = PoolGetPacket(length + headerSize);
            packet.Property = property;
            Buffer.BlockCopy(data, start, packet.RawData, headerSize, length);
            return packet;
        }

        // Get packet with size
        private NetPacket PoolGetWithProperty(PacketProperty property, int size)
        {
            NetPacket packet = PoolGetPacket(size + NetPacket.GetHeaderSize(property));
            packet.Property = property;
            return packet;
        }

        private NetPacket PoolGetWithProperty(PacketProperty property)
        {
            NetPacket packet = PoolGetPacket(NetPacket.GetHeaderSize(property));
            packet.Property = property;
            return packet;
        }

        internal NetPacket PoolGetPacket(int size)
        {
            if (size > NetConstants.MaxPacketSize)
                return new(size);

            NetPacket packet;
            lock (_poolLock)
            {
                packet = _poolHead;
                if (packet == null)
                    return new(size);

                _poolHead = _poolHead.Next;
                PoolCount--;
            }

            packet.Size = size;
            if (packet.RawData.Length < size)
                packet.RawData = new byte[size];
            return packet;
        }

        internal void PoolRecycle(NetPacket packet)
        {
            if (packet.RawData.Length > NetConstants.MaxPacketSize || PoolCount >= PacketPoolSize)
            {
                // Don't pool big packets. Save memory
                return;
            }

            // Clean fragmented flag
            packet.RawData[0] = 0;
            lock (_poolLock)
            {
                packet.Next = _poolHead;
                _poolHead = packet;
                PoolCount++;
            }
        }
    }
}