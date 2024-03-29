﻿using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorClient.Models
{
    public class Log
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

    public class MessageAndStatus
    {
        public string Status { get; set; }
        public string Message { get; set; }

    }

    public class MessageAndStatusAndData<T> : MessageAndStatus where T :class
    {
        public MessageAndStatusAndData(T data, bool error = false, string msg = "")
        {
            Data = data;
            Message = msg;
            IsError = error;
        }


        public T Data { get; set; }
        public bool IsError { get; set; }

    }
    public  static class ItemState
    {
        public static int Normal => 0;
        public static int Buyed => 1;

    }

    public class RegistrationModel
    {

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string UserName { get; set; }

        [MinLength(6, ErrorMessage = "Minimal lenght is 6")]
        [MaxLength(50)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Compare(nameof(RegistrationModel.Password), ErrorMessage ="Passwords are not equall.")]
        [DataType(DataType.Password)]
        [Required]
        public string PasswordConfirm { get; set; }

    }


    public class Invitation : IModelItemView
    {
        public int InvitationId { get; set; }
        public string EmailAddress { get; set; }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public string SenderName { get; set; }

        public int Id => InvitationId;

        public string Name
        {
            get { return EmailAddress;  }
            set { EmailAddress = value; }
        }

    }
    public class ListAggregationForPermission
    {

        public ListAggregator ListAggregatorEntity { get; set; }

        public List<UserPermissionToListAggregation> Users { get; set; }
    }

    public class UserPermissionToListAggregation: IModelItemView
    {     
        public User User { get; set; }
        public int Permission { get; set; }
      

        public int Order { get; set; }        

        public int Id => User.UserId;
        public string Name
        {
            get { return User.EmailAddress; }
            set { User.EmailAddress = value; }
        }

    }

    public class User
    {
      

        public User()
        {
            ListAggregators = new HashSet<ListAggregator>();


        }

        public int UserId { get; set; }
        public string EmailAddress { get; set; }
        // public string Password { get; set; }

        public byte LoginType { get; set; } // 1 - local 2 - facebook ()
        public ICollection<ListAggregator> ListAggregators { get; set; }


    }




    public class ListAggregator: IModelItem
    {

        public ListAggregator()
        {
            Lists = new HashSet<List>();

        }
        public int PermissionLevel { get; set; }
        public int ListAggregatorId { get; set; }
        public string ListAggregatorName { get; set; }
        public int Order { get; set; }
        public string Name
        {
            get { return ListAggregatorName; }
            set { ListAggregatorName = value; }
        }

        public int Id => ListAggregatorId;

        public ICollection<List> Lists { get; set; }

    }



    public class OrderListItem :IModelItemOrder
    {

        public OrderListItem()
        {
            List = new List<OrderItem>();
        }

        public List<OrderItem> List  {get; set;}
        public int Id { get; set; }

        public int Order { get; set; }
    }

    public class OrderListAggrItem:IModelItemOrder
    {

        public OrderListAggrItem()
        {
            List = new List<OrderListItem>();
        }

        public List<OrderListItem> List { get; set; }
        public int Id { get; set; }

        public int Order { get; set; }
    }

    public class OrderItem:IModelItemOrder
    {
        public int Id { get; set; }

        public int Order { get; set; }

    }


    public class List: IModelItem
    {
        public List()
        {
            ListItems = new HashSet<ListItem>();

        }
        public int ListId { get; set; }
        public string ListName { get; set; }
        public int Order { get; set; }
        public int Id => ListId;
        public string Name
        {
            get { return ListName; }
            set { ListName = value; }
        }
        public ICollection<ListItem> ListItems { get; set; }

    }

    
    public interface IModelItem:  IModelItemView, IModelItemOrder
    {
       

    }

    public interface IModelItemView: IModelItemBase
    {      
       
        public string Name { get; set; }

        //public int Id { get; set; }

    }

    public interface IModelItemOrder : IModelItemBase
    {

        public int  Order { get; set; }

        //public int Id { get; set; }

    }

    public interface IModelItemBase
    {

        public int Id { get; }
    }


    public class ListItem : IModelItem
    {
        public int ListItemId { get; set; }

        public int Order { get; set; }
        public int State { get; set; }


        [Required]
        [MinLength(2, ErrorMessage = "Minimalna długośc to 2")]
        public string ListItemName { get; set; }

        public int Id => ListItemId;
        public string Name
        {
            get { return ListItemName; }
            set { ListItemName = value; }
        }

    }

}
