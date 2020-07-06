using System.Collections.Generic;
using System.Linq;
using SecureMemo.DataModels;
using Serilog;

namespace SecureMemo.Storage
{
    public class PageDataCollectionManager
    {
        private readonly TabPageDataCollection _dataCollection;

        public PageDataCollectionManager(TabPageDataCollection dataCollection)
        {
            _dataCollection = dataCollection;
        }

        public bool DeleteTabPage(int tabIndex)
        {
            if (tabIndex < _dataCollection.TabPageDictionary.Count && tabIndex >= 0)
            {
                var tabPage = _dataCollection.TabPageDictionary[tabIndex];
                List<int> keyList = _dataCollection.TabPageDictionary.Keys.ToList();
                _dataCollection.TabPageDictionary.Remove(tabIndex);

                if (_dataCollection.ActiveTabIndex == tabIndex)
                {
                    if (tabIndex == 0)
                        _dataCollection.ActiveTabIndex = _dataCollection.TabPageDictionary.Values.Select(x => x.PageIndex).First();
                    else if (tabIndex == _dataCollection.TabPageDictionary.Count - 1)
                        _dataCollection.ActiveTabIndex = _dataCollection.TabPageDictionary.Values.Select(x => x.PageIndex).Last();
                    else
                        _dataCollection.ActiveTabIndex = _dataCollection.TabPageDictionary.Values.Select(x => x.PageIndex).Last(x => x < tabIndex);
                }


                return true;
            }

            return false;
        }

        public bool ValidateDataCollectionIntegrity()
        {
            List<int> pageKeyList = _dataCollection.TabPageDictionary.Keys.OrderBy(i => i).ToList();


            // Check for duplicate page ids
            if (pageKeyList.Distinct().Count() != pageKeyList.Count || pageKeyList.Max() > pageKeyList.Count)
            {
                //Rebuild index
                var tabPageDictionary = new Dictionary<int, TabPageData>();
                var pageCount = pageKeyList.Distinct().Count();
                pageKeyList.Sort();

                var tabPageDataList = _dataCollection.TabPageDictionary.Values.Select(x => new TabPageData {PageIndex = x.PageIndex, TabPageLabel = x.TabPageLabel, TabPageText = x.TabPageText, UniqueId = x.UniqueId}).ToList();

                for (int i = 0; i < tabPageDataList.Count; i++)
                {
                    int key = i;
                    var pageData = tabPageDataList[i];

                    if (pageData.PageIndex != i)
                    {
                        Log.Warning("Found Database integrity errors for page {PageIndex}", pageData.PageIndex);
                        pageData.PageIndex = i;
                        Log.Warning("Switching to PageIndex: {PageIndex}", pageData.PageIndex);
                        pageData.UniqueId = null;
                        pageData.GenerateUniqueIdIfNoneExists();
                    }

                    tabPageDictionary.Add(i, pageData);
                }

                _dataCollection.TabPageDictionary.Clear();
                _dataCollection.TabPageDictionary = tabPageDictionary;
                _dataCollection.ActiveTabIndex = 0;


                return false;
            }

            return true;
        }
    }
}