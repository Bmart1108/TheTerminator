#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtlasTracker.Data;
using AtlasTracker.Models;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using AtlasTracker.Extensions;
using Microsoft.AspNetCore.Authorization;
using AtlasTracker.Models.ViewModels;
using AtlasTracker.Models.Enums;
using AtlasTracker.Services;

namespace AtlasTracker.Controllers
{
    public class ProjectsController : Controller
    {

        private readonly IBTProjectService _projectService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRolesService _rolesService;
        private readonly IBTLookupService _lookupsService;
        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly IBTTicketService _ticketService;
        private readonly IBTFileService _fileService;
        public ProjectsController(UserManager<BTUser> userManager,
                                  IBTProjectService projectService,
                                  IBTRolesService rolesService,
                                  IBTLookupService lookupsService,
                                  IBTCompanyInfoService companyInfoService,
                                  IBTTicketService ticketService,
                                  IBTFileService fileService)
        {

            _projectService = projectService;
            _userManager = userManager;
            _rolesService = rolesService;
            _lookupsService = lookupsService;
            _companyInfoService = companyInfoService;
            _ticketService = ticketService;
            _fileService = fileService;
        }




        //MyProjects
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            string userId = _userManager.GetUserId(User);
            var projects = await _projectService.GetUserProjectsAsync(userId);

            return View(projects);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> ArchivedProjects()
        {
            int companyId = User.Identity.GetCompanyId();
            List<Project> projects = await _projectService.GetArchivedProjectsByCompanyAsync(companyId);

            return View(projects);
        }

        //AllProjects
        [HttpGet]
        public async Task<IActionResult> AllProjects()
        {

            List<Project> projects = new();
            int companyId = User.Identity.GetCompanyId();

            if (User.IsInRole(nameof(BTRole.Admin)) || User.IsInRole(nameof(BTRole.ProjectManager)))
            {
                projects = await _companyInfoService.GetAllProjectsAsync(companyId);
            }
            else
            {
                projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
            }

            return View(projects);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignedProjects()
        {
            int companyId = User.Identity.GetCompanyId();

            List<Project> projects = await _projectService.GetUnassignedProjectsAsync(companyId);

            return View(projects);

        }


        //Assign Project Managers to Projects
        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> AssignPM(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId();

            AssignPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(projectId.Value, companyId);
            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PMID))
            {
                await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                return RedirectToAction(nameof(AllProjects));
            }

            return RedirectToAction(nameof(AssignPM), new { projectId = model.Project!.Id });
        }


        //Assign Project Members to Projects
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignMembers(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            ProjectMembersViewModel model = new();

            int companyId = User.Identity.GetCompanyId();

            model.Project = await _projectService.GetProjectByIdAsync(projectId.Value, companyId);

            List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(BTRole.Developer), companyId);
            List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(BTRole.Submitter), companyId);

            List<BTUser> teamMembers = developers.Concat(submitters).ToList();

            List<string> projectMembers = model.Project.Members.Select(p => p.Id).ToList();

            model.UsersList = new MultiSelectList(teamMembers, "Id", "FullName", projectMembers);

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
        {
            if (model.SelectedUsers != null)
            {
                List<string> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id))
                                                               .Select(m => m.Id).ToList();


                //Remove Current Members
                foreach (string member in memberIds)
                {
                    await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
                }

                //Add project details
                foreach (string member in model.SelectedUsers)
                {
                    await _projectService.AddUserToProjectAsync(member, model.Project.Id);
                }

                //Go To project Details
                return RedirectToAction("Details", "Projects", new { Id = model.Project.Id });
            }

            return RedirectToAction(nameof(AssignMembers), new { Id = model.Project.Id });
        }




        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }


        // GET: Projects/Create
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId();
            AddProjectWithPMViewModel model = new();

            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");
            return View(model);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
        {
            int companyId = User.Identity.GetCompanyId();

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.Project?.ImageFormFile != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }

                    model.Project.CompanyId = companyId;
                    model.Project.CreatedDate = DateTime.UtcNow;

                    model.Project.StartDate = DateTime.SpecifyKind(model.Project.StartDate.DateTime, DateTimeKind.Utc);
                    model.Project.EndDate = DateTime.SpecifyKind(model.Project.EndDate.DateTime, DateTimeKind.Utc);

                    await _projectService.AddNewProjectAsync(model.Project);

                    if (!string.IsNullOrEmpty(model.PMID))
                    {
                        await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                    }

                    return RedirectToAction(nameof(AllProjects));

                }
                catch (Exception)
                {
                    throw;
                }
            }

            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");

            return View(model);
        }


        // GET: Projects/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();


            AddProjectWithPMViewModel model = new();

            model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (model.Project == null)
            {
                return NotFound();
            }

            BTUser projectManager = await _projectService.GetProjectManagerAsync(model.Project.Id);

            if (projectManager != null)
            {
                model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName", projectManager.Id);
            }
            else
            {
                model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName");

            }

            model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");

            return View(model);
        }

        // POST: Projects/Edit/5    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                try
                {
                    if (model.Project?.ImageFormFile! != null)
                    {
                        model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }

                    //format Dates
                    model.Project.CreatedDate = DateTime.SpecifyKind(model.Project.CreatedDate.DateTime, DateTimeKind.Utc);

                    model.Project.StartDate = DateTime.SpecifyKind(model.Project.StartDate.DateTime, DateTimeKind.Utc);
                    model.Project.EndDate = DateTime.SpecifyKind(model.Project.EndDate.DateTime, DateTimeKind.Utc);


                    await _projectService.UpdateProjectAsync(model.Project!);

                    if (!string.IsNullOrEmpty(model.PMID))
                    {
                        await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);
                    }

                    return RedirectToAction("AllProjects");
                }

                catch (DbUpdateConcurrencyException)
                {

                    if (!await ProjectExists(model.Project!.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            int companyId = User.Identity.GetCompanyId();

            model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(BTRole.ProjectManager), companyId), "Id", "FullName");
            model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");

            return View(model);
        }

        // GET: Projects/ARCHIVE/5
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity.GetCompanyId();
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);



            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/ARCHIVE/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            var project = await _projectService.GetProjectByIdAsync(id, companyId);
            project.Archived = true;
            await _projectService.ArchiveProjectAsync(project);

            return RedirectToAction(nameof(AllProjects));
        }


        //GET RESTORE PROJECTS
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity.GetCompanyId();
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);



            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/RESTORE/5
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            int companyId = User.Identity.GetCompanyId();
            var project = await _projectService.GetProjectByIdAsync(id, companyId);
            project.Archived = false;

            await _projectService.RestoreProjectAsync(project);

            return RedirectToAction(nameof(AllProjects));
        }


        private async Task<bool> ProjectExists(int id)
        {
            int companyId = User.Identity.GetCompanyId();

            return (await _projectService.GetAllProjectsByCompanyAsync(companyId))
                                         .Any(p => p.Id == id);
        }

    }
}

