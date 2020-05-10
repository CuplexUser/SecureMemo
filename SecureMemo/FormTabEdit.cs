using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autofac;
using SecureMemo.DataModels;
using SecureMemo.Library;
using SecureMemo.Managers;

namespace SecureMemo
{
    public partial class FormTabEdit : Form
    {
        private const int MaxLabelLength = 20;
        private const int MinLabelLength = 1;
        private readonly List<DraggableListItem> _listViewDataSource;
        private readonly ILifetimeScope _scope;
        private TabPageCollectionStates _tabPageCollectionStates;

        public FormTabEdit(ILifetimeScope scope)
        {
            _scope = scope;
            MainFormLogicManager logicManager = _scope.Resolve<MainFormLogicManager>();

            var tabPageDataCollection = logicManager.GetTabPageDataCollection();
            _listViewDataSource = tabPageDataCollection.Select(x => new DraggableListItem { Index = x.PageIndex, Label = x.TabPageLabel, PageData = x }).ToList();
            InitializeComponent();

            _tabPageCollectionStates = new TabPageCollectionStates();
            _tabPageCollectionStates.SetInitialState(tabPageDataCollection);
            

            scope.CurrentScopeEnding += Scope_CurrentScopeEnding;
        }

        private void Scope_CurrentScopeEnding(object sender, Autofac.Core.Lifetime.LifetimeScopeEndingEventArgs e)
        {
            _listViewDataSource?.ForEach(x => x.PageData = null);
            _listViewDataSource?.Clear();
            GC.Collect();
        }




        public bool TabDataChanged { get; private set; }

        //private void VerifyAndCorrectIndexing(TabPageDataCollection tabPageDataCollection)
        //{
        //    var pageIndexList = tabPageDataCollection.TabPageDictionary.Values.Select(x => x.PageIndex).ToList();
        //    bool incorrectPageIndexFound = pageIndexList.Any(i => i >= pageIndexList.Count) || tabPageDataCollection.TabPageDictionary.Any(x => x.Value.PageIndex != x.Key);
        //    if (!incorrectPageIndexFound) return;

        //    var tabPageDataList = tabPageDataCollection.TabPageDictionary.Values.ToList();
        //    tabPageDataCollection.TabPageDictionary.Clear();

        //    for (int i = 0; i < tabPageDataList.Count; i++)
        //    {
        //        tabPageDataList[i].PageIndex = i;
        //        tabPageDataCollection.TabPageDictionary.Add(i, tabPageDataList[i]);
        //    }


        //    TabDataChanged = true;
        //}

        private void btnOk_Click(object sender, EventArgs e)
        {
            MainFormLogicManager logicManager = _scope.Resolve<MainFormLogicManager>();
            //foreach (DraggableListItem draggableListItem in _listViewDataSource)
            //{
            //    int index = draggableListItem.Index;

            //    _tabPageDataCollection.TabPageDictionary.Add(index, draggableListItem.PageData);
            //    _
            //}

            // set types of updates and sync the model db in response.



            
            //int lastIndex = _tabPageDataCollection.TabPageDictionary.Values.Max(x => x.PageIndex);
            //if (_tabPageDataCollection.ActiveTabIndex > lastIndex)
            //    _tabPageDataCollection.ActiveTabIndex = lastIndex;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void frmTabEdit_Load(object sender, EventArgs e)
        {
            if (_listViewDataSource != null)
                LoadTabPageCollection();
        }

        private void LoadTabPageCollection()
        {
            listViewTabs.Clear();
            foreach (DraggableListItem draggableListItem in _listViewDataSource)
                listViewTabs.Items.Add(new ListViewItem { Text = draggableListItem.Label, ImageIndex = 0 });
        }

        private void listViewTabs_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label.Length <= MaxLabelLength && e.Label.Length >= MinLabelLength)
            {
                DraggableListItem listViewItem = _listViewDataSource.FirstOrDefault(x => x.Index == e.Item);
                if (listViewItem != null) listViewItem.Label = e.Label;
                TabDataChanged = true;
                return;
            }
            string textboxMessage = $"Invalid label length. The tab label must be between {MinLabelLength} and {MaxLabelLength} characters long";
            e.CancelEdit = true;
            MessageBox.Show(this, textboxMessage, "Unable to edit label", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void listViewTabs_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem listViewItem)
            {
                DraggableListItem draggableListItem = _listViewDataSource.FirstOrDefault(x => x.Index == listViewItem.Index);
                if (draggableListItem != null)
                    DoDragDrop(draggableListItem, DragDropEffects.Move);
            }
        }

