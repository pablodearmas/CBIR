using CBIR.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Data.Configurations
{
    public class ImageConfig : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Hash1)
                .IsRequired()
                .HasMaxLength(2048)
                .HasComment("Perceptual Hash");
            builder.HasIndex(x => x.Hash1);

            builder.Property(x => x.Hash2)
                .IsRequired()
                .HasMaxLength(2048)
                .HasComment("Color Moment Hash");
            builder.HasIndex(x => x.Hash2);

            builder.Property(x => x.ExternalFile)
                .IsRequired(false)
                .HasMaxLength(1024);
            builder.HasIndex(x => x.ExternalFile);

            builder.Property(x => x.Data)
                .IsRequired(false);
        }
    }
}
