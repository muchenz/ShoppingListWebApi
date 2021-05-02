using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared
{

    public class MessageAndStatus
    {
        public string Status { get; set; }
        public string Message { get; set; }

    }

    public class MessageAndStatusAndData<T>
    {
        public MessageAndStatusAndData(T data, string msg, bool error)
        {
            Data = data;
            Message = msg;
            Error = error;
        }

        public T Data { get; set; }
        public string Message { get; set; }
        public bool Error { get; set; }

    }

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


        public ICollection<ListAggregator> ListAggregators { get; set; }
        public ICollection<string> Roles { get; set; }

        // public virtual TokenItem Token { get; set; }

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

    }


    public class ListItem
    {
        public int ListItemId { get; set; }
        public int Order { get; set; }
        public int State { get; set; }

        public string ListItemName { get; set; }

    }




    //public class TokenItem
    //{
    //    public int TokenId { get; set; }
    //    public string Token { get; set; }
    //    public int UserId { get; set; }
    //    public User User { get; set; }


    //}


}
