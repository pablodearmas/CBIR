using CBIR.CV;
using CBIR.Data;
using CBIR.ML;
using CBIR.Model;
using CBIR.WebApp.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CBIR.WebApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImagesController : ControllerBase
    {
        private ImagesDbContext dbContext;
        private readonly IWebHostEnvironment environment;

        public ImagesController(ImagesDbContext db, IWebHostEnvironment env)
        {
            dbContext = db;
            environment = env;
        }

        [HttpGet("Image")]
        public IActionResult GetImage(string filename)
        {
            return PhysicalFile(filename, "image/jpeg");
        }

        [HttpGet("ByKey")]
        public IEnumerable<RelevantImageDto> GetByKey(string keys, bool strict)
        {
            if (!string.IsNullOrEmpty(keys))
            {
                var qryCategories = keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var categories = dbContext.Categories.Include(x => x.Images);
                foreach (var x in categories)
                    foreach (var c in qryCategories)
                        if (strict && x.Name == c || !strict && x.Name.Contains(c))
                            foreach (var i in x.Images)
                            {
                                yield return new RelevantImageDto()
                                {
                                    Category = x.Name,
                                    Filename = $"/images/image?filename={i.ExternalFile}",
                                    RelevanceText = "100%"
                                };
                            }
            }
        }

        [HttpGet("ByImage")]
        public IEnumerable<RelevantImageDto> GetByImage(string queryImgName, bool strict, ImageComparisonMode mode, double threshold, ImageKeypointsDetector detector, int max)
        {
            if (!string.IsNullOrEmpty(queryImgName))
            {
                var queryFeatures = new ImageFeatures(queryImgName);

                if (mode == ImageComparisonMode.Hashes)
                {
                    if (!strict)
                    {
                        var images = dbContext.Images.Include(x => x.Categories);

                        var counter = 0;
                        foreach (var img in images)
                        {
                            using (var imgFeatures = new ImageFeatures(img.Hash1, img.Hash2))
                            {
                                (var pdist, var cmdist) = ImageFeatures.GetHashesDistance(queryFeatures, imgFeatures);
                                if (pdist <= threshold || cmdist <= threshold)
                                {
                                    var relevance1 = (threshold - pdist) / threshold;
                                    var relevance2 = (threshold - cmdist) / threshold;
                                    
                                    double relevance;
                                    string relevanceText;
                                    if (relevance1 >= relevance2)
                                    {
                                        relevance = relevance1 * 100;
                                        relevanceText = $"{relevance:0.00}% (Perceptual)";
                                    }
                                    else
                                    {
                                        relevance = relevance2 * 100;
                                        relevanceText = $"{relevance:0.00}% (Color Moment)";
                                    }
                                            
                                    yield return new RelevantImageDto()
                                    {
                                        Category = img.Categories.First().Name,
                                        Filename = $"/images/image?filename={img.ExternalFile}",
                                        Relevance = relevance,
                                        RelevanceText = relevanceText
                                    };
                                    ++counter;
                                }
                            }
                            if (max > 0 && counter >= max)
                                yield break;
                        }
                    }
                    else
                    {
                        var images = dbContext.Images
                                .Include(x => x.Categories)
                                .Where(x => x.Hash1 == queryFeatures.PerceptualHash || x.Hash2 == queryFeatures.ColorMomentHash);
                        var counter = 0;
                        foreach (var img in images)
                        {
                            yield return new RelevantImageDto()
                            {
                                Category = img.Categories.First().Name,
                                Filename = $"/images/image?filename={img.ExternalFile}",
                                Relevance = 100,
                                RelevanceText = "100%"
                            };
                            ++counter;
                            if (max > 0 && counter >= max)
                                yield break;
                        }
                    }

                }
                else
                {
                    var images = dbContext.Images
                            .Include(x => x.Categories);

                    var counter = 0;
                    foreach (var img in images)
                    {
                        using (var imgFeatures = new ImageFeatures(img.ExternalFile, GetDescriptorType(detector)))
                        {
                            var dist = queryFeatures.GetDescriptorDistance(imgFeatures);
                            if (strict)
                            {
                                if (dist == 0)
                                {
                                    yield return new RelevantImageDto()
                                    {
                                        Category = img.Categories.First().Name,
                                        Filename = $"/images/image?filename={img.ExternalFile}",
                                        Relevance = 100,
                                        RelevanceText = $"100%"
                                    };
                                    ++counter;
                                }
                            }
                            else
                            {
                                if (0 <= dist && (threshold == 0 || dist <= threshold))
                                {
                                    double relevance;
                                    string relevanceText;
                                    if (threshold == 0)
                                    {
                                        relevance = -dist;
                                        relevanceText = $"{dist} (abs)";
                                    }
                                    else
                                    {
                                        relevance = 100 * (threshold - dist) / threshold;
                                        relevanceText = $"{relevance:0.00}%";
                                    }

                                    yield return new RelevantImageDto()
                                    {
                                        Category = img.Categories.First().Name,
                                        Filename = $"/images/image?filename={img.ExternalFile}",
                                        Relevance = relevance,
                                        RelevanceText = relevanceText
                                    };
                                    ++counter;
                                }
                            }
                        }
                        if (max > 0 && counter >= max)
                            yield break;
                    }
                }
            }
        }

        private ImageDescriptorType GetDescriptorType(ImageKeypointsDetector detector)
        {
            switch (detector) 
            {
                case ImageKeypointsDetector.Brisk:
                    return ImageDescriptorType.Brisk;

                case ImageKeypointsDetector.Orb:
                    return ImageDescriptorType.Orb;

                case ImageKeypointsDetector.Sift:
                    return ImageDescriptorType.Sift;

                case ImageKeypointsDetector.Fast:
                    return ImageDescriptorType.Fast;

                case ImageKeypointsDetector.SimpleBlob:
                    return ImageDescriptorType.SimpleBlob;

                case ImageKeypointsDetector.Default:
                default:
                    return ImageDescriptorType.Default;
            }
        }

        [HttpPost("Import")]
        public async Task<IActionResult> ImportImage([FromBody] CategorizedImageDto imageDto)
        {
            var fileName = imageDto.Filename;
            var categoryName = imageDto.Category;
            var message = string.Empty;

            try
            {
                var alreadyInDatabase = await dbContext.Images
                        .Include(x => x.Categories)
                        .SingleOrDefaultAsync(x => x.ExternalFile == fileName);
                if (alreadyInDatabase != null)
                {
                    if (alreadyInDatabase.Categories.Any(x => x.Name == categoryName))
                    {
                        return StatusCode(409, $"\tImage {fileName} is already on the database");
                    }
                }

                var features = new ImageFeatures(fileName);


                if (alreadyInDatabase == null)
                {
                    alreadyInDatabase = await dbContext.Images
                        .Include(x => x.Categories)
                        .SingleOrDefaultAsync(x => x.Hash1 == features.PerceptualHash);
                    if (alreadyInDatabase != null)
                    {
                        message = $"\tImage {fileName} is already on the database (phash): {alreadyInDatabase.ExternalFile}";
                        if (alreadyInDatabase.Categories.Any(x => x.Name == categoryName))
                            return StatusCode(409, message);
                    }
                }

                if (alreadyInDatabase == null)
                {
                    alreadyInDatabase = await dbContext.Images
                       .Include(x => x.Categories)
                        .SingleOrDefaultAsync(x => x.Hash2 == features.ColorMomentHash);
                    if (alreadyInDatabase != null)
                    {
                        message = $"\tImage {fileName} is already on the database (cmhash): {alreadyInDatabase.ExternalFile}";
                        if (alreadyInDatabase.Categories.Any(x => x.Name == categoryName))
                            return StatusCode(409, message);
                    }
                }

                var category = await dbContext.Categories.SingleOrDefaultAsync(x => x.Name == categoryName);
                if (category == null)
                    category = new Category() { Name = categoryName, Images = new List<Image>() };

                var image = alreadyInDatabase ?? new Image()
                {
                    ExternalFile = fileName,
                    Hash1 = features.PerceptualHash,
                    Hash2 = features.ColorMomentHash,
                    Categories = new List<Category>()
                };
                image.Categories.Add(category);

                if (category.Id != Guid.Empty)
                    dbContext.Update(category);
                else
                    await dbContext.AddAsync(category);

                if (image.Id != Guid.Empty)
                    dbContext.Update(image);
                else
                    dbContext.Add(image);

                await dbContext.SaveChangesAsync();

                return StatusCode(200, message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("UploadImageQry")]
        public async Task<IActionResult> PostImage()
        {
            try
            {
                var file = HttpContext.Request.Form.Files.SingleOrDefault();
                if (file != null)
                {
                    var filename = Path.GetTempFileName();
                    using (var stream = new FileStream(filename, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                        return StatusCode(200, $"/images/image?filename={filename}");
                    }
                }
                return StatusCode(204); //No content
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("PredictedCategory")]
        public async Task<PredictedCategory> GetPredictedCategory(string queryImgName)
        {
            if (!System.IO.File.Exists(queryImgName))
                return null;

            var imgClassifier = new ImageClassifier();
            var inceptionFolder = Path.Combine(environment.ContentRootPath, "Inception");
            var modelPath = Path.Combine(inceptionFolder, "trained-model.zip");
            await Task.Run(() => imgClassifier.LoadModel(modelPath));

            var imagesFolder = Path.Combine(environment.ContentRootPath, "Images");
            var result = imgClassifier.ClassifyImage(queryImgName);

            return new PredictedCategory()
            {
                Label = result.PredictedLabelValue,
                Score = result.Score.Max()
            };
        }

        [HttpGet("SampleImages")]
        public IEnumerable<ImageDto> GetSampleImages()
        {
            var imagesFolder = Path.Combine(environment.ContentRootPath, "Images");
            var result = Directory
                    .EnumerateFiles(imagesFolder)
                    .Select(x => new ImageDto() { Filename = $"/images/image?filename={x}" });
            return result;
        }

        [HttpGet("About")]
        public string GetAbout()
        {
            return "CBIR Images Controller - Copyright (C) Pablo Antonio de Armas Suárez - C511";
        }

    }
}
