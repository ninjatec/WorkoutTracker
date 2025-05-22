using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.VersionManagement;

namespace WorkoutTrackerWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class VersionsController : Controller
    {
        private readonly IVersionService _versionService;

        public VersionsController(IVersionService versionService)
        {
            _versionService = versionService;
        }

        // GET: Admin/Versions
        public async Task<IActionResult> Index()
        {
            var versions = await _versionService.GetVersionHistoryAsync();
            return View(versions);
        }

        // GET: Admin/Versions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var versions = await _versionService.GetVersionHistoryAsync();
            var version = versions.FirstOrDefault(m => m.VersionId == id);
            
            if (version == null)
            {
                return NotFound();
            }

            return View(version);
        }

        // GET: Admin/Versions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Versions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Major,Minor,Patch,BuildNumber,Description,GitCommitHash,ReleaseNotes")] AppVersion version)
        {
            if (ModelState.IsValid)
            {
                await _versionService.AddVersionHistoryAsync(
                    version.Major,
                    version.Minor,
                    version.Patch,
                    version.BuildNumber,
                    version.Description,
                    version.GitCommitHash,
                    version.ReleaseNotes
                );
                
                return RedirectToAction(nameof(Index));
            }
            return View(version);
        }

        // GET: Admin/Versions/SetCurrent/5
        public async Task<IActionResult> SetCurrent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var versions = await _versionService.GetVersionHistoryAsync();
            var version = versions.FirstOrDefault(m => m.VersionId == id);
            
            if (version == null)
            {
                return NotFound();
            }

            await _versionService.UpdateVersionAsync(
                version.Major,
                version.Minor,
                version.Patch,
                version.BuildNumber,
                version.Description,
                version.GitCommitHash
            );

            return RedirectToAction(nameof(Index));
        }
    }
}