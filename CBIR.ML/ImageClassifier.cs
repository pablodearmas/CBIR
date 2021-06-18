using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIR.ML
{
    public class ImageClassifier
    {
        private MLContext mlContext;
        ITransformer model;

        private IEstimator<ITransformer> createPipeline(
            string baseImagesFolder,
            string inceptionTensorFlowModelPath)
        {
            IEstimator<ITransformer> pipeline = mlContext.Transforms
                .LoadImages(outputColumnName: "input", imageFolder: baseImagesFolder, inputColumnName: nameof(ImageData.ImagePath))
                // The image transforms transform the images into the model's expected format.
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                // The ScoreTensorFlowModel transform scores the TensorFlow model and allows communication
                .Append(mlContext.Model.LoadTensorFlowModel(inceptionTensorFlowModelPath).ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                .AppendCacheCheckpoint(mlContext);

            return pipeline;
        }

        private ITransformer createModel(IEstimator<ITransformer> pipeline, IEnumerable<ImageData> trainingImages)
        {
            IDataView trainingData = mlContext.Data.LoadFromEnumerable(trainingImages);
            ITransformer model = pipeline.Fit(trainingData);

            return model;
        }

        private MulticlassClassificationMetrics evaluateModel(ITransformer model, IEnumerable<ImageData> testImages)
        {
            IDataView testData = mlContext.Data.LoadFromEnumerable(testImages);
            IDataView predictions = model.Transform(testData);
            MulticlassClassificationMetrics metrics =
                mlContext.MulticlassClassification.Evaluate(predictions,
                  labelColumnName: "LabelKey",
                  predictedLabelColumnName: "PredictedLabel");
            
            return metrics;
        }

        public ImageClassifier()
        {
            mlContext = new MLContext();
            Environment.SetEnvironmentVariable("TF_CPP_MIN_LOG_LEVEL", "3"); //Only errors
        }

        public MulticlassClassificationMetrics GenerateModel(
            string baseImagesFolder,
            string inceptionTensorFlowModelPath,
            IEnumerable<ImageData> trainingImages,
            IEnumerable<ImageData> testImages = null)
        {
            var pipeline = createPipeline(baseImagesFolder, inceptionTensorFlowModelPath);
            model = createModel(pipeline, trainingImages);

            MulticlassClassificationMetrics metrics = null;
            if (testImages != null)
                metrics = evaluateModel(model, testImages);

            return metrics;
        }

        public ImagePrediction ClassifyImage(string imagePath)
        {
            // load the fully qualified image file name into ImageData
            var imageData = new ImageData()
            {
                ImagePath = imagePath
            };

            // Make prediction function (input = ImageData, output = ImagePrediction)
            var predictor = mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
            var prediction = predictor.Predict(imageData);

            return prediction;
        }

        public void SaveModel(string filePath)
        {
            mlContext.Model.Save(model, null, filePath);
        }

        public void LoadModel(string filePath)
        {
            model = mlContext.Model.Load(filePath, out var inputSchema);
        }
    }
}
