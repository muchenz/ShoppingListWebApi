using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorClient.Services
{
    public class StateService
    {
        Dictionary<string, StateInfo> _userInfoDictionary = new Dictionary<string, StateInfo>();

        public StateInfo GetStateInfo(string id)
        {

            var value = _userInfoDictionary.GetValueOrDefault(id);
            if (value == null)
            {
                var newState = new StateInfo();

                _userInfoDictionary[id] = newState;

                return newState;
            }

            return value;

        }

        public StateInfo SetStateInfo(string id, StateInfo info) => _userInfoDictionary[id] = info;


        public void SetStateInfo(string id, Action<StateInfo> func)
        {

            var userInfo = GetStateInfo(id);

            func(userInfo);

            SetStateInfo(id, userInfo);

        }

    }


    public class StateInfo
    {
        public string Token { get; set; }
        public string ClientSignalRID { get; set; }

        public HubState HubState { get; set; } = new HubState();
       
    }

    public class HubState
    {

        event Action<HubConnection> HuBReady;
        event Func<HubConnection, Task> HuBReadyAsync;
        public HubConnection Hub { get; private set; }
        //public void CallHuBReady2(HubConnection _con)
        //{
        //    Hub = _con;
        //    HuBReady?.Invoke(_con);
        //    HuBReadyAsync?.Invoke(_con);
        //}

        public async Task CallHuBReadyAsync(HubConnection _con)
        {
            Hub = _con;

            foreach (var handler in HuBReadyAsync?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                if (handler is Func<HubConnection, Task> callback)
                {
                    try
                    {
                        await callback(_con);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"error in signar registration: {ex.Message}");
                    }

                    HuBReadyAsync -= callback;
                }
            }


            foreach (var handler in HuBReady?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                if (handler is Action<HubConnection> callback)
                {
                    try
                    {
                        callback(_con);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"error in signar registration: {ex.Message}");

                    }

                    HuBReady -= callback;
                }
            }
        }

        public void JoinToHub(Action<HubConnection> func)
        {

            if (Hub == null)
            {
                HuBReady += func;
            }
            else
                func(Hub);
        }

        public void JoinToHub(Func<HubConnection, Task> func)
        {

            if (Hub == null)
            {
                HuBReadyAsync += func;
            }
            else
                func(Hub);
        }

    }
}
