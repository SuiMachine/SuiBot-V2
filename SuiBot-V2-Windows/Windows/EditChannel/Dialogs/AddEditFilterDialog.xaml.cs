using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuiBot_V2_Windows.Windows.EditChannel.Dialogs
{
    /// <summary>
    /// Interaction logic for AddEditFilterDialog.xaml
    /// </summary>
    public partial class AddEditFilterDialog : Window
    {
        public SuiBot_Core.Storage.ChatFilter ReturnedFilter { get; private set; }
        private Regex regexInstance;
        private SolidColorBrush SuccessfullCompilationBG = new SolidColorBrush(Colors.Green);
        private SolidColorBrush FailedCompilationBG = new SolidColorBrush(Colors.Red);

        public AddEditFilterDialog(SuiBot_Core.Storage.ChatFilters.FilterType filterType, SuiBot_Core.Storage.ChatFilter editedFilter = null)
        {
            InitializeComponent();
            if(editedFilter != null)
            {
                ReturnedFilter = new SuiBot_Core.Storage.ChatFilter(editedFilter);
                switch(filterType)
                {
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Purge):
                        this.Title = "Edit Purge filter";
                        this.TB_Duration.IsEnabled = false;
                        this.ReturnedFilter.Duration = 1;
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Timeout):
                        this.Title = "Edit Timeout filter";
                        this.TB_Duration.IsEnabled = true;
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Ban):
                        this.Title = "Edit Ban filter";
                        this.TB_Duration.IsEnabled = false;
                        this.ReturnedFilter.Duration = 1;
                        break;
                }
            }
            else
            {
                ReturnedFilter = new SuiBot_Core.Storage.ChatFilter();
                switch (filterType)
                {
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Purge):
                        this.Title = "Add Purge filter";
                        this.TB_Duration.IsEnabled = false;
                        this.ReturnedFilter.Duration = 1;
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Timeout):
                        this.Title = "Add Timeout filter";
                        this.TB_Duration.IsEnabled = true;
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Ban):
                        this.Title = "Add Ban filter";
                        this.TB_Duration.IsEnabled = false;
                        this.ReturnedFilter.Duration = 1;
                        break;
                }
            }

            this.DataContext = ReturnedFilter;
            this.regexInstance = new Regex(ReturnedFilter.Syntax);
            this.OnFilterChanged(null, null);
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            if(this.ReturnedFilter.Syntax == "")
            {
                MessageBox.Show("Filter can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                regexInstance = new Regex(this.ReturnedFilter.Syntax, RegexOptions.IgnoreCase);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Regex failed to compile. Error:\n\n" +ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this.regexInstance = new Regex(TB_Filter.Text, RegexOptions.IgnoreCase);
                for (int i = 0; i < RB_ExampleLines.Document.Blocks.Count; i++)
                {
                    var currentBlock = RB_ExampleLines.Document.Blocks.ElementAt(i);
                    string text = new TextRange(currentBlock.ContentStart, currentBlock.ContentEnd).Text;
                    if (regexInstance.IsMatch(text))
                        currentBlock.Background = new SolidColorBrush(Colors.Red);
                    else
                        currentBlock.Background = new SolidColorBrush(Colors.White);
                }
                SetCompileStatus(true, "");
            }
            catch(Exception ex)
            {
                SetCompileStatus(false, ex.Message);
            }

        }

        private void SetCompileStatus(bool compiled, string exception)
        {
            if(compiled)
            {
                this.L_CompiledStatus.Content = "Compiled";
                this.L_CompiledStatus.Background = SuccessfullCompilationBG;
                this.L_CompilationError.Content = "";
            }
            else
            {
                this.L_CompiledStatus.Content = "Compilation error!";
                this.L_CompiledStatus.Background = FailedCompilationBG;
                this.L_CompilationError.Content = new TextBlock() { Text = exception,TextWrapping = TextWrapping.Wrap };
            }
        }
    }
}
