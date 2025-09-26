using AspNetCoreHero.ToastNotification.Abstractions;
using KBN.Models;
using KBN.RepoHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KBN.Controllers
{
    public class SubscriberController : Controller
    {
        private readonly SubscriberHelper _subHelper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotyfService _notyf;
        public SubscriberController(
            SubscriberHelper subHelper,
            UserManager<ApplicationUser> userManager,
            INotyfService notyf
        ) 
        { 
            _subHelper = subHelper;
            _userManager = userManager;
            _notyf = notyf;
        }
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SubPartial()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> AddSub(SubscriberModal modal)
        {
            if (!ModelState.IsValid)
            {
                _notyf.Error("INvalid form data");
                return Json(ModelState);
            }
            if(!_subHelper.IsUniqueUsername(modal.username))
            {
                return Json(new{ status = false});
            }
            var user = await _userManager.GetUserAsync(User);
            _subHelper.AddSubscriber(modal, user.Email);
            return Json(new {isCompleted = true});
        }

        [HttpPost]
        public IActionResult PaginatedEntries(SubscriberPaginationModal modal)
        {
            ViewBag.totalEntries = _subHelper.GetTotalEntries();
            return PartialView(_subHelper.PaginatedEntries(modal));
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            _subHelper.Delete(id);
            _notyf.Success("Deleted Successfully");
            return Json("Deleted Successfully");
        }

        [HttpGet]
        public IActionResult UpdatePartial(int id)
        {
            return PartialView(_subHelper.GetSubscriberToUpdate(id));
        }

        [HttpPost]
        public IActionResult Update(UpdateSubscriber modal)
        {
            if (!ModelState.IsValid)
            {
                _notyf.Warning("Invalid form data");
                return Json(ModelState);
            }
            if (!_subHelper.UpdateNamePassword(modal))
            {
                _notyf.Success("Updated!!!");
                return Json("Updated Successfully");
            }
            if (!_subHelper.CanUpdateCustomer(modal.customer_name, modal.id))
            {
                return Json(new{ status = false});
            }
            _subHelper.Update(modal);
            _notyf.Success("Updated!!!");
            return Json("Updated Successfully");
        }
    }
}
