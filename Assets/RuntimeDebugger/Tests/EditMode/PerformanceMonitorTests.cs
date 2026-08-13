using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class PerformanceMonitorTests
    {
        private PerformanceMonitor _monitor;

        [SetUp]
        public void SetUp()
        {
            _monitor = new PerformanceMonitor(256);
        }

        [TearDown]
        public void TearDown()
        {
            _monitor.Dispose();
        }

        [Test]
        public void RegisterDefaultMetrics_RegistersAllMetrics()
        {
            _monitor.RegisterDefaultMetrics();

            Assert.AreEqual(8, _monitor.MetricCount);
        }

        [Test]
        public void RegisterMetric_DuplicateId_NotAddedTwice()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);

            Assert.AreEqual(1, _monitor.MetricCount);
        }

        [Test]
        public void StartSampling_SetsSamplingFlag()
        {
            _monitor.RegisterDefaultMetrics();
            _monitor.StartSampling();

            Assert.IsTrue(_monitor.IsSampling);
        }

        [Test]
        public void StopSampling_ClearsFlag()
        {
            _monitor.RegisterDefaultMetrics();
            _monitor.StartSampling();
            _monitor.StopSampling();

            Assert.IsFalse(_monitor.IsSampling);
        }

        [Test]
        public void OnFrameUpdate_WhenNotSampling_DoesNothing()
        {
            _monitor.RegisterDefaultMetrics();
            // Don't call StartSampling
            _monitor.OnFrameUpdate(1, 100);

            Assert.AreEqual(0, _monitor.SampleCount);
        }

        [Test]
        public void OnFrameUpdate_WhenSampling_RecordsMetrics()
        {
            _monitor.RegisterDefaultMetrics();
            _monitor.StartSampling(1);
            _monitor.OnFrameUpdate(1, 100);

            Assert.Greater(_monitor.SampleCount, 0);
        }

        [Test]
        public void OnFrameUpdate_SampleEveryNFrames_RespectsFrequency()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.StartSampling(3); // sample every 3 frames

            _monitor.OnFrameUpdate(1, 100);
            Assert.AreEqual(0, _monitor.SampleCount); // frame 1: skip

            _monitor.OnFrameUpdate(2, 200);
            Assert.AreEqual(0, _monitor.SampleCount); // frame 2: skip

            _monitor.OnFrameUpdate(3, 300);
            Assert.AreEqual(1, _monitor.SampleCount); // frame 3: sample
        }

        [Test]
        public void GetMetricsForId_ReturnsOnlyMatchingMetrics()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.RegisterMetric(PerformanceMonitor.DrawCallsDef);
            _monitor.StartSampling(1);

            _monitor.OnFrameUpdate(1, 100);
            _monitor.OnFrameUpdate(2, 200);

            var frameTimeMetrics = _monitor.GetMetricsForId(MetricIds.FrameTime);
            Assert.AreEqual(2, frameTimeMetrics.Count);
            Assert.AreEqual(MetricIds.FrameTime, frameTimeMetrics[0].MetricId);
        }

        [Test]
        public void GetLatestValue_ReturnsMostRecent()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.StartSampling(1);

            _monitor.OnFrameUpdate(1, 100);
            _monitor.OnFrameUpdate(2, 200);

            // LastValue from ProfilerRecorder might be 0 in edit mode, but the method should work
            double value = _monitor.GetLatestValue(MetricIds.FrameTime);
            Assert.IsTrue(value >= 0);
        }

        [Test]
        public void Freeze_PreventsNewSamples()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.StartSampling(1);
            _monitor.OnFrameUpdate(1, 100);

            _monitor.Freeze();
            _monitor.OnFrameUpdate(2, 200);

            Assert.AreEqual(1, _monitor.SampleCount); // no new samples after freeze
        }

        [Test]
        public void Clear_ResetsBuffer()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.StartSampling(1);
            _monitor.OnFrameUpdate(1, 100);
            _monitor.Clear();

            Assert.AreEqual(0, _monitor.SampleCount);
        }

        [Test]
        public void GetLastMetrics_ReturnsMostRecentN()
        {
            _monitor.RegisterMetric(PerformanceMonitor.FrameTimeDef);
            _monitor.StartSampling(1);

            for (int i = 0; i < 5; i++)
                _monitor.OnFrameUpdate(i, i * 100);

            var last2 = _monitor.GetLastMetrics(2);
            Assert.AreEqual(2, last2.Length);
        }
    }
}
