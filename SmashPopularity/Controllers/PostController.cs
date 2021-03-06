﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmashPopularity.Data;
using SmashPopularity.Data.Models;
using SmashPopularity.Models.Post;
using SmashPopularity.Models.Reply;

namespace SmashPopularity.Controllers
{
    public class PostController : Controller
    {
        private readonly IPost _postService;
        private readonly IForum _forumService;

        private static UserManager<ApplicationUser> _userManager;

        public PostController(IPost postService, IForum forumService, UserManager<ApplicationUser> userManager)
        {
            _postService = postService;
            _forumService = forumService;
            _userManager = userManager;
        }

        public IActionResult Index(int id)
        {
            var post = _postService.GetByID(id);

            var replies = BuildPostReplies(post.Replies);

            var model = new PostIndexModel
            {
                ID = post.ID,
                Title = post.Title,
                AuthorID = post.User.Id,
                AuthorName = post.User.UserName,
                AuthorImageUrl = post.User.ProfileImageUrl,
                AuthorRating = post.User.Rating,
                Created = post.Created,
                PostContent = post.Content,
                Replies = replies,
                ForumID = post.Forum.ID,
                ForumName = post.Forum.Title,
                IsAuthorAdmin = IsAuthorAdmin(post.User),
            };

            return View(model);
        }

        public IActionResult Create(int id)
        {
            // Note id is Forum.id
            var forum = _forumService.GetById(id);

            var model = new NewPostModel
            {
                ForumName = forum.Title,
                ForumID = forum.ID,
                ForumImageUrl = forum.ImageUrl,
                AuthorName = User.Identity.Name,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddPost(NewPostModel model)
        {
            var userId = _userManager.GetUserId(User);
            var user = _userManager.FindByIdAsync(userId).Result;
            var post = BuildPost(model, user);

            _postService.Add(post).Wait(); //Block current thread until the task is complete.
            // TODO Implement User Rating Management

            return RedirectToAction("Index", "Post", new { id = post.ID });
        }

        private bool IsAuthorAdmin(ApplicationUser user)
        {
            return _userManager.GetRolesAsync(user)
                .Result.Contains("Admin");
        }

        private Post BuildPost(NewPostModel model, ApplicationUser user)
        {
            var forum = _forumService.GetById(model.ForumID);

            return new Post
            {
                Title = model.Title,
                Content = model.Content,
                Created = DateTime.Now,
                User = user,
                Forum = forum,
            };
        }

        private IEnumerable<PostReplyModel> BuildPostReplies(IEnumerable<PostReply> replies)
        {
            return replies.Select(reply => new PostReplyModel
            {
                ID = reply.ID,
                AuthorName = reply.User.UserName,
                AuthorID = reply.User.Id,
                AuthorImageUrl = reply.User.ProfileImageUrl,
                AuthorRating = reply.User.Rating,
                Created = reply.Created,
                ReplyContent = reply.Content,
                IsAuthorAdmin = IsAuthorAdmin(reply.User),
            });
        }
    }
}