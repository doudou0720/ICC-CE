using System;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace Ink_Canvas.Helpers
{
    public sealed class InkRecognitionManager
    {
        private static InkRecognitionManager _instance;
        private static readonly object _lock = new object();

        private ModernInkProcessor _modernProcessor;
        private ModernInkAnalyzer _modernAnalyzer;
        private bool _isModernSystemAvailable;
        private bool _isInitialized;

        public static InkRecognitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new InkRecognitionManager();
                    }
                }

                return _instance;
            }
        }

        private InkRecognitionManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                var tryModern = WinRtInkShapeRecognizer.IsApiAvailable && Environment.Is64BitProcess;

                _isModernSystemAvailable = false;
                if (tryModern)
                {
                    try
                    {
                        _modernProcessor = new ModernInkProcessor();
                        _modernAnalyzer = new ModernInkAnalyzer();
                        _isModernSystemAvailable = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("WinRT 墨迹初始化失败: " + ex.Message, LogHelper.LogType.Warning);
                        _isModernSystemAvailable = false;
                        _modernProcessor = null;
                        _modernAnalyzer = null;
                    }
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("墨迹识别管理器初始化失败: " + ex.Message, LogHelper.LogType.Error);
                _isInitialized = false;
            }
        }

        public Task<InkShapeRecognitionResult> RecognizeShapeAsync(
            StrokeCollection strokes,
            ShapeRecognitionEngineMode mode)
        {
            if (!_isInitialized || strokes == null || strokes.Count == 0)
                return Task.FromResult(InkShapeRecognitionResult.Empty);

            try
            {
                if (ShapeRecognitionRouter.ResolveUseWinRt(mode)
                    && WinRtInkShapeRecognizer.IsApiAvailable)
                {
                    return RecognizeShapeWinRtOnDispatcherContext(strokes);
                }

                var legacy = InkRecognizeHelper.RecognizeShapeIACore(strokes);
                return Task.FromResult(InkRecognizeHelper.FromIACoreOrEmpty(legacy));
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("墨迹形状识别失败: " + ex.Message, LogHelper.LogType.Error);
                return Task.FromResult(InkShapeRecognitionResult.Empty);
            }
        }

        private static async Task<InkShapeRecognitionResult> RecognizeShapeWinRtOnDispatcherContext(
            StrokeCollection strokes)
        {
            return await WinRtInkShapeRecognizer.RecognizeShapeAsync(strokes).ConfigureAwait(true);
        }

        public Task<StrokeCollection> CorrectInkAsync(
            StrokeCollection strokes,
            ShapeRecognitionEngineMode mode)
        {
            if (!_isInitialized || strokes == null || strokes.Count == 0)
                return Task.FromResult(strokes);

            try
            {
                if (ShapeRecognitionRouter.ResolveUseWinRt(mode) && _modernAnalyzer != null)
                    return _modernAnalyzer.AnalyzeAndCorrectAsync(strokes);

                return Task.FromResult(strokes);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("墨迹纠正失败: " + ex.Message, LogHelper.LogType.Error);
                return Task.FromResult(strokes);
            }
        }

        public bool IsValidShapeType(string shapeName)
        {
            return !string.IsNullOrEmpty(shapeName)
                   && (shapeName.Contains("Triangle") || shapeName.Contains("Circle")
                       || shapeName.Contains("Rectangle") || shapeName.Contains("Diamond")
                       || shapeName.Contains("Parallelogram") || shapeName.Contains("Square")
                       || shapeName.Contains("Ellipse") || shapeName.Contains("Line")
                       || shapeName.Contains("Arrow"));
        }

        public string GetSystemInfo()
        {
            return _isModernSystemAvailable
                ? $"现代化64位墨迹识别系统 (Windows Runtime API) - 进程架构: {Environment.Is64BitProcess}"
                : $"传统墨迹识别系统 (IACore) - 进程架构: {Environment.Is64BitProcess}";
        }

        public void Dispose()
        {
            _modernProcessor?.Dispose();
            _modernAnalyzer?.Dispose();
            _isInitialized = false;
        }
    }

    internal sealed class ModernInkProcessor : IDisposable
    {
        public ModernInkProcessor()
        {
            if (!WinRtInkShapeRecognizer.IsApiAvailable)
                throw new InvalidOperationException("WinRT 墨迹分析需要 Windows 10 及以上。");
        }

        public Task<InkShapeRecognitionResult> RecognizeShapeAsync(StrokeCollection strokes)
        {
            return WinRtInkShapeRecognizer.RecognizeShapeAsync(strokes);
        }

        public void Dispose()
        {
        }
    }

    internal sealed class ModernInkAnalyzer : IDisposable
    {
        public Task<StrokeCollection> AnalyzeAndCorrectAsync(StrokeCollection strokes)
        {
            return Task.FromResult(strokes);
        }

        public void Dispose()
        {
        }
    }
}
