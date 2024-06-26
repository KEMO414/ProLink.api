﻿using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProLink.Application.DTOs;
using ProLink.Application.Helpers;
using ProLink.Application.Interfaces;
using ProLink.Application.Mail;
using ProLink.Data.Consts;
using ProLink.Data.Entities;
using ProLink.Infrastructure.GenericRepository_UOW;
using ProLink.Infrastructure.IGenericRepository_IUOW;

namespace ProLink.Application.Services
{
    public class FriendRequestService : IFriendRequestService
    {
        #region fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IUserHelpers _userHelpers;
        private readonly IMailingService _mailingService;

        #endregion

        #region ctor
        public FriendRequestService(IUnitOfWork unitOfWork,
            UserManager<User> userManager, IMapper mapper,
            IUserHelpers userHelpers,
            IMailingService mailingService
            )
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _userHelpers = userHelpers;
            _mailingService = mailingService;
        }
        #endregion

        #region  friend request
        public async Task<List<FriendRequestDto>> GetFriendRequistsAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
                throw new Exception("User not found");

            var requests = await _unitOfWork.FriendRequest.FindAsync(f => f.ReceiverId == user.Id && f.Status == Status.Pending, n => n.DateSent, OrderDirection.Descending);
            var result = _mapper.Map<IEnumerable<FriendRequestDto>>(requests).ToList();
            return result;
        }

        public async Task<bool> SendFriendAsync(string userId)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var request=await _unitOfWork.FriendRequest.FindFirstAsync(f=>f.ReceiverId == userId);
            if (request != null) return true;
            await _unitOfWork.CreateTransactionAsync();
            try
            {
                var friendRequest = new FriendRequest
                {
                    Status = Status.Pending,
                    DateSent = DateTime.Now,
                    SenderId = currentUser.Id,
                    ReceiverId = user.Id,
                };
                _unitOfWork.FriendRequest.Add(friendRequest);
                if (await _unitOfWork.SaveAsync() <= 0) return false;
                var message = new MailMessage(new string[] { user.Email }, "friend request",
                    $"{currentUser.FirstName} {currentUser.LastName} sent you friend request");
                _mailingService.SendMail(message);
                await _unitOfWork.CreateSavePointAsync("sendrequest");
                var notification = new Notification
                {
                    Content = $"{currentUser.FirstName} {currentUser.LastName} sent you friend request",
                    Timestamp = DateTime.Now,
                    ReceiverId = user.Id,
                    AboutUserId = currentUser.Id,
                    Type = NotificationType.FriendRequest,
                    IsRead = false
                };
                _unitOfWork.Notification.Add(notification);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackToSavePointAsync("sendrequest");
            }
            return true;
        }

        public async Task<bool> DeletePendingFriendAsync(string userId)
        {
            if (userId.IsNullOrEmpty())
                throw new ArgumentException("Invalid friend Request ID");
            var currentuser=await _userHelpers.GetCurrentUserAsync();
            if (currentuser == null) throw new Exception("current user not found");
            var user = _unitOfWork.User.GetById(userId);
            var friendRequest=await _unitOfWork.FriendRequest.FindFirstAsync(fr=>fr.SenderId==currentuser.Id&&fr.ReceiverId==userId);
            if (friendRequest == null)
                throw new Exception("friend request not found");

            if (friendRequest.Status != Status.Pending)
                return false;


            _unitOfWork.FriendRequest.Remove(friendRequest);
            if (await _unitOfWork.SaveAsync() > 0) return true;
            return false;
        }

        public async Task<bool> DeclinePendingFriendAsync(string friendRequestId)
        {
            if (friendRequestId.IsNullOrEmpty())
                throw new ArgumentException("Invalid friend request ID");

            var request = _unitOfWork.FriendRequest.GetById(friendRequestId);
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (request == null || request.Receiver != currentUser)
                return false;

            if (request.Status != Status.Pending)
                return false;

                request.Status = Status.Declined;
                _unitOfWork.FriendRequest.Update(request);
                if (await _unitOfWork.SaveAsync() <= 0) return false;
                var user = await _userManager.FindByIdAsync(request.SenderId);
                var message = new MailMessage(new string[] { user.Email }, "friend request",
                    $"{currentUser.FirstName} {currentUser.LastName} declined your friend request");
                _mailingService.SendMail(message);

                

            return await _unitOfWork.SaveAsync()>0;
        }

        public async Task<bool> AcceptFriendAsync(string friendRequestId)
        {

            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) return false;
            var request = await _unitOfWork.FriendRequest.FindFirstAsync(f => f.Id == friendRequestId && f.Status == Status.Pending);
            if (request == null || request.ReceiverId != currentUser.Id) return false;
            var user = await _userManager.FindByIdAsync(request.SenderId);
            if (user == null) return false;
            request.Status = Status.Accepted;
            var userFriendForUser = new UserFriend { UserId = currentUser.Id, FriendId = user.Id };
            var userFriendForFriend = new UserFriend { UserId = user.Id, FriendId = currentUser.Id };

            _unitOfWork.UserFriend.Add(userFriendForUser);
            _unitOfWork.UserFriend.Add(userFriendForFriend);

            var notification = new Notification
            {
                Content = $"{currentUser.FirstName} {currentUser.LastName} accepted your friend request",
                Timestamp = DateTime.Now,
                ReceiverId = user.Id,
                AboutUserId = currentUser.Id,
                Type = NotificationType.FriendRequest,
                IsRead = false
            };
            _unitOfWork.Notification.Add(notification);


            var message = new MailMessage(new string[] { user.Email }, "friend request", $"{currentUser.FirstName} {currentUser.LastName} accept your friend request");
            _mailingService.SendMail(message);

            if (await _unitOfWork.SaveAsync() > 0) return true;
            return false;
        }








        public async Task<bool> AcceptAllFriendsAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) return false;
            var requests = await _unitOfWork.FriendRequest.FindAsync(r => r.ReceiverId == currentUser.Id && r.Status == Status.Pending);
            var users = requests.Select(r => r.Sender);
            foreach (var user in users)
            {
                var userFriendForUser = new UserFriend { UserId = currentUser.Id, FriendId = user.Id };
                var userFriendForFriend = new UserFriend { UserId = user.Id, FriendId = currentUser.Id };

                _unitOfWork.UserFriend.Add(userFriendForUser);
                _unitOfWork.UserFriend.Add(userFriendForFriend);
                var notification = new Notification
                {
                    Content = $"{currentUser.FirstName} {currentUser.LastName} accepted your friend request ",
                    Timestamp = DateTime.Now,
                    ReceiverId = user.Id,
                    AboutUserId = currentUser.Id,
                    Type = NotificationType.FriendRequest,
                    IsRead = false
                };
                _unitOfWork.Notification.Add(notification);
                var message = new MailMessage(new string[] { user.Email }, "friend request", $"{currentUser.FirstName} {currentUser.LastName} accept your friend request");
                _mailingService.SendMail(message);
            }
            foreach (var request in requests)
            {
                request.Status = Status.Accepted;
                _unitOfWork.FriendRequest.Update(request);
            }
            _unitOfWork.User.Update(currentUser);
            if (await _unitOfWork.SaveAsync() > 0) return true;
            return false;
        }
        #endregion
    }
}
