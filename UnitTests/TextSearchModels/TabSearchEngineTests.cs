using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.Managers;
using SecureMemo.TextSearchModels;
using UnitTests.Config;

namespace UnitTests.TextSearchModels
{
    [TestClass]
    public class TabSearchEngineTests
    {
        [TestMethod]
        public void Resolve_ViaContainer_DoesNotThrow()
        {
            // Regression test: TabPageDataCollection was never registered with the Autofac
            // container, so resolving TabSearchEngine (which needs one) always threw a
            // DependencyResolutionException, silently swallowed by FormMain's catch block -
            // meaning Find never worked at all.
            var container = AutofacConfig.CreateContainer();
            using var scope = container.BeginLifetimeScope();

            TabSearchEngine engine = scope.Resolve<TabSearchEngine>();

            Assert.IsNotNull(engine);
        }

        [TestMethod]
        public void GetTextSearchResult_FindsTextInActiveTab()
        {
            var container = AutofacConfig.CreateContainer();
            using var scope = container.BeginLifetimeScope();

            var logicManager = scope.Resolve<MainFormLogicManager>();
            logicManager.CreateNewDatabase();
            logicManager.SetTabPageText(0, "the quick brown fox jumps over the lazy dog");

            TabSearchEngine engine = scope.Resolve<TabSearchEngine>();
            TextSearchResult result = engine.GetTextSearchResult(new TextSearchProperties
            {
                SearchText = "brown fox",
                SearchDirection = TextSearchEvents.SearchDirection.Down
            });

            Assert.IsTrue(result.SearchTextFound);
        }

        [TestMethod]
        public void GetTextSearchResult_TextNotPresent_ReturnsNotFound()
        {
            var container = AutofacConfig.CreateContainer();
            using var scope = container.BeginLifetimeScope();

            var logicManager = scope.Resolve<MainFormLogicManager>();
            logicManager.CreateNewDatabase();
            logicManager.SetTabPageText(0, "some unrelated content");

            TabSearchEngine engine = scope.Resolve<TabSearchEngine>();
            TextSearchResult result = engine.GetTextSearchResult(new TextSearchProperties
            {
                SearchText = "nonexistent phrase",
                SearchDirection = TextSearchEvents.SearchDirection.Down
            });

            Assert.IsFalse(result.SearchTextFound);
        }
    }
}
