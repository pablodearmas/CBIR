using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CBIR.CV;
using CBIR.Model;
using CommandLine;

namespace CBIR.ImportImages
{
	public class GlobalOptions
    {
	}

	[Verb("import", HelpText = "import images from a folder")]
	public class ImportOptions : GlobalOptions
	{		
		[Option("path", Required = true, HelpText = "path of directory with the images")]
		public string RootFolder { set; get; }

		[Option("img-x-fldr", Required = false, HelpText = "max images per folder")]
		public uint? MaxImagesPerFolder { set; get; }

		[Option('s', Required = false, Default = false, HelpText = "import images sorting by name")]
		public bool ImportByName { set; get; }

		[Option('m', Required = false, Default = false, HelpText = "create ML model")]
		public bool TrainModel { set; get; }

		[Option("inception-path", Required = false, HelpText = "path of pre-trained inception model")]
		public string InceptionPath { set; get; }

	}

	[Verb("eval", HelpText = "eval features extraction method")]
	public class EvaluateOptions : GlobalOptions
	{
		[Option("testpath", Required = true, HelpText = "path of directory with test images")]
		public string TestRootFolder { set; get; }

		[Option("categ", Required = true, HelpText = "categories to use for testing", Min = 1)]
		public IEnumerable<string> Categories { set; get; }

		[Option("method", Required = false, Default = ImageDescriptorType.Default, HelpText = "features extraction method to test")]
		public ImageDescriptorType DescriptorType { set; get; }
	}


	[Verb("test-ctg", HelpText = "test by category")]
	public class TestByCategoryOptions : GlobalOptions
	{
		[Option("categ", Required = true, HelpText = "categories to search for", Min = 1)]
		public IEnumerable<string> Categories { set; get; }

		[Option('s', Required = false, Default = false, HelpText = "strict match")]
		public bool Strict { set; get; }
	}

	[Verb("test-img", HelpText = "test by image")]
	public class TestByImageOptions : GlobalOptions
	{
		[Option("img", Required = true, HelpText = "image full path")]
		public string ImageName { set; get; }

		[Option("thresholds", Required = false, HelpText = "max images per folder", Min = 1, Max = 2)]
		public IEnumerable<double> Thresholds { set; get; }

		[Option('d', Required = false, Default = false, HelpText = "use descriptor in place of hashes")]
		public bool UseDescriptor { set; get; }

		[Option("desc", Required = false, Default = ImageDescriptorType.Default, HelpText = "use descriptor in place of hashes")]
		public ImageDescriptorType DescriptorType { set; get; }
	}

	[Verb("test-ml", HelpText = "test by image classifier using ML")]
	public class TestByMLOptions : GlobalOptions
	{
		[Option("img", Required = true, HelpText = "image full path")]
		public string ImageName { set; get; }

		[Option("inception-path", Required = false, HelpText = "path of pre-trained inception model")]
		public string InceptionPath { set; get; }
	}

	[Verb("cmp-img", HelpText = "compare two images")]
	public class CompareImagesOptions : GlobalOptions
	{
		[Option("img", Required = true, HelpText = "images full path", Min = 1, Max = 2)]
		public IEnumerable<string> ImageNames { set; get; }

		[Option('d', Required = false, Default = false, HelpText = "use descriptor in place of hashes")]
		public bool UseDescripor { set; get; }

	}
}