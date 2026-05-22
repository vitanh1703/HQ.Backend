using HQ.Backend.Data;
using HQ.Backend.DTOs;
using HQ.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HQ.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuppliersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Suppliers
        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Select(s => new SupplierDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Phone = s.Phone,
                        Address = s.Address
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách nhà cung cấp", error = ex.Message });
            }
        }

        // POST: api/Suppliers
        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var supplier = new Supplier
                {
                    Name = request.Name,
                    Phone = request.Phone,
                    Address = request.Address
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                var supplierDto = new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Phone = supplier.Phone,
                    Address = supplier.Address
                };

                return CreatedAtAction(nameof(GetSuppliers), new { id = supplier.Id }, supplierDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo nhà cung cấp", error = ex.Message });
            }
        }

        // PUT: api/Suppliers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Nhà cung cấp không tìm thấy" });
                }

                supplier.Name = request.Name;
                supplier.Phone = request.Phone;
                supplier.Address = request.Address;

                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();

                var supplierDto = new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Phone = supplier.Phone,
                    Address = supplier.Address
                };

                return Ok(supplierDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật nhà cung cấp", error = ex.Message });
            }
        }

        // DELETE: api/Suppliers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Nhà cung cấp không tìm thấy" });
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa nhà cung cấp thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa nhà cung cấp", error = ex.Message });
            }
        }
    }
}
