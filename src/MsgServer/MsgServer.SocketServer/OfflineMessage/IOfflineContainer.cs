using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgServer.SocketServer.OfflineMessage
{
    public interface IOfflineContainer:IDisposable
    {
        /// <summary>
        /// 获取消息
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        OfflineMessageArgs[] Get(string userID);
        /// <summary>
        /// 取出消息（移除）
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        OfflineMessageArgs[] TakeOut(string userID);
        /// <summary>
        /// 获取全部有效消息
        /// </summary>
        /// <returns></returns>
        OfflineMessageArgs[] GetAllLiving();
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="arg"></param>
        void Add(OfflineMessageArgs arg);
        /// <summary>
        /// 删除用户所有消息
        /// </summary>
        /// <param name="userID"></param>
        void Remove(string userID);
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ID"></param>
        void RemoveMessage(string ID);
        /// <summary>
        /// 清除无效消息
        /// </summary>
        /// <returns></returns>
        int ClearDied();
        /// <summary>
        /// 消息默认有效时间（小时）
        /// </summary>
        int DefaultLiveHour { get; set; }
    }

    /// <summary>
    /// 离线消息参数
    /// </summary>
    public class OfflineMessageArgs
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
        public int LiveHours { get; set; }
    }
}
