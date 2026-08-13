using System.Collections.Generic;
using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    public class GameStateProvider : IStateProvider
    {
        public string Category => "Game";
        public int Turn { get; set; }
        public int EventCount { get; set; }

        public void Capture(List<StateSnapshot> outList, int frame, long timestampMs)
        {
            if (outList == null) return;
            outList.Add(StateSnapshot.Create(frame, timestampMs, Category, "Turn", Turn.ToString()));
            outList.Add(StateSnapshot.Create(frame, timestampMs, Category, "EventCount", EventCount.ToString()));
        }
    }

    public class UIStateProvider : IStateProvider
    {
        public string Category => "UI";
        public string CurrentPanel { get; set; }

        public void Capture(List<StateSnapshot> outList, int frame, long timestampMs)
        {
            if (outList == null) return;
            outList.Add(StateSnapshot.Create(frame, timestampMs, Category, "CurrentPanel", CurrentPanel ?? "None"));
        }
    }

    [TestFixture]
    public class StateSnapshotTests
    {
        private StateRegistry _registry;
        private GameStateProvider _gameProvider;
        private UIStateProvider _uiProvider;

        [SetUp]
        public void SetUp()
        {
            _registry = new StateRegistry(64);
            _gameProvider = new GameStateProvider { Turn = 12, EventCount = 27 };
            _uiProvider = new UIStateProvider { CurrentPanel = "TechnologyPanel" };
        }

        [Test]
        public void Register_AddsProvider()
        {
            _registry.Register(_gameProvider);
            Assert.AreEqual(1, _registry.ProviderCount);
        }

        [Test]
        public void Register_DuplicateProvider_NotAddedTwice()
        {
            _registry.Register(_gameProvider);
            _registry.Register(_gameProvider);
            Assert.AreEqual(1, _registry.ProviderCount);
        }

        [Test]
        public void Unregister_RemovesProvider()
        {
            _registry.Register(_gameProvider);
            _registry.Unregister(_gameProvider);
            Assert.AreEqual(0, _registry.ProviderCount);
        }

        [Test]
        public void CaptureAll_CollectsFromAllProviders()
        {
            _registry.Register(_gameProvider);
            _registry.Register(_uiProvider);

            var snapshots = new List<StateSnapshot>();
            _registry.CaptureAll(1, 100, snapshots);

            Assert.AreEqual(3, snapshots.Count); // 2 from Game + 1 from UI
            Assert.AreEqual(HashUtil.HashString("Game"), snapshots[0].CategoryHash);
            Assert.AreEqual(HashUtil.HashString("UI"), snapshots[2].CategoryHash);
        }

        [Test]
        public void CaptureAll_ReflectsCurrentValues()
        {
            _registry.Register(_gameProvider);

            var snapshots = new List<StateSnapshot>();
            _gameProvider.Turn = 5;
            _registry.CaptureAll(1, 100, snapshots);

            Assert.AreEqual("5", snapshots[0].Value);

            snapshots.Clear();
            _gameProvider.Turn = 99;
            _registry.CaptureAll(2, 200, snapshots);

            Assert.AreEqual("99", snapshots[0].Value);
        }

        [Test]
        public void WriteSnapshot_StoresInBuffer()
        {
            _registry.WriteSnapshot(StateSnapshot.Create(1, 100, "Test", "Key", "Value"));

            Assert.AreEqual(1, _registry.SnapshotCount);
            var snaps = _registry.GetSnapshots();
            Assert.AreEqual("Value", snaps[0].Value);
        }

        [Test]
        public void Freeze_PreventsWrites()
        {
            _registry.Freeze();
            _registry.WriteSnapshot(StateSnapshot.Create(1, 100, "Test", "Key", "Value"));

            Assert.AreEqual(0, _registry.SnapshotCount);
        }

        [Test]
        public void Clear_ResetsRegistry()
        {
            _registry.Register(_gameProvider);
            _registry.WriteSnapshot(StateSnapshot.Create(1, 100, "Test", "K", "V"));
            _registry.Clear();

            Assert.AreEqual(0, _registry.ProviderCount);
            Assert.AreEqual(0, _registry.SnapshotCount);
        }

        [Test]
        public void GetLastSnapshots_ReturnsMostRecentN()
        {
            for (int i = 0; i < 5; i++)
                _registry.WriteSnapshot(StateSnapshot.Create(i, i * 100, "Cat", "K" + i, "V" + i));

            var last2 = _registry.GetLastSnapshots(2);
            Assert.AreEqual(2, last2.Length);
            Assert.AreEqual("V3", last2[0].Value);
            Assert.AreEqual("V4", last2[1].Value);
        }
    }
}
