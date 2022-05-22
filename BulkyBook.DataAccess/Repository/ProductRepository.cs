using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product obj)
        {
            var productObj = _db.Products.FirstOrDefault(u => u.Id == obj.Id);
            if (productObj != null)
            {
                productObj.Title = obj.Title;
                productObj.Description = obj.Description;
                productObj.ISBN = obj.ISBN;
                productObj.Author = obj.Author;
                productObj.ListPrice = obj.ListPrice;
                productObj.Price = obj.Price;
                productObj.Price50 = obj.Price50;
                productObj.Price100 = obj.Price100;
                productObj.CategoryId = obj.CategoryId;
                productObj.CoverTypeId = obj.CoverTypeId;

                if(obj.ImageUrl != null)
                {
                    productObj.ImageUrl = obj.ImageUrl;
                }
            }

        }
    }
}
