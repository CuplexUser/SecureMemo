using System;
using SecureMemo.DataModels;

namespace SecureMemo.TextSearchModels
{
    public class TabSearchEngine
    {
        private readonly TabPageDataCollection _tabPageDataCollection;

        private TextSearchState _searchState;

        public TabSearchEngine(TabPageDataCollection tabPageDataCollection, int textStartIndex = 0)
        {
            _searchState = new TextSearchState();
            _tabPageDataCollection = tabPageDataCollection;
            ResetSearchState(_tabPageDataCollection.ActiveTabIndex, textStartIndex, 0);
        }

        public bool SelectionSetByCode { get; set; }

        public void ResetSearchState(int tabIndex, int textStartPos, int selectionLength)
        {
            _searchState = new TextSearchState
            {
                TabIndex = tabIndex,
                StartPosUp = textStartPos,
                StartPosDown = textStartPos + selectionLength
            };
        }

        public TextSearchResult GetTextSearchResult(TextSearchProperties searchProperties)
        {
            if (!searchProperties.SearchAllTabs)
                return GetTextSearchResultInActiveState(searchProperties);

            // Every tab once, plus the starting tab a second time so the text on the far side of
            // the cursor is searched after the search has wrapped around.
            int maxTabVisits = _tabPageDataCollection.TabPageDictionary.Count + 1;
            for (int visit = 0; visit < maxTabVisits; visit++)
            {
                TextSearchResult textSearchResult = GetTextSearchResultInActiveState(searchProperties);
                if (textSearchResult.SearchTextFound)
                {
                    textSearchResult.TabIndex = _searchState.TabIndex;
                    return textSearchResult;
                }
            }

            return new TextSearchResult();
        }

        // StartPosDown is where the next downward match may begin; StartPosUp is where the next
        // upward match must end by (-1 meaning "from the end of the tab").
        private TextSearchResult GetTextSearchResultInActiveState(TextSearchProperties searchProperties)
        {
            var textSearchResult = new TextSearchResult();

            // Tabs may have been deleted since the search state was set (the Find dialog is modeless).
            if (_searchState.TabIndex >= _tabPageDataCollection.TabPageDictionary.Count)
                _searchState.TabIndex = 0;

            if (!_tabPageDataCollection.TabPageDictionary.TryGetValue(_searchState.TabIndex, out TabPageData tabPageData))
                return textSearchResult;

            string text = tabPageData.TabPageText;
            if (string.IsNullOrEmpty(text))
            {
                MoveToNextTab(searchProperties.SearchDirection);
                return textSearchResult;
            }

            StringComparison comparison = searchProperties.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            int matchPos;

            if (searchProperties.SearchDirection == TextSearchEvents.SearchDirection.Down)
            {
                _searchState.StartPosDown = Math.Clamp(_searchState.StartPosDown, 0, text.Length);
                matchPos = text.IndexOf(searchProperties.SearchText, _searchState.StartPosDown, comparison);
            }
            else
            {
                if (_searchState.StartPosUp < 0 || _searchState.StartPosUp > text.Length)
                    _searchState.StartPosUp = text.Length;

                // LastIndexOf only returns matches that fit entirely at or before startIndex.
                matchPos = _searchState.StartPosUp == 0 ? -1 : text.LastIndexOf(searchProperties.SearchText, _searchState.StartPosUp - 1, comparison);
            }

            if (matchPos >= 0)
            {
                _searchState.StartPosDown = matchPos + searchProperties.SearchText.Length;
                _searchState.StartPosUp = matchPos;

                textSearchResult.StartPos = matchPos;
                textSearchResult.Length = searchProperties.SearchText.Length;
                textSearchResult.SearchTextFound = true;
            }
            else
            {
                MoveToNextTab(searchProperties.SearchDirection);
            }

            return textSearchResult;
        }

        private void MoveToNextTab(TextSearchEvents.SearchDirection searchDirection)
        {
            _searchState.StartPosDown = 0;
            _searchState.StartPosUp = -1;

            if (searchDirection == TextSearchEvents.SearchDirection.Down)
                _searchState.TabIndex++;
            else
                _searchState.TabIndex--;

            if (_searchState.TabIndex >= _tabPageDataCollection.TabPageDictionary.Count)
                _searchState.TabIndex = 0;
            else if (_searchState.TabIndex < 0)
                _searchState.TabIndex = _tabPageDataCollection.TabPageDictionary.Count - 1;
        }
    }
}