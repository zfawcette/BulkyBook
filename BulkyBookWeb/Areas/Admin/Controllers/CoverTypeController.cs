using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverTypeList = _unitOfWork.CoverType.GetAll();
            return View(objCoverTypeList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }
            _unitOfWork.CoverType.Add(obj);
            _unitOfWork.Save();
            TempData["Success"] = "Cover Type Created Successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int? id)
        {
            if(id==null || id == 0)
            {
                return NotFound();
            }
            var coverTypeFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefault(u=>u.Id == id);
            if (coverTypeFromDbFirst == null)
            {
                return NotFound();
            }
            return View(coverTypeFromDbFirst);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            _unitOfWork.CoverType.Update(obj);
            _unitOfWork.Save();
            TempData["Success"] = "Cover Type Updated Successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            }
            var coverTypeFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefault(u=>u.Id==id);
            if(coverTypeFromDbFirst == null)
            {
                return NotFound();
            }
            return View(coverTypeFromDbFirst);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var coverTypeFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if(coverTypeFromDbFirst == null)
            {
                return NotFound();
            }
            _unitOfWork.CoverType.Remove(coverTypeFromDbFirst);
            _unitOfWork.Save();
            TempData["Success"] = "Cover Type Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}
