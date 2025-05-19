using BlazorClient.Models;
using BlazorClient.Models.Requests;
using BlazorClient.Models.Response;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClient.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;
        private readonly StateService _userInfoService;

        public UserService(HttpClient httpClient, IConfiguration configuration, ILocalStorageService localStorage
            , StateService userInfoService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _localStorage = localStorage;
            _userInfoService = userInfoService;
            _httpClient.BaseAddress = new Uri(_configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlazorServer");

        }

        string token;
        private async Task SetRequestBearerAuthorizationHeader(HttpRequestMessage httpRequestMessage)
        {

            token = await _localStorage.GetItemAsync<string>("accessToken");
            var gid = await _localStorage.GetItemAsync<string>("gid");

            if (token != null)
            {

                httpRequestMessage.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
                // _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer  {token}");
            }


            var signalRId = _userInfoService.GetStateInfo(gid).ClientSignalRID;

            httpRequestMessage.Headers.Add("SignalRId", signalRId);
        }


        public async Task<MessageAndStatusAndData<string>> RegisterAsync(RegistrationModel model)
        {

            var loginRequest = new RegistrationRequest
            {
                UserName = model.UserName,
                Password = model.Password
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Register")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };


            var response = await _httpClient.SendAsync(requestMessage);


            switch (response)
            {
                case { StatusCode: System.Net.HttpStatusCode.OK }:
                    var token = await response.Content.ReadAsStringAsync();
                    return MessageAndStatusAndData<string>.Ok(token);

                case { StatusCode: System.Net.HttpStatusCode.Conflict }:
                    return MessageAndStatusAndData<string>.Fail("User exists.");

                default:
                    return MessageAndStatusAndData<string>.Fail("Server error.");
            }

            //if (response.IsSuccessStatusCode)
            //{
            //    var token = await response.Content.ReadAsStringAsync();

            //    return MessageAndStatusAndData<string>.Ok(token);
            //}

            //return response switch
            //{
            //    { StatusCode: System.Net.HttpStatusCode.Conflict } =>
            //         MessageAndStatusAndData<string>.Fail("User exists."),
            //    _ =>
            //        MessageAndStatusAndData<string>.Fail("Server error."),
            //};

        }


        public async Task<MessageAndStatusAndData<UserNameAndTokenResponse>> LoginAsync(string userName, string password)
        {
            var loginRequest = new LoginRequest
            {
                UserName = userName,
                Password = password
            };

            var json = JsonConvert.SerializeObject(loginRequest);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/Login")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };


            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                return MessageAndStatusAndData<UserNameAndTokenResponse>.Fail("Invalid username or password.");
            }

            var content = await response.Content.ReadAsStringAsync();

            var tokenAndUsername = JsonConvert.DeserializeObject<UserNameAndTokenResponse>(content);


            return MessageAndStatusAndData<UserNameAndTokenResponse>.Ok(tokenAndUsername);

        }


        public async Task<List<ListAggregationForPermission>> GetListAggregationForPermissionAsync(string userName)
        {

            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            // querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "User/GetListAggregationForPermission" + querry.ToString());


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(data);


            //  var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermissionTransferClass>>(data);
            var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermission>>(message.Message);


            return await Task.FromResult(dataObjects);
        }
        public async Task<List<ListAggregationForPermission>> GetListAggregationForPermission_EmptyAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/GetListAggregationForPermission_Empty");


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatusAndData<List<ListAggregationForPermission>>>(data);


            //  var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermissionTransferClass>>(data);
            var dataObjects = message.Data;


            return await Task.FromResult(dataObjects);
        }

        public async Task<ListAggregationForPermission> GetListAggregationForPermissionByListAggrId
            (ListAggregationForPermission listAggregationForPermission)
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Permissions/GetListAggregationForPermissionByListAggrId");

            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(listAggregationForPermission));

            await SetRequestBearerAuthorizationHeader(requestMessage);
            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatusAndData<ListAggregationForPermission>>(data);


            //  var dataObjects = JsonConvert.DeserializeObject<List<ListAggregationForPermissionTransferClass>>(data);
            var dataObjects = message.Data;


            return await Task.FromResult(dataObjects);
        }




        public async Task<User> GetUserDataTreeAsync()
        {

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "User/UserDataTree");


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var user = JsonConvert.DeserializeObject<User>(data);


            return await Task.FromResult(user);
        }


        public async Task<string> AddUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "AddUserPermission");
        }

        public async Task<string> ChangeUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {

            return await UniversalUserPermission(userPermissionToList, listAggregationId, "ChangeUserPermission");

        }


        public async Task<string> DeleteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "DeleteUserPermission");
        }

        public async Task<string> InviteUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId)
        {
            return await UniversalUserPermission(userPermissionToList, listAggregationId, "InviteUserPermission");
        }

        private async Task<string> UniversalUserPermission(UserPermissionToListAggregation userPermissionToList, int listAggregationId,
            string actionName)
        {
            var querry = new QueryBuilder();

            querry.Add("listAggregationId", listAggregationId.ToString());

            var httpMethod = actionName == "DeleteUserPermission"? HttpMethod.Delete : HttpMethod.Post;

            string serializedUser = JsonConvert.SerializeObject(userPermissionToList);

            var requestMessage = new HttpRequestMessage(httpMethod, "Permissions/" + actionName + querry.ToString());


            requestMessage.Content = new StringContent(serializedUser);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);
            SetRequestAuthorizationLevelHeader(requestMessage, listAggregationId);

            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                var message = JsonConvert.DeserializeObject<ProblemDetails>(responseBody);

                return message.Title;
            }

            return actionName switch
            {
                "AddUserPermission" => "User was added.",
                "ChangeUserPermission" => "Permission has changed.",
                "InviteUserPermission" => "Ivitation was added.",
                "DeleteUserPermission" => "User permission was deleted.",
                _ => throw new ArgumentException("Bad action name.")
            };



        }


        public async Task<List<Invitation>> GetInvitationsListAsync(string userName)
        {
            var querry = new QueryBuilder();
            querry.Add("userName", userName);
            // querry.Add("password", password);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/GetInvitationsList" + querry.ToString());


            await SetRequestBearerAuthorizationHeader(requestMessage);


            var response = await _httpClient.SendAsync(requestMessage);

            var data = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(data);

            var dataObjects = JsonConvert.DeserializeObject<List<Invitation>>(message.Message);


            return await Task.FromResult(dataObjects);
        }



        public async Task<string> AcceptInvitationAsync(Invitation invitation)
        {
            return await UniversalInvitationAction(invitation, "AcceptInvitation");

        }
        public async Task<string> RejectInvitaionAsync(Invitation invitation)
        {

            return await UniversalInvitationAction(invitation, "RejectInvitaion");

        }

        async Task<string> UniversalInvitationAction(Invitation invitation, string actionName)
        {
            string serialized = JsonConvert.SerializeObject(invitation);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Invitation/" + actionName);


            requestMessage.Content = new StringContent(serialized);

            requestMessage.Content.Headers.ContentType
              = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


            await SetRequestBearerAuthorizationHeader(requestMessage);

            var response = await _httpClient.SendAsync(requestMessage);

            var responseStatusCode = response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync();

            var message = JsonConvert.DeserializeObject<MessageAndStatus>(responseBody);


            return await Task.FromResult(message.Message);
        }

        void SetRequestAuthorizationLevelHeader(HttpRequestMessage httpRequestMessage, int listAggregationId)
        {

            if (token != null)
            {
                httpRequestMessage.Headers.Add("listAggregationId", listAggregationId.ToString());

                using SHA256 mySHA256 = SHA256.Create();

                var bytes = Encoding.ASCII.GetBytes(token + listAggregationId.ToString());

                var hashBytes = mySHA256.ComputeHash(bytes);

                var hashString = Convert.ToBase64String(hashBytes);

                httpRequestMessage.Headers.Add("Hash", hashString);

            }
        }
    }
}
