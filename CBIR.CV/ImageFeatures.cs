using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.ImgHash;
using Emgu.CV.Util;
using System;
using System.Text;
using System.Diagnostics;

namespace CBIR.CV
{
    public enum ImageDescriptorType
    {
        Default = -1,
        //Detect Keypoints and Descriptors
        Brisk,
        Orb,
        Sift,
        //Only detect Descriptors
        Brief,
        Latch,
        Lucid,
        Freak,
        //Only detect KeyPoints
        Fast,  
        SimpleBlob
    }

    public class ImageFeatures : IDisposable
    {
        //Hashes
        private Mat pHash;
        private Mat cmHash;

        //Descriptors
        private ImageDescriptorType imgDescType;
        private DistanceType distType;
        private Mat descriptor;
        private DescriptorMatcher matcher;

        private bool disposedValue;

        public static string GetStringHash(Mat hash)
        {
            var bytes = hash.GetRawData(0);

            var result = new StringBuilder();
            foreach (var b in bytes)
                result.AppendFormat("{0:X2}", b);

            return result.ToString();
        }

        public static Mat GetMatHash(Emgu.CV.CvEnum.DepthType depthType, string hash)
        {
            var bytes = new byte[hash.Length / 2];

            var depthTypeName = Enum.GetName(typeof(Emgu.CV.CvEnum.DepthType), depthType);
            var depthTypeSize = int.Parse(depthTypeName.Substring(2, depthTypeName.Length - 3)) / 8;
            var cols = bytes.Length / depthTypeSize;

            var result = new Mat(1, cols, depthType, 1);

            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = byte.Parse(hash.Substring(2 * i, 2), System.Globalization.NumberStyles.HexNumber);
            result.SetTo(bytes);

            return result;
        }

        private Feature2D GetKeyPointsDetector(ImageDescriptorType descType)
        {
            switch (descType)
            {
                case ImageDescriptorType.Brief:
                    return new BriefDescriptorExtractor();

                case ImageDescriptorType.Brisk:
                    return new Brisk();

                case ImageDescriptorType.Fast:
                case ImageDescriptorType.Default:
                default:
                    return new FastFeatureDetector();

                case ImageDescriptorType.Freak:
                    return new Freak();

                case ImageDescriptorType.Latch:
                    return new LATCH();

                case ImageDescriptorType.Lucid:
                    return new LUCID();

                case ImageDescriptorType.Orb:
                    return new ORBDetector();

                case ImageDescriptorType.Sift:
                    return new SIFT();

                case ImageDescriptorType.SimpleBlob:
                    return new SimpleBlobDetector();
            }
        }

        private DistanceType GetDistanceType(Feature2D descDetector)
        {
            switch (descDetector)
            {
                case Brisk _:
                    return DistanceType.Hamming2;

                case ORBDetector _:
                    return DistanceType.Hamming2;

                case SIFT _:
                default:
                    return DistanceType.L2;
            }
        }

        private Feature2D GetDescriptorDetector(Feature2D keyPointsDetector)
        {
            switch (keyPointsDetector)
            {
                case Brisk _:
                case ORBDetector _:
                case SIFT _:
                    return keyPointsDetector;

                default:
                    return GetKeyPointsDetector(ImageDescriptorType.Brisk);
            }
        }


        public string PerceptualHash => GetStringHash(pHash);

        public string ColorMomentHash => GetStringHash(cmHash);

        public ImageFeatures(string externalFile, ImageDescriptorType imgDesc = ImageDescriptorType.Default, bool calculateHashes = true)
        {
            imgDescType = imgDesc;

            using (var img = CvInvoke.Imread(externalFile, Emgu.CV.CvEnum.ImreadModes.AnyColor))
            {
                if (calculateHashes)
                {
                    pHash = new Mat();
                    using (var hashAlgorithm = new PHash())
                    {
                        hashAlgorithm.Compute(img, pHash);
                    }

                    cmHash = new Mat();
                    using (var hashAlgorithm = new ColorMomentHash())
                    {
                        hashAlgorithm.Compute(img, cmHash);
                    }

                }

                descriptor = new Mat();
                using (var keyPointsDetector = GetKeyPointsDetector(imgDescType))
                using (var descriptorDetector = GetDescriptorDetector(keyPointsDetector))
                using (var keyPoints = new VectorOfKeyPoint(keyPointsDetector.Detect(img, null)))
                {
                    distType = GetDistanceType(descriptorDetector);
                    descriptorDetector.Compute(img, keyPoints, descriptor);
                }
            }
        }

        public ImageFeatures(string pmHash, string cmHash)
        {
            pHash = GetMatHash(Emgu.CV.CvEnum.DepthType.Cv8U, pmHash);
            this.cmHash = GetMatHash(Emgu.CV.CvEnum.DepthType.Cv64F, cmHash);
        }

        public static (double phash, double cmhash) GetHashesDistance(ImageFeatures img1, ImageFeatures img2)
        {
            double pdistance, cmdistance;
            using (var hashAlgorithm = new PHash())
            {
                pdistance = hashAlgorithm.Compare(img1.pHash, img2.pHash);
            }

            using (var hashAlgorithm = new ColorMomentHash())
            {
                cmdistance = hashAlgorithm.Compare(img1.cmHash, img2.cmHash);
            }

            return (pdistance, cmdistance);
        }

        public double GetDescriptorDistance(ImageFeatures imgQuery, double goodmatch_threshold = 0.7, double goodmatch_percent = 0.005)
        {
            if (descriptor == null || descriptor.Rows == 0)
                return -1;

            if (matcher == null)
            {
                matcher = new BFMatcher(distType);
                matcher.Add(descriptor);
            }

            var result = 0.0d;
            using (var matches = new VectorOfVectorOfDMatch())
            {
                matcher.KnnMatch(imgQuery.descriptor, matches, 1);
                var good_matches = 0;
                for (var i = 0; i < matches.Size; i++)
                {
                    if (matches[i].Size > 0)
                    {
                        ++good_matches;
                        result += matches[i][0].Distance;
                    }
                }
                if (good_matches > 0)
                    result /= good_matches;
                else
                    result = -1;

                //if (matches[i][0].Distance / matches[i][1].Distance <= goodmatch_threshold)
                //{
                //    ++good_matches;
                //    result += matches[i][0].Distance;
                //}
                //if (good_matches >= Math.Min(descriptor.Rows, imgQuery.descriptor.Rows) * goodmatch_percent)

                //if (good_matches >= descriptor.Rows * goodmatch_percent)
                //        result /= good_matches;
                //else
                //    result = -1.0;
            }
            return result;
        }

        public static double GetDescriptorsDistance(ImageFeatures imgModel, ImageFeatures imgQuery, double goodmatch_threshold = 0.75, double goodmatch_percent = 0.05)
            => imgModel.GetDescriptorDistance(imgQuery, goodmatch_threshold, goodmatch_percent);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    pHash?.Dispose();
                    cmHash?.Dispose();
                    descriptor?.Dispose();
                    matcher?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
