using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Models
{
    public  class Log
    {
        public long LogId { get; set; }
        public string LogLevel { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string CreatedDate { get; set; }
        public long? UserId { get; set; }
        public Log Inner { get; set; }

    }
    public class MessageSatus
    {
        public const string OK = "OK";
        public const string Error = "ERROR";

    }
    public class MessageAndStatus
    {
        public bool IsError => Status == MessageSatus.Error;
        public string Status { get; set; }
        public string Message { get; set; }

    }

    public class TokenAndEmailData
    {
        public string Token { get; set; }
        public string Email { get; set; }
    }

    public class MessageAndStatusAndData<T> : MessageAndStatus
    {
        private MessageAndStatusAndData(T data, string msg, string status)
        {
            Data = data;
            Message = msg;
            Status = status;
        }

        public T Data { get; set; }

        public static MessageAndStatusAndData<T> Ok(T data) =>
            new MessageAndStatusAndData<T>(data, string.Empty, MessageSatus.OK);

        public static MessageAndStatusAndData<T> Fail(string msg) =>
           new MessageAndStatusAndData<T>(default, msg, MessageSatus.Error);
    }

    //public class MessageAndStatusAndData
    //{
    //    public static MessageAndStatusAndData<T> Ok<T>(T data) =>
    //       new MessageAndStatusAndData<T>(data, string.Empty, MessageSatus.OK);

    //    public static MessageAndStatusAndData<T> Fail<T>(string msg) =>
    //       new MessageAndStatusAndData<T>(default, msg, MessageSatus.Error);
    //}

    public class Invitation
    {
        public int InvitationId { get; set; }
        public string EmailAddress { get; set; }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public string SenderName { get; set; }

    }

    public class User
    {

        public User()
        {
            ListAggregators = new HashSet<ListAggregator>();
            Roles = new HashSet<string>();

        }

        public int UserId { get; set; }
        public string EmailAddress { get; set; }
        // public string Password { get; set; }

        public byte LoginType { get; set; } // 1 - local 2 - facebook ()
        public ICollection<ListAggregator> ListAggregators { get; set; }
        public ICollection<string> Roles { get; set; }

        // public virtual TokenItem Token { get; set; }

    }
    public class UserListAggregator
    { 
        public int UserId { get; set; }
        public int ListAggregatorId { get; set; }
        public virtual User User { get; set; }
        public virtual ListAggregator ListAggregator { get; set; }
        public int PermissionLevel { get; set; }
        public int State { get; set; }

    }

    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }

    }


    public class ListAggregator
    {

        public ListAggregator()
        {
            Lists = new HashSet<List>();

        }

        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public int PermissionLevel { get; set; }


        public ICollection<List> Lists { get; set; }

    }



    public class List
    {
        public List()
        {
            ListItems = new HashSet<ListItem>();

        }
        public int ListId { get; set; }
        public string ListName { get; set; }
        public int Order { get; set; }

        public ICollection<ListItem> ListItems { get; set; }
        public int ListAggrId { get; set; }


    }


    public class ListItem
    {
        public int ListItemId { get; set; }
        public int Order { get; set; }
        public int State { get; set; }

        public string ListItemName { get; set; }
        public int ListAggrId { get; set; }

    }


    public class ListAggregationForPermission
    {

        public ListAggregator ListAggregatorEntity { get; set; }

        public List<UserPermissionToListAggregation> Users { get; set; }
    }

    public class UserPermissionToListAggregation
    {

        public User User { get; set; }
        public int Permission { get; set; }

    }

    //public class TokenItem
    //{
    //    public int TokenId { get; set; }
    //    public string Token { get; set; }
    //    public int UserId { get; set; }
    //    public User User { get; set; }


    //}


}
