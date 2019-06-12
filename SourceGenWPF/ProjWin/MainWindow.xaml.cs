﻿/*
 * Copyright 2019 faddenSoft
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using CommonWPF;

namespace SourceGenWPF.ProjWin {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        /// <summary>
        /// Disassembled code display list provided to XAML.
        /// </summary>
        public DisplayList CodeDisplayList { get; private set; }

        /// Version string, for display.
        /// </summary>
        public string ProgramVersionString {
            get { return App.ProgramVersion.ToString(); }
        }

        /// <summary>
        /// Reference to controller object.
        /// </summary>
        private MainController mMainCtrl;

        /// <summary>
        /// Analyzed selection state.
        /// </summary>
        private MainController.SelectionState mSelectionState;

        // Handle to protected ListView.SetSelectedItems() method
        private MethodInfo listViewSetSelectedItems;


        public MainWindow() {
            InitializeComponent();

            listViewSetSelectedItems = codeListView.GetType().GetMethod("SetSelectedItems",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(listViewSetSelectedItems != null);

            this.DataContext = this;

            CodeDisplayList = new DisplayList();
            codeListView.ItemsSource = CodeDisplayList;
            // https://dlaa.me/blog/post/9425496 to re-auto-size after data added

            mMainCtrl = new MainController(this);

            mSelectionState = new MainController.SelectionState();

            AddMultiKeyGestures();

            //GridView gv = (GridView)codeListView.View;
            //gv.Columns[0].Width = 50;
        }

        private void AddMultiKeyGestures() {
            RoutedUICommand ruic;

            ruic = (RoutedUICommand)FindResource("HintAsCodeEntryPointCmd");
            ruic.InputGestures.Add(
                new MultiKeyInputGesture(new KeyGesture[] {
                      new KeyGesture(Key.H, ModifierKeys.Control, "Ctrl+H"),
                      new KeyGesture(Key.C, ModifierKeys.Control, "Ctrl+C")
                }));
            ruic = (RoutedUICommand)FindResource("HintAsDataStartCmd");
            ruic.InputGestures.Add(
                new MultiKeyInputGesture(new KeyGesture[] {
                      new KeyGesture(Key.H, ModifierKeys.Control, "Ctrl+H"),
                      new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl+D")
                }));
            ruic = (RoutedUICommand)FindResource("HintAsInlineDataCmd");
            ruic.InputGestures.Add(
                new MultiKeyInputGesture(new KeyGesture[] {
                      new KeyGesture(Key.H, ModifierKeys.Control, "Ctrl+H"),
                      new KeyGesture(Key.I, ModifierKeys.Control, "Ctrl+I")
                }));
            ruic = (RoutedUICommand)FindResource("RemoveHintsCmd");
            ruic.InputGestures.Add(
                new MultiKeyInputGesture(new KeyGesture[] {
                      new KeyGesture(Key.H, ModifierKeys.Control, "Ctrl+H"),
                      new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl+R")
                }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mMainCtrl.WindowLoaded();
            CreateCodeListContextMenu();

#if DEBUG
            // Get more info on CollectionChanged events that do not agree with current
            // state of Items collection.
            PresentationTraceSources.SetTraceLevel(codeListView.ItemContainerGenerator,
                PresentationTraceLevel.High);
#endif
        }

        private void CreateCodeListContextMenu() {
            // Find Actions menu.
            ItemCollection mainItems = this.appMenu.Items;
            MenuItem actionsMenu = null;
            foreach (object obj in mainItems) {
                if (!(obj is MenuItem)) {
                    continue;
                }
                MenuItem mi = (MenuItem)obj;
                if (mi.Name.Equals("ActionsMenu")) {
                    actionsMenu = mi;
                    break;
                }
            }
            Debug.Assert(actionsMenu != null);

            // Clone the Actions menu into the codeListView context menu.
            ContextMenu ctxt = this.codeListView.ContextMenu;
            foreach (object item in actionsMenu.Items) {
                if (item is MenuItem) {
                    MenuItem oldItem = (MenuItem)item;
                    MenuItem newItem = new MenuItem();
                    // I don't see a "clone" method, so just copy the fields we think we care about
                    newItem.Name = oldItem.Name;
                    newItem.Header = oldItem.Header;
                    newItem.Icon = oldItem.Icon;
                    newItem.InputGestureText = oldItem.InputGestureText;
                    newItem.Command = oldItem.Command;
                    ctxt.Items.Add(newItem);
                } else if (item is Separator) {
                    ctxt.Items.Add(new Separator());
                } else {
                    Debug.Assert(false, "Found weird thing in menu: " + item);
                }
            }
        }

        /// <summary>
        /// INotifyPropertyChanged event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Call this when a notification-worthy property changes value.
        /// 
        /// The CallerMemberName attribute puts the calling property's name in the first arg.
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool mShowCodeListView;

        /// <summary>
        /// Which panel are we showing, launchPanel or codeListView?
        /// </summary>
        public bool ShowCodeListView {
            get {
                return mShowCodeListView;
            }
            set {
                mShowCodeListView = value;
                OnPropertyChanged("LaunchPanelVisibility");
                OnPropertyChanged("CodeListVisibility");
            }
        }

        /// <summary>
        /// Returns the visibility status of the launch panel.
        /// (Intended for use from XAML.)
        /// </summary>
        public Visibility LaunchPanelVisibility {
            get { return mShowCodeListView ? Visibility.Hidden : Visibility.Visible; }
        }

        /// <summary>
        /// Returns the visibility status of the code ListView.
        /// (Intended for use from XAML.)
        /// </summary>
        public Visibility CodeListVisibility {
            get { return mShowCodeListView ? Visibility.Visible : Visibility.Hidden; }
        }

        /// <summary>
        /// Catch mouse-down events so we can treat the fourth mouse button as "back".
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.XButton1) {
                Debug.WriteLine("TODO: navigate back");
            }
        }

        /// <summary>
        /// Handles a double-click on the code list.  We have to figure out which row and
        /// column were clicked, which is not easy in WPF.
        /// </summary>
        private void CodeListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Debug.Assert(sender == codeListView);

            ListViewItem lvi = codeListView.GetClickedItem(e);
            if (lvi == null) {
                return;
            }
            DisplayList.FormattedParts parts = (DisplayList.FormattedParts)lvi.Content;
            int row = parts.ListIndex;
            int col = codeListView.GetClickEventColumn(e);
            if (col < 0) {
                return;
            }
            mMainCtrl.HandleDoubleClick(row, col);
        }


        #region Selection management

        private void CodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            //DateTime startWhen = DateTime.Now;

            // Update the selected-item bitmap.
            CodeDisplayList.SelectedIndices.SelectionChanged(e);

            // Notify MainController that the selection has changed.  This hands back an updated
            // selection summary, which is used for "can execute" methods.
            mMainCtrl.SelectionChanged(out mSelectionState);

            Debug.Assert(CodeDisplayList.SelectedIndices.DebugValidateSelectionCount(
                codeListView.SelectedItems.Count));

            //Debug.WriteLine("SelectionChanged took " +
            //    (DateTime.Now - startWhen).TotalMilliseconds + " ms");
        }

        /// <summary>
        /// Returns the number of selected items.
        /// </summary>
        /// <returns>
        /// The SelectedItems list appears to hold the full set, so we can just return the count.
        /// </returns>
        public int GetSelectionCount() {
            return codeListView.SelectedItems.Count;
        }

        /// <summary>
        /// Returns the index of the first selected item, or -1 if nothing is selected.
        /// </summary>
        /// <remarks>
        /// The ListView.SelectedIndex property returns the index of a selected item, but
        /// doesn't make guarantees about first or last.
        /// 
        /// This would be easier if the ListView kept SelectedItems in sorted order.  However,
        /// if you ctrl+click around you can get to a point where entry[0] is not the first
        /// and entry[count-1] is not the last selected item.
        /// 
        /// So we either have to walk the SelectedItems list or the DisplayListSelection array.
        /// For short selections the former will be faster than the later.  I'm assuming the
        /// common cases will be short selections and select-all, so this should handle both
        /// efficiently.
        /// </remarks>
        public int GetFirstSelectedIndex() {
            int count = codeListView.SelectedItems.Count;
            if (count == 0) {
                return -1;
            } else if (count < 500) {
                int min = CodeDisplayList.Count;
                foreach (DisplayList.FormattedParts parts in codeListView.SelectedItems) {
                    if (min > parts.ListIndex) {
                        min = parts.ListIndex;
                    }
                }
                Debug.Assert(min < CodeDisplayList.Count);
                return min;
            } else {
                return CodeDisplayList.SelectedIndices.GetFirstSelectedIndex();
            }
        }

        /// <summary>
        /// Returns the index of the last selected item, or -1 if nothing is selected.
        /// </summary>
        /// <remarks>
        /// Again, the ListView does not provide what we need.
        /// </remarks>
        public int GetLastSelectedIndex() {
            int count = codeListView.SelectedItems.Count;
            if (count == 0) {
                return -1;
            } else if (count < 500) {
                int max = -1;
                foreach (DisplayList.FormattedParts parts in codeListView.SelectedItems) {
                    if (max < parts.ListIndex) {
                        max = parts.ListIndex;
                    }
                }
                Debug.Assert(max >= 0);
                return max;
            } else {
                return CodeDisplayList.SelectedIndices.GetLastSelectedIndex();
            }
        }

        /// <summary>
        /// Sets the code list selection.
        /// </summary>
        /// <param name="sel">Selection bitmap.</param>
        public void SetSelection(DisplayListSelection sel) {
            const int MAX_SEL_COUNT = 2000;

            if (sel.IsAllSelected()) {
                Debug.WriteLine("SetSelection: re-selecting all items");
                codeListView.SelectAll();
                return;
            }
            Debug.Assert(codeListView.SelectedItems.Count == 0);    // expected
            codeListView.SelectedItems.Clear();                     // just in case

            if (sel.Count > MAX_SEL_COUNT) {
                // Too much for WPF -- only restore the first item.
                Debug.WriteLine("SetSelection: not restoring (" + sel.Count + " items)");
                codeListView.SelectedItems.Add(CodeDisplayList[sel.GetFirstSelectedIndex()]);
                return;
            }

            //DateTime startWhen = DateTime.Now;

            DisplayList.FormattedParts[] tmpArray = new DisplayList.FormattedParts[sel.Count];
            int ai = 0;
            foreach (int listIndex in sel) {
                tmpArray[ai++] = CodeDisplayList[listIndex];
            }

            // Use a reflection call to provide the full set.  This is much faster than
            // adding the items one at a time to SelectedItems.  (For one thing, it only
            // invokes the SelectionChanged method once.)
            listViewSetSelectedItems.Invoke(codeListView, new object[] { tmpArray });

            //Debug.WriteLine("SetSelection on " + sel.Count + " items took " +
            //    (DateTime.Now - startWhen).TotalMilliseconds + " ms");
        }

        public int GetCodeListTopIndex() {
            return codeListView.GetTopItemIndex();
        }

        public void SetCodeListTopIndex(int index) {
            // ScrollIntoView does the least amount of scrolling required.  This extension
            // method scrolls to the bottom, then scrolls back up to the top item.
            //
            // NOTE: it looks like scroll-to-bottom (which is done directly on the
            // ScrollViewer) happens immediately, whiel scroll-to-item (which is done via the
            // ListView) kicks in later.  So don't try to check the topmost item immediately.
            codeListView.ScrollToTopItem(CodeDisplayList[index]);
        }

        #endregion Selection management

        #region Can-execute handlers

        /// <summary>
        /// Returns true if the project is open.  Intended for use in XAML CommandBindings.
        /// </summary>
        private void IsProjectOpen(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mMainCtrl.IsProjectOpen();
        }

        private void CanHintAsCodeEntryPoint(object sender, CanExecuteRoutedEventArgs e) {
            if (!mMainCtrl.IsProjectOpen()) {
                e.CanExecute = false;
                return;
            }
            MainController.EntityCounts counts = mSelectionState.mEntityCounts;
            e.CanExecute = (counts.mDataLines > 0 || counts.mCodeLines > 0) &&
                (counts.mDataHints != 0 || counts.mInlineDataHints != 0 || counts.mNoHints != 0);
        }
        private void CanHintAsDataStart(object sender, CanExecuteRoutedEventArgs e) {
            if (!mMainCtrl.IsProjectOpen()) {
                e.CanExecute = false;
                return;
            }
            MainController.EntityCounts counts = mSelectionState.mEntityCounts;
            e.CanExecute = (counts.mDataLines > 0 || counts.mCodeLines > 0) &&
                (counts.mCodeHints != 0 || counts.mInlineDataHints != 0 || counts.mNoHints != 0);
        }
        private void CanHintAsInlineData(object sender, CanExecuteRoutedEventArgs e) {
            if (!mMainCtrl.IsProjectOpen()) {
                e.CanExecute = false;
                return;
            }
            MainController.EntityCounts counts = mSelectionState.mEntityCounts;
            e.CanExecute = (counts.mDataLines > 0 || counts.mCodeLines > 0) &&
                (counts.mCodeHints != 0 || counts.mDataHints != 0 || counts.mNoHints != 0);
        }
        private void CanRemoveHints(object sender, CanExecuteRoutedEventArgs e) {
            if (!mMainCtrl.IsProjectOpen()) {
                e.CanExecute = false;
                return;
            }
            MainController.EntityCounts counts = mSelectionState.mEntityCounts;
            e.CanExecute = (counts.mDataLines > 0 || counts.mCodeLines > 0) &&
                (counts.mCodeHints != 0 || counts.mDataHints != 0 || counts.mInlineDataHints != 0);
        }

        private void CanRedo(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mMainCtrl.CanRedo();
        }
        private void CanUndo(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mMainCtrl.CanUndo();
        }

        #endregion Can-execute handlers


        #region Command handlers

        private void AssembleCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            // test
            Debug.WriteLine("assembling");
        }

        private void CloseCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (!mMainCtrl.CloseProject()) {
                Debug.WriteLine("Close canceled");
            }
        }

        private void HelpCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.ShowHelp();
        }

        private void HintAsCodeEntryPointCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("hint as code entry point");
            mMainCtrl.MarkAsType(CodeAnalysis.TypeHint.Code, true);
        }

        private void HintAsDataStartCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("hint as data start");
            mMainCtrl.MarkAsType(CodeAnalysis.TypeHint.Data, true);
        }

        private void HintAsInlineDataCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("hint as inline data");
            mMainCtrl.MarkAsType(CodeAnalysis.TypeHint.InlineData, false);
        }

        private void RemoveHintsCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            Debug.WriteLine("remove hints");
            mMainCtrl.MarkAsType(CodeAnalysis.TypeHint.NoHint, false);
        }

        private void RedoCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.RedoChanges();
        }

        private void SelectAllCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            DateTime start = DateTime.Now;

            codeListView.SelectAll();

            //codeListView.SelectedItems.Clear();
            //foreach (var item in codeListView.Items) {
            //    codeListView.SelectedItems.Add(item);
            //}

            // This seems to be faster than setting items individually (10x), but is still O(n^2)
            // or worse, and hence unsuitable for very large lists.
            //codeListView.SelectedItems.Clear();
            //listViewSetSelectedItems.Invoke(codeListView, new object[] { codeListView.Items });

            Debug.WriteLine("Select All cmd: " + (DateTime.Now - start).TotalMilliseconds + " ms");
        }

        private void RecentProjectCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (!int.TryParse((string)e.Parameter, out int recentIndex) ||
                    recentIndex < 0 || recentIndex >= MainController.MAX_RECENT_PROJECTS) {
                throw new Exception("Bad parameter: " + e.Parameter);
            }

            Debug.WriteLine("Recent project #" + recentIndex);
            mMainCtrl.OpenRecentProject(recentIndex);
        }

        private void UndoCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
            mMainCtrl.UndoChanges();
        }

        #endregion Command handlers


        #region References panel

        public class ReferencesListItem {
            public string Offset { get; private set; }
            public string Addr { get; private set; }
            public string Type { get; private set; }

            public ReferencesListItem(string offset, string addr, string type) {
                Offset = offset;
                Addr = addr;
                Type = type;
            }

            public override string ToString() {
                return "[ReferencesListItem: off=" + Offset + " addr=" + Addr + " type=" +
                    Type + "]";
            }
        }

        public ObservableCollection<ReferencesListItem> ReferencesList { get; private set; } =
            new ObservableCollection<ReferencesListItem>();

        #endregion References panel


        #region Notes panel
        // TODO
        #endregion Notes panel


        #region Symbols panel

        public class SymbolsListItem {
            public Symbol Sym { get; private set; }
            public string Type { get; private set; }
            public string Value { get; private set; }
            public string Name { get; private set; }

            public SymbolsListItem(Symbol sym, string type, string value, string name) {
                Sym = sym;

                Type = type;
                Value = value;
                Name = name;
            }

            public override string ToString() {
                return "[SymbolsListItem: type=" + Type + " value=" + Value + " name=" +
                    Name + "]";
            }
        }

        public ObservableCollection<SymbolsListItem> SymbolsList { get; private set; } =
            new ObservableCollection<SymbolsListItem>();

        private void SymbolsList_Filter(object sender, FilterEventArgs e) {
            SymbolsListItem sli = (SymbolsListItem)e.Item;
            if (sli == null) {
                return;
            }
            if ((symUserLabels.IsChecked != true && sli.Sym.SymbolSource == Symbol.Source.User) ||
                (symProjectSymbols.IsChecked != true && sli.Sym.SymbolSource == Symbol.Source.Project) ||
                (symPlatformSymbols.IsChecked != true && sli.Sym.SymbolSource == Symbol.Source.Platform) ||
                (symAutoLabels.IsChecked != true && sli.Sym.SymbolSource == Symbol.Source.Auto) ||
                (symConstants.IsChecked != true && sli.Sym.SymbolType == Symbol.Type.Constant) ||
                (symAddresses.IsChecked != true && sli.Sym.SymbolType != Symbol.Type.Constant))
            {
                e.Accepted = false;
            } else {
                e.Accepted = true;
            }
        }

        /// <summary>
        /// Refreshes the symbols list when a filter option changes.  Set this to be called
        /// for Checked/Unchecked events on the filter option buttons.
        /// </summary>
        private void SymbolsListFilter_Changed(object sender, RoutedEventArgs e) {
            // This delightfully obscure call causes the list to refresh.  See
            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-group-sort-and-filter-data-in-the-datagrid-control
            CollectionViewSource.GetDefaultView(symbolsList.ItemsSource).Refresh();
        }

        /// <summary>
        /// Handles a Sorting event.  We want to do a secondary sort on Name when one of the
        /// other columns is the primary sort key.
        /// </summary>
        private void SymbolsList_Sorting(object sender, DataGridSortingEventArgs e) {
            DataGridColumn col = e.Column;

            // Set the SortDirection to a specific value.  If we don't do this, SortDirection
            // remains un-set, and the column header doesn't show up/down arrows or change
            // direction when clicked twice.
            ListSortDirection direction = (col.SortDirection != ListSortDirection.Ascending) ?
                ListSortDirection.Ascending : ListSortDirection.Descending;
            col.SortDirection = direction;

            bool isAscending = direction != ListSortDirection.Descending;

            IComparer comparer;

            switch (col.Header) {
                case "Type":
                    comparer = new SymTabSortComparer(SymTabSortField.CombinedType, isAscending);
                    break;
                case "Value":
                    comparer = new SymTabSortComparer(SymTabSortField.Value, isAscending);
                    break;
                case "Name":
                    comparer = new SymTabSortComparer(SymTabSortField.Name, isAscending);
                    break;
                default:
                    comparer = null;
                    Debug.Assert(false);
                    break;
            }

            ListCollectionView lcv =
                (ListCollectionView)CollectionViewSource.GetDefaultView(symbolsList.ItemsSource);
            lcv.CustomSort = comparer;
            e.Handled = true;
        }

        // Symbol table sort comparison helper.
        private enum SymTabSortField { CombinedType, Value, Name };
        private class SymTabSortComparer : IComparer {
            private SymTabSortField mSortField;
            private bool mIsAscending;

            public SymTabSortComparer(SymTabSortField prim, bool isAscending) {
                mSortField = prim;
                mIsAscending = isAscending;
            }

            // IComparer interface
            public int Compare(object oa, object ob) {
                Symbol a = ((SymbolsListItem)oa).Sym;
                Symbol b = ((SymbolsListItem)ob).Sym;

                // Label is always unique, so we use it as a secondary sort.
                if (mSortField == SymTabSortField.CombinedType) {
                    if (mIsAscending) {
                        int cmp = string.Compare(a.SourceTypeString, b.SourceTypeString);
                        if (cmp == 0) {
                            cmp = string.Compare(a.Label, b.Label);
                        }
                        return cmp;
                    } else {
                        int cmp = string.Compare(a.SourceTypeString, b.SourceTypeString);
                        if (cmp == 0) {
                            // secondary sort is always ascending, so negate
                            cmp = -string.Compare(a.Label, b.Label);
                        }
                        return -cmp;
                    }
                } else if (mSortField == SymTabSortField.Value) {
                    if (mIsAscending) {
                        int cmp;
                        if (a.Value < b.Value) {
                            cmp = -1;
                        } else if (a.Value > b.Value) {
                            cmp = 1;
                        } else {
                            cmp = string.Compare(a.Label, b.Label);
                        }
                        return cmp;
                    } else {
                        int cmp;
                        if (a.Value < b.Value) {
                            cmp = -1;
                        } else if (a.Value > b.Value) {
                            cmp = 1;
                        } else {
                            cmp = -string.Compare(a.Label, b.Label);
                        }
                        return -cmp;
                    }
                } else if (mSortField == SymTabSortField.Name) {
                    if (mIsAscending) {
                        return string.Compare(a.Label, b.Label);
                    } else {
                        return -string.Compare(a.Label, b.Label);
                    }
                } else {
                    Debug.Assert(false);
                    return 0;
                }
            }
        }

        #endregion Symbols panel


        #region Info panel

        /// <summary>
        /// Text to display in the Info panel.  This is a simple TextBox.
        /// </summary>
        public string InfoPanelContents {
            get {
                return mInfoBoxContents;
            }
            set {
                mInfoBoxContents = value;
                OnPropertyChanged();
            }
        }
        private string mInfoBoxContents;

        #endregion Info panel
    }
}