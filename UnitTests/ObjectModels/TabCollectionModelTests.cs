using System;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureMemo.DataModels;
using SecureMemo.Storage;

namespace UnitTests.ObjectModels
{
    [TestClass]
    public class TabCollectionModelTests
    {
        private static TabPageData Page(int pageIndex, string text)
        {
            return new TabPageData {PageIndex = pageIndex, TabPageLabel = "L" + pageIndex, TabPageText = text, UniqueId = Guid.NewGuid().ToString()};
        }

        [TestMethod]
        public void CreateNewPageDataCollection_CreatesLabeledPagesWithUniqueIds()
        {
            TabPageDataCollection collection = TabPageDataCollection.CreateNewPageDataCollection(3);

            CollectionAssert.AreEqual(new[] {"Page1", "Page2", "Page3"}, collection.TabPageDictionary.Values.Select(p => p.TabPageLabel).ToArray());
            CollectionAssert.AreEqual(new[] {0, 1, 2}, collection.TabPageDictionary.Values.Select(p => p.PageIndex).ToArray());
            Assert.AreEqual(3, collection.TabPageDictionary.Values.Select(p => p.UniqueId).Distinct().Count());
            Assert.AreEqual(0, collection.ActiveTabIndex);
        }

        [TestMethod]
        public void GenerateUniqueIdIfNoneExists_OnlyAssignsWhenMissing()
        {
            var page = new TabPageData();

            Assert.IsTrue(page.GenerateUniqueIdIfNoneExists());
            string id = page.UniqueId;
            Assert.IsFalse(page.GenerateUniqueIdIfNoneExists());
            Assert.AreEqual(id, page.UniqueId);
        }

        [TestMethod]
        public void ValidateDataCollectionIntegrity_WellFormedCollection_IsLeftUntouched()
        {
            var collection = new TabPageDataCollection {ActiveTabIndex = 1};
            collection.TabPageDictionary.Add(0, Page(0, "a"));
            collection.TabPageDictionary.Add(1, Page(1, "b"));
            string firstId = collection.TabPageDictionary[0].UniqueId;

            Assert.IsTrue(new PageDataCollectionManager(collection).ValidateDataCollectionIntegrity());
            Assert.AreEqual(1, collection.ActiveTabIndex);
            Assert.AreEqual(firstId, collection.TabPageDictionary[0].UniqueId);
        }

        [TestMethod]
        public void ValidateDataCollectionIntegrity_GapInKeys_IsReindexedInOrder()
        {
            // Regression test: the check was "highest key > page count", which a single gap (keys
            // 0 and 2 for two pages) never trips.
            var collection = new TabPageDataCollection {ActiveTabIndex = 1};
            collection.TabPageDictionary.Add(2, Page(2, "c"));
            collection.TabPageDictionary.Add(0, Page(0, "a"));

            Assert.IsFalse(new PageDataCollectionManager(collection).ValidateDataCollectionIntegrity());

            CollectionAssert.AreEqual(new[] {"a", "c"}, new[] {collection.TabPageDictionary[0].TabPageText, collection.TabPageDictionary[1].TabPageText});
            Assert.AreEqual(1, collection.TabPageDictionary[1].PageIndex);
            Assert.AreEqual(0, collection.ActiveTabIndex);
        }

        [TestMethod]
        public void ValidateDataCollectionIntegrity_PageIndexDisagreeingWithKey_IsRepaired()
        {
            var collection = new TabPageDataCollection();
            collection.TabPageDictionary.Add(0, Page(1, "a"));
            collection.TabPageDictionary.Add(1, Page(0, "b"));

            Assert.IsFalse(new PageDataCollectionManager(collection).ValidateDataCollectionIntegrity());

            Assert.AreEqual("a", collection.TabPageDictionary[0].TabPageText);
            Assert.AreEqual(0, collection.TabPageDictionary[0].PageIndex);
            Assert.AreEqual(1, collection.TabPageDictionary[1].PageIndex);
        }

        [TestMethod]
        public void ValidateDataCollectionIntegrity_EmptyCollection_IsValid()
        {
            Assert.IsTrue(new PageDataCollectionManager(new TabPageDataCollection()).ValidateDataCollectionIntegrity());
        }

        [TestMethod]
        public void FontSettings_FontFamilyUpdated_PicksUpTheNewFamilyName()
        {
            var fontSettings = new SecureMemoFontSettings {FontFamilyName = "Arial", FontSize = 12f, Style = FontStyle.Regular};
            Assert.AreEqual("Arial", fontSettings.FontFamily.Name);

            fontSettings.FontFamilyName = "Courier New";
            fontSettings.FontFamilyUpdated();

            Assert.AreEqual("Courier New", fontSettings.FontFamily.Name);
        }
    }
}
