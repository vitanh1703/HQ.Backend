using HQ.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] string range = "7days")
        {
            var startDate = GetStartDate(range);

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate)
                .ToListAsync();

            var customers = await _context.Users
                .Where(u => u.Role == "Customer")
                .ToListAsync();

            var products = await _context.Products.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var totalOrders = orders.Count;
            var totalCustomers = customers.Count;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var revenueTrend = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueTrendDto
                {
                    Day = g.Key.ToString("dd/MM/yyyy"),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => DateTime.ParseExact(x.Day, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var categoryDistribution = categories
                .Select(c => new CategoryDistributionDto
                {
                    Name = c.Name,
                    Value = products.Count(p => p.CategoryId == c.Id)
                })
                .ToList();

            var vipCustomers = customers
                .Select(customer =>
                {
                    var customerOrders = orders.Where(o => o.UserId == customer.Id).ToList();

                    return new VipCustomerDto
                    {
                        Name = customer.FullName ?? "Khách hàng",
                        Orders = customerOrders.Count,
                        Total = customerOrders.Sum(x => x.TotalAmount)
                    };
                })
                .Where(x => x.Orders > 0)
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            var orderStatusDistribution = new List<OrderStatusDto>
            {
                new OrderStatusDto
                {
                    Name = "Pending",
                    Value = orders.Count(o => o.Status == "Pending")
                },
                new OrderStatusDto
                {
                    Name = "Shipping",
                    Value = orders.Count(o => o.Status == "Shipping")
                },
                new OrderStatusDto
                {
                    Name = "Success",
                    Value = orders.Count(o => o.Status == "Success")
                },
                new OrderStatusDto
                {
                    Name = "Cancel",
                    Value = orders.Count(o => o.Status == "Cancel")
                }
            };

            var result = new DashboardStatisticsDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalCustomers = totalCustomers,
                AvgOrderValue = avgOrderValue,
                TotalProducts = products.Count,
                RevenueTrend = revenueTrend,
                CategoryDistribution = categoryDistribution,
                VipCustomers = vipCustomers,
                OrderStatusDistribution = orderStatusDistribution
            };

            return Ok(result);
        }

        private DateTime GetStartDate(string range)
        {
            var now = DateTime.Now;

            return range.ToLower() switch
            {
                "7days" => now.AddDays(-6).Date,
                "30days" => now.AddDays(-29).Date,
                "3months" => now.AddMonths(-3).Date,
                "thisyear" => new DateTime(now.Year, 1, 1),
                _ => now.AddDays(-6).Date
            };
        }
    }

    public class DashboardStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AvgOrderValue { get; set; }
        public int TotalProducts { get; set; }
        public List<RevenueTrendDto> RevenueTrend { get; set; } = new();
        public List<CategoryDistributionDto> CategoryDistribution { get; set; } = new();
        public List<VipCustomerDto> VipCustomers { get; set; } = new();
        public List<OrderStatusDto> OrderStatusDistribution { get; set; } = new();
    }

    public class RevenueTrendDto
    {
        public string Day { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class CategoryDistributionDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class VipCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public int Orders { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderStatusDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}