using CBIR.CV;
using CBIR.Data;
using CBIR.ML;
using CBIR.Model;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pluralize.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CBIR.ImportImages
{
    class Program
    {
        IConfiguration configuration;
        IServiceProvider services;
        GlobalOptions options;

        static async Task Main(string[] args)
        {
            await new Program()
                .ConfigureServices()
                .Run(args);
        }

        private Program ConfigureServices()
        {
            var user = Environment.UserName;

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{user}.json", true, true)
                .Build();

            var sc = new ServiceCollection()
                .AddDbContext<ImagesDbContext>(opts => opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
                .AddSingleton(configuration)
                .AddSingleton<IPluralize>(new Pluralizer());

            services = sc.BuildServiceProvider();

            return this;
        }

        private T GetOptions<T>() where T : GlobalOptions => (T)options;

        private async Task Run(string[] args)
        {
            await new Parser(x =>
            {
                x.CaseInsensitiveEnumValues = true;
                x.HelpWriter = Parser.Default.Settings.HelpWriter;
            }).ParseArguments<ImportOptions, EvaluateOptions, TestByCategoryOptions, TestByImageOptions, TestByMLOptions, CompareImagesOptions>(args)
                    .MapResult(
                        (ImportOptions opts) => ImportImages(opts),
                        (EvaluateOptions opts) => EvaluateMethod(opts),
                        (TestByCategoryOptions opts) => TestCategory(opts),
                        (TestByImageOptions opts) => TestImage(opts),
                        (TestByMLOptions opts) => TestImageWithML(opts),
                        (CompareImagesOptions opts) => CompareImages(opts),
                        errs => Task.CompletedTask //Dummy Error
                    );
        }

        private Task CompareImages(CompareImagesOptions opts)
        {
            options = opts;

            var notfound = opts.ImageNames.FirstOrDefault(x => !File.Exists(x));
            if (notfound != null)
            {
                ShowMessage($"File {notfound} does not exists");
                return Task.CompletedTask;
            }

            var img1Features = new ImageFeatures(opts.ImageNames.First());
            var img2Features = new ImageFeatures(opts.ImageNames.Last());

            if (opts.UseDescripor)
            {
                var dist = ImageFeatures.GetDescriptorsDistance(img1Features, img2Features);
                ShowMessage($"dist = {dist}");
            }
            else
            {
                (var pdist, var cmdist) = ImageFeatures.GetHashesDistance(img1Features, img2Features);
                ShowMessage($"pdist = {pdist}, cmdist = {cmdist}");
            }

            return Task.CompletedTask;
        }

        private async Task TestImageWithML(TestByMLOptions opts)
        {
            options = opts;

            if (!File.Exists(opts.ImageName))
            {
                ShowMessage($"File {opts.ImageName} does not exists");
                return;
            }

            var imgClassifier = new ImageClassifier();
            var modelPath = Path.Combine(Path.GetDirectoryName(opts.InceptionPath), "trained-model.zip");
            await Task.Run(() => imgClassifier.LoadModel(modelPath));

            var result = imgClassifier.ClassifyImage(opts.ImageName);
            ShowMessage($"Predicted Category: {result.PredictedLabelValue} Score: {result.Score.Max()}");
        }


        private async Task TestImage(TestByImageOptions opts)
        {
            options = opts;

            if (!File.Exists(opts.ImageName))
            {
                ShowMessage($"File {opts.ImageName} does not exists");
                return;
            }

            var dbContext = services.GetService<ImagesDbContext>();

            if (opts.UseDescriptor)
            {
                var queryFeatures = new ImageFeatures(opts.ImageName, false, opts.DescriptorType);

                var images = dbContext.Images;

                await images.ForEachAsync(img =>
                {
                    using (var imgFeatures = new ImageFeatures(img.ExternalFile, false, opts.DescriptorType))
                    {
                        var dist = queryFeatures.GetDescriptorDistance(imgFeatures);
                        if (opts.Thresholds.Any())
                        {
                            if (0 <= dist && dist <= opts.Thresholds.First())
                                ShowMessage($"dist = {dist}, file = {img.ExternalFile}");
                        }
                        else
                        {
                            if (dist >= 0)
                                ShowMessage($"dist = {dist}, file = {img.ExternalFile}");
                        }
                    }
                });
            }
            else
            {
                var queryFeatures = new ImageFeatures(opts.ImageName, true);
                if (opts.Thresholds.Any())
                {
                    var images = dbContext.Images;

                    await images.ForEachAsync(img =>
                    {
                        using (var imgFeatures = new ImageFeatures(img.Hash1, img.Hash2))
                        {
                            (var pdist, var cmdist) = ImageFeatures.GetHashesDistance(queryFeatures, imgFeatures);
                            if (pdist <= opts.Thresholds.First() || cmdist <= opts.Thresholds.Last())
                                ShowMessage($"pdist = {pdist}, cmdist = {cmdist}, file = {img.ExternalFile}");
                        }
                    });
                }
                else
                {
                    var images = dbContext.Images
                            .Where(x => x.Hash1 == queryFeatures.PerceptualHash || x.Hash2 == queryFeatures.ColorMomentHash);

                    await images.ForEachAsync(x =>
                    {
                        ShowMessage(x.ExternalFile);
                    });
                }
            }
        }

        private async Task TestCategory(TestByCategoryOptions opts)
        {
            options = opts;

            var dbContext = services.GetService<ImagesDbContext>();

            var categories = dbContext.Categories.Include(x => x.Images);
            await categories.ForEachAsync(x =>
            {
                foreach (var c in opts.Categories)
                    if (opts.Strict && x.Name == c ||
                        !opts.Strict && x.Name.Contains(c))
                    {
                        ShowMessage(x.Name);
                        foreach (var i in x.Images)
                            ShowMessage($"\t{i.ExternalFile}");
                    }
            });
        }

        private async Task<(double, double)> EvalMethod(string categName, string queryImgName, ImageDescriptorType method, double threshold = 100, int max = 8)
        {
            var relevants = new List<RelevantImage>();

            var dbContext = services.GetService<ImagesDbContext>();

            var imgDescType = method;
            var queryFeatures = new ImageFeatures(queryImgName, false, imgDescType);

            var images = dbContext.Images
                    .Include(x => x.Categories)
                    .Include(x => x.Descriptors.Where(y => y.Type == imgDescType));

            await foreach (var img in images.AsAsyncEnumerable())
            {
                using (var imgFeatures = new ImageFeatures(img.Descriptors.Single(), imgDescType))
                {
                    var dist = queryFeatures.GetDescriptorDistance(imgFeatures);

                    if (0 <= dist && dist <= threshold)
                    {
                        var relevance = 100 * (threshold - dist) / threshold;

                        relevants.Add(new RelevantImage()
                        {
                            Category = img.Categories.First().Name,
                            Filename = img.ExternalFile,
                            Relevance = relevance,
                        });
                    }

                }
            }

            var sortedRelevants = relevants.OrderByDescending(x => x.Relevance).AsEnumerable().Take(max);
            var rate1 = relevants.Count(x => x.Category == categName) / (double)relevants.Count();
            var rate2 = sortedRelevants.Count(x => x.Category == categName) / (double)sortedRelevants.Count();

            return (rate1, rate2);
        }

        private async Task<IDictionary<string, string>> GetCategoryFolders(string testRootFolder, IEnumerable<string> categories)
        {
            var root = testRootFolder;
            if (!Path.EndsInDirectorySeparator(root))
                root += Path.DirectorySeparatorChar;

            var pluralizer = services.GetService<IPluralize>();

            var result = new SortedList<string, string>();
            await Task.Run(() =>
            {
                foreach (var folderCategory in Directory.GetDirectories(root))
                {
                    var categoryName = pluralizer.Singularize(
                        Path.GetFileName(folderCategory)
                            .Replace('-', ' ')
                            .Replace('_', ' ')
                            .ToLower());

                    if (categories.Contains(categoryName))
                        result.Add(categoryName, folderCategory);
                }
            });

            return result;
        }

        private async Task<double> EstimateThreshold(string categName, string queryImgName, ImageDescriptorType method)
        {
            var dbContext = services.GetService<ImagesDbContext>();

            var imgDescType = method;
            var queryFeatures = new ImageFeatures(queryImgName, false, imgDescType);

            var images = dbContext.Images
                    .Include(x => x.Categories)
                    .Include(x => x.Descriptors.Where(y => y.Type == imgDescType))
                    .Where(x => x.Categories.Any(y => y.Name == categName));

            double maxDistance = 0;
            await foreach (var img in images.AsAsyncEnumerable())
            {
                using (var imgFeatures = new ImageFeatures(img.Descriptors.Single(), imgDescType))
                {
                    var dist = queryFeatures.GetDescriptorDistance(imgFeatures);

                    if (dist > maxDistance)
                        maxDistance = dist;
                }
            }

            return maxDistance;
        }

        private async Task EvaluateMethod(EvaluateOptions opts)
        {
            options = opts;

            if (!Directory.Exists(opts.TestRootFolder))
            {
                ShowMessage($"Folder {opts.TestRootFolder} does not exists");
                return;
            }

            var categFolders = await GetCategoryFolders(opts.TestRootFolder, opts.Categories);

            var crono = new Stopwatch();
            foreach (var categName in opts.Categories)
            {
                if (!categFolders.ContainsKey(categName))
                {
                    ShowMessage($"There is no folder for category {categName}");
                    continue;
                }

                var queryImgName = Directory.GetFiles(categFolders[categName]).FirstOrDefault();

                crono.Restart();
                var threshold = Math.Ceiling(await EstimateThreshold(categName, queryImgName, opts.DescriptorType));
                var (rate1, rate2) = await EvalMethod(categName, queryImgName, opts.DescriptorType, threshold);
                crono.Stop();
                ShowMessage($"Category: {categName,10:C} Rate1: {100.0*rate1:0.#0}% Rate2: {100.0 * rate2:0.#0}% Threshold: {threshold,6:0.##} Time: {crono.Elapsed.TotalSeconds:0.#0}s");
            }
        }

        private async Task ImportImages(ImportOptions opts)
        {
            options = opts;

            if (!Directory.Exists(opts.RootFolder))
            {
                ShowMessage($"Folder {opts.RootFolder} does not exists");
                return;
            }

            var imgClassifier = opts.TrainModel ? new ImageClassifier() : null;
            var imgData = new List<ImageData>();

            var root = opts.RootFolder;
            if (!Path.EndsInDirectorySeparator(root))
                root += Path.DirectorySeparatorChar;

            var timer = new Stopwatch();
            timer.Start();
            await Task.Run(async () =>
            {
                foreach (var folder in Directory.GetDirectories(opts.RootFolder))
                {
                    var importedImages = await ProcessFolder(folder);
                    imgData.AddRange(
                        importedImages.images.Select(x => new ImageData()
                        {
                            Label = importedImages.categ,
                            ImagePath = x.Replace(root, "")
                        })); ;
                }
            });
            timer.Stop();
            ShowMessage($"{imgData.Count} images were imported in {timer.Elapsed.TotalSeconds} seconds");

            if (imgClassifier != null)
            {
                timer.Start();
                imgClassifier.GenerateModel(opts.RootFolder, opts.InceptionPath, imgData);
                var modelPath = Path.Combine(Path.GetDirectoryName(opts.InceptionPath), "trained-model.zip");
                imgClassifier.SaveModel(modelPath);
                timer.Stop();
                ShowMessage($"Model was trained with {imgData.Count} images in {timer.Elapsed.TotalSeconds} seconds");
            }
        }

        private async Task<(string categ, IEnumerable<string> images)> ProcessFolder(string folderCategory)
        {
            var dbContext = services.GetService<ImagesDbContext>();

            var pluralizer = services.GetService<IPluralize>();
            var categoryName = pluralizer.Singularize(
                Path.GetFileName(folderCategory)
                    .Replace('-', ' ')
                    .Replace('_', ' ')
                    .ToLower());

            var category = await dbContext.Categories.SingleOrDefaultAsync(x => x.Name == categoryName);
            if (category == null)
            {
                category = new Category() { Name = categoryName, Images = new List<Image>() };
                await dbContext.AddAsync(category);
            }

            ShowMessage($"Processing folder: {folderCategory} as category {categoryName}...");
            IEnumerable<string> imageNames = null;
            await Task.Run(async () =>
            {
                var processed = 0;
                var timer = new Stopwatch();
                timer.Start();
                imageNames = Directory.EnumerateFiles(Path.Combine(GetOptions<ImportOptions>().RootFolder, folderCategory), "*.jpg");
                if (GetOptions<ImportOptions>().ImportByName)
                    imageNames = imageNames.OrderBy(x => x);
                if (GetOptions<ImportOptions>().MaxImagesPerFolder.HasValue)
                    imageNames = imageNames.Take((int)GetOptions<ImportOptions>().MaxImagesPerFolder);
                foreach (var imageName in imageNames)
                {
                    if (!GetOptions<ImportOptions>().TrainModel)
                        await ProcessImage(category, imageName);
                    processed++;
                }
                timer.Stop();
                ShowMessage($"\t{processed} images processed in {timer.Elapsed.TotalSeconds} seconds");
            });
            return (categ: categoryName, images: imageNames);
        }

        private async Task ProcessImage(Category category, string fileName)
        {
            var dbContext = services.GetService<ImagesDbContext>();

            var alreadyInDatabase = await dbContext.Images
                    .Include(x => x.Categories)
                    .SingleOrDefaultAsync(x => x.ExternalFile == fileName);
            if (alreadyInDatabase != null)
            {
                if (alreadyInDatabase.Categories.Any(x => x.Name == category.Name))
                {
                    ShowMessage($"\tImage {fileName} is already on the database");
                    return;
                }
            }

            var imgDescTypes = new ImageDescriptorType[]
            {
                ImageDescriptorType.Brisk,
                ImageDescriptorType.Orb,
                ImageDescriptorType.Sift,
                ImageDescriptorType.Fast,
                ImageDescriptorType.SimpleBlob
            };

            var features = new ImageFeatures(fileName, true, imgDescTypes);

            if (alreadyInDatabase == null)
            {
                alreadyInDatabase = await dbContext.Images
                    .Include(x => x.Categories)
                    .SingleOrDefaultAsync(x => x.Hash1 == features.PerceptualHash);
                if (alreadyInDatabase != null)
                {
                    ShowMessage($"\tImage {fileName} is already on the database (phash): {alreadyInDatabase.ExternalFile}");
                    if (alreadyInDatabase.Categories.Any(x => x.Name == category.Name))
                        return;
                }
            }

            if (alreadyInDatabase == null)
            {
                alreadyInDatabase = await dbContext.Images
                   .Include(x => x.Categories)
                    .SingleOrDefaultAsync(x => x.Hash2 == features.ColorMomentHash);
                if (alreadyInDatabase != null)
                {
                    ShowMessage($"\tImage {fileName} is already on the database (cmhash): {alreadyInDatabase.ExternalFile}");
                    if (alreadyInDatabase.Categories.Any(x => x.Name == category.Name))
                        return;
                }
            }

            var image = alreadyInDatabase ?? new Image()
            {
                ExternalFile = fileName,
                Hash1 = features.PerceptualHash,
                Hash2 = features.ColorMomentHash,
                Categories = new List<Category>(),
                Descriptors = new List<ImageDescriptor>()
            };
            image.Categories.Add(category);


            if (image.Id != Guid.Empty)
            {
                dbContext.Update(image);
            }
            else
            {
                foreach (var t in imgDescTypes)
                {
                    var mDesc = features[t];
                    var imgDesc = new ImageDescriptor()
                    {
                        Image = image,

                        Cols = mDesc.Cols,
                        Rows = mDesc.Rows,
                        Depth = mDesc.Depth,
                        Type = t,
                        Data = mDesc.Data
                    };
                    image.Descriptors.Add(imgDesc);
                }
                await dbContext.AddAsync(image);
            }

            await dbContext.SaveChangesAsync();
        }

        private void ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
