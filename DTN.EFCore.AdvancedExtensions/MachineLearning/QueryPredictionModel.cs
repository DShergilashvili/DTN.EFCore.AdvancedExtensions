using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Concurrent;

namespace DTN.EFCore.AdvancedExtensions.MachineLearning
{
    public class QueryPredictionModel
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private readonly ConcurrentDictionary<string, QueryPerformancePrediction> _predictionCache;

        public QueryPredictionModel()
        {
            _mlContext = new MLContext(seed: 0);
            _predictionCache = new ConcurrentDictionary<string, QueryPerformancePrediction>();
            InitializeModel();
        }

        public async Task<QueryPerformancePrediction> PredictQueryPerformanceAsync<T>(IQueryable<T> query, DbContext context)
        {
            var queryString = query.ToQueryString();
            if (_predictionCache.TryGetValue(queryString, out var cachedPrediction))
            {
                return cachedPrediction;
            }

            var queryFeatures = ExtractQueryFeatures(queryString);
            var prediction = PredictPerformance(queryFeatures);

            // If the model's confidence is low, fallback to actual execution
            if (prediction.Confidence < 0.7)
            {
                var actualPerformance = await MeasureActualPerformanceAsync(query, context);
                prediction = new QueryPerformancePrediction
                {
                    EstimatedExecutionTime = actualPerformance,
                    Confidence = 1.0f
                };

                // Use this actual performance data to improve the model
                await UpdateModelAsync(queryFeatures, actualPerformance);
            }

            _predictionCache[queryString] = prediction;
            return prediction;
        }

        private QueryFeatures ExtractQueryFeatures(string queryString)
        {
            return new QueryFeatures
            {
                TableCount = CountOccurrences(queryString, "FROM") + CountOccurrences(queryString, "JOIN"),
                JoinCount = CountOccurrences(queryString, "JOIN"),
                WhereCount = CountOccurrences(queryString, "WHERE"),
                OrderByCount = CountOccurrences(queryString, "ORDER BY"),
                GroupByCount = CountOccurrences(queryString, "GROUP BY"),
                HavingCount = CountOccurrences(queryString, "HAVING"),
                SubqueryCount = CountOccurrences(queryString, "(SELECT"),
                QueryLength = queryString.Length
            };
        }

        private int CountOccurrences(string source, string searchString)
        {
            return System.Text.RegularExpressions.Regex.Matches(source, searchString, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        }

        private QueryPerformancePrediction PredictPerformance(QueryFeatures queryFeatures)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<QueryFeatures, Prediction>(_model);
            var prediction = predictionEngine.Predict(queryFeatures);

            return new QueryPerformancePrediction
            {
                EstimatedExecutionTime = TimeSpan.FromMilliseconds(prediction.ExecutionTime),
                Confidence = prediction.Confidence
            };
        }

        private async Task<TimeSpan> MeasureActualPerformanceAsync<T>(IQueryable<T> query, DbContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await query.LoadAsync();
            sw.Stop();
            return sw.Elapsed;
        }

        private async Task UpdateModelAsync(QueryFeatures queryFeatures, TimeSpan actualPerformance)
        {
            var trainingData = _mlContext.Data.LoadFromEnumerable(new[]
            {
                new QueryPerformanceData
                {
                    Features = queryFeatures,
                    ExecutionTime = actualPerformance.TotalMilliseconds
                }
            });

            var pipeline = BuildPipeline();
            _model = pipeline.Fit(trainingData);

            await Task.CompletedTask; // Placeholder for potential async operations
        }

        private void InitializeModel()
        {
            var initialData = new List<QueryPerformanceData>
            {
                new QueryPerformanceData { Features = new QueryFeatures { TableCount = 1, QueryLength = 100 }, ExecutionTime = 10 },
                new QueryPerformanceData { Features = new QueryFeatures { TableCount = 2, JoinCount = 1, QueryLength = 200 }, ExecutionTime = 20 },
                // Add more initial data as needed
            };

            var trainingData = _mlContext.Data.LoadFromEnumerable(initialData);
            var pipeline = BuildPipeline();
            _model = pipeline.Fit(trainingData);
        }

        private IEstimator<ITransformer> BuildPipeline()
        {
            return _mlContext.Transforms.Concatenate("Features",
                    nameof(QueryFeatures.TableCount),
                    nameof(QueryFeatures.JoinCount),
                    nameof(QueryFeatures.WhereCount),
                    nameof(QueryFeatures.OrderByCount),
                    nameof(QueryFeatures.GroupByCount),
                    nameof(QueryFeatures.HavingCount),
                    nameof(QueryFeatures.SubqueryCount),
                    nameof(QueryFeatures.QueryLength))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Regression.Trainers.FastForest(labelColumnName: "ExecutionTime", featureColumnName: "Features"));
        }
    }

    public class QueryFeatures
    {
        public int TableCount { get; set; }
        public int JoinCount { get; set; }
        public int WhereCount { get; set; }
        public int OrderByCount { get; set; }
        public int GroupByCount { get; set; }
        public int HavingCount { get; set; }
        public int SubqueryCount { get; set; }
        public int QueryLength { get; set; }
    }

    public class QueryPerformanceData
    {
        public QueryFeatures Features { get; set; }
        public double ExecutionTime { get; set; }
    }

    public class Prediction
    {
        [ColumnName("Score")]
        public float ExecutionTime;

        public float Confidence;
    }

    public class QueryPerformancePrediction
    {
        public TimeSpan EstimatedExecutionTime { get; set; }
        public float Confidence { get; set; }
    }
}