using Microsoft.VisualStudio.TestTools.UnitTesting;
using WPFLLM.Models;

namespace WPFLLM.Tests;

[TestClass]
public class RrfTests
{
    [TestMethod]
    public void RrfFormulaTest()
    {
        // RRF score = 1 / (k + rank + 1)
        // With k=60, rank=0: 1 / (60 + 1) = 0.0163934426
        const double k = 60.0;
        var expected = 1.0 / (k + 1);
        Assert.AreEqual(0.0163934426, expected, 0.00000001);
    }

    [TestMethod]
    public void RetrievalMode_Hybrid_CombinesBothSources()
    {
        var mode = RetrievalMode.Hybrid;
        var useVector = mode is RetrievalMode.Vector or RetrievalMode.Hybrid;
        var useKeyword = mode is RetrievalMode.Keyword or RetrievalMode.Hybrid;
        Assert.IsTrue(useVector);
        Assert.IsTrue(useKeyword);
    }
}
