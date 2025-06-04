using SuiBot_Core.Storage;
using System;
using System.Windows;

namespace SuiBot_V2_Windows.Windows.Settings
{
	/// <summary>
	/// Interaction logic for ConnectionSettingsWindow.xaml
	/// </summary>
	public partial class ConnectionSettingsWindow : Window
	{
		public ConnectionConfig ConnectionConfig { get; private set; }

		public ConnectionSettingsWindow(ConnectionConfig connectionConfig)
		{
			InitializeComponent();
			this.ConnectionConfig = connectionConfig;
			this.DataContext = ConnectionConfig;

			//For whatever reason (security?) you need to jump through hoops and loops to do binding on password, so here is plain old manual way
			this.PassBox_Password.Password = ConnectionConfig.Password;
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
		}

		private void OKClicked(object sender, RoutedEventArgs e)
		{
			this.ConnectionConfig.Password = this.PassBox_Password.Password.Trim();
			this.DialogResult = true;
			this.Close();
		}

		private void CancelClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void TestConnectionClicked(object sender, RoutedEventArgs e)
		{
			this.ConnectionConfig.Password = this.PassBox_Password.Password.Trim();

			SuiBot_Core.SuiBot sb = SuiBot_Core.SuiBot.GetInstance(this.ConnectionConfig, SuiBot_Core.Storage.CoreConfig.Load());
			var result = sb.VerifyAuthy();
			if(string.IsNullOrEmpty(result))
				MessageBox.Show("Failed to verify login!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			else
			{
				MessageBox.Show(result, "Result", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void ObtainAuthy(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(SuiBot_Core.SuiBot.GetAuthenticationURL());
			MessageBox.Show("Authentication url was copied to your clipboard. Please paste it in browser address field, where you are logged in to an account you want to use as a bot and authenticate it. Then copy the oauth key returned to you to password password field. Whitespaces at the beginning and end of a string will be removed, so don't worry about them.\n\n" +
				"Make sure not to display neither the web browser or authy key on stream during this process.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
