using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();

            return View(objCategoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if(obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name,", "Name and Display Order cannot be the same.");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(obj);
                _unitOfWork.Save();
                TempData["Success"] = "Category Created Successfully.";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if(id==null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);

            if (categoryFromDbFirst == null)
            {
                return NotFound();
            }

            return View(categoryFromDbFirst);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name,", "Name and Display Order cannot be the same.");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(obj);
                _unitOfWork.Save();
                TempData["Success"] = "Category Updated Successfully.";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);

            if (categoryFromDbFirst == null)
            {
                return NotFound();
            }

            return View(categoryFromDbFirst);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
            if(categoryFromDbFirst == null)
            {
                return NotFound();
            }
            _unitOfWork.Category.Remove(categoryFromDbFirst);
            _unitOfWork.Save();
            TempData["Success"] = "Category Deleted Successfully.";
            return RedirectToAction("Index");
        }
    }
}
