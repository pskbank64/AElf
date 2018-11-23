﻿namespace AElf.Network.Connection
{
    public class PartialPacket
    {
        public int Type { get; set; }
        public int Position { get; set; }
        public bool IsEnd { get; set; }
        public int TotalDataSize { get; set; }
        public bool IsCompress { get; set; }
        
        public bool HasId { get; set; }
        public byte[] Id { get; set; }
        
        public byte[] Data { get; set; }
    }
}