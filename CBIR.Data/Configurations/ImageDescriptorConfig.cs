using CBIR.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.Data.Configurations
{
    public class ImageDescriptorConfig : IEntityTypeConfiguration<ImageDescriptor>
    {
        public void Configure(EntityTypeBuilder<ImageDescriptor> builder)
        {
            builder.ToTable("ImageDescriptors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ImageId)
                .IsRequired();
            builder.HasIndex(x => x.ImageId);

            builder.Property(x => x.Rows)
                .IsRequired();

            builder.Property(x => x.Cols)
                .IsRequired();

            builder.Property(x => x.Depth)
                .IsRequired();

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.Data)
                .IsRequired(false);

            builder.HasOne(x => x.Image)
                .WithMany(x => x.Descriptors)
                .IsRequired()
                .HasForeignKey(x => x.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
