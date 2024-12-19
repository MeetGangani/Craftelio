using Craftelio.Models;
using System.Collections.Generic;

namespace Craftelio.Models.ViewModels
{
    public class CategoryViewModel
    {
        public Category Category { get; set; }
        public IEnumerable<Product> Products { get; set; }
    }
}