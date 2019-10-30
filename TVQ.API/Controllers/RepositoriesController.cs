﻿using Genometric.TVQ.API.Crawlers;
using Genometric.TVQ.API.Infrastructure;
using Genometric.TVQ.API.Infrastructure.BackgroundTasks;
using Genometric.TVQ.API.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Genometric.TVQ.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RepositoriesController : ControllerBase
    {
        private readonly TVQContext _context;
        private readonly IBackgroundToolRepoCrawlingQueue _queue;
        private readonly ILogger<RepositoriesController> _logger;

        public RepositoriesController(
            TVQContext context,
            IBackgroundToolRepoCrawlingQueue queue,
            ILogger<RepositoriesController> logger)
        {
            _context = context;
            _queue = queue;
            _logger = logger;
        }

        // GET: api/v1/repositories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Repository>>> GetRepos()
        {
            return await _context.Repositories.Include(repo => repo.Tools).ToListAsync().ConfigureAwait(false);
        }

        // GET: api/v1/repositories/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRepo([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var DataItem = await _context.Repositories.FindAsync(id);
            if (DataItem == null)
                return NotFound();

            return Ok(DataItem);
        }

        // PUT: api/v1/repositories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRepo([FromRoute] int id, [FromBody] Repository dataItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dataItem.ID)
                return BadRequest();

            _context.Entry(dataItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RepoExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/v1/repositories
        [HttpPost]
        public async Task<IActionResult> PostRepo([FromBody] Repository dataItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Repositories.Add(dataItem);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return CreatedAtAction("GetRequestItems", new { }, dataItem);
        }

        // DELETE: api/v1/repositories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRepo([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dataItem = await _context.Repositories.FindAsync(id);
            if (dataItem == null)
                return NotFound();

            _context.Repositories.Remove(dataItem);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return Ok(dataItem);
        }

        // GET: api/v1/repositories/scan/1
        [HttpGet("{id}/scan")]
        public async Task<IActionResult> ScanToolsInRepo([FromRoute] int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var repository = await 
                _context.Repositories
                .Include(repo => repo.Tools)
                    .ThenInclude(tool => tool.Downloads)
                .Include(repo => repo.Tools)
                    .ThenInclude(tool => tool.Publications)
                .FirstAsync(x=>x.ID == id)
                .ConfigureAwait(false);

            if (repository == null)
                return NotFound();

            _queue.QueueBackgroundWorkItem(repository);

            return Ok(repository);
        }

        private bool RepoExists(int id)
        {
            return _context.Repositories.Any(e => e.ID == id);
        }
    }
}