using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class UrlRecordConfiguration : IEntityTypeConfiguration<UrlRecord>
{
    public void Configure(EntityTypeBuilder<UrlRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.ShortCode)
            .IsRequired()
            .HasMaxLength(16)
            // ascii_bin гарантирует Case-Sensitive (регистрозависимое) сравнение, 
            // необходимое для Base62, и использует строго 1 байт на символ, 
            // что радикально уменьшает размер B-Tree индекса в MariaDB.
            .UseCollation("ascii_bin")
            .HasColumnType("varchar(16)");

        builder.HasIndex(x => x.ShortCode)
            .IsUnique();
        
        // Для ускорения аналитических запросов и выборок в UI (например, сортировка по дате)
        builder.HasIndex(x => x.CreatedAt);
    }
}