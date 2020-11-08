﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrickingLibrary.Api.Form;
using TrickingLibrary.Api.ViewModels;
using TrickingLibrary.Data;
using TrickingLibrary.Models;
using TrickingLibrary.Models.Moderation;

namespace TrickingLibrary.Api.Controllers
{
    [Route("api/tricks")]
    public class TricksController : ApiController
    {
        private readonly AppDbContext _ctx;

        public TricksController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public IEnumerable<object> All() => _ctx.Tricks
            .AsNoTracking()
            .Where(x => x.Active)
            .Include(x => x.TrickCategories)
            .Include(x => x.Progressions)
            .Include(x => x.Prerequisites)
            .Include(x => x.User)
            .Select(TrickViewModels.UserProjection).ToList();

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            var query = _ctx.Tricks.AsQueryable();

            if (int.TryParse(id, out var intId))
            {
                query = query.Where(x => x.Id == intId);
            }
            else
            {
                query = query.Where(x => x.Slug.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                                         && x.Active);
            }

            var trick = query
                .Include(x => x.TrickCategories)
                .Include(x => x.Progressions)
                .Include(x => x.Prerequisites)
                .Include(x => x.User)
                .Select(TrickViewModels.FullProjection)
                .FirstOrDefault();

            if (trick == null)
            {
                return NoContent();
            }

            return Ok(trick);
        }

        [HttpGet("{trickId}/submissions")]
        public IEnumerable<object> ListSubmissionsForTrick(string trickId, [FromQuery] FeedQuery feedQuery)
        {
            return _ctx.Submissions
                .Include(x => x.Video)
                .Include(x => x.User)
                .Where(x => x.TrickId.Equals(trickId, StringComparison.InvariantCultureIgnoreCase))
                .OrderFeed(feedQuery)
                .Select(SubmissionViewModels.PerspectiveProjection(UserId))
                .ToList();
        }

        [HttpPost]
        [Authorize]
        public async Task<object> Create([FromBody] CreateTrickForm createTrickForm)
        {
            var trick = new Trick
            {
                Slug = createTrickForm.Name.Replace(" ", "-").ToLowerInvariant(),
                Name = createTrickForm.Name,
                Version = 1,
                Description = createTrickForm.Description,
                Difficulty = createTrickForm.Difficulty,
                Prerequisites = createTrickForm.Prerequisites
                    .Select(x => new TrickRelationship {PrerequisiteId = x})
                    .ToList(),
                Progressions = createTrickForm.Progressions
                    .Select(x => new TrickRelationship {ProgressionId = x})
                    .ToList(),
                TrickCategories = createTrickForm.Categories
                    .Select(x => new TrickCategory {CategoryId = x})
                    .ToList(),
                UserId = UserId,
            };
            _ctx.Add(trick);
            await _ctx.SaveChangesAsync();
            _ctx.Add(new ModerationItem
            {
                Target = trick.Id,
                Type = ModerationTypes.Trick,
                UserId = UserId,
            });
            await _ctx.SaveChangesAsync();
            return TrickViewModels.Create(trick);
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update([FromBody] UpdateTrickForm createTrickForm)
        {
            var trick = _ctx.Tricks.FirstOrDefault(x => x.Id == createTrickForm.Id);
            if (trick == null)
            {
                return NoContent();
            }

            var newTrick = new Trick
            {
                Slug = trick.Slug,
                Name = trick.Name,
                Version = trick.Version + 1,
                Description = createTrickForm.Description,
                Difficulty = createTrickForm.Difficulty,
                Prerequisites = createTrickForm.Prerequisites
                    .Select(x => new TrickRelationship {PrerequisiteId = x})
                    .ToList(),
                Progressions = createTrickForm.Progressions
                    .Select(x => new TrickRelationship {ProgressionId = x})
                    .ToList(),
                TrickCategories = createTrickForm.Categories
                    .Select(x => new TrickCategory {CategoryId = x})
                    .ToList(),
                UserId = UserId,
            };

            _ctx.Add(newTrick);
            await _ctx.SaveChangesAsync();
            _ctx.Add(new ModerationItem
            {
                Current = trick.Id,
                Target = newTrick.Id,
                Type = ModerationTypes.Trick,
                // todo validation for reason
                Reason = createTrickForm.Reason,
                UserId = UserId,
            });
            await _ctx.SaveChangesAsync();

            // todo redirect to the mod item instead of returning the trick
            return Ok(TrickViewModels.Create(newTrick));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var trick = _ctx.Tricks.FirstOrDefault(x => x.Slug.Equals(id));
            trick.Deleted = true;
            await _ctx.SaveChangesAsync();
            return Ok();
        }
    }
}