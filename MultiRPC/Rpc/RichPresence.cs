﻿using System;
using System.Text.Json.Serialization;
using Fonderie;

namespace MultiRPC.Rpc
{
    public partial class RichPresence
    {
        public RichPresence(string name, long id)
        {
            _name = name;
            ID = id;
        }

        [GeneratedProperty]
        private string _name;

        public long ID { get; set; }

        [JsonIgnore]
        public DiscordRPC.RichPresence Presence => Profile.ToRichPresence();
        
        public RpcProfile Profile { get; set; } = new RpcProfile();

        [JsonIgnore]
        public Uri? CustomLargeImageUrl { get; set; }

        [JsonIgnore]
        public Uri? CustomSmallImageUrl { get; set; }

        [GeneratedProperty] private bool _useTimestamp;

        //TODO: Remake to use proper testing
        [JsonIgnore]
        public bool IsValidPresence =>
            Presence.Details?.Length != 1
            && Presence.State?.Length != 1
            && Presence.Assets?.LargeImageText?.Length != 1
            && Presence.Assets?.SmallImageText?.Length != 1;
    }
}
