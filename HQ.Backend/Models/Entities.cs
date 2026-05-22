using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HQ.Backend.Models;

[Table("users")]
public class User
{
    [Key]
    public int Id { get; set; }
    [Column("password")]
    public string Password { get; set; } = null!;
    [Column("email")]
    public string Email { get; set; } = null!;
    [Column("role")]
    public string Role { get; set; } = "Customer";
    [Column("auth_provider")]
    public string AuthProvider { get; set; } = "local";
    [Column("google_id")]
    public string? GoogleId { get; set; }
    [Column("full_name")]
    public string FullName { get; set; } = null!;
    [Column("phone")]
    public string? Phone { get; set; }
    [Column("address")]
    public string? Address { get; set; }
    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }
    [Column("status")]
    public bool Status { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

[Table("categories")]
public class Category
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

[Table("products")]
public class Product
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    [Column("brand_text")]
    public string BrandText { get; set; } = "H&Q";
    [Column("image_url")]
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    [Column("category_id")]
    public int? CategoryId { get; set; }
    [Column("supplier_id")]
    public int? SupplierId { get; set; }
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

[Table("reviews")]
public class Review
{
    [Key]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("product_variants")]
public class ProductVariant
{
    [Key]
    public int Id { get; set; }
    [Column("product_id")]
    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
    public string Size { get; set; } = null!;
    public string Color { get; set; } = null!;
    public decimal Price { get; set; }
    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

     public string? Sku { get; set; }
}

[Table("carts")]
public class Cart
{
    [Key]
    public int Id { get; set; }
    [Column("user_id")]
    public int UserId { get; set; }
}

[Table("cart_items")]
public class CartItem
{
    [Key]
    public int Id { get; set; }
    [Column("cart_id")]
    public int CartId { get; set; }
    [Column("variant_id")]
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

[Table("news")]
public class News
{
    [Key]
    public int Id { get; set; }
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    [Column("publish_date")]
    public DateTime PublishDate { get; set; }
    [Column("img_url")]
    public string? ImgUrl { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[Table("services")]
public class Service
{
    public int Id { get; set; } 

    [Column("icon_name")]
    public string IconName { get; set; } 

    public string Title { get; set; } 

    public string Description { get; set; } 

    [Column("order_index")]
    public int OrderIndex { get; set; } 
}

public class Promotion
{
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("discount_value")]
    public decimal DiscountValue { get; set; }

    [Column("discount_type")]
    public string DiscountType { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    public int Status { get; set; } 
}

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("full_name")] 
    public string FullName { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("phone")]
    public string Phone { get; set; }

    [Column("address")]
    public string Address { get; set; }

    [Column("order_code")]
    public string OrderCode { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("order_date")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }
}

[Table("order_items")]
public class OrderItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("variant_id")]
    public int VariantId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price_at_purchase")]
    public decimal PriceAtPurchase { get; set; }
    public Order Order { get; set; }
}

[Table("suppliers")]
public class Supplier
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }
}

[Table("promotions")] // THÊM DÒNG NÀY (Đảm bảo tên khớp với DB MySQL)
public class PromotionEntity
{
    [Key] // Nên thêm cái này để EF biết đâu là khóa chính
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("discount_value")]
    public decimal DiscountValue { get; set; }

    [Column("discount_type")]
    public string DiscountType { get; set; } = null!;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    public int Status { get; set; } 
}
