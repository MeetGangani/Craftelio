using Craftelio.DataAccess;
using Craftelio.DataAccess.Repository.IRepository;
using Craftelio.Models;
using Craftelio.Models.ViewModels;
using Craftelio.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CraftelioWeb.Controllers;
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IUnitOfWork unitOfWork,
        IWebHostEnvironment hostEnvironment,
        ILogger<ProductController> logger)
    {
        _unitOfWork = unitOfWork;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            _logger.LogInformation("Starting Index action in ProductController");

            // Get categories first
            var categories = _unitOfWork.Category.GetAll().ToList();
            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("No categories found in database");
                TempData["warning"] = "No categories found in the system.";
                return View(new List<Product>());
            }

            // Get all products with their categories
            var products = _unitOfWork.Product
                .GetAll(includeProperties: "Category")
                ?.ToList() ?? new List<Product>();

            _logger.LogInformation($"Successfully retrieved {products.Count} products");

            // Store categories in ViewBag for dropdown
            ViewBag.Categories = categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

            return View(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Product/Index action");
            TempData["error"] = "An error occurred while loading products. Please try again later.";
            return View(new List<Product>());
        }
    }

    public IActionResult Upsert(int? id)
    {
        try
        {
            ProductVM productVM = new()
            {
                Product = new(),
                CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            if (id == null || id == 0)
            {
                return View(productVM);
            }

            productVM.Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (productVM.Product == null)
            {
                _logger.LogWarning($"Product with ID {id} not found");
                TempData["error"] = "Product not found";
                return RedirectToAction(nameof(Index));
            }

            return View(productVM);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in Upsert action with id: {id}");
            TempData["error"] = "An error occurred while loading the product.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(ProductVM obj, IFormFile? file)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            string wwwRootPath = _hostEnvironment.WebRootPath;
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"images\products");
                var extension = Path.GetExtension(file.FileName);

                // Ensure directory exists
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(obj.Product.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                {
                    file.CopyTo(fileStreams);
                }
                obj.Product.ImageUrl = @"\images\products\" + fileName + extension;
            }

            if (obj.Product.Id == 0)
            {
                _unitOfWork.Product.Add(obj.Product);
                TempData["success"] = "Product created successfully";
            }
            else
            {
                _unitOfWork.Product.Update(obj.Product);
                TempData["success"] = "Product updated successfully";
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Upsert Post action");
            TempData["error"] = "An error occurred while saving the product.";
            return View(obj);
        }
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return Json(new { data = productList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAll API call");
            return Json(new { error = "An error occurred while fetching products." });
        }
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        try
        {
            if (id == null)
            {
                return Json(new { success = false, message = "Invalid product ID" });
            }

            var obj = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting product with ID: {id}");
            return Json(new { success = false, message = "An error occurred while deleting the product" });
        }
    }
    #endregion
}