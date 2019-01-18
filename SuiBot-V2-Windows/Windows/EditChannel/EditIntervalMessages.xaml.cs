using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SuiBot_V2_Windows.Windows.EditChannel
{
    /// <summary>
    /// Interaction logic for EditIntervalMessages.xaml
    /// </summary>
    public partial class EditIntervalMessages : Window
    {
        public SuiBot_Core.Storage.IntervalMessages IntervalMessages { get; private set; }

        public EditIntervalMessages(SuiBot_Core.Storage.IntervalMessages IntervalMessages)
        {
            InitializeComponent();
            this.IntervalMessages = IntervalMessages;
            this.DataContext = IntervalMessages;
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ContexMenuAddClicked(object sender, RoutedEventArgs e)
        {
            Dialogs.AddRemoveIntervalMessageDialog arimd = new Dialogs.AddRemoveIntervalMessageDialog();
            var result = arimd.ShowDialog();
            if (result != null && result == true)
            {
                IntervalMessages.Messages.Add(arimd.ReturnedIntervalMessage);
                ListView_IntervalMessages.Items.Refresh();
            }
        }

        private void ContexMenuEditClicked(object sender, RoutedEventArgs e)
        {
            if (ListView_IntervalMessages.SelectedItem != null)
            {
                var id = ListView_IntervalMessages.SelectedIndex;

                var msg = (SuiBot_Core.Storage.IntervalMessage)ListView_IntervalMessages.Items[id];

                Dialogs.AddRemoveIntervalMessageDialog arimd = new Dialogs.AddRemoveIntervalMessageDialog(msg);
                var result = arimd.ShowDialog();
                if(result != null && result == true)
                {
                    IntervalMessages.Messages[id] = arimd.ReturnedIntervalMessage;
                    ListView_IntervalMessages.Items.Refresh();
                }
            }
        }

        private void ContextMenuRemoveClicked(object sender, RoutedEventArgs e)
        {
            if(ListView_IntervalMessages.SelectedItem != null)
            {
                var id = ListView_IntervalMessages.SelectedIndex;
                IntervalMessages.Messages.RemoveAt(id);
                ListView_IntervalMessages.Items.Refresh();
            }
        }
    }
}
