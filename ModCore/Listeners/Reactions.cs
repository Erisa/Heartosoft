﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using ModCore.Database;
using ModCore.Entities;
using ModCore.Logic;
using ModCore.Logic.Extensions;

namespace ModCore.Listeners
{
    public class Reactions
    {
        static DiscordMessage message;

        [AsyncListener(EventTypes.MessageReactionAdded)]
        public static async Task ReactionAdd(ModCoreShard bot, MessageReactionAddEventArgs e)
        {
            GuildSettings cfg = null;
            using (var db = bot.Database.CreateContext())
            {
                cfg = e.Channel.Guild.GetGuildSettings(db);
                if (cfg == null)
                    return;

                if (cfg.ReactionRoles.Any(x => (ulong)x.ChannelId == e.Channel.Id && (ulong)x.MessageId == message.Id && (ulong)x.Reaction.EmojiId == e.Emoji.Id && x.Reaction.EmojiName == e.Emoji.Name))
                {
                    var rrid = (ulong)cfg.ReactionRoles.First(
                        x => (ulong)x.ChannelId == e.Channel.Id && (ulong)x.MessageId == message.Id && (ulong)x.Reaction.EmojiId == e.Emoji.Id && x.Reaction.EmojiName == e.Emoji.Name).RoleId;
                    var rrrole = e.Channel.Guild.GetRole(rrid);
                    var mem = await e.Channel.Guild.GetMemberAsync(e.User.Id);
                    if (!mem.Roles.Any(x => x.Id == rrid))
                        await mem.GrantRoleAsync(rrrole);
                }

                var emoji = cfg.Starboard.Emoji;
                DiscordEmoji em = null;
                if (emoji.EmojiId != 0)
                    em = DiscordEmoji.FromGuildEmote(e.Client, (ulong)emoji.EmojiId);
                else
                    em = DiscordEmoji.FromUnicode(e.Client, emoji.EmojiName);

                if (!cfg.Starboard.AllowNSFW && e.Channel.IsNSFW)
                    return;

                if (cfg.Starboard.Enable && e.Emoji == em)
                {
                    long sbmid = 0;
                    var c = e.Channel.Guild.Channels.First(x => x.Key == (ulong)cfg.Starboard.ChannelId).Value;
                    if (c.Id == e.Channel.Id) // star on starboard entry
                    {
                        /*if (db.StarDatas.Any(x => (ulong)x.StarboardMessageId == message.Id))
                        {
                            var other = db.StarDatas.First(x => (ulong)x.StarboardMessageId == message.Id);
                            var count = db.StarDatas.Count(x => (ulong)x.StarboardMessageId == message.Id);
                            if (!db.StarDatas.Any(x => x.MessageId == other.MessageId && x.StargazerId == (long)e.User.Id))
                            {
                                var chn = await e.Client.GetChannelAsync((ulong)other.ChannelId);
                                var msg = await chn.GetMessageAsync((ulong)other.MessageId);

                                if (msg.Author.Id == e.User.Id || e.User.IsBot)
                                    return;

                                var d = await (await c.GetMessageAsync((ulong)other.StarboardMessageId)).ModifyAsync($"{e.Emoji}: {count + 1} ({msg.Id}) in {msg.Channel.Mention}", embed: BuildMessageEmbed(msg));
                                sbmid = (long)d.Id;
                                await db.StarDatas.AddAsync(new DatabaseStarData
                                {
                                    ChannelId = other.ChannelId,
                                    GuildId = (long)e.Channel.Guild.Id,
                                    MessageId = other.MessageId,
                                    AuthorId = other.AuthorId,
                                    StarboardMessageId = sbmid,
                                    StargazerId = (long)e.User.Id,
                                });
                                await db.SaveChangesAsync();
                            }
                        }*/

                        // Removing this behaviour for jump links.
                    }
                    else // star on actual message
                    {
                        message = await e.Channel.GetMessageAsync(e.Message.Id);


                        if (message.Author.Id == e.User.Id)
                        {
                            await message.DeleteReactionAsync(e.Emoji, message.Author);
                            return;
                        }


                        if (db.StarDatas.Any(x => (ulong)x.MessageId == message.Id))
                        {
                            var count = db.StarDatas.Count(x => (ulong)x.MessageId == message.Id);

                            if (db.StarDatas.Any(x => (ulong)x.MessageId == message.Id && x.StarboardMessageId != 0))
                            {
                                var other = db.StarDatas.First(x => (ulong)x.MessageId == message.Id && x.StarboardMessageId != 0);
                                var msg = await c.GetMessageAsync((ulong)other.StarboardMessageId);

                                var d = await msg.ModifyAsync($"{e.Emoji}: {count + 1} ({message.Id}) in {message.Channel.Mention}", embed: BuildMessageEmbed(message));
                                sbmid = (long)d.Id;
                            }
                            else
                            {
                                if (count + 1 >= cfg.Starboard.Minimum)
                                {
                                    // create msg
                                    var d = await c.ElevatedMessageAsync($"{e.Emoji}: {count + 1} ({message.Id}) in {message.Channel.Mention}", embed: BuildMessageEmbed(message));
                                    sbmid = (long)d.Id;
                                }
                            }
                        }
                        else if (cfg.Starboard.Minimum <= 1)
                        {
                            var d = await c.ElevatedMessageAsync($"{e.Emoji}: 1 ({message.Id}) in {message.Channel.Mention}", embed: BuildMessageEmbed(message));
                            sbmid = (long)d.Id;
                        }

                        await db.StarDatas.AddAsync(new DatabaseStarData
                        {
                            ChannelId = (long)e.Channel.Id,
                            GuildId = (long)e.Channel.Guild.Id,
                            MessageId = (long)message.Id,
                            AuthorId = (long)message.Author.Id,
                            StarboardMessageId = sbmid,
                            StargazerId = (long)e.User.Id,
                        });

                        // somebody once told me...
                        var allstars = db.StarDatas.Where(x => (ulong)x.MessageId == message.Id).ToList();
                        allstars.ForEach(x => x.StarboardMessageId = sbmid);
                        db.StarDatas.UpdateRange(allstars);

                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        [AsyncListener(EventTypes.MessageReactionRemoved)]
        public static async Task ReactionRemove(ModCoreShard bot, MessageReactionRemoveEventArgs e)
        {
            GuildSettings cfg = null;
            using (var db = bot.Database.CreateContext())
            {
                cfg = e.Channel.Guild.GetGuildSettings(db);
                if (cfg == null)
                    return;

                if (cfg.ReactionRoles.Any(x => (ulong)x.ChannelId == e.Channel.Id && (ulong)x.MessageId == message.Id && (ulong)x.Reaction.EmojiId == e.Emoji.Id && x.Reaction.EmojiName == e.Emoji.Name))
                {
                    var rrid = (ulong)cfg.ReactionRoles.First(
                        x => (ulong)x.ChannelId == e.Channel.Id && (ulong)x.MessageId == message.Id && (ulong)x.Reaction.EmojiId == e.Emoji.Id && x.Reaction.EmojiName == e.Emoji.Name).RoleId;
                    var rrrole = e.Channel.Guild.GetRole(rrid);
                    var mem = await e.Channel.Guild.GetMemberAsync(e.User.Id);
                    if (mem.Roles.Any(x => x.Id == rrid))
                        await mem.RevokeRoleAsync(rrrole);
                }

                var emoji = cfg.Starboard.Emoji;
                DiscordEmoji em = null;
                if (emoji.EmojiId != 0)
                    em = DiscordEmoji.FromGuildEmote(e.Client, (ulong)emoji.EmojiId);
                else
                    em = DiscordEmoji.FromUnicode(e.Client, emoji.EmojiName);

                if (cfg.Starboard.Enable && e.Emoji == em)
                {
                    var c = e.Channel.Guild.Channels.First(x => x.Key == (ulong)cfg.Starboard.ChannelId).Value;
                    if (c.Id == e.Channel.Id)
                    {
                        /*if (db.StarDatas.Any(x => (ulong)x.StarboardMessageId == message.Id && (ulong)x.StargazerId == e.User.Id))
                        {
                            var star = db.StarDatas.First(x => (ulong)x.StarboardMessageId == message.Id && (ulong)x.StargazerId == e.User.Id);
                            var count = db.StarDatas.Count(x => (ulong)x.StarboardMessageId == message.Id);
                            var m = await c.GetMessageAsync((ulong)star.StarboardMessageId);
                            var chn = await e.Client.GetChannelAsync((ulong)star.ChannelId);
                            var msg = await chn.GetMessageAsync((ulong)star.MessageId);
                            if (count - 1 >= cfg.Starboard.Minimum)
                                await m.ModifyAsync($"{e.Emoji}: {count - 1} ({msg.Id}) in {msg.Channel.Mention}", embed: BuildMessageEmbed(msg));
                            else
                                await m.DeleteAsync();
                            db.StarDatas.Remove(star);
                            await db.SaveChangesAsync();
                        }*/
                        // Removing behaviour due to jump links
                    }
                    else
                    {
                        long nsbid = 0;
                        if (db.StarDatas.Any(x => (ulong)x.MessageId == message.Id && (ulong)x.StargazerId == e.User.Id && x.StarboardMessageId != 0))
                        {
                            var star = db.StarDatas.First(x => (ulong)x.MessageId == message.Id && (ulong)x.StargazerId == e.User.Id);
                            var count = db.StarDatas.Count(x => (ulong)x.MessageId == message.Id);

                            if (db.StarDatas.Any(x => (ulong)x.MessageId == message.Id && (ulong)x.StargazerId == e.User.Id && x.StarboardMessageId != 0))
                            {
                                var mid = db.StarDatas.First(x => (ulong)x.MessageId == message.Id && (ulong)x.StargazerId == e.User.Id && x.StarboardMessageId != 0)
                                    .StarboardMessageId;

                                var m = await c.GetMessageAsync((ulong)mid);
                                if (count - 1 >= cfg.Starboard.Minimum)
                                {
                                    await m.ModifyAsync($"{e.Emoji}: {count - 1} ({message.Id}) in {message.Channel.Mention}", embed: BuildMessageEmbed(message));
                                    nsbid = mid;
                                }
                                else
                                {
                                    await m.DeleteAsync();
                                }
                            }

                            db.StarDatas.Remove(star);
                            await db.SaveChangesAsync();

                            // somebody once told me...
                            var allstars = db.StarDatas.Where(x => (ulong)x.MessageId == message.Id).ToList();
                            allstars.ForEach(x => x.StarboardMessageId = nsbid);
                            db.StarDatas.UpdateRange(allstars);

                            await db.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        [AsyncListener(EventTypes.MessageReactionsCleared)]
        public static async Task ReactionClear(ModCoreShard bot, MessageReactionsClearEventArgs e)
        {
            GuildSettings cfg = null;
            using (var db = bot.Database.CreateContext())
            {
                cfg = e.Channel.Guild.GetGuildSettings(db);
                if (cfg == null)
                    return;

                var emoji = cfg.Starboard.Emoji;
                DiscordEmoji em = null;
                if (emoji.EmojiId != 0)
                    em = DiscordEmoji.FromGuildEmote(e.Client, (ulong)emoji.EmojiId);
                else
                    em = DiscordEmoji.FromUnicode(e.Client, emoji.EmojiName);

                if (cfg.Starboard.Enable)
                {
                    var c = e.Channel.Guild.Channels.First(x => x.Key == (ulong)cfg.Starboard.ChannelId).Value;
                    if (db.StarDatas.Any(x => (ulong)x.MessageId == message.Id))
                    {
                        await (await c.GetMessageAsync((ulong)db.StarDatas.First(x => (ulong)x.MessageId == message.Id).StarboardMessageId)).DeleteAsync();
                        db.StarDatas.RemoveRange(db.StarDatas.Where(x => (ulong)x.MessageId == message.Id));
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        public static DiscordEmbed BuildMessageEmbed(DiscordMessage m)
        {
            var e = new DiscordEmbedBuilder()
                .WithAuthor($"{m.Author.Username}#{m.Author.Discriminator}",
                iconUrl: (string.IsNullOrEmpty(m.Author.AvatarHash) ? m.Author.DefaultAvatarUrl : m.Author.AvatarUrl))
                .WithDescription(m.Content.Truncate(1000) + $"\n\n[Jump to message]({m.JumpLink})");

            // This is shit code kek
            if (m.Attachments.Any(x => x.Url.ToLower().EndsWith(".jpg") || x.Url.ToLower().EndsWith(".png")
             || x.Url.ToLower().EndsWith(".jpeg") || x.Url.ToLower().EndsWith(".gif")))
                return e.WithImageUrl(m.Attachments.First(x => x.Url.ToLower().EndsWith(".jpg") || x.Url.ToLower().EndsWith(".png")
            || x.Url.ToLower().EndsWith(".jpeg") || x.Url.ToLower().EndsWith(".gif")).Url).Build();

            return e.Build();
        }
    }
}
