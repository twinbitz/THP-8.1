﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Menu_Planner_with_Grocery_List
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class GroceryList : Menu_Planner_with_Grocery_List.Common.LayoutAwarePage
    {
        ObservableCollection<PlannerData> itemCollection = new ObservableCollection<PlannerData>();

        public GroceryList()
        {
            this.InitializeComponent();
        }

        //string folderName = "TheHandyPantry";
        string fileName = "groceryData.json";
        string imageFolder = "GroceryImages";
        
        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState == null)
            {
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                    this.itemListView.SelectedIndex = 1;
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (pageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    // TODO: Invoke this.itemsViewSource.View.MoveCurrentTo() with the selected
                    //       item as specified by the value of pageState["SelectedItem"]
                }
            }

            StorageFile localFile;
            try
            {
                localFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            }
            catch (FileNotFoundException ex)
            {
                NotifyUser(ex.Message);
                localFile = null;
            }
            if (localFile != null)
            {
                string localData = await FileIO.ReadTextAsync(localFile);
                itemCollection = JsonConvert.DeserializeObject<ObservableCollection<PlannerData>>(localData);
            }
            itemListView.ItemsSource = itemCollection;
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequest;
        }

        private void OnDataRequest(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            var item = (PlannerData)this.itemListView.SelectedItem;
            request.Data.Properties.Title = item.GroceryItem;
            request.Data.Properties.Description = "Grocery Item";

            // Share Snack item
            var groceryItem = "\r\nITEM\r\n";
            groceryItem += String.Join("\r\n", item.GroceryItem);
            groceryItem += ("\r\n\r\nSTORE TO BUY\r\n" + item.GroceryPlace);
            groceryItem += ("\r\n\r\nPRICE\r\n" + item.GroceryPrice);
            request.Data.SetText(groceryItem);

            // Share Image of cooked recipe
            var reference = RandomAccessStreamReference.CreateFromUri(new Uri(item.Image));
            request.Data.Properties.Thumbnail = reference;
            request.Data.SetBitmap(reference);
        }

        public void NotifyUser(string message)
        {
            this.statusText.Text = message;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected async override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = this.itemsViewSource.View.CurrentItem;
                // TODO: Derive a serializable navigation parameter and assign it to
                //       pageState["SelectedItem"]
                if (selectedItem != null)
                {
                    string itemTitle = ((PlannerData)selectedItem).GroceryItem;
                    pageState["SelectedItem"] = itemTitle;
                }
            }

            try
            {
                string localData = JsonConvert.SerializeObject(itemCollection);

                if (!string.IsNullOrEmpty(localData))
                {
                    StorageFile localFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(localFile, localData);
                }
            }
            catch (Exception ex)
            {
                NotifyUser(ex.Message);
            }
            DataTransferManager.GetForCurrentView().DataRequested -= OnDataRequest;
        }

        // Visual state management typically reflects the four application view states directly
        // (full screen landscape and portrait plus snapped and filled views.)  The split page is
        // designed so that the snapped and portrait view states each have two distinct sub-states:
        // either the item list or the details are displayed, but not both at the same time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed, or null
        /// for the current view state.  This parameter is optional with null as the default
        /// value.</param>
        /// <returns>True when the view state in question is portrait or snapped, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is Snapped)
        /// displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
            HideEdit(this, null);
            BottomAppBar.IsOpen = true;
        }

        /// <summary>
        /// Invoked when the page's back button is pressed.
        /// </summary>
        /// <param name="sender">The back button instance.</param>
        /// <param name="e">Event data that describes how the back button was clicked.</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // When logical page navigation is not in effect, or when there is no selected
                // item, use the default back button behavior.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed.</param>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Determine visual states for landscape layouts based not on the view state, but
            // on the width of the window.  This page has one layout that is appropriate for
            // 1366 virtual pixels or wider, and another for narrower displays or when a snapped
            // application reduces the horizontal space available to less than 1366.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscape";
                return "Filled";
            }

            // When in portrait or snapped start with the default visual state name, then add a
            // suffix when viewing details instead of the list
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        private void AddClick(object sender, RoutedEventArgs e)
        {
            PlannerData grocery = new PlannerData();
            grocery.GroceryItem = itemTitle.Text;
            grocery.GroceryPlace = itemSubtitle.Text;
            grocery.GroceryPrice = itemContent.Text;
            grocery.Image = displayImage.Name;
            itemCollection.Insert(0, grocery);
            HideEdit(this, null);
            this.itemListView.SelectedIndex = -1;
            this.itemListView.UpdateLayout();
            ClearFields();
        }

        private void ClearFields()
        {
            itemTitle.Text = string.Empty;
            itemSubtitle.Text = string.Empty;
            itemContent.Text = string.Empty;
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (itemListView.Items.Count > 0)
            {
                int index = 0;
                if (itemListView.SelectedItem != null)
                    index = itemListView.SelectedIndex;
                itemCollection.RemoveAt(index);
            }
        }

        private void editText(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "EditText", true);
            BottomAppBar.IsOpen = true;
        }

        private void HideEdit(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "DisplayTextOnly", true);
            BottomAppBar.IsOpen = false;
        }

        private void AddItemClick(object sender, RoutedEventArgs e)
        {
            // the next two lines of code make the selected item unselect before the text is cleared
            this.itemListView.SelectedIndex = -1;
            this.itemListView.UpdateLayout();
            editText(this, null);
            ClearFields();
        }

        private async void SyncClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string localData = JsonConvert.SerializeObject(itemCollection);
                if (!string.IsNullOrEmpty(localData))
                {
                    StorageFile localFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(localFile, localData);
                }
            }
            catch (Exception ex)
            {
                NotifyUser(ex.Message);
            }
            //HideEdit(this, null);
        }

        #region Photo Click
        private async void PhotoClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".bmp");
            StorageFile localFile = await picker.PickSingleFileAsync();
            if (localFile != null)
            {
                try
                {
                    StorageFolder localFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(imageFolder, CreationCollisionOption.OpenIfExists);
                    var result = await localFile.CopyAsync(localFolder);
                    string path = string.Format("ms-appdata:///local/GroceryImages/{0}", result.Name);
                    using (IRandomAccessStream ras = await localFile.OpenAsync(FileAccessMode.Read))
                    {
                        // Load the data into a BitmapImage
                        BitmapImage source = new BitmapImage();
                        source.SetSource(ras);
                        // Assign the BitmapImage to the element on the page
                        displayImage.Name = path;
                        displayImage.Source = source;
                        String mruToken = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(localFile);
                        String falToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(localFile);
                    }
                }
                catch (Exception ex)
                {
                    NotifyUser(ex.Message);
                }
            }
        }
        #endregion

        private void TappedClick(object sender, TappedRoutedEventArgs e)
        {
            editText(this, null);
        }


        ///Need to add more code to make this work properly
        ///
        //private void CheckedClick(object sender, RoutedEventArgs e)
        //{
        //    this.Checked();
        //}
        //private void Checked()
        //{
        //    switch (this.GLToggle.IsChecked)
        //    {
        //        case null:
        //            this.toggleTextBlock.Text = " ";
        //            break;
        //        case true:
        //            this.toggleTextBlock.Text = "In your cart";
        //            break;
        //        case false:
        //            this.toggleTextBlock.Text = "Not in cart";
        //            break;
        //    }
        //}
    }
}