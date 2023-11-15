using Microsoft.AspNetCore.SignalR;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace KingfoodIO.Common
{

    public interface IClock
    {
        Task ShowTime(DateTime currentTime);
        Task SendMessage(string name, string content);
        /// <summary>
        /// SignalR接收信息
        /// </summary>
        /// <param name="message">信息内容</param>
        /// <returns></returns>
        Task ReceiveMessage(object message);

        /// <summary>
        /// SignalR接收信息
        /// </summary>
        /// <param name="user">指定接收客户端</param>
        /// <param name="message">信息内容</param>
        /// <returns></returns>
        Task ReceiveMessage(string user, string message);

        Task ReceiveUpdate(object message);
    }
    public class ClockHub : Hub<IClock>
    {
        public async Task SendTimeToClients(DateTime dateTime)
        {
            await Clients.All.ShowTime(dateTime);
        }
        /// <summary>
        /// 向指定群组发送信息
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="message">信息内容</param>  
        /// <returns></returns>
        public async Task SendMessageToGroupAsync(string groupName, string message)
        {
            await Clients.Group(groupName).ReceiveMessage(message);
        }
        /// <summary>
        /// 向指定成员发送信息
        /// </summary>
        /// <param name="user">成员名</param>
        /// <param name="message">信息内容</param>
        /// <returns></returns>
        public async Task SendPrivateMessage(string user, string message)
        {
            await Clients.User(user).ReceiveMessage(message);
        }
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                //按用户分组
                //是有必要的 例如多个浏览器、多个标签页使用同个用户登录 应当归属于一组
                await AddToGroup(Context.User.Identity.Name);

                //加入角色组
                //根据角色分组 例如管理员分组发送管理员的消息
                var roles = Context.User.Claims.Where(s => s.Type == ClaimTypes.Role).ToList();
                foreach (var role in roles)
                {
                    await AddToGroup(role.Value);
                }
            }
        }
        /// <summary>
        /// 当链接断开时运行
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(System.Exception ex)
        {
            //TODO..
            return base.OnDisconnectedAsync(ex);
        }
        /// <summary>
        /// 加入指定组
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <returns></returns>
        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        /// <summary>
        /// 退出指定组
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <returns></returns>
        public async Task RemoveFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
        public async Task SendMessage(string name, string content)
        {
            await Clients.All.SendMessage(name, content);
        }
        //定于一个通讯管道，用来管理我们和客户端的连接
        //1、客户端调用 GetLatestCount，就像订阅
        public async Task GetLatestCount(string random)
        {
            //2、服务端主动向客户端发送数据，名字千万不能错
            //if (AppSettings.app(new string[] { "Middleware", "SignalRSendLog", "Enabled" }).ObjToBool())
            //{
            //    //TODO 主动发送错误消息
            //    await Clients.All.ReceiveUpdate(LogLock.GetLogData());
            //}


            //3、客户端再通过 ReceiveUpdate ，来接收
        }
    }
}
