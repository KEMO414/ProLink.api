﻿using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using ProLink.Application.Consts;
using ProLink.Application.DTOs;
using ProLink.Application.Helpers;
using ProLink.Application.Interfaces;
using ProLink.Data.Consts;
using ProLink.Data.Entities;
using ProLink.Infrastructure.IGenericRepository_IUOW;

namespace ProLink.Application.Services
{
    public class PostService : IPostService
    {
        #region fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserHelpers _userHelpers;
        private readonly IMapper _mapper;
        #endregion
        #region ctor
        public PostService(IUnitOfWork unitOfWork,
            IMapper mapper,
            IUserHelpers userHelpers)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userHelpers = userHelpers;
        }


        #endregion

        #region post methods
        public async Task<bool> AddPostAsync(PostDto postDto)
        {
            var image = await _userHelpers.AddFileAsync(postDto.PostImage, ConstsFiles.Posts);
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("user not found.");
            var post = _mapper.Map<Post>(postDto);
            post.PostImage = image;
            post.User = currentUser;
            try
            {
                _unitOfWork.Post.Add(post);
            }
            catch
            {
                await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
                return false;
            }
            if (_unitOfWork.Save() > 0)
                return true;
            await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
            return false;
        }



        public async Task<bool> DeletePostAsync(string id)
        {
            var currentUser= await _userHelpers.GetCurrentUserAsync();
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == id);
            string imagePath = post.PostImage;
            if (post == null) throw new Exception("post not found");
            if (currentUser==null||currentUser.Id != post.UserId)
                throw new Exception("not allowed to delete");
            _unitOfWork.Post.Remove(post);
            if (_unitOfWork.Save() > 0)
            {
                if (!imagePath.IsNullOrEmpty())
                    await _userHelpers.DeleteFileAsync(imagePath, ConstsFiles.Posts);
                return true;
            }
            return false;
        }

        public async Task<List<PostResultDto>> GetAllPostsAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.GetAllAsync(p => p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.LikedPosts.Select(p => p.Id);
            foreach (var post in postResults)
            {
                var request=currentUser.SentJobRequests.FirstOrDefault(j => j.PostId==post.Id);
                if (request == null||request.Status==Status.Declined) post.IsRequestSent = false;
                else post.IsRequestSent = true;
                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.Like.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }



        public async Task<PostResultDto> GetPostByIdAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == id);
            if (post == null) throw new Exception("post not found");
            var postResult = _mapper.Map<PostResultDto>(post);
            var likedPostIds = currentUser.LikedPosts.Select(p => p.Id);
            var request = currentUser.SentJobRequests.FirstOrDefault(j => j.PostId == post.Id);
            if (request == null || request.Status == Status.Declined) postResult.IsRequestSent = false;
            else postResult.IsRequestSent = true;
            if (likedPostIds.Contains(post.Id))
            {
                postResult.IsLiked = true;
                var Like = await _unitOfWork.Like.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                postResult.LikeId = Like.Id;
            }
            return postResult;
        }

        public async Task<List<PostResultDto>> GetPostsByTitleAsync(string title)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p => p.Title.Contains(title),p=> p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.LikedPosts.Select(p => p.Id);
            foreach (var post in postResults)
            {
                var request = currentUser.SentJobRequests.FirstOrDefault(j => j.PostId == post.Id);
                if (request == null || request.Status == Status.Declined) post.IsRequestSent = false;
                else post.IsRequestSent = true;
                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.Like.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }

        public async Task<List<PostResultDto>> GetUserPostsAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p => p.UserId == currentUser.Id,p=> p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.LikedPosts.Select(p => p.Id);
            foreach (var post in postResults)
            {
                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.Like.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }

        public async Task<List<PostResultDto>> GetUserPostsByUserIdAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p => p.UserId == id,p=> p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.LikedPosts.Select(p => p.Id);
            foreach (var post in postResults)
            {
                var request = currentUser.SentJobRequests.FirstOrDefault(j => j.PostId == post.Id);
                if (request == null || request.Status == Status.Declined) post.IsRequestSent = false;
                else post.IsRequestSent = true;
                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like=await _unitOfWork.Like.FindFirstAsync(p => p.UserId == currentUser.Id&&p.PostId==post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }



        public async Task<bool> UpdatePostAsync(string id, PostDto postDto)
        {
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == id);
            var oldImage = post.PostImage;
            if (post == null) throw new Exception("post not found");
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("user not found.");
            if (currentUser.Id !=post.UserId)
                throw new Exception("not allowed to edit");

            _mapper.Map(postDto, post);
            string image = "";
            if (postDto.PostImage != null)
            {
                image = await _userHelpers.AddFileAsync(postDto.PostImage, ConstsFiles.Posts);
                post.PostImage = image;
            }
            _unitOfWork.Post.Update(post);
            if (_unitOfWork.Save() > 0)
            {
                await _userHelpers.DeleteFileAsync(oldImage, ConstsFiles.Posts);
                return true;
            }
            if (image.IsNullOrEmpty())
                await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
            return false;
        }

        #endregion

        #region comments methods
        public async Task<bool> AddCommentAsync(string postId, AddCommentDto addCommentDto)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == postId);
            if (post == null) throw new Exception("post doesnt exist");
            Comment comment = _mapper.Map<Comment>(addCommentDto);
            comment.User = user;
            comment.Post = post;
            _unitOfWork.Comment.Add(comment);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }
        public async Task<bool> UpdateCommentAsync(string commentId, AddCommentDto addCommentDto)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var oldComment = await _unitOfWork.Comment.FindFirstAsync(p => p.Id == commentId);
            if (oldComment == null) throw new Exception("Comment doesnt exist");
            if (oldComment.UserId != user.Id) throw new UnauthorizedAccessException("cant update some one comment");
            _mapper.Map(addCommentDto, oldComment);
            _unitOfWork.Comment.Update(oldComment);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }
        public async Task<bool> DeleteCommentAsync(string commentId)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var comment = await _unitOfWork.Comment.FindFirstAsync(p => p.Id == commentId);
            if (comment == null) throw new Exception("Comment doesnt exist");
            if (comment.UserId != user.Id) throw new UnauthorizedAccessException("cant delete some one comment");
            _unitOfWork.Comment.Remove(comment);
            if (_unitOfWork.Save() > 0) return true;
            return false;
        }
        #endregion

        #region like methods
        public async Task<bool> AddLikeAsync(string postId)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == postId);
            if (post == null) throw new Exception("post doesnt exist");
            var likeExist = await _unitOfWork.Like.FindFirstAsync(l => l.UserId == user.Id && l.PostId == post.Id);
            if (likeExist == null)
            {
                Like like = new Like();
                like.User = user;
                like.Post = post;
                like.DateLiked = DateTime.Now;
                _unitOfWork.CreateTransaction();
                try
                {
                    user.LikedPosts.Add(post);
                    _unitOfWork.Like.Add(like);
                    _unitOfWork.Save();
                    _unitOfWork.Commit();
                    return true;
                }
                catch
                {
                    _unitOfWork.Rollback();
                    return false;
                }


            }

            return false;
        }
        public async Task<bool> DeleteLikeAsync(string LikeId)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var like = await _unitOfWork.Like.FindFirstAsync(l => l.Id == LikeId);
            if (like == null) throw new Exception("like doesnt exist");
            if (like.UserId != user.Id) throw new UnauthorizedAccessException("cant update some one comment");

            _unitOfWork.CreateTransaction();
            try
            {
                user.LikedPosts.Remove(like.Post);
                _unitOfWork.Save();
                _unitOfWork.Like.Remove(like);
                _unitOfWork.Save();
                _unitOfWork.Commit();
                return true;
            }
            catch
            {
                _unitOfWork.Rollback();
                return false;
            }
        }
        #endregion
    }
}
