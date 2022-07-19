﻿using System;
using System.Threading;
using BattlefieldRichPresence.ChangePrensence;
using BattlefieldRichPresence.GameReader;
using BattlefieldRichPresence.Resources;
using BattlefieldRichPresence.Structs;
using DiscordRPC;

namespace BattlefieldRichPresence
{
    internal class DiscordPresence
    {
        private readonly Config _config;
        private DiscordRpcClient _client;
        private bool _discordIsRunning;
        private DateTime _startTime;

        public DiscordPresence()
        {
            _config = new Config();
        }

        private void StartStopDiscord(GameInfo gameInfo)
        {
            if (gameInfo.IsRunning && !_discordIsRunning)
            {
                _client = new DiscordRpcClient("993783880777744524");
                _client.Initialize();
                _discordIsRunning = true;
                _startTime = DateTime.UtcNow.AddSeconds(1);
            }
            else if (!gameInfo.IsRunning && _discordIsRunning)
            {
                _client.Dispose();
                _discordIsRunning = false;
            }
        }

        private void UpdatePresenceInMenu(GameInfo gameInfo)
        {
            if (gameInfo.IsRunning && _discordIsRunning)
            {
                _client.SetPresence(new RichPresence
                {
                    Details = "In the menus",
                    State = "",
                    Timestamps = new Timestamps
                    {
                        Start = _startTime
                    },
                    Assets = new Assets
                    {
                        LargeImageKey = gameInfo.ShortName,
                        LargeImageText = gameInfo.GameName,
                        SmallImageKey = gameInfo.ShortName
                    },

                });
            }
        }

        private void UpdatePresenceStatusUnknown(GameInfo gameInfo, string reason)
        {
            if (gameInfo.IsRunning && _discordIsRunning)
            {
                _client.SetPresence(new RichPresence
                {
                    Details = "Status unknown",
                    State = reason,
                    Timestamps = new Timestamps
                    {
                        Start = _startTime
                    },
                    Assets = new Assets
                    {
                        LargeImageKey = gameInfo.ShortName,
                        LargeImageText = gameInfo.GameName,
                        SmallImageKey = gameInfo.ShortName
                    }
                });
            }
        }

        private void UpdatePresence(GameInfo gameInfo, ServerInfo serverInfo)
        {
            if (_discordIsRunning)
            {
                try
                {
                    if (Statics.Frostbite3Games.Contains(gameInfo.ShortName))
                    {
                        Frostbite3.Update(_client, _startTime, gameInfo, serverInfo);
                    } else
                    {
                        OlderTitles.Update(_client, _startTime, gameInfo, serverInfo);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public void Main()
        {
            while (true)
            {
                GameInfo gameInfo = Game.IsRunning();
                StartStopDiscord(gameInfo);
                if (gameInfo.ShortName == "bf1")
                {
                    CurrentServerReader currentServerReader = new CurrentServerReader();
                    if (currentServerReader.HasResults)
                    {
                        if (currentServerReader.PlayerListsAll.Count > 0 && currentServerReader.ServerName != "")
                        {
                            ServerInfo serverInfo = new ServerInfo
                            {
                                Name = currentServerReader.ServerName,
                                NumPlayers = currentServerReader.PlayerListsAll.Count,
                                MaxPlayers = 0,
                                Ip = "",
                                Port = 0
                            };
                            UpdatePresence(gameInfo, serverInfo);
                        }
                        else
                        {
                            UpdatePresenceInMenu(gameInfo);
                        }
                    }
                } else if (gameInfo.IsRunning && _config.PlayerName != "")
                {
                    try
                    {
                        ServerInfo serverInfo = Api.OldTitleServerInfo(_config, gameInfo.ShortName);
                        UpdatePresence(gameInfo, serverInfo);
                    }
                    catch (Exception)
                    {
                        UpdatePresenceInMenu(gameInfo);  
                    }
                } else if (gameInfo.IsRunning)
                {
                    UpdatePresenceStatusUnknown(gameInfo, "Playername not configured");
                }

                Thread.Sleep(10000);
                _config.Refresh();
            }
        }
    }
}