        private void listViewTabs_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void listViewTabs_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DraggableListItem))) return;
            if (listViewTabs.SelectedItems.Count == 0)
                return;

            Point p = listViewTabs.PointToClient(MousePosition);
            ListViewItem liteViewItemClosestToDropPosition = GetClosestItemInRelationToDropPosition(listViewTabs, p);

            if (!(e.Data.GetData(typeof(DraggableListItem)) is DraggableListItem dragItem))
            {
                return;
            }

            if ((liteViewItemClosestToDropPosition == null || liteViewItemClosestToDropPosition.Index == dragItem.Index))
                return;

            int originalIndex = dragItem.Index;
            int newIndex = liteViewItemClosestToDropPosition.Index;

            if (radioButtonSwitch.Checked)
            {
                DraggableListItem listItem1 = _listViewDataSource.First(x => x.Index == newIndex);
                DraggableListItem listItem2 = _listViewDataSource.First(x => x.Index == originalIndex);

                listItem1.Index = originalIndex;
                listItem2.Index = newIndex;
            }
            else
            {
                int minIndex = Math.Min(newIndex, originalIndex);
                int maxIndex = Math.Max(newIndex, originalIndex);
                bool leftShift = newIndex > originalIndex;

                if (leftShift)
                {
                    var itemsToShiftIndex = _listViewDataSource.Where(x => x.Index > minIndex && x.Index <= maxIndex).OrderBy(x => x.Index).ToList();
                    DraggableListItem listViewItemToSwitch = _listViewDataSource.First(x => x.Index == minIndex);

                    foreach (DraggableListItem listItem in itemsToShiftIndex)
                        listItem.Index = listItem.Index - 1;

                    listViewItemToSwitch.Index = maxIndex;
                }
                else
                {
                    var itemsToShiftIndex = _listViewDataSource.Where(x => x.Index >= minIndex && x.Index < maxIndex).OrderBy(x => x.Index).ToList();
                    DraggableListItem listViewItemToSwitch = _listViewDataSource.First(x => x.Index == maxIndex);

                    foreach (DraggableListItem listItem in itemsToShiftIndex)
                        listItem.Index = listItem.Index + 1;

                    listViewItemToSwitch.Index = minIndex;
                }
            }

            TabDataChanged = true;
            _listViewDataSource.Sort();
            LoadTabPageCollection();
        }

        private ListViewItem GetClosestItemInRelationToDropPosition(ListView listView, Point p)
        {
            var allListViewItems = (from ListViewItem viewItem in listView.Items select viewItem).ToList();

            int xMax = allListViewItems.Max(x => x.Bounds.Right);
            int yMax = allListViewItems.Max(x => x.Bounds.Bottom);
            int xMin = allListViewItems.Min(x => x.Bounds.Left);
            int yMin = allListViewItems.Min(x => x.Bounds.Top);

            if (p.X > xMax)
                p.X = xMax;

            if (p.X < xMin)
                p.X = xMin;

            if (p.Y > yMax)
                p.Y = yMax;

            if (p.Y < yMin)
                p.Y = yMin;

            var allListViewItemsOnTheSameHorizontalLevel = allListViewItems.Where(x => x.Bounds.Top <= p.Y && x.Bounds.Bottom >= p.Y).ToList();
            return allListViewItemsOnTheSameHorizontalLevel.FirstOrDefault(x => x.Bounds.Left <= p.X && x.Bounds.Right >= p.X);
        }

        private void listViewTabs_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(DraggableListItem)))
                e.Effect = e.AllowedEffect;
        }

        private void addNewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int newIndex = _listViewDataSource.Max(x => x.Index) + 1;
            string tabPageLabel = "Page" + (newIndex);
            var tabPageData = new TabPageData { PageIndex = newIndex, TabPageLabel = tabPageLabel, TabPageText = "" };
            tabPageData.GenerateUniqueIdIfNoneExists();

            _listViewDataSource.Add(new DraggableListItem { Index = newIndex, Label = tabPageLabel, PageData = tabPageData });

            TabDataChanged = true;
            LoadTabPageCollection();
        }

        private void deleteSelectedTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTabs.SelectedItems.Count <= 0) return;
            ListViewItem itemToDelete = listViewTabs.SelectedItems[0];
            _listViewDataSource.RemoveAll(x => x.Index == itemToDelete.Index);

            //Reindex collection
            int pageIndex = 0;
            foreach (DraggableListItem draggableListItem in _listViewDataSource.OrderBy(x => x.PageData.PageIndex))
            {
                draggableListItem.Index = pageIndex;
                pageIndex++;
            }

            TabDataChanged = true;
            LoadTabPageCollection();
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            deleteSelectedTabToolStripMenuItem.Enabled = listViewTabs.SelectedItems.Count > 0;
        }

        private void FormTabEdit_ResizeBegin(object sender, EventArgs e)
        {
        }

        private void FormTabEdit_ResizeEnd(object sender, EventArgs e)
        {
            LoadTabPageCollection();
            listViewTabs.Refresh();
        }

        private class DraggableListItem : IComparable<DraggableListItem>
        {
            private int _index;
            private string _label;

            public int Index
            {
                get
                {
                    return _index;
                }
                set
                {
                    _index = value;
                    UpdatePageData();
                }
            }

            public string Label
            {
                get
                {
                    return _label;
                }
                set
                {
                    _label = value;
                    UpdatePageData();
                }

            }
            public TabPageData PageData { get; set; }

            private void UpdatePageData()
            {
                if (PageData != null)
                {
                    PageData.TabPageLabel = Label;
                    PageData.PageIndex = Index;
                }
            }

            public int CompareTo(DraggableListItem other)
            {
                return Index.CompareTo(other.Index);
            }

            public override string ToString()
            {
                return $"Label: {Label}, Index: {Index}";
            }
        }
    }
}