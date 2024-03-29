﻿using System.Windows;

namespace SuiBot_V2_Windows.Windows.EditChannel
{
	/// <summary>
	/// Interaction logic for EditQuotes.xaml
	/// </summary>
	public partial class EditQuotes : Window
	{
		public SuiBot_Core.Storage.Quotes Quotes { get; private set; }

		public EditQuotes(SuiBot_Core.Storage.Quotes Quotes)
		{
			InitializeComponent();
			this.Quotes = Quotes;
			this.DataContext = this.Quotes;
		}

		private void ContextMenuAddClicked(object sender, RoutedEventArgs e)
		{
			Dialogs.AddEditQuoteDialog aeqd = new Dialogs.AddEditQuoteDialog();
			var result = aeqd.ShowDialog();
			if (result != null && result == true)
			{
				Quotes.QuotesList.Add(aeqd.ReturnedQuote);
				ListView_Quotes.Items.Refresh();
			}
		}

		private void ContextMenuEditClicked(object sender, RoutedEventArgs e)
		{
			if (ListView_Quotes.SelectedItem != null)
			{
				var id = ListView_Quotes.SelectedIndex;
				var tmpQuote = (SuiBot_Core.Storage.Quote)ListView_Quotes.Items[id];
				Dialogs.AddEditQuoteDialog aeqd = new Dialogs.AddEditQuoteDialog(tmpQuote);
				var result = aeqd.ShowDialog();
				if (result != null && result == true)
				{
					Quotes.QuotesList[id] = aeqd.ReturnedQuote;
					ListView_Quotes.Items.Refresh();
				}


			}
		}

		private void ContextMenuRemoveClicked(object sender, RoutedEventArgs e)
		{
			if (ListView_Quotes.SelectedItem != null)
			{
				var id = ListView_Quotes.SelectedIndex;
				Quotes.QuotesList.RemoveAt(id);
				ListView_Quotes.Items.Refresh();
			}
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
	}
}
