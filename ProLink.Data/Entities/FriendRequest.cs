﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ProLink.Data.Consts;

namespace ProLink.Data.Entities
{
    public class FriendRequest
    {
        public string Id { get; set; }= Guid.NewGuid().ToString();
        public DateTime DateSent { get; set; }
        public Status Status { get; set; }
        public string SenderId { get; set; }
        public virtual User Sender { get; set; }
        public string ReceiverId { get; set; }
        public virtual User Receiver { get; set; }
    }

}
