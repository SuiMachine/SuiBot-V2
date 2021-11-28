using System.Windows;

namespace SuiBot_V2_Windows.Windows.EditChannel
{
	/// <summary>
	/// Interaction logic for EditChatFiltersUsers.xaml
	/// </summary>
	public partial class EditChatFiltersUsers : Window
	{
		public SuiBot_Core.Storage.ChatFilterUsersDB ChatFilterUsersDB { get; private set; }

		public EditChatFiltersUsers(SuiBot_Core.Storage.ChatFilterUsersDB chatFilterUsersDB)
		{
			InitializeComponent();
			this.ChatFilterUsersDB = ChatFilterUsersDB;
		}

		private void Button_OKClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			this.Close();
		}

		private void Button_CancelClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			this.Close();
		}
	}
}
