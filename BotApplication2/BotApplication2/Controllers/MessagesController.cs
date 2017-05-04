using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System.Text;
using System.IO;
using System.Collections;

namespace BotApplication2
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        static bool ifWelcome = true;
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (ifWelcome)
            {
                ifWelcome =false;
                await Conversation.SendAsync(activity, () => new WelcomeDialog());    
            }
            else if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new SurevilDialog());
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        [Serializable]
        public class WelcomeDialog : LuisDialog<object>
        {
            override public async Task StartAsync(IDialogContext context)
            {
                context.Wait(WelcomeAsync);
            }

            public async Task WelcomeAsync(IDialogContext context, IAwaitable<object> result)
            {
                string message = "呀~你好，我叫surevil，我应该全知全能的，可是现在还只能查音乐，以及告诉你我主人的联系方式（委屈巴巴），希望你是个妹子……";
                await context.PostAsync(message);
            }
        }

        [LuisModel("5a4c078a-e90d-49b3-81cc-aba022b38b99", "a811cfdd46a24275945d8e563c9cd047")]
        [Serializable]
        public class SurevilDialog : LuisDialog<object>
        {
            /*static int count = 0;
            public async Task ifSendWelcome(IDialogContext context)
            {
                if (count == 0)
                {
                    await WelcomeAsync(context);
                    count++;
                }
            }

            public async Task WelcomeAsync(IDialogContext context)
            {
                string message = "呀~你好，我叫surevil，我应该全知全能的，可是现在还只能查音乐，以及告诉你我主人的联系方式（委屈巴巴），希望你是个妹子……";
                await context.PostAsync(message);
            }*/

            public SurevilDialog()
            {
            }
            public SurevilDialog(ILuisService service)
             : base(service)
            {
            }

            [LuisIntent("询问机器人的主人")]
            public async Task TellMaster(IDialogContext context,LuisResult result)
            {
                // ifSendWelcome(context);

                string message = "张凌哲，联系方式：<br>QQ：445073309<br>Phone：18851822162<br>欢迎各路妹子来联系我（斜眼笑）";
                await context.PostAsync(message);
                context.Wait(MessageReceived);
            }

            [LuisIntent("")]
            public async Task None(IDialogContext context,LuisResult result)
            {
                //await ifSendWelcome(context);

                string message = "我还只能帮你找歌词，别的是不存在的！";
                await context.PostAsync(message);
                context.Wait(MessageReceived);
            }

            [LuisIntent("查询歌词")]
            public async Task SearchSong(IDialogContext context,LuisResult result)
            {
                //await ifSendWelcome(context);

                string song = "";
                string name = "";

                if(CanGetSong(result,out song)&&CanGetSinger(result,out name))
                {
                    string word =GetWord(song,name);
                    if (word.Equals(""))
                    {
                        word = "找不到" + name + "的" + song + "哟~";
                    }
                    await context.PostAsync(word);
                    context.Wait(MessageReceived);
                }
                else if ((CanGetSong(result,out song)==true)&&(CanGetSinger(result,out name)==false))
                {
                    string reply = "请一起输入歌手哟~";
                    await context.PostAsync(reply);
                    context.Wait(MessageReceived);
                }
                else
                {
                    string reply = "请一起输入歌名哟~";
                    await context.PostAsync(reply);
                    context.Wait(MessageReceived);
                }
            }

            [LuisIntent("随便拿一首该歌星的歌")]
            public async Task SingerSong(IDialogContext context,LuisResult result)
            {
                //await ifSendWelcome(context);

                string singer = "";
                string reply = "";
                if (CanGetSinger(result,out singer))
                {
                    reply = GetSong(singer);
                    if (reply.Equals(""))
                    {
                        reply = "找不到" + singer + "的歌哟~";
                    }
                }
                else
                {
                    reply = "请输入歌手哟";
                }
                await context.PostAsync(reply);
                context.Wait(MessageReceived);
            }

            [LuisIntent("查找哪首歌里有哪个字")]
            public async Task FindWord(IDialogContext context, LuisResult result)
            {
                //await ifSendWelcome(context);

                string singer = "";
                string word = "";
                string reply = "";
                if (CanGetSinger(result, out singer) && CanGetWord(result, out word))
                {
                    reply = GetSongOfWord(singer, word);
                    if (reply.Equals(""))
                    {
                        reply = singer + "的歌里竟然没有这个字！";
                    }
                }
                else
                {
                    reply = "请输入歌手和查找的关键字哟";
                }
                await context.PostAsync(reply);
                context.Wait(MessageReceived);
            }



            public string GetSongOfWord(string singer,string word)
            {
                string url = "http://sou.kuwo.cn/ws/NSearch?type=all&catalog=yueku2016&key=" + GetEncode(singer);
                StreamReader streamReader = new StreamReader(GetResponse(url).GetResponseStream());

                //得到歌曲列表
                ArrayList songArray = new ArrayList();
                string r1;
                while ((r1 = streamReader.ReadLine()) != null)
                {
                    if ((r1.Contains("http://www.kuwo.cn/yinyue/")) && (!r1.Contains("{")))
                    {
                        songArray.Add(r1);
                    }
                }

                foreach(string song in songArray)
                {
                    string newUrl = song.Split('"')[1];
                    StreamReader newStreamReader = new StreamReader(GetResponse(newUrl).GetResponseStream());
                    string r2;
                    string result="";
                    while ((r2 = newStreamReader.ReadLine()) != null)
                    {
                        if ((r2.Contains("lrcItem")) && (r2.Contains("data-time")) && (!r2.Contains("{")) && (r2.Contains(word)))
                        {
                            result = song.Split('"')[3] + "<br>";
                            result = result + (r2.Split('>')[1]).Split('<')[0] + "<br>";
                            return result;
                        }
                    }
                }
                return "";
            }

            public string GetSong(string singer)
            {
                string url = "http://sou.kuwo.cn/ws/NSearch?type=all&catalog=yueku2016&key=" + GetEncode(singer);
                StreamReader streamReader = new StreamReader(GetResponse(url).GetResponseStream());

                //得到歌曲列表
                ArrayList songArray = new ArrayList();
                string r1;
                while ((r1 = streamReader.ReadLine()) != null)
                {
                    if ((r1.Contains("http://www.kuwo.cn/yinyue/")) && (!r1.Contains("{")))
                    {
                        songArray.Add(r1);
                    }
                }

                //随机选一首
                string choosed = (string) songArray[(new Random().Next(songArray.Count))];

                //获得其歌词
                string newUrl = choosed.Split('"')[1];
                StreamReader newStreamReader = new StreamReader(GetResponse(newUrl).GetResponseStream());
                string r2;
                string result = choosed.Split('"')[3]+"<br>";
                while ((r2 = newStreamReader.ReadLine()) != null)
                {
                    
                    if ((r2.Contains("lrcItem")) && (r2.Contains("data-time")) && (!r2.Contains("{")))
                    {
                        result = result + (r2.Split('>')[1]).Split('<')[0] + "<br>";
                    }
                }
                return result;
            }

            //获取歌词
            public string GetWord(string song,string name)
            {
                string url= "http://sou.kuwo.cn/ws/NSearch?type=all&key="+name+"++"+song ;
                StreamReader streamReader = new StreamReader(GetResponse(url).GetResponseStream());
                //string responseContent = streamReader.ReadToEnd();
                //StringBuilder sb = new StringBuilder();
                string r1;
                while ((r1 = streamReader.ReadLine()) != null)
                {
                    if((r1.Contains("http://www.kuwo.cn/yinyue/"))&&(!r1.Contains("{")))
                    {
                        break;
                    }
                }
               
                string newUrl=r1.Split('"')[1];
                StreamReader newStreamReader = new StreamReader(GetResponse(newUrl).GetResponseStream());
                string r2;
                string result="";
                while ((r2 = newStreamReader.ReadLine()) != null)
                {
                    if ((r2.Contains("lrcItem")) && (r2.Contains("data-time")) && (!r2.Contains("{")))
                    {
                        result = result + (r2.Split('>')[1]).Split('<')[0]+"\n";
                    }
                }
                return result;
            }

            //将文字转化为网页编码
            public static string GetEncode(string str)
            {
                byte[] bts = Encoding.UTF8.GetBytes(str);
                string r = "";
                for (int i = 0; i < bts.Length; i += 1)
                {
                    r += "%" + bts[i].ToString("x");
                }
                return r;
            }

            //得到网页的response
            public static HttpWebResponse GetResponse(string url)
            {
                string defaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = defaultUserAgent;
                return request.GetResponse() as HttpWebResponse;
            }

            public bool CanGetSong(LuisResult result,out string song)
            {
                song = "";
                EntityRecommendation title;
                if (result.TryFindEntity("音乐名字",out title))
                {
                    song = title.Entity;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool CanGetWord(LuisResult result, out string word)
            {
                word = "";
                EntityRecommendation title;
                if (result.TryFindEntity("随机字", out title))
                {
                    word = title.Entity;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool CanGetSinger(LuisResult result,out string name)
            {
                name = "";
                EntityRecommendation singer;
                if(result.TryFindEntity("歌手",out singer))
                {
                    name = singer.Entity;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}