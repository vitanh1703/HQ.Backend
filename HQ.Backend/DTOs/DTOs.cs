namespace HQ.Backend.DTOs;

public class GoogleLoginRequest
{
    public string Token { get; set; } = null!;
}

public class AddToCartRequest
{
    public int UserId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    public int? UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CassoWebhookRequest
{
    public int error { get; set; }
    public List<CassoTransaction> data { get; set; }
}

public class CassoTransaction
{
    public string? id { get; set; }        
    public string? tid { get; set; }      
    public string? description { get; set; }
    public decimal amount { get; set; }
    public string? when { get; set; }   
}

public class CreateOrderRequest
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemRequest> Items { get; set; }
}

public class OrderItemRequest
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

public class UpdateInfoRequest
{
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
}

public class UpdatePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class PaymentRequest
{
    public int OrderId { get; set; }
    public long Amount { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UpdateSupplierRequest
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = null!;
    public string? BrandText { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public List<CreateVariantRequest>? Variants { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = null!;
    public string? BrandText { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public List<CreateVariantRequest>? Variants { get; set; }
}

public class CreateVariantRequest
{
    public string Size { get; set; } = null!;
    public string Color { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; } = 0;
    public string? Sku { get; set; }
}

public class CreateNewsRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? ImgUrl { get; set; }
    public DateTime? PublishDate { get; set; }
}

public class UpdateNewsRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? ImgUrl { get; set; }
    public DateTime? PublishDate { get; set; }
}
