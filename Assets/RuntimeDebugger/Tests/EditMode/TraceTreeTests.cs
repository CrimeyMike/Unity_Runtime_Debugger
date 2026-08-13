using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class TraceTreeTests
    {
        [Test]
        public void BeginEndTrace_RecordsNode()
        {
            var tree = new TraceTree(32);
            int id = tree.BeginTrace(HashUtil.HashString("Turn.End"), 1, 100);
            tree.EndTrace(id, 150);

            var nodes = tree.GetTree();
            Assert.AreEqual(1, nodes.Length);
            Assert.AreEqual(HashUtil.HashString("Turn.End"), nodes[0].EventHash);
            Assert.AreEqual(100, nodes[0].StartMs);
            Assert.AreEqual(150, nodes[0].EndMs);
            Assert.AreEqual(50, nodes[0].DurationMs);
            Assert.IsTrue(nodes[0].IsRoot);
        }

        [Test]
        public void NestedTrace_CorrectParentChildRelationship()
        {
            var tree = new TraceTree(32);
            int parentHash = HashUtil.HashString("Turn.End");
            int childHash = HashUtil.HashString("Event.Resolve");

            int parentId = tree.BeginTrace(parentHash, 1, 100);
            int childId = tree.BeginTrace(childHash, 1, 110);
            tree.EndTrace(childId, 130);
            tree.EndTrace(parentId, 150);

            var nodes = tree.GetTree();
            Assert.AreEqual(2, nodes.Length);

            // First written = child (completed first)
            Assert.AreEqual(childHash, nodes[0].EventHash);
            Assert.AreEqual(parentId, nodes[0].ParentId);
            Assert.AreEqual(0, nodes[0].ChildCount);

            // Parent written second, has 1 child
            Assert.AreEqual(parentHash, nodes[1].EventHash);
            Assert.IsTrue(nodes[1].IsRoot);
            Assert.AreEqual(1, nodes[1].ChildCount);
            Assert.AreEqual(1, nodes[1].ChildCount);
        }

        [Test]
        public void SequentialTraces_AllRecorded()
        {
            var tree = new TraceTree(32);
            int h1 = HashUtil.HashString("A");
            int h2 = HashUtil.HashString("B");
            int h3 = HashUtil.HashString("C");

            int id1 = tree.BeginTrace(h1, 1, 100);
            tree.EndTrace(id1, 110);

            int id2 = tree.BeginTrace(h2, 2, 120);
            tree.EndTrace(id2, 130);

            int id3 = tree.BeginTrace(h3, 3, 140);
            tree.EndTrace(id3, 150);

            var nodes = tree.GetTree();
            Assert.AreEqual(3, nodes.Length);
            Assert.AreEqual(h1, nodes[0].EventHash);
            Assert.AreEqual(h2, nodes[1].EventHash);
            Assert.AreEqual(h3, nodes[2].EventHash);
        }

        [Test]
        public void EndTrace_InvalidNodeId_DoesNothing()
        {
            var tree = new TraceTree(32);
            tree.BeginTrace(HashUtil.HashString("A"), 1, 100);
            tree.EndTrace(999, 110); // invalid nodeId

            Assert.AreEqual(0, tree.CompletedCount);
            Assert.AreEqual(1, tree.ActiveDepth);
        }

        [Test]
        public void Freeze_PreventsNewWrites()
        {
            var tree = new TraceTree(32);
            int id = tree.BeginTrace(HashUtil.HashString("A"), 1, 100);
            tree.EndTrace(id, 110);

            tree.Freeze();
            Assert.IsTrue(tree.IsFrozen);

            int id2 = tree.BeginTrace(HashUtil.HashString("B"), 2, 120);
            tree.EndTrace(id2, 130);

            var nodes = tree.GetTree();
            Assert.AreEqual(1, nodes.Length); // only the pre-freeze node
        }

        [Test]
        public void Clear_ResetsTree()
        {
            var tree = new TraceTree(32);
            int id = tree.BeginTrace(HashUtil.HashString("A"), 1, 100);
            tree.EndTrace(id, 110);
            tree.Clear();

            Assert.AreEqual(0, tree.CompletedCount);
            Assert.AreEqual(0, tree.ActiveDepth);
        }

        [Test]
        public void RingBufferOverflow_OverwritesOldest()
        {
            var tree = new TraceTree(2); // small capacity
            for (int i = 0; i < 5; i++)
            {
                int id = tree.BeginTrace(HashUtil.HashString("E" + i), i, i * 10);
                tree.EndTrace(id, i * 10 + 5);
            }

            var nodes = tree.GetTree();
            Assert.AreEqual(2, nodes.Length);
            // Most recent two: E3 and E4
            Assert.AreEqual(HashUtil.HashString("E3"), nodes[0].EventHash);
            Assert.AreEqual(HashUtil.HashString("E4"), nodes[1].EventHash);
        }

        [Test]
        public void GetLast_ReturnsMostRecentNodes()
        {
            var tree = new TraceTree(32);
            for (int i = 0; i < 5; i++)
            {
                int id = tree.BeginTrace(HashUtil.HashString("E" + i), i, i * 10);
                tree.EndTrace(id, i * 10 + 5);
            }

            var last2 = tree.GetLast(2);
            Assert.AreEqual(2, last2.Length);
            Assert.AreEqual(HashUtil.HashString("E3"), last2[0].EventHash);
            Assert.AreEqual(HashUtil.HashString("E4"), last2[1].EventHash);
        }
    }
}
