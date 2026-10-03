using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Managers;
using SecureMemo.TextSearchModels;
using UnitTests.TestSupport;

namespace UnitTests.TextSearchModels
{
    [TestClass]
    public class TabSearchEngineTests
    {
        private static TabPageDataCollection CreateCollection(params string[] tabTexts)
        {
            var collection = new TabPageDataCollection();
            for (int i = 0; i < tabTexts.Length; i++)
                collection.TabPageDictionary.Add(i, new TabPageData {PageIndex = i, TabPageLabel = "Page" + (i + 1), TabPageText = tabTexts[i]});

            return collection;
        }

        private static TextSearchProperties Down(string text, bool allTabs = false, bool caseSensitive = false)
        {
            return new TextSearchProperties {SearchText = text, SearchDirection = TextSearchEvents.SearchDirection.Down, SearchAllTabs = allTabs, CaseSensitive = caseSensitive};
        }

        private static TextSearchProperties Up(string text, bool allTabs = false)
        {
            return new TextSearchProperties {SearchText = text, SearchDirection = TextSearchEvents.SearchDirection.Up, SearchAllTabs = allTabs};
        }

        [TestMethod]
        public void Resolve_ViaContainer_DoesNotThrow()
        {
            // Regression test: TabPageDataCollection was never registered with the Autofac
            // container, so resolving TabSearchEngine (which needs one) always threw a
            // DependencyResolutionException, silently swallowed by FormMain's catch block -
            // meaning Find never worked at all.
            using var scope = TestEnvironment.Container.BeginLifetimeScope();

            TabSearchEngine engine = scope.Resolve<TabSearchEngine>();

            Assert.IsNotNull(engine);
        }

        [TestMethod]
        public void GetTextSearchResult_ViaContainer_FindsTextInActiveTab()
        {
            using var scope = TestEnvironment.Container.BeginLifetimeScope();
            var logicManager = scope.Resolve<MainFormLogicManager>();
            logicManager.ResetToDefaultDatabase();
            logicManager.SetTabPageText(0, "the quick brown fox jumps over the lazy dog");

            TabSearchEngine engine = scope.Resolve<TabSearchEngine>();
            TextSearchResult result = engine.GetTextSearchResult(Down("brown fox"));

            Assert.IsTrue(result.SearchTextFound);
            Assert.AreEqual(10, result.StartPos);
            Assert.AreEqual(9, result.Length);
        }

        [TestMethod]
        public void GetTextSearchResult_TextNotPresent_ReturnsNotFound()
        {
            var engine = new TabSearchEngine(CreateCollection("some unrelated content"));

            Assert.IsFalse(engine.GetTextSearchResult(Down("nonexistent phrase")).SearchTextFound);
        }

        [TestMethod]
        public void Down_RepeatedSearch_StepsThroughEachMatchInOrder()
        {
            var engine = new TabSearchEngine(CreateCollection("cat dog cat dog cat"));

            Assert.AreEqual(0, engine.GetTextSearchResult(Down("cat")).StartPos);
            Assert.AreEqual(8, engine.GetTextSearchResult(Down("cat")).StartPos);
            Assert.AreEqual(16, engine.GetTextSearchResult(Down("cat")).StartPos);
            Assert.IsFalse(engine.GetTextSearchResult(Down("cat")).SearchTextFound);
        }

        [TestMethod]
        public void Down_StartsAfterTheCurrentSelection()
        {
            var engine = new TabSearchEngine(CreateCollection("foo bar foo"));
            engine.ResetSearchState(0, 0, 3);

            Assert.AreEqual(8, engine.GetTextSearchResult(Down("foo")).StartPos);
        }

        [TestMethod]
        public void Down_SingleCharacterMatchAtEndOfText_IsNotReturnedTwice()
        {
            // Regression test: the start position was clamped to Length - 1 instead of Length, so a
            // one-character match on the last character was found again on every search.
            var engine = new TabSearchEngine(CreateCollection("ab"));

            Assert.AreEqual(1, engine.GetTextSearchResult(Down("b")).StartPos);
            Assert.IsFalse(engine.GetTextSearchResult(Down("b")).SearchTextFound);
        }

        [TestMethod]
        public void Up_RepeatedSearch_StepsBackwardThroughAdjacentMatches()
        {
            // Regression test: after a match the next upward search started SearchText.Length
            // characters before it, which skipped a match that ended right where the last one began.
            var engine = new TabSearchEngine(CreateCollection("abab"));
            engine.ResetSearchState(0, 4, 0);

            Assert.AreEqual(2, engine.GetTextSearchResult(Up("ab")).StartPos);
            Assert.AreEqual(0, engine.GetTextSearchResult(Up("ab")).StartPos);
            Assert.IsFalse(engine.GetTextSearchResult(Up("ab")).SearchTextFound);
        }

        [TestMethod]
        public void Up_DoesNotReturnTheMatchStartingAtTheCursor()
        {
            var engine = new TabSearchEngine(CreateCollection("x one x two"));
            engine.ResetSearchState(0, 6, 0);

            Assert.AreEqual(0, engine.GetTextSearchResult(Up("x")).StartPos);
        }

