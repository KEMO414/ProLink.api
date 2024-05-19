﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProLink.Data.Entities
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? JopTitle { get; set; }
        public string? BackImage {  get; set; }
        public string? Description { get; set; }
        public string? FriendId { get; set; }
        public string? FollowerId { get; set; }
        public string? ProfilePicture { get; set; }
        public string? CV { get; set; }
        [InverseProperty("Rater")]
        public virtual ICollection<Rate>? SentRates { get; set; }
        [InverseProperty("Rated")]
        public virtual ICollection<Rate>? ReceivedRates { get; set; }
        [InverseProperty("Sender")]
        public virtual ICollection<JobRequest>? SentJobRequests { get; set; }
        [InverseProperty("Receiver")]
        public virtual ICollection<JobRequest>? ReceivedJobRequests { get; set; }
        public virtual ICollection<FriendRequest>? SentFriendRequests { get; set; }
        public virtual ICollection<FriendRequest>? ReceivedFriendRequests { get; set; }
        public virtual List<string>? Skill { get; set; }

        public virtual ICollection<Post>? Posts { get; set; }
        public virtual ICollection<Post>? LikedPosts { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }

        public virtual ICollection<Like>? Likes { get; set; }
        public virtual ICollection<Message>? SentMessages { get; set; }
        public virtual ICollection<Message>? ReceivedMessages { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<User>? Friends { get; set; } = new List<User>();
        public virtual ICollection<User>? Followers { get; set; } = new List<User>();


    }
}
