using Microsoft.EntityFrameworkCore;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Entities.Auth;
using StockTrading.Domain.Enums;
using StockTrading.Domain.Extensions;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext : DbContext
{
    private readonly IEncryptionService _encryptionService;

    public ApplicationDbContext(DbContextOptions options, IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<KisToken> KisTokens { get; set; }
    public DbSet<StockOrder> StockOrders { get; set; } = null!;
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.GoogleId)
                .HasColumnName("google_id")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasDefaultValue(UserRole.User)
                .HasConversion<int>();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(256)
                .IsRequired(false);

            entity.Property(e => e.KisAppKey)
                .HasColumnName("kis_app_key")
                .IsRequired(false)
                .HasConversion(
                    v => _encryptionService.Encrypt(v),
                    v => _encryptionService.Decrypt(v));

            entity.Property(e => e.KisAppSecret)
                .HasColumnName("kis_app_secret")
                .IsRequired(false)
                .HasConversion(
                    v => _encryptionService.Encrypt(v),
                    v => _encryptionService.Decrypt(v));

            entity.Property(e => e.AccountNumber)
                .HasColumnName("account_number")
                .IsRequired(false)
                .HasConversion(
                    v => _encryptionService.Encrypt(v),
                    v => _encryptionService.Decrypt(v));

            entity.Property(e => e.WebSocketToken)
                .HasColumnName("websocket_token")
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.HasIndex(e => e.GoogleId)
                .HasDatabaseName("ix_users_google_id");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");
        });

        modelBuilder.Entity<KisToken>(entity =>
        {
            entity.ToTable("kis_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AccessToken)
                .HasColumnName("access_token")
                .IsRequired();
            entity.Property(e => e.ExpiresIn)
                .HasColumnName("expires_in")
                .IsRequired();
            entity.Property(e => e.TokenType)
                .HasColumnName("token_type")
                .IsRequired();

            entity.HasOne(e => e.User)
                .WithOne(e => e.KisToken)
                .HasForeignKey<KisToken>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockOrder>(entity =>
        {
            entity.ToTable("stock_orders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.StockCode)
                .HasColumnName("stock_code")
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.TradeType)
                .HasColumnName("trade_type")
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.OrderType)
                .HasColumnName("order_type")
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            entity.Property(e => e.Price)
                .HasColumnName("price")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Market)
                .HasColumnName("market")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.IsScheduledOrder)
                .HasColumnName("is_scheduled_order")
                .HasDefaultValue(false);
            
            entity.Property(e => e.ScheduledExecutionTime)
                .HasColumnName("scheduled_execution_time");
            
            entity.Property(e => e.ReservedOrderNumber)
                .HasColumnName("reserved_order_number")
                .HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.ToTable("stocks");
            entity.HasKey(e => e.Code);

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(6)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.EnglishName)
                .HasColumnName("english_name")
                .HasMaxLength(200)
                .IsRequired(false);

            entity.Property(e => e.Sector)
                .HasColumnName("sector")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.StockType)
                .HasColumnName("stock_type")
                .HasMaxLength(50)
                .IsRequired(false);

            entity.Property(e => e.ParValue)
                .HasColumnName("par_value")
                .HasMaxLength(20)
                .IsRequired(false);

            entity.Property(e => e.ListedShares)
                .HasColumnName("listed_shares")
                .HasMaxLength(20)
                .IsRequired(false);

            entity.Property(e => e.ListedDate)
                .HasColumnName("listed_date")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            entity.Property(e => e.Market)
                .HasColumnName("market")
                .HasMaxLength(20)
                .HasConversion(
                    v => v.GetDescription(),
                    v => EnumExtensions.GetEnumFromDescription<Market>(v))
                .IsRequired();

            entity.Property(e => e.Market)
                .HasColumnName("market")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.LastUpdated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .HasDatabaseName("ix_stocks_name");

            entity.HasIndex(e => e.Market)
                .HasDatabaseName("ix_stocks_market");

            entity.HasIndex(e => e.Sector)
                .HasDatabaseName("ix_stocks_sector");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.IsRevoked)
                .HasColumnName("is_revoked")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<ForeignStock>(entity =>
        {
            entity.ToTable("foreign_stocks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.Symbol)
                .HasColumnName("symbol")
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.DisplaySymbol)
                .HasColumnName("display_symbol")
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(50);

            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(10);

            entity.Property(e => e.Exchange)
                .HasColumnName("exchange")
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Country)
                .HasColumnName("country")
                .HasMaxLength(10);

            entity.Property(e => e.Mic)
                .HasColumnName("mic")
                .HasMaxLength(10);

            entity.Property(e => e.LastUpdated)
                .HasColumnName("last_updated")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Symbol)
                .HasDatabaseName("ix_foreign_stocks_symbol")
                .IsUnique();

            entity.HasIndex(e => e.Description)
                .HasDatabaseName("ix_foreign_stocks_description");

            entity.HasIndex(e => e.Exchange)
                .HasDatabaseName("ix_foreign_stocks_exchange");
        });
    }
}