        [TestMethod]
        public void SwitchingDirection_FromDownToUp_ReturnsThePreviousMatch()
        {
            var engine = new TabSearchEngine(CreateCollection("one two one two one"));

            Assert.AreEqual(0, engine.GetTextSearchResult(Down("one")).StartPos);
            Assert.AreEqual(8, engine.GetTextSearchResult(Down("one")).StartPos);
            Assert.AreEqual(0, engine.GetTextSearchResult(Up("one")).StartPos);
        }

        [TestMethod]
        public void CaseSensitive_OnlyMatchesExactCase()
        {
            var collection = CreateCollection("Hello World");

            Assert.IsFalse(new TabSearchEngine(collection).GetTextSearchResult(Down("hello", caseSensitive: true)).SearchTextFound);
            Assert.IsTrue(new TabSearchEngine(collection).GetTextSearchResult(Down("hello", caseSensitive: false)).SearchTextFound);
        }

        [TestMethod]
        public void SearchAllTabs_StepsThroughTabsAndWrapsAround()
        {
            // Regression test: the search stopped as soon as it reached the tab the search state
            // was first created on, so after finding matches in later tabs it never wrapped back.
            var engine = new TabSearchEngine(CreateCollection("alpha", "beta", "gamma beta"));

            TextSearchResult first = engine.GetTextSearchResult(Down("beta", allTabs: true));
            TextSearchResult second = engine.GetTextSearchResult(Down("beta", allTabs: true));
            TextSearchResult third = engine.GetTextSearchResult(Down("beta", allTabs: true));

            Assert.AreEqual((1, 0), (first.TabIndex, first.StartPos));
            Assert.AreEqual((2, 6), (second.TabIndex, second.StartPos));
            Assert.IsTrue(third.SearchTextFound, "Search should wrap around to the first tab with a match");
            Assert.AreEqual((1, 0), (third.TabIndex, third.StartPos));
        }

        [TestMethod]
        public void SearchAllTabs_RepeatedSearches_KeepFindingMatches()
        {
            // Regression test: the per-search tab counter was never reset, so after enough
            // tab-to-tab steps every search reported "not found" even though matches existed.
            var engine = new TabSearchEngine(CreateCollection("foo", "foo"));

            for (int i = 0; i < 10; i++)
            {
                TextSearchResult result = engine.GetTextSearchResult(Down("foo", allTabs: true));
                Assert.IsTrue(result.SearchTextFound, $"Search #{i + 1} should have found a match");
                Assert.AreEqual(i % 2, result.TabIndex);
            }
        }

        [TestMethod]
        public void SearchAllTabs_FindsMatchInTabBeforeTheStartingTab()
        {
            var engine = new TabSearchEngine(CreateCollection("needle", "x", "y"));
            engine.ResetSearchState(1, 0, 0);

            TextSearchResult result = engine.GetTextSearchResult(Down("needle", allTabs: true));

            Assert.IsTrue(result.SearchTextFound);
            Assert.AreEqual(0, result.TabIndex);
        }

        [TestMethod]
        public void SearchAllTabs_Up_MovesToThePreviousTab()
        {
            var engine = new TabSearchEngine(CreateCollection("needle here", "nothing", "x"));
            engine.ResetSearchState(1, 0, 0);

            TextSearchResult result = engine.GetTextSearchResult(Up("needle", allTabs: true));

            Assert.AreEqual((0, 0), (result.TabIndex, result.StartPos));
        }

        [TestMethod]
        public void SearchAllTabs_SkipsEmptyTabs()
        {
            var engine = new TabSearchEngine(CreateCollection("", null, "target"));

            TextSearchResult result = engine.GetTextSearchResult(Down("target", allTabs: true));

            Assert.AreEqual((2, 0), (result.TabIndex, result.StartPos));
        }

        [TestMethod]
        public void SearchAllTabs_NotFoundAnywhere_ReturnsNotFound()
        {
            var engine = new TabSearchEngine(CreateCollection("a", "b", "c"));

            Assert.IsFalse(engine.GetTextSearchResult(Down("zzz", allTabs: true)).SearchTextFound);
            Assert.IsFalse(engine.GetTextSearchResult(Up("zzz", allTabs: true)).SearchTextFound);
        }

        [TestMethod]
        public void Search_AfterTheSearchedTabWasDeleted_DoesNotThrow()
        {
            // The Find dialog is modeless, so tabs can be deleted while a search is in progress.
            TabPageDataCollection collection = CreateCollection("one", "two", "three");
            var engine = new TabSearchEngine(collection);
            engine.ResetSearchState(2, 0, 0);
            collection.TabPageDictionary.Remove(2);

            TextSearchResult result = engine.GetTextSearchResult(Down("one", allTabs: true));

            Assert.AreEqual((0, 0), (result.TabIndex, result.StartPos));
        }
    }
}
