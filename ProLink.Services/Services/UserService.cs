﻿using Microsoft.AspNetCore.Identity;
using ProLink.Data.Entities;
using ProLink.Application.Interfaces;
using ProLink.Application.DTOs;
using ProLink.Application.Helpers;
using ProLink.Infrastructure.IGenericRepository_IUOW;
using AutoMapper;
using ProLink.Application.Consts;
using ProLink.Data.Consts;

namespace ProLink.Application.Services
{
    public class UserService : IUserService
    {
        #region fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IUserHelpers _userHelpers;

        #endregion

        #region ctor
        public UserService(IUnitOfWork unitOfWork,
            UserManager<User> userManager, IMapper mapper,
            IUserHelpers userHelpers)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _userHelpers = userHelpers;
        }
        #endregion

        #region user methods

        public async Task<UserResultDto> GetCurrentUserInfoAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("User not found.");
            var userResult = _mapper.Map<UserResultDto>(currentUser);

            return userResult;
        }

        public async Task<UserInfoResultDto> GetUserInfoAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("User not found.");
            var userResult = _mapper.Map<UserInfoResultDto>(currentUser);

            return userResult;
        }

        public async Task<UserResultDto> GetUserByIdAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var user = await _userManager.FindByIdAsync(id);
            var userResult = _mapper.Map<UserResultDto>(user);

           


            var friendRequest = await _unitOfWork.FriendRequest.FindFirstAsync(f=>
            f.ReceiverId == user.Id && f.SenderId == currentUser.Id);
            if(friendRequest!=null)
                userResult.IsFriendRequestSent = true;
            else userResult.IsFriendRequestSent = false;


            var userfollower=await _unitOfWork.UserFollower.FindFirstAsync(uf=>uf.FollowerId==currentUser.Id&&uf.UserId==user.Id);
            if (userfollower!=null) userResult.IsFollowed = true;
            else userResult.IsFollowed = false;

            var userFriend = await _unitOfWork.UserFriend.FindFirstAsync(uf =>
            (uf.UserId == currentUser.Id && uf.FriendId == user.Id)
            || (uf.UserId == user.Id && uf.FriendId == currentUser.Id));

            if (userFriend!=null) userResult.IsFriend = true;
            else userResult.IsFriend = false;
            return userResult;
        }

        public async Task<List<UserResultDto>> GetUsersByNameOrTitleAsync(string cratirea)
        {
            var users = await _unitOfWork.User.FindAsync(u => u.FirstName.Contains(cratirea) || u.LastName.Contains(cratirea)||u.JopTitle.Contains(cratirea));
            var usersResult = users.Select(user => _mapper.Map<UserResultDto>(user));
            return usersResult.ToList();
        }

        public async Task<bool> UpdateUserInfoAsync(UserDto userDto)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new ArgumentNullException("user not found");
            try
            {
                currentUser = _mapper.Map(userDto, currentUser);
                _unitOfWork.User.Update(currentUser);
                await _unitOfWork.SaveAsync();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
            {
                return false;
            }
             _unitOfWork.User.Remove(user);
            return await _unitOfWork.SaveAsync() > 0;
        }

        #endregion

        

        #region file handlling
        

        public async Task<bool> DeleteUserPictureAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var oldPicture = user.ProfilePicture;
            user.ProfilePicture = null;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
                return true;
                //return await _userHelpers.DeleteFileAsync(oldPicture, ConstsFiles.Profile);
            return false;
        }

        public async Task<bool> UpdateUserPictureAsync(/*IFormFile? file*/ string path)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var newPicture = await _userHelpers.AddFileAsync(file, ConstsFiles.Profile);
            //var oldPicture = user.ProfilePicture;
            user.ProfilePicture = path;// newPicture;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
            {
                return true;
                //if (!oldPicture.IsNullOrEmpty())
                //    return await _userHelpers.DeleteFileAsync(oldPicture, ConstsFiles.Profile);
                //return true;
            }
            //await _userHelpers.DeleteFileAsync(newPicture, ConstsFiles.Profile);
            return false;
        }

        public async Task<string> GetUserPictureAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
                throw new Exception("User not found");
            return user.ProfilePicture;
        }

        public async Task<bool> DeleteUserCVAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var oldCV = user.CV;
            user.CV = null;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
                return true; //return await _userHelpers.DeleteFileAsync(oldCV, ConstsFiles.CV);
            return false;
        }

        public async Task<bool> UpdateUserCVAsync(/*IFormFile? file*/ string path)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var newCV = await _userHelpers.AddFileAsync(file, ConstsFiles.CV);
            //var oldCV = user.CV;
            user.CV = path;//newCV;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
            {
                //if (!oldCV.IsNullOrEmpty())
                //    return await _userHelpers.DeleteFileAsync(oldCV, ConstsFiles.CV);
                return true;
            }
            //await _userHelpers.DeleteFileAsync(newCV, ConstsFiles.CV);
            return false;
        }

        public async Task<string> GetUserCVAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
                throw new Exception("User not found");
            return user.CV;
        }

        public async Task<bool> DeleteUserBackImageAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var oldBackImage = user.BackImage;
            user.BackImage = null;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
                return true;// return await _userHelpers.DeleteFileAsync(oldBackImage, ConstsFiles.BackImage);
            return false;
        }

        public async Task<bool> UpdateUserBackImageAsync(/*IFormFile? file*/ string path)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            //var newBackImage = await _userHelpers.AddFileAsync(file, ConstsFiles.BackImage);
            //var oldBackImage = user.BackImage;
            user.BackImage = path;// newBackImage;
            _unitOfWork.User.Update(user);
            if (await _unitOfWork.SaveAsync() > 0)
            {
                //if (!oldBackImage.IsNullOrEmpty())
                //    return await _userHelpers.DeleteFileAsync(oldBackImage, ConstsFiles.BackImage);
                return true;
            }
            //await _userHelpers.DeleteFileAsync(newBackImage, ConstsFiles.BackImage);
            return false;
        }

        public async Task<string> GetUserBackImageAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
                throw new Exception("User not found");
            return user.BackImage;
        }

        
        #endregion

    }
}
