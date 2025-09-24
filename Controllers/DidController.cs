using AspNetCoreHero.ToastNotification.Abstractions;
using KBN.Models;
using KBN.RepoHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KBN.Controllers
{
    [Authorize]
    public class DidController : Controller
    {
        private readonly DIDHelper _didhelper;
        private readonly INotyfService _notyf;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DidController(
            DIDHelper didhelper,
            INotyfService notyf,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _didhelper = didhelper;
            _notyf = notyf;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public ActionResult DIDs()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddDidPartial()
        {
            return PartialView();   
        }

        [HttpPost]
        public async Task<IActionResult> SaveDIDs([FromBody] List<DIDEntry> tableData)
        {
            var user = await _userManager.GetUserAsync(User);
            _didhelper.AddDids(tableData, user.Email);
            return Json("Addded successfully");
        }

        [HttpPost]
        public IActionResult GenerateTempTable([FromBody] List<string> lines)
        {
            return PartialView(lines);
        }

        [HttpGet]
        public IActionResult UpdatePartial(int id)
        {
            _notyf.Success("Success");
            return PartialView(_didhelper.GetDetailsToUpdate(id));
        }

        [HttpPut]
        public IActionResult Update(UpdateDidModal formData)
        {
            _didhelper.Update(formData);
            return Json("Updated Successfully");
        }

        [HttpPost]
        public IActionResult GetPaginatedPage(PaginationModal modal)
        {
            ViewBag.totalDIDs = _didhelper.GetTotal();
            return PartialView(_didhelper.GetPaginatedPage(modal));
        }

        [HttpDelete]
        public IActionResult Delete(double id)
        {
            _didhelper.Delete(id);
            _notyf.Success("Success");
            return Json("Deleted Successfully");
        }
    }
}
