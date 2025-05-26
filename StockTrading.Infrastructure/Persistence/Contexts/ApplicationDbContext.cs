using Microsoft.EntityFrameworkCore;
using StockTrading.Domain.Entities;
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
                .HasMaxLength(50)
                .HasDefaultValue("User");

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
    }
}