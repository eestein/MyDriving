using Foundation;
using System;
using System.Collections.Generic;
using UIKit;
using System.Globalization;

using MyTrips.Model;
using MyTrips.ViewModel;

namespace MyTrips.iOS
{
	partial class SettingsViewController : UIViewController
	{
		SettingsViewModel ViewModel { get; set; }

		string[] keys;

		public SettingsViewController(IntPtr handle) : base(handle) { }

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Wire up view model
			ViewModel = new SettingsViewModel();

			keys = new string[ViewModel.SettingsData.Count];
			int i = 0;
			foreach (var grouping in ViewModel.SettingsData) {
				keys[i] = grouping.Key;
				i++;
			}

			// Wire up table source
			settingsTableView.Source = new SettingsDataSource(ViewModel, keys);

			btnLogout.TouchUpInside += async delegate {
				await ViewModel.ExecuteLogoutCommandAsync();

				InvokeOnMainThread(() =>
				{
					var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;
					var viewController = UIStoryboard.FromName("Main", null).InstantiateViewController("loginViewController"); // Storyboard.InstantiateViewController("loginViewController");
					appDelegate.Window.RootViewController = viewController;

					Acr.UserDialogs.UserDialogs.Instance.ShowSuccess("Successfully logged out");
				});
			};

			btnLogout.Layer.CornerRadius = 4;
			btnLogout.Layer.MasksToBounds = true;

			NSNotificationCenter.DefaultCenter.AddObserver(new NSString ("RefreshSettingsTable"), HandleReloadTableNotification); 
		}

		public override bool ShouldPerformSegue(string segueIdentifier, NSObject sender)
		{
			var cell = (SettingTableViewCell)sender;
			if (cell.Accessory == UITableViewCellAccessory.None)
				return false;
			else
				return true;
		}

		public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "settingsDetailSegue")
			{
				var controller = segue.DestinationViewController as SettingsDetailViewController;
				var cell = settingsTableView.IndexPathForCell(sender as UITableViewCell);
				var row = cell.Row;
				var section = cell.Section;

				var setting = ViewModel.SettingsData[keys[section]][row];

				settingsTableView.DeselectRow(settingsTableView.IndexPathForSelectedRow, true);

				controller.Setting = setting;
				controller.ViewModel = ViewModel;
				controller.SettingKey = keys[cell.Section];
			}
		}

		void HandleReloadTableNotification(NSNotification obj)
		{
			InvokeOnMainThread(delegate
			{
				settingsTableView.ReloadData();
			});
		}
	}

	// TODO: Alter shared logic
	public class SettingsDataSource : UITableViewSource
	{
		Dictionary<string, List<Setting>> data;
		SettingsViewModel viewModel;

		string[] keys;

		public SettingsDataSource(SettingsViewModel viewModel, string[] keys)
		{
			this.viewModel = viewModel;
			data = viewModel.SettingsData;

			this.keys = keys;
		}

		public override void WillDisplayHeaderView(UITableView tableView, UIView headerView, nint section)
		{
			var header = headerView as UITableViewHeaderFooterView;
			header.TextLabel.TextColor = "5C5C5C".ToUIColor();
			header.TextLabel.Font = UIFont.FromName("AvenirNext-Medium", 16);
			header.TextLabel.Text = TitleForHeader(tableView, section);
		}

		public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var setting = data[keys[indexPath.Section]][indexPath.Row];
			if (setting.IsButton)
			{
				if (setting.ButtonUrl != "Permissions")
				{
					await viewModel.ExecuteOpenBrowserCommandAsync(setting.ButtonUrl);
				}
				else
				{
					var url = NSUrl.FromString(UIApplication.OpenSettingsUrlString);
					UIApplication.SharedApplication.OpenUrl(url);
				}
			}
		}

		public override nint NumberOfSections(UITableView tableView)
		{
			return keys.Length;
		}

		public override string TitleForHeader(UITableView tableView, nint section)
		{
			return keys[section];
		}

		public override nint RowsInSection(UITableView tableView, nint section)
		{
			return data[keys[section]].Count;
		}

		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			var cell = tableView.DequeueReusableCell("SETTING_VALUE_CELL") as SettingTableViewCell;

			var setting = data[keys[indexPath.Section]][indexPath.Row];
			if (setting.IsButton)
			{
				cell.Accessory = UITableViewCellAccessory.None;
				cell.Value = "";
			}
			else
			{
				cell.Value = !String.IsNullOrEmpty(setting.Value) ? cell.Value = setting.Value : cell.Value = "";
			}

			cell.Name = setting.Name;

			return cell;
		}
	}
}