using System.Windows;
using System.Windows.Input;

namespace SuiBot_V2_Windows.Windows.EditChannel
{
	/// <summary>
	/// Interaction logic for EditFeatures.xaml
	/// </summary>
	public partial class EditFeatures : Window
	{
		public SuiBot_Core.Storage.ChannelConfig ReturnedChannelConfig { get; private set; }

		public EditFeatures(SuiBot_Core.Storage.ChannelConfig ChannelConfig)
		{
			InitializeComponent();
			this.ReturnedChannelConfig = ChannelConfig;
			this.DataContext = ReturnedChannelConfig;
		}

		private void Button_OKClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}

		private void Button_CancelClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void ContextMenu_OnAddClicked(object sender, RoutedEventArgs e)
		{
			Dialogs.AddEditUsernameDialog aeu = new Dialogs.AddEditUsernameDialog();
			var result = aeu.ShowDialog();
			if (result != null && result == true)
			{
				ReturnedChannelConfig.SuperMods.Add(aeu.ReturnedUsername);
				ListBox_SuperMods.Items.Refresh();
			}
		}

		private void ContextMenu_OnEditClicked(object sender, RoutedEventArgs e)
		{
			if (ListBox_SuperMods.SelectedItem != null)
			{
				var id = ListBox_SuperMods.SelectedIndex;

				Dialogs.AddEditUsernameDialog aeu = new Dialogs.AddEditUsernameDialog(ReturnedChannelConfig.SuperMods[id]);
				var result = aeu.ShowDialog();
				if (result != null && result == true)
				{
					ReturnedChannelConfig.SuperMods[id] = aeu.ReturnedUsername;
					ListBox_SuperMods.Items.Refresh();
				}
			}

		}

		private void ContextMenu_OnRemoveClicked(object sender, RoutedEventArgs e)
		{
			if (ListBox_SuperMods.SelectedItem != null)
			{
				var id = ListBox_SuperMods.SelectedIndex;
				ReturnedChannelConfig.SuperMods.RemoveAt(id);
				ListBox_SuperMods.Items.Refresh();
			}
		}

		private void SuperModsList_OnKeyDown(object sender, KeyEventArgs e)
		{
			this.ContextMenu_OnRemoveClicked(null, null);
		}
	}
}
