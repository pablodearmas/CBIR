using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.ImgHash;
using Emgu.CV.Util;
using System;
using System.Text;
using System.Diagnostics;
using CBIR.Model;
using System.Collections.Generic;
using System.Linq;

namespace CBIR.CV
{
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
        private IDictionary<ImageDescriptorType, Mat> allDescriptors;

        private bool disposedValue;

        public static string GetString(Mat hash)
        {
            var bytes = hash.GetRawData(0);

            var result = new StringBuilder();
            foreach (var b in bytes)
                result.AppendFormat("{0:X2}", b);

            return result.ToString();
        }

        public static Mat GetMatrix(Emgu.CV.CvEnum.DepthType depthType, string hash)
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

        private static Mat GetMatrix(MatrixDescriptor desc)
        {
            var result = new Mat(desc.Rows, desc.Cols, (Emgu.CV.CvEnum.DepthType)desc.Depth, 1);
            result.SetTo(desc.Data);
            return result;
        }

        private static MatrixDescriptor GetDescriptor(Mat m)
        {
            try
            {
                var dataLength = m.Rows * m.Cols * m.ElementSize;
                byte[] data = new byte[dataLength];
                if (dataLength != 0)
                    Buffer.BlockCopy(m.GetData(false), 0, data, 0, data.Length);

                var result = new MatrixDescriptor()
                {
                    Cols = m.Cols,
                    Rows = m.Rows,
                    Depth = (int)m.Depth,
                    Data = data
                };
                return result;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static Feature2D GetKeyPointsDetector(ImageDescriptorType descType)
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

        private DistanceType GetDistanceType(ImageDescriptorType imgDescType)
        {
            switch (imgDescType)
            {
                case ImageDescriptorType.Brisk:
                    return DistanceType.Hamming2;

                case ImageDescriptorType.Orb:
                    return DistanceType.Hamming2;

                case ImageDescriptorType.Sift:
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

        private Mat GetDescriptor(Mat img, ImageDescriptorType imgDescType)
        {
            var descriptor = new Mat();
            using (var keyPointsDetector = GetKeyPointsDetector(imgDescType))
            using (var descriptorDetector = GetDescriptorDetector(keyPointsDetector))
            using (var keyPoints = new VectorOfKeyPoint(keyPointsDetector.Detect(img, null)))
            {
                descriptorDetector.Compute(img, keyPoints, descriptor);
            }
            return descriptor;
        }


        public string PerceptualHash => GetString(pHash);

        public string ColorMomentHash => GetString(cmHash);

        public ImageFeatures(string externalFile, bool calculateHashes = true, params ImageDescriptorType[] imgDescTypes)
        {
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

                if (imgDescTypes.Any())
                {
                    imgDescType = imgDescTypes.First();
                    descriptor = GetDescriptor(img, imgDescType);
                    distType = GetDistanceType(imgDescType);
                    if (imgDescTypes.Count() > 1)
                    {
                        allDescriptors = new SortedList<ImageDescriptorType, Mat>()
                        {
                            { imgDescType, descriptor }
                        };
                        foreach (var type in imgDescTypes.Skip(1))
                            allDescriptors.Add(type, GetDescriptor(img, type));
                    }
                }
            }
        }

        public ImageFeatures(string pmHash, string cmHash)
        {
            this.pHash = GetMatrix(Emgu.CV.CvEnum.DepthType.Cv8U, pmHash);
            this.cmHash = GetMatrix(Emgu.CV.CvEnum.DepthType.Cv64F, cmHash);
        }

        public ImageFeatures(MatrixDescriptor descriptor, ImageDescriptorType imgDescType = ImageDescriptorType.Default)
        {
            this.imgDescType = imgDescType;
            this.descriptor = GetMatrix(descriptor);
            this.distType = GetDistanceType(imgDescType);
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
                    matcher?.Dispose();

                    if (allDescriptors != null)
                    {
                        foreach (var d in allDescriptors.Values)
                            d.Dispose();
                    }
                    else 
                    {
                        descriptor?.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public MatrixDescriptor this[ImageDescriptorType index] => GetDescriptor(allDescriptors[index]);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
