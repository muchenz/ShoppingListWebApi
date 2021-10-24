using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorClient.Services
{
    public class UserInfoService
    {
        Dictionary<string, UserInfo> _userInfoDictionary = new Dictionary<string, UserInfo>();

        public UserInfo GetUserInfo(string id) => _userInfoDictionary.GetValueOrDefault(id) ?? new UserInfo();

        public UserInfo SetUserInfo(string id, UserInfo info) => _userInfoDictionary[id] = info;


        public void  SetUserInfo(string id, Action<UserInfo> func)
        {

            var userInfo = GetUserInfo(id);

            func(userInfo);

            SetUserInfo(id, userInfo);

        }

    }


    public class UserInfo
    {
         public string Token { get; set; }
         public string ClientSignalRID { get; set; }
    }
}
