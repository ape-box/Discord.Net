﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MessageModel = Discord.API.Message;
using Model = Discord.API.Channel;

namespace Discord
{
    internal class CachedDMChannel : DMChannel, IDMChannel, ICachedChannel, ICachedMessageChannel
    {
        private readonly MessageCache _messages;

        public new DiscordSocketClient Discord => base.Discord as DiscordSocketClient;
        public new CachedPublicUser Recipient => base.Recipient as CachedPublicUser;
        public IReadOnlyCollection<IUser> Members => ImmutableArray.Create<IUser>(Discord.CurrentUser, Recipient);

        public CachedDMChannel(DiscordSocketClient discord, CachedPublicUser recipient, Model model)
            : base(discord, recipient, model)
        {
            _messages = new MessageCache(Discord, this);
        }

        public override Task<IUser> GetUser(ulong id) => Task.FromResult(GetCachedUser(id));
        public override Task<IReadOnlyCollection<IUser>> GetUsers() => Task.FromResult(Members);
        public override Task<IReadOnlyCollection<IUser>> GetUsers(int limit, int offset) 
            => Task.FromResult<IReadOnlyCollection<IUser>>(Members.Skip(offset).Take(limit).ToImmutableArray());
        public IUser GetCachedUser(ulong id)
        {
            var currentUser = Discord.CurrentUser;
            if (id == Recipient.Id)
                return Recipient;
            else if (id == currentUser.Id)
                return currentUser;
            else
                return null;
        }

        public override async Task<IMessage> GetMessage(ulong id)
        {
            return await _messages.Download(id).ConfigureAwait(false);
        }
        public override async Task<IReadOnlyCollection<IMessage>> GetMessages(int limit)
        {
            return await _messages.Download(null, Direction.Before, limit).ConfigureAwait(false);
        }
        public override async Task<IReadOnlyCollection<IMessage>> GetMessages(ulong fromMessageId, Direction dir, int limit)
        {
            return await _messages.Download(fromMessageId, dir, limit).ConfigureAwait(false);
        }
        public CachedMessage AddCachedMessage(IUser author, MessageModel model)
        {
            var msg = new CachedMessage(this, author, model);
            _messages.Add(msg);
            return msg;
        }
        public CachedMessage GetCachedMessage(ulong id)
        {
            return _messages.Get(id);
        }
        public CachedMessage RemoveCachedMessage(ulong id)
        {
            return _messages.Remove(id);
        }

        public CachedDMChannel Clone() => MemberwiseClone() as CachedDMChannel;

        IMessage IMessageChannel.GetCachedMessage(ulong id) => GetCachedMessage(id);
        ICachedChannel ICachedChannel.Clone() => Clone();
    }
}