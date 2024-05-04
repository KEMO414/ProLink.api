﻿using Microsoft.AspNetCore.Identity;
using ProLink.Data.Entities;
using ProLink.Application.Interfaces;
using ProLink.Application.DTOs;
using ProLink.Application.Helpers;
using ProLink.Infrastructure.IGenericRepository_IUOW;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using ProLink.Application.Consts;

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
            IUserHelpers userHelpers
            )
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
            var user = _mapper.Map<UserResultDto>(currentUser);
            return user;
        }
        public async Task<UserResultDto> GetUserByIdAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("User not found.");
            var user = _mapper.Map<UserResultDto>(currentUser);
            return user;
        }

        public async Task<List<UserResultDto>> GetUsersByNameAsync(string name)
        {
            var users = await _unitOfWork.User.FindAsync(u => u.FirstName.Contains(name)|| u.LastName.Contains(name));
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
                _unitOfWork.Save();
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
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> AddRateAsync(string userId, RateDto rateDto)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) throw new UnauthorizedAccessException("unAuthorized");
            var user=await _userManager.FindByIdAsync(userId);
            if (user == null) throw new ArgumentNullException("user not found");
            var rate = await _unitOfWork.Rate.FindFirstAsync(r => r.RaterId == currentUser.Id);
            if (rate == null)
                _unitOfWork.Rate.Add(new Rate { RatedId = userId, RaterId = currentUser.Id, RateValue = rateDto.RateValue });
            else 
            { 
                rate.RateValue = rateDto.RateValue;
                _unitOfWork.Rate.Update(rate);
            }
            if(_unitOfWork.Save()>0)return true;
            return false;
        }

        public async Task<bool> DeleteRateAsync(string rateId)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) throw new UnauthorizedAccessException("unAuthorized");
            var rate=await _unitOfWork.Rate.FindFirstAsync(f=>f.Id==rateId);
            if (rate == null) return false;
            _unitOfWork.Rate.Remove(rate);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }

        #endregion

        #region skill methods
        public async Task<bool> AddSkillAsync(AddSkillDto addSkilltDto)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            Skill skill = _mapper.Map<Skill>(addSkilltDto);
            skill.User = user;
            _unitOfWork.Skill.Add(skill);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }

        public async Task<List<SkillDto>> GetCurrentUserSkillsAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var skills = await _unitOfWork.Skill.GetAllAsync();
            var skillDtos = _mapper.Map<List<SkillDto>>(skills);
            return skillDtos;
        }

        

        public async Task<List<SkillDto>> GetUserSkillsByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) throw new Exception("user not found");
            var skills = await _unitOfWork.Skill.FindAsync(s=>s.UserId == id);
            var skillDtos = _mapper.Map<List<SkillDto>>(skills);
            return skillDtos;
        }

        public async Task<bool> UpdateSkillAsync(string skillId, AddSkillDto addSkillDto)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var oldSkill = await _unitOfWork.Skill.FindFirstAsync(s => s.SkillId == skillId);
            if (oldSkill == null) throw new Exception("skill doesnt exist");
            if (oldSkill.UserId != user.Id) throw new UnauthorizedAccessException("cant update some one skill");
            _mapper.Map(addSkillDto, oldSkill);
            _unitOfWork.Skill.Update(oldSkill);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }

        public async Task<bool> DeleteSkillAsync(string skillId)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var skill = await _unitOfWork.Skill.FindFirstAsync(s => s.SkillId == skillId);
            if (skill == null) throw new Exception("skill doesnt exist");
            if (skill.UserId != user.Id) throw new UnauthorizedAccessException("cant delete some one skill");
            _unitOfWork.Skill.Remove(skill);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }
        #endregion

        #region file handlling
        public async Task<bool> AddUserPictureAsync(IFormFile file)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            var picture = await _userHelpers.AddImageAsync(file,ConstsFiles.Profile);
            if (picture != null)
                user.ProfilePicture = picture;
            _unitOfWork.User.Update(user);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }

        public async Task<bool> DeleteUserPictureAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            var oldPicture = user.ProfilePicture;
            user.ProfilePicture = null;
            _unitOfWork.User.Update(user);
            if (_unitOfWork.Save() > 0)
                return await _userHelpers.DeleteImageAsync(oldPicture, ConstsFiles.Profile);
            return false;
        }

        public async Task<bool> UpdateUserPictureAsync(IFormFile? file)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) return false;
            var newPicture = await _userHelpers.AddImageAsync(file, ConstsFiles.Profile);
            var oldPicture = user.ProfilePicture;
            user.ProfilePicture = newPicture;
            _unitOfWork.User.Update(user);
            if (_unitOfWork.Save() > 0 && !oldPicture.IsNullOrEmpty())
            {
                return await _userHelpers.DeleteImageAsync(oldPicture, ConstsFiles.Profile);
            }
            await _userHelpers.DeleteImageAsync(newPicture, ConstsFiles.Profile);
            return false;
        }

        public async Task<string> GetUserPictureAsync()
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null)
                throw new Exception("User not found");
            else if (user.ProfilePicture.IsNullOrEmpty())
                throw new Exception("User dont have profile picture");
            return user.ProfilePicture;
        }

        

        #endregion
    }
}
