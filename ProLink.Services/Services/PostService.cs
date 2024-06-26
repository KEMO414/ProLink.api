﻿using AutoMapper;
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
            //var image = await _userHelpers.AddFileAsync(postDto.PostImage, ConstsFiles.Posts);
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("user not found.");
            var post = _mapper.Map<Post>(postDto);
            //post.PostImage = //image;
            post.User = currentUser;
            //try
            //{
            _unitOfWork.Post.Add(post);
            //}
            //catch
            //{
            //    await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
            //    return false;
            //}
            foreach (var follower in currentUser.Followers)
            {
                var notification = new Notification
                {
                    Content = $"{currentUser.FirstName} {currentUser.LastName} just add a post",
                    Timestamp = DateTime.Now,
                    ReceiverId = follower.FollowerId,
                    AboutUserId = currentUser.Id,
                    Type=NotificationType.Post,
                    IsRead=false
                };
                _unitOfWork.Notification.Add(notification);
            }
            if (await _unitOfWork.SaveAsync() > 0)
                return true;
            //await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
            return false;
        }
        public async Task<bool> UpdatePostAsync(string id, PostDto postDto)
        {
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == id);
            //var oldImage = post.PostImage;
            if (post == null) throw new Exception("post not found");
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null)
                throw new Exception("user not found.");
            if (currentUser.Id != post.UserId)
                throw new Exception("not allowed to edit");

            _mapper.Map(postDto, post);
            //string image = "";
            //if (postDto.PostImage != null)
            //{
            //    image = await _userHelpers.AddFileAsync(postDto.PostImage, ConstsFiles.Posts);
            //    post.PostImage = image;
            //}
            _unitOfWork.Post.Update(post);
            if (await _unitOfWork.SaveAsync() > 0)
            {
                //await _userHelpers.DeleteFileAsync(oldImage, ConstsFiles.Posts);
                return true;
            }
            //if (image.IsNullOrEmpty())
            //    await _userHelpers.DeleteFileAsync(image, ConstsFiles.Posts);
            return false;
        }
        public async Task<bool> DeletePostAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == id);
            //string imagePath = post.PostImage;
            if (post == null) throw new Exception("post not found");
            if (currentUser == null || currentUser.Id != post.UserId)
                throw new Exception("not allowed to delete");
            _unitOfWork.Post.Remove(post);
            if (await _unitOfWork.SaveAsync() > 0)
            {
                //if (!imagePath.IsNullOrEmpty())
                //    await _userHelpers.DeleteFileAsync(imagePath, ConstsFiles.Posts);
                return true;
            }
            return false;
        }
        public async Task<List<PostResultDto>> GetAllPostsAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.GetAllAsync(p => p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.Reacts.Select(r => r.PostId);
            foreach (var post in postResults)
            {
                var react = await _unitOfWork.React.FindFirstAsync(r => r.PostId == post.Id && r.UserId == currentUser.Id);
                post.React=_mapper.Map<ReactDto>(react);
                var userFollower = await _unitOfWork.UserFollower.FindFirstAsync(uf =>
                uf.FollowerId == currentUser.Id && uf.UserId == post.User.Id);
                if (userFollower != null) post.IsUserFollowed = true;
                else post.IsUserFollowed = false;

                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.React.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
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
            var likedPostIds = currentUser.Reacts.Select(r => r.PostId); 
            var react = await _unitOfWork.React.FindFirstAsync(r => r.PostId == post.Id && r.UserId == currentUser.Id);
            postResult.React = _mapper.Map<ReactDto>(react);
            if (likedPostIds.Contains(post.Id))
            {
                postResult.IsLiked = true;
                var Like = await _unitOfWork.React.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                postResult.LikeId = Like.Id;
            }
            return postResult;
        }
        public async Task<List<PostResultDto>> GetPostsByTitleAsync(string title)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p=>p.Description.Contains(title),p => p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.Reacts.Select(r => r.PostId); 
            foreach (var post in postResults)
            {
                var react = await _unitOfWork.React.FindFirstAsync(r => r.PostId == post.Id && r.UserId == currentUser.Id);
                post.React = _mapper.Map<ReactDto>(react);
                var userFollower = await _unitOfWork.UserFollower.FindFirstAsync(uf =>
                uf.FollowerId == currentUser.Id && uf.UserId == post.User.Id);
                if (userFollower != null) post.IsUserFollowed = true;
                else post.IsUserFollowed = false;

                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.React.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }
        public async Task<List<PostResultDto>> GetUserPostsAsync()
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p => p.UserId == currentUser.Id, p => p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.Reacts.Select(r => r.PostId); 
            foreach (var post in postResults)
            {
                var react = await _unitOfWork.React.FindFirstAsync(r => r.PostId == post.Id && r.UserId == currentUser.Id);
                post.React = _mapper.Map<ReactDto>(react);
                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.React.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }
        public async Task<List<PostResultDto>> GetUserPostsByUserIdAsync(string id)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            var posts = await _unitOfWork.Post.FindAsync(p => p.UserId == id, p => p.DateCreated, OrderDirection.Descending);
            var postResults = _mapper.Map<IEnumerable<PostResultDto>>(posts);
            var likedPostIds = currentUser.Reacts.Select(r => r.PostId); 
            foreach (var post in postResults)
            {
                var react = await _unitOfWork.React.FindFirstAsync(r => r.PostId == post.Id && r.UserId == currentUser.Id);
                post.React = _mapper.Map<ReactDto>(react);
                var userFollower = await _unitOfWork.UserFollower.FindFirstAsync(uf =>
                uf.FollowerId == currentUser.Id && uf.UserId == post.User.Id);
                if (userFollower != null) post.IsUserFollowed = true;
                else post.IsUserFollowed = false;

                if (likedPostIds.Contains(post.Id))
                {
                    post.IsLiked = true;
                    var Like = await _unitOfWork.React.FindFirstAsync(p => p.UserId == currentUser.Id && p.PostId == post.Id);
                    post.LikeId = Like.Id;
                }
            }
            return postResults.ToList();
        }
            

        #endregion

        #region comments methods
        public async Task<bool> AddCommentAsync(string postId, AddCommentDto addCommentDto)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) throw new Exception("user not found");
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == postId);
            if (post == null) throw new Exception("post doesnt exist");
            Comment comment = _mapper.Map<Comment>(addCommentDto);
            comment.User = currentUser;
            comment.Post = post;
            _unitOfWork.Comment.Add(comment);

            var notification = new Notification
            {
                Content = $"{currentUser.FirstName} {currentUser.LastName} commented on your post",
                Timestamp = DateTime.Now,
                ReceiverId = post.UserId,
                AboutUserId = currentUser.Id,
                Type=NotificationType.Comment,
                IsRead=false
            };
            _unitOfWork.Notification.Add(notification);
            if (await _unitOfWork.SaveAsync() > 0) return true;
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
            if (await _unitOfWork.SaveAsync() > 0) return true;
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
            if (await _unitOfWork.SaveAsync() > 0) return true;
            return false;
        }
        #endregion

        #region like methods
        public async Task<bool> AddReactAsync(string postId,ReactType type)
        {
            var currentUser = await _userHelpers.GetCurrentUserAsync();
            if (currentUser == null) throw new Exception("user not found");
            var post = await _unitOfWork.Post.FindFirstAsync(p => p.Id == postId);
            if (post == null) throw new Exception("post doesnt exist");
            var reactExist = await _unitOfWork.React.FindFirstAsync(l => l.UserId == currentUser.Id && l.PostId == postId);
            if (reactExist != null)
            {
                reactExist.DateReacted = DateTime.Now;
                reactExist.Type = type;
                _unitOfWork.React.Update(reactExist);
                if (await _unitOfWork.SaveAsync() > 0) return true;
                return false;
            }
            else
            {
                React react = new React
                {
                    User = currentUser,
                    Post = post,
                    Type = type,
                    DateReacted = DateTime.Now
                };
                
                await _unitOfWork.CreateTransactionAsync();
                try
                {
                    _unitOfWork.React.Add(react);
                    await _unitOfWork.SaveAsync();

                    var notification = new Notification
                    {
                        Content = $"{currentUser.FirstName} {currentUser.LastName} reacted with your post",
                        Timestamp = DateTime.Now,
                        ReceiverId = post.UserId,
                        AboutUserId = currentUser.Id,
                        Type=NotificationType.React,
                        IsRead = false
                    };
                    _unitOfWork.Notification.Add(notification);

                    await _unitOfWork.CommitAsync();
                    return true;
                }
                catch
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }
            }
        }
        public async Task<bool> DeleteReactAsync(string reactId)
        {
            var user = await _userHelpers.GetCurrentUserAsync();
            if (user == null) throw new Exception("user not found");
            var react = await _unitOfWork.React.FindFirstAsync(l => l.Id == reactId);
            if (react == null) throw new Exception("like doesnt exist");
            if (react.UserId != user.Id) throw new UnauthorizedAccessException("cant update some one comment");

                _unitOfWork.React.Remove(react);
                return await _unitOfWork.SaveAsync()>0;
        }


        #endregion
    }
}
