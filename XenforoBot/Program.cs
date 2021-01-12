using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using xNet;

namespace XenforoBot
{
    class Program
    {
        private readonly DiscordSocketClient _client;

        public static SettingsJSON settings;

        static void Main(string[] args)
        {
            if (!File.Exists("settings.json"))
                File.Create("settings.json").Close();
            settings = JsonConvert.DeserializeObject<SettingsJSON>(File.ReadAllText("settings.json"));
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _client = new DiscordSocketClient();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.UserJoined += AnnounceJoinedUser;
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, settings.dsToken);
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Channel is SocketDMChannel)
            {
                if (message.Content.Split(' ').Length == 2)
                {
                    settings = JsonConvert.DeserializeObject<SettingsJSON>(File.ReadAllText("settings.json"));
                    using (var request = new xNet.HttpRequest())
                    {
                        try
                        {
                            string[] data = message.Content.Split(' ');
                            request.UserAgent = Http.ChromeUserAgent();
                            request.Cookies = new CookieDictionary();
                            request.AddHeader("XF-Api-Key", settings.xfToken);
                            var response = request.Post(settings.xfUri + "/api/auth", "login=" + data[0] + "&password=" + data[1], "application/x-www-form-urlencoded").ToString();
                            XenforoJSON response_data = JsonConvert.DeserializeObject<XenforoJSON>(response);

                            if (response.Contains("success") && response_data != null && response_data.success != null && response_data.User != null)
                            {
                                int max_group = response_data.User.user_group_id;
                                foreach (int group in (int[])response_data.User.secondary_group_ids)
                                {
                                    int place = Array.IndexOf(settings.groupsHierarchy, group);
                                    max_group = max_group > place ? max_group : place;
                                }
                                var user = message.Author as SocketGuildUser;
                                SocketRole role = user.Guild.GetRole(settings.groups[max_group].dsRoleId);
                                // TODO: можете также работать с данными полученного класса, например, для выдачи роли стаффа или добавления в какую-нибудь рассылку
                                await user.AddRoleAsync(role);
                                await message.Author.SendMessageAsync("Успешно");
                            }
                            else
                                await message.Author.SendMessageAsync("Неверный логин и/или пароль");
                        }
                        catch (Exception ex)
                        {
                            await message.Author.SendMessageAsync("Во время авторизации возникла ошибка: " + ex.Message);
                        }
                    }
                }
            }
            else 
                if (message.Content == "!auth")
                    await message.Author.SendMessageAsync("Для авторизации в системе и получения соответствущей вам роли, отправьте в текущий диалог свой логин и пароль от форума MYPASTE.COM в формате log{пробел}pass");
        }

        private async Task AnnounceJoinedUser(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook) return;
            await user.SendMessageAsync("Добро пожаловать на официальный сервер проекта MYPASTE.COM\nДля авторизации в системе и получения соответствущей вам роли, отправьте в текущий диалог свой логин и пароль от форума MYPASTE.COM в формате log{пробел}pass");
        }
    }
}
