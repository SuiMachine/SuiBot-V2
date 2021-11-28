using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace SuiBot_V2_Windows.Windows
{
	/// <summary>
	/// Interaction logic for AboutMe.xaml
	/// </summary>
	public partial class AboutMeWindow : Window
	{
		public AboutMeWindow()
		{
			InitializeComponent();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var snd = (Hyperlink)sender;
			Process.Start(snd.NavigateUri.ToString());
		}
	}
}
