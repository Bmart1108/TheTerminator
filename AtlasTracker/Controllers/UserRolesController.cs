using AtlasTracker.Extensions;
using AtlasTracker.Models;
using AtlasTracker.Models.ViewModels;
using AtlasTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AtlasTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRolesController : Controller
    {


        private readonly IBTCompanyInfoService _companyInfoService;
        private readonly IBTRolesService _rolesService;

        public UserRolesController(IBTCompanyInfoService companyInfoService,
                                   IBTRolesService rolesService)

        {

            _companyInfoService = companyInfoService;
            _rolesService = rolesService;
        }



        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageUserRoles()
        {
            //1-Add an instance of the ViewModel as a List (model)
            List<ManageUserRolesViewModel> model = new();

            //2-Get CompanyId
            int companyId = User.Identity!.GetCompanyId();

            //3-Loop over the users to populate the ViewModel

            List<BTUser> users = await _companyInfoService.GetAllMembersAsync(companyId);

            foreach (BTUser user in users)
            {
                ManageUserRolesViewModel viewmodel = new();
                viewmodel.BTUser = user;
                IEnumerable<string> selected = await _rolesService.GetUserRolesAsync(user);
                viewmodel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", selected);

                model.Add(viewmodel);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel member)
        {
            int companyId = User.Identity!.GetCompanyId();

            //Instantiate the BTUser
            BTUser? btUser = (await _companyInfoService.GetAllMembersAsync(companyId))
                                                       .FirstOrDefault(u => u.Id == member.BTUser?.Id);
            //Get Roles for the User
            IEnumerable<string> roles = await _rolesService.GetUserRolesAsync(btUser!);

            //Get Selected Roles for the User
            string userRole = member.SelectedRoles?.FirstOrDefault()!;

            if (!string.IsNullOrEmpty(userRole))
            {
                //Remove User from their Roles
                if (await _rolesService.RemoveUserFromRolesAsync(btUser!, roles))
                {

                }
                //Add User to a Role
                await _rolesService.AddUserToRoleAsync(btUser!, userRole);
            }
            //Navigate back to the View
            return RedirectToAction(nameof(ManageUserRoles));
        }
    }
}